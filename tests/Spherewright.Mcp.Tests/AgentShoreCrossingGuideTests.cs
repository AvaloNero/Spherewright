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

    [Fact]
    public void ShortCrossingGuideRequiresCompleteEvidenceAndAnAboveWaterLandingMargin()
    {
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("fully observed short crossing", guide, StringComparison.Ordinal);
        Assert.Contains("water in transit", guide, StringComparison.Ordinal);
        Assert.Contains("`state=observed`", guide, StringComparison.Ordinal);
        Assert.Contains("`unknownSampleCount=0`", guide, StringComparison.Ordinal);
        Assert.Contains("`arcLengthMetres<=32`", guide, StringComparison.Ordinal);
        Assert.Contains("arcLengthMetres/(samples.Count-1)<=1", guide, StringComparison.Ordinal);
        Assert.Contains("at least4m", guide, StringComparison.Ordinal);
        Assert.Contains("`groundHit=true`, `waterHit=true`", guide, StringComparison.Ordinal);
        Assert.Contains("finite non-null `groundBelowWaterMetres<=-0.1`", guide, StringComparison.Ordinal);
        Assert.Contains("`arrivalTolerance<=0.5`", guide, StringComparison.Ordinal);
    }

    [Fact]
    public void ShortCrossingGuideDoesNotPromoteShallowOrUnknownSamplesToWalkProof()
    {
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("not proof of Walk or route clearance", guide, StringComparison.Ordinal);
        Assert.Contains("shallow-water endpoint, missing sample, `partial` or `unavailable`", guide, StringComparison.Ordinal);
        Assert.Contains("check the evidence again, commit once, poll to terminal", guide, StringComparison.Ordinal);
        Assert.Contains("Stop on failure or non-ready arrival, retain the action", guide, StringComparison.Ordinal);
        Assert.Contains("not turn this into a candidate sweep or an automatic routing loop", guide, StringComparison.Ordinal);
    }
}
