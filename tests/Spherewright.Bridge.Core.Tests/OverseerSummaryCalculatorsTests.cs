using Spherewright.Bridge.Core.Diagnostics;
using Spherewright.Contracts.Power;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class OverseerSummaryCalculatorsTests
{
    [Fact]
    public void PowerSummary_AggregatesAllNetworksButBoundsDetails()
    {
        var result = OverseerPowerSummaryCalculator.Calculate(
            new[]
            {
                Network(3, consumers: 2, generators: 1, required: 30, served: 15, ratio: 0.5),
                Network(1, consumers: 1, generators: 2, required: 20, served: 20, ratio: 1.0),
            },
            maximumNetworkDetails: 1);

        Assert.Equal(2, result.ActiveNetworkCount);
        Assert.Equal(1, result.ReturnedNetworkCount);
        Assert.True(result.NetworkDetailsTruncated);
        Assert.Equal(3, result.ConsumerCount);
        Assert.Equal(3, result.GeneratorCount);
        Assert.Equal(50, result.TotalEnergyRequired);
        Assert.Equal(35, result.TotalEnergyServed);
        Assert.Equal(35, result.TotalEnergyGenerated);
        Assert.Equal(6, result.TotalEnergyExported);
        Assert.Equal(0.5, result.MinimumConsumerRatio);
        Assert.Equal(1, Assert.Single(result.Networks).NetworkId);
    }

    [Fact]
    public void PowerSummary_DuplicateIdentityFailsClosed()
    {
        Assert.Throws<ArgumentException>(() => OverseerPowerSummaryCalculator.Calculate(
            new[] { Network(1), Network(1) },
            maximumNetworkDetails: 4));
    }

    [Fact]
    public void PowerSummary_NegativeNativeCounterFailsClosed()
    {
        var network = Network(1);
        network.EnergyExported = -1;

        Assert.Throws<ArgumentException>(() => OverseerPowerSummaryCalculator.Calculate(
            new[] { network },
            maximumNetworkDetails: 4));
    }

    [Theory]
    [InlineData(36_000, 60, 600)]
    [InlineData(1, 60, 0)]
    [InlineData(0, 60, 0)]
    public void ResearchMath_UsesNativePointsPerItem(long hashes, int pointsPerHash, long expected)
    {
        Assert.Equal(expected, OverseerResearchMath.CalculateItemCount(hashes, pointsPerHash));
    }

    private static PowerNetworkSnapshot Network(
        int id,
        int consumers = 0,
        int generators = 0,
        long required = 0,
        long served = 0,
        double ratio = 1d)
    {
        return new PowerNetworkSnapshot
        {
            NetworkId = id,
            ConsumerCount = consumers,
            GeneratorCount = generators,
            EnergyRequired = required,
            EnergyServed = served,
            EnergyCapacity = served,
            EnergyGenerated = served,
            EnergyExported = id + 1,
            ConsumerRatio = ratio,
            GeneratorRatio = 1d,
        };
    }
}
