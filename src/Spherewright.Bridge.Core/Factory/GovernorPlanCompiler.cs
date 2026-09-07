using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Diagnostics;
using Spherewright.Contracts.Factory;
using Spherewright.Contracts.Progression;

namespace Spherewright.Bridge.Core.Factory;

public static partial class GovernorPlanCompiler
{
    public static void ValidateRequest(GetGovernorPlanRequest request)
    {
        if (request is null || request.PlanetId <= 0 || request.TargetItemId <= 0
            || request.TargetRatePerMinute <= 0 || request.TargetRatePerMinute > 1000000
            || request.ToleranceFraction <= 0 || request.ToleranceFraction > .5m
            || request.ValidationGameTicks < 36000 || request.ValidationGameTicks > 216000
            || request.SourceEntities is null || request.SourceEntities.Count < 1 || request.SourceEntities.Count > 32
            || request.SourceEntities.Any(x => x is null || x.ObjectId <= 0 || string.IsNullOrWhiteSpace(x.ExpectedEndpointStateHash))
            || request.SourceEntities.Select(x => x.ObjectId).Distinct().Count() != request.SourceEntities.Count)
            throw new FoundryPlanningException("governor_request_invalid", "Select1..32 fresh entities, a bounded positive rate/tolerance and10..60 game-minute validation window.");
        if (request.ValidationBaselineProposalHash is not null
            && (string.IsNullOrWhiteSpace(request.ValidationBaselineProposalHash) || request.ValidationBaselineProposalHash.Length > 128))
            throw new FoundryPlanningException("governor_validation_hash_invalid", "Use one exact bounded proposal hash, never supplied baseline history.");
        if (request.ParallelExpansionBlueprint is { } blueprint
            && (blueprint.Site is null || string.IsNullOrWhiteSpace(blueprint.BlueprintCode)
                || blueprint.BlueprintCode.Length > BoundedBlueprintReader.MaximumCodeBytes
                || System.Text.Encoding.UTF8.GetByteCount(blueprint.BlueprintCode) > BoundedBlueprintReader.MaximumCodeBytes
                || blueprint.BoundaryPorts is null || blueprint.BoundaryPorts.Count > 33
                || blueprint.BoundaryPorts.Any(p => p is null)))
            throw new FoundryPlanningException("governor_parallel_layout_invalid", "Use one explicit bounded blueprint/site/port layout; file paths and claimed historical rates are not accepted.");
        if (request.ParallelExpansionBlueprint is { } layout)
        {
            try { BlueprintSitePolicy.ValidateRequest(layout.Site); }
            catch (BlueprintReadException)
            { throw new FoundryPlanningException("governor_parallel_layout_invalid", "Use the existing bounded native site contract with a fresh player hash."); }
        }
    }

    public static GovernorPlanSnapshot Compile(GetGovernorPlanRequest request, RecipeCatalogSnapshot recipes,
        IReadOnlyList<BuildCatalogItem> buildings, IReadOnlyList<FactoryEntitySnapshot> source,
        OverseerDiagnosticBundlePlanetSnapshot measured, OverseerWindowSnapshot window,
        GovernorMeasurementSeries series, string sourceHash, long revision, BlueprintInspection? copy)
    {
        ValidateRequest(request);
        if (source.Count != request.SourceEntities.Count || measured.PlanetId != request.PlanetId
            || recipes.PlanetId != request.PlanetId || recipes.CapturedAtGameTick != measured.CapturedAtGameTick
            || source.Any(e => e.SessionId != recipes.SessionId || e.PlanetId != recipes.PlanetId
                || e.CapturedAtGameTick != recipes.CapturedAtGameTick)
            || string.IsNullOrWhiteSpace(sourceHash))
            throw new FoundryPlanningException("governor_scope_mismatch", "All observations must share one owned local capture.");
        var scaleRequest = new GetFoundryPlanRequest { PlanetId = request.PlanetId, TargetItemId = request.TargetItemId,
            TargetRatePerMinute = request.TargetRatePerMinute, ExternalSupplyItemIds = request.ExternalSupplyItemIds, RecipeChoices = request.RecipeChoices };
        var scale = FoundryPlanCompiler.Compile(scaleRequest, recipes, buildings);
        var target = measured.Production.Single(p => p.ItemId == request.TargetItemId);
        var baseline = series.Baseline(request.ToleranceFraction);
        var current = CheckedRate(target.ActualProductionPerMinute);
        var referenceRate = baseline.State == "ready" ? baseline.ProductionPerMinute!.Value : current;
        var additionFraction = Math.Max(0, request.TargetRatePerMinute - referenceRate) / request.TargetRatePerMinute;
        var targetRecipeIds = new HashSet<int>(recipes.Recipes.Where(r => r.Outputs.Any(o => o.ItemId == request.TargetItemId)).Select(r => r.RecipeId));
        var selectedProducers = source.Count(e => targetRecipeIds.Contains(e.RecipeId));
        var selectedIds = new HashSet<int>(source.Select(e => e.ObjectId));
        var allFindings = measured.Production.SelectMany(p => p.Findings).Concat(measured.InfrastructureFindings).Distinct().ToArray();
        var relatedFindings = target.Findings.Concat(allFindings.Where(f =>
            (f.PlanetId == request.PlanetId && selectedIds.Contains(f.ObjectId))
            || f.UpstreamPath.Any(p => p.PlanetId == request.PlanetId && p.ObjectId.HasValue && selectedIds.Contains(p.ObjectId.Value))))
            .Distinct().ToArray();
        var result = new GovernorPlanSnapshot { SessionId = recipes.SessionId, PlanetId = request.PlanetId,
            CapturedAtGameTick = recipes.CapturedAtGameTick, Revision = revision, SourceStateHash = sourceHash,
            TargetItemId = request.TargetItemId, TargetRatePerMinute = request.TargetRatePerMinute,
            ToleranceFraction = request.ToleranceFraction, ValidationGameTicks = request.ValidationGameTicks,
            Baseline = baseline, CurrentWindow = window, FullTargetScale = scale,
            Power = measured.Power, Logistics = measured.Logistics,
            FindingsTruncated = measured.InfrastructureFindingsTruncated || measured.Production.Any(p => p.FindingsTruncated)
                || measured.Production.Sum(p => p.Findings.Count) + measured.InfrastructureFindings.Count > 64,
            SelectionContainsAllTargetProducers = selectedProducers > 0 && selectedProducers == target.DirectProducerCount,
            Findings = allFindings.Take(64).ToList(), TargetChainFindings = relatedFindings.Take(64).ToList(),
            UnattributedPlanetFindingCount = allFindings.Except(relatedFindings).Count(),
            RemainingChecks = new List<string> {
                "A proposal is not a write token or measured success. Choose one finite plan, then fresh native prepare/commit/terminal/readback.",
                "Rates are native LOCAL PLANET item counters, not selected-machine counters; selection attribution requires every direct target producer.",
                "Actual consumption is a lower bound on demand under starvation. Imports/reservations/transport are not included in allocatable local surplus.",
                "Buffer deltas cover selected objects only, include transport/manual changes, and are not inferred from production minus consumption.",
                "Preserve the pre-execution nonzero baseline, declared target/tolerance/window. Prove at least10 game minutes afterward; no post-hoc tolerance changes.",
                "Recheck every upstream, selected power network and external connection after upgrades/copy. Machine count/nameplate speed is not throughput.",
                "A depleted-source finding requires fresh finite vein/coverage evidence before replacement; a backed-up miner is not proof of exhaustion.",
                "TargetChainFindings have target-diagnostic/selected-object path evidence. Other planet findings remain visible but do not alone prove this selected line is unhealthy. Supply-rate deficits remain separate and must be resolved for sustainable expansion.",
            } };
        if (baseline.State != "ready") result.Blockers.Add("stable_nonzero_three_independent_windows_required");
        if (!result.SelectionContainsAllTargetProducers) result.Blockers.Add("selected_line_cannot_be_attributed_from_planet_counters");
        if (window.State != "ready" || window.CrossedSessionBoundary) result.Blockers.Add("production_window_not_ready");
        if (!measured.Power.MinimumConsumerRatio.HasValue) result.Blockers.Add("power_service_unproven");
        else if (measured.Power.MinimumConsumerRatio < .999) result.Blockers.Add("power_not_fully_served");
        if (result.FindingsTruncated) result.Blockers.Add("diagnostic_findings_truncated");
        if (referenceRate >= request.TargetRatePerMinute) result.Blockers.Add("target_not_above_observed_reference");
        var demand = scale.Stages.SelectMany(s => s.Inputs).GroupBy(f => f.ItemId)
            .ToDictionary(g => g.Key, g => g.Sum(f => f.RatePerMinute));
        demand[request.TargetItemId] = request.TargetRatePerMinute;
        foreach (var flow in demand.OrderBy(d => d.Key))
        {
            var rate = measured.Production.SingleOrDefault(p => p.ItemId == flow.Key)
                ?? throw new FoundryPlanningException("governor_measurement_missing", "Every scale input must have same-window production evidence.");
            var produced = CheckedRate(rate.ActualProductionPerMinute); var consumed = CheckedRate(rate.ActualConsumptionPerMinute);
            var stock = source.SelectMany(e => e.Buffers).Where(b => b.ItemId == flow.Key && FactoryBufferSemantics.IsItemCount(b)).Sum(b => (long)b.Count);
            var additional = flow.Value * additionFraction;
            result.Supply.Add(new GovernorSupplyBalance { ItemId = flow.Key, TargetChainDemandPerMinute = flow.Value,
                ActualProductionPerMinute = produced, ActualConsumptionPerMinute = consumed,
                AllocatableSurplusPerMinute = Math.Max(0, produced - consumed), AdditionalChainDemandPerMinute = additional,
                MinimumAdditionalAutomaticSupplyPerMinute = flow.Key == request.TargetItemId ? Math.Max(0, request.TargetRatePerMinute - produced)
                    : Math.Max(0, consumed + additional - produced), ProductionMinusConsumptionPerMinute = produced - consumed,
                SelectedBufferItemCount = stock, SelectedBufferItemDelta = series.PreviousStocks is null ? (long?)null
                    : stock - (series.PreviousStocks.TryGetValue(flow.Key, out var old) ? old : 0),
                InventoryObservationStartGameTick = series.PreviousStockTick, InventoryObservationEndGameTick = recipes.CapturedAtGameTick });
        }
        var add = new GovernorAlternative { Kind = "add_parallel_production", Status = "requires_full_site_logistics_power_budget",
            Conditions = new List<string> { "Uses the shared Foundry compiler for an additional parallel chain; costs exclude belts/sorters/power infrastructure and do not credit unproved spare capacity." } };
        if (additionFraction > 0)
        {
            scaleRequest.TargetRatePerMinute = request.TargetRatePerMinute - referenceRate;
            var delta = FoundryPlanCompiler.Compile(scaleRequest, recipes, buildings);
            add.Consume = delta.MachineCost; add.AdditionalMachineWorkPowerWatts = delta.MachineWorkPowerWatts;
            add.TheoreticalTargetCapacityGainPerMinute = delta.Stages.Single(s => s.ItemId == request.TargetItemId).InstalledRatePerMinute;
        }
        else add.Status = "no_additional_capacity_requested";
        result.Alternatives.Add(add);
        result.Alternatives.Add(CopyAlternative(copy, source, buildings, recipes, request.TargetItemId));
        result.Alternatives.Add(UpgradeAlternative(source, buildings, recipes, request.TargetItemId));
        result.ProposalHash = Fingerprint(result);
        return result;
    }

    public static string Fingerprint(GovernorPlanSnapshot result)
    {
        var core = FingerprintCore(result);
        return result.ParallelExpansion is null ? core : CanonicalStateHash.Combine("governor-parallel-proposal-v1", core,
            result.ParallelExpansion.RateBasis, FoundryConstructionCompiler.IntentHash(result.ParallelExpansion.Intent),
            result.ParallelExpansion.Plan.PlanHash,
            FoundryConstructionCompiler.Fingerprint(result.ParallelExpansion.Plan.Construction!),
            result.ParallelExpansion.Plan.BlueprintSite!.AssessmentHash,
            result.ParallelExpansion.Plan.BlueprintSite.Power?.AssessmentHash);
    }

    private static string FingerprintCore(GovernorPlanSnapshot result) => CanonicalStateHash.Combine("governor-proposal-v2",
        result.SessionId, result.PlanetId, result.Revision, result.CapturedAtGameTick, result.SourceStateHash,
        result.FullTargetScale.PlanHash, result.TargetRatePerMinute, result.ToleranceFraction, result.ValidationGameTicks,
        result.Baseline.State, result.Baseline.StartGameTick, result.Baseline.EndGameTick, result.Baseline.ProductionPerMinute,
        result.Power.TotalEnergyRequired, result.Power.TotalEnergyServed, result.Power.TotalEnergyCapacity, result.Power.MinimumConsumerRatio,
        result.FindingsTruncated, CanonicalStateHash.Combine("blockers", result.Blockers.Cast<object>().ToArray()),
        result.UnattributedPlanetFindingCount,
        CanonicalStateHash.Combine("target-chain-findings", result.TargetChainFindings.Select(f => (object)CanonicalStateHash.Combine(
            "finding", f.Kind, f.Confidence, f.Severity, f.PlanetId, f.ObjectId, f.ItemId, f.Summary,
            CanonicalStateHash.Combine("path", f.UpstreamPath.Select(p => (object)CanonicalStateHash.Combine("node", p.PlanetId, p.ObjectId, p.Kind)).ToArray()))).ToArray()),
        CanonicalStateHash.Combine("measurements", result.Supply.Select(s => (object)CanonicalStateHash.Combine(
            "item", s.ItemId, s.ActualProductionPerMinute, s.ActualConsumptionPerMinute, s.SelectedBufferItemCount,
            s.SelectedBufferItemDelta, s.InventoryObservationStartGameTick)).ToArray()),
        CanonicalStateHash.Combine("alternatives", result.Alternatives.Select(a => (object)CanonicalStateHash.Combine(
            "option", a.Kind, a.Status, a.CostScope, a.TheoreticalTargetCapacityGainPerMinute, a.AdditionalMachineWorkPowerWatts,
            CanonicalStateHash.Combine("consume", a.Consume.Select(c => (object)CanonicalStateHash.Combine("item", c.ItemId, c.Count)).ToArray()),
            CanonicalStateHash.Combine("refund", a.Refund.Select(c => (object)CanonicalStateHash.Combine("item", c.ItemId, c.Count)).ToArray()),
            CanonicalStateHash.Combine("upgrade", a.Upgrades.Select(c => (object)CanonicalStateHash.Combine("entity", c.ObjectId, c.CurrentItemId, c.TargetItemId, c.RecipeId)).ToArray()),
            CanonicalStateHash.Combine("conditions", a.Conditions.Cast<object>().ToArray()))).ToArray()));

    private static GovernorAlternative CopyAlternative(BlueprintInspection? copy, IReadOnlyList<FactoryEntitySnapshot> source,
        IReadOnlyList<BuildCatalogItem> buildings, RecipeCatalogSnapshot recipes, int target)
    {
        var option = new GovernorAlternative { Kind = "copy_selected_module", CostScope = "selected_module_objects", Status = copy is null ? "unsupported_selection" : "requires_native_site_and_remaining_supply",
            Conditions = new List<string> { "No cargo, external supply or boundary connections are copied. Actual throughput gain remains unknown; closed internal sorter ends and material/technology/site checks are mandatory." } };
        if (copy is null) return option;
        try { BlueprintSitePolicy.BuildConnections(copy, buildings.ToDictionary(b => b.ItemId, b => b.SlotCount)); }
        catch (BlueprintReadException exception)
        {
            option.Status = "unsupported_selection_graph";
            option.Conditions.Add(exception.Reason);
            return option;
        }
        option.Consume = copy.ConstructionItems.Select(i => new FoundryMaterialCost { ItemId = i.ItemId, Count = i.RequiredCount }).ToList();
        option.TheoreticalTargetCapacityGainPerMinute = source.Sum(e => Capacity(e, buildings.SingleOrDefault(b => b.ItemId == e.ItemId), recipes, target));
        var machineItems = source.Where(e => e.RecipeId > 0).Select(e => buildings.SingleOrDefault(b => b.ItemId == e.ItemId)).ToArray();
        option.AdditionalMachineWorkPowerWatts = machineItems.All(b => b?.WorkEnergyPerTick is not null)
            ? machineItems.Sum(b => b!.WorkEnergyPerTick!.Value * 60) : (long?)null;
        return option;
    }

    private static GovernorAlternative UpgradeAlternative(IReadOnlyList<FactoryEntitySnapshot> source,
        IReadOnlyList<BuildCatalogItem> buildings, RecipeCatalogSnapshot recipes, int target)
    {
        var option = new GovernorAlternative { Kind = "upgrade_in_place", CostScope = "whole_upgrade_devices", TheoreticalTargetCapacityGainPerMinute = 0,
            AdditionalMachineWorkPowerWatts = 0, Conditions = new List<string> {
                "Only currently advertised native upgrade families are candidates. Each requires fresh exact endpoint/configuration proof, one complete target device and refund space.",
                "Sorter speed has no direct item-production capacity credit. Repair missing reciprocal links separately, never via upgrade." } };
        foreach (var entity in source)
        {
            var old = buildings.SingleOrDefault(b => b.ItemId == entity.ItemId);
            var next = old?.SupportedUpgradeTargetItemIds.Select(id => buildings.SingleOrDefault(b => b.ItemId == id))
                .Where(b => b is not null && b.Unlocked).OrderBy(b => b!.Grade).ThenBy(b => b!.ItemId).FirstOrDefault();
            if (next is null) continue;
            if (entity.ComponentKind == "inserter" && !BuildingUpgradePolicy.HasCompleteInserterConnections(entity))
            { option.Conditions.Add("Missing required sorter links:" + entity.ObjectId); continue; }
            option.Upgrades.Add(new GovernorUpgradeCandidate { ObjectId = entity.ObjectId, CurrentItemId = entity.ItemId, TargetItemId = next.ItemId, RecipeId = entity.RecipeId });
            option.TheoreticalTargetCapacityGainPerMinute += Math.Max(0, Capacity(entity, next, recipes, target) - Capacity(entity, old, recipes, target));
            if (next.WorkEnergyPerTick.HasValue && old!.WorkEnergyPerTick.HasValue)
                option.AdditionalMachineWorkPowerWatts += Math.Max(0, next.WorkEnergyPerTick.Value - old.WorkEnergyPerTick.Value) * 60;
            else option.AdditionalMachineWorkPowerWatts = null;
        }
        option.Consume = option.Upgrades.GroupBy(u => u.TargetItemId).Select(g => new FoundryMaterialCost { ItemId = g.Key, Count = g.Count() }).ToList();
        option.Refund = option.Upgrades.GroupBy(u => u.CurrentItemId).Select(g => new FoundryMaterialCost { ItemId = g.Key, Count = g.Count() }).ToList();
        if (option.Upgrades.Count == 0) option.Status = "no_supported_unlocked_upgrade";
        return option;
    }

    private static decimal Capacity(FactoryEntitySnapshot entity, BuildCatalogItem? machine, RecipeCatalogSnapshot recipes, int target)
    {
        var recipe = recipes.Recipes.SingleOrDefault(r => r.RecipeId == entity.RecipeId);
        var output = recipe?.Outputs.SingleOrDefault(o => o.ItemId == target);
        return machine?.ProductionSpeedRaw > 0 && recipe?.TimeSpend > 0 && output is not null
            ? 3600m * machine.ProductionSpeedRaw.Value * output.Count / (recipe.TimeSpend * 10000m) : 0;
    }
    private static decimal CheckedRate(double value) => double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > 1000000000
        ? throw new FoundryPlanningException("governor_rate_invalid", "Runtime rate is not finite and bounded.") : (decimal)value;
}
