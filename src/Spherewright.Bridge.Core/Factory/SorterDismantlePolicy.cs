using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

public static class SorterDismantlePolicy
{
    public static bool Supports(int itemId) => itemId == 2011 || itemId == 2012;

    public static bool NativeCargoRecoverable(int itemId, int grade, bool canStack, bool bidirectional,
        int stackInput, int stackOutput, int heldItemId, int heldCount, int heldInc, int stackCount)
    {
        if (!Supports(itemId) || grade != itemId - 2010 || canStack || bidirectional || stackInput != 1 || stackOutput != 1)
            return false;
        // TakeBackItems_Inserter requires BOTH a positive itemId and stackCount.
        // Never declare a nonempty zero-stack corrupted component recoverable.
        return heldCount == 0 ? heldInc == 0 && stackCount == 0
            : heldCount == 1 && heldItemId > 0 && heldInc >= 0 && heldInc <= short.MaxValue && stackCount == 1;
    }

    public static bool PresentConnectionsAreReciprocal(FactoryEntitySnapshot target, IReadOnlyList<FactoryEntitySnapshot> neighbors)
    {
        if (target.Connections is null || target.Connections.Count > 2 || neighbors is null || neighbors.Count > 4
            || neighbors.Any(n => n is null || n.ObjectId <= 0 || n.ObjectId == target.ObjectId || n.Connections is null || n.Connections.Count > 16)
            || neighbors.Select(n => n.ObjectId).Distinct().Count() != neighbors.Count
            || target.Connections.Select(e => e.Slot).Distinct().Count() != target.Connections.Count) return false;
        foreach (var edge in target.Connections)
        {
            if (edge.OtherObjectId <= 0 || edge.OtherSlot < 0 || edge.OtherSlot >= 16
                || edge.Slot != (edge.IsOutput ? 0 : 1)) return false;
            var other = neighbors.SingleOrDefault(n => n.ObjectId == edge.OtherObjectId);
            if (other is null || !other.Connections.Any(e => e.Slot == edge.OtherSlot && e.OtherObjectId == target.ObjectId
                && e.OtherSlot == edge.Slot && e.IsOutput != edge.IsOutput)) return false;
        }
        // Missing ends are permitted; mismatched present or neighbor-only ends are not.
        foreach (var neighbor in neighbors)
            foreach (var edge in neighbor.Connections.Where(e => e.OtherObjectId == target.ObjectId))
                if (!target.Connections.Any(e => e.Slot == edge.OtherSlot && e.OtherObjectId == neighbor.ObjectId
                    && e.OtherSlot == edge.Slot && e.IsOutput != edge.IsOutput)) return false;
        return true;
    }

    public static bool ProvesIncRecovery(IReadOnlyDictionary<int, int> before, IReadOnlyDictionary<int, int> after,
        int heldItemId, int heldInc) => before.Keys.Concat(after.Keys).Append(heldItemId).Distinct()
        .All(id => (after.TryGetValue(id, out var a) ? a : 0) - (long)(before.TryGetValue(id, out var b) ? b : 0)
            == (id == heldItemId ? heldInc : 0));

    public static bool SurvivorMatches(FactoryEntitySnapshot expectedWithoutRemovedEdges, FactoryEntitySnapshot actual) =>
        CanonicalStateHash.FactoryEndpoint(expectedWithoutRemovedEdges) == CanonicalStateHash.FactoryEndpoint(actual);
}
