namespace Spherewright.Bridge.Core.Safety;

public static class SorterFilterPolicy
{
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
