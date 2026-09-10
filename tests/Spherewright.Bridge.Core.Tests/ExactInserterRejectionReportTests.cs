using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class ExactInserterRejectionReportTests
{
    [Fact]
    public void LaterAngleFailuresCannotHideAnActualNativeRejection()
    {
        var report = new ExactInserterRejectionReport();
        report.Record("TooSkew", false);
        report.Record("DSP inserter validation returned TooClose.", true);
        report.Record("TooSkew", false);
        Assert.Equal(3, report.Attempts);
        Assert.Equal(1, report.NativeChecks);
        Assert.Contains("lastNativeRejection=DSP inserter validation returned TooClose.", report.DescribeFailure());
        Assert.Contains("lastPreNativeRejection=TooSkew", report.DescribeFailure());
    }

    [Fact]
    public void AngleOnlyFailureDoesNotPretendNativePlacementWasChecked()
    {
        var report = new ExactInserterRejectionReport();
        report.Record("TooSkew", false);
        Assert.Equal(0, report.NativeChecks);
        Assert.Contains("lastNativeRejection=none", report.DescribeFailure());
        Assert.Contains("nativeChecks=0", report.DescribeFailure());
    }

    [Fact]
    public void CountsEveryActualNativeCheckButRetainsOnlyBoundedLastReasons()
    {
        var report = new ExactInserterRejectionReport();
        for (var i = 0; i < 256; i++)
            report.Record(i == 255 ? "Collide" : "TooSkew", i % 2 == 1);
        Assert.Equal(256, report.Attempts);
        Assert.Equal(128, report.NativeChecks);
        Assert.Contains("lastNativeRejection=Collide", report.DescribeFailure());
        Assert.True(report.DescribeFailure().Length < 160);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DoesNotRetainUnboundedReasonText(bool native)
    {
        var report = new ExactInserterRejectionReport();
        report.Record(new string('x', 2000), native);
        Assert.Contains(new string('x', ExactInserterRejectionReport.MaximumReasonCharacters), report.DescribeFailure());
        Assert.DoesNotContain(new string('x', ExactInserterRejectionReport.MaximumReasonCharacters + 1), report.DescribeFailure());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void MissingReasonIsUnknownNotSuccessful(string? reason)
    {
        var report = new ExactInserterRejectionReport();
        report.Record(reason, true);
        Assert.Contains("lastNativeRejection=unknown", report.DescribeFailure());
    }

    [Theory]
    [InlineData(.541458f, -1.65405f, .079645f, 6.72)]
    [InlineData(.536026f, -1.65405f, .07649f, 6.73)]
    public void LiveFacingPairsStillOnlyPassGeometryNotNativePlacement(float x, float y, float z, double expected)
    {
        var direction = V(x, y, z);
        var source = V(.2542881f, -.9560288f, .146104664f);
        var destination = V(-.254561573f, .9556029f, -.1483964f);
        Assert.True(NativeInserterEndpointGeometry.TryGetStraightPairDeviation(direction, source, destination, out var angle));
        Assert.InRange(angle, expected - .03, expected + .03);
        Assert.True(NativeInserterEndpointGeometry.AcceptsStraightPair(direction, source, destination, false));
        var report = new ExactInserterRejectionReport();
        report.Record("native outcome not observed", false);
        Assert.Equal(0, report.NativeChecks);
    }

    private static Vector3Snapshot V(float x, float y, float z) => new() { X = x, Y = y, Z = z };
}
