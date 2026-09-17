using Spherewright.Mcp.Resources;
using Xunit;

namespace Spherewright.Mcp.Tests;

public sealed class NativeGridGuidanceTests
{
    [Theory]
    [InlineData("longitude grid spacing changes with latitude", "complete fresh `plannedPath`")]
    [InlineData("reject a mismatch", "ignoring unexpected points")]
    [InlineData("OutOfReach", "separately checked short approach and fresh build prepare")]
    [InlineData("shallow-water samples", "eventual sorter connections")]
    public void EmbeddedGuideSeparatesGridPredictionReachAndConnectionApproval(string first, string second)
    {
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains(first, guide, StringComparison.Ordinal);
        Assert.Contains(second, guide, StringComparison.Ordinal);
    }
}
