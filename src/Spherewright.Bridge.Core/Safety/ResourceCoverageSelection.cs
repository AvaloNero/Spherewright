namespace Spherewright.Bridge.Core.Safety;

public sealed class ResourceCoverageCandidateScore
{
    public int Index { get; set; }

    public int CoveredNodeCount { get; set; }

    public double DistanceToBoundNode { get; set; }

    public double Yaw { get; set; }
}

public static class ResourceCoverageSelection
{
    public static int SelectBestIndex(IReadOnlyList<ResourceCoverageCandidateScore> candidates)
    {
        if (candidates is null || candidates.Count == 0)
        {
            return -1;
        }

        return candidates
            .OrderByDescending(candidate => candidate.CoveredNodeCount)
            .ThenBy(candidate => candidate.DistanceToBoundNode)
            .ThenBy(candidate => candidate.Yaw)
            .ThenBy(candidate => candidate.Index)
            .First()
            .Index;
    }
}
