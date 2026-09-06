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

    private static Vector3Snapshot V(float x, float y, float z) => new() { X = x, Y = y, Z = z };
}
