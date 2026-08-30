namespace Spherewright.Bridge.Core.Snapshots;

public sealed class SnapshotPageStore<T>
{
    private readonly object _gate = new object();
    private readonly Dictionary<string, SnapshotRecord> _snapshots =
        new Dictionary<string, SnapshotRecord>(StringComparer.Ordinal);
    private readonly Dictionary<string, CursorRecord> _cursors =
        new Dictionary<string, CursorRecord>(StringComparer.Ordinal);
    private readonly TimeSpan _lifetime;
    private readonly int _capacity;
    private readonly Func<DateTimeOffset> _utcNow;

    public SnapshotPageStore(
        TimeSpan lifetime,
        int capacity,
        Func<DateTimeOffset>? utcNow = null)
    {
        if (lifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(lifetime));
        }

        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        _lifetime = lifetime;
        _capacity = capacity;
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public bool TryCreate(
        string sessionId,
        int planetId,
        string filterHash,
        IReadOnlyList<T> items,
        int pageSize,
        out SnapshotPage<T>? page)
    {
        ValidateBinding(sessionId, planetId, filterHash, pageSize);
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        lock (_gate)
        {
            RemoveExpiredUnsafe();
            if (_snapshots.Count >= _capacity)
            {
                page = null;
                return false;
            }

            var now = _utcNow();
            var snapshot = new SnapshotRecord(
                OpaqueToken.Create(),
                sessionId,
                planetId,
                filterHash,
                pageSize,
                now.Add(_lifetime),
                items.ToArray());
            _snapshots.Add(snapshot.Id, snapshot);
            page = CreatePageUnsafe(snapshot, 0);
            return true;
        }
    }

    public SnapshotCursorStatus TryGetPage(
        string? cursor,
        string sessionId,
        int planetId,
        string filterHash,
        int pageSize,
        out SnapshotPage<T>? page)
    {
        ValidateBinding(sessionId, planetId, filterHash, pageSize);
        page = null;
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return SnapshotCursorStatus.Missing;
        }

        lock (_gate)
        {
            if (!_cursors.TryGetValue(cursor!, out var cursorRecord)
                || !_snapshots.TryGetValue(cursorRecord.SnapshotId, out var snapshot))
            {
                return SnapshotCursorStatus.Missing;
            }

            if (snapshot.ExpiresAtUtc <= _utcNow())
            {
                RemoveSnapshotUnsafe(snapshot.Id);
                return SnapshotCursorStatus.Expired;
            }

            if (!string.Equals(snapshot.SessionId, sessionId, StringComparison.Ordinal)
                || snapshot.PlanetId != planetId
                || !string.Equals(snapshot.FilterHash, filterHash, StringComparison.Ordinal)
                || snapshot.PageSize != pageSize)
            {
                return SnapshotCursorStatus.Stale;
            }

            page = CreatePageUnsafe(snapshot, cursorRecord.Offset);
            return SnapshotCursorStatus.Success;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _cursors.Clear();
            _snapshots.Clear();
        }
    }

    private SnapshotPage<T> CreatePageUnsafe(SnapshotRecord snapshot, int offset)
    {
        var count = Math.Min(snapshot.PageSize, Math.Max(0, snapshot.Items.Count - offset));
        var items = new T[count];
        for (var index = 0; index < count; index++)
        {
            items[index] = snapshot.Items[offset + index];
        }

        var nextOffset = offset + count;
        string? nextCursor = null;
        if (nextOffset < snapshot.Items.Count)
        {
            nextCursor = snapshot.CursorsByOffset.TryGetValue(nextOffset, out var existing)
                ? existing
                : OpaqueToken.Create();
            snapshot.CursorsByOffset[nextOffset] = nextCursor;
            _cursors[nextCursor] = new CursorRecord(snapshot.Id, nextOffset);
        }

        return new SnapshotPage<T>(snapshot.Id, snapshot.ExpiresAtUtc, items, nextCursor);
    }

    private void RemoveExpiredUnsafe()
    {
        var now = _utcNow();
        foreach (var snapshotId in _snapshots
            .Where(pair => pair.Value.ExpiresAtUtc <= now)
            .Select(pair => pair.Key)
            .ToArray())
        {
            RemoveSnapshotUnsafe(snapshotId);
        }
    }

    private void RemoveSnapshotUnsafe(string snapshotId)
    {
        if (!_snapshots.TryGetValue(snapshotId, out var snapshot))
        {
            return;
        }

        _snapshots.Remove(snapshotId);

        foreach (var cursor in snapshot.CursorsByOffset.Values)
        {
            _cursors.Remove(cursor);
        }
    }

    private static void ValidateBinding(string sessionId, int planetId, string filterHash, int pageSize)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("A snapshot session ID is required.", nameof(sessionId));
        }

        if (planetId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(planetId));
        }

        if (string.IsNullOrWhiteSpace(filterHash))
        {
            throw new ArgumentException("A snapshot filter hash is required.", nameof(filterHash));
        }

        if (pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }
    }

    private sealed class SnapshotRecord
    {
        public SnapshotRecord(
            string id,
            string sessionId,
            int planetId,
            string filterHash,
            int pageSize,
            DateTimeOffset expiresAtUtc,
            IReadOnlyList<T> items)
        {
            Id = id;
            SessionId = sessionId;
            PlanetId = planetId;
            FilterHash = filterHash;
            PageSize = pageSize;
            ExpiresAtUtc = expiresAtUtc;
            Items = items;
        }

        public string Id { get; }

        public string SessionId { get; }

        public int PlanetId { get; }

        public string FilterHash { get; }

        public int PageSize { get; }

        public DateTimeOffset ExpiresAtUtc { get; }

        public IReadOnlyList<T> Items { get; }

        public Dictionary<int, string> CursorsByOffset { get; } = new Dictionary<int, string>();
    }

    private sealed class CursorRecord
    {
        public CursorRecord(string snapshotId, int offset)
        {
            SnapshotId = snapshotId;
            Offset = offset;
        }

        public string SnapshotId { get; }

        public int Offset { get; }
    }
}
