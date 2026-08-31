using Spherewright.Bridge.Core.Safety;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class SafetyStoreTests
{
    [Fact]
    public void PreparedPlanStore_TakesPlanOnlyOnce()
    {
        var store = new PreparedPlanStore<string>(TimeSpan.FromMinutes(1), 2);
        var prepared = store.Add("fingerprint", "payload");

        Assert.True(store.TryTake(prepared.Token, out var plan, out var expired));
        Assert.False(expired);
        Assert.Equal("payload", plan?.Payload);
        Assert.False(store.TryTake(prepared.Token, out _, out _));
    }

    [Fact]
    public void PreparedPlanStore_ReportsExpiredPlan()
    {
        var now = DateTimeOffset.Parse("2026-08-30T00:00:00Z");
        var store = new PreparedPlanStore<string>(TimeSpan.FromSeconds(30), 2, () => now);
        var prepared = store.Add("fingerprint", "payload");
        now = now.AddSeconds(31);

        Assert.False(store.TryTake(prepared.Token, out _, out var expired));
        Assert.True(expired);
    }

    [Fact]
    public void IdempotencyCache_ReplaysSameFingerprintAndRejectsConflict()
    {
        var cache = new IdempotencyCache<string>(2);
        Assert.True(cache.TryAdd("key", "request-a", "result-a"));

        Assert.True(cache.TryGet("key", "request-a", out var result, out var conflict));
        Assert.False(conflict);
        Assert.Equal("result-a", result);

        Assert.False(cache.TryGet("key", "request-b", out _, out conflict));
        Assert.True(conflict);
    }

    [Fact]
    public void IdempotencyCache_ExpiresEntriesAndReclaimsCapacity()
    {
        var now = DateTimeOffset.Parse("2026-08-30T00:00:00Z");
        var cache = new IdempotencyCache<string>(
            1,
            TimeSpan.FromSeconds(30),
            () => now);
        Assert.True(cache.TryAdd("session-a", "key-a", "request-a", "result-a"));

        now = now.AddSeconds(31);

        Assert.False(cache.TryGet("session-a", "key-a", "request-a", out _, out var conflict));
        Assert.False(conflict);
        Assert.True(cache.TryAdd("session-a", "key-b", "request-b", "result-b"));
    }

    [Fact]
    public void IdempotencyCache_IsolatesCapacityAndKeysByScope()
    {
        var cache = new IdempotencyCache<string>(1);
        Assert.True(cache.TryAdd("session-a", "shared-key", "request-a", "result-a"));
        Assert.True(cache.TryAdd("session-b", "shared-key", "request-b", "result-b"));

        Assert.True(cache.TryGet("session-a", "shared-key", "request-a", out var first, out var firstConflict));
        Assert.False(firstConflict);
        Assert.Equal("result-a", first);

        Assert.True(cache.TryGet("session-b", "shared-key", "request-b", out var second, out var secondConflict));
        Assert.False(secondConflict);
        Assert.Equal("result-b", second);
    }

    [Fact]
    public void IdempotencyCache_AcceptsOnlyOneConcurrentReservation()
    {
        var cache = new IdempotencyCache<string>(8);
        var accepted = 0;

        Parallel.For(0, 32, _ =>
        {
            if (cache.TryAdd("session-a", "shared-key", "request-a", "result-a"))
            {
                Interlocked.Increment(ref accepted);
            }
        });

        Assert.Equal(1, accepted);
    }

    [Fact]
    public void IdempotencyCache_HasCapacityPrunesExpiredEntriesWithinScope()
    {
        var now = DateTimeOffset.Parse("2026-08-30T00:00:00Z");
        var cache = new IdempotencyCache<string>(
            1,
            TimeSpan.FromSeconds(30),
            () => now);
        Assert.True(cache.TryAdd("session-a", "key-a", "request-a", "result-a"));
        Assert.False(cache.HasCapacity("session-a"));
        Assert.True(cache.HasCapacity("session-b"));

        now = now.AddSeconds(31);

        Assert.True(cache.HasCapacity("session-a"));
    }
}
