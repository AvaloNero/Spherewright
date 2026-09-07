using System.ComponentModel;
using System.Reflection;
using Spherewright.Mcp.Resources;
using Spherewright.Mcp.Tools;
using Xunit;

namespace Spherewright.Mcp.Tests;

public sealed class AgentInventoryAccountingTests
{
    [Fact]
    public void PackagedPlaybookRequiresResearchBufferEvidenceForInventoryReturns()
    {
        var playbook = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("mechaResearchItemBuffer", playbook, StringComparison.Ordinal);
        Assert.Contains("3600 points", playbook, StringComparison.Ordinal);
        Assert.Contains("before/after progression", playbook, StringComparison.Ordinal);
        Assert.Contains("never round up", playbook, StringComparison.Ordinal);
        Assert.Contains("not permission to accept arbitrary inventory gains", playbook, StringComparison.Ordinal);
    }

    [Fact]
    public void PlayerToolDescriptionMakesExistingResearchAccountingDiscoverable()
    {
        var description = typeof(SpherewrightTools).GetMethod(nameof(SpherewrightTools.GetPlayerStateAsync))!
            .GetCustomAttribute<DescriptionAttribute>()!.Description;
        Assert.Contains("mechaResearchItemBuffer", description, StringComparison.Ordinal);
        Assert.Contains("autoManageResearchItems", description, StringComparison.Ordinal);
        Assert.Contains("fresh progression", description, StringComparison.Ordinal);
    }
}
