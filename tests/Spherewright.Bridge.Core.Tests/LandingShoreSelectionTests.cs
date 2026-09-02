using Spherewright.Bridge.Core.Safety;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class LandingShoreSelectionTests
{
    [Theory]
    [InlineData(0.99, 1.0, 120.0, 0.2, false)]
    [InlineData(1.0, 1.0, 120.0, 0.2, true)]
    [InlineData(120.0, 1.0, 120.0, 0.2, true)]
    [InlineData(120.01, 1.0, 120.0, 0.2, false)]
    [InlineData(8.0, 1.0, 120.0, 0.19, false)]
    public void IsEligible_EnforcesBoundedDistanceAndDryClearance(
        double distance,
        double minimumDistance,
        double maximumDistance,
        double clearance,
        bool expected)
    {
        var candidate = new LandingShoreCandidateScore
        {
            Index = 1,
            SurfaceDistance = distance,
            TerrainClearance = clearance,
        };

        Assert.Equal(
            expected,
            LandingShoreSelection.IsEligible(
                candidate,
                minimumDistance,
                maximumDistance,
                minimumTerrainClearance: 0.2));
    }

    [Fact]
    public void IsEligible_RejectsNonFiniteTerrainEvidence()
    {
        var candidate = new LandingShoreCandidateScore
        {
            Index = 1,
            SurfaceDistance = double.NaN,
            TerrainClearance = 1.0,
        };

        Assert.False(LandingShoreSelection.IsEligible(candidate, 1.0, 120.0, 0.2));

        candidate.SurfaceDistance = 8.0;
        candidate.TerrainClearance = double.PositiveInfinity;
        Assert.False(LandingShoreSelection.IsEligible(candidate, 1.0, 120.0, 0.2));
    }

    [Fact]
    public void IsPreferred_UsesNearestThenDriestThenStableIndex()
    {
        var current = new LandingShoreCandidateScore
        {
            Index = 7,
            SurfaceDistance = 9.0,
            TerrainClearance = 0.8,
        };

        Assert.True(LandingShoreSelection.IsPreferred(
            new LandingShoreCandidateScore { Index = 9, SurfaceDistance = 8.0, TerrainClearance = 0.2 },
            current));
        Assert.True(LandingShoreSelection.IsPreferred(
            new LandingShoreCandidateScore { Index = 9, SurfaceDistance = 9.0, TerrainClearance = 1.0 },
            current));
        Assert.True(LandingShoreSelection.IsPreferred(
            new LandingShoreCandidateScore { Index = 3, SurfaceDistance = 9.0, TerrainClearance = 0.8 },
            current));
        Assert.False(LandingShoreSelection.IsPreferred(
            new LandingShoreCandidateScore { Index = 8, SurfaceDistance = 10.0, TerrainClearance = 2.0 },
            current));
    }
}
