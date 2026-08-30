using Spherewright.Bridge.Core.Snapshots;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class SnapshotPageStoreTests
{
    [Fact]
    public void PagesRemainStableAndBindSessionPlanetFilterAndPageSize()
    {
        var now = DateTimeOffset.Parse("2026-08-30T00:00:00Z");
        var store = new SnapshotPageStore<int>(TimeSpan.FromMinutes(1), 2, () => now);

        Assert.True(store.TryCreate("session-a", 1001, "filter-a", new[] { 1, 2, 3 }, 2, out var first));
        Assert.Equal(new[] { 1, 2 }, first!.Items);
        Assert.NotNull(first.NextCursor);

        Assert.Equal(
            SnapshotCursorStatus.Success,
            store.TryGetPage(first.NextCursor, "session-a", 1001, "filter-a", 2, out var second));
        Assert.Equal(new[] { 3 }, second!.Items);
        Assert.Null(second.NextCursor);

        Assert.Equal(
            SnapshotCursorStatus.Stale,
            store.TryGetPage(first.NextCursor, "session-b", 1001, "filter-a", 2, out _));
        Assert.Equal(
            SnapshotCursorStatus.Stale,
            store.TryGetPage(first.NextCursor, "session-a", 1002, "filter-a", 2, out _));
        Assert.Equal(
            SnapshotCursorStatus.Stale,
            store.TryGetPage(first.NextCursor, "session-a", 1001, "filter-b", 2, out _));
        Assert.Equal(
            SnapshotCursorStatus.Stale,
            store.TryGetPage(first.NextCursor, "session-a", 1001, "filter-a", 1, out _));
    }

    [Fact]
    public void ExpiredCursorIsRejectedAndCapacityIsBounded()
    {
        var now = DateTimeOffset.Parse("2026-08-30T00:00:00Z");
        var store = new SnapshotPageStore<int>(TimeSpan.FromSeconds(30), 1, () => now);

        Assert.True(store.TryCreate("session", 1001, "filter", new[] { 1, 2 }, 1, out var first));
        Assert.False(store.TryCreate("session", 1001, "other", new[] { 3 }, 1, out _));

        now = now.AddSeconds(31);
        Assert.Equal(
            SnapshotCursorStatus.Expired,
            store.TryGetPage(first!.NextCursor, "session", 1001, "filter", 1, out _));
        Assert.True(store.TryCreate("session", 1001, "other", new[] { 3 }, 1, out _));
    }
}
