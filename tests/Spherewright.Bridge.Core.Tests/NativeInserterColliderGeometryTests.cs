using Spherewright.Bridge.Core.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class NativeInserterColliderGeometryTests
{
    [Theory]
    [InlineData(false, false, 0, 2)]
    [InlineData(true, false, -.35f, 2.35f)]
    [InlineData(false, true, .35f, 2.35f)]
    [InlineData(true, true, 0, 2.7f)]
    public void MatchesNativeBoxFormulaAndBeltEndOffsets(bool input, bool output, float offset, float extent)
    {
        var actual = NativeInserterColliderGeometry.Calculate(4, .5f, input, output);
        Assert.Equal(offset, actual.CentreOffsetZ, 4); Assert.Equal(extent, actual.HalfLength, 4);
    }
    [Fact]
    public void NativeMinimumIsPreserved() => Assert.Equal(.1f, NativeInserterColliderGeometry.Calculate(0, 0, false, false).HalfLength);
    [Theory]
    [InlineData(float.NaN)] [InlineData(float.PositiveInfinity)] [InlineData(-1)] [InlineData(65)]
    public void InvalidDimensionsFailClosed(float span) => Assert.Throws<ArgumentOutOfRangeException>(() => NativeInserterColliderGeometry.Calculate(span, .5f, false, false));
}
