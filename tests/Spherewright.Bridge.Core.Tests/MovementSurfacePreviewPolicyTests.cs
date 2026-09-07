using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class MovementSurfacePreviewPolicyTests
{
    private static Vector3Snapshot Point(double arc, double radius = 200) => new()
    { X = (float)(radius * Math.Cos(arc / radius)), Z = (float)(radius * Math.Sin(arc / radius)) };
    private static MovementSurfaceSamplingPlan Plan(double arc = 1) => MovementSurfacePreviewPolicy.CreatePlan(Point(0), Point(arc), 200)!;
    private static MovementSurfaceRayEvidence Ground(double altitude = .2, double waterDistance = 10) => new()
    { GroundHit = true, GroundDistance = 10 - altitude, GroundAltitude = altitude, WaterHit = true, WaterDistance = waterDistance };

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(12.5)]
    [InlineData(28)]
    [InlineData(32)]
    public void SamplesCompleteShortArcWithinFixedQueryBudget(double arc)
    {
        var plan = Plan(arc);
        Assert.NotNull(plan);
        Assert.InRange(plan.Points.Count, 2, 33);
        Assert.InRange(plan.ArcLengthMetres, Math.Max(0, arc - .0001), arc + .0001);
        Assert.InRange(plan.ArcLengthMetres / (plan.Points.Count - 1), 0, 1.000001);
        Assert.InRange(Math.Abs(plan.Points[^1].Z - Point(arc).Z), 0, .0001);
        Assert.All(plan.Points, p => Assert.InRange(Math.Sqrt((double)p.X * p.X + (double)p.Y * p.Y + (double)p.Z * p.Z), 199.9999, 200.0001));
        var preview = MovementSurfacePreviewPolicy.Summarize(plan, plan.Points.Select(_ => Ground()).ToArray(), 1000, 123);
        Assert.Equal(2 * plan.Points.Count, preview.RaycastCount);
        Assert.Equal("not_detected", preview.ShoreRisk);
        Assert.Equal("downward_samples_not_route_clearance", preview.EvidenceScope);
    }

    [Theory]
    [InlineData(32.1)]
    [InlineData(200)]
    [InlineData(628)]
    public void LongSpanIsNotTruncatedOrGivenFalseClearEvidence(double arc) => Assert.Null(MovementSurfacePreviewPolicy.CreatePlan(Point(0), Point(arc), 200));

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(10001)]
    public void InvalidRadiusIsUnavailable(double radius) => Assert.Null(MovementSurfacePreviewPolicy.CreatePlan(Point(0), Point(1), radius));

    [Fact]
    public void BadOrAntipodalCoordinatesAreUnavailable()
    {
        Assert.Null(MovementSurfacePreviewPolicy.CreatePlan(null, Point(1), 200));
        Assert.Null(MovementSurfacePreviewPolicy.CreatePlan(new(), Point(1), 200));
        Assert.Null(MovementSurfacePreviewPolicy.CreatePlan(new() { X = float.NaN }, Point(1), 200));
        Assert.Null(MovementSurfacePreviewPolicy.CreatePlan(Point(0), new() { X = -200 }, 200));
        Assert.Null(MovementSurfacePreviewPolicy.CreatePlan(new() { X = 10 }, new() { X = -10 }, 10));
    }

    [Theory]
    [InlineData(-.39, "not_detected")]
    [InlineData(-.41, "detected")]
    [InlineData(-1, "detected")]
    public void NativeDownwardGapContributesAdvisoryRisk(double altitude, string expected)
    {
        var plan = Plan();
        var preview = MovementSurfacePreviewPolicy.Summarize(plan, plan.Points.Select(_ => Ground(altitude)).ToArray(), 1000, 1);
        Assert.Equal(expected, preview.ShoreRisk);
        Assert.Equal(altitude, preview.Samples[0].GroundAltitudeMetres);
    }

    [Theory]
    [InlineData("ground_miss")]
    [InlineData("water_miss")]
    [InlineData("nan")]
    [InlineData("negative_distance")]
    [InlineData("beyond_ray")]
    [InlineData("inconsistent_altitude")]
    [InlineData("bad_water")]
    public void MissingOrInvalidRayEvidenceRemainsUnknown(string fault)
    {
        var ray = Ground();
        switch (fault)
        {
            case "ground_miss": ray.GroundHit = false; break;
            case "water_miss": ray.WaterHit = false; break;
            case "nan": ray.GroundAltitude = double.NaN; break;
            case "negative_distance": ray.GroundDistance = -1; break;
            case "beyond_ray": ray.GroundDistance = 31; break;
            case "inconsistent_altitude": ray.GroundAltitude = 5; break;
            case "bad_water": ray.WaterDistance = double.PositiveInfinity; break;
        }
        var plan = Plan(); var preview = MovementSurfacePreviewPolicy.Summarize(plan, plan.Points.Select(_ => ray).ToArray(), 1000, 1);
        Assert.Equal("unknown", preview.ShoreRisk);
        Assert.Equal("partial", preview.State);
        Assert.Equal(plan.Points.Count, preview.UnknownSampleCount);
    }

    [Fact]
    public void KnownRiskIsNotMaskedByAnUnknownSample()
    {
        var plan = Plan(); var rays = plan.Points.Select(_ => Ground()).ToArray();
        rays[0] = Ground(-1); rays[^1].GroundHit = false;
        var preview = MovementSurfacePreviewPolicy.Summarize(plan, rays, 1000, 1);
        Assert.Equal("detected", preview.ShoreRisk);
        Assert.Equal(1, preview.ShoreRiskSampleCount);
        Assert.Equal(1, preview.UnknownSampleCount);
    }

    [Fact]
    public void NoWaterItemDoesNotRequireWaterColliderButStillRequiresGround()
    {
        var plan = Plan(); var rays = plan.Points.Select(_ => Ground()).ToArray();
        foreach (var ray in rays) ray.WaterHit = false;
        Assert.Equal("not_detected", MovementSurfacePreviewPolicy.Summarize(plan, rays, 0, 1).ShoreRisk);
        rays[0].GroundHit = false;
        Assert.Equal("unknown", MovementSurfacePreviewPolicy.Summarize(plan, rays, 0, 1).ShoreRisk);
    }

    [Fact]
    public void ProbeCountAndMetadataAreCheckedAndOutputPositionsAreCopied()
    {
        var plan = Plan(); var rays = plan.Points.Select(_ => Ground()).ToArray();
        Assert.Throws<ArgumentException>(() => MovementSurfacePreviewPolicy.Summarize(plan, Array.Empty<MovementSurfaceRayEvidence>(), 1000, 1));
        Assert.Throws<ArgumentException>(() => MovementSurfacePreviewPolicy.Summarize(plan, rays, -1, 1));
        Assert.Throws<ArgumentException>(() => MovementSurfacePreviewPolicy.Summarize(plan, rays, 1000, -1));
        var preview = MovementSurfacePreviewPolicy.Summarize(plan, rays, 1000, 1);
        preview.Samples[0].Position.X = 0;
        Assert.Equal(200, plan.Points[0].X);
    }
}
