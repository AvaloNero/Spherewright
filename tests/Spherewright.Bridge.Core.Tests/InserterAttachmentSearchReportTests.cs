using System.Globalization;
using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class InserterAttachmentSearchReportTests
{
    [Fact]
    public void NoCandidateEvidenceDoesNotClaimAnAngleOrNativePlacementTest()
    {
        var report = new InserterAttachmentSearchReport();
        report.RecordSeed(false);
        Assert.Equal("no_admissible_seed", report.FailureStage);
        Assert.Null(report.BestFacingDegrees);
        Assert.Contains("bestFacingDegrees=unknown", report.DescribeFailure());
        Assert.Contains("candidateChecks=0", report.DescribeFailure());
    }

    [Fact]
    public void AdmittedSeedWithoutProjectionHasItsOwnStage()
    {
        var report = new InserterAttachmentSearchReport();
        report.RecordSeed(true);
        Assert.Equal("no_finite_projection", report.FailureStage);
        Assert.Equal(1, report.AdmittedSeeds);
        Assert.Equal(0, report.Projections);
    }

    [Fact]
    public void ReportsBestActualDeviationRatherThanTheLastRejectedPort()
    {
        var report = new InserterAttachmentSearchReport();
        report.RecordSeed(true);
        report.RecordProjection(30.25);
        report.RecordProjection(14.75);
        report.RecordProjection(170);
        Assert.Equal("no_facing_interpolated_pair", report.FailureStage);
        Assert.Equal(14.75, report.BestFacingDegrees);
        Assert.Equal(3, report.Projections);
        Assert.Contains("bestFacingDegrees=14.750", report.DescribeFailure());
    }

    [Theory]
    [InlineData(double.NaN)] [InlineData(double.PositiveInfinity)] [InlineData(-1)] [InlineData(181)]
    public void InvalidDeviationIsUnknownNotAnInventedZero(double value)
    {
        var report = new InserterAttachmentSearchReport();
        report.RecordProjection(value);
        Assert.Null(report.BestFacingDegrees);
        Assert.Equal(1, report.Projections);
    }

    [Fact]
    public void DistinguishesTiltRejectionFromCandidateValidatorInvocation()
    {
        var report = new InserterAttachmentSearchReport();
        report.RecordProjection(5);
        report.RecordFacingPair();
        report.RecordTiltRejection();
        Assert.Equal("native_tilt_rejected", report.FailureStage);
        Assert.Equal(0, report.CandidateChecks);
        Assert.True(report.TryRecordCandidateCheck());
        Assert.Equal("candidate_validation_rejected", report.FailureStage);
    }

    [Fact]
    public void CountsOnlyAllowedCandidateChecksAndNeverExceedsExistingBudget()
    {
        var report = new InserterAttachmentSearchReport();
        for (var i = 0; i < InserterBeltAttachmentPolicy.MaximumNativeCandidates; i++)
            Assert.True(report.TryRecordCandidateCheck());
        Assert.False(report.TryRecordCandidateCheck());
        Assert.False(report.TryRecordCandidateCheck());
        Assert.Equal(64, report.CandidateChecks);
        Assert.Equal("candidate_limit", report.FailureStage);
    }

    [Fact]
    public void RenderingIsCultureIndependentAndRecoveryDoesNotRecommendBlindMovement()
    {
        var old = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var report = new InserterAttachmentSearchReport();
            report.RecordProjection(12.25);
            Assert.Contains("bestFacingDegrees=12.250", report.DescribeFailure());
            Assert.Contains("geometry_unavailable is not proof of bad angles", InserterAttachmentSearchReport.Recovery);
            Assert.Contains("Do not repeat the same endpoint pair unchanged", InserterAttachmentSearchReport.Recovery);
            Assert.Contains("Only an explicit native OutOfReach", InserterAttachmentSearchReport.Recovery);
        }
        finally { CultureInfo.CurrentCulture = old; }
    }

    [Theory]
    [InlineData(0, true, true)] [InlineData(10, true, true)]
    [InlineData(12, false, true)] [InlineData(15, false, false)] [InlineData(30, false, false)]
    public void ExposingDeviationKeepsExistingStraightSubsetDecisions(double degrees, bool belt, bool device)
    {
        var radians = degrees * Math.PI / 180;
        var direction = new Vector3Snapshot { Z = 1 };
        var source = new Vector3Snapshot { X = (float)Math.Sin(radians), Z = (float)Math.Cos(radians) };
        var destination = new Vector3Snapshot { Z = -1 };
        Assert.True(NativeInserterEndpointGeometry.TryGetStraightPairDeviation(direction, source, destination, out var actual));
        Assert.InRange(actual, Math.Max(0, degrees - .0001), degrees + .0001);
        Assert.Equal(belt, NativeInserterEndpointGeometry.AcceptsStraightPair(direction, source, destination, true));
        Assert.Equal(device, NativeInserterEndpointGeometry.AcceptsStraightPair(direction, source, destination, false));
    }
}
