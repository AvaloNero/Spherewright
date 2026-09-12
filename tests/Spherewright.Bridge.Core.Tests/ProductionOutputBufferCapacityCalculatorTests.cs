using Spherewright.Bridge.Core.Diagnostics;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class ProductionOutputBufferCapacityCalculatorTests
{
    [Theory]
    [InlineData(true, false, 1, 100)]
    [InlineData(true, false, 2, 99)]
    [InlineData(true, false, 100, 1)]
    [InlineData(false, true, 1, 10)]
    [InlineData(false, true, 2, 19)]
    [InlineData(false, true, 3, 28)]
    [InlineData(false, false, 1, 20)]
    [InlineData(false, false, 2, 39)]
    [InlineData(false, false, 3, 58)]
    public void CalculateAssemblerCapacity_ReturnsFirstNativeRejectedOutputCount(
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
    [InlineData(true, false, 1)]
    [InlineData(true, false, 2)]
    [InlineData(true, false, 100)]
    [InlineData(false, true, 1)]
    [InlineData(false, true, 2)]
    [InlineData(false, true, 3)]
    [InlineData(false, false, 1)]
    [InlineData(false, false, 2)]
    [InlineData(false, false, 3)]
    public void CalculateAssemblerCapacity_MatchesNativeStrictComparisonAtEveryBufferCount(
        bool isSmelting, bool isAssembly, int perCycle)
    {
        var threshold = ProductionOutputBufferCapacityCalculator.CalculateAssemblerCapacity(
            isSmelting, isAssembly, perCycle);

        for (var buffered = 0; buffered <= 120; buffered++)
        {
            var nativeRejects = isSmelting
                ? buffered + perCycle > 100
                : buffered > perCycle * (isAssembly ? 9 : 19);
            Assert.Equal(nativeRejects, buffered >= threshold);
        }
    }

    [Theory]
    [InlineData(true, 9)]
    [InlineData(false, 19)]
    public void CalculateAssemblerCapacity_PreservesCheckedThresholdBoundary(bool isAssembly, int multiplier)
    {
        var largestBatch = (int.MaxValue - 1) / multiplier;
        Assert.Equal(checked(largestBatch * multiplier + 1),
            ProductionOutputBufferCapacityCalculator.CalculateAssemblerCapacity(false, isAssembly, largestBatch));
        Assert.Throws<OverflowException>(() =>
            ProductionOutputBufferCapacityCalculator.CalculateAssemblerCapacity(false, isAssembly, largestBatch + 1));
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
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ProductionOutputBufferCapacityCalculator.CalculateAssemblerCapacity(true, false, 101));
        Assert.Throws<OverflowException>(() =>
            ProductionOutputBufferCapacityCalculator.CalculateAssemblerCapacity(false, false, int.MaxValue));
        Assert.Equal(
            2_147_490,
            ProductionOutputBufferCapacityCalculator.CalculateMatrixLabCapacity(int.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ProductionOutputBufferCapacityCalculator.CalculateCycleGameTicks(1, 0));
    }
}
