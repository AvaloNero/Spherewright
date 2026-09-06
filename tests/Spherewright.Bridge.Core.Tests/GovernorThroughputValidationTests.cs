using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Diagnostics;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class GovernorThroughputValidationTests
{
    [Fact]
    public void TargetNeedsAll36000CoveredGameTicksNotWallTimeOrOneFastSample()
    {
        var run = Started();
        for (var tick = 3000; tick < 38400; tick += 600)
            Assert.False(run.Observe(Current(tick), true).ThroughputTargetObserved);
        var result = run.Observe(Current(38400), true);
        Assert.Equal(36000, result.ObservedContiguousGameTicks);
        Assert.True(result.ThroughputTargetObserved); Assert.True(result.DoubleThroughputTargetObserved);
        Assert.False(result.Durable); Assert.Equal(60, result.ObservationCount);
        Assert.Equal(30, result.BaselineProductionPerMinute); Assert.Equal(2, result.TargetMultiplier);
        Assert.Equal(2400, result.LockedAtGameTick);
        Assert.Contains("sampled", result.HealthEvidenceBasis);
        Assert.Contains(result.RemainingChecks, c => c.Contains("not complete balance"));
    }

    [Fact]
    public void OverlappingWindowsCoverOnlyTheirUnionAndDuplicateReadsAddNothing()
    {
        var run = Started();
        run.Observe(Current(3000), true);
        Assert.Equal(900, run.Observe(Current(3300), true).ObservedContiguousGameTicks);
        var duplicate = run.Observe(Current(3300), true);
        Assert.Equal(900, duplicate.ObservedContiguousGameTicks); Assert.Equal(2, duplicate.ObservationCount);
    }

    [Fact]
    public void AOneTickUnobservedGapStartsANewWindowWithoutBridgingHistory()
    {
        var run = Started(); run.Observe(Current(3000), true);
        var result = run.Observe(Current(3601), true);
        Assert.Equal(600, result.ObservedContiguousGameTicks);
        Assert.Equal("unobserved_game_tick_gap", result.ResetReason);
    }

    [Theory]
    [InlineData(0)] [InlineData(53)] [InlineData(67)]
    public void ZeroOrOutOfToleranceOutputResetsRatherThanDeclaringBalance(int rate)
    {
        var run = Started(); run.Observe(Current(3000), true);
        var result = run.Observe(Current(3600, rate), true);
        Assert.Equal(0, result.ObservedContiguousGameTicks);
        Assert.Equal("measured_rate_outside_declared_tolerance", result.ResetReason);
        Assert.False(result.ThroughputTargetObserved);
    }

    [Theory]
    [InlineData("session")] [InlineData("target")] [InlineData("item")]
    [InlineData("planet")] [InlineData("tolerance")] [InlineData("duration")] [InlineData("recipe_scale")]
    public void ACallerCannotRelaxOrRetargetTheOriginalDeclaration(string changed)
    {
        var run = Started(); var current = Current(3000);
        switch (changed)
        {
            case "session": current.SessionId = "new-session"; break;
            case "target": current.TargetRatePerMinute = 55; break;
            case "item": current.TargetItemId++; break;
            case "planet": current.PlanetId++; break;
            case "tolerance": current.ToleranceFraction = .2m; break;
            case "duration": current.ValidationGameTicks = 36001; break;
            case "recipe_scale": current.FullTargetScale.PlanHash = "changed"; break;
        }
        var error = Assert.Throws<FoundryPlanningException>(() => run.Observe(current, true));
        Assert.Equal("governor_validation_declaration_mismatch", error.Reason);
        Assert.Equal(0, run.Snapshot().ObservedContiguousGameTicks);
    }

    [Theory]
    [InlineData("write")] [InlineData("source")] [InlineData("age")] [InlineData("already_expanded")]
    public void BaselineMustBeExplicitlyLockedBeforeExpansionOrWrites(string changed)
    {
        var run = new GovernorThroughputValidation(Declaration());
        var current = Declaration();
        switch (changed)
        {
            case "write": current.Revision++; break;
            case "source": current.SourceStateHash = "expanded"; break;
            case "age": current.CapturedAtGameTick += 3601; break;
            case "already_expanded": current.Supply[0].ActualProductionPerMinute = 60; break;
        }
        Assert.Equal("governor_validation_start_stale",
            Assert.Throws<FoundryPlanningException>(() => run.Begin(current, true)).Reason);
        Assert.Null(run.Snapshot().LockedAtGameTick);
    }

    [Fact]
    public void SourceChangesAfterChosenExpansionRequireEntirelyPostChangeWindows()
    {
        var run = Started(); run.Observe(Current(3000), true);
        var expanded = Current(3300); expanded.SourceStateHash = "expanded";
        Assert.Equal(0, run.Observe(expanded, true).ObservedContiguousGameTicks);
        expanded = Current(3899); expanded.SourceStateHash = "expanded";
        Assert.Equal(600, run.Observe(expanded, true).ObservedContiguousGameTicks);
        Assert.Equal(30, run.Snapshot().BaselineProductionPerMinute);
    }

    [Theory]
    [InlineData("power")] [InlineData("truncated")] [InlineData("attribution")]
    [InlineData("finding")] [InlineData("reciprocal")] [InlineData("writes")]
    public void SampledHealthOrAttributionFailureClearsTheThroughputStreak(string changed)
    {
        var run = Started(); run.Observe(Current(3000), true); var current = Current(3600);
        switch (changed)
        {
            case "power": current.Power.MinimumConsumerRatio = .9; break;
            case "truncated": current.FindingsTruncated = true; break;
            case "attribution": current.SelectionContainsAllTargetProducers = false; break;
            case "finding": current.TargetChainFindings.Add(new OverseerFindingSnapshot()); break;
            case "reciprocal": current.Blockers.Add("selected_reciprocal_connection_unproven:715:0"); break;
        }
        var result = run.Observe(current, changed != "writes");
        Assert.Equal(0, result.ObservedContiguousGameTicks);
        Assert.Equal("sampled_health_or_attribution_unproven", result.ResetReason);
    }

    [Theory]
    [InlineData("not_ready")] [InlineData("wrong_duration")] [InlineData("cross_session")] [InlineData("old_window")]
    public void InvalidOrStaleNativeWindowsCannotContribute(string changed)
    {
        var run = Started(); var current = Current(3000);
        switch (changed)
        {
            case "not_ready": current.CurrentWindow.State = "warming_up"; break;
            case "wrong_duration": current.CurrentWindow.ElapsedGameTicks = 599; break;
            case "cross_session": current.CurrentWindow.CrossedSessionBoundary = true; break;
            case "old_window": current.CapturedAtGameTick += 3; break;
        }
        Assert.Equal("native_window_unavailable", run.Observe(current, true).ResetReason);
    }

    [Theory]
    [InlineData("zero")] [InlineData("unstable")] [InlineData("few_samples")] [InlineData("unattributed")]
    public void InvalidPreExecutionBaselinesAreNeverAccepted(string changed)
    {
        var baseline = Declaration();
        switch (changed)
        {
            case "zero": baseline.Baseline.ProductionPerMinute = 0; break;
            case "unstable": baseline.Baseline.State = "unstable"; break;
            case "few_samples": baseline.Baseline.IndependentWindowCount = 2; break;
            case "unattributed": baseline.SelectionContainsAllTargetProducers = false; break;
        }
        Assert.Throws<FoundryPlanningException>(() => new GovernorThroughputValidation(baseline));
    }

    [Fact]
    public void ImmutableScalarsSurviveExternalDtoEditsAndTickRegressionLosesStreak()
    {
        var baseline = Declaration(); var run = new GovernorThroughputValidation(baseline);
        run.Begin(baseline, true); baseline.Baseline.ProductionPerMinute = 1; baseline.TargetRatePerMinute = 2;
        run.Observe(Current(3000), true);
        var regressed = run.Observe(Current(2999), true);
        Assert.Equal(30, regressed.BaselineProductionPerMinute); Assert.Equal(60, regressed.TargetRatePerMinute);
        Assert.Equal(0, regressed.ObservedContiguousGameTicks); Assert.Equal("game_tick_regressed", regressed.ResetReason);
    }

    [Fact]
    public void AOnePointFiveTargetIsNotReportedAsTheDoubleThroughputGate()
    {
        var baseline = Declaration(); baseline.TargetRatePerMinute = 45;
        var run = new GovernorThroughputValidation(baseline); run.Begin(baseline, true);
        for (var tick = 3000; tick <= 38400; tick += 600)
        {
            var current = Current(tick, 45); current.TargetRatePerMinute = 45; run.Observe(current, true);
        }
        Assert.True(run.Snapshot().ThroughputTargetObserved); Assert.False(run.Snapshot().DoubleThroughputTargetObserved);
    }

    [Fact]
    public void ReadingAReadyProposalDoesNotImplicitlyStartTheExperiment()
    {
        var run = new GovernorThroughputValidation(Declaration());
        Assert.Throws<FoundryPlanningException>(() => run.Observe(Current(3000), true));
    }

    [Fact]
    public void UnattributedGlobalWarningsRemainSeparateFromAHealthyMeasuredTarget()
    {
        var run = Started(); var current = Current(3000);
        current.Findings.Add(new OverseerFindingSnapshot { PlanetId = 104, ObjectId = 14, Kind = "vein_exhausted" });
        current.UnattributedPlanetFindingCount = 1;
        Assert.Equal(600, run.Observe(current, true).ObservedContiguousGameTicks);
        Assert.Single(current.Findings); Assert.False(current.Balanced);
    }

    private static GovernorThroughputValidation Started()
    {
        var declaration = Declaration(); var run = new GovernorThroughputValidation(declaration);
        run.Begin(declaration, true); return run;
    }
    private static GovernorPlanSnapshot Declaration() => Current(2400, 30);
    private static GovernorPlanSnapshot Current(long tick, decimal rate = 60) => new()
    {
        SessionId = "owned-session", PlanetId = 104, TargetItemId = 1112, Revision = 1,
        CapturedAtGameTick = tick, ProposalHash = "pre-execution-proposal", SourceStateHash = "source",
        TargetRatePerMinute = 60, ToleranceFraction = .1m, ValidationGameTicks = 36000,
        SelectionContainsAllTargetProducers = true,
        Baseline = new GovernorBaselineSnapshot { State = "ready", IndependentWindowCount = 3,
            StartGameTick = 601, EndGameTick = 2400, ProductionPerMinute = 30, MinimumPerMinute = 30, MaximumPerMinute = 30 },
        CurrentWindow = new OverseerWindowSnapshot { State = "ready", StartGameTick = tick - 599, EndGameTick = tick, ElapsedGameTicks = 600 },
        FullTargetScale = new FoundryPlanSnapshot { PlanHash = "scale" },
        Power = new OverseerPowerSummarySnapshot { MinimumConsumerRatio = 1 },
        Supply = new List<GovernorSupplyBalance> { new() { ItemId = 1112, ActualProductionPerMinute = rate } },
    };
}
