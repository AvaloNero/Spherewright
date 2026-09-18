using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

/// <summary>Narrow non-removing DESTINATION cover subset. Partitioning is not placement approval:
/// both covers remain in the native check/create list and every returned NEW point keeps occupancy checks.</summary>
public static class BeltDestinationReusePolicy
{
    public const string PreservationMode = "empty_open_path_native_geometry_v1";
    private const float FlatTiltTolerance = .0001f;
    private const double ExactCentreDistanceSquared = .0001;

    public static bool Supports(int destinationId, int destinationItemId, int requestedItemId, int destinationSlot,
        float tilt, bool isOpenHead, int occupiedInputSlots, int cargoCount, int externalInputCount, int outputPathId) =>
        destinationId > 0 && destinationId <= BeltBuildOccupancyPolicy.MaximumFactorySlots
        && destinationItemId == 2001 && requestedItemId == 2001 && destinationSlot == 1
        && Finite(tilt) && Math.Abs(tilt) <= FlatTiltTolerance && isOpenHead
        && occupiedInputSlots == 0 && cargoCount == 0 && externalInputCount == 0 && outputPathId == 0;

    /// <summary>Removes only a caller-bound optional source cover and the caller-bound destination cover.
    /// The result is a deep-copied list of NEW points, never an occupancy exemption.</summary>
    public static bool TrySeparateNewPoints(IReadOnlyList<Vector3Snapshot> path, Vector3Snapshot? source,
        Vector3Snapshot destination, int nativeCapacity, out IReadOnlyList<Vector3Snapshot> newPoints, out string reason)
    {
        newPoints = Array.Empty<Vector3Snapshot>();
        reason = string.Empty;
        if (path is null || destination is null || nativeCapacity <= BeltPathRoutingPolicy.NativeReservedPoints + 2
            || nativeCapacity > BeltBuildOccupancyPolicy.MaximumPathPoints)
            return Reject("destination_reuse_path_bounds", out newPoints, out reason);
        if (path.Count == 0 || path.Count >= nativeCapacity - BeltPathRoutingPolicy.NativeReservedPoints)
            return Reject("destination_reuse_path_saturated", out newPoints, out reason);
        if (!Valid(destination) || (source is not null && !Valid(source)))
            return Reject("destination_reuse_endpoint_invalid", out newPoints, out reason);
        if (source is not null && SameCentre(source, destination))
            return Reject("destination_reuse_covers_not_distinct", out newPoints, out reason);

        var firstNewIndex = 0;
        if (source is not null)
        {
            if (!SameCentre(path[0], source))
                return Reject("destination_reuse_source_mismatch", out newPoints, out reason);
            firstNewIndex = 1;
        }
        if (!SameCentre(path[path.Count - 1], destination))
            return Reject("destination_reuse_destination_mismatch", out newPoints, out reason);
        if (path.Count - 1 - firstNewIndex < 2)
            return Reject("destination_reuse_new_points_insufficient", out newPoints, out reason);

        var radius = Radius(destination);
        for (var index = 0; index < path.Count; index++)
        {
            var point = path[index];
            if (!Valid(point)) return Reject("destination_reuse_path_invalid", out newPoints, out reason);
            if (Math.Abs(Radius(point) - radius) > .05)
                return Reject("destination_reuse_path_not_horizontal", out newPoints, out reason);
            if (index >= firstNewIndex && index < path.Count - 1
                && ((source is not null && SameCentre(point, source)) || SameCentre(point, destination)))
                return Reject("destination_reuse_cover_repeated_as_new", out newPoints, out reason);
        }
        if (source is not null && Math.Abs(Radius(source) - radius) > .05)
            return Reject("destination_reuse_path_not_horizontal", out newPoints, out reason);

        var copies = new List<Vector3Snapshot>(path.Count - firstNewIndex - 1);
        for (var index = firstNewIndex; index < path.Count - 1; index++)
        {
            var point = path[index];
            copies.Add(new Vector3Snapshot { X = point.X, Y = point.Y, Z = point.Z });
        }
        newPoints = copies;
        return true;
    }

    public static bool ProvesOnlyInputChanged(IReadOnlyList<FactoryConnectionSnapshot>? before,
        IReadOnlyList<FactoryConnectionSnapshot>? after, int lastNewId)
    {
        if (!ValidObjectId(lastNewId) || !Complete(before) || !Complete(after)) return false;
        var prior = before![1];
        var current = after![1];
        if (prior.IsOutput || prior.OtherObjectId != 0 || prior.OtherSlot != 0
            || current.IsOutput || current.OtherObjectId != lastNewId || current.OtherSlot != 0)
            return false;

        for (var slot = 0; slot < 16; slot++)
        {
            if (slot == 1) continue;
            var priorSlot = before[slot];
            var currentSlot = after[slot];
            if (priorSlot.IsOutput != currentSlot.IsOutput || priorSlot.OtherObjectId != currentSlot.OtherObjectId
                || priorSlot.OtherSlot != currentSlot.OtherSlot || priorSlot.OtherObjectId == lastNewId)
                return false;
        }
        return true;
    }

    /// <summary>Proves the native path membership changed only by adjoining the exact NEW sequence.
    /// A null source denotes the free-head subset; this does not validate occupancy or placement.</summary>
    public static bool ProvesJoinedMembership(IReadOnlyList<int>? source, IReadOnlyList<int>? newIds,
        IReadOnlyList<int>? destination, IReadOnlyList<int>? actual)
    {
        if (newIds is null || destination is null || actual is null || newIds.Count < 2 || destination.Count < 1)
            return false;
        var expectedCount = (long)(source?.Count ?? 0) + newIds.Count + destination.Count;
        if (expectedCount > BeltUpgradePathPolicy.MaximumBelts || actual.Count != expectedCount) return false;

        var seen = new HashSet<int>();
        var index = 0;
        if (!Matches(source, actual, ref index, seen)) return false;
        if (!Matches(newIds, actual, ref index, seen)) return false;
        return Matches(destination, actual, ref index, seen) && index == actual.Count;
    }

    private static bool Complete(IReadOnlyList<FactoryConnectionSnapshot>? connections) => connections is not null
        && connections.Count == 16 && connections.Select((connection, index) => connection is not null
            && connection.Slot == index && connection.OtherObjectId != int.MinValue
            && Math.Abs(connection.OtherObjectId) <= BeltBuildOccupancyPolicy.MaximumFactorySlots
            && connection.OtherSlot >= 0 && connection.OtherSlot < 16).All(valid => valid);

    private static bool ValidObjectId(int value) => value != 0 && value != int.MinValue
        && Math.Abs(value) <= BeltBuildOccupancyPolicy.MaximumFactorySlots;
    private static bool Matches(IReadOnlyList<int>? expected, IReadOnlyList<int> actual, ref int index, HashSet<int> seen)
    {
        if (expected is null) return true;
        foreach (var value in expected)
        {
            if (!ValidPositiveObjectId(value) || !seen.Add(value) || index >= actual.Count || actual[index] != value)
                return false;
            index++;
        }
        return true;
    }
    private static bool ValidPositiveObjectId(int value) => value > 0
        && value <= BeltBuildOccupancyPolicy.MaximumFactorySlots;
    private static bool Valid(Vector3Snapshot? point) => point is not null && Finite(point.X) && Finite(point.Y) && Finite(point.Z)
        && Math.Abs(point.X) <= 10000 && Math.Abs(point.Y) <= 10000 && Math.Abs(point.Z) <= 10000
        && DistanceSquared(point, new Vector3Snapshot()) >= 1;
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static bool SameCentre(Vector3Snapshot? left, Vector3Snapshot? right) => Valid(left) && Valid(right)
        && DistanceSquared(left!, right!) <= ExactCentreDistanceSquared;
    private static double Radius(Vector3Snapshot point) => Math.Sqrt(DistanceSquared(point, new Vector3Snapshot()));
    private static double DistanceSquared(Vector3Snapshot left, Vector3Snapshot right) =>
        Math.Pow((double)left.X - right.X, 2) + Math.Pow((double)left.Y - right.Y, 2) + Math.Pow((double)left.Z - right.Z, 2);
    private static bool Reject(string failure, out IReadOnlyList<Vector3Snapshot> newPoints, out string reason)
    {
        newPoints = Array.Empty<Vector3Snapshot>();
        reason = failure;
        return false;
    }
}
