using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class InserterBeltAttachmentPolicyTests
{
    [Theory]
    [InlineData(80, 20, 12, 6, 20, 32, 26)]
    [InlineData(40, 0, 12, 5, 4, 12, 5)]
    [InlineData(40, 28, 12, 5, 28, 34, 33)]
    [InlineData(11, 0, 11, 4, 4, 5, 4)]
    public void UsesNativeClippedHalfOpenWindowWithOneExtraInterpolationPoint(int path, int start, int length,
        int pivotOffset, int expectedStart, int expectedEnd, int expectedPivot)
    {
        Assert.True(InserterBeltAttachmentPolicy.TryGetWindow(path, start, length, pivotOffset, out var a, out var b, out var p));
        Assert.Equal((expectedStart, expectedEnd, expectedPivot), (a, b, p));
        Assert.InRange(b, a + 1, path - 6);
    }

    [Theory]
    [InlineData(10, 0, 10, 5)] [InlineData(8193, 20, 12, 6)]
    [InlineData(80, -1, 12, 6)] [InlineData(80, 20, 0, 0)]
    [InlineData(100, 20, 65, 6)] [InlineData(80, 75, 12, 6)]
    [InlineData(80, 20, 12, -1)] [InlineData(80, 20, 12, 12)]
    [InlineData(80, int.MaxValue, 12, 6)] [InlineData(80, 20, int.MaxValue, 6)]
    [InlineData(40, 0, 12, 3)] [InlineData(40, 28, 12, 7)]
    [InlineData(40, 34, 1, 0)]
    public void RejectsInvalidOversizedOverflowOrNativePivotCorrection(int path, int start, int length, int pivotOffset)
    {
        Assert.False(InserterBeltAttachmentPolicy.TryGetWindow(path, start, length, pivotOffset, out var a, out var b, out var p));
        Assert.Equal((0, 0, 0), (a, b, p));
    }

    [Theory]
    [InlineData(-1, 1, .5f)] [InlineData(0, 1, 0)] [InlineData(-1, 0, 1)]
    public void ProjectsOntoTheActualFiniteBeltInterval(float firstX, float secondX, float expected)
    {
        Assert.True(InserterBeltAttachmentPolicy.TryProjectionFraction(V(0, 200, 0), V(0, 0, 1), V(0, 200, 4),
            V(firstX, 200, 4), V(secondX, 200, 4), out var fraction));
        Assert.Equal(expected, fraction);
    }

    [Theory]
    [InlineData(1, 2)] [InlineData(-2, -1)] [InlineData(0, 0)] [InlineData(0, .0001f)]
    public void DoesNotClampExtrapolationOrDegenerateIntervals(float firstX, float secondX)
    {
        Assert.False(InserterBeltAttachmentPolicy.TryProjectionFraction(V(0, 200, 0), V(0, 0, 1), V(0, 200, 4),
            V(firstX, 200, 4), V(secondX, 200, 4), out _));
    }

    [Theory]
    [InlineData(float.NaN)] [InlineData(float.PositiveInfinity)] [InlineData(10001)]
    public void RejectsMalformedGeometryBeforeProjection(float value) => Assert.False(
        InserterBeltAttachmentPolicy.TryProjectionFraction(V(value, 200, 0), V(0, 0, 1), V(0, 200, 4),
            V(-1, 200, 4), V(1, 200, 4), out _));

    [Fact]
    public void AScaledOrZeroSlotForwardIsNotAUnitNativePose()
    {
        foreach (var forward in new[] { V(0, 0, 2), V(0, 0, 0) })
            Assert.False(InserterBeltAttachmentPolicy.TryProjectionFraction(V(0, 200, 0), forward, V(0, 200, 4),
                V(-1, 200, 4), V(1, 200, 4), out _));
    }

    [Fact]
    public void SearchSeedIsNotFinalPlacementApproval()
    {
        var tilt = 30 * Math.PI / 180;
        var beltForward = V((float)Math.Sin(tilt), 0, -(float)Math.Cos(tilt));
        Assert.True(InserterBeltAttachmentPolicy.AcceptsSearchSeed(V(0, 200, -1), V(0, 200, 0), V(0, 0, 1), V(0, 200, 4), beltForward));
        Assert.False(NativeInserterEndpointGeometry.AcceptsStraightPair(V(0, 0, 4), V(0, 0, 1), beltForward, true));
    }

    [Fact]
    public void BackOfBuildingAndFortyFiveDegreeSeedsCannotBypassNativeSelection()
    {
        Assert.False(InserterBeltAttachmentPolicy.AcceptsSearchSeed(V(0, 200, -1), V(0, 200, 0), V(0, 0, -1), V(0, 200, 4), V(0, 0, -1)));
        var s = (float)Math.Sqrt(.5);
        Assert.False(InserterBeltAttachmentPolicy.AcceptsSearchSeed(V(0, 200, -1), V(0, 200, 0), V(0, 0, 1), V(0, 200, 4), V(s, 0, -s)));
    }

    [Theory]
    [InlineData(0, true)] [InlineData(24, true)] [InlineData(24.2, false)] [InlineData(90, false)]
    public void FinalTiltRetainsNativeTwentyFourDegreeLimit(double angle, bool expected)
    {
        var radians = angle * Math.PI / 360;
        Assert.Equal(expected, InserterBeltAttachmentPolicy.AcceptsFinalRotations(Q(),
            new QuaternionSnapshot { X = (float)Math.Sin(radians), W = (float)Math.Cos(radians) }));
    }

    [Fact]
    public void QuaternionSignIsEquivalentButInvalidNormIsNot()
    {
        Assert.True(InserterBeltAttachmentPolicy.AcceptsFinalRotations(Q(), new QuaternionSnapshot { W = -1 }));
        Assert.False(InserterBeltAttachmentPolicy.AcceptsFinalRotations(Q(), new QuaternionSnapshot()));
        Assert.False(InserterBeltAttachmentPolicy.AcceptsFinalRotations(Q(), new QuaternionSnapshot { W = float.NaN }));
    }

    [Fact]
    public void StaticGeometryHashIsDeterministicAndDoesNotBindFlowingCargo()
    {
        var fixture = new Fixture();
        Assert.Equal(fixture.Hash(), new Fixture().Hash());
        // There is intentionally no inventory/cargo or current tick in this geometry input.
        Assert.StartsWith("sha256:", fixture.Hash());
    }

    [Theory]
    [InlineData("entity")] [InlineData("belt")] [InlineData("path")] [InlineData("pathLength")]
    [InlineData("pivot")] [InlineData("position")] [InlineData("rotation")] [InlineData("tilt")]
    [InlineData("samplePosition")] [InlineData("sampleRotation")] [InlineData("lastPoint")]
    public void EveryRelevantStaticChangeInvalidatesThePrivateGeometryBinding(string change)
    {
        var f = new Fixture(); var before = f.Hash();
        switch (change)
        {
            case "entity": f.Entity++; break;
            case "belt": f.Belt++; break;
            case "path": f.Path++; break;
            case "pathLength": f.Length++; break;
            case "pivot": f.Pivot++; break;
            case "position": f.Position.X++; break;
            case "rotation": f.Rotation.W = -1; break;
            case "tilt": f.Tilt = 1; break;
            case "samplePosition": f.Points[5].X++; break;
            case "sampleRotation": f.Rotations[5].W = -1; break;
            case "lastPoint": f.Points[12].Z++; break;
        }
        Assert.NotEqual(before, f.Hash());
    }

    [Theory]
    [InlineData("missingPoint")] [InlineData("extraPoint")] [InlineData("missingRotation")]
    [InlineData("badPoint")] [InlineData("badRotation")] [InlineData("badTilt")]
    [InlineData("zeroEntity")]
    public void MalformedEvidenceDoesNotReturnAPartialHash(string change)
    {
        var f = new Fixture();
        switch (change)
        {
            case "missingPoint": f.Points.RemoveAt(0); break;
            case "extraPoint": f.Points.Add(V(0, 200, 4)); break;
            case "missingRotation": f.Rotations.RemoveAt(0); break;
            case "badPoint": f.Points[0].X = float.NaN; break;
            case "badRotation": f.Rotations[0].W = 2; break;
            case "badTilt": f.Tilt = float.PositiveInfinity; break;
            case "zeroEntity": f.Entity = 0; break;
        }
        Assert.False(f.TryHash(out var hash)); Assert.Empty(hash);
    }

    [Fact]
    public void BothSignedOffsetsAndGeometryParticipateInBuildFingerprint()
    {
        var baseline = InserterBeltAttachmentPolicy.BindOffsets(0, 0, null);
        Assert.NotEqual(baseline, InserterBeltAttachmentPolicy.BindOffsets(1, 0, null));
        Assert.NotEqual(baseline, InserterBeltAttachmentPolicy.BindOffsets(0, -1, null));
        Assert.NotEqual(baseline, InserterBeltAttachmentPolicy.BindOffsets(0, 0, "geometry"));
        Assert.Equal(baseline, InserterBeltAttachmentPolicy.BindOffsets(0, 0, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => InserterBeltAttachmentPolicy.BindOffsets(int.MaxValue, 0, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => InserterBeltAttachmentPolicy.BindOffsets(0, int.MinValue, null));
    }

    private static Vector3Snapshot V(float x, float y, float z) => new() { X = x, Y = y, Z = z };
    private static QuaternionSnapshot Q() => new() { W = 1 };
    private sealed class Fixture
    {
        internal int Entity = 1, Belt = 2, Path = 3, Length = 80, Pivot = 6;
        internal float Tilt;
        internal Vector3Snapshot Position = V(0, 200, 4);
        internal QuaternionSnapshot Rotation = Q();
        internal List<Vector3Snapshot> Points = Enumerable.Range(0, 13).Select(i => V(i * .1f, 200, 4)).ToList();
        internal List<QuaternionSnapshot> Rotations = Enumerable.Range(0, 13).Select(_ => Q()).ToList();
        internal bool TryHash(out string hash) => InserterBeltAttachmentPolicy.TryGeometryHash(Entity, Belt, Path, Length,
            20, 12, Pivot, Position, Rotation, Tilt, Points, Rotations, out hash);
        internal string Hash() { Assert.True(TryHash(out var hash)); return hash; }
    }
}
