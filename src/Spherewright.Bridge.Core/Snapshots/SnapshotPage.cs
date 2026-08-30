namespace Spherewright.Bridge.Core.Snapshots;

public sealed class SnapshotPage<T>
{
    public SnapshotPage(
        string snapshotId,
        DateTimeOffset expiresAtUtc,
        IReadOnlyList<T> items,
        string? nextCursor)
    {
        SnapshotId = snapshotId;
        ExpiresAtUtc = expiresAtUtc;
        Items = items;
        NextCursor = nextCursor;
    }

    public string SnapshotId { get; }

    public DateTimeOffset ExpiresAtUtc { get; }

    public IReadOnlyList<T> Items { get; }

    public string? NextCursor { get; }
}
