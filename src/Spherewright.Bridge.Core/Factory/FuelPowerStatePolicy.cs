using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

public static class FuelPowerStatePolicy
{
    public static FuelPowerStateSnapshot Unavailable(string reason) => new() { ReasonCode = reason };

    // Native scalar copies only. Never call EnergyCap_Fuel/GenEnergyByFuel,
    // derive fuel counts from generation, or treat a point read as burn/flow.
    public static FuelPowerStateSnapshot Capture(int buildingItemId, int fuelMask,
        int bufferedItemId, int bufferedCount, int bufferedInc, long bufferedHeat,
        int loadedItemId, long loadedEnergy, int loadedIncLevel, bool productive,
        long ratedGeneration, long ratedUse, long capacity, long generation)
    {
        if (!((buildingItemId == 2204 && fuelMask == 1) || (buildingItemId == 2211 && fuelMask == 2)))
            return Unavailable("unsupported_fuel_generator");
        if (FuelPowerCatalogPolicy.Capture(buildingItemId, true, ratedGeneration, ratedUse, fuelMask) is null
            || bufferedItemId < 0 || bufferedCount < 0 || bufferedCount > short.MaxValue
            || bufferedInc < 0 || bufferedInc > short.MaxValue || bufferedHeat < 0
            || loadedItemId < 0 || loadedEnergy < 0 || loadedIncLevel < 0 || loadedIncLevel > 10
            || capacity < 0 || generation < 0
            || (bufferedCount > 0 && (bufferedItemId == 0 || bufferedHeat == 0))
            || (loadedEnergy > 0 && loadedItemId == 0))
            return Unavailable("invalid_native_fuel_state");

        return new FuelPowerStateSnapshot
        {
            State = "observed", BufferedFuelItemId = bufferedItemId,
            BufferedFuelCount = bufferedCount, BufferedFuelInc = bufferedInc,
            BufferedFuelHeatPerItemJoules = bufferedHeat, LoadedFuelItemId = loadedItemId,
            LoadedFuelEnergyJoules = loadedEnergy, LoadedFuelIncLevel = loadedIncLevel,
            LoadedFuelProductive = productive, RatedGenerationEnergyPerTick = ratedGeneration,
            RatedFuelEnergyUsePerTick = ratedUse, CapacityEnergyPerTick = capacity,
            GeneratedEnergyPerTick = generation,
        };
    }
}
