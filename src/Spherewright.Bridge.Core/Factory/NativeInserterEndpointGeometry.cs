using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

// Conservative straight subset of current native DeterminePreviews pose selection.
// CheckBuildConditions alone does not recreate the preceding TooSkew decision.
public static class NativeInserterEndpointGeometry
{
    public static bool AcceptsStraightPair(Vector3Snapshot direction, Vector3Snapshot sourceOutward,
        Vector3Snapshot destinationOutward, bool touchesBelt)
    {
        if (!Valid(direction) || !Valid(sourceOutward) || !Valid(destinationOutward)) return false;
        var limit = touchesBelt ? 11d : 14d;
        var sourceAngle = Angle(direction, sourceOutward);
        var destinationAngle = 180d - Angle(direction, destinationOutward);
        var oppositionError = 180d - Angle(sourceOutward, destinationOutward);
        return Math.Max(sourceAngle, Math.Max(destinationAngle, oppositionError)) < limit;
    }

    private static double Angle(Vector3Snapshot a, Vector3Snapshot b) =>
        Math.Acos(Math.Max(-1d, Math.Min(1d, Dot(a, b) / Math.Sqrt(Dot(a, a) * Dot(b, b))))) * (180d / Math.PI);
    private static double Dot(Vector3Snapshot a, Vector3Snapshot b) => (double)a.X * b.X + (double)a.Y * b.Y + (double)a.Z * b.Z;
    private static bool Valid(Vector3Snapshot? v) => v is not null
        && new[] { v.X, v.Y, v.Z }.All(x => !float.IsNaN(x) && !float.IsInfinity(x) && Math.Abs(x) <= 10000)
        && Dot(v, v) > 1e-12;
}
