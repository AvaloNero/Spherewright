using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class EmptyBeltDismantlePolicyTests
{
    private static readonly int[] Order = { 30, 20, 10 };

    [Fact]
    public void CompleteEmptyIsolatedChainAllowsOnlyItsNativeHead()
    {
        var cut = Cut();
        Assert.True(EmptyBeltDismantlePolicy.TryQualify(cut.Objects[0], Order, cut, out _));
        Assert.False(EmptyBeltDismantlePolicy.TryQualify(cut.Objects[1], Order, cut, out _));
        Assert.False(EmptyBeltDismantlePolicy.TryQualify(cut.Objects[2], Order, cut, out _));
        Assert.False(EmptyBeltDismantlePolicy.TryQualify(null, Order, cut, out _));
        Assert.False(EmptyBeltDismantlePolicy.TryQualify(cut.Objects[0], Order, null, out _));
        Assert.False(EmptyBeltDismantlePolicy.TryQualify(cut.Objects[0], null, cut, out _));
    }

    [Fact]
    public void LastIsolatedBeltStillNeedsCompleteNativePathEvidence()
    {
        var cut = Cut();
        cut.Objects = cut.Objects.Take(1).ToList(); cut.Objects[0].Connections.Clear();
        cut.RequestedObjectIds = new() { 30 }; cut.CargoPaths[0].BeltObjectIds = new() { 30 };
        Assert.True(EmptyBeltDismantlePolicy.TryQualify(cut.Objects[0], new[] { 30 }, cut, out _));
        cut.CargoPaths.Clear();
        Assert.False(EmptyBeltDismantlePolicy.TryQualify(cut.Objects[0], new[] { 30 }, cut, out _));
    }

    [Theory]
    [InlineData("partial")] [InlineData("unknown")] [InlineData("wrong_coverage")]
    [InlineData("missing_path")] [InlineData("two_paths")] [InlineData("closed")]
    [InlineData("path_feed")] [InlineData("path_output")] [InlineData("cargo")]
    [InlineData("cargo_stack")] [InlineData("cargo_list")] [InlineData("long_path")]
    [InlineData("duplicate_member")] [InlineData("missing_member")] [InlineData("missing_object")]
    [InlineData("wrong_session")] [InlineData("wrong_planet")] [InlineData("mixed_tick")]
    [InlineData("path_tick")] [InlineData("zero_tick")] [InlineData("wrong_grade")]
    [InlineData("prebuild")] [InlineData("buffer")] [InlineData("recipe")]
    [InlineData("external_output")] [InlineData("external_input")] [InlineData("branch")]
    [InlineData("nonreciprocal")] [InlineData("duplicate_slot")] [InlineData("reverse_direction")]
    [InlineData("wrong_object_order")] [InlineData("duplicate_request")] [InlineData("missing_buffers")]
    [InlineData("missing_connections")] [InlineData("changed_target_pose")]
    public void CargoConnectionsCoverageAndIdentityCannotBeAssumedEmpty(string change)
    {
        var cut = Cut();
        var target = Entity(30); target.Connections = cut.Objects[0].Connections.ToList();
        var path = cut.CargoPaths[0];
        switch (change)
        {
            case "partial": cut.State = "partial"; break;
            case "unknown": cut.ReasonCode = "unavailable"; break;
            case "wrong_coverage": cut.Coverage = "one_segment"; break;
            case "missing_path": cut.CargoPaths.Clear(); break;
            case "two_paths": cut.CargoPaths.Add(path); break;
            case "closed": path.PathClosed = true; break;
            case "path_feed": path.InputPathIds.Add(3); break;
            case "path_output": path.OutputPathId = 3; break;
            case "cargo": path.ItemCount = 1; break;
            case "cargo_stack": path.CargoStackCount = 1; break;
            case "cargo_list": path.Items.Add(new() { ItemId = 1005, Count = 1 }); break;
            case "long_path": path.PathLengthCells = 513; break;
            case "duplicate_member": path.BeltObjectIds[2] = 20; break;
            case "missing_member": path.BeltObjectIds.RemoveAt(2); break;
            case "missing_object": cut.Objects.RemoveAt(2); break;
            case "wrong_session": cut.Objects[2].SessionId = "other"; break;
            case "wrong_planet": cut.Objects[2].PlanetId = 105; break;
            case "mixed_tick": cut.Objects[2].CapturedAtGameTick++; break;
            case "path_tick": path.CapturedAtGameTick++; break;
            case "zero_tick": cut.CapturedAtGameTick = 0; break;
            case "wrong_grade": cut.Objects[2].ItemId = 2002; break;
            case "prebuild": cut.Objects[2].ObjectKind = FactoryObjectKinds.Prebuild; break;
            case "buffer": cut.Objects[2].Buffers.Add(new() { ItemId = 1005, Count = 1 }); break;
            case "recipe": cut.Objects[2].RecipeId = 24; break;
            case "external_output": cut.Objects[2].Connections.Add(Edge(4, true, 87, 1)); break;
            case "external_input": cut.Objects[0].Connections.Add(Edge(4, false, 86, 0)); break;
            case "branch": cut.Objects[1].Connections.Add(Edge(2, false, 5, 0)); break;
            case "nonreciprocal": cut.Objects[1].Connections.Single(e => e.Slot == 1).OtherObjectId = 10; break;
            case "duplicate_slot": cut.Objects[1].Connections[1].Slot = 0; break;
            case "reverse_direction": cut.Objects[1].Connections[0].IsOutput = false; break;
            case "wrong_object_order": cut.Objects.Reverse(); break;
            case "duplicate_request": cut.RequestedObjectIds[2] = 20; break;
            case "missing_buffers": cut.Objects[2].Buffers = null!; break;
            case "missing_connections": cut.Objects[2].Connections = null!; break;
            case "changed_target_pose": target.Position.X = 1; break;
        }
        Assert.False(EmptyBeltDismantlePolicy.TryQualify(target, Order, cut, out _));
    }

    [Fact]
    public void NativeOrderMustBeUniquePositiveAndWithinDeclaredBound()
    {
        var cut = Cut();
        foreach (var ids in new[] { Array.Empty<int>(), new[] { 30, 20, 20 }, new[] { 30, 20, -10 },
            new[] { 10, 20, 30 }, Enumerable.Range(30, 17).ToArray() })
            Assert.False(EmptyBeltDismantlePolicy.TryQualify(cut.Objects[0], ids, cut, out _));
    }

    [Theory]
    [InlineData(30, 20, true)] [InlineData(30, 0, true)] [InlineData(30, 30, false)]
    [InlineData(30, 87, false)] [InlineData(87, 30, false)] [InlineData(-87, 30, false)]
    [InlineData(87, 0, true)] [InlineData(87, 5, true)] [InlineData(0, 5, false)]
    public void OutsideOneSidedAndPrebuildReferencesReject(int owner, int other, bool allowed) =>
        Assert.Equal(allowed, EmptyBeltDismantlePolicy.ReferenceIsSafe(Order, owner, other));

    [Fact]
    public void PlanBindsAllMemberEndpointsAndThePrivateFullNativePathEvidence()
    {
        var cut = Cut();
        var before = EmptyBeltDismantlePolicy.Fingerprint(cut.Objects[0], Order, cut, "native-A");
        Assert.NotEqual(before, EmptyBeltDismantlePolicy.Fingerprint(cut.Objects[0], Order, cut, "native-B"));
        cut.Objects[2].Position.X = 1;
        Assert.NotEqual(before, EmptyBeltDismantlePolicy.Fingerprint(cut.Objects[0], Order, cut, "native-A"));
        Assert.Throws<ArgumentException>(() => EmptyBeltDismantlePolicy.Fingerprint(cut.Objects[0], Order, cut, ""));
        cut.CargoPaths[0].ItemCount = 1;
        Assert.Throws<ArgumentException>(() => EmptyBeltDismantlePolicy.Fingerprint(cut.Objects[0], Order, cut, "native-A"));
    }

    private static FactoryEntitySnapshot Entity(int id) => new()
    {
        SessionId = "s", PlanetId = 104, ObjectId = id, ObjectKind = FactoryObjectKinds.Entity,
        ItemId = 2001, ComponentKind = "belt", CapturedAtGameTick = 50,
    };

    private static FactoryConnectionSnapshot Edge(int slot, bool output, int other, int otherSlot) => new()
        { Slot = slot, IsOutput = output, OtherObjectId = other, OtherSlot = otherSlot };

    private static MaterialInventoryCutSnapshot Cut()
    {
        var objects = Order.Select(Entity).ToList();
        objects[0].Connections.Add(Edge(0, true, 20, 1));
        objects[1].Connections.Add(Edge(0, true, 10, 1));
        objects[1].Connections.Add(Edge(1, false, 30, 0));
        objects[2].Connections.Add(Edge(1, false, 20, 0));
        return new()
        {
            State = "observed", SessionId = "s", PlanetId = 104, CapturedAtGameTick = 50,
            RequestedObjectIds = Order.ToList(), Objects = objects,
            CargoPaths = new() { new() { PathId = 7, PathLengthCells = 60, CapturedAtGameTick = 50,
                BeltObjectIds = new() { 10, 20, 30 } } },
        };
    }
}
