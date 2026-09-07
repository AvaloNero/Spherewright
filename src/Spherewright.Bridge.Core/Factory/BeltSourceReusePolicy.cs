using Spherewright.Contracts.Factory;
using Spherewright.Contracts.Actions;

namespace Spherewright.Bridge.Core.Factory;

/// <summary>Narrow non-removing SOURCE cover subset. Partitioning is not placement approval:
/// the cover remains in the full native check/create list and ALL new points keep occupancy checks.</summary>
public static class BeltSourceReusePolicy
{
    public const string Recovery = "Do not retry this site or omit its bound endpoint. Inspect the exact source, "
        + "native path and occupancy evidence; moving closer only helps an explicit OutOfReach rejection.";

    public static bool Supports(int sourceId, int sourceItem, int newItem, int sourceSlot,
        int destinationId, float tilt, bool closed, int outputObjectId, int incomingBelts) =>
        sourceId > 0 && sourceId <= BeltBuildOccupancyPolicy.MaximumFactorySlots
        && (sourceItem == 2001 || sourceItem == 2002 || sourceItem == 2003) && newItem == sourceItem
        && sourceSlot == 0 && destinationId == 0 && tilt == 0 && !closed
        && outputObjectId == 0 && incomingBelts >= 0 && incomingBelts <= 1;

    public static bool TrySeparateNewPoints(IReadOnlyList<Vector3Snapshot>? fullPath,
        Vector3Snapshot? source, out List<Vector3Snapshot> newPoints)
    {
        newPoints = new List<Vector3Snapshot>();
        if (fullPath is null || fullPath.Count < 3 || fullPath.Count >= BeltBuildOccupancyPolicy.MaximumPathPoints
            || !Valid(source) || !Valid(fullPath[0]) || DistanceSquared(source!, fullPath[0]) > .0001) return false;
        var radius = Math.Sqrt(DistanceSquared(source!, new Vector3Snapshot()));
        var candidates = new List<Vector3Snapshot>();
        for (var i = 1; i < fullPath.Count; i++)
        {
            var point = fullPath[i];
            if (!Valid(point) || Math.Abs(Math.Sqrt(DistanceSquared(point, new Vector3Snapshot())) - radius) > .05)
                return false; // No raised/tilted path or vertical-cover removal branch in this subset.
            candidates.Add(new Vector3Snapshot { X = point.X, Y = point.Y, Z = point.Z });
        }
        newPoints = candidates;
        return true;
    }

    public static bool IsNonRemovingCover(int expectedId, int actualId, bool willRemove) =>
        expectedId > 0 && expectedId <= BeltBuildOccupancyPolicy.MaximumFactorySlots && actualId == expectedId && !willRemove;

    public static bool HasUniqueSourceCentre(int sourceId, Vector3Snapshot source,
        IReadOnlyList<BeltBuildObstacle> allBelts)
    {
        if (sourceId <= 0 || !Valid(source) || allBelts is null || allBelts.Count > BeltBuildOccupancyPolicy.MaximumFactorySlots) return false;
        var found = false;
        foreach (var belt in allBelts)
        {
            if (!Valid(belt.Position)) return false;
            var distance = DistanceSquared(source, belt.Position);
            if (belt.ObjectId == sourceId)
            {
                if (found || distance > .0001) return false;
                found = true;
            }
            else if (distance <= .0625) return false;
        }
        return found;
    }

    public static bool ProvesOnlyOutputChanged(IReadOnlyList<FactoryConnectionSnapshot>? before,
        IReadOnlyList<FactoryConnectionSnapshot>? after, int expectedNewObjectId)
    {
        if (expectedNewObjectId == 0 || expectedNewObjectId == int.MinValue
            || Math.Abs(expectedNewObjectId) > BeltBuildOccupancyPolicy.MaximumFactorySlots
            || !Complete(before) || !Complete(after) || before![0].OtherObjectId != 0) return false;
        for (var slot = 0; slot < 16; slot++)
        {
            var a = before![slot]; var b = after![slot];
            if (slot == 0)
            {
                if (!b.IsOutput || b.OtherObjectId != expectedNewObjectId || b.OtherSlot != 1) return false;
            }
            else if (a.IsOutput != b.IsOutput || a.OtherObjectId != b.OtherObjectId || a.OtherSlot != b.OtherSlot) return false;
        }
        return true;
    }

    public static bool SameEvidence(string? before, string? after) =>
        !string.IsNullOrEmpty(before) && string.Equals(before, after, StringComparison.Ordinal);

    public static bool ConfirmsPlanEcho(PreparedNormalAction? plan, int itemId, int sourceId, int destinationId)
    {
        var echo = plan?.PlannedBeltPath;
        if (sourceId < 0 || sourceId > 65536 || destinationId < 0 || destinationId > 65536
            || plan is null || !plan.Prepared || plan.BuildKind != "belt" || echo is null
            || echo.NativeValidationMode != "full_path_stage1" || echo.NewObjectCount < 2 || echo.NewObjectCount >= 256
            || plan.PlannedPath is null || plan.PlannedPath.Count != echo.NewObjectCount || plan.SourceObjectId.GetValueOrDefault() != sourceId
            || plan.DestinationObjectId.GetValueOrDefault() != destinationId
            || plan.ItemBudget is null || plan.ItemBudget.Count != 1 || plan.ItemBudget[0] is null || plan.ItemBudget[0].ItemId != itemId
            || plan.ItemBudget[0].Count != echo.NewObjectCount || plan.ItemBudget[0].Direction != "construction-consumption") return false;
        return echo.SourceBindingMode switch
        {
            "none" => sourceId == 0 && !echo.ReusedSourceObjectId.HasValue,
            "native_device_port" => sourceId > 0 && !echo.ReusedSourceObjectId.HasValue,
            "non_removing_belt_cover" => sourceId > 0 && destinationId == 0 && echo.ReusedSourceObjectId == sourceId
                && echo.SourcePreservationMode == BeltSourceRotationPolicy.PreservationMode,
            _ => false,
        };
    }

    private static bool Complete(IReadOnlyList<FactoryConnectionSnapshot>? connections) => connections is not null
        && connections.Count == 16 && connections.Select((c, i) => c is not null && c.Slot == i
            && c.OtherObjectId != int.MinValue && Math.Abs(c.OtherObjectId) <= BeltBuildOccupancyPolicy.MaximumFactorySlots
            && c.OtherSlot >= 0 && c.OtherSlot < 16).All(valid => valid);
    private static bool Valid(Vector3Snapshot? p) => p is not null && Finite(p.X) && Finite(p.Y) && Finite(p.Z)
        && Math.Abs(p.X) <= 10000 && Math.Abs(p.Y) <= 10000 && Math.Abs(p.Z) <= 10000
        && DistanceSquared(p, new Vector3Snapshot()) >= 1;
    private static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
    private static double DistanceSquared(Vector3Snapshot a, Vector3Snapshot b) =>
        Math.Pow((double)a.X - b.X, 2) + Math.Pow((double)a.Y - b.Y, 2) + Math.Pow((double)a.Z - b.Z, 2);
}
