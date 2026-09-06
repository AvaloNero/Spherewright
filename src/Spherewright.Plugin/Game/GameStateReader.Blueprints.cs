using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Factory;
using Spherewright.Contracts.Sessions;
using UnityEngine;

namespace Spherewright.Plugin.Game;

internal sealed partial class GameStateReader
{
    public GameCallResult<BlueprintInspection> InspectBlueprintOnMainThread(string? sessionId, InspectBlueprintRequest request)
    {
        var error = ValidateOwnedPlanetOnMainThread(sessionId, request.PlanetId, out _);
        if (error is not null) return GameCallResult<BlueprintInspection>.Failed(error);
        try { return GameCallResult<BlueprintInspection>.Succeeded(ReadBlueprint(sessionId!, request.PlanetId, request.BlueprintCode)); }
        catch (BlueprintReadException exception) { return BlueprintRejected(exception.Reason); }
        catch (Exception) { return BlueprintRejected("blueprint_native_read_failed"); }
    }

    public GameCallResult<BlueprintInspection> ExportBlueprintOnMainThread(string? sessionId, ExportBlueprintRequest request)
    {
        var error = ValidateOwnedPlanetOnMainThread(sessionId, request.PlanetId, out var factory);
        if (error is not null) return GameCallResult<BlueprintInspection>.Failed(error);
        if (request.Entities is null || request.Entities.Count < 1 || request.Entities.Count > BoundedBlueprintReader.MaximumObjects
            || request.Entities.Any(e => e is null || e.ObjectId <= 0 || string.IsNullOrWhiteSpace(e.ExpectedEndpointStateHash))
            || request.Entities.Select(e => e.ObjectId).Distinct().Count() != request.Entities.Count)
            return BlueprintRejected("blueprint_selection_invalid");
        try
        {
            var snapshots = new List<FactoryEntitySnapshot>();
            foreach (var selected in request.Entities)
            {
                var entity = TryCaptureFactoryEntity(factory!, selected.ObjectId);
                if (entity is null || entity.EndpointStateHash != selected.ExpectedEndpointStateHash
                    || entity.RecipeId != selected.ExpectedRecipeId)
                    return GameCallResult<BlueprintInspection>.Failed(BridgeError.Create(BridgeErrorCodes.StaleState,
                        "An explicitly selected blueprint entity, endpoint or recipe changed.", false, "Fresh inspect the entire explicit selection."));
                if (!BoundedBlueprintReader.SupportsItem(entity.ItemId)) return BlueprintRejected("blueprint_type_unsupported");
                if (snapshots.Count > 0 && Vector3.Distance(ToBlueprintVector(snapshots[0].Position), ToBlueprintVector(entity.Position)) > 64f)
                    return BlueprintRejected("blueprint_selection_span_limit");
                snapshots.Add(entity);
            }
            if (!ReferenceEquals(factory, factory!.planet.factory) || !factory.planet.factoryLoaded || factory.planet.aux is null)
                return BlueprintRejected("blueprint_native_factory_unavailable");
            var ids = snapshots.Select(e => e.ObjectId).ToArray();
            var blueprint = new BlueprintData();
            blueprint.ResetAsEmpty();
            blueprint.shortDesc = "Spherewright selected module";
            blueprint.desc = "Explicit owned-world selection. Source-boundary connections require separate site planning.";
            blueprint.author = string.Empty; blueprint.customVersion = string.Empty; blueprint.externalFields = string.Empty;
            var origin = ToBlueprintVector(snapshots[0].Position).normalized;
            var divideLongitude = BlueprintUtils.GetLongitudeRad(origin) - Mathf.PI;
            // Only positive completed IDs: the native negative/prebuild branch can refresh displays.
            BlueprintUtils.GenerateBlueprintData(blueprint, factory.planet, factory.planet.aux, ids, ids.Length,
                divideLongitude, null!, false);
            var code = blueprint.ToBase64String();
            var result = ReadBlueprint(sessionId!, request.PlanetId, code);
            if (result.Objects.Count != snapshots.Count) return BlueprintRejected("blueprint_selection_readback_mismatch");
            for (var i = 0; i < snapshots.Count; i++)
            {
                if (result.Objects[i].ItemId != snapshots[i].ItemId || result.Objects[i].RecipeId != snapshots[i].RecipeId
                    || result.Objects[i].FilterItemId != (snapshots[i].FilterItemId ?? 0))
                    return BlueprintRejected("blueprint_selection_readback_mismatch");
                foreach (var connection in snapshots[i].Connections.Where(c => !ids.Contains(c.OtherObjectId)))
                    result.SourceBoundaryConnections.Add(new BlueprintBoundaryConnection
                    {
                        SourceBlueprintIndex = i, SourceObjectId = snapshots[i].ObjectId, Slot = connection.Slot,
                        IsOutput = connection.IsOutput, OtherObjectId = connection.OtherObjectId, OtherSlot = connection.OtherSlot,
                    });
            }
            result.ExportedBlueprintCode = code;
            return GameCallResult<BlueprintInspection>.Succeeded(result);
        }
        catch (BlueprintReadException exception) { return BlueprintRejected(exception.Reason); }
        catch (Exception) { return BlueprintRejected("blueprint_native_export_failed"); }
    }

    private BlueprintInspection ReadBlueprint(string sessionId, int planetId, string code)
    {
        var bounded = BoundedBlueprintReader.Read(code);
        var native = new BlueprintData();
        if (native.HeaderFromBase64String(code) != BlueprintDataIOError.OK || native.CheckSignature(code) != BlueprintDataIOError.OK)
            throw new BlueprintReadException("blueprint_native_header_or_signature_invalid");
        using (var stream = new MemoryStream(bounded.Payload, false))
        using (var reader = new BinaryReader(stream)) native.Import(reader);
        // Exact roundtrip also checks the independently parsed binary layout against this DLL.
        using (var stream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true)) native.Export(writer);
            if (!stream.ToArray().SequenceEqual(bounded.Payload)) throw new BlueprintReadException("blueprint_native_roundtrip_mismatch");
        }
        var result = bounded.Inspection;
        result.NativeSignatureVerified = true;
        result.SessionId = sessionId; result.PlanetId = planetId;
        result.Revision = _sessions.CaptureOnMainThread().Revision; result.CapturedAtGameTick = GameMain.gameTick;
        result.UntrustedTitle = native.shortDesc; result.UntrustedDescription = native.desc;
        foreach (var obj in result.Objects)
        {
            var item = LDB.items.Select(obj.ItemId);
            if (item?.prefabDesc is null || !item.CanBuild || item.ModelIndex != obj.ModelIndex)
                throw new BlueprintReadException("blueprint_native_model_unsupported");
            obj.Role = GetPrefabComponentKind(item.prefabDesc);
            obj.BuildingUnlocked = GameMain.history.ItemUnlocked(item.ID);
            if (obj.RecipeId > 0)
            {
                var recipe = LDB.recipes.Select(obj.RecipeId);
                if (recipe is null || !item.prefabDesc.isAssembler || recipe.Type != item.prefabDesc.assemblerRecipeType)
                    throw new BlueprintReadException("blueprint_native_recipe_unsupported");
            }
            obj.RecipeUnlocked = obj.RecipeId == 0 || GameMain.history.RecipeUnlocked(obj.RecipeId);
            if (obj.FilterItemId > 0 && LDB.items.Select(obj.FilterItemId) is null)
                throw new BlueprintReadException("blueprint_native_filter_invalid");
        }
        var player = GameMain.mainPlayer;
        foreach (var group in result.Objects.GroupBy(o => o.ItemId).OrderBy(g => g.Key))
        {
            var available = player.package.GetItemCount(group.Key);
            result.ConstructionItems.Add(new FoundryInventoryBudget
            { ItemId = group.Key, RequiredCount = group.Count(), PackageCount = available, MissingCount = Math.Max(0, group.Count() - available) });
        }
        return result;
    }

    private static Vector3 ToBlueprintVector(Vector3Snapshot v) => new Vector3(v.X, v.Y, v.Z);
    private static GameCallResult<BlueprintInspection> BlueprintRejected(string reason) =>
        GameCallResult<BlueprintInspection>.Failed(BridgeError.Create(BridgeErrorCodes.InvalidRequest, reason, false,
            "Provide a bounded supported native blueprint code or fresh explicit owned-world selection; no unsupported content is silently dropped."));
}
