namespace Spherewright.Bridge.Core.Safety;

public sealed class IdempotencyCache<T>
{
    private readonly object _gate = new object();
    private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
    private readonly int _capacity;

    public IdempotencyCache(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        _capacity = capacity;
    }

    public bool TryGet(string key, string fingerprint, out T? result, out bool conflict)
    {
        result = default;
        conflict = false;
        lock (_gate)
        {
            if (!_entries.TryGetValue(key, out var entry))
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

    public bool TryAdd(string key, string fingerprint, T result)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(fingerprint))
        {
            throw new ArgumentException("Idempotency key and fingerprint are required.");
        }

        lock (_gate)
        {
            if (_entries.ContainsKey(key))
            {
                return false;
            }

            if (_entries.Count >= _capacity)
            {
                return false;
            }

            _entries.Add(key, new Entry(fingerprint, result));
            return true;
        }
    }

    private sealed class Entry
    {
        public Entry(string fingerprint, T result)
        {
            Fingerprint = fingerprint;
            Result = result;
        }

        public string Fingerprint { get; }

        public T Result { get; }
    }
}
