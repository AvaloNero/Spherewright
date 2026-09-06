using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Actions;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class StorageConfigurationReadbackTests
{
    private static readonly IReadOnlyDictionary<int, int> Sizes = new Dictionary<int, int>
        { [1000] = 20, [1114] = 20, [1120] = 20 };
    private static StorageUiGrid Hydrogen(int count = 1, int inc = 0) => new(1120, count, inc, 1120, 20);
    private static StorageUiState Before() => new("filtered", 2,
        new[] { Hydrogen(), new StorageUiGrid(1000, 0, 0, 1000, 20) });
    private static StorageConfigurationReadback Readback(StorageUiState before, StorageUiState after,
        int entity = 761, long tick = 100, int connections = 5) => StorageConfigurationPolicy.CreateReadback(
            entity, tick, before, after, StorageConfigurationOperations.SetBans, 0, 0, Sizes, connections);

    [Fact]
    public void UnbanProofRetainsInstantInventoryNotSubsequentNativeDelivery()
    {
        var before = Before();
        var after = new StorageUiState("filtered", 0, before.Grids);
        var proof = Readback(before, after);
        var later = new StorageUiState("filtered", 0, new[] { Hydrogen(4), before.Grids[1] });
        Assert.Equal(1, Assert.Single(proof.BuffersBefore).Count);
        Assert.Equal(1, Assert.Single(proof.BuffersAfter).Count);
        Assert.Equal(4, later.Grids[0].Count);
        Assert.Equal(100, proof.CapturedAtGameTick);
        Assert.Equal(761, proof.EntityId);
        Assert.Equal(5, proof.VerifiedConnectionCount);
        Assert.Equal("synchronous_native_configuration_boundary", proof.EvidenceScope);
        Assert.Equal(2, proof.ConfigurationBefore.BannedGridCount);
        Assert.Equal(0, proof.ConfigurationAfter.BannedGridCount);
    }

    [Fact]
    public void EmptyWaterReservationCreatesNoItemAndListsHaveIndependentCopies()
    {
        var before = new StorageUiState("default", 0, new[]
            { new StorageUiGrid(1114, 12, 3, 0, 20), new StorageUiGrid(0, 0, 0, 0, 0) });
        var after = StorageConfigurationPolicy.Project(before,
            StorageConfigurationOperations.FilterEmptyOrMatching, 1000, -1, Sizes);
        var proof = StorageConfigurationPolicy.CreateReadback(761, 200, before, after,
            StorageConfigurationOperations.FilterEmptyOrMatching, 1000, -1, Sizes, 5);
        Assert.Equal(new[] { 0, 1000 }, proof.ConfigurationAfter.GridFilterItemIds);
        Assert.Equal(1114, Assert.Single(proof.BuffersAfter).ItemId);
        Assert.Equal(12, proof.BuffersAfter[0].Count); Assert.Equal(3, proof.BuffersAfter[0].Inc);
        proof.BuffersAfter[0].Count = 999;
        proof.ConfigurationAfter.GridFilterItemIds[0] = 1120;
        Assert.Equal(12, proof.BuffersBefore[0].Count);
        Assert.Equal(0, proof.ConfigurationBefore.GridFilterItemIds[0]);
        Assert.Equal(12, after.Grids[0].Count); Assert.Equal(0, after.Grids[0].Filter);
    }

    [Theory]
    [InlineData("count")]
    [InlineData("inc")]
    [InlineData("order")]
    [InlineData("filter")]
    [InlineData("bans")]
    [InlineData("mode")]
    [InlineData("missing")]
    public void UnexpectedActualStateCannotBecomeSuccessfulReadback(string change)
    {
        var before = Before(); var grids = before.Grids.ToList();
        var bans = 0; var mode = "filtered";
        switch (change)
        {
            case "count": grids[0] = Hydrogen(4); break;
            case "inc": grids[0] = Hydrogen(1, 2); break;
            case "order": grids.Reverse(); break;
            case "filter": grids[1] = new StorageUiGrid(1114, 0, 0, 1114, 20); break;
            case "bans": bans = 1; break;
            case "mode": mode = "default"; break;
            case "missing": grids.RemoveAt(1); break;
        }
        Assert.Throws<ArgumentException>(() => Readback(before, new StorageUiState(mode, bans, grids)));
    }

    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(-1, 1, 0)]
    [InlineData(761, -1, 0)]
    [InlineData(761, 1, -1)]
    [InlineData(761, 1, 17)]
    public void InvalidIdentityTickOrUnboundedLinkCountIsRejected(int entity, long tick, int links)
    {
        var before = Before();
        Assert.Throws<ArgumentException>(() => Readback(before,
            new StorageUiState("filtered", 0, before.Grids), entity, tick, links));
    }

    [Fact]
    public void AllFourUiOperationsUseTheSameCheckedProjection()
    {
        var before = new StorageUiState("default", 0, new[]
            { new StorageUiGrid(1120, 1, 0, 0, 20), new StorageUiGrid(0, 0, 0, 0, 0) });
        foreach (var operation in new[] { StorageConfigurationOperations.SetBans,
            StorageConfigurationOperations.LockOccupied, StorageConfigurationOperations.FilterEmptyOrMatching,
            StorageConfigurationOperations.ClearFilters })
        {
            var input = operation == StorageConfigurationOperations.ClearFilters ? Before() : before;
            var filter = operation == StorageConfigurationOperations.FilterEmptyOrMatching ? 1000 : 0;
            var bans = operation == StorageConfigurationOperations.SetBans ? 2 : -1;
            var after = StorageConfigurationPolicy.Project(input, operation, filter, bans, Sizes);
            var proof = StorageConfigurationPolicy.CreateReadback(761, 0, input, after, operation, filter, bans, Sizes, 0);
            Assert.Equal(operation, proof.Operation);
            Assert.Equal(1, Assert.Single(proof.BuffersAfter).Count);
        }
    }
}
