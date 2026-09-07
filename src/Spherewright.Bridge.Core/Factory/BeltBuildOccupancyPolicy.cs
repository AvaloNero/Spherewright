using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

/// <summary>Conservative centre-coincidence guard for NEW belt objects only.
/// Not a native collision validator, route planner, or permission to reuse an anchor.</summary>
public static class BeltBuildOccupancyPolicy
{
    public const int MaximumPathPoints = 256;
    public const int MaximumFactorySlots = 65536;
    public const int MaximumDistanceChecks = 65536;
    public const float MinimumCentreSeparation = .25f;

    public static bool TryValidateNewPath(IReadOnlyList<Vector3Snapshot> planned,
        IReadOnlyList<BeltBuildObstacle> existing, out BeltBuildOccupancyFailure? failure)
    {
        failure = null;
        if (planned is null || existing is null || planned.Count < 2
            || planned.Count > MaximumPathPoints || existing.Count > MaximumFactorySlots)
            return Reject("belt_occupancy_bounds_unavailable", -1, 0, out failure);
        var occupied = new Dictionary<(int X, int Y, int Z), List<BeltBuildObstacle>>();
        var ids = new HashSet<int>();
        foreach (var obstacle in existing)
        {
            if (obstacle.ObjectId == 0 || obstacle.ObjectId == int.MinValue
                || Math.Abs(obstacle.ObjectId) > MaximumFactorySlots || !ids.Add(obstacle.ObjectId)
                || !Valid(obstacle.Position))
                return Reject("belt_occupancy_evidence_invalid", -1, obstacle.ObjectId, out failure);
            Add(occupied, obstacle);
        }
        // Zero is used only in this private planned grid, never as an existing ID.
        // No source/destination exemption within this NEW-object list. A separately
        // proven native non-removing cover must never be passed as a NEW node.
        var proposed = new Dictionary<(int X, int Y, int Z), List<BeltBuildObstacle>>();
        var checks = 0;
        for (var index = 0; index < planned.Count; index++)
        {
            var point = planned[index];
            if (!Valid(point)) return Reject("belt_path_position_invalid", index, 0, out failure);
            if (!CheckNeighbours(occupied, point, index, "belt_path_existing_overlap", ref checks, out failure)
                || !CheckNeighbours(proposed, point, index, "belt_path_self_overlap", ref checks, out failure))
                return false;
            Add(proposed, new BeltBuildObstacle(0, point));
        }
        return true;
    }

    private static bool CheckNeighbours(Dictionary<(int X, int Y, int Z), List<BeltBuildObstacle>> grid,
        Vector3Snapshot point, int index, string overlapReason, ref int checks,
        out BeltBuildOccupancyFailure? failure)
    {
        failure = null;
        var cell = Cell(point);
        for (var x = -1; x <= 1; x++)
        for (var y = -1; y <= 1; y++)
        for (var z = -1; z <= 1; z++)
        {
            if (!grid.TryGetValue((cell.X + x, cell.Y + y, cell.Z + z), out var neighbours)) continue;
            foreach (var obstacle in neighbours)
            {
                if (++checks > MaximumDistanceChecks)
                    return Reject("belt_occupancy_comparison_limit", index, 0, out failure);
                var dx = (double)point.X - obstacle.Position.X;
                var dy = (double)point.Y - obstacle.Position.Y;
                var dz = (double)point.Z - obstacle.Position.Z;
                if (dx * dx + dy * dy + dz * dz <= MinimumCentreSeparation * MinimumCentreSeparation)
                    return Reject(overlapReason, index, obstacle.ObjectId, out failure);
            }
        }
        return true;
    }

    private static void Add(Dictionary<(int X, int Y, int Z), List<BeltBuildObstacle>> grid,
        BeltBuildObstacle obstacle)
    {
        var cell = Cell(obstacle.Position);
        if (!grid.TryGetValue(cell, out var values)) grid.Add(cell, values = new List<BeltBuildObstacle>());
        values.Add(obstacle);
    }
    private static (int X, int Y, int Z) Cell(Vector3Snapshot p) =>
        ((int)Math.Floor(p.X / MinimumCentreSeparation),
         (int)Math.Floor(p.Y / MinimumCentreSeparation),
         (int)Math.Floor(p.Z / MinimumCentreSeparation));
    private static bool Valid(Vector3Snapshot? p) => p is not null
        && Finite(p.X) && Finite(p.Y) && Finite(p.Z)
        && Math.Abs(p.X) <= 10000 && Math.Abs(p.Y) <= 10000 && Math.Abs(p.Z) <= 10000
        && (double)p.X * p.X + (double)p.Y * p.Y + (double)p.Z * p.Z >= 1;
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static bool Reject(string reason, int index, int objectId, out BeltBuildOccupancyFailure? failure)
    {
        failure = new BeltBuildOccupancyFailure(reason, index, objectId);
        return false;
    }
}

public readonly struct BeltBuildObstacle
{
    public BeltBuildObstacle(int objectId, Vector3Snapshot position) { ObjectId = objectId; Position = position; }
    public int ObjectId { get; }
    public Vector3Snapshot Position { get; }
}

public sealed class BeltBuildOccupancyFailure
{
    internal BeltBuildOccupancyFailure(string reason, int plannedIndex, int objectId)
    { Reason = reason; PlannedIndex = plannedIndex; ObjectId = objectId; }
    public string Reason { get; }
    public int PlannedIndex { get; }
    public int ObjectId { get; }
}
