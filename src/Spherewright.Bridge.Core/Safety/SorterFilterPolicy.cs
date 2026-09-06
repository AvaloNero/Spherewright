namespace Spherewright.Bridge.Core.Safety;

public static class SorterFilterPolicy
{
    // UIInserterWindow changes only filter/sign. Existing cargo still goes to the
    // old insert target; this is not a take-back, flush, or cargo rerouting action.
    public static bool IsSafePreservingAssignmentWindow(
        int sorterItemId, bool bidirectional, string stage,
        int filterItemId, int pickTargetObjectId, int insertTargetObjectId,
        int heldItemId, int heldItemCount, int heldStackCount, int heldItemInc)
    {
        if (filterItemId < 0 || filterItemId > short.MaxValue) return false;
        if (IsSafeAssignmentWindow(filterItemId, pickTargetObjectId, insertTargetObjectId,
                heldItemId, heldItemCount, heldStackCount, heldItemInc)) return true;
        return (sorterItemId == 2011 || sorterItemId == 2012) && !bidirectional
            && stage == "Inserting" && pickTargetObjectId > 0 && insertTargetObjectId > 0
            && heldItemId > 0 && heldItemId <= short.MaxValue
            && heldItemCount > 0 && heldItemCount <= short.MaxValue
            && heldStackCount > 0 && heldStackCount <= byte.MaxValue && heldStackCount <= heldItemCount
            && heldItemInc >= 0 && heldItemInc <= short.MaxValue;
    }

    public static bool IsSafeAssignmentWindow(
        int filterItemId,
        int pickTargetObjectId,
        int insertTargetObjectId,
        int heldItemId,
        int heldItemCount,
        int heldStackCount,
        int heldItemInc)
    {
        return filterItemId >= 0
               && pickTargetObjectId != 0
               && insertTargetObjectId != 0
               && heldItemId == 0
               && heldItemCount == 0
               && heldStackCount == 0
               && heldItemInc == 0;
    }
}
