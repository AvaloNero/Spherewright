using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Actions;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class StorageConfigurationPolicyTests
{
    private static readonly IReadOnlyDictionary<int, int> Sizes = new Dictionary<int, int>
        { [1000] = 20, [1114] = 20, [1115] = 100, [1120] = 20 };
    private static StorageUiGrid Grid(int item = 0, int count = 0, int inc = 0, int filter = 0) =>
        new(item, count, inc, filter, item == 0 ? 0 : Sizes[item]);
    private static StorageUiState State(params StorageUiGrid[] grids) => new("default", 0, grids);
    private static StorageUiState Project(StorageUiState before, string operation, int filter = 0, int bans = -1) =>
        StorageConfigurationPolicy.Project(before, operation, filter, bans, Sizes);

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void BansCountDisabledTailGridsAndNeverMoveStock(int bans)
    {
        var before = State(Grid(1114, 20, 8), Grid(1115, 50, 10), Grid());
        var after = Project(before, StorageConfigurationOperations.SetBans, bans: bans);
        Assert.Equal(bans, after.Bans); Assert.Equal(before.Mode, after.Mode);
        Assert.Equal(before.Grids, after.Grids);
        var restored = Project(after, StorageConfigurationOperations.SetBans, bans: 0);
        Assert.Equal(StorageConfigurationPolicy.Fingerprint(before), StorageConfigurationPolicy.Fingerprint(restored));
    }

    [Fact]
    public void BansPreserveEmptyNativeMetadataToo()
    {
        var before = State(Grid(1000));
        var after = Project(before, StorageConfigurationOperations.SetBans, bans: 1);
        Assert.Same(before.Grids[0], after.Grids[0]);
    }

    [Fact]
    public void LockExistingItemsKeepsEmptyGridsAndStockUnchanged()
    {
        var before = State(Grid(1114, 20, 10), Grid(1115, 17, 6), Grid());
        var after = Project(before, StorageConfigurationOperations.LockOccupied);
        Assert.Equal("filtered", after.Mode);
        Assert.Equal(new[] { 1114, 1115, 0 }, after.Grids.Select(grid => grid.Filter));
        Assert.Equal(new[] { 20, 17, 0 }, after.Grids.Select(grid => grid.Count));
        Assert.Equal(new[] { 10, 6, 0 }, after.Grids.Select(grid => grid.Inc));
    }

    [Fact]
    public void FilterEmptyOrMatchingReservesWaterWithoutReplacingOil()
    {
        var before = State(Grid(1114, 20, 8), Grid(), Grid(1000, 3, 2));
        var after = Project(before, StorageConfigurationOperations.FilterEmptyOrMatching, filter: 1000);
        Assert.Same(before.Grids[0], after.Grids[0]);
        Assert.Equal(new[] { 0, 1000, 1000 }, after.Grids.Select(grid => grid.Filter));
        Assert.Equal(new[] { 20, 0, 3 }, after.Grids.Select(grid => grid.Count));
        Assert.Equal(new[] { 8, 0, 2 }, after.Grids.Select(grid => grid.Inc));
        Assert.Equal(20, after.Grids[1].StackSize);
        Assert.Equal(1000, after.Grids[1].ItemId);
    }

    [Fact]
    public void ZeroFilterOnOccupiedGridIsNotAnEmptyReservationCandidate()
    {
        var before = new StorageUiState("filtered", 0, new[] { Grid(1114, 20, 8) });
        var hash = StorageConfigurationPolicy.Fingerprint(before);
        Assert.Equal(0, before.Grids[0].Filter);
        var error = Assert.Throws<ArgumentException>(() => Project(before,
            StorageConfigurationOperations.FilterEmptyOrMatching, filter: 1000));
        Assert.Equal("storage_configuration_unchanged", error.Message);
        Assert.Equal(hash, StorageConfigurationPolicy.Fingerprint(before));
    }

    [Fact]
    public void DeliveryIntoSpaceLeftByLockChangesFullHashAndInvalidatesTheReservationProjection()
    {
        var locked = Project(State(Grid(1114, 20), Grid()), StorageConfigurationOperations.LockOccupied);
        var previouslyPossible = Project(locked, StorageConfigurationOperations.FilterEmptyOrMatching, filter: 1000);
        // Two separate observed native states, not a simulated delivery implementation.
        var delivered = new StorageUiState("filtered", 0, new[] { locked.Grids[0], Grid(1114, 1) });
        Assert.Equal(locked.Grids.Select(grid => grid.Filter), delivered.Grids.Select(grid => grid.Filter));
        Assert.NotEqual(StorageConfigurationPolicy.Fingerprint(locked), StorageConfigurationPolicy.Fingerprint(delivered));
        Assert.Equal(1000, previouslyPossible.Grids[1].Filter);
        var error = Assert.Throws<ArgumentException>(() => Project(delivered,
            StorageConfigurationOperations.FilterEmptyOrMatching, filter: 1000));
        Assert.Equal("storage_configuration_unchanged", error.Message);
    }

    [Fact]
    public void ReservingAnActuallyEmptiedEarlierGridPreservesStockAndDeclaredBans()
    {
        // A post-transfer fixture: native transfer order/conservation need separate live proof.
        var afterTransfer = new StorageUiState("filtered", 3,
            new[] { Grid(1114, filter: 1114), Grid(1115, 50, 10, 1115), Grid(1114, 20, 8) });
        var reserved = Project(afterTransfer, StorageConfigurationOperations.FilterEmptyOrMatching, filter: 1000);
        Assert.Equal(3, reserved.Bans);
        Assert.Equal(new[] { 1000, 1115, 0 }, reserved.Grids.Select(grid => grid.Filter));
        Assert.Equal(afterTransfer.Grids.Select(grid => grid.Count), reserved.Grids.Select(grid => grid.Count));
        Assert.Equal(afterTransfer.Grids.Select(grid => grid.Inc), reserved.Grids.Select(grid => grid.Inc));
        Assert.Same(afterTransfer.Grids[1], reserved.Grids[1]);
        Assert.Same(afterTransfer.Grids[2], reserved.Grids[2]);
        var restored = Project(reserved, StorageConfigurationOperations.SetBans, bans: 0);
        Assert.Equal(0, restored.Bans);
        Assert.Equal(reserved.Grids, restored.Grids);
        Assert.Equal("filtered", restored.Mode);
    }

    [Fact]
    public void ClearFiltersPreservesOccupiedItemsAndClearsOnlyEmptyMetadata()
    {
        var before = new StorageUiState("filtered", 1, new[] { Grid(1114, 10, 4, 1114), Grid(1000, filter: 1000) });
        var after = Project(before, StorageConfigurationOperations.ClearFilters);
        Assert.Equal("default", after.Mode); Assert.Equal(1, after.Bans);
        Assert.Equal(1114, after.Grids[0].ItemId); Assert.Equal(10, after.Grids[0].Count); Assert.Equal(4, after.Grids[0].Inc);
        Assert.Equal(0, after.Grids[1].ItemId); Assert.Equal(0, after.Grids[1].StackSize);
        Assert.All(after.Grids, grid => Assert.Equal(0, grid.Filter));
    }

    [Fact]
    public void FiltersAndBansAreIndependentAndThePreviewDoesNotMutateItsInput()
    {
        var before = State(Grid(1114, 20), Grid());
        var hash = StorageConfigurationPolicy.Fingerprint(before);
        var banned = Project(before, StorageConfigurationOperations.SetBans, bans: 2);
        var filtered = Project(banned, StorageConfigurationOperations.FilterEmptyOrMatching, filter: 1000);
        Assert.Equal(2, filtered.Bans);
        Assert.Equal(hash, StorageConfigurationPolicy.Fingerprint(before));
        var snapshot = StorageConfigurationPolicy.ToSnapshot(filtered);
        snapshot.GridFilterItemIds[1] = 1120;
        Assert.Equal(1000, filtered.Grids[1].Filter);
    }

    [Fact]
    public void ExactGridOrderIsBoundEvenWhenPublicBufferTotalsWouldBeEqual()
    {
        var before = State(Grid(1114, 20), Grid(), Grid(1115, 30));
        var swapped = State(Grid(1115, 30), Grid(), Grid(1114, 20));
        Assert.NotEqual(StorageConfigurationPolicy.Fingerprint(before), StorageConfigurationPolicy.Fingerprint(swapped));
        Assert.NotEqual(StorageConfigurationPolicy.Fingerprint(before), StorageConfigurationPolicy.Fingerprint(State(Grid(1114, 20), Grid(1115, 30), Grid())));
    }

    [Fact]
    public void EveryNativeGridFieldAndCapacityModeAreBound()
    {
        var hash = StorageConfigurationPolicy.Fingerprint(State(Grid(1114, 10, 2)));
        var differences = new[]
        {
            State(Grid(1115, 10, 2)), State(Grid(1114, 9, 2)), State(Grid(1114, 10, 3)),
            new StorageUiState("filtered", 0, new[] { Grid(1114, 10, 2, 1114) }),
            new StorageUiState("default", 1, new[] { Grid(1114, 10, 2) }),
            State(new StorageUiGrid(1114, 10, 2, 0, 21)),
            State(Grid(1114, 10, 2), Grid()),
        };
        Assert.All(differences, state => Assert.NotEqual(hash, StorageConfigurationPolicy.Fingerprint(state)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("sort")]
    [InlineData("clear-stock")]
    public void UnsupportedOperationsAreRejectedWithoutPartialProjection(string operation) =>
        Assert.Throws<ArgumentException>(() => Project(State(Grid()), operation));

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(int.MaxValue)]
    public void OutOfRangeBansAreRejectedNotClamped(int bans) =>
        Assert.Throws<ArgumentException>(() => Project(State(Grid()), StorageConfigurationOperations.SetBans, bans: bans));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(9999)]
    [InlineData(12000)]
    public void UnknownOrEmptyItemFilterIsRejected(int filter) =>
        Assert.Throws<ArgumentException>(() => Project(State(Grid()), StorageConfigurationOperations.FilterEmptyOrMatching, filter: filter));

    [Fact]
    public void IrrelevantArgumentsAndNoOpsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => Project(State(Grid()), StorageConfigurationOperations.SetBans, filter: 1000, bans: 1));
        Assert.Throws<ArgumentException>(() => Project(State(Grid()), StorageConfigurationOperations.LockOccupied, bans: 0));
        Assert.Throws<ArgumentException>(() => Project(State(Grid()), StorageConfigurationOperations.ClearFilters));
        Assert.Throws<ArgumentException>(() => Project(State(Grid()), StorageConfigurationOperations.SetBans, bans: 0));
    }

    [Theory]
    [InlineData("delivery-filtered")]
    [InlineData("ammo")]
    [InlineData("")]
    public void UnsupportedModesFailClosed(string mode) =>
        Assert.Throws<ArgumentException>(() => Project(new StorageUiState(mode, 0, new[] { Grid() }), StorageConfigurationOperations.SetBans, bans: 1));

    [Fact]
    public void InvalidNativeGridEvidenceFailsClosed()
    {
        var invalid = new[]
        {
            new StorageUiGrid(0, 1, 0, 0, 0), new StorageUiGrid(1114, -1, 0, 0, 20),
            new StorageUiGrid(1114, 21, 0, 0, 20), new StorageUiGrid(1114, 1, -1, 0, 20),
            new StorageUiGrid(1114, 0, 1, 0, 20), new StorageUiGrid(1114, 1, 0, 0, 21),
            new StorageUiGrid(1114, 1, 0, 1000, 20), new StorageUiGrid(12000, 1, 0, 0, 20),
        };
        Assert.All(invalid, grid => Assert.Throws<ArgumentException>(() => Project(State(grid), StorageConfigurationOperations.SetBans, bans: 1)));
        Assert.Throws<ArgumentException>(() => Project(State(Grid(1114, 1, filter: 1114)), StorageConfigurationOperations.SetBans, bans: 1));
        Assert.Throws<ArgumentException>(() => Project(State(), StorageConfigurationOperations.SetBans, bans: 0));
    }

    [Fact]
    public void StateCopyIsBoundedAndImmutable()
    {
        var source = new[] { Grid(1114, 20) };
        var state = State(source);
        source[0] = Grid(1120, 3);
        Assert.Equal(1114, state.Grids[0].ItemId);
        Assert.Throws<ArgumentException>(() => State(Enumerable.Repeat(Grid(), 101).ToArray()));
        Assert.Throws<ArgumentNullException>(() => State(null!));
        var max = new StorageUiState("default", 0, Enumerable.Repeat(Grid(), 100));
        Assert.Equal(100, Project(max, StorageConfigurationOperations.SetBans, bans: 100).Bans);
    }
}
