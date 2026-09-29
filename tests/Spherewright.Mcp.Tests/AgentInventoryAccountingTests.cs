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
    public void PackagedPlaybookRequiresRuntimeItemIdentityBeforeSupplyRepair()
    {
        var playbook = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("runtime item identity", playbook, StringComparison.Ordinal);
        Assert.Contains("itemId", playbook, StringComparison.Ordinal);
        Assert.Contains("itemName", playbook, StringComparison.Ordinal);
        Assert.Contains("get_recipe_catalog", playbook, StringComparison.Ordinal);
        Assert.Contains("discard that diagnosis", playbook, StringComparison.Ordinal);
        Assert.Contains("do not commit a supply change for the wrong material", playbook, StringComparison.Ordinal);
    }

    [Fact]
    public void PackagedPlaybookKeepsShortLivedPlanInProtectedCaller()
    {
        var playbook = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("same protected caller context", playbook, StringComparison.Ordinal);
        Assert.Contains("do not print or persist a `planToken`", playbook, StringComparison.Ordinal);
        Assert.Contains("Ordinary MCP prepare/commit tool calls", playbook, StringComparison.Ordinal);
        Assert.Contains("zero accepted/in-flight actions", playbook, StringComparison.Ordinal);
        Assert.Contains("reconcile its original action", playbook, StringComparison.Ordinal);
    }

    [Fact]
    public void PackagedPlaybookNamesResearchGuardsAndHashFields()
    {
        var playbook = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("name any failed field", playbook, StringComparison.Ordinal);
        Assert.Contains("ownedBySpherewright", playbook, StringComparison.Ordinal);
        Assert.Contains("expectedSelectionStateHash", playbook, StringComparison.Ordinal);
        Assert.Contains("selectionStateHash", playbook, StringComparison.Ordinal);
        Assert.Contains("not the general progression `stateHash`", playbook, StringComparison.Ordinal);
    }

    [Fact]
    public void PackagedPlaybookReusesNormalActionCallerInsteadOfHandBuiltCommit()
    {
        var playbook = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("Invoke-SpherewrightNormalAction", playbook, StringComparison.Ordinal);
        Assert.Contains("both commit `sessionId` and `planetId`", playbook, StringComparison.Ordinal);
        Assert.Contains("Do not hand-build a fresh commit payload", playbook, StringComparison.Ordinal);
        Assert.Contains("Two rejected commits at the same stage", playbook, StringComparison.Ordinal);
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
