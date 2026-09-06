using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

/// <summary>Fail-closed first native upgrade slice; this is not a generic proto replacement.</summary>
public static class BuildingUpgradePolicy
{
    public static bool SupportsItem(int itemId) => IsAssembler(itemId) || itemId == 2011 || itemId == 2012;

    public static bool IsAssembler(int itemId) => itemId == 2303 || itemId == 2304 || itemId == 2305;

    public static bool SupportsPair(int source, int target) =>
        (IsAssembler(source) && IsAssembler(target)) || (source == 2011 && target == 2012);

    public static string? Validate(int sourceItemId, int targetItemId, bool nativeSameFamily,
        int sourceGrade, int targetGrade, bool unlocked, int availableCount, int emptyPackageSlots)
    {
        if (!SupportsPair(sourceItemId, targetItemId) || !nativeSameFamily
            || sourceItemId == targetItemId || sourceGrade < 1 || targetGrade <= sourceGrade)
            return BridgeErrorCodes.InvalidRequest;
        if (!unlocked) return BridgeErrorCodes.TechnologyLocked;
        if (availableCount < 1) return BridgeErrorCodes.InventoryInsufficient;
        // DoUpgradeObject may throw a refund into trash if no room exists. Reserve conservatively.
        if (emptyPackageSlots < 1) return BridgeErrorCodes.InventoryFull;
        return null;
    }

    // Dynamic buffers/progress are deliberately not part of the plan binding: production may run
    // between prepare and commit. Live cargo is compared immediately around the synchronous call.
    public static string BindingHash(FactoryEntitySnapshot entity, bool forceAcceleration) =>
        CanonicalStateHash.Combine("upgrade-binding-v2", entity.EndpointStateHash, entity.RecipeId,
            entity.FilterItemId ?? 0, entity.PickTargetObjectId ?? 0, entity.InsertTargetObjectId ?? 0,
            forceAcceleration);

    // Cached inserter targets can survive a previously cleared factory connection. Checking only
    // the remaining connection list proves its entries, not that BOTH required ends still exist.
    public static bool HasCompleteInserterConnections(FactoryEntitySnapshot entity)
    {
        if (entity.ComponentKind != "inserter" || entity.ObjectId <= 0
            || entity.PickTargetObjectId is not > 0 || entity.InsertTargetObjectId is not > 0
            || entity.PickTargetObjectId == entity.ObjectId || entity.InsertTargetObjectId == entity.ObjectId
            || entity.Connections.Count != 2) return false;
        return entity.Connections.Count(c => c.Slot == 1 && !c.IsOutput
                && c.OtherObjectId == entity.PickTargetObjectId && c.OtherSlot >= 0 && c.OtherSlot < 16) == 1
            && entity.Connections.Count(c => c.Slot == 0 && c.IsOutput
                && c.OtherObjectId == entity.InsertTargetObjectId && c.OtherSlot >= 0 && c.OtherSlot < 16) == 1;
    }

    // Exact grade-1 -> grade-2 native timing formula; advanced stacking is intentionally excluded.
    public static bool TryGetBasicInserterTiming(int currentStt, int currentTime, int sourcePrefabStt,
        int targetPrefabStt, out int newStt, out int newTime)
    {
        newStt = newTime = 0;
        if (sourcePrefabStt <= 0 || targetPrefabStt <= 0 || currentStt <= 0 || currentTime < 0
            || currentTime > currentStt || currentStt % sourcePrefabStt != 0) return false;
        var span = currentStt / sourcePrefabStt;
        if (span < 1 || span > 3 || targetPrefabStt > int.MaxValue / span) return false;
        newStt = targetPrefabStt * span;
        var expectedTime = (double)currentTime / currentStt * newStt + 0.5;
        if (expectedTime > int.MaxValue) return false;
        newTime = (int)expectedTime;
        return true;
    }

    public static bool ProvesPreservation(FactoryEntitySnapshot before, FactoryEntitySnapshot after,
        int targetItemId)
    {
        if (before.ObjectKind != FactoryObjectKinds.Entity || after.ObjectKind != FactoryObjectKinds.Entity
            || before.ObjectId <= 0 || after.ObjectId <= 0 || before.SessionId != after.SessionId
            || before.PlanetId != after.PlanetId || after.ItemId != targetItemId
            || !SupportsPair(before.ItemId, targetItemId) || before.ComponentKind != after.ComponentKind
            || (IsAssembler(before.ItemId) ? before.ComponentKind != "assembler" : before.ComponentKind != "inserter")
            || before.RecipeId != after.RecipeId || before.PowerNetworkId != after.PowerNetworkId
            || before.FilterItemId != after.FilterItemId
            || before.PickTargetObjectId != after.PickTargetObjectId
            || before.InsertTargetObjectId != after.InsertTargetObjectId
            || before.InserterStage != after.InserterStage || before.InserterStackCount != after.InserterStackCount
            || before.Position.X != after.Position.X || before.Position.Y != after.Position.Y
            || before.Position.Z != after.Position.Z || before.Rotation.X != after.Rotation.X
            || before.Rotation.Y != after.Rotation.Y || before.Rotation.Z != after.Rotation.Z
            || before.Rotation.W != after.Rotation.W || before.Connections.Count != after.Connections.Count
            || before.Buffers.Count != after.Buffers.Count)
            return false;
        var oldConnections = before.Connections.OrderBy(c => c.Slot).ToArray();
        var newConnections = after.Connections.OrderBy(c => c.Slot).ToArray();
        for (var i = 0; i < oldConnections.Length; i++)
        {
            var a = oldConnections[i];
            var b = newConnections[i];
            var mapped = a.OtherObjectId == before.ObjectId ? after.ObjectId : a.OtherObjectId;
            if (a.Slot != b.Slot || a.IsOutput != b.IsOutput || mapped != b.OtherObjectId
                || a.OtherSlot != b.OtherSlot) return false;
        }
        var oldBuffers = before.Buffers.OrderBy(b => b.Role, StringComparer.Ordinal).ThenBy(b => b.ItemId).ToArray();
        var newBuffers = after.Buffers.OrderBy(b => b.Role, StringComparer.Ordinal).ThenBy(b => b.ItemId).ToArray();
        for (var i = 0; i < oldBuffers.Length; i++)
        {
            var a = oldBuffers[i];
            var b = newBuffers[i];
            if (a.Role != b.Role || a.ItemId != b.ItemId || a.Count != b.Count || a.Inc != b.Inc) return false;
        }
        return true;
    }

    public static bool ProvesInventory(IReadOnlyDictionary<int, int> before,
        IReadOnlyDictionary<int, int> after, int sourceItemId, int targetItemId) =>
        sourceItemId != targetItemId && before.Keys.Concat(after.Keys).Concat(new[] { sourceItemId, targetItemId })
            .Distinct().All(id => (after.TryGetValue(id, out var a) ? a : 0L)
                - (before.TryGetValue(id, out var b) ? b : 0L)
                == (id == sourceItemId ? 1 : id == targetItemId ? -1 : 0));
}
