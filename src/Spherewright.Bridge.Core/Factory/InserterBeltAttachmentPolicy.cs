using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

/// <summary>Bounds and evidence for one explicitly selected native belt segment.
/// This is not a path finder, cargo reader, placement approval or write primitive.</summary>
public static class InserterBeltAttachmentPolicy
{
    public const int MaximumSegmentCells = 64;
    public const int MaximumNativeCandidates = 64;

    public static bool TryGetWindow(int pathLength, int segmentStart, int segmentLength, int pivotOffset,
        out int start, out int endExclusive, out int pivot)
    {
        start = endExclusive = pivot = 0;
        if (pathLength < 11 || pathLength > BeltUpgradePathPolicy.MaximumPathCells
            || segmentStart < 0 || segmentLength < 1 || segmentLength > MaximumSegmentCells
            || (long)segmentStart + segmentLength > pathLength
            || pivotOffset < 0 || pivotOffset >= segmentLength) return false;
        var nativePivot = segmentStart + pivotOffset;
        // Native preview clamps the pivot, while SetInserter*Target clamps pivot+offset.
        // Reject an already-clamped pivot instead of silently moving the attachment.
        if (nativePivot < 4 || nativePivot > pathLength - 6) return false;
        var first = Math.Max(4, Math.Min(pathLength - 6, segmentStart));
        var end = Math.Max(4, Math.Min(pathLength - 6, segmentStart + segmentLength));
        if (end <= first) return false;
        start = first; endExclusive = end; pivot = nativePivot;
        return true;
    }

    public static bool TryGeometryHash(int entityId, int beltId, int pathId, int pathLength,
        int segmentStart, int segmentLength, int pivotOffset, Vector3Snapshot beltPosition,
        QuaternionSnapshot beltRotation, float beltTilt, IReadOnlyList<Vector3Snapshot> positions,
        IReadOnlyList<QuaternionSnapshot> rotations, out string hash)
    {
        hash = string.Empty;
        if (entityId <= 0 || beltId <= 0 || pathId <= 0
            || !TryGetWindow(pathLength, segmentStart, segmentLength, pivotOffset, out var start, out var end, out _)
            || positions is null || rotations is null || positions.Count != end - start + 1
            || rotations.Count != positions.Count || !ValidPosition(beltPosition) || !ValidRotation(beltRotation)
            || !Finite(beltTilt) || Math.Abs(beltTilt) > 180) return false;
        var fields = new List<object?>
        {
            entityId, beltId, pathId, pathLength, segmentStart, segmentLength, pivotOffset, start, end,
            beltPosition.X, beltPosition.Y, beltPosition.Z,
            beltRotation.X, beltRotation.Y, beltRotation.Z, beltRotation.W, beltTilt, positions.Count,
        };
        for (var i = 0; i < positions.Count; i++)
        {
            var p = positions[i]; var q = rotations[i];
            if (!ValidPosition(p) || !ValidRotation(q)) return false;
            fields.Add(p.X); fields.Add(p.Y); fields.Add(p.Z);
            fields.Add(q.X); fields.Add(q.Y); fields.Add(q.Z); fields.Add(q.W);
        }
        hash = CanonicalStateHash.Combine("inserter-belt-geometry-v1", fields.ToArray());
        return true;
    }

    // Same finite line projection as DeterminePreviews/Kit.ClosestPoint2Straight.
    // The Plugin uses the native interpolation and quaternion operations afterwards.
    public static bool TryProjectionFraction(Vector3Snapshot devicePosition, Vector3Snapshot deviceForward,
        Vector3Snapshot currentBeltPosition, Vector3Snapshot first, Vector3Snapshot second, out float fraction)
    {
        fraction = 0;
        if (!ValidPosition(devicePosition) || !ValidPosition(currentBeltPosition)
            || !ValidPosition(first) || !ValidPosition(second) || !ValidDirection(deviceForward)) return false;
        var distance = Dot(Subtract(currentBeltPosition, devicePosition), deviceForward);
        var projected = new Vector3Snapshot
        {
            X = devicePosition.X + deviceForward.X * distance,
            Y = devicePosition.Y + deviceForward.Y * distance,
            Z = devicePosition.Z + deviceForward.Z * distance,
        };
        var delta = Subtract(second, first);
        var lengthSquared = Dot(delta, delta);
        if (!Finite(lengthSquared) || lengthSquared < 1e-6f) return false;
        var t = Dot(Subtract(projected, first), delta) / lengthSquared;
        if (!Finite(t) || t < 0 || t > 1) return false;
        fraction = t;
        return true;
    }

    public static bool AcceptsSearchSeed(Vector3Snapshot deviceCentre, Vector3Snapshot devicePosition,
        Vector3Snapshot deviceForward, Vector3Snapshot beltPosition, Vector3Snapshot beltForward)
    {
        if (!ValidPosition(deviceCentre) || !ValidPosition(devicePosition) || !ValidPosition(beltPosition)
            || !ValidDirection(deviceForward) || !ValidDirection(beltForward)) return false;
        var direction = Subtract(beltPosition, devicePosition);
        var centreDirection = Subtract(beltPosition, deviceCentre);
        if (Dot(direction, direction) < 1e-10f || Dot(centreDirection, centreDirection) < 1e-10f
            || Angle(deviceForward, centreDirection) > 80) return false;
        var facing = Math.Max(Angle(direction, deviceForward), 180 - Angle(direction, beltForward));
        var opposition = 180 - Angle(deviceForward, beltForward);
        return Math.Max(FoldQuarterTurn(facing), FoldQuarterTurn(opposition)) < 40;
    }

    public static bool AcceptsFinalRotations(QuaternionSnapshot first, QuaternionSnapshot second)
    {
        if (!ValidRotation(first) || !ValidRotation(second)) return false;
        var dot = Math.Abs((double)first.X * second.X + (double)first.Y * second.Y
            + (double)first.Z * second.Z + (double)first.W * second.W);
        var denominator = Math.Sqrt(QuaternionLengthSquared(first) * QuaternionLengthSquared(second));
        return Math.Acos(Math.Min(1, dot / denominator)) * (360 / Math.PI) <= 24.1;
    }

    public static string BindOffsets(int inputOffset, int outputOffset, string? geometryHash)
    {
        if (inputOffset < short.MinValue || inputOffset > short.MaxValue
            || outputOffset < short.MinValue || outputOffset > short.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(inputOffset));
        return CanonicalStateHash.Combine("build-attachment-offsets-v1", inputOffset, outputOffset, geometryHash);
    }

    private static double FoldQuarterTurn(double angle) => Math.Abs((angle + 45) % 90 - 45);
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static bool ValidPosition(Vector3Snapshot? p) => p is not null
        && Finite(p.X) && Finite(p.Y) && Finite(p.Z)
        && Math.Abs(p.X) <= 10000 && Math.Abs(p.Y) <= 10000 && Math.Abs(p.Z) <= 10000
        && Dot(p, p) >= 1;
    private static bool ValidDirection(Vector3Snapshot? p) => p is not null
        && Finite(p.X) && Finite(p.Y) && Finite(p.Z) && Math.Abs(Dot(p, p) - 1) <= .002f;
    private static bool ValidRotation(QuaternionSnapshot? q) => q is not null
        && Finite(q.X) && Finite(q.Y) && Finite(q.Z) && Finite(q.W)
        && Math.Abs(QuaternionLengthSquared(q) - 1) <= .002;
    private static double QuaternionLengthSquared(QuaternionSnapshot q) => (double)q.X * q.X
        + (double)q.Y * q.Y + (double)q.Z * q.Z + (double)q.W * q.W;
    private static Vector3Snapshot Subtract(Vector3Snapshot a, Vector3Snapshot b) => new()
        { X = a.X - b.X, Y = a.Y - b.Y, Z = a.Z - b.Z };
    private static float Dot(Vector3Snapshot a, Vector3Snapshot b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    private static double Angle(Vector3Snapshot a, Vector3Snapshot b) =>
        Math.Acos(Math.Max(-1, Math.Min(1, Dot(a, b) / Math.Sqrt((double)Dot(a, a) * Dot(b, b))))) * (180 / Math.PI);
}
