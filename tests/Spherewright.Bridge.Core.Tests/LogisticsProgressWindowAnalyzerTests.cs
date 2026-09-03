using Spherewright.Bridge.Core.Diagnostics;
using Spherewright.Contracts.Diagnostics;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class LogisticsProgressWindowAnalyzerTests
{
    [Fact]
    public void Analyze_RequiresABoundedStagnantWindowBeforeReportingKnownNoProgress()
    {
        var initial = LogisticsProgressWindowAnalyzer.Analyze(null, Sample(1_000));
        var warming = LogisticsProgressWindowAnalyzer.Analyze(initial.NextState, Sample(1_599));
        var ready = LogisticsProgressWindowAnalyzer.Analyze(warming.NextState, Sample(1_600));

        Assert.Equal(OverseerWindowStates.WarmingUp, warming.Window.State);
        Assert.False(warming.ProgressStateKnown);
        Assert.Equal(599, warming.Window.ElapsedGameTicks);
        Assert.Equal(OverseerWindowStates.Ready, ready.Window.State);
        Assert.True(ready.ProgressStateKnown);
        Assert.False(ready.ProgressObserved);
        Assert.Equal(600, ready.Window.ElapsedGameTicks);
    }

    [Fact]
    public void Analyze_CarrierMotionResetsTheStagnantBaselineAsObservedProgress()
    {
        var initial = LogisticsProgressWindowAnalyzer.Analyze(null, Sample(1_000, fingerprint: "carrier-a"));
        var progress = LogisticsProgressWindowAnalyzer.Analyze(
            initial.NextState,
            Sample(1_120, fingerprint: "carrier-b"));
        var warming = LogisticsProgressWindowAnalyzer.Analyze(
            progress.NextState,
            Sample(1_719, fingerprint: "carrier-b"));

        Assert.Equal(OverseerWindowStates.Ready, progress.Window.State);
        Assert.True(progress.ProgressStateKnown);
        Assert.True(progress.ProgressObserved);
        Assert.Equal(120, progress.Window.ElapsedGameTicks);
        Assert.Equal(OverseerWindowStates.WarmingUp, warming.Window.State);
        Assert.False(warming.ProgressStateKnown);
        Assert.Equal(599, warming.Window.ElapsedGameTicks);
    }

    [Fact]
    public void Analyze_DeliveryAndOrderReductionAreProgressEvidence()
    {
        var initial = LogisticsProgressWindowAnalyzer.Analyze(null, Sample(1_000));
        var delivery = Sample(1_100);
        delivery.DemandInventoryCount = 40;
        delivery.OutstandingOrderMagnitude = 80;

        var result = LogisticsProgressWindowAnalyzer.Analyze(initial.NextState, delivery);

        Assert.True(result.ProgressObserved);
        Assert.True(result.ProgressStateKnown);
    }

    [Fact]
    public void Analyze_PreservesTheWindowAcrossSessionRestartAndExcludesOfflineTime()
    {
        var initial = LogisticsProgressWindowAnalyzer.Analyze(
            null,
            Sample(1_000, sessionId: "old", capturedAt: "2026-09-03T00:00:00Z"));
        var current = Sample(1_600, sessionId: "new", capturedAt: "2026-09-03T02:00:10Z");

        var result = LogisticsProgressWindowAnalyzer.Analyze(initial.NextState, current);

        Assert.Equal(OverseerWindowStates.Ready, result.Window.State);
        Assert.True(result.Window.CrossedSessionBoundary);
        Assert.Equal(10, result.Window.ElapsedGameSeconds);
        Assert.True(result.Window.ExcludedNonGameSeconds > 7_199);
    }

    [Theory]
    [InlineData(false, 100, 4)]
    [InlineData(true, 0, 4)]
    [InlineData(true, 100, 0)]
    public void Analyze_NonQualifyingRouteResetsInsteadOfInventingAStall(
        bool orderOutstanding,
        long sourceInventory,
        int carrierFleet)
    {
        var initial = LogisticsProgressWindowAnalyzer.Analyze(null, Sample(1_000));
        var current = Sample(2_000);
        current.OrderOutstanding = orderOutstanding;
        current.OutstandingOrderMagnitude = orderOutstanding ? 100 : 0;
        current.SourceInventoryCount = sourceInventory;
        current.CarrierFleetCount = carrierFleet;
        current.ActiveRouteCarrierCount = Math.Min(current.ActiveRouteCarrierCount, carrierFleet);

        var result = LogisticsProgressWindowAnalyzer.Analyze(initial.NextState, current);

        Assert.Equal(OverseerWindowStates.WarmingUp, result.Window.State);
        Assert.Equal(OverseerWindowResetReasons.LogisticsObservationNotQualifying, result.Window.ResetReason);
        Assert.False(result.ProgressStateKnown);
    }

    [Fact]
    public void Analyze_SufficientConsumerInputResetsTheStagnantWindow()
    {
        var initial = LogisticsProgressWindowAnalyzer.Analyze(null, Sample(1_000));
        var sufficient = Sample(1_300);
        sufficient.ConsumerInputMissing = false;

        var reset = LogisticsProgressWindowAnalyzer.Analyze(initial.NextState, sufficient);
        var missingAgain = LogisticsProgressWindowAnalyzer.Analyze(reset.NextState, Sample(1_899));
        var warming = LogisticsProgressWindowAnalyzer.Analyze(missingAgain.NextState, Sample(2_498));

        Assert.Equal(OverseerWindowResetReasons.LogisticsObservationNotQualifying, reset.Window.ResetReason);
        Assert.Equal(OverseerWindowStates.WarmingUp, missingAgain.Window.State);
        Assert.False(missingAgain.ProgressStateKnown);
        Assert.Equal(OverseerWindowStates.WarmingUp, warming.Window.State);
        Assert.False(warming.ProgressStateKnown);
        Assert.Equal(599, warming.Window.ElapsedGameTicks);
    }

    [Theory]
    [InlineData("other-save", "route", 1_100, OverseerWindowResetReasons.OwnedSaveChanged)]
    [InlineData("save", "other-route", 1_100, OverseerWindowResetReasons.LogisticsRouteChanged)]
    [InlineData("save", "route", 999, OverseerWindowResetReasons.GameTickRegressed)]
    [InlineData("save", "route", 4_601, OverseerWindowResetReasons.LogisticsSampleGapExceeded)]
    public void Analyze_InvalidatesIdentityTickAndSamplingDiscontinuities(
        string saveKey,
        string routeKey,
        long gameTick,
        string reason)
    {
        var initial = LogisticsProgressWindowAnalyzer.Analyze(null, Sample(1_000));
        var current = Sample(gameTick);
        current.ProtectedSaveKey = saveKey;
        current.RouteKey = routeKey;

        var result = LogisticsProgressWindowAnalyzer.Analyze(initial.NextState, current);

        Assert.Equal(OverseerWindowStates.Discontinuous, result.Window.State);
        Assert.Equal(reason, result.Window.ResetReason);
        Assert.False(result.ProgressStateKnown);
    }

    [Fact]
    public void Analyze_RejectsObservableChangeAtTheSameGameTick()
    {
        var initial = LogisticsProgressWindowAnalyzer.Analyze(null, Sample(1_000));
        var current = Sample(1_000, fingerprint: "changed");

        var result = LogisticsProgressWindowAnalyzer.Analyze(initial.NextState, current);

        Assert.Equal(OverseerWindowStates.Discontinuous, result.Window.State);
        Assert.Equal(
            OverseerWindowResetReasons.LogisticsStateAdvancedWithoutGameTime,
            result.Window.ResetReason);
    }

    [Fact]
    public void Analyze_RepeatedSameTickKeepsAMatureWindowReady()
    {
        var initial = LogisticsProgressWindowAnalyzer.Analyze(null, Sample(1_000));
        var ready = LogisticsProgressWindowAnalyzer.Analyze(initial.NextState, Sample(1_600));

        var repeated = LogisticsProgressWindowAnalyzer.Analyze(ready.NextState, Sample(1_600));

        Assert.Equal(OverseerWindowStates.Ready, repeated.Window.State);
        Assert.True(repeated.ProgressStateKnown);
        Assert.Equal(600, repeated.Window.ElapsedGameTicks);
    }

    private static LogisticsProgressSample Sample(
        long gameTick,
        string fingerprint = "carrier-a",
        string sessionId = "session",
        string capturedAt = "2026-09-03T00:00:00Z")
    {
        return new LogisticsProgressSample
        {
            ProtectedSaveKey = "save",
            RouteKey = "route",
            SessionId = sessionId,
            GameTick = gameTick,
            CapturedAtUtc = DateTimeOffset.Parse(capturedAt).AddSeconds((gameTick - 1_000) / 60d),
            OrderOutstanding = true,
            OutstandingOrderMagnitude = 100,
            ConsumerInputMissing = true,
            DemandInventoryCount = 0,
            SourceInventoryCount = 100,
            CarrierFleetCount = 4,
            ActiveRouteCarrierCount = 1,
            CarrierProgressFingerprint = fingerprint,
        };
    }
}
