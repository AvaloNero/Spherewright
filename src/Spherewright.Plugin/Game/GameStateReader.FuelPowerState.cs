using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;

namespace Spherewright.Plugin.Game;

internal sealed partial class GameStateReader
{
    private static FuelPowerStateSnapshot CaptureFuelPowerState(PlanetFactory factory, int entityId)
    {
        if (entityId <= 0 || entityId >= factory.entityCursor || entityId >= factory.entityPool.Length)
            return FuelPowerStatePolicy.Unavailable("invalid_generator_entity");
        ref var entity = ref factory.entityPool[entityId];
        var system = factory.powerSystem;
        var id = entity.powerGenId;
        if (entity.id != entityId || system?.genPool is null || id <= 0
            || id >= system.genCursor || id >= system.genPool.Length)
            return FuelPowerStatePolicy.Unavailable("generator_component_unavailable");
        ref var generator = ref system.genPool[id];
        if (generator.id != id || generator.entityId != entityId
            || generator.photovoltaic || generator.wind || generator.gamma || generator.geothermal)
            return FuelPowerStatePolicy.Unavailable("generator_identity_or_kind_mismatch");
        // Both buffered and loaded identities can differ during a fuel switch.
        // Do not read LDB heat for curFuelId as a replacement for fuelEnergy.
        if ((generator.fuelId > 0 && LDB.items.Select(generator.fuelId) is null)
            || (generator.curFuelId > 0 && LDB.items.Select(generator.curFuelId) is null))
            return FuelPowerStatePolicy.Unavailable("native_fuel_item_unavailable");
        return FuelPowerStatePolicy.Capture(entity.protoId, generator.fuelMask,
            generator.fuelId, generator.fuelCount, generator.fuelInc, generator.fuelHeat,
            generator.curFuelId, generator.fuelEnergy, generator.fuelIncLevel, generator.productive,
            generator.genEnergyPerTick, generator.useFuelPerTick,
            generator.capacityCurrentTick, generator.generateCurrentTick);
    }
}
