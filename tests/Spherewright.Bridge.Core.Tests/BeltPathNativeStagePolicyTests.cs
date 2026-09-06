using Spherewright.Bridge.Core.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class BeltPathNativeStagePolicyTests
{
    [Theory]
    [InlineData(true, false, false, 1, true)]
    [InlineData(false, false, false, 1, false)]
    [InlineData(true, true, false, 1, false)]
    [InlineData(true, false, true, 1, false)]
    [InlineData(true, false, false, 0, false)]
    [InlineData(true, false, false, 2, false)]
    [InlineData(true, false, false, -1, false)]
    public void NeverAcceptsSharedActiveOrAnchorOnlyCommandState(bool detached, bool active, bool enabled,
        int stage, bool expected) => Assert.Equal(expected, BeltPathNativeStagePolicy.CanCheck(detached, active, enabled, stage));
}
