using Spherewright.Mcp.Resources;
using Xunit;

namespace Spherewright.Mcp.Tests;

public sealed class NativeGridGuidanceTests
{
    [Fact]
    public void EmbeddedGuideDistinguishesElevatedEndpointSpanFromCallerLengthFailure()
    {
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("straight3D distance between the native-snapped endpoints", guide, StringComparison.Ordinal);
        Assert.Contains("not the sum of grid-route segment lengths", guide, StringComparison.Ordinal);
        Assert.Contains("separate64-point, per-segment, altitude and full native checks", guide, StringComparison.Ordinal);
        Assert.Contains("A local caller assertion is not a native rejection", guide, StringComparison.Ordinal);
        Assert.Contains("permission to execute an unapproved whole-chain plan", guide, StringComparison.Ordinal);
    }

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

    [Theory]
    [InlineData("Free endpoints are snapped before line generation, including `native_geodesic`", "both endpoint snaps before paper occupancy")]
    [InlineData("rejected prepare returns no path", "do not invent its snapped coordinates")]
    [InlineData("Retire the rejected candidate", "do not repeat the unchanged target")]
    public void EmbeddedGuideDoesNotTreatUnsnappedGeodesicPaperAsNativeApproval(string first, string second)
    {
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains(first, guide, StringComparison.Ordinal);
        Assert.Contains(second, guide, StringComparison.Ordinal);
    }
}
