using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Factory;
using Spherewright.Contracts.Sessions;
using UnityEngine;

namespace Spherewright.Plugin.Game;

internal sealed partial class GameStateReader
{
    public GameCallResult<FoundryPlanSnapshot> GetFoundryPlanOnMainThread(
        string? requestedSessionId, GetFoundryPlanRequest request)
    {
        var recipes = GetRecipeCatalogOnMainThread(requestedSessionId, new LocalPlanetRequest { PlanetId = request.PlanetId });
        if (!recipes.Success || recipes.Value is null) return GameCallResult<FoundryPlanSnapshot>.Failed(recipes.Error!);
        var catalog = GetBuildCatalogOnMainThread(requestedSessionId);
        if (!catalog.Success || catalog.Value is null) return GameCallResult<FoundryPlanSnapshot>.Failed(catalog.Error!);
        try
        {
            var plan = FoundryPlanCompiler.Compile(request, recipes.Value, catalog.Value.Buildings);
            if (request.Site is not null)
            {
                FoundrySitePlanner.ValidateRequest(request.Site);
                var accessError = ValidateOwnedPlanetOnMainThread(requestedSessionId, request.PlanetId, out var factory);
                if (accessError is not null) return GameCallResult<FoundryPlanSnapshot>.Failed(accessError);
                var playerResult = GetPlayerStateOnMainThread(requestedSessionId, new LocalPlanetRequest { PlanetId = request.PlanetId });
                if (!playerResult.Success || playerResult.Value is null)
                    return GameCallResult<FoundryPlanSnapshot>.Failed(playerResult.Error!);
                var player = GameMain.mainPlayer!;
                if (factory!.entityCursor > 131072 || factory.prebuildCursor > 131072)
                    throw new FoundryPlanningException("site_factory_limit", "The local factory exceeds the bounded site-preview occupied-object scan.");
                var origin = new Vector3(request.Site.Origin.X, request.Site.Origin.Y, request.Site.Origin.Z);
                if (Math.Abs(origin.magnitude - factory!.planet.realRadius) > 10f)
                    throw new FoundryPlanningException("invalid_site", "The chosen origin must be near the current planet surface.");
                var basis = Maths.SphericalRotation(origin, request.Site.YawDegrees);
                var site = FoundrySitePlanner.CreateCandidates(plan, request.Site, catalog.Value.Buildings,
                    CaptureVector(basis * Vector3.right), CaptureVector(basis * Vector3.forward));
                foreach (var machine in site.Machines)
                {
                    var raw = new Vector3(machine.Position.X, machine.Position.Y, machine.Position.Z);
                    machine.Position = CaptureVector(factory.planet.aux.Snap(raw, onTerrain: true));
                }
                FoundrySitePlanner.CheckPlannedClearance(site);
                foreach (var machine in site.Machines)
                {
                    if (machine.OverlappingPlacementIds.Count != 0)
                    {
                        machine.Rejection = "Conservative build-collider clearance overlaps another planned machine after grid snapping.";
                        continue;
                    }
                    var item = LDB.items.Select(machine.BuildingItemId)
                        ?? throw new FoundryPlanningException("catalog_invalid", "The exact machine prototype is no longer available.");
                    var position = new Vector3(machine.Position.X, machine.Position.Y, machine.Position.Z);
                    var evidence = NormalGameActionCoordinator.InspectFoundryMachine(factory, player, item, position, machine.YawDegrees);
                    machine.NativeCheckPassed = evidence.Passed;
                    machine.NativeCheckPerformed = evidence.NativeCheckPerformed;
                    machine.NativeBuildCondition = evidence.Condition;
                    machine.OccupiedObjectId = evidence.OccupiedObjectId;
                    machine.Rejection = evidence.Rejection;
                }
                FoundrySitePlanner.CompleteAssessment(site,
                    playerResult.Value.Inventory.ToDictionary(i => i.ItemId, i => i.Count),
                    catalog.Value.Revision, playerResult.Value.StateHash);
                plan.Site = site;
                plan.Phase = "site_preview";
                plan.RemainingChecks[0] = "Machine positions have a one-tick native assessment only; fresh-read and prepare every actual build again. AssessmentHash is not a state hash, write token or restartable plan.";
                plan.RemainingChecks.Add("This bounded machine grid does not route or validate logistics or power; machine_previews_clear does not mean an executable factory.");
            }
            return GameCallResult<FoundryPlanSnapshot>.Succeeded(plan);
        }
        catch (FoundryPlanningException error)
        {
            return GameCallResult<FoundryPlanSnapshot>.Failed(BridgeError.Create(
                BridgeErrorCodes.InvalidRequest, $"Foundry {error.Reason}: {error.Message}", false,
                "Read current catalogs and use an available recipe, bounded rate and, if requested, a finite local site with at most 32 machines."));
        }
    }

    private static float? CapturePlacementRadius(PrefabDesc description)
    {
        if (!description.hasBuildCollider || (description.buildColliders?.Length ?? 0) > 64) return null;
        float? Bound(ColliderData collider) => FoundrySitePlanner.ColliderBoundingRadius(
            collider.shape.ToString(), CaptureVector(collider.pos), CaptureVector(collider.ext), collider.radius);
        var radius = Bound(description.buildCollider);
        if (radius is null) return null;
        foreach (var collider in description.buildColliders ?? Array.Empty<ColliderData>())
        {
            var bound = Bound(collider);
            if (bound is null) return null;
            radius = Math.Max(radius.Value, bound.Value);
        }
        return radius;
    }
}
