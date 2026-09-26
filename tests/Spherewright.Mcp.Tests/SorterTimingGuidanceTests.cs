using Spherewright.Mcp.Resources;
using Xunit;

namespace Spherewright.Mcp.Tests;

public sealed class SorterTimingGuidanceTests
{
    [Fact]
    public void EmbeddedPlaybookSeparatesBuiltSorterReadbackFromProspectiveGeometry()
    {
        var resource = AgentPlaybookResources.GetOpeningMovementPlaybook();

        Assert.Equal(AgentPlaybookResources.OpeningMovementUri, resource.Uri);
        Assert.Contains("prospective placement geometry", resource.Text, StringComparison.Ordinal);
        Assert.Contains("pickTargetObjectId", resource.Text, StringComparison.Ordinal);
        Assert.Contains("insertTargetObjectId", resource.Text, StringComparison.Ordinal);
        Assert.Contains("sorterEndpoints=unavailable", resource.Text, StringComparison.Ordinal);
        Assert.Contains("turn a successful terminal into a failed build", resource.Text, StringComparison.Ordinal);
        Assert.Contains("does not waive a native `prepare_build` rejection", resource.Text, StringComparison.Ordinal);
        Assert.Contains("Verify power and actual cargo/delivery separately", resource.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("progressRequired", "not elapsed game ticks")]
    [InlineData("inserterSttRaw", "inserterGrade")]
    [InlineData("ordinary2011 has base200000", "span3 gives600000")]
    [InlineData("fast2012 has base100000", "span3 gives300000")]
    [InlineData("not measured throughput", "actual power and observed flow")]
    [InlineData("original action", "never replay a successful build")]
    public void EmbeddedPlaybookExplainsNativeGradeTimingAndPreservesSuccessfulActions(
        string first, string second)
    {
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains(first, guide, StringComparison.Ordinal);
        Assert.Contains(second, guide, StringComparison.Ordinal);
    }
}
