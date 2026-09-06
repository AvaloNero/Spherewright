using System.IO;
using System.Security.Cryptography;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

/// <summary>Whole affected native path proof; a local beltCargo sample is insufficient.</summary>
public static class BeltUpgradePathPolicy
{
    public const int MaximumPathCells = 8192;
    public const int MaximumBelts = 512;
    public const int MaximumInputPaths = 128;
    public const int MaximumExportBytes = 1024 * 1024;

    public static bool TryReadExport(byte[]? bytes, int expectedPathId, int expectedPathLength,
        out BeltPathExportSnapshot? path, out string? reason)
    {
        path = null;
        reason = "belt_path_export_invalid";
        if (bytes is null || bytes.Length < 45 || bytes.Length > MaximumExportBytes
            || expectedPathId <= 0 || expectedPathLength < 1 || expectedPathLength > MaximumPathCells) return false;
        using var stream = new MemoryStream(bytes, writable: false);
        using var reader = new BinaryReader(stream);
        var version = reader.ReadInt32();
        var id = reader.ReadInt32();
        var capacity = reader.ReadInt32();
        var length = reader.ReadInt32();
        var chunkCapacity = reader.ReadInt32();
        var chunkCount = reader.ReadInt32();
        var updateLength = reader.ReadInt32();
        var closedByte = reader.ReadByte();
        var outputId = reader.ReadInt32();
        var outputIndex = reader.ReadInt32();
        var beltCount = reader.ReadInt32();
        var inputCount = reader.ReadInt32();
        if (version != 1 || id != expectedPathId || length != expectedPathLength || capacity < length
            || chunkCount < 1 || chunkCount > length || chunkCapacity < chunkCount
            || updateLength < 0 || updateLength > length || closedByte > 1
            || beltCount < 1 || beltCount > MaximumBelts || inputCount < 0 || inputCount > MaximumInputPaths
            || outputId < 0 || (outputId == 0 ? outputIndex != -1 : outputIndex < 0)) return false;
        var closed = closedByte == 1;
        if (closed ? length < 19 || outputId != id || outputIndex != 4 : outputId == id) return false;
        var requiredBytes = 45L + 29L * length + 12L * chunkCount + 4L * (beltCount + inputCount);
        if (requiredBytes != bytes.Length) return false;
        var cargo = reader.ReadBytes(length);
        var speeds = new int[length];
        var end = 0;
        for (var chunk = 0; chunk < chunkCount; chunk++)
        {
            var start = reader.ReadInt32();
            var cells = reader.ReadInt32();
            var speed = reader.ReadInt32();
            if (start != end || cells < 1 || speed < 1 || (long)start + cells > length) return false;
            end = start + cells;
            for (var cell = start; cell < end; cell++) speeds[cell] = speed;
        }
        if (end != length) return false;
        var geometry = reader.ReadBytes(length * 28);
        using (var geometryStream = new MemoryStream(geometry, writable: false))
        using (var geometryReader = new BinaryReader(geometryStream))
            for (var value = 0; value < length * 7; value++)
            {
                var coordinate = geometryReader.ReadSingle();
                if (float.IsNaN(coordinate) || float.IsInfinity(coordinate)) return false;
            }
        var belts = new List<int>();
        var inputs = new List<int>();
        for (var i = 0; i < beltCount; i++) belts.Add(reader.ReadInt32());
        for (var i = 0; i < inputCount; i++) inputs.Add(reader.ReadInt32());
        if (belts.Any(value => value <= 0) || belts.Distinct().Count() != belts.Count
            || inputs.Any(value => value <= 0) || inputs.Distinct().Count() != inputs.Count
            || inputs.Contains(id) != closed) return false;
        path = new BeltPathExportSnapshot(id, closed, outputId, outputIndex, cargo, geometry, speeds, belts, inputs);
        reason = null;
        return true;
    }

    // Reuse the strict local native-frame decoder in bounded windows; deduplicate a
    // crossing packet, but never accept a repeated cargo ID at a different frame.
    public static bool TryLocateAllCargo(BeltPathExportSnapshot path,
        out List<BeltCargoReference> references, out string? reason)
    {
        references = new List<BeltCargoReference>();
        reason = "belt_path_cargo_unavailable";
        if (!ValidPath(path)) return false;
        var seen = new Dictionary<int, BeltCargoReference>();
        for (var start = 0; start < path.Length; start += BeltCargoObservationPolicy.MaximumSegmentCells)
        {
            var count = Math.Min(BeltCargoObservationPolicy.MaximumSegmentCells, path.Length - start);
            if (!BeltCargoObservationPolicy.TryGetWindow(path.Length, start, count,
                    out var guardStart, out var guardLength, out reason)) return false;
            var window = new byte[guardLength];
            Array.Copy(path.CargoBuffer, guardStart, window, 0, guardLength);
            if (!BeltCargoObservationPolicy.TryLocateCargo(window, guardStart, path.Length,
                    start, count, path.Closed, out var local, out reason)) return false;
            foreach (var reference in local)
            {
                if (seen.TryGetValue(reference.CargoId, out var previous))
                {
                    var difference = reference.ObservedPathCell - previous.ObservedPathCell;
                    if (difference < 0 || difference >= BeltCargoObservationPolicy.CargoCellLength)
                    {
                        reason = "belt_path_duplicate_cargo_reference";
                        return false;
                    }
                }
                else seen.Add(reference.CargoId, reference);
            }
        }
        references = seen.Values.OrderBy(value => value.CargoId).ToList();
        reason = null;
        return true;
    }

    public static bool TryBindingHash(BeltUpgradePathState? state, out string hash, out string? reason)
    {
        hash = string.Empty;
        if (!ValidateState(state, out reason)) return false;
        hash = CanonicalStateHash.Combine("upgrade-belt-path-binding-v1", StaticTopologyHash(state!),
            string.Join(",", state!.Path.Speeds),
            string.Join(",", state.Members.Select(member => member.Speed)),
            state.TargetEntityId, state.TargetBeltId, Target(state).Entity.ItemId);
        return true;
    }

    public static bool ProvesUpgrade(BeltUpgradePathState? before, BeltUpgradePathState? after,
        int sourceItemId, int targetItemId, int expectedTargetSpeed, out string? reason)
    {
        reason = "belt_path_upgrade_not_proven";
        if (!ValidateState(before, out _) || !ValidateState(after, out _) || !IsBelt(sourceItemId)
            || !IsBelt(targetItemId) || targetItemId <= sourceItemId || expectedTargetSpeed < 1) return false;
        var oldTarget = Target(before!);
        var newTarget = Target(after!);
        if (oldTarget.Entity.ItemId != sourceItemId || newTarget.Entity.ItemId != targetItemId
            || oldTarget.Speed >= expectedTargetSpeed || newTarget.Speed != expectedTargetSpeed
            || StaticTopologyHash(before!) != StaticTopologyHash(after!)
            || !before!.Path.CargoBuffer.SequenceEqual(after!.Path.CargoBuffer)
            || !before.Path.Geometry.SequenceEqual(after.Path.Geometry)
            || !TryCargoHash(before, out var oldCargoHash) || !TryCargoHash(after, out var newCargoHash)
            || oldCargoHash != newCargoHash) return false;
        var expectedSpeeds = before.Path.Speeds.ToArray();
        var index = before.Members.FindIndex(member => member.BeltId == before.TargetBeltId);
        var changeEnd = index == before.Members.Count - 1
            ? before.Path.Length : oldTarget.SegmentStart + oldTarget.SegmentLength;
        for (var cell = oldTarget.SegmentStart; cell < changeEnd; cell++) expectedSpeeds[cell] = expectedTargetSpeed;
        // Exactly the native rear-speed branch AFTER InsertChunk/ArrangeChunk.
        if (before.Path.Closed && oldTarget.SegmentStart == 0
            && expectedSpeeds.Skip(1).Where((value, cell) => value != expectedSpeeds[cell]).Any())
        {
            var rearSpeed = expectedSpeeds[expectedSpeeds.Length - 1];
            for (var cell = expectedSpeeds.Length - 9; cell < expectedSpeeds.Length; cell++)
                expectedSpeeds[cell] = rearSpeed;
        }
        if (!expectedSpeeds.SequenceEqual(after.Path.Speeds)) return false;
        for (var member = 0; member < before.Members.Count; member++)
            if (member != index && before.Members[member].Speed != after.Members[member].Speed) return false;
        reason = null;
        return true;
    }

    public static bool TryCargoHash(BeltUpgradePathState? state, out string hash)
    {
        hash = string.Empty;
        if (state is null || !TryLocateAllCargo(state.Path, out var references, out _)
            || state.Cargo is null || state.Cargo.Count != references.Count) return false;
        var expectedIds = new HashSet<int>(references.Select(reference => reference.CargoId));
        var samples = new List<string>();
        foreach (var cargo in state.Cargo.OrderBy(cargo => cargo?.CargoId))
        {
            if (cargo is null || !expectedIds.Remove(cargo.CargoId) || cargo.ItemId <= 0 || cargo.ItemId > short.MaxValue
                || cargo.StackCount < 1 || cargo.StackCount > byte.MaxValue || cargo.Inc < 0 || cargo.Inc > byte.MaxValue
                || string.IsNullOrWhiteSpace(cargo.PoseHash)) return false;
            samples.Add(CanonicalStateHash.Combine("upgrade-path-cargo", cargo.CargoId, cargo.ItemId,
                cargo.StackCount, cargo.Inc, cargo.PoseHash));
        }
        if (expectedIds.Count != 0) return false;
        hash = CanonicalStateHash.Combine("upgrade-path-cargo-v1", BytesHash(state.Path.CargoBuffer), string.Join(",", samples));
        return true;
    }

    public static List<BeltCargoItemSnapshot> CargoTotals(BeltUpgradePathState state) => state.Cargo
        .GroupBy(cargo => cargo.ItemId).OrderBy(group => group.Key)
        .Select(group => new BeltCargoItemSnapshot
        {
            ItemId = group.Key, CargoStackCount = group.Count(),
            Count = group.Sum(cargo => cargo.StackCount), Inc = group.Sum(cargo => cargo.Inc),
        }).ToList();

    public static string BytesHash(byte[] bytes)
    {
        using var sha = SHA256.Create();
        return "sha256:" + BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
    }

    private static bool ValidateState(BeltUpgradePathState? state, out string? reason)
    {
        reason = "belt_path_identity_or_members_invalid";
        if (state is null || !ValidPath(state.Path) || state.TargetEntityId <= 0 || state.TargetBeltId <= 0
            || state.Members is null || state.Members.Count != state.Path.BeltIds.Count
            || string.IsNullOrWhiteSpace(state.AdjacentRoutingHash)) return false;
        var entityIds = new HashSet<int>();
        for (var index = 0; index < state.Members.Count; index++)
        {
            var member = state.Members[index];
            if (member is null || member.BeltId != state.Path.BeltIds[index] || member.Speed < 1
                || member.SegmentStart < 0 || member.SegmentLength < 1 || member.PivotOffset < 0
                || member.PivotOffset >= member.SegmentLength
                || (long)member.SegmentStart + member.SegmentLength > state.Path.Length
                || member.Entity is null || member.Entity.ObjectId <= 0 || !entityIds.Add(member.Entity.ObjectId)
                || member.Entity.ObjectKind != FactoryObjectKinds.Entity || member.Entity.ComponentKind != "belt"
                || member.Entity.PlanetId <= 0 || string.IsNullOrWhiteSpace(member.Entity.SessionId)
                || member.Entity.PlanetId != state.Members[0].Entity.PlanetId
                || member.Entity.SessionId != state.Members[0].Entity.SessionId
                || !IsBelt(member.Entity.ItemId) || member.Entity.Connections is null || member.Entity.Connections.Count > 16
                || !FinitePose(member.Entity) || member.OutputBeltId < 0 || member.BackInputBeltId < 0
                || member.LeftInputBeltId < 0 || member.RightInputBeltId < 0) return false;
            var slots = new HashSet<int>();
            foreach (var edge in member.Entity.Connections)
                if (edge is null || edge.Slot < 0 || edge.Slot >= 16 || !slots.Add(edge.Slot)
                    || edge.OtherObjectId <= 0 || edge.OtherSlot < 0 || edge.OtherSlot >= 16) return false;
        }
        if (state.Members.Count(member => member.BeltId == state.TargetBeltId
                && member.Entity.ObjectId == state.TargetEntityId) != 1) return false;
        reason = null;
        return true;
    }

    private static bool ValidPath(BeltPathExportSnapshot? path) => path is not null
        && path.Id > 0 && path.Length > 0 && path.Length <= MaximumPathCells
        && path.CargoBuffer.Length == path.Length && path.Geometry.Length == path.Length * 28
        && path.Speeds.Length == path.Length && path.Speeds.All(speed => speed > 0)
        && path.BeltIds.Count > 0 && path.BeltIds.Count <= MaximumBelts
        && path.BeltIds.All(id => id > 0) && path.BeltIds.Distinct().Count() == path.BeltIds.Count
        && path.InputPathIds.Count <= MaximumInputPaths && path.InputPathIds.All(id => id > 0)
        && path.InputPathIds.Distinct().Count() == path.InputPathIds.Count
        && path.InputPathIds.Contains(path.Id) == path.Closed
        && (path.Closed ? path.Length >= 19 && path.OutputPathId == path.Id && path.OutputIndex == 4
            : path.OutputPathId >= 0 && path.OutputPathId != path.Id
                && (path.OutputPathId == 0 ? path.OutputIndex == -1 : path.OutputIndex >= 0));

    private static BeltUpgradeMember Target(BeltUpgradePathState state) =>
        state.Members.Single(member => member.BeltId == state.TargetBeltId);

    private static string StaticTopologyHash(BeltUpgradePathState state)
    {
        int Object(int id) => id == state.TargetEntityId ? -1 : id;
        int Belt(int id) => id == state.TargetBeltId ? -1 : id;
        var members = state.Members.Select(member =>
        {
            var entity = member.Entity;
            var position = entity.Position;
            var rotation = entity.Rotation;
            var edges = entity.Connections.OrderBy(edge => edge.Slot).Select(edge =>
                CanonicalStateHash.Combine("path-edge", edge.Slot, edge.IsOutput, Object(edge.OtherObjectId), edge.OtherSlot));
            return CanonicalStateHash.Combine("path-member", entity.SessionId, entity.PlanetId,
                Belt(member.BeltId), Object(entity.ObjectId),
                member.BeltId == state.TargetBeltId ? -1 : entity.ItemId,
                member.SegmentStart, member.SegmentLength, member.PivotOffset,
                Belt(member.OutputBeltId), Belt(member.BackInputBeltId), Belt(member.LeftInputBeltId), Belt(member.RightInputBeltId),
                position.X, position.Y, position.Z, rotation.X, rotation.Y, rotation.Z, rotation.W, string.Join(",", edges));
        });
        return CanonicalStateHash.Combine("path-topology-v1", state.Path.Id, state.Path.Length, state.Path.Closed,
            state.Path.OutputPathId, state.Path.OutputIndex, string.Join(",", state.Path.InputPathIds),
            BytesHash(state.Path.Geometry), state.AdjacentRoutingHash, string.Join(",", members));
    }

    private static bool FinitePose(FactoryEntitySnapshot entity) => entity.Position is not null && entity.Rotation is not null
        && new[] { entity.Position.X, entity.Position.Y, entity.Position.Z,
            entity.Rotation.X, entity.Rotation.Y, entity.Rotation.Z, entity.Rotation.W }
            .All(value => !float.IsNaN(value) && !float.IsInfinity(value));

    private static bool IsBelt(int itemId) => itemId == 2001 || itemId == 2002 || itemId == 2003;
}

public sealed class BeltPathExportSnapshot
{
    internal BeltPathExportSnapshot(int id, bool closed, int outputPathId, int outputIndex,
        byte[] cargo, byte[] geometry, int[] speeds, List<int> belts, List<int> inputs)
    {
        Id = id; Closed = closed; OutputPathId = outputPathId; OutputIndex = outputIndex;
        CargoBuffer = cargo; Geometry = geometry; Speeds = speeds; BeltIds = belts; InputPathIds = inputs;
    }
    public int Id { get; }
    public int Length => CargoBuffer.Length;
    public bool Closed { get; }
    public int OutputPathId { get; }
    public int OutputIndex { get; }
    public byte[] CargoBuffer { get; }
    public byte[] Geometry { get; }
    public int[] Speeds { get; }
    public List<int> BeltIds { get; }
    public List<int> InputPathIds { get; }
}

public sealed class BeltUpgradePathState
{
    public BeltPathExportSnapshot Path { get; set; } = null!;
    public int TargetEntityId { get; set; }
    public int TargetBeltId { get; set; }
    public string AdjacentRoutingHash { get; set; } = string.Empty;
    public List<BeltUpgradeMember> Members { get; set; } = new List<BeltUpgradeMember>();
    public List<BeltUpgradeCargo> Cargo { get; set; } = new List<BeltUpgradeCargo>();
}

public sealed class BeltUpgradeMember
{
    public int BeltId { get; set; }
    public int Speed { get; set; }
    public int SegmentStart { get; set; }
    public int SegmentLength { get; set; }
    public int PivotOffset { get; set; }
    public int OutputBeltId { get; set; }
    public int BackInputBeltId { get; set; }
    public int LeftInputBeltId { get; set; }
    public int RightInputBeltId { get; set; }
    public FactoryEntitySnapshot Entity { get; set; } = new FactoryEntitySnapshot();
}

public sealed class BeltUpgradeCargo
{
    public int CargoId { get; set; }
    public int ItemId { get; set; }
    public int StackCount { get; set; }
    public int Inc { get; set; }
    public string PoseHash { get; set; } = string.Empty;
}
