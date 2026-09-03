namespace Spherewright.Bridge.Core.Snapshots;

public sealed class SnapshotPage<T>
{
    public SnapshotPage(
        string snapshotId,
        DateTimeOffset expiresAtUtc,
        IReadOnlyList<T> items,
        int totalItemCount,
        string? nextCursor)
    {
        SnapshotId = snapshotId;
        ExpiresAtUtc = expiresAtUtc;
        Items = items;
        TotalItemCount = totalItemCount;
        NextCursor = nextCursor;
    }

    public string SnapshotId { get; }

    public DateTimeOffset ExpiresAtUtc { get; }

    public IReadOnlyList<T> Items { get; }

    public int TotalItemCount { get; }

    public string? NextCursor { get; }
}
