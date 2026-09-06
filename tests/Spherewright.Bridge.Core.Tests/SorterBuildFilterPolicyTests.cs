using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Errors;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class SorterBuildFilterPolicyTests
{
    [Theory]
    [InlineData(2011, true, 1000, true, true, null)]
    [InlineData(2012, true, 1114, true, true, null)]
    [InlineData(2011, true, 0, false, false, null)]
    [InlineData(2101, false, 0, false, false, null)]
    [InlineData(2001, false, 0, false, false, null)]
    [InlineData(2303, false, 0, false, false, null)]
    [InlineData(2013, true, 0, false, false, null)]
    [InlineData(2011, true, -1, true, true, BridgeErrorCodes.InvalidRequest)]
    [InlineData(2011, true, 32768, true, true, BridgeErrorCodes.InvalidRequest)]
    [InlineData(2011, true, int.MaxValue, true, true, BridgeErrorCodes.InvalidRequest)]
    [InlineData(2011, true, 1000, false, true, BridgeErrorCodes.InvalidRequest)]
    [InlineData(2011, true, 1000, true, false, BridgeErrorCodes.TechnologyLocked)]
    [InlineData(2011, false, 1000, true, true, BridgeErrorCodes.InvalidRequest)]
    [InlineData(2001, false, 1000, true, true, BridgeErrorCodes.InvalidRequest)]
    [InlineData(2101, false, 1000, true, true, BridgeErrorCodes.InvalidRequest)]
    [InlineData(2013, true, 1000, true, true, BridgeErrorCodes.InvalidRequest)]
    [InlineData(2014, true, 1000, true, true, BridgeErrorCodes.InvalidRequest)]
    public void OnlyBoundedUnlockedInitialFiltersAreAcceptedWithoutChangingZeroCompatibility(
        int building, bool native, int filter, bool exists, bool unlocked, string? expected) =>
        Assert.Equal(expected, SorterBuildFilterPolicy.Validate(building, native, filter, exists, unlocked));

    [Theory]
    [InlineData(1000, 1000, 1000u, 1u, true)]
    [InlineData(0, 0, 0u, 0u, true)]
    [InlineData(1000, 0, 0u, 0u, false)]
    [InlineData(1000, 1115, 1000u, 1u, false)]
    [InlineData(1000, 1000, 1115u, 1u, false)]
    [InlineData(1000, 1000, 1000u, 0u, false)]
    [InlineData(0, 0, 0u, 1u, false)]
    [InlineData(-1, -1, uint.MaxValue, 0u, false)]
    public void FinalFilterAndNativeSignMustBothMatch(int wanted, int actual, uint icon, uint type, bool expected) =>
        Assert.Equal(expected, SorterBuildFilterPolicy.MatchesReadback(wanted, actual, icon, type));
}
