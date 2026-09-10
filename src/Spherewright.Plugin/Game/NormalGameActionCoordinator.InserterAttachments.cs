using System.Threading;
using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;
using UnityEngine;

namespace Spherewright.Plugin.Game;

internal sealed partial class NormalGameActionCoordinator
{
    // Current DeterminePreviews one-belt/one-device branch, restricted to the two
    // caller-selected entities. No nearby-belt retargeting, UI input or position write.
    private static bool TryPrepareBeltAttachment(PlanetFactory factory, Player player, ItemProto item,
        IReadOnlyList<EndpointPoint> sources, IReadOnlyList<EndpointPoint> destinations, int filter,
        out BuildStepPlan? accepted, out string rejection)
    {
        accepted = null;
        rejection = "Native attachment stage=unsupported_endpoint_family; requires ordinary2011/2012 and one belt with one supported device.";
        if ((item.ID != 2011 && item.ID != 2012) || sources.Count == 0 || destinations.Count == 0) return false;
        var sourceIsBelt = factory.entityPool[sources[0].ObjectId].beltId > 0;
        var destinationIsBelt = factory.entityPool[destinations[0].ObjectId].beltId > 0;
        if (sourceIsBelt == destinationIsBelt) return false;
        var beltPoints = sourceIsBelt ? sources : destinations;
        var devicePoints = sourceIsBelt ? destinations : sources;
        var device = factory.entityPool[devicePoints[0].ObjectId];
        // No generator-to-generator offset overload, exotic attachments or implicit targets.
        if (device.inserterId != 0 || device.powerGenId != 0
            || (device.storageId != 0 && device.protoId != 2101)
            || (device.storageId == 0 && device.assemblerId == 0 && device.labId == 0)) return false;
        if (!TryCaptureBeltAttachment(factory, beltPoints[0].ObjectId, out var geometry, out var geometryReason))
        {
            rejection = "Native attachment stage=geometry_unavailable; reason=" + geometryReason + ". No interpolated angle or native placement was tested.";
            return false;
        }
        var report = new InserterAttachmentSearchReport();
        string? lastNativeRejection = null;
        foreach (var devicePoint in devicePoints)
        {
            foreach (var beltPoint in beltPoints)
            {
                var beltPose = beltPoint.Pose;
                beltPose.rotation = FaceNativeBeltQuarterTurn(beltPose.rotation, devicePoint.Pose.position - beltPose.position);
                var admitted = InserterBeltAttachmentPolicy.AcceptsSearchSeed(Snapshot(device.pos), Snapshot(devicePoint.Pose.position),
                    Snapshot(devicePoint.Pose.forward), Snapshot(beltPose.position), Snapshot(beltPose.forward));
                report.RecordSeed(admitted);
                if (!admitted) continue;
                // This loop, including the evolving projection target, mirrors the native
                // single-belt branch. All samples are a value-copy of at most65 points.
                for (var i = 0; i < geometry!.Positions.Length - 1; i++)
                {
                    var towardsDevice = devicePoint.Pose.position - beltPose.position;
                    if (!InserterBeltAttachmentPolicy.TryProjectionFraction(Snapshot(devicePoint.Pose.position),
                            Snapshot(devicePoint.Pose.forward), Snapshot(beltPose.position),
                            Snapshot(geometry.Positions[i]), Snapshot(geometry.Positions[i + 1]), out var fraction)) continue;
                    beltPose.position = Vector3.Lerp(geometry.Positions[i], geometry.Positions[i + 1], fraction);
                    beltPose.position -= beltPose.position.normalized * .15f;
                    beltPose.rotation = FaceNativeBeltQuarterTurn(
                        Quaternion.Slerp(geometry.Rotations[i], geometry.Rotations[i + 1], fraction), towardsDevice);
                    var sourcePose = sourceIsBelt ? beltPose : devicePoint.Pose;
                    var destinationPose = sourceIsBelt ? devicePoint.Pose : beltPose;
                    var hasDeviation = NativeInserterEndpointGeometry.TryGetStraightPairDeviation(
                            Snapshot(destinationPose.position - sourcePose.position), Snapshot(sourcePose.forward),
                            Snapshot(destinationPose.forward), out var deviation);
                    report.RecordProjection(hasDeviation ? deviation : (double?)null);
                    if (!hasDeviation || deviation >= 11d) continue;
                    report.RecordFacingPair();
                    var step = BuildStepPlan.Inserter(item.ID, sourcePose, destinationPose,
                        sources[0].ObjectId, sourceIsBelt ? -1 : devicePoint.Slot,
                        destinations[0].ObjectId, sourceIsBelt ? devicePoint.Slot : -1);
                    var offset = geometry.Start + i - geometry.Pivot;
                    step.InputOffset = sourceIsBelt ? offset : 0;
                    step.OutputOffset = sourceIsBelt ? 0 : offset;
                    step.FilterItemId = filter;
                    step.AttachmentBeltObjectId = geometry.EntityId;
                    step.AttachmentGeometryHash = geometry.Hash;
                    step.ApplyNativeBeltTilt(factory, sourceIsBelt);
                    if (!InserterBeltAttachmentPolicy.AcceptsFinalRotations(
                            AttachmentRotation(step.Rotation), AttachmentRotation(step.Rotation2)))
                    {
                        report.RecordTiltRejection();
                        break;
                    }
                    if (!report.TryRecordCandidateCheck())
                    {
                        rejection = report.DescribeFailure();
                        return false;
                    }
                    if (TryValidateInserterBuild(factory, player, item, step, out var checkedStep, out rejection, out _))
                    {
                        accepted = checkedStep;
                        return true;
                    }
                    lastNativeRejection = rejection;
                    // Native selects the first <11-degree point for this seed; do not
                    // silently search later points to evade its placement rejection.
                    break;
                }
            }
        }
        rejection = report.DescribeFailure();
        if (lastNativeRejection is not null) rejection += " Last candidate validation: " + lastNativeRejection;
        return false;
    }

    private static Quaternion FaceNativeBeltQuarterTurn(Quaternion rotation, Vector3 direction)
    {
        foreach (var angle in new[] { 90f, 180f, -90f })
        {
            var candidate = rotation * Quaternion.Euler(0, angle, 0);
            if (Vector3.Angle(direction, candidate * Vector3.forward) < 40f) rotation = candidate;
        }
        return rotation;
    }

    private static bool TryCaptureBeltAttachment(PlanetFactory factory, int entityId, out BeltAttachmentGeometry? geometry,
        out string reason)
    {
        geometry = null;
        reason = "belt_entity_identity_unavailable";
        if (factory.entityPool is null || entityId <= 0 || entityId >= factory.entityCursor
            || entityId >= factory.entityPool.Length || factory.entityPool[entityId].id != entityId) return false;
        var entity = factory.entityPool[entityId];
        var traffic = factory.cargoTraffic;
        reason = "belt_component_unavailable";
        if (traffic?.beltPool is null || entity.beltId <= 0 || entity.beltId >= traffic.beltCursor
            || entity.beltId >= traffic.beltPool.Length) return false;
        var belt = traffic.beltPool[entity.beltId];
        reason = "belt_component_or_path_identity_unavailable";
        if (belt.id != entity.beltId || belt.entityId != entityId || traffic.pathPool is null
            || belt.segPathId <= 0 || belt.segPathId >= traffic.pathCursor || belt.segPathId >= traffic.pathPool.Length) return false;
        var path = traffic.GetCargoPath(belt.segPathId);
        reason = "cargo_path_unavailable";
        if (path is null || path.id != belt.segPathId || path.buffer is null
            || path.cargoContainer is null || !ReferenceEquals(path.cargoContainer, factory.cargoContainer)
            || path.pointPos is null || path.pointRot is null) return false;
        reason = "closed_path_unsupported";
        if (path.closed) return false;
        reason = "geometry_array_shape_invalid";
        if (path.pathLength > path.buffer.Length || path.pointPos.Length != path.buffer.Length
            || path.pointRot.Length != path.buffer.Length) return false;
        reason = "segment_membership_invalid";
        if (path.belts is null || path.belts.Count > BeltUpgradePathPolicy.MaximumBelts
            || path.belts.Count(id => id == belt.id) != 1) return false;
        reason = "segment_window_unsupported";
        if (!InserterBeltAttachmentPolicy.TryGetWindow(path.pathLength, belt.segIndex, belt.segLength,
                belt.segPivotOffset, out var start, out var end, out var pivot)) return false;
        var buffer = path.buffer;
        reason = "geometry_buffer_busy";
        if (!Monitor.TryEnter(buffer)) return false;
        try
        {
            var positions = new Vector3[end - start + 1];
            var rotations = new Quaternion[positions.Length];
            Array.Copy(path.pointPos, start, positions, 0, positions.Length);
            Array.Copy(path.pointRot, start, rotations, 0, rotations.Length);
            reason = "geometry_values_invalid";
            if (!InserterBeltAttachmentPolicy.TryGeometryHash(entityId, belt.id, path.id, path.pathLength,
                    belt.segIndex, belt.segLength, belt.segPivotOffset, Snapshot(entity.pos), AttachmentRotation(entity.rot),
                    entity.tilt, positions.Select(Snapshot).ToArray(), rotations.Select(AttachmentRotation).ToArray(), out var hash))
                return false;
            geometry = new BeltAttachmentGeometry(entityId, start, pivot, positions, rotations, hash);
            reason = string.Empty;
            return true;
        }
        finally { Monitor.Exit(buffer); }
    }

    private static bool RevalidateBeltAttachment(PlanetFactory factory, BuildStepPlan step)
    {
        if (step.AttachmentGeometryHash is null)
            return step.AttachmentBeltObjectId == 0 && step.InputOffset == 0 && step.OutputOffset == 0;
        var beltIsSource = step.AttachmentBeltObjectId == step.InputObjectId;
        var beltIsDestination = step.AttachmentBeltObjectId == step.OutputObjectId;
        if (beltIsSource == beltIsDestination
            || (beltIsSource ? step.OutputOffset != 0 : step.InputOffset != 0)
            || !TryCaptureBeltAttachment(factory, step.AttachmentBeltObjectId, out var geometry, out _)
            || !string.Equals(step.AttachmentGeometryHash, geometry!.Hash, StringComparison.Ordinal)) return false;
        var offset = beltIsSource ? step.InputOffset : step.OutputOffset;
        var index = (long)geometry.Pivot + offset;
        return index >= geometry.Start && index < geometry.Start + geometry.Positions.Length - 1;
    }

    private static QuaternionSnapshot AttachmentRotation(Quaternion q) => new()
        { X = q.x, Y = q.y, Z = q.z, W = q.w };

    private sealed class BeltAttachmentGeometry
    {
        internal BeltAttachmentGeometry(int entityId, int start, int pivot, Vector3[] positions, Quaternion[] rotations, string hash)
        { EntityId = entityId; Start = start; Pivot = pivot; Positions = positions; Rotations = rotations; Hash = hash; }
        internal int EntityId { get; }
        internal int Start { get; }
        internal int Pivot { get; }
        internal Vector3[] Positions { get; }
        internal Quaternion[] Rotations { get; }
        internal string Hash { get; }
    }
}
