using System.Text.Json;
using Spherewright.Contracts.Actions;
using Xunit;

namespace Spherewright.Contracts.Tests;

public sealed class MovementSurfacePreviewContractTests
{
    [Fact]
    public void LegacyPlanDoesNotClaimASurfaceObservation() => Assert.Null(JsonSerializer.Deserialize<PreparedNormalAction>("{}")!.SurfacePreview);

    [Theory]
    [InlineData("detected")]
    [InlineData("unknown")]
    [InlineData("not_detected")]
    public void AdditiveSurfaceEvidenceRoundTripsWithoutChangingAuthority(string risk)
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var plan = new PreparedNormalAction
        {
            Prepared = true,
            PlanToken = "opaque",
            ExpectedStateHash = "same",
            SurfacePreview = new()
            { State = "partial", CapturedAtGameTick = 42, ShoreRisk = risk, UnknownSampleCount = 1, Samples = new() { new() { GroundHit = false } } }
        };
        var round = JsonSerializer.Deserialize<PreparedNormalAction>(JsonSerializer.Serialize(plan, options), options)!;
        Assert.Equal("opaque", round.PlanToken); Assert.Equal("same", round.ExpectedStateHash);
        Assert.Equal(risk, round.SurfacePreview!.ShoreRisk); Assert.Equal(42, round.SurfacePreview.CapturedAtGameTick);
        Assert.Null(round.SurfacePreview.Samples[0].GroundAltitudeMetres);
        Assert.Equal("downward_samples_not_route_clearance", round.SurfacePreview.EvidenceScope);
    }
}
