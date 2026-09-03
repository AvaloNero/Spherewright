using Spherewright.Bridge.Core.Diagnostics;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class ProductionOutputBufferCapacityCalculatorTests
{
    [Theory]
    [InlineData(true, false, 1, 100)]
    [InlineData(false, true, 2, 20)]
    [InlineData(false, false, 3, 60)]
    public void CalculateAssemblerCapacity_MatchesCurrentRuntimeGates(
        bool isSmelting,
        bool isAssembly,
        int perCycle,
        int expected)
    {
        Assert.Equal(
            expected,
            ProductionOutputBufferCapacityCalculator.CalculateAssemblerCapacity(
                isSmelting,
                isAssembly,
                perCycle));
    }

    [Theory]
    [InlineData(10_000, 10)]
    [InlineData(10_001, 20)]
    [InlineData(20_000, 20)]
    public void CalculateMatrixLabCapacity_UsesCurrentSpeedOverrideCeiling(int speed, int expected)
    {
        Assert.Equal(expected, ProductionOutputBufferCapacityCalculator.CalculateMatrixLabCapacity(speed));
    }

    [Theory]
    [InlineData(600_000, 10_000, 60)]
    [InlineData(600_001, 10_000, 61)]
    public void CalculateCycleGameTicks_RoundsUp(int timeSpend, int speed, long expected)
    {
        Assert.Equal(expected, ProductionOutputBufferCapacityCalculator.CalculateCycleGameTicks(timeSpend, speed));
    }

    [Fact]
    public void CapacityCalculators_RejectInvalidOrOverflowingRuntimeValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ProductionOutputBufferCapacityCalculator.CalculateAssemblerCapacity(false, true, 0));
        Assert.Throws<OverflowException>(() =>
            ProductionOutputBufferCapacityCalculator.CalculateAssemblerCapacity(false, false, int.MaxValue));
        Assert.Equal(
            2_147_490,
            ProductionOutputBufferCapacityCalculator.CalculateMatrixLabCapacity(int.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ProductionOutputBufferCapacityCalculator.CalculateCycleGameTicks(1, 0));
    }
}
