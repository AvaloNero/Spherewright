using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Factory;
using Spherewright.Contracts.Sessions;
using UnityEngine;

namespace Spherewright.Plugin.Game;

internal sealed partial class GameStateReader
{
    private GameCallResult<BlueprintSiteSnapshot> InspectBlueprintSiteOnMainThread(string sessionId, int planetId,
        BlueprintSiteRequest request, BlueprintInspection inspection, BlueprintData native)
    {
        BlueprintSitePolicy.ValidateRequest(request);
        var error = ValidateOwnedPlanetOnMainThread(sessionId, planetId, out var factory);
        if (error is not null) return GameCallResult<BlueprintSiteSnapshot>.Failed(error);
        var player = GetPlayerStateOnMainThread(sessionId, new LocalPlanetRequest { PlanetId = planetId });
        if (!player.Success) return GameCallResult<BlueprintSiteSnapshot>.Failed(player.Error!);
        if (request.ExpectedPlayerStateHash != player.Value!.StateHash)
            return GameCallResult<BlueprintSiteSnapshot>.Failed(BridgeError.Create(BridgeErrorCodes.StaleState,
                "The player changed after the requested blueprint site inspection.", false, "Fresh read and assess again."));
        if (factory!.entityCursor > 131072 || factory.prebuildCursor > 131072
            || factory.planet.aux?.activeGrid is null || !factory.planet.factoryLoaded
            || GameMain.mainPlayer.controller?.actionBuild?.model is null)
            throw new BlueprintReadException("blueprint_site_factory_unavailable_or_limit");
        var position = ToBlueprintVector(request.Position);
        if (Math.Abs(position.magnitude - factory.planet.realRadius) > 10)
            throw new BlueprintReadException("blueprint_site_not_surface");
        var connections = BlueprintSitePolicy.BuildConnections(inspection, inspection.Objects
            .Select(o => o.ItemId).Distinct().ToDictionary(id => id, id => LDB.items.Select(id).prefabDesc.slotPoses?.Length ?? 0));
        var result = new BlueprintSiteSnapshot
        {
            SessionId = sessionId, PlanetId = planetId, Revision = inspection.Revision,
            CapturedAtGameTick = GameMain.gameTick, BlueprintHash = inspection.BlueprintHash,
            Position = CaptureVector(position), QuarterTurns = request.QuarterTurns,
            Connections = connections, ConstructionItems = inspection.ConstructionItems,
            InventorySufficient = inspection.ConstructionItems.All(i => i.MissingCount == 0),
            NativeBlueprintObjectLimit = GameMain.history.blueprintLimit,
            TechnologySatisfied = GameMain.history.blueprintLimit >= inspection.Objects.Count
                && inspection.Objects.All(o => o.BuildingUnlocked && o.RecipeUnlocked
                    && (o.FilterItemId == 0 || GameMain.history.ItemUnlocked(o.FilterItemId))
                    && (o.ItemId != 2101 || BlueprintStoragePolicy.FiltersUnlocked(o.Parameters, GameMain.history.ItemUnlocked))),
        };
        if (!result.TechnologySatisfied) result.Blockers.Add("blueprint_or_item_or_recipe_technology_locked");
        if (!result.InventorySufficient) result.Blockers.Add("whole_blueprint_inventory_insufficient");
        using (var tool = new SpherewrightBlueprintBuildTool())
        {
            tool._Init(GameMain.data);
            tool.SetFactoryReferences();
            if (!ReferenceEquals(tool.factory, factory)) throw new BlueprintReadException("blueprint_site_factory_mismatch");
            tool.Translate(native, position, request.QuarterTurns);
            if (tool.bpCursor != inspection.Objects.Count) throw new BlueprintReadException("blueprint_site_native_object_mismatch");
            for (var i = 0; i < tool.bpCursor; i++)
            {
                var preview = tool.bpPool[i];
                if (Vector3.Distance(position, preview.lpos) > 64 || Vector3.Distance(position, preview.lpos2) > 64)
                    throw new BlueprintReadException("blueprint_site_span_limit");
                result.Objects.Add(CaptureBlueprintSiteObject(preview, i, BlueprintSitePolicy.Dependencies(inspection, i)));
            }
            NormalGameActionCoordinator.InspectNewBlueprintPreviews(factory, tool, result);
            // Native conditions can adjust sorter span and endpoint poses. Expose exact results,
            // never let a Cartesian approximation or a pre-translation pose become a write plan.
            for (var i = 0; i < tool.bpCursor; i++)
            {
                var captured = CaptureBlueprintSiteObject(tool.bpPool[i], i, result.Objects[i].Dependencies);
                captured.OccupiedObjectId = result.Objects[i].OccupiedObjectId;
                result.Objects[i] = captured;
            }
        }
        var catalog = GetBuildCatalogOnMainThread(sessionId);
        if (!catalog.Success) return GameCallResult<BlueprintSiteSnapshot>.Failed(catalog.Error!);
        result.Power = AssessFoundryPowerOnMainThread(factory, result.Objects, catalog.Value!.Buildings);
        // Independent capture hash: changing native current power counters must not
        // turn this advisory assessment into a per-tick-stale construction token.
        result.AssessmentHash = BlueprintSitePolicy.AssessmentHash(result, player.Value.StateHash);
        return GameCallResult<BlueprintSiteSnapshot>.Succeeded(result);
    }

    private static BlueprintSiteObject CaptureBlueprintSiteObject(BuildPreview preview, int index, List<int> dependencies) => new BlueprintSiteObject
    {
        Index = index, ItemId = preview.item.ID, RecipeId = preview.recipeId, FilterItemId = preview.filterId,
        Position = CaptureVector(preview.lpos), Position2 = CaptureVector(preview.lpos2),
        Rotation = new QuaternionSnapshot { X = preview.lrot.x, Y = preview.lrot.y, Z = preview.lrot.z, W = preview.lrot.w },
        Rotation2 = new QuaternionSnapshot { X = preview.lrot2.x, Y = preview.lrot2.y, Z = preview.lrot2.z, W = preview.lrot2.w },
        Tilt = preview.tilt, InputOffset = preview.inputOffset, OutputOffset = preview.outputOffset,
        Parameters = (preview.parameters ?? Array.Empty<int>()).Take(preview.paramCount).ToArray(),
        NativeCondition = preview.condition.ToString(), Dependencies = dependencies,
        InputEndpointFacingDot = CaptureBlueprintEndpointFacing(preview, output: false),
        OutputEndpointFacingDot = CaptureBlueprintEndpointFacing(preview, output: true),
    };

    private static float? CaptureBlueprintEndpointFacing(BuildPreview preview, bool output)
    {
        if (!preview.desc.isInserter) return null;
        var device = output ? preview.output : preview.input;
        var slot = output ? preview.outputToSlot : preview.inputFromSlot;
        if (device is null || device.desc.isBelt || device.desc.slotPoses is null
            || slot < 0 || slot >= device.desc.slotPoses.Length) return null;
        var outward = device.desc.slotPoses[slot].GetTransformedBy(new Pose(device.lpos, device.lrot)).forward;
        var direction = output ? preview.lpos - preview.lpos2 : preview.lpos2 - preview.lpos;
        return direction.sqrMagnitude < .000001f ? (float?)null : Vector3.Dot(outward, direction.normalized);
    }
}
