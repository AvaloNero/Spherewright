using Spherewright.Contracts.Diagnostics;
using Spherewright.Contracts.Power;

namespace Spherewright.Bridge.Core.Diagnostics;

public static class OverseerPowerSummaryCalculator
{
    public static OverseerPowerSummarySnapshot Calculate(
        IReadOnlyList<PowerNetworkSnapshot> networks,
        int maximumNetworkDetails)
    {
        if (networks is null)
        {
            throw new ArgumentNullException(nameof(networks));
        }

        if (maximumNetworkDetails <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumNetworkDetails));
        }

        var ordered = networks.OrderBy(network => network.NetworkId).ToArray();
        if (ordered.Any(network =>
                network.NetworkId <= 0
                || network.NodeCount < 0
                || network.ConsumerCount < 0
                || network.GeneratorCount < 0
                || network.AccumulatorCount < 0
                || network.ExchangerCount < 0
                || network.EnergyRequired < 0
                || network.EnergyServed < 0
                || network.EnergyCapacity < 0
                || network.EnergyGenerated < 0
                || network.EnergyExported < 0
                || network.EnergyStored < 0
                || !IsFiniteNonNegative(network.ConsumerRatio)
                || !IsFiniteNonNegative(network.GeneratorRatio))
            || ordered.Select(network => network.NetworkId).Distinct().Count() != ordered.Length)
        {
            throw new ArgumentException(
                "Power networks must have unique positive identities and non-negative finite counters.",
                nameof(networks));
        }

        var result = new OverseerPowerSummarySnapshot
        {
            ActiveNetworkCount = ordered.Length,
            ReturnedNetworkCount = Math.Min(ordered.Length, maximumNetworkDetails),
            NetworkDetailsTruncated = ordered.Length > maximumNetworkDetails,
            Networks = ordered.Take(maximumNetworkDetails).ToList(),
        };
        foreach (var network in ordered)
        {
            checked
            {
                result.ConsumerCount += network.ConsumerCount;
                result.GeneratorCount += network.GeneratorCount;
                result.AccumulatorCount += network.AccumulatorCount;
                result.ExchangerCount += network.ExchangerCount;
                result.TotalEnergyRequired += network.EnergyRequired;
                result.TotalEnergyServed += network.EnergyServed;
                result.TotalEnergyCapacity += network.EnergyCapacity;
                result.TotalEnergyGenerated += network.EnergyGenerated;
                result.TotalEnergyExported += network.EnergyExported;
                result.TotalEnergyStored += network.EnergyStored;
            }

            if (network.ConsumerCount > 0
                && (!result.MinimumConsumerRatio.HasValue
                    || network.ConsumerRatio < result.MinimumConsumerRatio.Value))
            {
                result.MinimumConsumerRatio = network.ConsumerRatio;
            }
        }

        return result;
    }

    private static bool IsFiniteNonNegative(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d;
}

public static class OverseerResearchMath
{
    public const int PointsPerItem = 3600;

    public static long CalculateItemCount(long hashCount, int pointsPerHash)
    {
        if (hashCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hashCount));
        }

        if (pointsPerHash < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pointsPerHash));
        }

        var result = decimal.Truncate((decimal)hashCount * pointsPerHash / PointsPerItem);
        if (result > long.MaxValue)
        {
            throw new OverflowException("The technology item requirement exceeds Int64 capacity.");
        }

        return (long)result;
    }
}
