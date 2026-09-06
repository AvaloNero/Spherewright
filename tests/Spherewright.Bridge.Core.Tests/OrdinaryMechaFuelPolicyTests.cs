using Spherewright.Bridge.Core.Safety;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class OrdinaryMechaFuelPolicyTests
{
    [Theory]
    [InlineData(0L, 1, true, false)]
    [InlineData(-1L, 1, true, false)]
    [InlineData(8000000L, 0, true, false)]
    [InlineData(8000000L, -1, true, false)]
    [InlineData(8000000L, 1, false, false)]
    [InlineData(8000000L, 1, true, true)]
    [InlineData(4400000L, 1, true, true)]
    [InlineData(1L, 2, true, true)]
    public void PreservesAllThreeExistingNativeRefuelEligibilityChecks(long heat, int type, bool nativeFlag, bool expected)
        => Assert.Equal(expected, OrdinaryMechaFuelPolicy.IsAccepted(heat, type, nativeFlag));
}
