using Spherewright.Contracts.Factory;
using Spherewright.Contracts.Progression;
using Spherewright.Bridge.Core.Safety;

namespace Spherewright.Bridge.Core.Factory;

public static partial class GovernorPlanCompiler
{
    // Reuses Foundry's existing material/site/finite-execution contract. No new
    // placement algorithm, approval store or autonomous expansion is introduced.
    public static GetFoundryPlanRequest? CreateParallelConstructionRequest(GetGovernorPlanRequest request, GovernorPlanSnapshot proposal)
    {
        ValidateRequest(request);
        if (request.ParallelExpansionBlueprint is null) return null;
        if (proposal is null || proposal.TargetItemId != request.TargetItemId || proposal.PlanetId != request.PlanetId
            || proposal.TargetRatePerMinute != request.TargetRatePerMinute)
            throw ParallelMismatch();
        if (proposal.Baseline.State != "ready") return null;
        if (proposal.Baseline.IndependentWindowCount != 3 || proposal.Baseline.ProductionPerMinute is not decimal baseline || baseline <= 0)
            throw ParallelMismatch();
        if (baseline >= request.TargetRatePerMinute) return null;
        return new GetFoundryPlanRequest
        {
            PlanetId = request.PlanetId, TargetItemId = request.TargetItemId,
            TargetRatePerMinute = request.TargetRatePerMinute - baseline,
            ExternalSupplyItemIds = request.ExternalSupplyItemIds.ToList(),
            RecipeChoices = request.RecipeChoices.Select(c => new FoundryRecipeChoice
                { ItemId = c.ItemId, RecipeId = c.RecipeId, BuildingItemId = c.BuildingItemId }).ToList(),
            Blueprint = request.ParallelExpansionBlueprint,
        };
    }

    public static void AttachParallelConstruction(GetGovernorPlanRequest request, GovernorPlanSnapshot proposal,
        FoundryPlanSnapshot parallel, RecipeCatalogSnapshot recipes, IReadOnlyList<BuildCatalogItem> buildings)
    {
        var expectedRequest = CreateParallelConstructionRequest(request, proposal) ?? throw ParallelMismatch();
        var material = FoundryPlanCompiler.Compile(expectedRequest, recipes, buildings);
        var layout = expectedRequest.Blueprint!;
        var site = parallel?.BlueprintSite;
        if (parallel is null || site is null || parallel.Construction is null || proposal.ParallelExpansion is not null
            || parallel.Executable || site.Executable || parallel.Phase != "blueprint_construction_plan"
            || parallel.SessionId != proposal.SessionId || parallel.PlanetId != proposal.PlanetId
            || parallel.CapturedAtGameTick != proposal.CapturedAtGameTick
            || site.SessionId != proposal.SessionId || site.PlanetId != proposal.PlanetId
            || site.CapturedAtGameTick != proposal.CapturedAtGameTick || site.Revision != proposal.Revision
            || recipes.SessionId != proposal.SessionId || recipes.PlanetId != proposal.PlanetId
            || recipes.CapturedAtGameTick != proposal.CapturedAtGameTick
            || parallel.TargetItemId != expectedRequest.TargetItemId || parallel.TargetRatePerMinute != expectedRequest.TargetRatePerMinute
            || parallel.PlanHash != material.PlanHash || string.IsNullOrWhiteSpace(site.AssessmentHash)
            || site.BlueprintHash != CanonicalStateHash.Combine("blueprint-code-v1", layout.BlueprintCode)
            || site.Position is null || site.Position.X != layout.Site.Position.X
            || site.Position.Y != layout.Site.Position.Y || site.Position.Z != layout.Site.Position.Z
            || site.QuarterTurns != layout.Site.QuarterTurns
            || site.AssessmentHash != BlueprintSitePolicy.AssessmentHash(site, layout.Site.ExpectedPlayerStateHash)
            || site.Power is not null && site.Power.CapturedAtGameTick != proposal.CapturedAtGameTick)
            throw ParallelMismatch();
        var construction = FoundryConstructionCompiler.Compile(material, site, expectedRequest.Blueprint!.BoundaryPorts, buildings);
        if (construction.PlanHash != parallel.Construction.PlanHash
            || FoundryConstructionCompiler.Fingerprint(parallel.Construction) != construction.PlanHash)
            throw ParallelMismatch();
        var intent = new FoundryConstructionIntent
        {
            TargetItemId = expectedRequest.TargetItemId, TargetRatePerMinute = expectedRequest.TargetRatePerMinute,
            RecipeChoices = expectedRequest.RecipeChoices, ExternalSupplyItemIds = expectedRequest.ExternalSupplyItemIds,
            BoundaryPorts = expectedRequest.Blueprint.BoundaryPorts.Select(p => new FoundryBoundaryPort
                { ItemId = p.ItemId, Direction = p.Direction, ObjectIndex = p.ObjectIndex, Slot = p.Slot }).ToList(),
        };
        proposal.ParallelExpansion = new GovernorParallelExpansion { Intent = intent, Plan = parallel };
        var option = proposal.Alternatives.Single(a => a.Kind == "add_parallel_production");
        option.CostScope = "all_explicit_module_objects_external_infrastructure_excluded";
        option.Consume = construction.ConstructionCost.Select(c => new FoundryMaterialCost { ItemId = c.ItemId, Count = c.Count }).ToList();
        option.AdditionalMachineWorkPowerWatts = material.MachineWorkPowerWatts;
        option.TheoreticalTargetCapacityGainPerMinute = material.Stages.Single(s => s.ItemId == request.TargetItemId).InstalledRatePerMinute;
        option.Status = construction.CanPrepare ? "bounded_construction_ready_at_capture" : "bounded_construction_blocked";
        option.Conditions = new List<string>
        {
            "Complete cost of every explicit module object, including its logistics and power; unplanned external source/sink connections or extra generation remain excluded, never free.",
            "ParallelExpansion uses target minus the measured nonzero baseline, not target minus an instantaneous starved rate. It is not a replacement of the existing line.",
            "Inspect the full native site, power components, transport budget and every blocker. Rated flow does not guarantee fair splitting or automatic external supply.",
            "Read-only, never a write token: use the identical blueprint/site plus returned Intent and construction.PlanHash with the EXISTING fresh prepare_blueprint_build flow.",
            "Lock the Governor declaration before expansion; retain the original finite buildId across partial construction, cancellation and fresh restart reconciliation. A restart still discards passive throughput declarations.",
        };
        option.Conditions.AddRange(construction.Blockers.Select(b => "construction_blocker:" + b));
        if (!construction.CanPrepare) proposal.Blockers.Add("parallel_construction_not_ready");
        proposal.ProposalHash = Fingerprint(proposal);
    }

    private static FoundryPlanningException ParallelMismatch() => new FoundryPlanningException("governor_parallel_scope_mismatch",
        "A parallel construction comparison requires the exact same-capture owned scope, stable nonzero baseline, requested delta scale and unchanged Foundry graph.");
}
