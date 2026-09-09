using Spherewright.Bridge.Core.Diagnostics;
using Spherewright.Contracts.Diagnostics;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class NativeProductionRateCalculatorTests
{
    [Fact]
    public void FullNativeWindow_UsesOnlySixHundredGameTicks()
    {
        var result = NativeProductionRateCalculator.Calculate(12_000, 10, 4);

        Assert.Equal(OverseerWindowStates.Ready, result.Window.State);
        Assert.Null(result.Window.ResetReason);
        Assert.Equal(11_401, result.Window.StartGameTick);
        Assert.Equal(12_000, result.Window.EndGameTick);
        Assert.Equal(600, result.Window.ElapsedGameTicks);
        Assert.Equal(10d, result.Window.ElapsedGameSeconds);
        Assert.Equal(60d, result.ActualProductionPerMinute);
        Assert.Equal(24d, result.ActualConsumptionPerMinute);
        Assert.Equal(0d, result.Window.WallClockElapsedSeconds);
    }

    [Fact]
    public void YoungWorld_ReportsWarmingWindowAndUsesAvailableGameTicks()
    {
        var result = NativeProductionRateCalculator.Calculate(299, 5, 2);

        Assert.Equal(OverseerWindowStates.WarmingUp, result.Window.State);
        Assert.Equal(OverseerWindowResetReasons.NativeWindowNotFull, result.Window.ResetReason);
        Assert.Equal(0, result.Window.StartGameTick);
        Assert.Equal(300, result.Window.ElapsedGameTicks);
        Assert.Equal(60d, result.ActualProductionPerMinute);
        Assert.Equal(24d, result.ActualConsumptionPerMinute);
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, -1)]
    public void NegativeNativeValues_FailClosed(long tick, long produced, long consumed)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NativeProductionRateCalculator.Calculate(tick, produced, consumed));
    }

    [Fact]
    public void GappedWindows_CanShowSampledDeficitOnAConservedPeriodicLine()
    {
        // Synthetic events, with the relative capture times of the 24-window live diagnostic.
        // One item is produced then consumed every 480 ticks; no full-interval deficit exists.
        int[] ends = [599, 1223, 1849, 2468, 3098, 3718, 4342, 4964,
            5588, 6217, 6836, 7462, 8079, 8685, 9312, 9919,
            10539, 11169, 11789, 12417, 13021, 13644, 14276, 14899];
        var produced = PeriodicEvents(480, 79, ends[^1]);
        var consumed = PeriodicEvents(480, 239, ends[^1]);
        var samples = ends.Select(end => NativeProductionRateCalculator.Calculate(
            600 + end, CountInWindow(produced, end), CountInWindow(consumed, end))).ToArray();

        Assert.Equal(31, produced.Length);
        Assert.Equal(produced.Length, consumed.Length);
        Assert.All(produced.Zip(consumed), pair => Assert.True(pair.First < pair.Second));
        Assert.Equal(14_400, samples.Sum(sample => sample.Window.ElapsedGameTicks));
        var span = samples[^1].Window.EndGameTick - samples[0].Window.StartGameTick + 1;
        Assert.Equal(14_900, span);
        Assert.Equal(500, span - samples.Sum(sample => sample.Window.ElapsedGameTicks));
        Assert.Equal(29d, samples.Sum(sample => sample.ActualProductionPerMinute / 6d));
        Assert.Equal(31d, samples.Sum(sample => sample.ActualConsumptionPerMinute / 6d));
        // The native rates are correct; summing gapped windows is not a conservation test.
    }

    [Fact]
    public void OverlappingWindows_DoubleCountEventsIfSummedInsteadOfKeptSeparate()
    {
        var produced = PeriodicEvents(100, 80, 899);
        var first = NativeProductionRateCalculator.Calculate(1199, CountInWindow(produced, 599), 0);
        var second = NativeProductionRateCalculator.Calculate(1499, CountInWindow(produced, 899), 0);

        Assert.Equal(9, produced.Length);
        Assert.Equal(36d, first.ActualProductionPerMinute);
        Assert.Equal(36d, second.ActualProductionPerMinute);
        Assert.Equal(12d, (first.ActualProductionPerMinute + second.ActualProductionPerMinute) / 6d);
        Assert.Equal(300, first.Window.EndGameTick - second.Window.StartGameTick + 1);
    }

    private static int[] PeriodicEvents(int period, int phase, int end) =>
        Enumerable.Range(0, end + 1).Where(tick => tick >= phase && (tick - phase) % period == 0).ToArray();

    private static long CountInWindow(int[] events, int end) =>
        events.LongCount(tick => tick >= end - 599 && tick <= end);
}
