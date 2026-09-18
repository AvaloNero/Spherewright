using System.Threading;
using Spherewright.Bridge.Core.Factory;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Actions;
using Spherewright.Contracts.Factory;
using UnityEngine;

namespace Spherewright.Plugin.Game;

internal sealed partial class NormalGameActionCoordinator
{
    // Copied evidence only. Joining even non-removing covers can rebuild BOTH
    // cargo paths; an endpoint hash is not a sufficient construction binding.
    private sealed class EmptyBeltPathState
    {
        internal int PathId;
        internal int AnchorId;
        internal int ChangingSlot;
        internal int[] EntityIds = Array.Empty<int>();
        internal string BindingHash = string.Empty;
        internal string PreservationHash = string.Empty;
    }

    private sealed class BeltDestinationState
    {
        internal int EntityId;
        internal Vector3 Position;
        internal EmptyBeltPathState Path = null!;
        internal EmptyBeltPathState? SourcePath;
        internal List<FactoryConnectionSnapshot> Connections = new List<FactoryConnectionSnapshot>();
        internal string BindingHash => CanonicalStateHash.Combine("belt-empty-join-v1", Path.BindingHash, SourcePath?.BindingHash);
    }

    private static bool TryCaptureEmptyBeltPath(PlanetFactory factory, int anchorId, int changingSlot,
        out EmptyBeltPathState? state, out string reason)
    {
        state = null;
        reason = "belt_join_path_identity_unavailable";
        var identity = new List<object?>();
        if ((changingSlot != 0 && changingSlot != 1) || !AppendBeltSourceObject(factory, anchorId, identity)) return false;
        var traffic = factory.cargoTraffic;
        var entity = factory.entityPool[anchorId];
        if (entity.protoId != 2001 || entity.tilt != 0 || traffic?.beltPool is null || traffic.pathPool is null
            || entity.beltId <= 0 || entity.beltId >= traffic.beltCursor || entity.beltId >= traffic.beltPool.Length) return false;
        var belt = traffic.beltPool[entity.beltId];
        if (belt.id != entity.beltId || belt.entityId != anchorId || belt.segPathId <= 0
            || belt.segPathId >= traffic.pathCursor || belt.segPathId >= traffic.pathPool.Length) return false;
        var path = traffic.GetCargoPath(belt.segPathId);
        if (path is null || path.id != belt.segPathId || path.closed || path.buffer is null
            || !ReferenceEquals(path.cargoContainer, factory.cargoContainer)) return false;
        var buffer = path.buffer;
        reason = "belt_join_path_buffer_busy";
        if (!Monitor.TryEnter(buffer)) return false;
        try
        {
            if (!NativeBeltPathCapture.TryRead(path, out var export, out var captureReason))
            { reason = "belt_join_" + captureReason; return false; }
            reason = "belt_join_requires_empty_open_independent_path";
            if (export!.Closed || export.OutputPathId != 0 || export.InputPathIds.Count != 0
                || !BeltUpgradePathPolicy.TryLocateAllCargo(export, out var cargo, out _) || cargo.Count != 0
                || (changingSlot == 0 ? export.BeltIds.Last() : export.BeltIds.First()) != belt.id) return false;
            var members = new List<int>();
            var fields = new List<object?> { export.Id, export.Length, export.OutputPathId, export.OutputIndex,
                Convert.ToBase64String(export.Geometry), Convert.ToBase64String(export.CargoBuffer),
                string.Join(",", export.Speeds), string.Join(",", export.BeltIds), string.Join(",", export.InputPathIds) };
            foreach (var id in export.BeltIds)
            {
                reason = "belt_join_path_member_invalid";
                if (id <= 0 || id >= traffic.beltCursor || id >= traffic.beltPool.Length) return false;
                var member = traffic.beltPool[id];
                if (member.id != id || member.segPathId != path.id || member.segIndex < 0 || member.segLength <= 0
                    || (long)member.segIndex + member.segLength > export.Length
                    || !AppendBeltSourceObject(factory, member.entityId, fields)
                    || factory.entityPool[member.entityId].beltId != id
                    || factory.entityPool[member.entityId].protoId != 2001 || factory.entityPool[member.entityId].tilt != 0) return false;
                members.Add(member.entityId);
                fields.Add(id); fields.Add(member.segIndex); fields.Add(member.segLength); fields.Add(member.segPivotOffset);
            }
            if (members.Distinct().Count() != members.Count) return false;
            // Require a simple chain, not a side input, device-fed path or prebuild
            // feed. Only the declared endpoint slot may change during creation.
            for (var i = 0; i < members.Count; i++)
            {
                var component = traffic.beltPool[factory.entityPool[members[i]].beltId];
                var previousBelt = i > 0 ? factory.entityPool[members[i - 1]].beltId : 0;
                var nextBelt = i + 1 < members.Count ? factory.entityPool[members[i + 1]].beltId : 0;
                var inputs = new[] { component.backInputId, component.leftInputId, component.rightInputId }.Where(id => id != 0).ToArray();
                if (component.outputId != nextBelt || component.mainInputId != previousBelt
                    || !inputs.SequenceEqual(previousBelt == 0 ? Array.Empty<int>() : new[] { previousBelt }))
                { reason = "belt_join_component_path_topology_mismatch"; return false; }
                fields.Add(component.outputId); fields.Add(component.mainInputId);
                fields.Add(component.backInputId); fields.Add(component.leftInputId); fields.Add(component.rightInputId);
                for (var slot = 0; slot < 16; slot++)
                {
                    if (members[i] == anchorId && slot == changingSlot) continue;
                    factory.ReadObjectConn(members[i], slot, out var output, out var other, out var otherSlot);
                    var expected = slot == 0 ? (i + 1 < members.Count ? members[i + 1] : 0)
                        : slot == 1 ? (i > 0 ? members[i - 1] : 0) : 0;
                    if (slot < 4)
                    {
                        if (other != expected || (other != 0 && (output != (slot == 0) || otherSlot != (slot == 0 ? 1 : 0))))
                        { reason = "belt_join_path_not_simple_open_chain"; return false; }
                    }
                    else if (other != 0 && !output)
                    { reason = "belt_join_external_input_unsupported"; return false; }
                }
            }
            if (!TryEmptyBeltMembersHash(factory, members, anchorId, changingSlot, false, out var topology)
                || !TryEmptyBeltMembersHash(factory, members, anchorId, changingSlot, true, out var preservation)) return false;
            fields.Add(topology);
            state = new EmptyBeltPathState { PathId = path.id, AnchorId = anchorId, ChangingSlot = changingSlot,
                EntityIds = members.ToArray(), BindingHash = CanonicalStateHash.Combine("belt-empty-path-v1", fields.ToArray()),
                PreservationHash = preservation };
            reason = string.Empty;
            return true;
        }
        finally { Monitor.Exit(buffer); }
    }

    private static bool TryEmptyBeltMembersHash(PlanetFactory factory, IReadOnlyList<int> members,
        int anchorId, int changingSlot, bool nativeRotation, out string hash)
    {
        hash = string.Empty;
        if (members.Count < 1 || members.Count > BeltUpgradePathPolicy.MaximumBelts) return false;
        var memberSet = new HashSet<int>(members);
        if (memberSet.Count != members.Count) return false;
        var fields = new List<object?>();
        foreach (var id in members)
        {
            if (!AppendBeltSourceObject(factory, id, fields, nativeRotation)) return false;
            for (var slot = 0; slot < 16; slot++)
            {
                if (id == anchorId && slot == changingSlot) continue;
                factory.ReadObjectConn(id, slot, out var output, out var other, out var otherSlot);
                fields.Add(slot); fields.Add(output); fields.Add(other); fields.Add(otherSlot);
                if (other == 0) continue;
                if (other == id || otherSlot < 0 || otherSlot >= 16 || other <= 0
                    || other >= factory.entityCursor || other >= factory.entityPool.Length
                    || (long)other * 16 + 16 > factory.entityConnPool.Length || factory.entityPool[other].id != other) return false;
                factory.ReadObjectConn(other, otherSlot, out var reverseOutput, out var reverseId, out var reverseSlot);
                if (reverseOutput == output || reverseId != id || reverseSlot != slot) return false;
                if (memberSet.Contains(other)) continue;
                // External consumers may remain, but cannot be unobserved feeds.
                if (!output || !AppendBeltSourceObject(factory, other, fields, nativeRotation)) return false;
                for (var s = 0; s < 16; s++)
                {
                    factory.ReadObjectConn(other, s, out var o, out var n, out var p);
                    fields.Add(s); fields.Add(o); fields.Add(n); fields.Add(p);
                }
            }
        }
        hash = CanonicalStateHash.Combine(nativeRotation ? "belt-empty-members-preserved-v1" : "belt-empty-members-exact-v1", fields.ToArray());
        return true;
    }

    private static bool TryCaptureBeltDestination(PlanetFactory factory, ItemProto item, EndpointPoint destination,
        BeltSourceState? source, out BeltDestinationState? state, out string reason)
    {
        state = null;
        if (!TryCaptureEmptyBeltPath(factory, destination.ObjectId, 1, out var path, out reason)) return false;
        var connections = ReadBeltJoinConnections(factory, destination.ObjectId);
        var entity = factory.entityPool[destination.ObjectId];
        if (!BeltDestinationReusePolicy.Supports(destination.ObjectId, entity.protoId, item.ID, destination.Slot,
            entity.tilt, true, connections.Skip(1).Take(3).Count(c => c.OtherObjectId != 0), 0, 0, 0))
        { reason = "belt_destination_requires_same_grade_empty_open_head"; return false; }
        EmptyBeltPathState? sourcePath = null;
        if (source is not null && (!TryCaptureEmptyBeltPath(factory, source.EntityId, 0, out sourcePath, out reason)
            || sourcePath!.PathId == path!.PathId || sourcePath.EntityIds.Intersect(path.EntityIds).Any()))
        { reason = "belt_join_source_path_must_be_empty_independent_and_unfed: " + reason; return false; }
        // The eventual concatenated path must also remain within our readable bound.
        if (path!.EntityIds.Length + (sourcePath?.EntityIds.Length ?? 0) + 2 > BeltUpgradePathPolicy.MaximumBelts)
        { reason = "belt_join_combined_path_too_large"; return false; }
        state = new BeltDestinationState { EntityId = destination.ObjectId, Position = entity.pos,
            Path = path, SourcePath = sourcePath, Connections = connections };
        return true;
    }

    private static List<FactoryConnectionSnapshot> ReadBeltJoinConnections(PlanetFactory factory, int id)
    {
        var connections = new List<FactoryConnectionSnapshot>();
        for (var slot = 0; slot < 16; slot++)
        {
            factory.ReadObjectConn(id, slot, out var output, out var other, out var otherSlot);
            connections.Add(new FactoryConnectionSnapshot { Slot = slot, IsOutput = output, OtherObjectId = other, OtherSlot = otherSlot });
        }
        return connections;
    }

    private static bool EmptyJoinBindingsMatch(PlanetFactory factory, BeltDestinationState bound)
    {
        if (!TryCaptureEmptyBeltPath(factory, bound.EntityId, 1, out var target, out _)
            || !BeltSourceReusePolicy.SameEvidence(bound.Path.BindingHash, target!.BindingHash)) return false;
        return bound.SourcePath is null || (TryCaptureEmptyBeltPath(factory, bound.SourcePath.AnchorId, 0, out var source, out _)
            && BeltSourceReusePolicy.SameEvidence(bound.SourcePath.BindingHash, source!.BindingHash)
            && source.PathId != target.PathId && !source.EntityIds.Intersect(target.EntityIds).Any());
    }

    private static bool TryAttachDestinationCover(PlanetFactory factory, ItemProto item, IReadOnlyList<BuildStepPlan> steps,
        List<BuildPreview> previews, SpherewrightPathBuildTool tool, out BuildPreview? cover, out string reason)
    {
        cover = null;
        reason = "belt_destination_binding_changed";
        var last = steps[steps.Count - 1];
        var bound = last.DestinationBeltAnchor;
        if (bound is null)
        {
            var id = last.OutputObjectId;
            if (id > 0 && (id >= factory.entityCursor || id >= factory.entityPool.Length
                || factory.entityPool[id].id != id || factory.entityPool[id].beltId > 0)) return false;
            reason = string.Empty;
            return true;
        }
        if (steps.Count < 2 || item.ID != 2001 || steps.Take(steps.Count - 1).Any(s => s.DestinationBeltAnchor is not null)
            || steps.Any(s => (s.BeltPathMode != BeltPathModes.NativeGrid
                && (s.BeltPathMode != BeltPathModes.NativeGeodesic || bound.SourcePath is null)) || s.Tilt != 0)
            || last.OutputObjectId != bound.EntityId || last.OutputFromSlot != 0 || last.OutputToSlot != 1
            || last.OutputStepIndex != -1 || (steps[0].SourceBeltAnchor is null) != (bound.SourcePath is null)
            || (bound.SourcePath is null && steps[0].InputObjectId != 0)
            || (bound.SourcePath is not null && bound.SourcePath.AnchorId != steps[0].InputObjectId)
            || bound.Path.EntityIds.Length + (bound.SourcePath?.EntityIds.Length ?? 0) + steps.Count > BeltUpgradePathPolicy.MaximumBelts
            || !EmptyJoinBindingsMatch(factory, bound)) return false;
        var connections = ReadBeltJoinConnections(factory, bound.EntityId);
        if (connections.Skip(1).Take(3).Any(c => c.OtherObjectId != 0)) return false;
        cover = new BuildPreview { item = item, desc = item.prefabDesc, lpos = bound.Position, lpos2 = bound.Position,
            lrot = Maths.SphericalRotation(bound.Position, 0f), lrot2 = Maths.SphericalRotation(bound.Position, 0f),
            tilt = 0, isConnNode = true, needModel = false, condition = EBuildCondition.Ok,
            coverObjId = bound.EntityId, willRemoveCover = false };
        // Native target-cover convention: the last NEW preview points at the
        // cover; the cover itself does not rewrite the target's old output.
        previews[previews.Count - 1].outputObjId = 0;
        previews[previews.Count - 1].output = cover;
        tool.buildPreviews.Add(cover);
        reason = string.Empty;
        return true;
    }

    private static bool DestinationCoverMatches(BuildPreview? cover, BeltDestinationState? bound,
        IReadOnlyList<BuildPreview> previews) => bound is null ? cover is null
        : cover is not null && previews.Count >= 2 && BeltSourceReusePolicy.IsNonRemovingCover(bound.EntityId, cover.coverObjId, cover.willRemoveCover)
            && cover.condition == EBuildCondition.Ok && Vector3.Distance(cover.lpos, bound.Position) <= .01f
            && Vector3.Distance(cover.lpos2, bound.Position) <= .01f && cover.tilt == 0
            && cover.inputObjId == 0 && cover.input is null && cover.outputObjId == 0 && cover.output is null
            && ReferenceEquals(previews[previews.Count - 1].output, cover)
            && previews[previews.Count - 1].outputObjId == 0
            && previews[previews.Count - 1].outputFromSlot == 0 && previews[previews.Count - 1].outputToSlot == 1;

    private static bool CompletePreparedGroundBeltPath(IReadOnlyList<BuildStepPlan> steps, float radius,
        IReadOnlyList<BuildStepPlan>? expected = null)
    {
        if (steps.Count < 2) return false;
        var source = steps[0].SourceBeltAnchor;
        var target = steps[steps.Count - 1].DestinationBeltAnchor;
        if (target is null && steps.Any(s => s.SourceBeltAnchor is not null || s.DestinationBeltAnchor is not null
            || s.InputObjectId != 0 || s.OutputObjectId != 0)) return false;
        if (target is not null && (source is null || target.SourcePath is null || steps.Any(s => s.ItemId != 2001 || s.Tilt != 0)
            || steps[0].InputObjectId != source.EntityId || steps[steps.Count - 1].OutputObjectId != target.EntityId)) return false;
        var full = steps.Select(step => Snapshot(step.Position)).ToList();
        if (source is not null) full.Insert(0, Snapshot(source.Position));
        if (target is not null) full.Add(Snapshot(target.Position));
        expected ??= steps;
        var start = expected[0].SourceBeltAnchor?.Position ?? expected[0].Position;
        var end = expected[expected.Count - 1].DestinationBeltAnchor?.Position ?? expected[expected.Count - 1].Position;
        return BeltPathRoutingPolicy.CompleteGroundPath(full, BeltBuildOccupancyPolicy.MaximumPathPoints,
            Snapshot(start), Snapshot(end), radius);
    }

    private static void CreateWithBeltCoverProof(PlanetFactory factory, SpherewrightPathBuildTool tool,
        BuildPreview? sourceCover, BuildPreview? destinationCover, List<BuildPreview> previews,
        BeltSourceState? source, BeltDestinationState? destination)
    {
        if (destination is not null && (!DestinationCoverMatches(destinationCover, destination, previews)
            || !EmptyJoinPreviewGraphMatches(tool, sourceCover, destinationCover!, previews)
            || !EmptyJoinBindingsMatch(factory, destination)
            || ReadBeltJoinConnections(factory, destination.EntityId).Skip(1).Take(3).Any(c => c.OtherObjectId != 0)))
            throw new InvalidOperationException("The exact empty target path changed before native construction.");
        CreateWithSourceCoverProof(factory, tool, sourceCover, previews, source);
        if (destination is not null && (destinationCover!.objId != destination.EntityId
            || !DestinationCoverMatches(destinationCover, destination, previews) || !EmptyJoinBindingsMatch(factory, destination)
            || !ProvesCreatedEmptyJoin(factory, source, destination, previews)
            || !ProvesDestinationInput(factory, destination, previews[previews.Count - 1].objId, true)))
            throw new InvalidOperationException("Native target reuse path, cargo or reciprocal connection proof failed; do not replay.");
    }

    private static bool EmptyJoinPreviewGraphMatches(SpherewrightPathBuildTool tool, BuildPreview? source,
        BuildPreview target, IReadOnlyList<BuildPreview> previews)
    {
        var full = new List<BuildPreview>();
        if (source is not null) full.Add(source);
        full.AddRange(previews); full.Add(target);
        if (!tool.buildPreviews.SequenceEqual(full)) return false;
        for (var i = 0; i < previews.Count; i++)
        {
            var p = previews[i];
            if (p.coverObjId != 0 || p.willRemoveCover || p.outputObjId != 0 || p.outputFromSlot != 0 || p.outputToSlot != 1
                || !ReferenceEquals(p.output, i + 1 < previews.Count ? previews[i + 1] : target)
                || p.inputFromSlot != 0 || p.inputToSlot != 1
                || p.inputObjId != (i == 0 && source is not null ? source.coverObjId : 0)
                || !ReferenceEquals(p.input, i > 0 ? previews[i - 1] : null)) return false;
        }
        return true;
    }

    private static bool ProvesCreatedEmptyJoin(PlanetFactory factory, BeltSourceState? source,
        BeltDestinationState destination, IReadOnlyList<BuildPreview> previews)
    {
        if (factory.prebuildPool is null || factory.prebuildConnPool is null
            || previews.Select(p => p.objId).Distinct().Count() != previews.Count) return false;
        for (var i = 0; i < previews.Count; i++)
        {
            var id = previews[i].objId;
            if (id >= 0 || id == int.MinValue || -id >= factory.prebuildCursor || -id >= factory.prebuildPool.Length
                || (long)-id * 16 + 16 > factory.prebuildConnPool.Length) return false;
            var prebuild = factory.prebuildPool[-id];
            if (prebuild.id != -id || prebuild.protoId != 2001 || prebuild.isDestroyed || prebuild.tilt != 0
                || Vector3.Distance(prebuild.pos, previews[i].lpos) > .01f) return false;
            for (var slot = 0; slot < 16; slot++)
            {
                var expected = slot == 0 ? (i + 1 < previews.Count ? previews[i + 1].objId : destination.EntityId)
                    : slot == 1 ? (i > 0 ? previews[i - 1].objId : source?.EntityId ?? 0) : 0;
                factory.ReadObjectConn(id, slot, out var output, out var other, out var otherSlot);
                if (other != expected || (other != 0 && (output != (slot == 0) || otherSlot != (slot == 0 ? 1 : 0)))) return false;
            }
        }
        return true;
    }

    private static bool ProvesDestinationInput(PlanetFactory factory, BeltDestinationState bound, int lastNewId, bool prebuild)
    {
        if (!BeltDestinationReusePolicy.ProvesOnlyInputChanged(bound.Connections,
            ReadBeltJoinConnections(factory, bound.EntityId), lastNewId)) return false;
        if (prebuild)
        {
            if (lastNewId >= 0 || lastNewId == int.MinValue || factory.prebuildPool is null || factory.prebuildConnPool is null
                || -lastNewId >= factory.prebuildCursor || -lastNewId >= factory.prebuildPool.Length
                || (long)-lastNewId * 16 + 16 > factory.prebuildConnPool.Length || factory.prebuildPool[-lastNewId].id != -lastNewId) return false;
        }
        else if (lastNewId <= 0 || lastNewId >= factory.entityCursor || lastNewId >= factory.entityPool.Length
            || (long)lastNewId * 16 + 16 > factory.entityConnPool.Length || factory.entityPool[lastNewId].id != lastNewId) return false;
        factory.ReadObjectConn(lastNewId, 0, out var output, out var other, out var slot);
        return output && other == bound.EntityId && slot == 1;
    }

    private static bool ProvesCompletedEmptyJoin(PlanetFactory factory, BeltDestinationState bound, IReadOnlyList<int> newIds)
    {
        var expected = (bound.SourcePath?.EntityIds ?? Array.Empty<int>()).Concat(newIds).Concat(bound.Path.EntityIds).ToArray();
        if (expected.Length > BeltUpgradePathPolicy.MaximumBelts
            || !TryCaptureEmptyBeltPath(factory, expected[expected.Length - 1], 0, out var joined, out _)
            || !BeltDestinationReusePolicy.ProvesJoinedMembership(bound.SourcePath?.EntityIds, newIds, bound.Path.EntityIds, joined!.EntityIds)
            || !ProvesDestinationInput(factory, bound, newIds[newIds.Count - 1], false)) return false;
        // The final open tail is NOT one of the approved mutable endpoints.
        factory.ReadObjectConn(expected[expected.Length - 1], 0, out _, out var output, out _);
        if (output != 0 || !TryEmptyBeltMembersHash(factory, bound.Path.EntityIds, bound.EntityId, 1, true, out var targetHash)
            || !BeltSourceReusePolicy.SameEvidence(bound.Path.PreservationHash, targetHash)) return false;
        return bound.SourcePath is null || (TryEmptyBeltMembersHash(factory, bound.SourcePath.EntityIds,
            bound.SourcePath.AnchorId, 0, true, out var sourceHash)
            && BeltSourceReusePolicy.SameEvidence(bound.SourcePath.PreservationHash, sourceHash));
    }
}
