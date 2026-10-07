using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

/// <summary>Read budgets only. No traversal, layout, writes or automatic selection.</summary>
public sealed class MaterialInventoryCutPolicy
{
    public const int MaximumSelectedObjects = 256;
    public const int MaximumPaths = 64;
    public const int MaximumTotalPathCells = 32768;
    public const int MaximumTotalPathBelts = 4096;
    private readonly Dictionary<int, (int Cells, int Belts)> _paths = new();
    private readonly HashSet<int> _cargoIds = new();
    private int _cells;
    private int _belts;

    public static BridgeError? ValidateSelection(IReadOnlyList<int>? ids)
    {
        if (ids is null || ids.Count == 0) return null;
        return ids.Count <= MaximumSelectedObjects && ids.All(id => id > 0)
            && ids.Distinct().Count() == ids.Count ? null
            : BridgeError.Create(BridgeErrorCodes.InvalidRequest,
                "materialInventoryObjectIds requires at most256 unique positive built-object IDs.",
                false, "Supply an explicit bounded owned-world selection; do not infer missing inventory as zero.");
    }

    // Native identity checks precede this unit/coverage check in the adapter.
    // Station orders and loaded fuel heat are never converted into item stock.
    public static bool HasValidStockUnits(FactoryEntitySnapshot entity)
    {
        if (entity.Buffers is null) return false;
        if (entity.ComponentKind == "station")
        {
            var station = entity.LogisticsStation;
            return entity.Buffers.Count == 0 && station is not null
                && station.SessionId == entity.SessionId && station.PlanetId == entity.PlanetId
                && station.EntityId == entity.ObjectId && station.CapturedAtGameTick == entity.CapturedAtGameTick
                && station.StorageSlots is not null && station.StorageSlots.Count is > 0 and <= 64
                && station.StorageSlots.Select((slot, index) => slot is not null && slot.Index == index
                    && slot.ItemId >= 0 && slot.Count >= 0 && slot.Inc >= 0
                    && (slot.ItemId > 0 || (slot.Count == 0 && slot.Inc == 0))).All(valid => valid);
        }
        if (entity.ComponentKind == "power-generator")
        {
            var fuel = entity.FuelPowerState;
            if (entity.ItemId is not (2204 or 2211) || fuel?.State != "observed"
                || fuel.BufferedFuelItemId is null || fuel.BufferedFuelCount is null || fuel.BufferedFuelInc is null
                || fuel.BufferedFuelHeatPerItemJoules is null || fuel.LoadedFuelItemId is null
                || fuel.LoadedFuelEnergyJoules is null || fuel.LoadedFuelIncLevel is null
                || fuel.LoadedFuelProductive is null || fuel.RatedGenerationEnergyPerTick is null
                || fuel.RatedFuelEnergyUsePerTick is null || fuel.CapacityEnergyPerTick is null
                || fuel.GeneratedEnergyPerTick is null) return false;
            var checkedFuel = FuelPowerStatePolicy.Capture(entity.ItemId, entity.ItemId == 2204 ? 1 : 2,
                fuel.BufferedFuelItemId.Value, fuel.BufferedFuelCount.Value, fuel.BufferedFuelInc.Value,
                fuel.BufferedFuelHeatPerItemJoules.Value, fuel.LoadedFuelItemId.Value, fuel.LoadedFuelEnergyJoules.Value,
                fuel.LoadedFuelIncLevel.Value, fuel.LoadedFuelProductive.Value,
                fuel.RatedGenerationEnergyPerTick.Value, fuel.RatedFuelEnergyUsePerTick.Value,
                fuel.CapacityEnergyPerTick.Value, fuel.GeneratedEnergyPerTick.Value);
            return checkedFuel.State == "observed" && entity.Buffers.Count == 1
                && entity.Buffers[0].Role == "power-generation-current-tick"
                && entity.Buffers[0].CountUnit == "joules_per_tick" && entity.Buffers[0].UnitsPerItem == 0
                && entity.Buffers[0].Count == fuel.GeneratedEnergyPerTick
                && entity.Buffers[0].ItemId == fuel.LoadedFuelItemId;
        }
        return entity.ComponentKind is "belt" or "storage" or "tank" or "inserter" or "assembler" or "miner"
            && entity.Buffers.All(buffer => buffer is not null && buffer.ItemId > 0
            && buffer.Count >= 0 && buffer.Inc >= 0 && buffer.CountUnit == "items" && buffer.UnitsPerItem == 1);
    }

    public bool TryReservePath(int id, int cells, int belts, out bool alreadyReserved)
    {
        alreadyReserved = _paths.ContainsKey(id);
        if (id <= 0 || cells < 1 || cells > BeltUpgradePathPolicy.MaximumPathCells
            || belts < 1 || belts > BeltUpgradePathPolicy.MaximumBelts) return false;
        if (alreadyReserved) return _paths[id] == (cells, belts);
        if (_paths.Count >= MaximumPaths || (long)_cells + cells > MaximumTotalPathCells
            || (long)_belts + belts > MaximumTotalPathBelts) return false;
        _paths.Add(id, (cells, belts));
        _cells += cells;
        _belts += belts;
        return true;
    }

    public bool TryReserveCargo(IReadOnlyList<BeltCargoReference>? references)
    {
        if (references is null || references.Count > BeltUpgradePathPolicy.MaximumPathCells / BeltCargoObservationPolicy.CargoCellLength + 1) return false;
        var ids = new HashSet<int>();
        foreach (var reference in references)
            if (reference is null || reference.CargoId < 0 || !ids.Add(reference.CargoId)) return false;
        // A native cargo handle cannot be counted on two distinct paths in one cut.
        // Reject before mutating the set; a failed reservation supplies no counts.
        if (_cargoIds.Overlaps(ids)) return false;
        _cargoIds.UnionWith(ids);
        return true;
    }
}
