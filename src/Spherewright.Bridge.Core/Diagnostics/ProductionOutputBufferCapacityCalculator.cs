namespace Spherewright.Bridge.Core.Diagnostics;

public static class ProductionOutputBufferCapacityCalculator
{
    public const int MinerOutputThreshold = 50;

    // Legacy capacity name: this is the first buffered count that rejects the
    // next native batch, not the maximum stock after adding a whole batch.
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
            if (productCountPerCycle > 100)
            {
                // Such a batch cannot fit even an empty native smelter buffer;
                // do not invent a positive threshold for unsupported input.
                throw new ArgumentOutOfRangeException(nameof(productCountPerCycle));
            }

            return 101 - productCountPerCycle;
        }

        return checked(productCountPerCycle * (isAssemblyRecipe ? 9 : 19) + 1);
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
