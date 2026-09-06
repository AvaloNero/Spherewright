using Spherewright.Contracts.Errors;

namespace Spherewright.Bridge.Core.Factory;

/// <summary>Initial native construction filter, not a post-build cargo mutation.</summary>
public static class SorterBuildFilterPolicy
{
    public static string? Validate(int buildingItemId, bool nativeInserter, int filterItemId,
        bool filterExists, bool filterUnlocked)
    {
        if (filterItemId < 0 || filterItemId > short.MaxValue) return BridgeErrorCodes.InvalidRequest;
        // Zero retains existing unfiltered construction, including non-sorters.
        if (filterItemId == 0) return null;
        if (!nativeInserter || (buildingItemId != 2011 && buildingItemId != 2012) || !filterExists)
            return BridgeErrorCodes.InvalidRequest;
        return filterUnlocked ? null : BridgeErrorCodes.TechnologyLocked;
    }

    public static bool MatchesReadback(int expectedFilter, int actualFilter, uint iconItemId, uint iconType) =>
        expectedFilter >= 0 && actualFilter == expectedFilter
        && iconItemId == (uint)expectedFilter && iconType == (expectedFilter > 0 ? 1u : 0u);
}
