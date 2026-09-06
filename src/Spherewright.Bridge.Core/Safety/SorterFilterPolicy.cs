using Spherewright.Contracts.Errors;

namespace Spherewright.Bridge.Core.Safety;

public static class SorterFilterPolicy
{
    public static BridgeError? ValidateInspectionHash(string? supplied, string? fullHash, string? configurationHash)
    {
        if (string.IsNullOrWhiteSpace(configurationHash))
            return BridgeError.Create(BridgeErrorCodes.BridgeNotReady,
                "Sorter configurationStateHash is unavailable.", false, "Fresh inspect a supported sorter; do not substitute its full stateHash.");
        if (string.IsNullOrWhiteSpace(supplied))
            return BridgeError.Create(BridgeErrorCodes.InvalidRequest,
                "sorter-filter requires the root configurationStateHash in expectedFactoryStateHash.", false,
                "Use this mode's documented hash, not an empty value or another action mode's hash.");
        if (string.Equals(supplied, configurationHash, StringComparison.Ordinal)) return null;
        if (string.Equals(supplied, fullHash, StringComparison.Ordinal))
            return BridgeError.Create(BridgeErrorCodes.InvalidRequest,
                "sorter-filter received full stateHash, but requires root configurationStateHash in expectedFactoryStateHash.", false,
                "Correct the hash field for sorter-filter and fresh prepare; repeating the full hash is not a state-change recovery. storage-capacity still uses full stateHash.");
        return BridgeError.Create(BridgeErrorCodes.StaleState,
            "Sorter configuration hash changed or belongs to the wrong hash domain; identity, topology, filter and held cargo must match.", true,
            "Fresh inspect and use root configurationStateHash for sorter-filter, not full stateHash. Do not repeat an unchanged wrong-domain request.");
    }

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
