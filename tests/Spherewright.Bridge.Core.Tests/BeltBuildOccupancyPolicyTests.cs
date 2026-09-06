using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class BeltBuildOccupancyPolicyTests
{
    private static Vector3Snapshot P(float x, float y = 200, float z = 0) => new() { X = x, Y = y, Z = z };
    private static Vector3Snapshot[] Path() => new[] { P(0), P(1), P(2) };
    private static BeltBuildObstacle B(int id, float x, float y = 200, float z = 0) => new(id, P(x, y, z));

    [Fact]
    public void ClearPathIsOnlyAnOccupancyPassAndDoesNotChangeInputs()
    {
        var path = Path(); var obstacles = new[] { B(92, 10), B(-92, 12) };
        Assert.True(BeltBuildOccupancyPolicy.TryValidateNewPath(path, obstacles, out var failure));
        Assert.Null(failure);
        Assert.Equal(new float[] { 0, 1, 2 }, path.Select(p => p.X));
        Assert.Equal(new[] { 92, -92 }, obstacles.Select(p => p.ObjectId));
    }

    [Theory]
    [InlineData(0, 92)] // The old source is NOT an exception.
    [InlineData(1, 93)] // Every intermediate point, not just the two ends.
    [InlineData(2, 94)] // Neither is the old destination.
    [InlineData(1, -93)] // Signed prebuild identity; a positive entity is not required.
    public void EveryNewPointRejectsExistingBelts(int pointIndex, int objectId)
    {
        Assert.False(BeltBuildOccupancyPolicy.TryValidateNewPath(Path(), new[] { B(objectId, pointIndex) }, out var failure));
        Assert.Equal("belt_path_existing_overlap", failure!.Reason);
        Assert.Equal(pointIndex, failure.PlannedIndex);
        Assert.Equal(objectId, failure.ObjectId);
    }

    [Theory]
    [InlineData(-.25f, false)]
    [InlineData(.25f, false)]
    [InlineData(-.251f, true)]
    [InlineData(.251f, true)]
    public void ConservativeBoundaryUsesAllNeighbourCells(float offset, bool expected)
    {
        Assert.Equal(expected, BeltBuildOccupancyPolicy.TryValidateNewPath(Path(), new[] { B(10, offset) }, out _));
    }

    [Theory]
    [InlineData(-.01f, .01f)]
    [InlineData(.24f, .26f)]
    [InlineData(-.24f, -.26f)]
    public void BucketEdgesNeverHideCoincidence(float proposed, float existing)
    {
        Assert.False(BeltBuildOccupancyPolicy.TryValidateNewPath(new[] { P(proposed), P(2) },
            new[] { B(10, existing) }, out var failure));
        Assert.Equal("belt_path_existing_overlap", failure!.Reason);
    }

    [Fact]
    public void VerticalSeparationIsThreeDimensionalNotAFlatMap()
    {
        Assert.True(BeltBuildOccupancyPolicy.TryValidateNewPath(Path(), new[] { B(10, 1, 201.333f) }, out _));
    }

    [Fact]
    public void SelfOverlapRejectsEvenWithNoFactoryObjects()
    {
        Assert.False(BeltBuildOccupancyPolicy.TryValidateNewPath(new[] { P(0), P(1), P(0) },
            Array.Empty<BeltBuildObstacle>(), out var failure));
        Assert.Equal("belt_path_self_overlap", failure!.Reason);
        Assert.Equal(2, failure.PlannedIndex);
        Assert.Equal(0, failure.ObjectId);
    }

    [Fact]
    public void UnrelatedLegacyCoincidentBeltsAreNeitherRepairedNorUsedToBlockAClearSite()
    {
        Assert.True(BeltBuildOccupancyPolicy.TryValidateNewPath(Path(), new[] { B(2202, 10), B(2205, 10) }, out _));
    }

    [Fact]
    public void ANewEntityOrPrebuildBetweenPrepareAndCommitInvalidatesTheSite()
    {
        var planned = Path();
        Assert.True(BeltBuildOccupancyPolicy.TryValidateNewPath(planned, Array.Empty<BeltBuildObstacle>(), out _));
        Assert.False(BeltBuildOccupancyPolicy.TryValidateNewPath(planned, new[] { B(-10, 1) }, out _));
        Assert.False(BeltBuildOccupancyPolicy.TryValidateNewPath(planned, new[] { B(11, 1) }, out _));
    }

    [Fact]
    public void APathAdjustedByNativeCheckingMustBeCheckedAgain()
    {
        var obstacles = new[] { B(10, 1, 201.333f) };
        Assert.True(BeltBuildOccupancyPolicy.TryValidateNewPath(Path(), obstacles, out _));
        Assert.False(BeltBuildOccupancyPolicy.TryValidateNewPath(new[] { P(0), P(1, 201.333f), P(2) }, obstacles, out _));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(257)]
    public void PathSizeIsBounded(int count)
    {
        Assert.False(BeltBuildOccupancyPolicy.TryValidateNewPath(Enumerable.Range(0, count).Select(i => P(i)).ToArray(),
            Array.Empty<BeltBuildObstacle>(), out var failure));
        Assert.Equal("belt_occupancy_bounds_unavailable", failure!.Reason);
    }

    [Fact]
    public void SupportsTheBoundedPurePolicyMaximum()
    {
        Assert.True(BeltBuildOccupancyPolicy.TryValidateNewPath(
            Enumerable.Range(0, 256).Select(i => P(i)).ToArray(), Array.Empty<BeltBuildObstacle>(), out _));
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    [InlineData(10001)]
    public void InvalidCoordinatesFailClosed(float x)
    {
        Assert.False(BeltBuildOccupancyPolicy.TryValidateNewPath(new[] { P(x), P(1) }, Array.Empty<BeltBuildObstacle>(), out _));
        Assert.False(BeltBuildOccupancyPolicy.TryValidateNewPath(Path(), new[] { B(10, x) }, out _));
    }

    [Fact]
    public void MissingAndOriginEvidenceIsNotAssumedClear()
    {
        Assert.False(BeltBuildOccupancyPolicy.TryValidateNewPath(null!, Array.Empty<BeltBuildObstacle>(), out _));
        Assert.False(BeltBuildOccupancyPolicy.TryValidateNewPath(Path(), null!, out _));
        Assert.False(BeltBuildOccupancyPolicy.TryValidateNewPath(new[] { null!, P(1) }, Array.Empty<BeltBuildObstacle>(), out _));
        Assert.False(BeltBuildOccupancyPolicy.TryValidateNewPath(new[] { P(0, 0), P(1) }, Array.Empty<BeltBuildObstacle>(), out _));
        Assert.False(BeltBuildOccupancyPolicy.TryValidateNewPath(Path(), new[] { new BeltBuildObstacle(10, null!) }, out _));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(int.MinValue)]
    [InlineData(65537)]
    [InlineData(-65537)]
    public void InvalidObjectIdentityIsNotSilentlySkipped(int id)
    {
        Assert.False(BeltBuildOccupancyPolicy.TryValidateNewPath(Path(), new[] { B(id, 10) }, out var failure));
        Assert.Equal("belt_occupancy_evidence_invalid", failure!.Reason);
    }

    [Fact]
    public void DuplicateIdsAndOversizedEvidenceFailClosed()
    {
        Assert.False(BeltBuildOccupancyPolicy.TryValidateNewPath(Path(), new[] { B(10, 10), B(10, 11) }, out _));
        Assert.False(BeltBuildOccupancyPolicy.TryValidateNewPath(Path(), new BeltBuildObstacle[65537], out var failure));
        Assert.Equal("belt_occupancy_bounds_unavailable", failure!.Reason);
    }

    [Fact]
    public void DenseAdversarialNeighbourhoodHasAnExplicitWorkLimit()
    {
        var crowded = Enumerable.Range(1, 40000).Select(id => B(id, .251f, 200.251f, .251f)).ToArray();
        Assert.False(BeltBuildOccupancyPolicy.TryValidateNewPath(new[] { P(0), P(.5f, 200.5f, .5f) }, crowded, out var failure));
        Assert.Equal("belt_occupancy_comparison_limit", failure!.Reason);
    }
}
