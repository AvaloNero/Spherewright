using Spherewright.Bridge.Core.Diagnostics;
using Spherewright.Contracts.Diagnostics;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class OverseerCounterWindowAnalyzerTests
{
    [Fact]
    public void Analyze_UsesGameTicksForRatesAndExcludesOfflineWallTime()
    {
        var previous = Sample("save", "old-session", 1_000, 100, 40, "2026-09-03T00:00:00Z");
        var current = Sample("save", "new-session", 1_600, 120, 45, "2026-09-03T01:00:10Z");

        var result = OverseerCounterWindowAnalyzer.Analyze(
            previous,
            current,
            theoreticalProductionPerMinute: 240,
            maximumAdjacentSampleGapTicks: 600);

        Assert.Equal(OverseerWindowStates.Ready, result.Window.State);
        Assert.True(result.Window.CrossedSessionBoundary);
        Assert.Equal(10, result.Window.ElapsedGameSeconds);
        Assert.Equal(20, result.ProducedDelta);
        Assert.Equal(120, result.ActualProductionPerMinute);
        Assert.Equal(30, result.ActualConsumptionPerMinute);
        Assert.Equal(0.5, result.Utilization);
        Assert.True(result.Window.ExcludedNonGameSeconds > 3_599);
    }

    [Fact]
    public void Analyze_DoesNotCreateRateAtSameGameTick()
    {
        var previous = Sample("save", "session", 1_000, 100, 40, "2026-09-03T00:00:00Z");
        var current = Sample("save", "session", 1_000, 100, 40, "2026-09-03T02:00:00Z");

        var result = OverseerCounterWindowAnalyzer.Analyze(previous, current);

        Assert.Equal(OverseerWindowStates.WarmingUp, result.Window.State);
        Assert.Equal(OverseerWindowResetReasons.SameGameTick, result.Window.ResetReason);
        Assert.Equal(0, result.ActualProductionPerMinute);
    }

    [Theory]
    [InlineData("other-save", 1_600, 120, 45, OverseerWindowResetReasons.OwnedSaveChanged)]
    [InlineData("save", 900, 120, 45, OverseerWindowResetReasons.GameTickRegressed)]
    [InlineData("save", 1_600, 99, 45, OverseerWindowResetReasons.CounterRegressed)]
    [InlineData("save", 1_601, 120, 45, OverseerWindowResetReasons.SampleGapExceeded)]
    public void Analyze_InvalidatesDiscontinuousSamples(
        string saveKey,
        long tick,
        long produced,
        long consumed,
        string expectedReason)
    {
        var previous = Sample("save", "session", 1_000, 100, 40, "2026-09-03T00:00:00Z");
        var current = Sample(saveKey, "session", tick, produced, consumed, "2026-09-03T00:00:10Z");

        var result = OverseerCounterWindowAnalyzer.Analyze(previous, current, maximumAdjacentSampleGapTicks: 600);

        Assert.Equal(OverseerWindowStates.Discontinuous, result.Window.State);
        Assert.Equal(expectedReason, result.Window.ResetReason);
        Assert.Equal(0, result.ProducedDelta);
    }

    [Fact]
    public void Analyze_RejectsCounterAdvanceWithoutGameTime()
    {
        var previous = Sample("save", "session", 1_000, 100, 40, "2026-09-03T00:00:00Z");
        var current = Sample("save", "session", 1_000, 101, 40, "2026-09-03T00:00:01Z");

        var result = OverseerCounterWindowAnalyzer.Analyze(previous, current);

        Assert.Equal(OverseerWindowStates.Discontinuous, result.Window.State);
        Assert.Equal(OverseerWindowResetReasons.CounterAdvancedWithoutGameTime, result.Window.ResetReason);
    }

    private static OverseerCounterSample Sample(
        string saveKey,
        string sessionId,
        long tick,
        long produced,
        long consumed,
        string capturedAtUtc)
    {
        return new OverseerCounterSample
        {
            ProtectedSaveKey = saveKey,
            SessionId = sessionId,
            GameTick = tick,
            ProducedTotal = produced,
            ConsumedTotal = consumed,
            CapturedAtUtc = DateTimeOffset.Parse(capturedAtUtc),
        };
    }
}
