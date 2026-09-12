using Spherewright.Mcp.Resources;
using Xunit;

namespace Spherewright.Mcp.Tests;

public sealed class SorterTimingGuidanceTests
{
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
