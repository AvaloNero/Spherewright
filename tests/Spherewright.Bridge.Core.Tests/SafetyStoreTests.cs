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
}
