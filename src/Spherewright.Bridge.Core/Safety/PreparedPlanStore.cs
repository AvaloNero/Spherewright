using Spherewright.Bridge.Core.Snapshots;

namespace Spherewright.Bridge.Core.Safety;

public sealed class PreparedPlanStore<T>
{
    private readonly object _gate = new object();
    private readonly Dictionary<string, PreparedPlan<T>> _plans = new Dictionary<string, PreparedPlan<T>>(StringComparer.Ordinal);
    private readonly TimeSpan _lifetime;
    private readonly int _capacity;
    private readonly Func<DateTimeOffset> _utcNow;

    public PreparedPlanStore(TimeSpan lifetime, int capacity, Func<DateTimeOffset>? utcNow = null)
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

    public PreparedPlan<T> Add(string fingerprint, T payload)
    {
        if (string.IsNullOrWhiteSpace(fingerprint))
        {
            throw new ArgumentException("A plan fingerprint is required.", nameof(fingerprint));
        }

        lock (_gate)
        {
            RemoveExpiredUnsafe();
            if (_plans.Count >= _capacity)
            {
                throw new InvalidOperationException("Prepared-plan capacity has been reached.");
            }

            var plan = new PreparedPlan<T>(
                OpaqueToken.Create(),
                _utcNow().Add(_lifetime),
                fingerprint,
                payload);
            _plans.Add(plan.Token, plan);
            return plan;
        }
    }

    public bool TryTake(string token, out PreparedPlan<T>? plan, out bool expired)
    {
        plan = null;
        expired = false;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        lock (_gate)
        {
            if (!_plans.TryGetValue(token, out var found))
            {
                return false;
            }

            _plans.Remove(token);
            if (found.ExpiresAtUtc <= _utcNow())
            {
                expired = true;
                return false;
            }

            plan = found;
            return true;
        }
    }

    public bool TryGet(string token, out PreparedPlan<T>? plan, out bool expired)
    {
        plan = null;
        expired = false;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        lock (_gate)
        {
            if (!_plans.TryGetValue(token, out var found))
            {
                return false;
            }

            if (found.ExpiresAtUtc <= _utcNow())
            {
                _plans.Remove(token);
                expired = true;
                return false;
            }

            plan = found;
            return true;
        }
    }

    public bool Remove(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        lock (_gate)
        {
            return _plans.Remove(token);
        }
    }

    private void RemoveExpiredUnsafe()
    {
        var now = _utcNow();
        foreach (var token in _plans.Where(pair => pair.Value.ExpiresAtUtc <= now).Select(pair => pair.Key).ToArray())
        {
            _plans.Remove(token);
        }
    }
}
