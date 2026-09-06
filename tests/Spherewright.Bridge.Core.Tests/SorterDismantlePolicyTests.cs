using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class SorterDismantlePolicyTests
{
    [Theory]
    [InlineData(2011, 1, 0, 0, 0, 0, true)]
    [InlineData(2012, 2, 1006, 1, 4, 1, true)]
    [InlineData(2011, 1, 1006, 1, 0, 0, false)]
    [InlineData(2011, 1, 0, 1, 0, 1, false)]
    [InlineData(2011, 1, 1006, 2, 0, 1, false)]
    [InlineData(2011, 1, 1006, 0, 4, 0, false)]
    [InlineData(2011, 1, 1006, 1, -1, 1, false)]
    [InlineData(2013, 3, 1006, 1, 0, 1, false)]
    [InlineData(2011, 2, 1006, 1, 0, 1, false)]
    public void RecoveryMatchesNativePositiveStackPredicate(int item, int grade, int heldId, int count, int inc, int stack, bool allowed) =>
        Assert.Equal(allowed, SorterDismantlePolicy.NativeCargoRecoverable(item, grade, false, false, 1, 1, heldId, count, inc, stack));

    [Theory]
    [InlineData(true, false, 1, 1)] [InlineData(false, true, 1, 1)]
    [InlineData(false, false, 2, 1)] [InlineData(false, false, 1, 2)]
    public void AdvancedModesCannotInheritBasicSorterRecovery(bool stacking, bool bidirectional, int input, int output) =>
        Assert.False(SorterDismantlePolicy.NativeCargoRecoverable(2011, 1, stacking, bidirectional, input, output, 0, 0, 0, 0));

    [Fact]
    public void MissingOutputMayBeRemovedIfItsPresentInputIsReciprocal()
    {
        var (target, neighbor) = Pair();
        Assert.True(SorterDismantlePolicy.PresentConnectionsAreReciprocal(target, new[] { neighbor }));
        Assert.True(SorterDismantlePolicy.PresentConnectionsAreReciprocal(new() { ObjectId = 115 }, Array.Empty<FactoryEntitySnapshot>()));
    }

    [Theory]
    [InlineData("wrong_id")] [InlineData("wrong_slot")] [InlineData("direction")]
    [InlineData("neighbor_only")] [InlineData("duplicate_slot")]
    public void NativeUnconditionalClearMustNotEraseAnUnrelatedSlot(string change)
    {
        var (target, neighbor) = Pair();
        switch (change)
        {
            case "wrong_id": neighbor.Connections[0].OtherObjectId = 999; break;
            case "wrong_slot": neighbor.Connections[0].OtherSlot = 5; break;
            case "direction": neighbor.Connections[0].IsOutput = false; break;
            case "neighbor_only": target.Connections.Clear(); break;
            case "duplicate_slot": target.Connections.Add(target.Connections[0]); break;
        }
        Assert.False(SorterDismantlePolicy.PresentConnectionsAreReciprocal(target, new[] { neighbor }));
    }

    [Fact]
    public void ProliferationPointsMustReturnWithoutAnyOtherIncrement()
    {
        var before = new Dictionary<int, int> { [1006] = 2, [1001] = 5 };
        var after = new Dictionary<int, int> { [1006] = 6, [1001] = 5 };
        Assert.True(SorterDismantlePolicy.ProvesIncRecovery(before, after, 1006, 4));
        after[1001]++; Assert.False(SorterDismantlePolicy.ProvesIncRecovery(before, after, 1006, 4));
        after[1001]--; after[1006]--; Assert.False(SorterDismantlePolicy.ProvesIncRecovery(before, after, 1006, 4));
    }

    [Fact]
    public void SurvivingEndpointCannotChangePoseOrUnrelatedSlots()
    {
        var expected = new FactoryEntitySnapshot { ObjectId = 108, ItemId = 2001 };
        var actual = new FactoryEntitySnapshot { ObjectId = 108, ItemId = 2001 };
        Assert.True(SorterDismantlePolicy.SurvivorMatches(expected, actual));
        actual.Connections.Add(new() { Slot = 0, OtherObjectId = 999 });
        Assert.False(SorterDismantlePolicy.SurvivorMatches(expected, actual));
        actual.Connections.Clear(); actual.Position.X = 1;
        Assert.False(SorterDismantlePolicy.SurvivorMatches(expected, actual));
    }

    private static (FactoryEntitySnapshot, FactoryEntitySnapshot) Pair() =>
        (new() { ObjectId = 115, ItemId = 2011, Connections = new() { new() { Slot = 1, OtherObjectId = 108, OtherSlot = 4, IsOutput = false } } },
         new() { ObjectId = 108, ItemId = 2001, Connections = new() { new() { Slot = 4, OtherObjectId = 115, OtherSlot = 1, IsOutput = true } } });
}
