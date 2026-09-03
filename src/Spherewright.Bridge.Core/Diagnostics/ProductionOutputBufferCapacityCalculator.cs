namespace Spherewright.Bridge.Core.Diagnostics;

public static class ProductionOutputBufferCapacityCalculator
{
    public const int MinerOutputThreshold = 50;

    public static int CalculateAssemblerCapacity(
        bool isSmeltingRecipe,
        bool isAssemblyRecipe,
        int productCountPerCycle)
    {
        if (productCountPerCycle <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productCountPerCycle));
        }

        if (isSmeltingRecipe)
        {
            return 100;
        }

        return checked(productCountPerCycle * (isAssemblyRecipe ? 10 : 20));
    }

    public static int CalculateMatrixLabCapacity(int speedOverride)
    {
        if (speedOverride <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(speedOverride));
        }

        var capacity = 10L * (((long)speedOverride + 9_999L) / 10_000L);
        return checked((int)capacity);
    }

    public static long CalculateCycleGameTicks(int timeSpend, int speed)
    {
        if (timeSpend <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeSpend));
        }

        if (speed <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(speed));
        }

        return Math.Max(1L, ((long)timeSpend + speed - 1L) / speed);
    }
}
