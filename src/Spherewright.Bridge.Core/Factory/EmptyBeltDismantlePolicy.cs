using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

// One empty, isolated, ordinary chain head. Never a general belt-delete policy.
public static class EmptyBeltDismantlePolicy
{
    public const int ItemId = 2001;
    public const int MaximumBelts = 16;
    public const int MaximumPathCells = 512;

    public static bool SupportsSnapshot(FactoryEntitySnapshot? target) => target is not null
        && target.ObjectKind == FactoryObjectKinds.Entity && target.ObjectId > 0
        && target.ItemId == ItemId && target.ComponentKind == "belt" && target.RecipeId == 0
        && target.Buffers is not null && target.Buffers.Count == 0
        && target.Connections is not null;

    // orderedIds comes from the complete native Export, not sorted object IDs.
    // A single empty belt sample cannot stand in for this complete stock cut.
    public static bool TryQualify(FactoryEntitySnapshot? target, IReadOnlyList<int>? orderedIds,
        MaterialInventoryCutSnapshot? cut, out string reason)
    {
        reason = "empty_belt_complete_chain_evidence_required";
        if (target is null || !SupportsSnapshot(target) || orderedIds is null || orderedIds.Count < 1
            || orderedIds.Count > MaximumBelts || orderedIds[0] != target!.ObjectId
            || orderedIds.Any(id => id <= 0) || orderedIds.Distinct().Count() != orderedIds.Count
            || cut is null || cut.State != "observed" || cut.ReasonCode is not null
            || cut.Coverage != "explicit_objects_and_complete_native_cargo_paths"
            || string.IsNullOrEmpty(cut.SessionId) || cut.SessionId != target.SessionId || cut.PlanetId != target.PlanetId
            || cut.CapturedAtGameTick <= 0 || cut.RequestedObjectIds is null
            || !cut.RequestedObjectIds.SequenceEqual(orderedIds) || cut.Objects is null
            || cut.Objects.Count != orderedIds.Count || cut.CargoPaths is null || cut.CargoPaths.Count != 1)
            return false;

        var path = cut.CargoPaths[0];
        reason = "empty_belt_requires_empty_open_independent_path";
        if (path is null || path.PathId <= 0 || path.PathLengthCells <= 0
            || path.PathLengthCells > MaximumPathCells || path.PathClosed || path.OutputPathId != 0
            || path.InputPathIds is null || path.InputPathIds.Count != 0
            || path.CapturedAtGameTick != cut.CapturedAtGameTick || path.CargoStackCount != 0
            || path.ItemCount != 0 || path.Items is null || path.Items.Count != 0
            || path.BeltObjectIds is null || path.BeltObjectIds.Count != orderedIds.Count
            || !path.BeltObjectIds.OrderBy(id => id).SequenceEqual(orderedIds.OrderBy(id => id)))
            return false;

        reason = "empty_belt_requires_exact_isolated_reciprocal_chain";
        for (var index = 0; index < orderedIds.Count; index++)
        {
            var entity = cut.Objects[index];
            if (!SupportsSnapshot(entity) || entity.ObjectId != orderedIds[index]
                || entity.SessionId != cut.SessionId || entity.PlanetId != cut.PlanetId
                || entity.CapturedAtGameTick != cut.CapturedAtGameTick
                || entity.Connections.Count != (index > 0 ? 1 : 0) + (index + 1 < orderedIds.Count ? 1 : 0))
                return false;
            var seenSlots = new HashSet<int>();
            foreach (var edge in entity.Connections)
            {
                if (edge is null || !seenSlots.Add(edge.Slot)) return false;
                if (edge.Slot == 0 && index + 1 < orderedIds.Count)
                {
                    if (!edge.IsOutput || edge.OtherObjectId != orderedIds[index + 1] || edge.OtherSlot != 1)
                        return false;
                }
                else if (edge.Slot == 1 && index > 0)
                {
                    if (edge.IsOutput || edge.OtherObjectId != orderedIds[index - 1] || edge.OtherSlot != 0)
                        return false;
                }
                else return false;
            }
        }
        if (CanonicalStateHash.FactoryEndpoint(target!) != CanonicalStateHash.FactoryEndpoint(cut.Objects[0]))
            return false;
        reason = string.Empty;
        return true;
    }

    // Used by the complete bounded native entity/prebuild reference scan.
    public static bool ReferenceIsSafe(IReadOnlyCollection<int> members, int signedOwnerId, int otherObjectId) =>
        members.Count > 0 && signedOwnerId != 0
        && (members.Contains(signedOwnerId)
            ? otherObjectId == 0 || members.Contains(otherObjectId) && otherObjectId != signedOwnerId
            : !members.Contains(otherObjectId));

    public static string Fingerprint(FactoryEntitySnapshot target, IReadOnlyList<int> orderedIds,
        MaterialInventoryCutSnapshot cut, string nativeBindingHash)
    {
        if (!TryQualify(target, orderedIds, cut, out _) || string.IsNullOrWhiteSpace(nativeBindingHash))
            throw new ArgumentException("An empty belt binding requires complete qualified native evidence.");
        return CanonicalStateHash.Combine("empty-belt-head-dismantle-v1", nativeBindingHash,
            string.Join(",", orderedIds), string.Join(",", cut.Objects.Select(CanonicalStateHash.FactoryEndpoint)),
            cut.CargoPaths[0].PathId, cut.CargoPaths[0].PathLengthCells);
    }
}
