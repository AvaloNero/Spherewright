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
}
