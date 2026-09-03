using Spherewright.Contracts.Diagnostics;

namespace Spherewright.Bridge.Core.Diagnostics;

public static class OverseerTheoreticalProductionCalculator
{
    public static double CalculateRecipeOutputPerMinute(
        int speed,
        int timeSpend,
        bool proliferated,
        bool productive,
        bool forceAccelerationMode,
        float productMultiplier,
        float accelerationMultiplier,
        int outputCount)
    {
        if (speed <= 0 || timeSpend <= 0 || outputCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(speed),
                "Recipe speed, time, and output count must be positive.");
        }

        ValidateMultiplier(productMultiplier, nameof(productMultiplier));
        ValidateMultiplier(accelerationMultiplier, nameof(accelerationMultiplier));

        var cyclesPerMinute = 3600f * speed / timeSpend;
        if (proliferated)
        {
            cyclesPerMinute *= productive && !forceAccelerationMode
                ? productMultiplier
                : accelerationMultiplier;
        }

        return ValidateRate(cyclesPerMinute * outputCount);
    }

    public static double CalculateMinerOutputPerMinute(
        int period,
        float miningSpeedScale,
        int speed,
        double sourceMultiplier)
    {
        if (period <= 0 || speed <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(period),
                "Miner period and speed must be positive.");
        }

        if (!IsFiniteNonNegative(miningSpeedScale)
            || !IsFiniteNonNegative(sourceMultiplier))
        {
            throw new ArgumentOutOfRangeException(
                nameof(sourceMultiplier),
                "Miner multipliers must be finite and non-negative.");
        }

        return ValidateRate((float)(3600d / period * miningSpeedScale * speed * sourceMultiplier));
    }

    public static int CalculateFractionatorStackMultiplier(
        bool fourStackTechnologyUnlocked,
        int inserterStackOutput,
        int stationPilerLevel)
    {
        if (inserterStackOutput < 0 || stationPilerLevel < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inserterStackOutput),
                "Runtime stack levels must be non-negative.");
        }

        var multiplier = fourStackTechnologyUnlocked ? 4 : 1;
        if (inserterStackOutput > multiplier)
        {
            multiplier = inserterStackOutput;
        }

        // This second comparison intentionally matches DSP 0.10.34.28529's
        // ProductionExtraInfoCalculator IL. It compares inserterStackOutput
        // again rather than stationPilerLevel before assigning the latter.
        if (inserterStackOutput > multiplier)
        {
            multiplier = stationPilerLevel;
        }

        return multiplier;
    }

    public static double CalculateFractionatorOutputPerMinute(
        bool proliferated,
        float accelerationMultiplier,
        float productionProbability,
        int stackMultiplier)
    {
        ValidateMultiplier(accelerationMultiplier, nameof(accelerationMultiplier));
        if (!IsFiniteNonNegative(productionProbability) || stackMultiplier <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(productionProbability),
                "Fractionator probability must be finite and non-negative and its stack multiplier must be positive.");
        }

        return ValidateRate(
            1800f
            * (proliferated ? accelerationMultiplier : 1f)
            * productionProbability
            * stackMultiplier);
    }

    public static double CalculateGammaOutputPerMinute(long capacityCurrentTick, long productHeat)
    {
        if (capacityCurrentTick < 0 || productHeat <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacityCurrentTick),
                "Gamma capacity must be non-negative and product heat must be positive.");
        }

        return ValidateRate(3600f * capacityCurrentTick / productHeat);
    }

    public static float CalculateCollectorSpeedFactor(
        float miningSpeedScale,
        double gasTotalHeat,
        double collectorsWorkCost)
    {
        if (!IsFiniteNonNegative(miningSpeedScale)
            || !IsFiniteNonNegative(gasTotalHeat)
            || !IsFiniteNonNegative(collectorsWorkCost))
        {
            throw new ArgumentOutOfRangeException(
                nameof(miningSpeedScale),
                "Collector inputs must be finite and non-negative.");
        }

        var denominator = gasTotalHeat - collectorsWorkCost;
        var factor = denominator <= 0d
            ? 1f
            : (float)((miningSpeedScale * gasTotalHeat - collectorsWorkCost) / denominator);
        if (!IsFiniteNonNegative(factor))
        {
            throw new ArgumentOutOfRangeException(
                nameof(miningSpeedScale),
                "The collector speed factor is not finite and non-negative.");
        }

        return factor;
    }

    public static double CalculateCollectorOutputPerMinute(
        float collectionPerTick,
        float collectorSpeedFactor)
    {
        if (!IsFiniteNonNegative(collectionPerTick)
            || !IsFiniteNonNegative(collectorSpeedFactor))
        {
            throw new ArgumentOutOfRangeException(
                nameof(collectionPerTick),
                "Collector rates must be finite and non-negative.");
        }

        return ValidateRate(3600f * collectionPerTick * collectorSpeedFactor);
    }

    public static double AddRates(double currentRate, double contribution)
    {
        if (!IsFiniteNonNegative(currentRate) || !IsFiniteNonNegative(contribution))
        {
            throw new ArgumentOutOfRangeException(
                nameof(contribution),
                "Theoretical rates must be finite and non-negative.");
        }

        return ValidateRate(currentRate + contribution);
    }

    public static double? CalculateUtilization(
        string windowState,
        double actualProductionPerMinute,
        double theoreticalProductionPerMinute)
    {
        if (!IsFiniteNonNegative(actualProductionPerMinute)
            || !IsFiniteNonNegative(theoreticalProductionPerMinute))
        {
            throw new ArgumentOutOfRangeException(
                nameof(actualProductionPerMinute),
                "Production rates must be finite and non-negative.");
        }

        if (!string.Equals(windowState, OverseerWindowStates.Ready, StringComparison.Ordinal)
            || theoreticalProductionPerMinute <= 0d)
        {
            return null;
        }

        return ValidateRate(actualProductionPerMinute / theoreticalProductionPerMinute);
    }

    private static void ValidateMultiplier(float value, string parameterName)
    {
        if (!IsFiniteNonNegative(value))
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static double ValidateRate(double value)
    {
        if (!IsFiniteNonNegative(value))
        {
            throw new OverflowException("The theoretical production rate exceeds finite numeric bounds.");
        }

        return value;
    }

    private static bool IsFiniteNonNegative(float value) =>
        !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;

    private static bool IsFiniteNonNegative(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d;
}
