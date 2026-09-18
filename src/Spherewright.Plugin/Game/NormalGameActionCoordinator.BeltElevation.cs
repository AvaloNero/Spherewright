using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Actions;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Factory;
using UnityEngine;

namespace Spherewright.Plugin.Game;

internal sealed partial class NormalGameActionCoordinator
{
    private static BuildPreparation TryPrepareElevatedBeltBuild(
        PlanetFactory factory, Player player, ItemProto item, PrepareBuildRequest request)
    {
        var error = BeltElevationPolicy.ValidateRequest(request, item.prefabDesc.isBelt);
        if (error is not null)
            return BuildPreparation.Failed(BridgeErrorCodes.InvalidRequest, error, BeltElevationPolicy.Recovery);
        if (factory.planet.aux?.customGrids is null || factory.planet.aux.activeGrid is null)
            return BuildPreparation.Failed(BridgeErrorCodes.BuildLocationInvalid,
                "belt_elevation_native_grid_unavailable: a geodesic fallback cannot satisfy an explicit grid request.",
                BeltElevationPolicy.Recovery);
        if (!TryResolveElevatedBeltEndpoint(factory, request.PreferredPosition!, request.BeltStartAltitudeLevel!.Value, out var start)
            || !TryResolveElevatedBeltEndpoint(factory, request.PathEnd!, request.BeltEndAltitudeLevel!.Value, out var end))
            return BuildPreparation.Failed(BridgeErrorCodes.BuildLocationInvalid,
                "belt_elevation_native_layer_mismatch: native Snap did not retain the exact requested altitude level.",
                BeltElevationPolicy.Recovery);
        if (!TryCreateBeltSteps(factory, item, start, end, request.BeltPathMode, out var candidate, out var rejection))
            return BuildPreparation.Failed(BridgeErrorCodes.BuildLocationInvalid, rejection, BeltElevationPolicy.Recovery);
        if (!TryValidateBeltBuild(factory, player, item, candidate, out var accepted, out rejection, out var rejectionError))
            return BuildPreparation.Failed(rejectionError?.Code ?? BridgeErrorCodes.BuildLocationInvalid,
                rejection, rejectionError?.Recovery ?? BeltElevationPolicy.Recovery);
        return BuildPreparation.Succeeded(NormalBuildKinds.Belt, accepted);
    }

    private static bool TryResolveElevatedBeltEndpoint(PlanetFactory factory, Vector3Snapshot requested,
        int level, out EndpointPoint endpoint)
    {
        // PlanetAuxData.Snap(false) floors altitude in native 1.3333333 m layers
        // relative to planet.radius and adds .2 m. Supply that native layer's
        // centre, then prove the returned layer rather than trusting float floor.
        var groundRadius = factory.planet.radius + .2f;
        var position = factory.planet.aux.Snap(ToVector(requested).normalized
            * (groundRadius + level * BeltElevationPolicy.NativeLayerHeight), onTerrain: false);
        endpoint = new EndpointPoint(0, -1, new Pose(position, Maths.SphericalRotation(position, 0f)));
        return BeltElevationPolicy.TryGetLevel(Snapshot(position), groundRadius, out var actual) && actual == level;
    }

    private static bool CompletePreparedElevatedBeltPath(IReadOnlyList<BuildStepPlan> steps, float radius,
        IReadOnlyList<BuildStepPlan>? original = null)
    {
        if (steps.Count < 4 || steps.Count > BeltElevationPolicy.MaximumPoints
            || original is not null && original.Count != steps.Count) return false;
        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            if (step.ItemId != 2001 || step.BeltPathMode != BeltPathModes.NativeElevatedGrid || step.Tilt != 0
                || step.SourceBeltAnchor is not null || step.DestinationBeltAnchor is not null
                || step.InputObjectId != 0 || step.OutputObjectId != 0
                || step.InputStepIndex != (i > 0 ? i - 1 : -1)
                || step.OutputStepIndex != (i + 1 < steps.Count ? i + 1 : -1)
                || !(Vector3.Distance(step.Position, step.Position2) <= .01f)
                || original is not null && !(Vector3.Distance(step.Position, original[i].Position) <= .01f))
                return false;
        }
        var points = steps.Select(step => Snapshot(step.Position)).ToArray();
        return BeltElevationPolicy.CompleteNativePath(points, BeltBuildOccupancyPolicy.MaximumPathPoints,
            points[0], points[points.Length - 1], radius);
    }

    private static bool ProvesCreatedElevatedPath(PlanetFactory factory, IReadOnlyList<BuildStepPlan> steps,
        IReadOnlyList<BuildPreview> previews)
    {
        if (steps.Count != previews.Count || previews.Select(p => p.objId).Distinct().Count() != steps.Count
            || factory.prebuildPool is null || factory.prebuildConnPool is null) return false;
        var ids = previews.Select(p => p.objId).ToArray();
        for (var i = 0; i < ids.Length; i++)
        {
            var id = ids[i];
            if (id >= 0 || id == int.MinValue || -id >= factory.prebuildCursor || -id >= factory.prebuildPool.Length
                || (long)-id * 16 + 16 > factory.prebuildConnPool.Length) return false;
            var prebuild = factory.prebuildPool[-id];
            // Native CreatePrebuilds normalises both prebuild rotations to the
            // spherical basis; the completed renderer later derives pitch from
            // CargoPath. Do not confuse the two native pose lifecycles.
            var rotation = Maths.SphericalRotation(steps[i].Position, 0f);
            if (prebuild.id != -id || prebuild.protoId != 2001 || prebuild.isDestroyed || prebuild.tilt != 0
                || !(Vector3.Distance(prebuild.pos, steps[i].Position) <= .01f)
                || !(Vector3.Distance(prebuild.pos2, steps[i].Position) <= .01f)
                || !(Quaternion.Angle(prebuild.rot, rotation) <= .1f) || !(Quaternion.Angle(prebuild.rot2, rotation) <= .1f))
                return false;
        }
        return ProvesFreeElevatedConnections(factory, ids);
    }

    private static bool ProvesCompletedElevatedPath(PlanetFactory factory, NormalActionPlanPayload plan,
        IReadOnlyList<int> ids)
    {
        if (ids.Count != plan.BuildSteps.Count || ids.Count < 4 || ids.Count > BeltElevationPolicy.MaximumPoints
            || ids.Distinct().Count() != ids.Count) return false;
        for (var i = 0; i < ids.Count; i++)
        {
            var id = ids[i];
            if (id <= 0 || id >= factory.entityCursor || id >= factory.entityPool.Length) return false;
            var entity = factory.entityPool[id];
            if (entity.id != id || entity.protoId != 2001 || entity.tilt != 0 || entity.beltId <= 0
                || !(Vector3.Distance(entity.pos, plan.BuildSteps[i].Position) <= .01f)
                || !ProvesNativeBeltRotation(factory, id, requireColliderExtent: true)) return false;
        }
        // This helper proves directed membership, native rotations, component
        // reciprocity and an empty independent open path. Its cover-specific
        // allowance for external consumers/changing slots is NOT allowed here.
        return TryCaptureEmptyBeltPath(factory, ids[0], 1, out var path, out _)
            && path!.EntityIds.SequenceEqual(ids) && ProvesFreeElevatedConnections(factory, ids);
    }

    private static bool ProvesFreeElevatedConnections(PlanetFactory factory, IReadOnlyList<int> ids)
    {
        for (var i = 0; i < ids.Count; i++)
        {
            for (var slot = 0; slot < 16; slot++)
            {
                var expected = slot == 0 ? (i + 1 < ids.Count ? ids[i + 1] : 0)
                    : slot == 1 ? (i > 0 ? ids[i - 1] : 0) : 0;
                factory.ReadObjectConn(ids[i], slot, out var output, out var other, out var otherSlot);
                if (other != expected || other != 0 && (output != (slot == 0) || otherSlot != (slot == 0 ? 1 : 0)))
                    return false;
            }
        }
        return true;
    }
}
