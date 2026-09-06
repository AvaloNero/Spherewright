using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class BeltUpgradePathPolicyTests
{
    [Fact]
    public void NativeExportProjectionSeparatesCargoGeometrySpeedsAndRouting()
    {
        var state = State();
        Assert.Equal(40, state.Path.Length);
        Assert.Equal(new[] { 10, 20, 30 }, state.Path.BeltIds);
        Assert.Equal(40 * 28, state.Path.Geometry.Length);
        Assert.All(state.Path.Speeds, value => Assert.Equal(1, value));
        Assert.True(BeltUpgradePathPolicy.TryLocateAllCargo(state.Path, out var references, out var reason));
        Assert.Null(reason);
        Assert.Equal(new[] { 0, 1 }, references.Select(value => value.CargoId));
        Assert.Equal(new[] { 5, 25 }, references.Select(value => value.ObservedPathCell));
        Assert.True(BeltUpgradePathPolicy.TryCargoHash(state, out var hash));
        Assert.StartsWith("sha256:", hash);
        var totals = Assert.Single(BeltUpgradePathPolicy.CargoTotals(state));
        Assert.Equal(2, totals.CargoStackCount);
        Assert.Equal(5, totals.Count);
        Assert.Equal(7, totals.Inc);
    }

    [Theory]
    [InlineData("version")]
    [InlineData("identity")]
    [InlineData("capacity")]
    [InlineData("length")]
    [InlineData("chunk-capacity")]
    [InlineData("chunk-zero")]
    [InlineData("chunk-overflow")]
    [InlineData("update-length")]
    [InlineData("closed-byte")]
    [InlineData("open-self-output")]
    [InlineData("output-index")]
    [InlineData("negative-output")]
    [InlineData("belt-zero")]
    [InlineData("belt-limit")]
    [InlineData("input-negative")]
    [InlineData("input-limit")]
    [InlineData("chunk-gap")]
    [InlineData("chunk-length")]
    [InlineData("chunk-overrun")]
    [InlineData("speed-zero")]
    [InlineData("geometry-nan")]
    [InlineData("geometry-infinity")]
    [InlineData("member-zero")]
    [InlineData("duplicate-member")]
    [InlineData("trailing-byte")]
    public void MalformedNativeProjectionFailsClosed(string change)
    {
        var bytes = Export(new byte[40], Enumerable.Repeat(1, 40).ToArray());
        var geometryOffset = 45 + 40 + 12;
        var memberOffset = geometryOffset + 28 * 40;
        switch (change)
        {
            case "version": Set(bytes, 0, 2); break;
            case "identity": Set(bytes, 4, 8); break;
            case "capacity": Set(bytes, 8, 39); break;
            case "length": Set(bytes, 12, 39); break;
            case "chunk-capacity": Set(bytes, 16, 0); break;
            case "chunk-zero": Set(bytes, 20, 0); break;
            case "chunk-overflow": Set(bytes, 20, int.MaxValue); break;
            case "update-length": Set(bytes, 24, 41); break;
            case "closed-byte": bytes[28] = 2; break;
            case "open-self-output": Set(bytes, 29, 7); Set(bytes, 33, 4); break;
            case "output-index": Set(bytes, 33, 0); break;
            case "negative-output": Set(bytes, 29, -1); break;
            case "belt-zero": Set(bytes, 37, 0); break;
            case "belt-limit": Set(bytes, 37, 513); break;
            case "input-negative": Set(bytes, 41, -1); break;
            case "input-limit": Set(bytes, 41, 129); break;
            case "chunk-gap": Set(bytes, 45 + 40, 1); break;
            case "chunk-length": Set(bytes, 45 + 40 + 4, 0); break;
            case "chunk-overrun": Set(bytes, 45 + 40 + 4, int.MaxValue); break;
            case "speed-zero": Set(bytes, 45 + 40 + 8, 0); break;
            case "geometry-nan": Set(bytes, geometryOffset, 0x7fc00000); break;
            case "geometry-infinity": Set(bytes, geometryOffset, 0x7f800000); break;
            case "member-zero": Set(bytes, memberOffset, 0); break;
            case "duplicate-member": Set(bytes, memberOffset + 4, 10); break;
            case "trailing-byte": bytes = bytes.Concat(new byte[1]).ToArray(); break;
        }
        Assert.False(BeltUpgradePathPolicy.TryReadExport(bytes, 7, 40, out var path, out var reason));
        Assert.Null(path);
        Assert.NotNull(reason);
    }

    [Fact]
    public void TruncatedAndRandomInputsNeverEscapeAsPartialEvidence()
    {
        var complete = Export(new byte[40], Enumerable.Repeat(1, 40).ToArray());
        for (var length = 0; length < complete.Length; length++)
        {
            Assert.False(BeltUpgradePathPolicy.TryReadExport(complete.Take(length).ToArray(), 7, 40, out var path, out _));
            Assert.Null(path);
        }
        var random = new Random(9047);
        for (var i = 0; i < 512; i++)
        {
            var bytes = new byte[random.Next(2048)];
            random.NextBytes(bytes);
            Assert.False(BeltUpgradePathPolicy.TryReadExport(bytes, 7, 40, out var path, out _));
            Assert.Null(path);
        }
        Assert.False(BeltUpgradePathPolicy.TryReadExport(null, 7, 40, out _, out _));
        Assert.False(BeltUpgradePathPolicy.TryReadExport(new byte[BeltUpgradePathPolicy.MaximumExportBytes + 1], 7, 40, out _, out _));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(8193)]
    [InlineData(int.MaxValue)]
    public void LogicalSizeIsBoundedBeforeAllocation(int length) =>
        Assert.False(BeltUpgradePathPolicy.TryReadExport(new byte[45], 7, length, out _, out _));

    [Fact]
    public void MaximumPathAndCargoZeroAreSupportedWithCrossWindowPacketsDeduplicated()
    {
        const int length = BeltUpgradePathPolicy.MaximumPathCells;
        var cargo = new byte[length];
        var starts = Enumerable.Range(0, length / 10).Select(index => index * 10).ToArray();
        for (var i = 0; i < starts.Length; i++) PutCargo(cargo, starts[i], i);
        var path = Read(Export(cargo, Enumerable.Repeat(1, length).ToArray()), length);
        Assert.True(BeltUpgradePathPolicy.TryLocateAllCargo(path, out var references, out _));
        Assert.Equal(starts.Length, references.Count);
        Assert.Equal(starts, references.Select(value => value.ObservedPathCell));
        Assert.Equal(0, references[0].CargoId);
    }

    [Fact]
    public void SameCargoIdAtSeparateFramesAcrossWindowsIsRejectedWithoutPartialReferences()
    {
        var cargo = new byte[1100];
        PutCargo(cargo, 505, 0);
        PutCargo(cargo, 1030, 0);
        var path = Read(Export(cargo, Enumerable.Repeat(1, cargo.Length).ToArray()), cargo.Length);
        Assert.False(BeltUpgradePathPolicy.TryLocateAllCargo(path, out var references, out var reason));
        Assert.Empty(references);
        Assert.Equal("belt_path_duplicate_cargo_reference", reason);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IncompleteSeamOrMalformedPacketCannotLookEmpty(bool closed)
    {
        var cargo = new byte[40];
        PutCargo(cargo, 0, 0);
        cargo[0] = 0;
        var path = Read(Export(cargo, Enumerable.Repeat(1, 40).ToArray(), closed: closed), 40);
        Assert.False(BeltUpgradePathPolicy.TryLocateAllCargo(path, out var references, out _));
        Assert.Empty(references);
    }

    [Theory]
    [InlineData("self-input-missing")]
    [InlineData("duplicate-input")]
    [InlineData("negative-input")]
    [InlineData("wrong-output")]
    [InlineData("wrong-index")]
    [InlineData("too-short")]
    public void ClosedPathsRequireExactNativeRouting(string change)
    {
        var length = change == "too-short" ? 18 : 40;
        var inputs = change switch
        {
            "self-input-missing" => Array.Empty<int>(),
            "duplicate-input" => new[] { 7, 7 },
            "negative-input" => new[] { 7, -1 },
            _ => new[] { 7 },
        };
        var bytes = Export(new byte[length], Enumerable.Repeat(1, length).ToArray(), closed: true, inputs: inputs);
        if (change == "wrong-output") Set(bytes, 29, 8);
        if (change == "wrong-index") Set(bytes, 33, 5);
        Assert.False(BeltUpgradePathPolicy.TryReadExport(bytes, 7, length, out _, out _));
    }

    [Theory]
    [InlineData(10, 0, 10)]
    [InlineData(20, 10, 20)]
    [InlineData(30, 20, 40)]
    public void UpgradeChangesOnlyNativeAffectedRangeIncludingLastMemberTail(int targetBelt, int start, int end)
    {
        var before = State(targetBelt: targetBelt);
        var after = State(targetBelt: targetBelt);
        SetUpgrade(after, start, end);
        Assert.True(BeltUpgradePathPolicy.ProvesUpgrade(before, after, 2001, 2002, 2, out var reason));
        Assert.Null(reason);
        after.Path.Speeds[end == 40 ? 19 : end] = 2;
        Assert.False(BeltUpgradePathPolicy.ProvesUpgrade(before, after, 2001, 2002, 2, out _));
    }

    [Fact]
    public void ClosedHeadUsesNativeRearSpeedNotNewHeadSpeed()
    {
        var before = State(closed: true, targetBelt: 10);
        var after = State(closed: true, targetBelt: 10);
        for (var cell = 31; cell < 35; cell++) before.Path.Speeds[cell] = after.Path.Speeds[cell] = 2;
        SetUpgrade(after, 0, 10);
        for (var cell = 31; cell < 40; cell++) after.Path.Speeds[cell] = 1;
        Assert.True(BeltUpgradePathPolicy.ProvesUpgrade(before, after, 2001, 2002, 2, out _));
        for (var cell = 31; cell < 40; cell++) after.Path.Speeds[cell] = 2;
        Assert.False(BeltUpgradePathPolicy.ProvesUpgrade(before, after, 2001, 2002, 2, out _));
    }

    [Theory]
    [InlineData("cargo-count")]
    [InlineData("cargo-inc")]
    [InlineData("cargo-item")]
    [InlineData("cargo-pose")]
    [InlineData("cargo-missing")]
    [InlineData("cargo-duplicate")]
    [InlineData("cargo-motion")]
    [InlineData("geometry")]
    [InlineData("neighbor-speed")]
    [InlineData("neighbor-item")]
    [InlineData("neighbor-identity")]
    [InlineData("neighbor-pose")]
    [InlineData("route")]
    [InlineData("connection")]
    [InlineData("adjacent-path")]
    [InlineData("target-proto")]
    [InlineData("target-speed")]
    [InlineData("pivot")]
    [InlineData("session")]
    [InlineData("planet")]
    public void WholePathReadbackRejectsEveryUnexplainedChange(string change)
    {
        var before = State();
        var after = State();
        SetUpgrade(after, 10, 20);
        switch (change)
        {
            case "cargo-count": after.Cargo[0].StackCount++; break;
            case "cargo-inc": after.Cargo[0].Inc++; break;
            case "cargo-item": after.Cargo[0].ItemId++; break;
            case "cargo-pose": after.Cargo[0].PoseHash = "different"; break;
            case "cargo-missing": after.Cargo.RemoveAt(0); break;
            case "cargo-duplicate": after.Cargo[1].CargoId = after.Cargo[0].CargoId; break;
            case "cargo-motion": Array.Clear(after.Path.CargoBuffer, 5, 10); PutCargo(after.Path.CargoBuffer, 6, 0); break;
            case "geometry": after.Path.Geometry[0] = 1; break;
            case "neighbor-speed": after.Members[0].Speed = 2; break;
            case "neighbor-item": after.Members[0].Entity.ItemId = 2002; break;
            case "neighbor-identity": after.Members[0].Entity.ObjectId = 101; break;
            case "neighbor-pose": after.Members[0].Entity.Position.X++; break;
            case "route": after.Members[0].OutputBeltId = 30; break;
            case "connection": after.Members[0].Entity.Connections[0].OtherObjectId = 300; break;
            case "adjacent-path": after.AdjacentRoutingHash = "changed"; break;
            case "target-proto": after.Members[1].Entity.ItemId = 2003; break;
            case "target-speed": after.Members[1].Speed = 5; break;
            case "pivot": after.Members[1].PivotOffset++; break;
            case "session": after.Members.ForEach(member => member.Entity.SessionId = "new-session"); break;
            case "planet": after.Members.ForEach(member => member.Entity.PlanetId = 102); break;
        }
        Assert.False(BeltUpgradePathPolicy.ProvesUpgrade(before, after, 2001, 2002, 2, out var reason));
        Assert.NotNull(reason);
    }

    [Fact]
    public void PrepareBindingIgnoresMovingCargoButBindsExactTargetAndWholeStaticSite()
    {
        var state = State();
        Assert.True(BeltUpgradePathPolicy.TryBindingHash(state, out var binding, out _));
        Array.Clear(state.Path.CargoBuffer, 0, state.Path.Length);
        state.Cargo.Clear();
        Assert.True(BeltUpgradePathPolicy.TryBindingHash(state, out var moving, out _));
        Assert.Equal(binding, moving);
        state.Path.Speeds[0]++;
        Assert.True(BeltUpgradePathPolicy.TryBindingHash(state, out var speedChange, out _));
        Assert.NotEqual(binding, speedChange);
        state.Path.Speeds[0]--;
        state.Members[1].Entity.ObjectId = state.TargetEntityId = 201;
        Assert.True(BeltUpgradePathPolicy.TryBindingHash(state, out var identityChange, out _));
        Assert.NotEqual(binding, identityChange);
    }

    [Fact]
    public void ResultIdentityMayChangeOnlyWhenAllNormalizedConnectionsAgree()
    {
        var before = State();
        var after = State();
        SetUpgrade(after, 10, 20);
        after.TargetBeltId = after.Members[1].BeltId = after.Path.BeltIds[1] = 21;
        after.TargetEntityId = after.Members[1].Entity.ObjectId = 201;
        after.Members[0].OutputBeltId = after.Members[2].BackInputBeltId = 21;
        after.Members[0].Entity.Connections[0].OtherObjectId = 201;
        after.Members[2].Entity.Connections[0].OtherObjectId = 201;
        Assert.True(BeltUpgradePathPolicy.ProvesUpgrade(before, after, 2001, 2002, 2, out _));
        after.Members[2].BackInputBeltId = 20;
        Assert.False(BeltUpgradePathPolicy.ProvesUpgrade(before, after, 2001, 2002, 2, out _));
    }

    [Theory]
    [InlineData("missing-state")]
    [InlineData("no-members")]
    [InlineData("bad-member-id")]
    [InlineData("duplicate-member-id")]
    [InlineData("bad-entity-kind")]
    [InlineData("bad-segment")]
    [InlineData("infinite-pose")]
    [InlineData("duplicate-slot")]
    [InlineData("mixed-session")]
    public void BindingRejectsMalformedOrIncompleteEvidence(string change)
    {
        BeltUpgradePathState? state = State();
        switch (change)
        {
            case "missing-state": state = null; break;
            case "no-members": state.Members.Clear(); break;
            case "bad-member-id": state.Path.BeltIds[1] = 0; break;
            case "duplicate-member-id": state.Path.BeltIds[1] = 10; break;
            case "bad-entity-kind": state.Members[0].Entity.ObjectKind = FactoryObjectKinds.Prebuild; break;
            case "bad-segment": state.Members[0].SegmentLength = int.MaxValue; break;
            case "infinite-pose": state.Members[0].Entity.Position.X = float.PositiveInfinity; break;
            case "duplicate-slot": state.Members[0].Entity.Connections.Add(state.Members[0].Entity.Connections[0]); break;
            case "mixed-session": state.Members[0].Entity.SessionId = "other"; break;
        }
        Assert.False(BeltUpgradePathPolicy.TryBindingHash(state, out var hash, out var reason));
        Assert.Empty(hash);
        Assert.NotNull(reason);
    }

    private static void SetUpgrade(BeltUpgradePathState state, int start, int end)
    {
        var target = state.Members.Single(member => member.BeltId == state.TargetBeltId);
        target.Entity.ItemId = 2002;
        target.Speed = 2;
        for (var cell = start; cell < end; cell++) state.Path.Speeds[cell] = 2;
    }

    private static BeltUpgradePathState State(bool closed = false, int targetBelt = 20)
    {
        var buffer = new byte[40];
        PutCargo(buffer, 5, 0);
        PutCargo(buffer, 25, 1);
        var state = new BeltUpgradePathState
        {
            Path = Read(Export(buffer, Enumerable.Repeat(1, 40).ToArray(), closed: closed), 40),
            TargetBeltId = targetBelt, TargetEntityId = targetBelt * 10, AdjacentRoutingHash = "adjacent-routing",
            Cargo = new List<BeltUpgradeCargo>
            {
                new() { CargoId = 0, ItemId = 1001, StackCount = 3, Inc = 6, PoseHash = "pose-a" },
                new() { CargoId = 1, ItemId = 1001, StackCount = 2, Inc = 1, PoseHash = "pose-b" },
            },
        };
        for (var index = 0; index < 3; index++)
        {
            var member = new BeltUpgradeMember
            {
                BeltId = (index + 1) * 10, Speed = 1, SegmentStart = index * 10, SegmentLength = 10, PivotOffset = 4,
                BackInputBeltId = index == 0 ? 0 : index * 10, OutputBeltId = index == 2 ? 0 : (index + 2) * 10,
                Entity = new FactoryEntitySnapshot
                {
                    SessionId = "owned-session", PlanetId = 104, ObjectId = (index + 1) * 100,
                    ObjectKind = FactoryObjectKinds.Entity, ComponentKind = "belt", ItemId = 2001,
                },
            };
            member.Entity.Position.X = index * 2;
            if (index < 2) member.Entity.Connections.Add(new FactoryConnectionSnapshot
            { Slot = 0, IsOutput = true, OtherObjectId = (index + 2) * 100, OtherSlot = 1 });
            if (index > 0) member.Entity.Connections.Add(new FactoryConnectionSnapshot
            { Slot = 1, IsOutput = false, OtherObjectId = index * 100, OtherSlot = 0 });
            state.Members.Add(member);
        }
        return state;
    }

    private static BeltPathExportSnapshot Read(byte[] bytes, int length)
    {
        Assert.True(BeltUpgradePathPolicy.TryReadExport(bytes, 7, length, out var result, out var reason), reason);
        return result!;
    }

    // A synthetic fixture for the documented current native binary layout; never
    // passed to DSP Import or represented as live-game evidence.
    private static byte[] Export(byte[] cargo, int[] speeds, bool closed = false, int[]? inputs = null)
    {
        inputs ??= closed ? new[] { 7 } : Array.Empty<int>();
        var chunks = new List<(int Start, int Length, int Speed)>();
        for (var start = 0; start < speeds.Length;)
        {
            var end = start + 1;
            while (end < speeds.Length && speeds[end] == speeds[start]) end++;
            chunks.Add((start, end - start, speeds[start]));
            start = end;
        }
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(1); writer.Write(7); writer.Write(cargo.Length); writer.Write(cargo.Length);
        writer.Write(chunks.Count); writer.Write(chunks.Count); writer.Write(cargo.Length);
        writer.Write(closed); writer.Write(closed ? 7 : 0); writer.Write(closed ? 4 : -1);
        writer.Write(3); writer.Write(inputs.Length); writer.Write(cargo);
        foreach (var chunk in chunks) { writer.Write(chunk.Start); writer.Write(chunk.Length); writer.Write(chunk.Speed); }
        for (var i = 0; i < cargo.Length * 7; i++) writer.Write(0f);
        foreach (var belt in new[] { 10, 20, 30 }) writer.Write(belt);
        foreach (var input in inputs) writer.Write(input);
        return stream.ToArray();
    }

    private static void Set(byte[] bytes, int offset, int value) => BitConverter.GetBytes(value).CopyTo(bytes, offset);

    private static void PutCargo(byte[] bytes, int start, int id)
    {
        for (var offset = 0; offset < 5; offset++) bytes[start + offset] = (byte)(246 + offset);
        for (var offset = 5; offset < 9; offset++) { bytes[start + offset] = (byte)(1 + id % 100); id /= 100; }
        bytes[start + 9] = 255;
    }
}
