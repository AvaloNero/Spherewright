using Spherewright.Bridge.Core.Safety;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class ResourceCoverageSelectionTests
{
    [Fact]
    public void SelectBestIndex_PrefersMostCoveredNodesBeforeDistance()
    {
        var selected = ResourceCoverageSelection.SelectBestIndex(new[]
        {
            new ResourceCoverageCandidateScore { Index = 0, CoveredNodeCount = 2, DistanceToBoundNode = 3.5, Yaw = 0 },
            new ResourceCoverageCandidateScore { Index = 1, CoveredNodeCount = 5, DistanceToBoundNode = 7.2, Yaw = 180 },
            new ResourceCoverageCandidateScore { Index = 2, CoveredNodeCount = 4, DistanceToBoundNode = 4.5, Yaw = 90 },
        });

        Assert.Equal(1, selected);
    }

    [Fact]
    public void SelectBestIndex_UsesStableDistanceYawAndIndexTieBreakers()
    {
        var selected = ResourceCoverageSelection.SelectBestIndex(new[]
        {
            new ResourceCoverageCandidateScore { Index = 7, CoveredNodeCount = 5, DistanceToBoundNode = 5.5, Yaw = 45 },
            new ResourceCoverageCandidateScore { Index = 3, CoveredNodeCount = 5, DistanceToBoundNode = 4.5, Yaw = 90 },
            new ResourceCoverageCandidateScore { Index = 2, CoveredNodeCount = 5, DistanceToBoundNode = 4.5, Yaw = 30 },
        });

        Assert.Equal(2, selected);
    }
}
