using Spherewright.Bridge.Core.Factory;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class BlueprintStoragePolicyTests
{
    [Theory]
    [InlineData(0, -1)] [InlineData(0, 101)] [InlineData(1, 1)] [InlineData(1, -9)]
    [InlineData(2, 1)] [InlineData(9, 1)] [InlineData(10, -1)] [InlineData(109, 32768)]
    public void RejectsUnknownOrNativeIgnoredFields(int index, int value)
    {
        var p = Filtered(); p[index] = value;
        Assert.False(BlueprintStoragePolicy.IsSupportedShape(p));
    }

    [Fact]
    public void DefaultModeCannotSilentlyDropFilters()
    {
        var p = Filtered(); p[1] = 0;
        Assert.False(BlueprintStoragePolicy.IsSupportedShape(p));
        p[10] = 0;
        Assert.True(BlueprintStoragePolicy.FitsNativeStorage(p, 30, _ => false));
    }

    [Fact]
    public void FilterTechnologyIsCheckedSeparatelyFromItemValidity()
    {
        Assert.True(BlueprintStoragePolicy.FiltersUnlocked(Filtered(), id => id == 1109));
        Assert.False(BlueprintStoragePolicy.FiltersUnlocked(Filtered(), _ => false));
        Assert.True(BlueprintStoragePolicy.FiltersUnlocked(new int[110], _ => throw new InvalidOperationException()));
        Assert.False(BlueprintStoragePolicy.FiltersUnlocked(Array.Empty<int>(), _ => true));
    }

    [Theory]
    [InlineData(0)] [InlineData(-1)] [InlineData(101)] [InlineData(int.MaxValue)]
    public void BoundsNativeCapacityBeforeGridAccess(int count) =>
        Assert.False(BlueprintStoragePolicy.FitsNativeStorage(Filtered(), count, _ => true));

    [Fact]
    public void RejectsBansClampingFiltersOutsideCapacityAndUnsupportedNativeItems()
    {
        var p = Filtered();
        Assert.True(BlueprintStoragePolicy.FitsNativeStorage(p, 30, id => id == 1109));
        Assert.False(BlueprintStoragePolicy.FitsNativeStorage(p, 30, _ => false));
        p[40] = 1109;
        Assert.False(BlueprintStoragePolicy.FitsNativeStorage(p, 30, _ => true));
        p[40] = 0; p[0] = 31;
        Assert.False(BlueprintStoragePolicy.FitsNativeStorage(p, 30, _ => true));
    }

    [Fact]
    public void ReconciliationRequiresExactModeCapacityBansAndEveryFilter()
    {
        var p = Filtered(); var actual = Configuration();
        Assert.True(BlueprintStoragePolicy.MatchesConfiguration(p, actual));
        actual.BannedGridCount++; Assert.False(BlueprintStoragePolicy.MatchesConfiguration(p, actual)); actual.BannedGridCount--;
        actual.Mode = "default"; Assert.False(BlueprintStoragePolicy.MatchesConfiguration(p, actual)); actual.Mode = "filtered";
        actual.GridFilterItemIds[1] = 1101; Assert.False(BlueprintStoragePolicy.MatchesConfiguration(p, actual)); actual.GridFilterItemIds[1] = 0;
        actual.GridCount++; Assert.False(BlueprintStoragePolicy.MatchesConfiguration(p, actual));
        Assert.False(BlueprintStoragePolicy.MatchesConfiguration(p, null));
    }

    [Theory]
    [InlineData("state")] [InlineData("endpoint")] [InlineData("configuration")]
    public void AllFactoryHashesBindStorageConfigurationIncludingEmptyGridFilters(string kind)
    {
        var snapshot = new FactoryEntitySnapshot { ItemId = 2101, StorageConfiguration = Configuration() };
        string Hash() => kind == "state" ? CanonicalStateHash.Factory(snapshot)
            : kind == "endpoint" ? CanonicalStateHash.FactoryEndpoint(snapshot) : CanonicalStateHash.FactoryConfiguration(snapshot);
        var before = Hash();
        AssertChanged(() => snapshot.StorageConfiguration.BannedGridCount++, () => snapshot.StorageConfiguration.BannedGridCount--);
        AssertChanged(() => snapshot.StorageConfiguration.GridCount++, () => snapshot.StorageConfiguration.GridCount--);
        AssertChanged(() => snapshot.StorageConfiguration.Mode = "default", () => snapshot.StorageConfiguration.Mode = "filtered");
        AssertChanged(() => snapshot.StorageConfiguration.GridFilterItemIds[29] = 1101, () => snapshot.StorageConfiguration.GridFilterItemIds[29] = 0);
        snapshot.StorageConfiguration = null; Assert.NotEqual(before, Hash());
        void AssertChanged(Action change, Action undo) { change(); Assert.NotEqual(before, Hash()); undo(); Assert.Equal(before, Hash()); }
    }

    [Fact]
    public void FreshStorageSelectionDoesNotStaleOnNormalCargoProgress()
    {
        var snapshot = new FactoryEntitySnapshot { ItemId = 2101, StorageConfiguration = Configuration() };
        var endpoint = CanonicalStateHash.FactoryEndpoint(snapshot);
        snapshot.Buffers.Add(new FactoryBufferSnapshot { Role = "storage", ItemId = 1109, Count = 100 });
        snapshot.CapturedAtGameTick += 60;
        Assert.Equal(endpoint, CanonicalStateHash.FactoryEndpoint(snapshot));
        Assert.True(BlueprintStoragePolicy.MatchesConfiguration(Filtered(), snapshot.StorageConfiguration));
    }

    private static int[] Filtered()
    {
        var p = new int[110]; p[0] = 2; p[1] = 9; p[10] = 1109; return p;
    }

    private static StorageConfigurationSnapshot Configuration() => new()
    {
        GridCount = 30, BannedGridCount = 2, Mode = "filtered",
        GridFilterItemIds = new[] { 1109 }.Concat(Enumerable.Repeat(0, 29)).ToList(),
    };
}
