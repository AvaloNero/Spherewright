using Spherewright.Mcp.Resources;
using Xunit;

namespace Spherewright.Mcp.Tests;

public sealed class AgentShoreCrossingGuideTests
{
    [Fact]
    public void EmbeddedGuideDistinguishesAReverifiedLandingFromUnknownWaterMidpoints()
    {
        var resource = AgentPlaybookResources.GetOpeningMovementPlaybook();
        Assert.Equal(AgentPlaybookResources.OpeningMovementUri, resource.Uri);
        Assert.Contains("previously verified Walk destination", resource.Text, StringComparison.Ordinal);
        Assert.Contains("specifically re-observed in the current owned world", resource.Text, StringComparison.Ordinal);
        Assert.Contains("single continuous native Move", resource.Text, StringComparison.Ordinal);
        Assert.Contains("Never create intermediate water targets", resource.Text, StringComparison.Ordinal);
        Assert.Contains("A building's presence alone is not a verified Walk landing", resource.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void ShoreCrossingGuidanceKeepsEnergyOutcomeAndUnknownEvidenceBoundaries()
    {
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("finite crossing and recovery reserve", guide, StringComparison.Ordinal);
        Assert.Contains("does not turn `unavailable` into dry-ground evidence", guide, StringComparison.Ordinal);
        Assert.Contains("not a general exception for unknown shores", guide, StringComparison.Ordinal);
        Assert.Contains("same action to terminal", guide, StringComparison.Ordinal);
        Assert.Contains("do not submit the same target again", guide, StringComparison.Ordinal);
        Assert.Contains("`movementState=Walk`", guide, StringComparison.Ordinal);
        Assert.Contains("not native pathfinding or route clearance", guide, StringComparison.Ordinal);
    }
}
