using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class BuildingUpgradePolicyTests
{
    [Theory]
    [InlineData(2303, 2304, true, 1, 2, true, 1, 1, null)]
    [InlineData(2303, 2305, true, 1, 3, true, 1, 1, null)]
    [InlineData(2303, 2303, true, 1, 1, true, 1, 1, BridgeErrorCodes.InvalidRequest)]
    [InlineData(2304, 2303, true, 2, 1, true, 1, 1, BridgeErrorCodes.InvalidRequest)]
    [InlineData(2303, 2304, false, 1, 2, true, 1, 1, BridgeErrorCodes.InvalidRequest)]
    [InlineData(2001, 2002, true, 1, 2, true, 1, 1, BridgeErrorCodes.InvalidRequest)]
    [InlineData(2011, 2012, true, 1, 2, true, 1, 1, null)]
    [InlineData(2012, 2013, true, 2, 3, true, 1, 1, BridgeErrorCodes.InvalidRequest)]
    [InlineData(2011, 2304, true, 1, 2, true, 1, 1, BridgeErrorCodes.InvalidRequest)]
    [InlineData(2302, 2315, true, 1, 2, true, 1, 1, BridgeErrorCodes.InvalidRequest)]
    [InlineData(2303, 2304, true, 1, 2, false, 1, 1, BridgeErrorCodes.TechnologyLocked)]
    [InlineData(2303, 2304, true, 1, 2, true, 0, 1, BridgeErrorCodes.InventoryInsufficient)]
    [InlineData(2303, 2304, true, 1, 2, true, 1, 0, BridgeErrorCodes.InventoryFull)]
    public void PreparationFailsClosed(int from, int to, bool family, int fromGrade, int toGrade,
        bool unlocked, int available, int empty, string? error) =>
        Assert.Equal(error, BuildingUpgradePolicy.Validate(from, to, family, fromGrade, toGrade, unlocked, available, empty));

    [Fact]
    public void BindingAllowsLiveProductionButRejectsConfigurationChanges()
    {
        var entity = Entity(1, 2303);
        var binding = BuildingUpgradePolicy.BindingHash(entity, false);
        entity.Progress++;
        entity.Buffers[0].Count++;
        Assert.Equal(binding, BuildingUpgradePolicy.BindingHash(entity, false));
        Assert.NotEqual(binding, BuildingUpgradePolicy.BindingHash(entity, true));
        entity.RecipeId++;
        Assert.NotEqual(binding, BuildingUpgradePolicy.BindingHash(entity, false));
        entity.RecipeId--;
        entity.EndpointStateHash = "changed-connections-or-pose";
        Assert.NotEqual(binding, BuildingUpgradePolicy.BindingHash(entity, false));
    }

    [Fact]
    public void ReadbackAllowsProvenReplacementIdentityAndNativeProgressReset()
    {
        var before = Entity(1, 2303);
        before.Progress = 100;
        var after = Entity(10, 2304);
        Assert.True(BuildingUpgradePolicy.ProvesPreservation(before, after, 2304));
    }

    [Theory]
    [InlineData("recipe")]
    [InlineData("cargo")]
    [InlineData("inc")]
    [InlineData("connection")]
    [InlineData("power")]
    [InlineData("identity")]
    [InlineData("pose")]
    public void ReadbackRejectsUnprovenChanges(string change)
    {
        var before = Entity(1, 2303);
        var after = Entity(1, 2304);
        switch (change)
        {
            case "recipe": after.RecipeId++; break;
            case "cargo": after.Buffers[0].Count--; break;
            case "inc": after.Buffers[0].Inc--; break;
            case "connection": after.Connections[0].OtherObjectId++; break;
            case "power": after.PowerNetworkId++; break;
            case "identity": after.PlanetId++; break;
            case "pose": after.Position.X++; break;
        }
        Assert.False(BuildingUpgradePolicy.ProvesPreservation(before, after, 2304));
    }

    [Fact]
    public void InventoryRequiresExactDebitRefundAndNoUnrelatedDelta()
    {
        var before = new Dictionary<int, int> { [2304] = 3, [1001] = 5 };
        var after = new Dictionary<int, int> { [2304] = 2, [2303] = 1, [1001] = 5 };
        Assert.True(BuildingUpgradePolicy.ProvesInventory(before, after, 2303, 2304));
        after[2303] = 0;
        Assert.False(BuildingUpgradePolicy.ProvesInventory(before, after, 2303, 2304));
        after[2303] = 1;
        after[1001]++;
        Assert.False(BuildingUpgradePolicy.ProvesInventory(before, after, 2303, 2304));
    }

    [Theory]
    [InlineData(600000, 123456, 600000, 300000, 300000, 61728)]
    [InlineData(1200000, 333333, 600000, 300000, 600000, 166667)]
    [InlineData(1800000, 1800000, 600000, 300000, 900000, 900000)]
    public void BasicSorterTimingUsesNativeSpanAndRounding(int stt, int time, int source, int target,
        int expectedStt, int expectedTime)
    {
        Assert.True(BuildingUpgradePolicy.TryGetBasicInserterTiming(stt, time, source, target, out var actualStt, out var actualTime));
        Assert.Equal(expectedStt, actualStt);
        Assert.Equal(expectedTime, actualTime);
    }

    [Theory]
    [InlineData(0, 0, 10, 5)]
    [InlineData(10, -1, 10, 5)]
    [InlineData(10, 11, 10, 5)]
    [InlineData(11, 5, 10, 5)]
    [InlineData(40, 0, 10, 5)]
    [InlineData(30, 1, 10, int.MaxValue)]
    public void UnprovenSorterCycleIsRejected(int stt, int time, int source, int target) =>
        Assert.False(BuildingUpgradePolicy.TryGetBasicInserterTiming(stt, time, source, target, out _, out _));

    [Fact]
    public void BasicSorterPreservesFilterHeldCargoAndBothTargetIdentities()
    {
        var before = Entity(7, 2011);
        var after = Entity(17, 2012);
        foreach (var entity in new[] { before, after })
        {
            entity.ComponentKind = "inserter";
            entity.RecipeId = 0;
            entity.FilterItemId = 1101;
            entity.PickTargetObjectId = 30;
            entity.InsertTargetObjectId = 40;
            entity.InserterStage = "Sending";
            entity.InserterStackCount = 1;
        }
        Assert.True(BuildingUpgradePolicy.ProvesPreservation(before, after, 2012));
        after.FilterItemId++;
        Assert.False(BuildingUpgradePolicy.ProvesPreservation(before, after, 2012));
        Assert.NotEqual(BuildingUpgradePolicy.BindingHash(before, false), BuildingUpgradePolicy.BindingHash(after, false));
        after.FilterItemId = before.FilterItemId;
        after.PickTargetObjectId++;
        Assert.False(BuildingUpgradePolicy.ProvesPreservation(before, after, 2012));
        after.PickTargetObjectId = before.PickTargetObjectId;
        after.Buffers[0].Inc--;
        Assert.False(BuildingUpgradePolicy.ProvesPreservation(before, after, 2012));
    }

    [Fact]
    public void SorterNeedsBothNativeFactoryConnectionsNotOnlyCachedTargets()
    {
        var entity = ConnectedSorter();
        Assert.True(BuildingUpgradePolicy.HasCompleteInserterConnections(entity));
        entity.Connections.RemoveAt(0);
        Assert.False(BuildingUpgradePolicy.HasCompleteInserterConnections(entity));
        // Still-positive cached target IDs do not repair the absent input connection.
        Assert.Equal(10, entity.PickTargetObjectId);
        Assert.Equal(26, entity.InsertTargetObjectId);
    }

    [Theory]
    [InlineData("missing-output")]
    [InlineData("duplicate-input")]
    [InlineData("wrong-direction")]
    [InlineData("wrong-target")]
    [InlineData("wrong-slot")]
    [InlineData("prebuild")]
    [InlineData("self")]
    [InlineData("virtual-slot")]
    [InlineData("out-of-range-slot")]
    [InlineData("extra")]
    public void IncompleteOrAmbiguousSorterTopologyIsRejected(string change)
    {
        var entity = ConnectedSorter();
        switch (change)
        {
            case "missing-output": entity.Connections.RemoveAt(1); break;
            case "duplicate-input": entity.Connections[1] = entity.Connections[0]; break;
            case "wrong-direction": entity.Connections[0].IsOutput = true; break;
            case "wrong-target": entity.PickTargetObjectId++; break;
            case "wrong-slot": entity.Connections[0].Slot = 2; break;
            case "prebuild": entity.PickTargetObjectId = entity.Connections[0].OtherObjectId = -10; break;
            case "self": entity.PickTargetObjectId = entity.Connections[0].OtherObjectId = entity.ObjectId; break;
            case "virtual-slot": entity.Connections[0].OtherSlot = -1; break;
            case "out-of-range-slot": entity.Connections[0].OtherSlot = 16; break;
            case "extra": entity.Connections.Add(new FactoryConnectionSnapshot { Slot = 15 }); break;
        }
        Assert.False(BuildingUpgradePolicy.HasCompleteInserterConnections(entity));
    }

    private static FactoryEntitySnapshot ConnectedSorter() => new FactoryEntitySnapshot
    {
        ObjectId = 27, ComponentKind = "inserter", PickTargetObjectId = 10, InsertTargetObjectId = 26,
        Connections = new List<FactoryConnectionSnapshot>
        {
            new FactoryConnectionSnapshot { Slot = 1, IsOutput = false, OtherObjectId = 10, OtherSlot = 4 },
            new FactoryConnectionSnapshot { Slot = 0, IsOutput = true, OtherObjectId = 26, OtherSlot = 2 },
        },
    };

    private static FactoryEntitySnapshot Entity(int id, int item) => new FactoryEntitySnapshot
    {
        SessionId = "owned-session", PlanetId = 104, ObjectKind = "entity", ObjectId = id, ItemId = item,
        ComponentKind = "assembler", RecipeId = 16, EndpointStateHash = "fresh-endpoint", PowerNetworkId = 1,
        Connections = new List<FactoryConnectionSnapshot>
        { new FactoryConnectionSnapshot { Slot = 1, OtherObjectId = 3, OtherSlot = 0, IsOutput = true } },
        Buffers = new List<FactoryBufferSnapshot>
        { new FactoryBufferSnapshot { Role = "input", ItemId = 1101, Count = 3, Inc = 2 } },
    };
}
