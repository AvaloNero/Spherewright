using Spherewright.Bridge.Core.Factory;

namespace Spherewright.Plugin.Game;

internal sealed partial class NormalGameActionCoordinator
{
    // Owned/local factory only, Unity thread. Bound by ALL scanned slots, including holes.
    private static bool TryValidateNewBeltOccupancy(PlanetFactory factory,
        IReadOnlyList<BuildStepPlan> steps, out string rejection)
    {
        rejection = "belt_occupancy_bounds_unavailable: a complete bounded current-factory read is required.";
        if (steps.Count < 2 || steps.Count > BeltBuildOccupancyPolicy.MaximumPathPoints
            || factory.entityPool is null || factory.prebuildPool is null
            || factory.entityCursor < 1 || factory.entityCursor > factory.entityPool.Length
            || factory.prebuildCursor < 1 || factory.prebuildCursor > factory.prebuildPool.Length
            || (long)factory.entityCursor + factory.prebuildCursor - 2 > BeltBuildOccupancyPolicy.MaximumFactorySlots)
            return false;
        var belts = new List<BeltBuildObstacle>();
        var types = new Dictionary<int, bool>();
        bool TryBeltType(int itemId, out bool isBelt)
        {
            isBelt = false;
            if (itemId <= 0) return false;
            if (types.TryGetValue(itemId, out isBelt)) return true;
            var desc = LDB.items.Select(itemId)?.prefabDesc;
            if (desc is null) return false;
            types.Add(itemId, isBelt = desc.isBelt);
            return true;
        }
        for (var id = 1; id < factory.entityCursor; id++)
        {
            ref var entity = ref factory.entityPool[id];
            if (entity.id == 0) continue;
            if (entity.id != id)
            { rejection = "belt_occupancy_evidence_invalid: an entity pool identity is inconsistent."; return false; }
            if (!TryBeltType(entity.protoId, out var isBelt) || (entity.beltId > 0 && !isBelt))
            { rejection = "belt_occupancy_evidence_invalid: an existing prototype cannot be classified."; return false; }
            if (isBelt) belts.Add(new BeltBuildObstacle(id, Snapshot(entity.pos)));
        }
        for (var id = 1; id < factory.prebuildCursor; id++)
        {
            ref var prebuild = ref factory.prebuildPool[id];
            if (prebuild.id == 0) continue;
            if (prebuild.id != id)
            { rejection = "belt_occupancy_evidence_invalid: a prebuild pool identity is inconsistent."; return false; }
            if (prebuild.isDestroyed) continue;
            if (!TryBeltType(prebuild.protoId, out var isBelt))
            { rejection = "belt_occupancy_evidence_invalid: an existing prebuild prototype cannot be classified."; return false; }
            if (isBelt) belts.Add(new BeltBuildObstacle(-id, Snapshot(prebuild.pos)));
        }
        if (!BeltBuildOccupancyPolicy.TryValidateNewPath(steps.Select(step => Snapshot(step.Position)).ToList(),
                belts, out var failure))
        {
            rejection = $"{failure!.Reason}: planned point {failure.PlannedIndex}, object {failure.ObjectId}; "
                + "new belt centres must remain more than 0.25 m apart, including both ends. "
                + "Only an explicitly proven non-removing source cover is separate from the NEW points; do not retry at the same site "
                + "or omit the bound endpoint to bypass occupancy. No objects were created.";
            return false;
        }
        rejection = string.Empty;
        var source = steps[0].SourceBeltAnchor;
        if (source is not null && !BeltSourceReusePolicy.HasUniqueSourceCentre(source.EntityId, Snapshot(source.Position), belts))
        {
            rejection = "belt_source_centre_ambiguous: the retained source overlaps another belt or its exact identity/pose is missing. "
                + BeltSourceReusePolicy.Recovery;
            return false;
        }
        return true;
    }
}
