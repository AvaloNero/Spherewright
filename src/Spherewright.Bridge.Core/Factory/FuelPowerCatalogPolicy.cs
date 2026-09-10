using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

public static class FuelPowerCatalogPolicy
{
    private const long MaximumEnergyPerTick = 1000000000000L;

    // Copy validated scalars, never run a generator or infer live fuel availability.
    public static FuelPowerCatalogProfile? Capture(int itemId, bool isOrdinaryFuelGenerator,
        long generationEnergyPerTick, long fuelEnergyUsePerTick, int fuelTypeMask)
    {
        if ((itemId != 2204 && itemId != 2211) || !isOrdinaryFuelGenerator
            || generationEnergyPerTick <= 0 || generationEnergyPerTick > MaximumEnergyPerTick
            || fuelEnergyUsePerTick <= 0 || fuelEnergyUsePerTick > MaximumEnergyPerTick
            || (fuelTypeMask != 1 && fuelTypeMask != 2))
            return null;

        return new FuelPowerCatalogProfile
        {
            GenerationEnergyPerTick = generationEnergyPerTick,
            FuelEnergyUsePerTick = fuelEnergyUsePerTick,
            FuelTypeMask = fuelTypeMask,
        };
    }
}
