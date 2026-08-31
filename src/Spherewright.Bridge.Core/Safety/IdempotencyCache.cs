namespace Spherewright.Bridge.Core.Safety;

public sealed class IdempotencyCache<T>
{
    private readonly object _gate = new object();
    private readonly Dictionary<string, Dictionary<string, Entry>> _entriesByScope =
        new Dictionary<string, Dictionary<string, Entry>>(StringComparer.Ordinal);
    private readonly int _capacityPerScope;
    private readonly TimeSpan _retention;
    private readonly Func<DateTimeOffset> _utcNow;

    public IdempotencyCache(int capacity)
        : this(capacity, TimeSpan.FromMinutes(30))
    {
    }

    public IdempotencyCache(
        int capacity,
        TimeSpan retention,
        Func<DateTimeOffset>? utcNow = null)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        if (retention <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retention));
        }

        _capacityPerScope = capacity;
        _retention = retention;
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public bool TryGet(string key, string fingerprint, out T? result, out bool conflict)
        => TryGetCore(string.Empty, key, fingerprint, out result, out conflict);

    public bool TryGet(
        string scope,
        string key,
        string fingerprint,
        out T? result,
        out bool conflict)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            throw new ArgumentException("An idempotency scope is required.", nameof(scope));
        }

        return TryGetCore(scope, key, fingerprint, out result, out conflict);
    }

    public bool TryAdd(string key, string fingerprint, T result)
        => TryAddCore(string.Empty, key, fingerprint, result);

    public bool TryAdd(string scope, string key, string fingerprint, T result)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            throw new ArgumentException("An idempotency scope is required.", nameof(scope));
        }

        return TryAddCore(scope, key, fingerprint, result);
    }

    public bool HasCapacity() => HasCapacityCore(string.Empty);

    public bool HasCapacity(string scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            throw new ArgumentException("An idempotency scope is required.", nameof(scope));
        }

        return HasCapacityCore(scope);
    }

    private bool TryGetCore(
        string scope,
        string key,
        string fingerprint,
        out T? result,
        out bool conflict)
    {
        result = default;
        conflict = false;
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(fingerprint))
        {
            throw new ArgumentException("Idempotency key and fingerprint are required.");
        }

        lock (_gate)
        {
            var now = _utcNow();
            RemoveExpiredUnsafe(now);
            if (!_entriesByScope.TryGetValue(scope, out var entries))
            {
                return false;
            }

            if (!entries.TryGetValue(key, out var entry))
            {
                return false;
            }

            if (!string.Equals(entry.Fingerprint, fingerprint, StringComparison.Ordinal))
            {
                conflict = true;
                return false;
            }

            result = entry.Result;
            return true;
        }
    }

    private bool TryAddCore(string scope, string key, string fingerprint, T result)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(fingerprint))
        {
            throw new ArgumentException("Idempotency key and fingerprint are required.");
        }

        lock (_gate)
        {
            var now = _utcNow();
            RemoveExpiredUnsafe(now);
            if (!_entriesByScope.TryGetValue(scope, out var entries))
            {
                entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
                _entriesByScope.Add(scope, entries);
            }

            if (entries.ContainsKey(key))
            {
                return false;
            }

            if (entries.Count >= _capacityPerScope)
            {
                return false;
            }

            entries.Add(key, new Entry(fingerprint, result, now.Add(_retention)));
            return true;
        }
    }

    private bool HasCapacityCore(string scope)
    {
        lock (_gate)
        {
            RemoveExpiredUnsafe(_utcNow());
            return !_entriesByScope.TryGetValue(scope, out var entries)
                || entries.Count < _capacityPerScope;
        }
    }

    private void RemoveExpiredUnsafe(DateTimeOffset now)
    {
        foreach (var scope in _entriesByScope.Keys.ToArray())
        {
            var entries = _entriesByScope[scope];
            foreach (var key in entries
                .Where(pair => pair.Value.ExpiresAtUtc <= now)
                .Select(pair => pair.Key)
                .ToArray())
            {
                entries.Remove(key);
            }

            if (entries.Count == 0)
            {
                _entriesByScope.Remove(scope);
            }
        }
    }

    private sealed class Entry
    {
        public Entry(string fingerprint, T result, DateTimeOffset expiresAtUtc)
        {
            Fingerprint = fingerprint;
            Result = result;
            ExpiresAtUtc = expiresAtUtc;
        }

        public string Fingerprint { get; }

        public T Result { get; }

        public DateTimeOffset ExpiresAtUtc { get; }
    }
}
