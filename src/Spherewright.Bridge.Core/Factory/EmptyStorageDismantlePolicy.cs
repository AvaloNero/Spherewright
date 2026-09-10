using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

// Recovery of a misplaced, unused warehouse; not a general storage-delete policy.
public static class EmptyStorageDismantlePolicy
{
    public const int ItemId = 2101;
    public const int GridCount = 30;
    public const int MaximumPoolEntries = 8192;

    public static bool SupportsSnapshot(FactoryEntitySnapshot? target) => target is not null
        && target.ObjectKind == FactoryObjectKinds.Entity && target.ObjectId > 0
        && target.ItemId == ItemId && target.ComponentKind == "storage" && target.RecipeId == 0
        && target.Connections is not null && target.Connections.Count == 0
        && target.Buffers is not null && target.Buffers.Count == 0
        && target.StorageConfiguration is { } config && config.GridCount == GridCount
        && config.BannedGridCount == 0 && config.Mode == "default"
        && config.GridFilterItemIds is not null && config.GridFilterItemIds.Count == GridCount
        && config.GridFilterItemIds.All(id => id == 0);

    public static bool EmptyDefaultContents(StorageUiState? state) => state is not null
        && state.Mode == "default" && state.Bans == 0 && state.Grids.Count == GridCount
        && state.Grids.All(g => g.ItemId == 0 && g.Count == 0 && g.Inc == 0
            && g.Filter == 0 && g.StackSize == 0);

    // InitConn gives an isolated warehouse self-references for BOTH bottom and top.
    // Null/zero bottom or top is missing evidence, not proof that there is no stack.
    public static bool SingleLayer(int id, int previous, int next, int bottom, int top,
        bool hasPreviousReference, bool hasNextReference, bool bottomIsSelf, bool topIsSelf) =>
        id > 0 && previous == 0 && next == 0 && bottom == id && top == id
        && !hasPreviousReference && !hasNextReference && bottomIsSelf && topIsSelf;

    public static bool BoundedPool(int cursor, int length) =>
        cursor >= 1 && cursor <= MaximumPoolEntries && length >= cursor;

    public static bool ReferenceIsSafe(int targetEntityId, int signedOwnerId, int otherObjectId) =>
        targetEntityId > 0 && signedOwnerId != 0 && otherObjectId != targetEntityId
        && (signedOwnerId != targetEntityId || otherObjectId == 0);

    public static string Fingerprint(FactoryEntitySnapshot target, StorageUiState contents,
        string nativeIdentityHash) => CanonicalStateHash.Combine("empty-storage-dismantle-v1",
            CanonicalStateHash.FactoryEndpoint(target), StorageConfigurationPolicy.Fingerprint(contents), nativeIdentityHash);
}
