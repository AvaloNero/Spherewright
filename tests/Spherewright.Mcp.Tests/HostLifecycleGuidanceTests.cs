using Spherewright.Mcp.Resources;
using Xunit;

namespace Spherewright.Mcp.Tests;

public sealed class HostLifecycleGuidanceTests
{
    [Fact]
    public void EmbeddedPlaybookKeepsHostRestartIndependentOfGameLifecycle()
    {
        var resource = AgentPlaybookResources.GetOpeningMovementPlaybook();

        Assert.Equal(AgentPlaybookResources.OpeningMovementUri, resource.Uri);
        Assert.Contains("Codex/MCP Host shutdown, restart, disconnect, context compaction, or turn completion", resource.Text);
        Assert.Contains("must not trigger DSP save, exit, relaunch, or reload", resource.Text);
        Assert.Contains("continue the current world without protected resume", resource.Text);
        Assert.Contains("preserve the ten-write count", resource.Text);
    }

    [Fact]
    public void ReconnectGuidancePreservesActionReconciliationAndAuthorizedRecovery()
    {
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;

        Assert.Contains("fresh-read owned identity, session/revision, durable Journal, external accepted and the original action ledger", guide);
        Assert.Contains("freeze new writes and reconcile the same action; never replay", guide);
        Assert.Contains("original writer stopped, no in-flight actions or unreconciled results", guide);
        Assert.Contains("explicit single-writer handoff", guide);
        Assert.Contains("explicit user game-close request or a necessary, already-authorized cold deployment", guide);
        Assert.Contains("a Host restart is not loading authorization or proof of save freshness", guide);
    }
}
