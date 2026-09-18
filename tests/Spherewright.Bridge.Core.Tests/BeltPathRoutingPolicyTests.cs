using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Actions;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class BeltPathRoutingPolicyTests
{
    [Fact]
    public void DefaultGridKeepsExistingRequestAndFingerprintSemantics()
    {
        var request = new PrepareBuildRequest { SourceObjectId = 754 };
        Assert.Null(BeltPathRoutingPolicy.ValidateRequest(request, false));
        Assert.Equal("old-geometry", BeltPathRoutingPolicy.BindGeometry(request.BeltPathMode, "old-geometry"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("straight")]
    public void UnknownModesAreNotSilentlyDefaulted(string? mode)
    {
        var request = Request(); request.BeltPathMode = mode!;
        Assert.Equal("belt_routing_mode_unknown", BeltPathRoutingPolicy.ValidateRequest(request, true));
        Assert.Throws<ArgumentException>(() => BeltPathRoutingPolicy.BindGeometry(mode!, "geometry"));
    }

    [Theory]
    [InlineData(2001)]
    [InlineData(2002)]
    [InlineData(2003)]
    public void GeodesicAcceptsOnlyExplicitOrdinaryBeltCandidates(int item)
    {
        var request = Request(); request.BuildingItemId = item;
        Assert.Null(BeltPathRoutingPolicy.ValidateRequest(request, true));
        Assert.NotNull(BeltPathRoutingPolicy.ValidateRequest(request, false));
    }

    [Theory]
    [InlineData("source")]
    [InlineData("negative_source")]
    [InlineData("destination")]
    [InlineData("resource")]
    [InlineData("missing_start")]
    [InlineData("missing_end")]
    [InlineData("nonfinite")]
    [InlineData("wrong_item")]
    public void BoundOrUnknownEndpointsCannotEnterTheFreeGroundSubset(string fault)
    {
        var r = Request();
        switch (fault)
        {
            case "source": r.SourceObjectId = 754; break;
            case "negative_source": r.SourceObjectId = -1; break;
            case "destination": r.DestinationObjectId = 755; break;
            case "resource": r.ResourceNodeId = 1; break;
            case "missing_start": r.PreferredPosition = null; break;
            case "missing_end": r.PathEnd = null; break;
            case "nonfinite": r.PreferredPosition!.X = float.NaN; break;
            case "wrong_item": r.BuildingItemId = 2011; break;
        }
        Assert.Equal("belt_geodesic_requires_explicit_free_endpoints", BeltPathRoutingPolicy.ValidateRequest(r, true));
    }

    [Fact]
    public void GeodesicAcceptsOnlyAnExplicitlyBoundEmpty2001CoverPair()
    {
        var request = new PrepareBuildRequest
        {
            BuildingItemId = 2001, BeltPathMode = BeltPathModes.NativeGeodesic,
            SourceObjectId = 754, DestinationObjectId = 755,
            ExpectedSourceStateHash = "fresh-source", ExpectedDestinationStateHash = "fresh-destination",
        };
        Assert.Null(BeltPathRoutingPolicy.ValidateRequest(request, true));
        Assert.NotNull(BeltPathRoutingPolicy.ValidateRequest(request, false));
    }

    [Theory]
    [InlineData("single_source")]
    [InlineData("zero_source")]
    [InlineData("large_destination")]
    [InlineData("same")]
    [InlineData("source_hash")]
    [InlineData("destination_hash")]
    [InlineData("position")]
    [InlineData("path_end")]
    [InlineData("resource")]
    [InlineData("wrong_item")]
    public void DualCoverGeodesicRejectsPartialStaleOrCoordinateAmbiguousRequests(string fault)
    {
        var request = new PrepareBuildRequest
        {
            BuildingItemId = 2001, BeltPathMode = BeltPathModes.NativeGeodesic,
            SourceObjectId = 754, DestinationObjectId = 755,
            ExpectedSourceStateHash = "fresh-source", ExpectedDestinationStateHash = "fresh-destination",
        };
        switch (fault)
        {
            case "single_source": request.DestinationObjectId = null; break;
            case "zero_source": request.SourceObjectId = 0; break;
            case "large_destination": request.DestinationObjectId = BeltBuildOccupancyPolicy.MaximumFactorySlots + 1; break;
            case "same": request.DestinationObjectId = 754; break;
            case "source_hash": request.ExpectedSourceStateHash = " "; break;
            case "destination_hash": request.ExpectedDestinationStateHash = null; break;
            case "position": request.PreferredPosition = Ground(0); break;
            case "path_end": request.PathEnd = Ground(10); break;
            case "resource": request.ResourceNodeId = 1; break;
            case "wrong_item": request.BuildingItemId = 2002; break;
        }
        Assert.Equal("belt_geodesic_requires_explicit_free_endpoints", BeltPathRoutingPolicy.ValidateRequest(request, true));
    }

    [Theory]
    [InlineData(1f, false)]
    [InlineData(2f, true)]
    [InlineData(29f, true)]
    [InlineData(31f, false)]
    public void NativeSnappedGroundSpanIsBounded(float x, bool supported) =>
        Assert.Equal(supported, BeltPathRoutingPolicy.ValidGroundEndpoints(Ground(0), Ground(x), 200.2f));

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(0)]
    [InlineData(201.2f)]
    public void GroundRadiusMustBeFiniteAndBothEndsActuallyOnIt(float radius) =>
        Assert.False(BeltPathRoutingPolicy.ValidGroundEndpoints(Ground(0), Ground(10), radius));

    [Theory]
    [InlineData(2, true)]
    [InlineData(245, true)]
    [InlineData(246, false)]
    [InlineData(256, false)]
    public void NativeReservationIsPartOfTheSaturationCheck(int count, bool complete)
    {
        var path = Enumerable.Range(0, count).Select(i => Ground(10f * i / (count - 1))).ToArray();
        Assert.Equal(complete, BeltPathRoutingPolicy.CompleteGroundPath(path, 256, Ground(0), Ground(10), 200.2f));
        // A true result is a completeness check, NOT native placement approval.
    }

    [Theory]
    [InlineData("last_point")]
    [InlineData("first_point")]
    [InlineData("raised_middle")]
    [InlineData("null_middle")]
    [InlineData("short_buffer")]
    public void NeverRepairAnIncompleteOrRaisedNativeResultByReplacingTheEnd(string fault)
    {
        var path = new[] { Ground(0), Ground(5), Ground(10) };
        var length = 256;
        switch (fault)
        {
            case "last_point": path[2] = Ground(9); break;
            case "first_point": path[0] = Ground(1); break;
            case "raised_middle": path[1].Y += 1.333f; break;
            case "null_middle": path[1] = null!; break;
            case "short_buffer": length = 12; break;
        }
        Assert.False(BeltPathRoutingPolicy.CompleteGroundPath(path, length, Ground(0), Ground(10), 200.2f));
    }

    [Fact]
    public void RoutingModeBindsIdenticalGeometryWithoutChangingDefaultHashes()
    {
        var grid = BeltPathRoutingPolicy.BindGeometry(BeltPathModes.NativeGrid, "same-geometry");
        var geodesic = BeltPathRoutingPolicy.BindGeometry(BeltPathModes.NativeGeodesic, "same-geometry");
        Assert.NotEqual(grid, geodesic);
        Assert.Equal(geodesic, BeltPathRoutingPolicy.BindGeometry(BeltPathModes.NativeGeodesic, "same-geometry"));
        Assert.NotEqual(geodesic, BeltPathRoutingPolicy.BindGeometry(BeltPathModes.NativeGeodesic, "changed-geometry"));
    }

    [Fact]
    public void FreeGeodesicEchoRemainsCompatibleButCannotHideAHalfCover()
    {
        var echo = new BeltPathPlanSnapshot { NativeValidationMode = "full_path_stage1", SourceBindingMode = "none" };
        Assert.True(BeltPathRoutingPolicy.ConfirmsPlanEcho(BeltPathModes.NativeGrid, echo));
        Assert.False(BeltPathRoutingPolicy.ConfirmsPlanEcho(BeltPathModes.NativeGeodesic, echo));
        echo.RoutingMode = BeltPathModes.NativeGeodesic;
        Assert.True(BeltPathRoutingPolicy.ConfirmsPlanEcho(BeltPathModes.NativeGeodesic, echo));
        Assert.False(BeltPathRoutingPolicy.ConfirmsPlanEcho(BeltPathModes.NativeGrid, echo));
        echo.ReusedSourceObjectId = 754;
        Assert.False(BeltPathRoutingPolicy.ConfirmsPlanEcho(BeltPathModes.NativeGeodesic, echo));
        echo.ReusedSourceObjectId = null;
        echo.DestinationBindingMode = "non_removing_belt_cover";
        echo.ReusedDestinationObjectId = 755;
        Assert.False(BeltPathRoutingPolicy.ConfirmsPlanEcho(BeltPathModes.NativeGeodesic, echo));
    }

    [Theory]
    [InlineData("source_mode")]
    [InlineData("source_id")]
    [InlineData("same_id")]
    [InlineData("source_preservation")]
    [InlineData("destination_mode")]
    [InlineData("destination_id")]
    [InlineData("destination_preservation")]
    [InlineData("routing")]
    public void DualCoverGeodesicEchoRequiresBothExactBoundCovers(string fault)
    {
        var echo = new BeltPathPlanSnapshot
        {
            NativeValidationMode = "full_path_stage1", RoutingMode = BeltPathModes.NativeGeodesic,
            SourceBindingMode = "non_removing_belt_cover", ReusedSourceObjectId = 754,
            SourcePreservationMode = BeltSourceRotationPolicy.PreservationMode,
            DestinationBindingMode = "non_removing_belt_cover", ReusedDestinationObjectId = 755,
            DestinationPreservationMode = BeltDestinationReusePolicy.PreservationMode,
        };
        Assert.True(BeltPathRoutingPolicy.ConfirmsPlanEcho(BeltPathModes.NativeGeodesic, echo));
        switch (fault)
        {
            case "source_mode": echo.SourceBindingMode = "none"; break;
            case "source_id": echo.ReusedSourceObjectId = 0; break;
            case "same_id": echo.ReusedDestinationObjectId = 754; break;
            case "source_preservation": echo.SourcePreservationMode = null; break;
            case "destination_mode": echo.DestinationBindingMode = "none"; break;
            case "destination_id": echo.ReusedDestinationObjectId = null; break;
            case "destination_preservation": echo.DestinationPreservationMode = "stale"; break;
            case "routing": echo.RoutingMode = BeltPathModes.NativeGrid; break;
        }
        Assert.False(BeltPathRoutingPolicy.ConfirmsPlanEcho(BeltPathModes.NativeGeodesic, echo));
    }

    private static Vector3Snapshot Ground(float x) => new() { X = x, Y = (float)Math.Sqrt(200.2 * 200.2 - x * x) };
    private static PrepareBuildRequest Request() => new()
    {
        BuildingItemId = 2001, BeltPathMode = BeltPathModes.NativeGeodesic,
        PreferredPosition = Ground(0), PathEnd = Ground(10),
    };
}
