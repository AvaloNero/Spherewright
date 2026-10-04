using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class NativeInserterEndpointGeometryTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void FacingStraightSlotsRemainAvailable(bool belt) => Assert.True(
        NativeInserterEndpointGeometry.AcceptsStraightPair(V(10, 0, 0), V(1, 0, 0), V(-1, 0, 0), belt));

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)] [InlineData(false, true)] [InlineData(true, true)]
    public void BackwardsSlotPairsDoNotBecomeBuildableBySkippingNativeSelection(bool sourceBackwards, bool belt)
    {
        Assert.False(NativeInserterEndpointGeometry.AcceptsStraightPair(V(1, 0, 0),
            V(sourceBackwards ? -1 : 1, 0, 0), V(sourceBackwards ? -1 : 1, 0, 0), belt));
    }

    [Theory]
    [InlineData(false, 13, true)] [InlineData(false, 15, false)]
    [InlineData(true, 10, true)] [InlineData(true, 12, false)]
    public void MirrorsTheSupportedNativeStraightThreshold(bool belt, double degrees, bool expected)
    {
        var radians = degrees * Math.PI / 180d;
        Assert.Equal(expected, NativeInserterEndpointGeometry.AcceptsStraightPair(V(1, 0, 0),
            V((float)Math.Cos(radians), 0, (float)Math.Sin(radians)), V(-1, 0, 0), belt));
    }

    [Fact]
    public void ChecksRelativeSlotOppositionNotOnlyIndividualFacing()
    {
        var angle = 10d * Math.PI / 180d;
        Assert.False(NativeInserterEndpointGeometry.AcceptsStraightPair(V(1, 0, 0),
            V((float)Math.Cos(angle), 0, (float)Math.Sin(angle)),
            V(-(float)Math.Cos(angle), 0, (float)Math.Sin(angle)), false));
    }

    [Theory]
    [InlineData(float.NaN)] [InlineData(float.PositiveInfinity)] [InlineData(10001)]
    public void RejectsInvalidGeometryBeforeNativeCalls(float x) => Assert.False(
        NativeInserterEndpointGeometry.AcceptsStraightPair(V(x, 0, 0), V(1, 0, 0), V(-1, 0, 0), false));

    [Fact]
    public void CoincidentAndZeroForwardVectorsAreNotValidSlots()
    {
        Assert.False(NativeInserterEndpointGeometry.AcceptsStraightPair(V(0, 0, 0), V(1, 0, 0), V(-1, 0, 0), false));
        Assert.False(NativeInserterEndpointGeometry.AcceptsStraightPair(V(1, 0, 0), V(0, 0, 0), V(-1, 0, 0), false));
    }

    [Theory]
    [InlineData(0, false)] [InlineData(1, false)] [InlineData(2, false)] [InlineData(3, true)]
    public void PlannedSourceBeltFacesBackTowardExistingSourceBeforeEndTransform(int quarter, bool expected)
    {
        // Captured positions/direction are an offline fixture, not native site approval.
        var existing = V(-174.17984f, -3.773458f, -98.62649f);
        var planned = V(-172.926758f, -3.773458f, -100.807449f);
        var existingOut = V(.4927274f, -.00000302493572f, -.870183647f);
        // BuildStepPlan.Inserter stores destinationPose * EulerY(180), so
        // its Rotation2 * back equals destinationPose * forward, NOT raw pose.back.
        Assert.Equal(expected, NativeInserterEndpointGeometry.AcceptsStraightPair(
            V(planned.X - existing.X, planned.Y - existing.Y, planned.Z - existing.Z),
            existingOut, SphericalQuarterOutward(planned, quarter), true));
    }

    [Theory]
    [InlineData(0, false)] [InlineData(1, false)] [InlineData(2, true)] [InlineData(3, false)]
    public void PlannedDestinationBeltFacesTowardExistingDestination(int quarter, bool expected)
    {
        var planned = V(-161.072893f, -8.618379f, -118.580297f);
        var existing = V(-160.96492f, -11.31501f, -118.500214f);
        Assert.Equal(expected, NativeInserterEndpointGeometry.AcceptsStraightPair(
            V(existing.X - planned.X, existing.Y - planned.Y, existing.Z - planned.Z),
            SphericalQuarterOutward(planned, quarter), V(-.04540962f, .9984014f, -.0336502343f), true));
    }

    // Current DLL's nonpolar SphericalRotation(pos, 0) tangent basis.
    private static Vector3Snapshot SphericalQuarterOutward(Vector3Snapshot position, int quarter)
    {
        var up = Unit(position);
        var right = Unit(V(-up.Z, 0, up.X));
        var forward = Unit(V(right.Y * up.Z - right.Z * up.Y,
            right.Z * up.X - right.X * up.Z, right.X * up.Y - right.Y * up.X));
        return quarter switch { 0 => forward, 1 => right,
            2 => V(-forward.X, -forward.Y, -forward.Z), 3 => V(-right.X, -right.Y, -right.Z),
            _ => throw new ArgumentOutOfRangeException(nameof(quarter)) };
    }

    private static Vector3Snapshot Unit(Vector3Snapshot value)
    {
        var length = MathF.Sqrt(value.X * value.X + value.Y * value.Y + value.Z * value.Z);
        return V(value.X / length, value.Y / length, value.Z / length);
    }

    private static Vector3Snapshot V(float x, float y, float z) => new() { X = x, Y = y, Z = z };
}
