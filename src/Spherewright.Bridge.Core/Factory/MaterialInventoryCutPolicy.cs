using Spherewright.Contracts.Errors;

namespace Spherewright.Bridge.Core.Factory;

/// <summary>Read budgets only. No traversal, layout, writes or automatic selection.</summary>
public sealed class MaterialInventoryCutPolicy
{
    public const int MaximumSelectedObjects = 256;
    public const int MaximumPaths = 64;
    public const int MaximumTotalPathCells = 32768;
    public const int MaximumTotalPathBelts = 4096;
    private readonly Dictionary<int, (int Cells, int Belts)> _paths = new();
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
}
