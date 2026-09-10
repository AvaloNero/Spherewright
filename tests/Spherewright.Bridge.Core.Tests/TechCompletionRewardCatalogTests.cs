using Spherewright.Bridge.Core.Progression;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Progression;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class TechCompletionRewardCatalogTests
{
    [Fact]
    public void EmptyPairedArraysProveNoRewardsWithoutLookup()
    {
        Assert.Empty(TechCompletionRewardCatalog.Capture(Array.Empty<int>(), Array.Empty<int>(),
            _ => throw new InvalidOperationException("No items to resolve"))!);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, new int[0])]
    [InlineData(new int[0], null)]
    [InlineData(new[] { 1 }, new int[0])]
    [InlineData(new int[0], new[] { 1 })]
    public void MissingOrMismatchedArraysAreUnknown(int[]? ids, int[]? counts)
    {
        Assert.Null(TechCompletionRewardCatalog.Capture(ids, counts, _ => "item"));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(2, 0)]
    [InlineData(2, -1)]
    public void InvalidEntryRejectsWholeListNotJustInvalidSuffix(int id, int count)
    {
        Assert.Null(TechCompletionRewardCatalog.Capture(new[] { 1, id }, new[] { 1, count }, _ => "item"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void UnknownItemRejectsWholeList(string? missingName)
    {
        Assert.Null(TechCompletionRewardCatalog.Capture(new[] { 1, 2 }, new[] { 1, 1 }, id => id == 1 ? "Known" : missingName));
    }

    [Fact]
    public void MaximumEntriesAcceptedButOversizedMetadataNeverTruncated()
    {
        var ids = Enumerable.Repeat(1, TechCompletionRewardCatalog.MaximumRewardEntries).ToArray();
        var counts = Enumerable.Repeat(2, ids.Length).ToArray();
        Assert.Equal(ids.Length, TechCompletionRewardCatalog.Capture(ids, counts, _ => "item")!.Count);
        Assert.Null(TechCompletionRewardCatalog.Capture(ids.Append(1).ToArray(), counts.Append(1).ToArray(),
            _ => throw new InvalidOperationException("Oversized data must not resolve items")));
    }

    [Fact]
    public void DeepCopyPreservesNativeOrderAndDuplicateGrantEntries()
    {
        var ids = new[] { 3, 1, 3 };
        var counts = new[] { 2, 5, 4 };
        var first = TechCompletionRewardCatalog.Capture(ids, counts, id => $"item{id}")!;
        var second = TechCompletionRewardCatalog.Capture(ids, counts, id => $"item{id}")!;
        ids[0] = 99;
        counts[0] = 99;
        Assert.Equal(new[] { 3, 1, 3 }, first.Select(r => r.ItemId));
        Assert.Equal(new[] { 2, 5, 4 }, first.Select(r => r.Count));
        first[0].Count = 100;
        Assert.Equal(2, second[0].Count);
        Assert.Equal("item3", second[0].Name);
    }

    [Fact]
    public void RewardMetadataDoesNotChangeExistingHashesButRealUnlockAndQueueStillDo()
    {
        var snapshot = new ProgressionStateSnapshot { SessionId = "test", PlanetId = 104,
            Technologies = new() { new TechStateSnapshot { TechId = 1501, HashRequired = 18000 } } };
        var full = CanonicalStateHash.Progression(snapshot);
        var selection = CanonicalStateHash.ProgressionSelection(snapshot);
        foreach (var rewards in new List<TechCompletionItemReward>?[] { new(), new()
            { new TechCompletionItemReward { ItemId = 2205, Name = "Solar panel", Count = 1 } }, null })
        {
            snapshot.Technologies[0].CompletionItemRewards = rewards;
            Assert.Equal(full, CanonicalStateHash.Progression(snapshot));
            Assert.Equal(selection, CanonicalStateHash.ProgressionSelection(snapshot));
        }
        snapshot.Technologies[0].Unlocked = true;
        Assert.NotEqual(full, CanonicalStateHash.Progression(snapshot));
        Assert.NotEqual(selection, CanonicalStateHash.ProgressionSelection(snapshot));
        snapshot.Technologies[0].Unlocked = false;
        snapshot.TechQueue.Add(1501);
        Assert.NotEqual(full, CanonicalStateHash.Progression(snapshot));
        Assert.NotEqual(selection, CanonicalStateHash.ProgressionSelection(snapshot));
    }
}
