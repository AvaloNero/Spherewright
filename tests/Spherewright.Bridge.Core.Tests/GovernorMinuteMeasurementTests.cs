using Spherewright.Bridge.Core.Diagnostics;
using Spherewright.Bridge.Core.Factory;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Diagnostics;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class GovernorMinuteMeasurementTests
{
    [Theory]
    [InlineData(3600)] [InlineData(3601)] [InlineData(3602)]
    [InlineData(3603)] [InlineData(3604)] [InlineData(3605)]
    public void MinuteWindowEndsAtTheLastCompletedSixTickBucket(long capture)
    {
        var rate = NativeProductionRateCalculator.Calculate(capture, 38, 40, 3600);
        Assert.Equal(1, rate.Window.StartGameTick); Assert.Equal(3600, rate.Window.EndGameTick);
        Assert.Equal(3600, rate.Window.ElapsedGameTicks); Assert.Equal(60, rate.Window.ElapsedGameSeconds);
        Assert.Equal("ready", rate.Window.State); Assert.Equal(38, rate.ActualProductionPerMinute);
        Assert.Equal(40, rate.ActualConsumptionPerMinute);
    }

    [Theory]
    [InlineData(0)] [InlineData(5)] [InlineData(3599)]
    public void PartialMinuteHistoryCannotBecomeReady(long tick) => Assert.Equal("warming_up",
        NativeProductionRateCalculator.Calculate(tick, 0, 0, 3600).Window.State);

    [Theory]
    [InlineData(0)] [InlineData(599)] [InlineData(601)] [InlineData(3599)] [InlineData(36000)]
    public void OnlyTwoNativePeriodsAreSupported(int duration) => Assert.Throws<ArgumentOutOfRangeException>(
        () => NativeProductionRateCalculator.Calculate(14400, 1, 1, duration));

    [Fact]
    public void DefaultTenSecondWindowRemainsExactAndIsNotSilentlyLengthened()
    {
        var value = NativeProductionRateCalculator.Calculate(3605, 6, 7);
        Assert.Equal(3006, value.Window.StartGameTick); Assert.Equal(3605, value.Window.EndGameTick);
        Assert.Equal(600, value.Window.ElapsedGameTicks); Assert.Equal(36, value.ActualProductionPerMinute);
        Assert.Equal(42, value.ActualConsumptionPerMinute);
    }

    [Fact]
    public void NativeMinuteCountersExcludeLifetimeAndDoNotRetainOrModifyArrays()
    {
        var (count, cursor, total) = Rings();
        total[6] = long.MaxValue; total[13] = long.MaxValue;
        var countsBefore = count.ToArray(); var totalsBefore = total.ToArray();
        Assert.Equal((38L, 20L), NativeMinuteProductionCounters.Read(1109, 1109, count, cursor, total));
        Assert.Equal(countsBefore, count); Assert.Equal(totalsBefore, total);
        total[6] = 0; total[13] = 0;
        Assert.Equal((38L, 20L), NativeMinuteProductionCounters.Read(1109, 1109, count, cursor, total));
    }

    [Theory]
    [InlineData("item")] [InlineData("counts")] [InlineData("cursors")] [InlineData("totals")]
    [InlineData("production_cursor")] [InlineData("consumption_cursor")]
    [InlineData("negative_bucket")] [InlineData("negative_total")] [InlineData("mismatch")]
    public void MalformedOrInconsistentNativeRingsFailClosed(string fault)
    {
        var (count, cursor, total) = Rings(); var item = 1109;
        switch (fault)
        {
            case "item": item++; break;
            case "counts": count = new int[7199]; break;
            case "cursors": cursor = new int[11]; break;
            case "totals": total = new long[13]; break;
            case "production_cursor": cursor[1] = 1200; break;
            case "consumption_cursor": cursor[7] = 4199; break;
            case "negative_bucket": count[700] = -1; break;
            case "negative_total": total[1] = -1; break;
            case "mismatch": total[8]++; break;
        }
        Assert.Throws<ArgumentException>(() => NativeMinuteProductionCounters.Read(1109, item, count, cursor, total));
    }

    [Fact]
    public void PerfectlyPeriodicLowRateProductionCanFailTenSecondsWithoutBeingReclassified()
    {
        // Synthetic native counters: one item every95 ticks, no downtime. This
        // demonstrates resolution, NOT that the live graphite line is stable.
        var seconds = new GovernorMeasurementSeries(); var minute = new GovernorMeasurementSeries();
        foreach (var width in new[] { 600, 3600 })
            for (var end = width; end <= 4 * width; end += width)
            {
                var count = end / 95 - (end - width) / 95;
                var read = NativeProductionRateCalculator.Calculate(end, count, 0, width);
                (width == 600 ? seconds : minute).Observe("source", read.Window,
                    (decimal)read.ActualProductionPerMinute, new Dictionary<int, long>(), width);
            }
        Assert.Equal("unstable", seconds.Baseline(.1m).State);
        Assert.Equal(38, seconds.Baseline(.1m).ProductionPerMinute);
        Assert.Equal("ready", minute.Baseline(.1m).State);
        Assert.Equal(38, minute.Baseline(.1m).ProductionPerMinute);
        Assert.Equal(3, minute.Baseline(.1m).IndependentWindowCount);
    }

    [Fact]
    public void DurationChangeDiscardsCandidateHistoryEvenWhenSourceIsUnchanged()
    {
        var series = new GovernorMeasurementSeries();
        for (var tick = 600; tick <= 2400; tick += 600) Sample(series, tick, 30, 600);
        Assert.Equal("ready", series.Baseline(.1m).State);
        Sample(series, 3600, 30, 3600);
        Assert.Equal(0, series.Baseline(.1m).IndependentWindowCount);
        Assert.NotEqual(GovernorSourceBinding.CreateMeasurementBinding("key", "source", new[] { 1109 }),
            GovernorSourceBinding.CreateMeasurementBinding("key", "source", new[] { 1109 }, 3600));
        Sample(series, 7200, 30, 3600); Sample(series, 10800, 30, 3600); Sample(series, 14400, 30, 3600);
        Assert.Equal("ready", series.Baseline(.1m).State);
        Sample(series, 14406, 30, 600);
        Assert.Equal(0, series.Baseline(.1m).IndependentWindowCount);
    }

    [Fact]
    public void AlignmentCannotImportHistoryFromBeforeTheActualBindingCapture()
    {
        var series = new GovernorMeasurementSeries();
        Sample(series, 3605, 38, 3600); Sample(series, 7200, 38, 3600);
        Assert.Equal(0, series.Baseline(.1m).IndependentWindowCount); // Start3601 < binding3605.
        Sample(series, 7206, 38, 3600); Sample(series, 7209, 38, 3600);
        Assert.Equal(1, series.Baseline(.1m).IndependentWindowCount);
        Assert.Equal(7206, series.PreviousStockTick); // Actual capture, not rounded minute end.
        Sample(series, 7211, 38, 3600);
        Assert.Equal(7209, series.PreviousStockTick);
    }

    [Theory]
    [InlineData("gap")] [InlineData("source")] [InlineData("zero")] [InlineData("varying")]
    public void MinuteCandidateStillRejectsMissingOrUnstableEvidence(string fault)
    {
        var series = new GovernorMeasurementSeries();
        Sample(series, 3600, 38, 3600); Sample(series, 7200, 38, 3600); Sample(series, 10800, 38, 3600);
        Sample(series, fault == "gap" ? 18006 : 14400, fault == "zero" ? 0 : fault == "varying" ? 50 : 38,
            3600, fault == "source" ? "changed" : "source");
        Assert.NotEqual("ready", series.Baseline(.1m).State);
    }

    [Fact]
    public void MinuteValidationStillNeeds36000CoveredPostLockTicks()
    {
        var run = StartMinute();
        for (var end = 18000; end < 50400; end += 3600)
            Assert.False(run.Observe(Current(end), true).DoubleThroughputTargetObserved);
        var result = run.Observe(Current(50400), true);
        Assert.True(result.DoubleThroughputTargetObserved); Assert.Equal(36000, result.ObservedContiguousGameTicks);
        Assert.Equal(3600, result.MeasurementGameTicks); Assert.Equal(10, result.ObservationCount);
        Assert.Equal(2, result.TargetMultiplier); Assert.Equal(.1m, result.ToleranceFraction);
        Assert.Contains("native_3600", result.MeasurementBasis);
    }

    [Theory]
    [InlineData("zero")] [InlineData("low")] [InlineData("high")] [InlineData("power")]
    [InlineData("findings")] [InlineData("attribution")] [InlineData("writes")]
    [InlineData("misaligned")] [InlineData("stale")] [InlineData("short_window")]
    public void MinuteValidationDoesNotMaskFailureOrWrongWindow(string fault)
    {
        var run = StartMinute(); run.Observe(Current(18000), true); var next = Current(21600);
        switch (fault)
        {
            case "zero": next.Supply[0].ActualProductionPerMinute = 0; break;
            case "low": next.Supply[0].ActualProductionPerMinute = 68; break;
            case "high": next.Supply[0].ActualProductionPerMinute = 84; break;
            case "power": next.Power.MinimumConsumerRatio = .5; break;
            case "findings": next.TargetChainFindings.Add(new OverseerFindingSnapshot()); break;
            case "attribution": next.SelectionContainsAllTargetProducers = false; break;
            case "misaligned": next.CurrentWindow.EndGameTick--; next.CurrentWindow.StartGameTick--; break;
            case "stale": next.CapturedAtGameTick += 6; break;
            case "short_window": next.CurrentWindow.ElapsedGameTicks = 600; break;
        }
        var result = run.Observe(next, fault != "writes");
        Assert.Equal(0, result.ObservedContiguousGameTicks); Assert.False(result.ThroughputTargetObserved);
    }

    [Fact]
    public void AlignedOverlapCountsOnlyUnionAndASixTickGapResets()
    {
        var run = StartMinute(); run.Observe(Current(18005), true);
        Assert.Equal(5400, run.Observe(Current(19800), true).ObservedContiguousGameTicks);
        Assert.Equal(2, run.Observe(Current(19805), true).ObservationCount);
        var afterGap = run.Observe(Current(23406), true);
        Assert.Equal(3600, afterGap.ObservedContiguousGameTicks);
        Assert.Equal("unobserved_game_tick_gap", afterGap.ResetReason);
    }

    [Fact]
    public void LockedPeriodSurvivesProtectedResumeAndCannotBeChanged()
    {
        var run = StartMinute(); var saved = run.CreateCheckpoint("owned", "version");
        Assert.Equal(3, saved.Version); Assert.Equal(3600, saved.MeasurementGameTicks); saved.Validate();
        var restored = GovernorThroughputValidation.Restore(saved, "owned", "version", "new-session", 15000, 15030);
        Assert.Equal(0, restored.Snapshot().ObservedContiguousGameTicks);
        Assert.Equal(3600, restored.Snapshot().MeasurementGameTicks);
        var current = Current(19000); current.SessionId = "new-session"; current.MeasurementGameTicks = 600;
        Assert.Equal("governor_validation_declaration_mismatch", Assert.Throws<FoundryPlanningException>(
            () => restored.Observe(current, true)).Reason);
        saved.MeasurementGameTicks = 600;
        Assert.Throws<FoundryPlanningException>(saved.Validate); // Hash bound to original3600.
    }

    [Fact]
    public void LegacyCheckpointKeepsItsOriginalHashAndTenSecondMeaning()
    {
        var saved = StartMinute().CreateCheckpoint("owned", "version");
        saved.Version = 1; saved.MeasurementGameTicks = 600;
        saved.IntegrityHash = CanonicalStateHash.Combine("governor-locked-declaration-v1",
            saved.Version, saved.OwnedIdentityHash, saved.GameVersion, saved.SourceSessionId, saved.BaselineProposalHash,
            saved.SourceStateHash, saved.ScalePlanHash, saved.PlanetId, saved.TargetItemId, saved.DeclaredAtGameTick,
            saved.DeclarationRevision, saved.LockedAtGameTick, saved.BaselineRatePerMinute, saved.TargetRatePerMinute,
            saved.ToleranceFraction, saved.RequiredGameTicks);
        saved.Validate();
        var json = System.Text.Json.JsonSerializer.Serialize(saved).Replace("\"MeasurementGameTicks\":600,", "");
        var old = System.Text.Json.JsonSerializer.Deserialize<GovernorValidationCheckpoint>(json)!;
        old.Validate(); Assert.Equal(600, old.MeasurementGameTicks);
        Assert.Equal(600, GovernorThroughputValidation.Restore(old, "owned", "version", "resumed", 15000, 15030)
            .Snapshot().MeasurementGameTicks);
        old.MeasurementGameTicks = 3600; old.IntegrityHash = old.CalculateIntegrityHash();
        Assert.Throws<FoundryPlanningException>(old.Validate); // v1 cannot reinterpret even with a recomputed hash.
    }

    private static void Sample(GovernorMeasurementSeries series, long capture, decimal rate, int width, string binding = "source") =>
        series.Observe(binding, NativeProductionRateCalculator.Calculate(capture, 0, 0, width).Window,
            rate, new Dictionary<int, long>(), width, capture);

    private static (int[] Count, int[] Cursor, long[] Total) Rings()
    {
        var count = new int[7200]; var cursor = new int[12]; var total = new long[14];
        cursor[1] = 600; cursor[7] = 4200; count[601] = 17; count[1099] = 21; count[4200] = 20;
        total[1] = 38; total[8] = 20; return (count, cursor, total);
    }

    private static GovernorThroughputValidation StartMinute()
    {
        var declaration = Current(14400, 38); var run = new GovernorThroughputValidation(declaration);
        run.Begin(declaration, true); return run;
    }

    private static GovernorPlanSnapshot Current(long tick, decimal rate = 76) => new()
    {
        SessionId = "session", PlanetId = 104, TargetItemId = 1109, Revision = 3,
        CapturedAtGameTick = tick, ProposalHash = "original", SourceStateHash = "source",
        TargetRatePerMinute = 76, ToleranceFraction = .1m, ValidationGameTicks = 36000, MeasurementGameTicks = 3600,
        SelectionContainsAllTargetProducers = true,
        Baseline = new GovernorBaselineSnapshot { State = "ready", IndependentWindowCount = 3,
            StartGameTick = 3601, EndGameTick = 14400, ProductionPerMinute = 38, MinimumPerMinute = 38, MaximumPerMinute = 38 },
        CurrentWindow = NativeProductionRateCalculator.Calculate(tick, 0, 0, 3600).Window,
        FullTargetScale = new FoundryPlanSnapshot { PlanHash = "scale" },
        Power = new OverseerPowerSummarySnapshot { MinimumConsumerRatio = 1 },
        Supply = new List<GovernorSupplyBalance> { new() { ItemId = 1109, ActualProductionPerMinute = rate } },
    };
}
