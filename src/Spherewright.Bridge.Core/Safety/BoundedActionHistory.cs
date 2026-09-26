namespace Spherewright.Bridge.Core.Safety;

/// <summary>
/// Main-thread action receipts: active work is indexed separately from retained
/// terminal evidence. Capacity refuses admission, never evicts protected work.
/// </summary>
public sealed class BoundedActionHistory<T> where T : class
{
    private readonly int _capacity;
    private readonly TimeSpan _retention;
    private readonly Func<T, bool> _isTerminal;
    private readonly Func<T, bool> _isProtected;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
    private readonly Dictionary<string, T> _active = new Dictionary<string, T>(StringComparer.Ordinal);

    public BoundedActionHistory(int capacity, TimeSpan retention, Func<T, bool> isTerminal,
        Func<T, bool> isProtected, Func<DateTimeOffset>? utcNow = null)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        if (retention <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(retention));
        _capacity = capacity;
        _retention = retention;
        _isTerminal = isTerminal ?? throw new ArgumentNullException(nameof(isTerminal));
        _isProtected = isProtected ?? throw new ArgumentNullException(nameof(isProtected));
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public IEnumerable<T> Values => _entries.Values.Select(entry => entry.Value);
    public IEnumerable<T> ActiveValues => _active.Values.Where(value => !_isTerminal(value));
    public int Count => _entries.Count;

    public bool HasCapacity()
    {
        PruneExpired();
        return _entries.Count < _capacity;
    }

    public void Add(string id, T value)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("An action identity is required.", nameof(id));
        if (value is null) throw new ArgumentNullException(nameof(value));
        if (_entries.ContainsKey(id)) throw new ArgumentException("Duplicate action identity.", nameof(id));
        if (!HasCapacity()) throw new InvalidOperationException("Retained action history is at capacity.");
        _entries.Add(id, new Entry(value));
        _active.Add(id, value);
        RefreshTerminals();
    }

    public bool TryGetValue(string id, out T value)
    {
        PruneExpired();
        if (_entries.TryGetValue(id, out var entry))
        {
            value = entry.Value;
            return true;
        }
        value = null!;
        return false;
    }

    // Called each frame: never enumerate historical terminal receipts here.
    public void RefreshTerminals()
    {
        var now = _utcNow();
        foreach (var pair in _active.Where(pair => _isTerminal(pair.Value)).ToArray())
        {
            _entries[pair.Key].TerminalObservedAtUtc = now;
            _active.Remove(pair.Key);
        }
    }

    // Request/admission-time maintenance, not per-frame historical scanning.
    private void PruneExpired()
    {
        RefreshTerminals();
        var now = _utcNow();
        foreach (var pair in _entries.Where(pair => pair.Value.TerminalObservedAtUtc.HasValue
                     && _isTerminal(pair.Value.Value) && !_isProtected(pair.Value.Value)
                     && now - pair.Value.TerminalObservedAtUtc.Value >= _retention).ToArray())
        {
            _entries.Remove(pair.Key);
        }
    }

    private sealed class Entry
    {
        public Entry(T value) => Value = value;
        public T Value { get; }
        public DateTimeOffset? TerminalObservedAtUtc { get; set; }
    }
}
