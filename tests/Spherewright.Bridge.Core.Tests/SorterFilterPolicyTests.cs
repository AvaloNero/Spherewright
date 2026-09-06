using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class SorterFilterPolicyTests
{
    private static bool Check(int proto = 2011, bool bidirectional = false, string stage = "Inserting",
        int filter = 1000, int pick = 10, int insert = 20, int item = 1120,
        int count = 1, int stacks = 1, int inc = 0) =>
        SorterFilterPolicy.IsSafePreservingAssignmentWindow(proto, bidirectional, stage,
            filter, pick, insert, item, count, stacks, inc);

    [Theory]
    [InlineData(2011, 0)]
    [InlineData(2011, 1000)]
    [InlineData(2012, 1120)]
    public void NativeFilterMayChangeWhileCargoRemainsForOriginalTarget(int proto, int filter) =>
        Assert.True(Check(proto: proto, filter: filter));

    [Theory]
    [InlineData("Picking")]
    [InlineData("Returning")]
    [InlineData("")]
    [InlineData("inserting")]
    public void CarriedCargoRequiresVerifiedNativeStage(string stage) => Assert.False(Check(stage: stage));

    [Fact]
    public void CargoFreeLegacyWindowRemainsUsable() =>
        Assert.True(Check(proto: 2013, stage: "Returning", item: 0, count: 0, stacks: 0));

    [Fact]
    public void CargoFreePolicyItselfHasNotBeenRelaxed() =>
        Assert.False(SorterFilterPolicy.IsSafeAssignmentWindow(1000, 10, 20, 1120, 1, 1, 0));

    [Fact]
    public void EndpointProofMustAllowOnlyTheDeclaredFilterChange()
    {
        var snapshot = new FactoryEntitySnapshot { ComponentKind = "inserter", ObjectId = 1, FilterItemId = 1115 };
        snapshot.Connections.Add(new FactoryConnectionSnapshot { Slot = 0, IsOutput = true, OtherObjectId = 20, OtherSlot = 3 });
        var old = CanonicalStateHash.FactoryEndpoint(snapshot);
        snapshot.FilterItemId = 1000;
        var expected = CanonicalStateHash.FactoryEndpoint(snapshot);
        Assert.NotEqual(old, expected);
        snapshot.Buffers.Add(new FactoryBufferSnapshot { ItemId = 1120, Count = 1 });
        Assert.Equal(expected, CanonicalStateHash.FactoryEndpoint(snapshot));
        snapshot.Connections[0].OtherObjectId = 21;
        Assert.NotEqual(expected, CanonicalStateHash.FactoryEndpoint(snapshot));
    }

    [Theory]
    [InlineData(2013)]
    [InlineData(2014)]
    [InlineData(0)]
    public void HeldCargoSubsetDoesNotEnableOtherDevices(int proto) => Assert.False(Check(proto: proto));

    [Fact]
    public void BidirectionalAndNonBuiltTargetsAreRejected()
    {
        Assert.False(Check(bidirectional: true));
        Assert.False(Check(pick: 0));
        Assert.False(Check(insert: 0));
        Assert.False(Check(pick: -1));
        Assert.False(Check(insert: -1));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(32768)]
    public void NonNativeFiltersAreRejected(int filter) => Assert.False(Check(filter: filter));

    [Fact]
    public void InvalidOrInconsistentCargoIsRejected()
    {
        Assert.False(Check(item: 0));
        Assert.False(Check(item: 32768));
        Assert.False(Check(count: 0));
        Assert.False(Check(count: 32768));
        Assert.False(Check(stacks: 0));
        Assert.False(Check(stacks: 256, count: 256));
        Assert.False(Check(stacks: 2, count: 1));
        Assert.False(Check(inc: -1));
        Assert.False(Check(inc: 32768));
        Assert.False(Check(item: 0, count: 0, stacks: 0, inc: 1));
        Assert.True(Check(count: 4, stacks: 1, inc: 16));
    }
}
