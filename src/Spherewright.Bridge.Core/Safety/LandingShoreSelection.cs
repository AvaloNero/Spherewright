namespace Spherewright.Bridge.Core.Safety;

public sealed class LandingShoreCandidateScore
{
    public int Index { get; set; }

    public double SurfaceDistance { get; set; }

    public double TerrainClearance { get; set; }
}

public static class LandingShoreSelection
{
    public static bool IsEligible(
        LandingShoreCandidateScore candidate,
        double minimumDistance,
        double maximumDistance,
        double minimumTerrainClearance)
    {
        if (candidate is null)
        {
            throw new ArgumentNullException(nameof(candidate));
        }

        return IsFinite(candidate.SurfaceDistance)
               && IsFinite(candidate.TerrainClearance)
               && candidate.SurfaceDistance >= minimumDistance
               && candidate.SurfaceDistance <= maximumDistance
               && candidate.TerrainClearance >= minimumTerrainClearance;
    }

    public static bool IsPreferred(
        LandingShoreCandidateScore candidate,
        LandingShoreCandidateScore? current)
    {
        if (candidate is null)
        {
            throw new ArgumentNullException(nameof(candidate));
        }
        if (current is null)
        {
            return true;
        }

        var distanceComparison = candidate.SurfaceDistance.CompareTo(current.SurfaceDistance);
        if (distanceComparison != 0)
        {
            return distanceComparison < 0;
        }

        var clearanceComparison = candidate.TerrainClearance.CompareTo(current.TerrainClearance);
        return clearanceComparison != 0
            ? clearanceComparison > 0
            : candidate.Index < current.Index;
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
