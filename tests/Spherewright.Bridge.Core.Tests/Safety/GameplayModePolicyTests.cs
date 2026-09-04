using Spherewright.Bridge.Core.Safety;
using Xunit;

namespace Spherewright.Bridge.Core.Tests.Safety;

public sealed class GameplayModePolicyTests
{
    [Theory]
    [InlineData(false, false, 1f)]
    [InlineData(true, false, 1f)]
    [InlineData(true, true, 1f)]
    [InlineData(false, false, 0.5f)]
    [InlineData(false, false, 8f)]
    [InlineData(true, true, 0f)]
    public void AllowsNormalActions_DoesNotGateSandboxOrResourceMultiplier(
        bool isSandboxMode,
        bool sandboxToolsEnabled,
        float resourceMultiplier)
    {
        Assert.True(GameplayModePolicy.AllowsNormalActions(
            descriptorAvailable: true,
            isPeaceful: true,
            isSandboxMode,
            sandboxToolsEnabled,
            resourceMultiplier));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void AllowsNormalActions_StillRequiresReadablePeacefulDescriptor(
        bool descriptorAvailable,
        bool isPeaceful)
    {
        Assert.False(GameplayModePolicy.AllowsNormalActions(
            descriptorAvailable,
            isPeaceful,
            isSandboxMode: false,
            sandboxToolsEnabled: false,
            resourceMultiplier: 1f));
    }
}
