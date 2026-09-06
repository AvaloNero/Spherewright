using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Actions;
using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

// Explicit single-storage UI operations, never an inventory repair/auto-balancer.
public static class StorageConfigurationPolicy
{
    public static StorageUiState Project(StorageUiState before, string operation,
        int filterItemId, int bannedGridCount, IReadOnlyDictionary<int, int> stackSizes)
    {
        Validate(before, stackSizes);
        if (operation != StorageConfigurationOperations.SetBans
            && operation != StorageConfigurationOperations.LockOccupied
            && operation != StorageConfigurationOperations.FilterEmptyOrMatching
            && operation != StorageConfigurationOperations.ClearFilters)
            throw new ArgumentException("storage_operation_unsupported");
        var setsFilter = operation == StorageConfigurationOperations.FilterEmptyOrMatching;
        if (setsFilter ? !KnownItem(filterItemId, stackSizes) : filterItemId != 0)
            throw new ArgumentException("storage_filter_invalid");
        if (operation == StorageConfigurationOperations.SetBans
            ? bannedGridCount < 0 || bannedGridCount > before.Grids.Count
            : bannedGridCount != -1)
            throw new ArgumentException("storage_bans_invalid");

        var mode = operation == StorageConfigurationOperations.SetBans ? before.Mode
            : operation == StorageConfigurationOperations.ClearFilters ? "default" : "filtered";
        var grids = new List<StorageUiGrid>();
        foreach (var grid in before.Grids)
        {
            var touched = operation == StorageConfigurationOperations.ClearFilters
                || (operation == StorageConfigurationOperations.LockOccupied && grid.ItemId > 0)
                || (setsFilter && (grid.Count == 0 || grid.ItemId == filterItemId));
            if (!touched) { grids.Add(grid); continue; }
            var filter = grid.Filter;
            if (operation == StorageConfigurationOperations.LockOccupied && grid.ItemId > 0)
                filter = grid.ItemId;
            if (setsFilter && (grid.Count == 0 || grid.ItemId == filterItemId))
                filter = filterItemId;
            if (operation == StorageConfigurationOperations.ClearFilters) filter = 0;
            // SetFilter(0) clears empty item/stack metadata; a nonzero filter
            // reserves even an empty grid. No count or inc is created/deleted.
            var item = grid.Count > 0 ? grid.ItemId : filter;
            var stack = item > 0 ? stackSizes[item] : 0;
            grids.Add(new StorageUiGrid(item, grid.Count, grid.Inc, filter, stack));
        }
        var after = new StorageUiState(mode,
            operation == StorageConfigurationOperations.SetBans ? bannedGridCount : before.Bans, grids);
        Validate(after, stackSizes);
        if (Fingerprint(before) == Fingerprint(after)) throw new ArgumentException("storage_configuration_unchanged");
        return after;
    }

    public static void Validate(StorageUiState state, IReadOnlyDictionary<int, int> stackSizes)
    {
        if ((state.Mode != "default" && state.Mode != "filtered")
            || state.Grids.Count < 1 || state.Grids.Count > BlueprintStoragePolicy.MaximumGridCount
            || state.Bans < 0 || state.Bans > state.Grids.Count)
            throw new ArgumentException("storage_state_unsupported");
        foreach (var grid in state.Grids)
        {
            if (grid.ItemId < 0 || grid.Filter < 0 || grid.Count < 0 || grid.Inc < 0
                || (grid.Count == 0 && grid.Inc != 0)
                || (grid.ItemId == 0 && (grid.Count != 0 || grid.Filter != 0 || grid.StackSize != 0))
                || (grid.ItemId > 0 && (!KnownItem(grid.ItemId, stackSizes)
                    || grid.StackSize != stackSizes[grid.ItemId] || grid.Count > grid.StackSize))
                || (grid.Filter > 0 && (grid.Filter != grid.ItemId || !KnownItem(grid.Filter, stackSizes)))
                || (state.Mode == "default" && grid.Filter != 0))
                throw new ArgumentException("storage_grid_invalid");
        }
    }

    private static bool KnownItem(int itemId, IReadOnlyDictionary<int, int> sizes) =>
        itemId > 0 && itemId < 12000 && sizes.TryGetValue(itemId, out var size) && size > 0;

    public static string Fingerprint(StorageUiState state)
    {
        var fields = new List<object?> { state.Mode, state.Bans, state.Grids.Count };
        for (var i = 0; i < state.Grids.Count; i++)
        {
            var grid = state.Grids[i];
            fields.Add(i); fields.Add(grid.ItemId); fields.Add(grid.Count); fields.Add(grid.Inc);
            fields.Add(grid.Filter); fields.Add(grid.StackSize);
        }
        return CanonicalStateHash.Combine("native-storage-ui-v1", fields.ToArray());
    }

    public static StorageConfigurationSnapshot ToSnapshot(StorageUiState state) => new StorageConfigurationSnapshot
    {
        GridCount = state.Grids.Count, BannedGridCount = state.Bans, Mode = state.Mode,
        GridFilterItemIds = state.Grids.Select(grid => grid.Filter).ToList(),
    };

    // This verifies the pure ordered-grid projection; the Plugin must additionally prove
    // native identity, unchanged connections and player invariants BEFORE exposing this evidence.
    public static StorageConfigurationReadback CreateReadback(int entityId, long gameTick,
        StorageUiState before, StorageUiState actualAfter, string operation,
        int filterItemId, int bannedGridCount, IReadOnlyDictionary<int, int> stackSizes,
        int verifiedConnectionCount)
    {
        if (entityId <= 0 || gameTick < 0 || verifiedConnectionCount < 0 || verifiedConnectionCount > 16)
            throw new ArgumentException("storage_readback_identity_invalid");
        var expected = Project(before, operation, filterItemId, bannedGridCount, stackSizes);
        Validate(actualAfter, stackSizes);
        if (Fingerprint(actualAfter) != Fingerprint(expected))
            throw new ArgumentException("storage_readback_projection_mismatch");
        return new StorageConfigurationReadback
        {
            CapturedAtGameTick = gameTick, EntityId = entityId, Operation = operation,
            ConfigurationBefore = ToSnapshot(before), ConfigurationAfter = ToSnapshot(actualAfter),
            BuffersBefore = ReadbackBuffers(before), BuffersAfter = ReadbackBuffers(actualAfter),
            VerifiedConnectionCount = verifiedConnectionCount,
        };
    }

    private static List<FactoryBufferSnapshot> ReadbackBuffers(StorageUiState state) => state.Grids
        .Where(grid => grid.Count > 0).Select(grid => new FactoryBufferSnapshot
        {
            Role = "storage", ItemId = grid.ItemId, Count = grid.Count, Inc = grid.Inc,
        }).ToList();
}

public sealed class StorageUiState
{
    public StorageUiState(string mode, int bans, IEnumerable<StorageUiGrid> grids)
    {
        Mode = mode; Bans = bans;
        var copy = grids.Take(BlueprintStoragePolicy.MaximumGridCount + 1).ToArray();
        if (copy.Length > BlueprintStoragePolicy.MaximumGridCount || copy.Any(grid => grid is null))
            throw new ArgumentException("storage_state_unsupported");
        Grids = Array.AsReadOnly(copy);
    }
    public string Mode { get; }
    public int Bans { get; }
    public IReadOnlyList<StorageUiGrid> Grids { get; }
}

public sealed class StorageUiGrid
{
    public StorageUiGrid(int itemId, int count, int inc, int filter, int stackSize)
    {
        ItemId = itemId; Count = count; Inc = inc; Filter = filter; StackSize = stackSize;
    }
    public int ItemId { get; }
    public int Count { get; }
    public int Inc { get; }
    public int Filter { get; }
    public int StackSize { get; }
}
