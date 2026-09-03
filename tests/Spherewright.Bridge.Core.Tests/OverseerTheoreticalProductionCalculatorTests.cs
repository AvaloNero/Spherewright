using Spherewright.Bridge.Core.Diagnostics;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class OverseerTheoreticalProductionCalculatorTests
{
    [Fact]
    public void RecipeOutput_UsesProductiveOrAccelerationBranchFromRuntimeFormula()
    {
        var productive = OverseerTheoreticalProductionCalculator.CalculateRecipeOutputPerMinute(
            speed: 10_000,
            timeSpend: 600_000,
            proliferated: true,
            productive: true,
            forceAccelerationMode: false,
            productMultiplier: 1.25f,
            accelerationMultiplier: 2f,
            outputCount: 2);
        var accelerated = OverseerTheoreticalProductionCalculator.CalculateRecipeOutputPerMinute(
            speed: 10_000,
            timeSpend: 600_000,
            proliferated: true,
            productive: true,
            forceAccelerationMode: true,
            productMultiplier: 1.25f,
            accelerationMultiplier: 2f,
            outputCount: 2);

        Assert.Equal(150d, productive);
        Assert.Equal(240d, accelerated);
    }

    [Fact]
    public void MinerOutput_MultipliesPeriodSpeedUpgradeAndCoveredSources()
    {
        var result = OverseerTheoreticalProductionCalculator.CalculateMinerOutputPerMinute(
            period: 600_000,
            miningSpeedScale: 1.5f,
            speed: 10_000,
            sourceMultiplier: 6d);

        Assert.Equal(540d, result);
    }

    [Fact]
    public void MinerOutput_AllowsADepletedZeroSourceSet()
    {
        var result = OverseerTheoreticalProductionCalculator.CalculateMinerOutputPerMinute(
            period: 600_000,
            miningSpeedScale: 1f,
            speed: 10_000,
            sourceMultiplier: 0d);

        Assert.Equal(0d, result);
    }

    [Theory]
    [InlineData(false, 1, 4, 1)]
    [InlineData(false, 2, 4, 2)]
    [InlineData(true, 1, 8, 4)]
    public void FractionatorStackMultiplier_ReproducesCurrentAssemblyBranch(
        bool fourStackUnlocked,
        int inserterStackOutput,
        int stationPilerLevel,
        int expected)
    {
        Assert.Equal(
            expected,
            OverseerTheoreticalProductionCalculator.CalculateFractionatorStackMultiplier(
                fourStackUnlocked,
                inserterStackOutput,
                stationPilerLevel));
    }

    [Fact]
    public void FractionatorOutput_UsesAccelerationProbabilityAndStack()
    {
        var result = OverseerTheoreticalProductionCalculator.CalculateFractionatorOutputPerMinute(
            proliferated: true,
            accelerationMultiplier: 2f,
            productionProbability: 0.01f,
            stackMultiplier: 4);

        Assert.Equal(144d, result, precision: 5);
    }

    [Fact]
    public void GammaAndCollectorOutput_UseCurrentTickCapacityFormulas()
    {
        var gamma = OverseerTheoreticalProductionCalculator.CalculateGammaOutputPerMinute(
            capacityCurrentTick: 1_000,
            productHeat: 10_000);
        var collectorFactor = OverseerTheoreticalProductionCalculator.CalculateCollectorSpeedFactor(
            miningSpeedScale: 1.5f,
            gasTotalHeat: 100d,
            collectorsWorkCost: 20d);
        var collector = OverseerTheoreticalProductionCalculator.CalculateCollectorOutputPerMinute(
            collectionPerTick: 0.25f,
            collectorSpeedFactor: collectorFactor);

        Assert.Equal(360d, gamma);
        Assert.Equal(1.625f, collectorFactor);
        Assert.Equal(1462.5d, collector);
    }

    [Fact]
    public void NonFiniteContributionFailsClosed()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OverseerTheoreticalProductionCalculator.AddRates(1d, double.NaN));
        Assert.Throws<OverflowException>(() =>
            OverseerTheoreticalProductionCalculator.AddRates(double.MaxValue, double.MaxValue));
    }

    [Fact]
    public void Utilization_RequiresReadyWindowAndPositiveCapacity()
    {
        Assert.Equal(
            0.5d,
            OverseerTheoreticalProductionCalculator.CalculateUtilization("ready", 60d, 120d));
        Assert.Null(OverseerTheoreticalProductionCalculator.CalculateUtilization("warming_up", 60d, 120d));
        Assert.Null(OverseerTheoreticalProductionCalculator.CalculateUtilization("ready", 0d, 0d));
    }
}
