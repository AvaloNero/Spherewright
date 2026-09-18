using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class BeltDestinationReusePolicyTests
{
    [Theory]
    [InlineData(754, 2001, 2001, 1, 0f, true, 0, 0, 0, 0, true)]
    [InlineData(754, 2001, 2001, 1, 0.00001f, true, 0, 0, 0, 0, true)]
    [InlineData(0, 2001, 2001, 1, 0f, true, 0, 0, 0, 0, false)]
    [InlineData(754, 2002, 2002, 1, 0f, true, 0, 0, 0, 0, false)]
    [InlineData(754, 2001, 2002, 1, 0f, true, 0, 0, 0, 0, false)]
    [InlineData(754, 2001, 2001, 0, 0f, true, 0, 0, 0, 0, false)]
    [InlineData(754, 2001, 2001, 1, 0.01f, true, 0, 0, 0, 0, false)]
    [InlineData(754, 2001, 2001, 1, 0f, false, 0, 0, 0, 0, false)]
    [InlineData(754, 2001, 2001, 1, 0f, true, 1, 0, 0, 0, false)]
    [InlineData(754, 2001, 2001, 1, 0f, true, 0, 1, 0, 0, false)]
    [InlineData(754, 2001, 2001, 1, 0f, true, 0, 0, 1, 0, false)]
    [InlineData(754, 2001, 2001, 1, 0f, true, 0, 0, 0, 1, false)]
    public void SupportsOnlyEmptyFlatOpen2001InputHeads(int id, int oldItem, int requestedItem, int slot,
        float tilt, bool openHead, int occupiedInputs, int cargo, int externalInputs, int outputPath, bool expected) =>
        Assert.Equal(expected, BeltDestinationReusePolicy.Supports(id, oldItem, requestedItem, slot, tilt, openHead,
            occupiedInputs, cargo, externalInputs, outputPath));

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(-0.01f)]
    public void TiltMustBeFiniteAndNearFlat(float tilt) => Assert.False(BeltDestinationReusePolicy.Supports(
        754, 2001, 2001, 1, tilt, true, 0, 0, 0, 0));

    [Fact]
    public void PartitionRemovesBoundCoversAndDeepCopiesEveryNewPoint()
    {
        var path = new[] { P(0), P(1), P(2), P(3) };
        Assert.True(BeltDestinationReusePolicy.TrySeparateNewPoints(path, P(0), P(3), 256, out var newPoints, out var reason));
        Assert.Equal(string.Empty, reason);
        Assert.Equal(new[] { 1f, 2f }, newPoints.Select(point => point.X));
        path[1].X = 99;
        Assert.Equal(1f, newPoints[0].X);
    }

    [Fact]
    public void PartitionAllowsFreeSourceButNeverCountsBoundDestinationAsNew()
    {
        var path = new[] { P(1), P(2), P(3) };
        Assert.True(BeltDestinationReusePolicy.TrySeparateNewPoints(path, null, P(3), 256, out var newPoints, out _));
        Assert.Equal(new[] { 1f, 2f }, newPoints.Select(point => point.X));
    }

    [Theory]
    [InlineData("saturated")]
    [InlineData("bounds")]
    [InlineData("reserved_capacity")]
    [InlineData("source")]
    [InlineData("destination")]
    [InlineData("too_few")]
    [InlineData("raised")]
    [InlineData("invalid")]
    public void PartitionRejectsSaturatedInvalidOrIncompleteCovers(string fault)
    {
        var path = new[] { P(0), P(1), P(2), P(3) };
        Vector3Snapshot? source = P(0);
        var destination = P(3);
        var capacity = 256;
        switch (fault)
        {
            case "saturated": capacity = BeltPathRoutingPolicy.NativeReservedPoints + path.Length; break;
            case "bounds": capacity = BeltBuildOccupancyPolicy.MaximumPathPoints + 1; break;
            case "reserved_capacity": capacity = BeltPathRoutingPolicy.NativeReservedPoints + 2; break;
            case "source": source = P(9); break;
            case "destination": destination = P(9); break;
            case "too_few": path = new[] { P(0), P(1), P(2) }; destination = P(2); break;
            case "raised": path[1].Y += 1; break;
            case "invalid": path[1].X = float.NaN; break;
        }
        Assert.False(BeltDestinationReusePolicy.TrySeparateNewPoints(path, source, destination, capacity,
            out var newPoints, out var reason));
        Assert.Empty(newPoints);
        Assert.NotEmpty(reason);
    }

    [Theory]
    [InlineData(246)]
    [InlineData(256)]
    public void PartitionReservesTenNativeTailSlotsBeforeTreatingThePathAsComplete(int count)
    {
        var path = Enumerable.Range(0, count).Select(index => Ground(10f * index / (count - 1))).ToArray();
        Assert.False(BeltDestinationReusePolicy.TrySeparateNewPoints(path, Ground(0), Ground(10), 256,
            out var newPoints, out var reason));
        Assert.Empty(newPoints);
        Assert.Equal("destination_reuse_path_saturated", reason);
    }

    [Theory]
    [InlineData("same_centre")]
    [InlineData("source_repeated")]
    [InlineData("destination_repeated")]
    public void CoversCannotCoincideOrReappearAsNewObjects(string fault)
    {
        var path = new[] { P(0), P(1), P(2), P(3) };
        var source = P(0);
        var destination = P(3);
        switch (fault)
        {
            case "same_centre": destination = P(0); path[^1] = P(0); break;
            case "source_repeated": path[1] = P(0); break;
            case "destination_repeated": path[2] = P(3); break;
        }
        Assert.False(BeltDestinationReusePolicy.TrySeparateNewPoints(path, source, destination, 256,
            out var newPoints, out var reason));
        Assert.Empty(newPoints);
        Assert.NotEmpty(reason);
    }

    [Fact]
    public void OnlyTheEmptyInputSlotMayChangeToTheLastNewBelt()
    {
        var before = Connections();
        var after = Connections();
        after[1] = new FactoryConnectionSnapshot { Slot = 1, OtherObjectId = 100, OtherSlot = 0 };
        Assert.True(BeltDestinationReusePolicy.ProvesOnlyInputChanged(before, after, 100));

        after[0].OtherObjectId = 99;
        Assert.False(BeltDestinationReusePolicy.ProvesOnlyInputChanged(before, after, 100));
    }

    [Theory]
    [InlineData("output")]
    [InlineData("wrong_other_slot")]
    [InlineData("not_empty_before")]
    [InlineData("duplicate")]
    [InlineData("invalid_id")]
    [InlineData("invalid_slot")]
    [InlineData("short")]
    public void InputReadbackRejectsDirectionSlotDuplicateOrInvalidEvidence(string fault)
    {
        var before = Connections();
        var after = Connections();
        after[1] = new FactoryConnectionSnapshot { Slot = 1, OtherObjectId = 100, OtherSlot = 0 };
        var id = 100;
        switch (fault)
        {
            case "output": after[1].IsOutput = true; break;
            case "wrong_other_slot": after[1].OtherSlot = 1; break;
            case "not_empty_before": before[1].OtherObjectId = 99; break;
            case "duplicate": before[3].OtherObjectId = 100; after[3].OtherObjectId = 100; break;
            case "invalid_id": id = 0; break;
            case "invalid_slot": after[5].Slot = 4; break;
            case "short": before.RemoveAt(15); break;
        }
        Assert.False(BeltDestinationReusePolicy.ProvesOnlyInputChanged(before, after, id));
    }

    [Fact]
    public void JoinedMembershipAllowsFreeAndDualCoverSequencesOnlyInExactOrder()
    {
        Assert.True(BeltDestinationReusePolicy.ProvesJoinedMembership(null, new[] { 2, 3 }, new[] { 4 },
            new[] { 2, 3, 4 }));
        Assert.True(BeltDestinationReusePolicy.ProvesJoinedMembership(new[] { 10, 11 }, new[] { 12, 13 }, new[] { 14 },
            new[] { 10, 11, 12, 13, 14 }));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("reordered")]
    [InlineData("extra")]
    [InlineData("overbound")]
    [InlineData("invalid")]
    [InlineData("nullactual")]
    public void JoinedMembershipRejectsMissingDuplicateReorderedOrUnboundedEvidence(string fault)
    {
        IReadOnlyList<int>? source = new[] { 10 };
        IReadOnlyList<int>? newIds = new[] { 11, 12 };
        IReadOnlyList<int>? destination = new[] { 13 };
        IReadOnlyList<int>? actual = new[] { 10, 11, 12, 13 };
        switch (fault)
        {
            case "missing": destination = Array.Empty<int>(); break;
            case "duplicate": newIds = new[] { 11, 11 }; actual = new[] { 10, 11, 11, 13 }; break;
            case "reordered": actual = new[] { 10, 12, 11, 13 }; break;
            case "extra": actual = new[] { 10, 11, 12, 13, 14 }; break;
            case "overbound":
                source = Enumerable.Range(1, BeltUpgradePathPolicy.MaximumBelts - 1).ToArray();
                newIds = new[] { BeltUpgradePathPolicy.MaximumBelts, BeltUpgradePathPolicy.MaximumBelts + 1 };
                destination = new[] { BeltUpgradePathPolicy.MaximumBelts + 2 };
                actual = source.Concat(newIds).Concat(destination).ToArray();
                break;
            case "invalid": newIds = new[] { 0, 12 }; actual = new[] { 10, 0, 12, 13 }; break;
            case "nullactual": actual = null; break;
        }
        Assert.False(BeltDestinationReusePolicy.ProvesJoinedMembership(source, newIds, destination, actual));
    }

    private static Vector3Snapshot P(float x) => new() { X = x, Y = 200 };
    private static Vector3Snapshot Ground(float x) => new() { X = x, Y = (float)Math.Sqrt(200.2 * 200.2 - x * x) };
    private static List<FactoryConnectionSnapshot> Connections() => Enumerable.Range(0, 16)
        .Select(slot => new FactoryConnectionSnapshot { Slot = slot }).ToList();
}
