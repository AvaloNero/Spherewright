using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class EmptyStorageDismantlePolicyTests
{
    [Fact]
    public void OnlyExplicitEmptyDefaultSingleWarehouseIsSupported()
    {
        Assert.True(EmptyStorageDismantlePolicy.SupportsSnapshot(Target()));
        Assert.True(EmptyStorageDismantlePolicy.EmptyDefaultContents(Contents()));
        Assert.False(EmptyStorageDismantlePolicy.SupportsSnapshot(null));
        Assert.False(EmptyStorageDismantlePolicy.EmptyDefaultContents(null));
    }

    [Theory]
    [InlineData("larger_storage")] [InlineData("tank")] [InlineData("sorter")]
    [InlineData("prebuild")] [InlineData("zero_id")] [InlineData("wrong_component")]
    [InlineData("recipe")] [InlineData("connection")] [InlineData("buffer")]
    [InlineData("missing_buffers")] [InlineData("missing_connections")] [InlineData("missing_configuration")]
    [InlineData("bans")] [InlineData("filtered_mode")] [InlineData("reserved_grid")]
    [InlineData("missing_filters")] [InlineData("short_filters")] [InlineData("wrong_size")]
    public void UnsafeOrIncompleteSnapshotCannotBeImplicitlyClaimed(string change)
    {
        var target = Target();
        switch (change)
        {
            case "larger_storage": target.ItemId = 2102; break;
            case "tank": target.ItemId = 2106; break;
            case "sorter": target.ItemId = 2012; break;
            case "prebuild": target.ObjectKind = FactoryObjectKinds.Prebuild; break;
            case "zero_id": target.ObjectId = 0; break;
            case "wrong_component": target.ComponentKind = "miner"; break;
            case "recipe": target.RecipeId = 40; break;
            case "connection": target.Connections.Add(new() { Slot = 13, OtherObjectId = 8 }); break;
            case "buffer": target.Buffers.Add(new() { ItemId = 1120, Count = 1 }); break;
            case "missing_buffers": target.Buffers = null!; break;
            case "missing_connections": target.Connections = null!; break;
            case "missing_configuration": target.StorageConfiguration = null; break;
            case "bans": target.StorageConfiguration!.BannedGridCount = 1; break;
            case "filtered_mode": target.StorageConfiguration!.Mode = "filtered"; break;
            case "reserved_grid": target.StorageConfiguration!.GridFilterItemIds[0] = 1120; break;
            case "missing_filters": target.StorageConfiguration!.GridFilterItemIds = null!; break;
            case "short_filters": target.StorageConfiguration!.GridFilterItemIds.RemoveAt(0); break;
            case "wrong_size": target.StorageConfiguration!.GridCount = 60; break;
        }
        Assert.False(EmptyStorageDismantlePolicy.SupportsSnapshot(target));
    }

    [Theory]
    [InlineData(1120, 1, 0, 0, 20)] [InlineData(1120, 0, 0, 1120, 20)]
    [InlineData(0, 0, 1, 0, 0)] [InlineData(0, -1, 0, 0, 0)]
    [InlineData(0, 0, 0, 0, -1)] [InlineData(0, 0, 0, 0, 20)]
    public void NativeGridMustBeActuallyEmptyNotJustAZeroAggregate(int item, int count, int inc, int filter, int stack)
    {
        var grids = Contents().Grids.ToArray();
        grids[29] = new(item, count, inc, filter, stack);
        Assert.False(EmptyStorageDismantlePolicy.EmptyDefaultContents(new("default", 0, grids)));
    }

    [Theory]
    [InlineData(0, 0, 5, 5, false, false, true, true, true)]
    [InlineData(1, 0, 5, 5, true, false, true, true, false)]
    [InlineData(0, 6, 5, 6, false, true, true, false, false)]
    [InlineData(0, 0, 0, 0, false, false, false, false, false)]
    [InlineData(0, 0, 5, 5, true, false, true, true, false)]
    [InlineData(0, 0, 5, 5, false, true, true, true, false)]
    [InlineData(0, 0, 5, 5, false, false, false, true, false)]
    [InlineData(0, 0, 5, 6, false, false, true, true, false)]
    public void NativeLayerIdsAndObjectReferencesMustBothProveStandalone(int previous, int next, int bottom, int top,
        bool previousRef, bool nextRef, bool bottomSelf, bool topSelf, bool allowed) =>
        Assert.Equal(allowed, EmptyStorageDismantlePolicy.SingleLayer(5, previous, next, bottom, top,
            previousRef, nextRef, bottomSelf, topSelf));

    [Theory]
    [InlineData(10, 10, 0, true)] [InlineData(10, 10, 4, false)]
    [InlineData(10, 4, 10, false)] [InlineData(10, -4, 10, false)]
    [InlineData(10, 10, -4, false)] [InlineData(10, 4, 5, true)]
    [InlineData(10, -4, 0, true)] [InlineData(0, 4, 0, false)] [InlineData(10, 0, 0, false)]
    public void OneSidedAndPrebuildReferencesCannotBeIgnored(int target, int owner, int other, bool allowed) =>
        Assert.Equal(allowed, EmptyStorageDismantlePolicy.ReferenceIsSafe(target, owner, other));

    [Theory]
    [InlineData(1, 1, true)] [InlineData(8192, 8192, true)] [InlineData(0, 1, false)]
    [InlineData(-1, 1, false)] [InlineData(8193, 9000, false)] [InlineData(10, 9, false)]
    public void NativeScansMustBeCompleteAndBounded(int cursor, int length, bool allowed) =>
        Assert.Equal(allowed, EmptyStorageDismantlePolicy.BoundedPool(cursor, length));

    [Fact]
    public void NativeIdentityAndConfigurationAreBoundIntoThePlan()
    {
        var target = Target();
        var before = EmptyStorageDismantlePolicy.Fingerprint(target, Contents(), "component-A");
        Assert.NotEqual(before, EmptyStorageDismantlePolicy.Fingerprint(target, Contents(), "component-B"));
        target.Position.X = 1;
        Assert.NotEqual(before, EmptyStorageDismantlePolicy.Fingerprint(target, Contents(), "component-A"));
        target = Target();
        Assert.NotEqual(before, EmptyStorageDismantlePolicy.Fingerprint(target, new("default", 1, Contents().Grids), "component-A"));
        Assert.NotEqual(before, EmptyStorageDismantlePolicy.Fingerprint(target, new("filtered", 0, Contents().Grids), "component-A"));
    }

    private static FactoryEntitySnapshot Target() => new()
    {
        ObjectId = 10, ObjectKind = FactoryObjectKinds.Entity, ItemId = 2101, ComponentKind = "storage",
        StorageConfiguration = new() { GridCount = 30, Mode = "default", GridFilterItemIds = Enumerable.Repeat(0, 30).ToList() },
    };

    private static StorageUiState Contents() => new("default", 0,
        Enumerable.Range(0, 30).Select(_ => new StorageUiGrid(0, 0, 0, 0, 0)));
}
