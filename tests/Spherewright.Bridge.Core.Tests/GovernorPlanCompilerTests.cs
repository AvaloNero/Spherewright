using Spherewright.Bridge.Core.Diagnostics;
using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Diagnostics;
using Spherewright.Contracts.Factory;
using Spherewright.Contracts.Progression;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class GovernorPlanCompilerTests
{
    [Theory]
    [InlineData(0)] [InlineData(601)] [InlineData(36000)]
    public void GovernorRequestRejectsUnboundedOrUnsupportedMeasurementPeriod(int period)
    {
        var request = Request(); request.MeasurementGameTicks = period;
        Assert.Equal("governor_request_invalid", Assert.Throws<FoundryPlanningException>(
            () => GovernorPlanCompiler.ValidateRequest(request)).Reason);
    }

    [Fact]
    public void MinuteProposalDeclaresActualPeriodAndBindsItWithoutAlteringFoundryScale()
    {
        var minute = Compile(measurementGameTicks: 3600); var normal = Compile();
        Assert.Equal(3600, minute.MeasurementGameTicks); Assert.Equal(3600, minute.CurrentWindow.ElapsedGameTicks);
        Assert.Equal("ready", minute.Baseline.State); Assert.Equal(3, minute.Baseline.IndependentWindowCount);
        Assert.Equal(normal.FullTargetScale.PlanHash, minute.FullTargetScale.PlanHash);
        var hash = minute.ProposalHash; minute.MeasurementGameTicks = 600;
        Assert.NotEqual(hash, GovernorPlanCompiler.Fingerprint(minute));
    }

    [Fact]
    public void RepricingAnUnchangedSourceToTwiceItsBaselineDoesNotRestartMeasurement()
    {
        var draft = Compile(targetRate: 90);
        var doubled = Compile(targetRate: 60);
        Assert.NotEqual(draft.FullTargetScale.PlanHash, doubled.FullTargetScale.PlanHash);
        Assert.NotEqual(draft.ProposalHash, doubled.ProposalHash); // This remains a new proposal, not an old token.
        Assert.NotEqual(draft.FullTargetScale.MachineCount, doubled.FullTargetScale.MachineCount);
        var items = draft.FullTargetScale.Stages.SelectMany(s => s.Inputs).Select(i => i.ItemId)
            .Append(draft.TargetItemId).Distinct().OrderBy(i => i).ToArray();
        Assert.Equal(items, doubled.FullTargetScale.Stages.SelectMany(s => s.Inputs).Select(i => i.ItemId)
            .Append(doubled.TargetItemId).Distinct().OrderBy(i => i).ToArray());

        var series = new GovernorMeasurementSeries();
        var legacy = new GovernorMeasurementSeries();
        var binding = GovernorSourceBinding.CreateMeasurementBinding("series", draft.SourceStateHash, items);
        var oldDraftBinding = Spherewright.Bridge.Core.Safety.CanonicalStateHash.Combine(
            "source-binding", "series", draft.SourceStateHash, draft.FullTargetScale.PlanHash);
        for (var tick = 600; tick <= 2400; tick += 600)
        {
            Observe(series, tick, 30, binding);
            Observe(legacy, tick, 30, oldDraftBinding);
        }
        Observe(series, 2410, 30, GovernorSourceBinding.CreateMeasurementBinding("series", doubled.SourceStateHash, items));
        Observe(legacy, 2410, 30, Spherewright.Bridge.Core.Safety.CanonicalStateHash.Combine(
            "source-binding", "series", doubled.SourceStateHash, doubled.FullTargetScale.PlanHash));
        Assert.Equal("warming_up", legacy.Baseline(.1m).State); // Exact previous adapter counterexample.
        Assert.Equal(0, legacy.Baseline(.1m).IndependentWindowCount);
        Assert.Equal("ready", series.Baseline(.1m).State);
        Assert.Equal(3, series.Baseline(.1m).IndependentWindowCount);
        Assert.Equal(doubled.TargetRatePerMinute, 2 * series.Baseline(.1m).ProductionPerMinute);
    }

    [Fact]
    public void StableBaselineRequiresThreeIndependentPostBindingNonzeroWindows()
    {
        var series = new GovernorMeasurementSeries();
        Observe(series, 600, 30); Assert.Equal(0, series.Baseline(.1m).IndependentWindowCount);
        Observe(series, 1200, 30); Observe(series, 1250, 30);
        Assert.Equal(1, series.Baseline(.1m).IndependentWindowCount);
        Observe(series, 1800, 30); Observe(series, 2400, 30);
        Assert.Equal("ready", series.Baseline(.1m).State);
        Assert.Equal(30, series.Baseline(.1m).ProductionPerMinute);
    }

    [Theory]
    [InlineData("other")] [InlineData("tick_regressed")] [InlineData("large_gap")]
    public void SourceChangeRegressionOrSamplingGapCannotReuseBaseline(string change)
    {
        var series = Series();
        if (change == "other") Observe(series, 3000, 30, "new-source");
        else if (change == "tick_regressed") Observe(series, 500, 30);
        else Observe(series, 5000, 30);
        Assert.NotEqual("ready", series.Baseline(.1m).State);
    }

    [Theory]
    [InlineData(0, "zero_baseline")] [InlineData(60, "unstable")]
    public void ZeroOrChangingProductionIsNotAStableBaseline(int last, string state)
    {
        var series = new GovernorMeasurementSeries();
        Observe(series, 600, 30); Observe(series, 1200, 30); Observe(series, 1800, 30); Observe(series, 2400, last);
        Assert.Equal(state, series.Baseline(.1m).State);
    }

    [Fact]
    public void StarvedProductionEqualsConsumptionDoesNotMeanBalanceOrSpareSupply()
    {
        var plan = Compile();
        var raw = plan.Supply.Single(s => s.ItemId == 1);
        Assert.Equal(60, raw.TargetChainDemandPerMinute);
        Assert.Equal(0, raw.AllocatableSurplusPerMinute);
        Assert.Equal(30, raw.MinimumAdditionalAutomaticSupplyPerMinute);
        Assert.True(raw.DemandIsLowerBound);
        Assert.False(plan.Balanced); Assert.False(plan.Executable);
        Assert.All(plan.Alternatives, a => Assert.Null(a.ActualTargetGainPerMinute));
    }

    [Fact]
    public void SelectedInventoryDeltaIsNotProductionMinusConsumption()
    {
        var plan = Compile();
        var raw = plan.Supply.Single(s => s.ItemId == 1);
        Assert.Equal(0, raw.ProductionMinusConsumptionPerMinute);
        Assert.Equal(-3, raw.SelectedBufferItemDelta);
        Assert.Equal(7, raw.SelectedBufferItemCount);
        Assert.Equal(1800, raw.InventoryObservationStartGameTick);
        Assert.Equal(2400, raw.InventoryObservationEndGameTick);
    }

    [Theory]
    [InlineData("items", 1)]
    [InlineData("joules_per_tick", 0)]
    public void GenerationIsNotInventoryEvenWhenLegacyUnitsSayItems(string unit, int scale)
    {
        var plan = Compile(configureSource: source => source[0].Buffers.Add(new FactoryBufferSnapshot
        {
            Role = "power-generation-current-tick", ItemId = 1, Count = 18686,
            CountUnit = unit, UnitsPerItem = scale,
        }));
        var raw = plan.Supply.Single(s => s.ItemId == 1);
        Assert.Equal(7, raw.SelectedBufferItemCount);
        Assert.Equal(-3, raw.SelectedBufferItemDelta);
    }

    [Fact]
    public void UpgradeCostsCompleteNewDeviceWithSeparateRefundNotIncrementalIngredients()
    {
        var option = Compile().Alternatives.Single(a => a.Kind == "upgrade_in_place");
        Assert.Equal(2304, Assert.Single(option.Consume).ItemId);
        Assert.Equal(1, option.Consume[0].Count);
        Assert.Equal(2303, Assert.Single(option.Refund).ItemId);
        Assert.Equal(60, option.TheoreticalTargetCapacityGainPerMinute);
        Assert.Equal(6000, option.AdditionalMachineWorkPowerWatts);
        Assert.Null(option.ActualTargetGainPerMinute);
    }

    [Fact]
    public void AddAlternativeReusesFoundryDeltaScaleAndLabelsExcludedInfrastructure()
    {
        var option = Compile().Alternatives.Single(a => a.Kind == "add_parallel_production");
        Assert.Equal(1, Assert.Single(option.Consume).Count);
        Assert.Equal(2303, option.Consume[0].ItemId);
        Assert.Equal(60, option.TheoreticalTargetCapacityGainPerMinute);
        Assert.Contains("exclude", string.Join(" ", option.Conditions));
        Assert.Equal("unsupported_selection", Compile().Alternatives.Single(a => a.Kind == "copy_selected_module").Status);
    }

    [Fact]
    public void IncompletePlanetProducerSelectionCannotClaimLineAttribution()
    {
        var plan = Compile(producerCount: 2);
        Assert.False(plan.SelectionContainsAllTargetProducers);
        Assert.Contains("selected_line_cannot_be_attributed_from_planet_counters", plan.Blockers);
    }

    [Fact]
    public void SourceAndTargetToleranceChangesRequireDifferentProposal()
    {
        Assert.NotEqual(Compile().ProposalHash, Compile(sourceHash: "changed").ProposalHash);
        Assert.NotEqual(Compile().ProposalHash, Compile(tolerance: .2m).ProposalHash);
    }

    [Theory]
    [InlineData(0, 36000)] [InlineData(1, 35999)] [InlineData(1, 216001)]
    public void BadRateOrShortValidationWindowIsRejected(int rate, int ticks)
    {
        var request = Request(); request.TargetRatePerMinute = rate; request.ValidationGameTicks = ticks;
        Assert.Throws<FoundryPlanningException>(() => GovernorPlanCompiler.ValidateRequest(request));
    }

    [Theory]
    [InlineData("power")] [InlineData("cost")] [InlineData("live_blocker")] [InlineData("upgrade")]
    public void ProposalHashBindsActualPowerCostsCapabilitiesAndReadbackBlockers(string change)
    {
        var plan = Compile(); var before = plan.ProposalHash;
        if (change == "power") plan.Power.TotalEnergyCapacity++;
        if (change == "cost") plan.Alternatives[0].Consume[0].Count++;
        if (change == "live_blocker") plan.Blockers.Add("reciprocal_missing");
        if (change == "upgrade") plan.Alternatives[2].Upgrades[0].TargetItemId++;
        Assert.NotEqual(before, GovernorPlanCompiler.Fingerprint(plan));
    }

    [Theory]
    [InlineData("unattributed", false)] [InlineData("selected", true)] [InlineData("target", true)] [InlineData("path", true)]
    public void GlobalFindingsNeedActualSelectionOrTargetPathEvidence(string scope, bool related)
    {
        var result = Compile(configure: measured =>
        {
            var finding = new OverseerFindingSnapshot { PlanetId = 104, ObjectId = scope == "selected" ? 20 : 14, Kind = "vein_exhausted" };
            if (scope == "path") finding.UpstreamPath.Add(new OverseerPathNodeSnapshot { PlanetId = 104, ObjectId = 20 });
            if (scope == "target") measured.Production[1].Findings.Add(finding);
            else measured.InfrastructureFindings.Add(finding);
        });
        Assert.Single(result.Findings);
        Assert.Equal(related ? 1 : 0, result.TargetChainFindings.Count);
        Assert.Equal(related ? 0 : 1, result.UnattributedPlanetFindingCount);
        Assert.False(result.Balanced);
    }

    [Fact]
    public void FindingScopeChangesInvalidateTheProposalHash()
    {
        var result = Compile(); var before = result.ProposalHash;
        result.TargetChainFindings.Add(new OverseerFindingSnapshot { PlanetId = 104, ObjectId = 20, Kind = "material_shortage" });
        Assert.NotEqual(before, GovernorPlanCompiler.Fingerprint(result));
    }

    private static GovernorPlanSnapshot Compile(int producerCount = 1, string sourceHash = "source", decimal tolerance = .1m,
        Action<OverseerDiagnosticBundlePlanetSnapshot>? configure = null,
        Action<FactoryEntitySnapshot[]>? configureSource = null, decimal targetRate = 60, int measurementGameTicks = 600)
    {
        var request = Request(); request.ToleranceFraction = tolerance; request.TargetRatePerMinute = targetRate;
        var recipes = new RecipeCatalogSnapshot { SessionId = "session", PlanetId = 104, CapturedAtGameTick = 2400,
            Items = new List<ItemCatalogEntry> { new() { ItemId = 1, Unlocked = true, IsRaw = true }, new() { ItemId = 2, Unlocked = true } },
            Recipes = new List<RecipeCatalogEntry> { new() { RecipeId = 1, Unlocked = true, RecipeType = "Assemble", TimeSpend = 60,
                Inputs = new List<CatalogItemAmount> { new() { ItemId = 1, Count = 1 } }, Outputs = new List<CatalogItemAmount> { new() { ItemId = 2, Count = 1 } } } } };
        var buildings = new List<BuildCatalogItem> { new() { ItemId = 2303, Role = "assembler", RecipeType = "Assemble", Grade = 1,
            Unlocked = true, Available = true, ProductionSpeedRaw = 10000, WorkEnergyPerTick = 100,
            SupportedUpgradeTargetItemIds = new List<int> { 2304 } }, new() { ItemId = 2304, Role = "assembler", RecipeType = "Assemble", Grade = 2,
            Unlocked = true, Available = true, ProductionSpeedRaw = 20000, WorkEnergyPerTick = 200 } };
        var source = new[] { new FactoryEntitySnapshot { SessionId = "session", PlanetId = 104, CapturedAtGameTick = 2400,
            ObjectId = 20, ItemId = 2303, RecipeId = 1, Buffers = new List<FactoryBufferSnapshot> { new() { ItemId = 1, Count = 7 } } } };
        configureSource?.Invoke(source);
        var measured = new OverseerDiagnosticBundlePlanetSnapshot { PlanetId = 104, CapturedAtGameTick = 2400,
            Production = new List<ProductionRateSnapshot> { new() { ItemId = 1, ActualProductionPerMinute = 30, ActualConsumptionPerMinute = 30 },
                new() { ItemId = 2, ActualProductionPerMinute = 30, ActualConsumptionPerMinute = 0, DirectProducerCount = producerCount } } };
        configure?.Invoke(measured);
        request.MeasurementGameTicks = measurementGameTicks;
        recipes.CapturedAtGameTick = measured.CapturedAtGameTick = source[0].CapturedAtGameTick = 4 * measurementGameTicks;
        return GovernorPlanCompiler.Compile(request, recipes, buildings, source, measured,
            NativeProductionRateCalculator.Calculate(4 * measurementGameTicks, 0, 0, measurementGameTicks).Window,
            Series(measurementGameTicks), sourceHash, 1, null);
    }
    private static GetGovernorPlanRequest Request() => new() { PlanetId = 104, TargetItemId = 2, TargetRatePerMinute = 60,
        SourceEntities = new List<BlueprintSelectedEntity> { new() { ObjectId = 20, ExpectedRecipeId = 1, ExpectedEndpointStateHash = "endpoint" } } };
    private static OverseerWindowSnapshot Window(long tick) => NativeProductionRateCalculator.Calculate(tick, 0, 0).Window;
    private static void Observe(GovernorMeasurementSeries series, long tick, decimal rate, string binding = "bound") =>
        series.Observe(binding, Window(tick), rate, new Dictionary<int, long> { [1] = tick < 2400 ? 10 : 7 });
    private static GovernorMeasurementSeries Series(int measurementGameTicks = 600)
    {
        var series = new GovernorMeasurementSeries();
        for (var tick = measurementGameTicks; tick <= 4 * measurementGameTicks; tick += measurementGameTicks)
            series.Observe("bound", NativeProductionRateCalculator.Calculate(tick, 0, 0, measurementGameTicks).Window,
                30, new Dictionary<int, long> { [1] = tick < 4 * measurementGameTicks ? 10 : 7 }, measurementGameTicks);
        return series;
    }
}
