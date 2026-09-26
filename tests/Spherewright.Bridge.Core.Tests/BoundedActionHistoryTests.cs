using Spherewright.Bridge.Core.Safety;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class BoundedActionHistoryTests
{
    private sealed class Receipt
    {
        public bool Terminal { get; set; }
        public bool Protected { get; set; }
    }

    [Fact]
    public void IdempotencyReplayWindowCannotOutliveTheTerminalReceipt()
    {
        var now = DateTimeOffset.UnixEpoch;
        var retention = TimeSpan.FromMinutes(30);
        var cache = new IdempotencyCache<string>(1, retention, () => now);
        var history = new BoundedActionHistory<Receipt>(1, retention, r => r.Terminal, r => r.Protected, () => now);
        var receipt = new Receipt();
        Assert.True(cache.TryAdd("session", "key", "fingerprint", "action"));
        history.Add("action", receipt);
        now = now.AddMinutes(10);
        receipt.Terminal = true;
        history.RefreshTerminals();
        now = now.AddMinutes(19);
        Assert.True(cache.TryGet("session", "key", "fingerprint", out var id, out _));
        Assert.True(history.TryGetValue(id!, out _));
        now = now.AddMinutes(11);
        Assert.False(cache.TryGet("session", "key", "fingerprint", out _, out _));
        Assert.False(history.TryGetValue("action", out _));
    }

    [Fact]
    public void FullHistoryRefusesNewAdmissionWithoutEviction()
    {
        var history = new BoundedActionHistory<Receipt>(1, TimeSpan.FromMinutes(30), r => r.Terminal, r => r.Protected);
        var receipt = new Receipt();
        history.Add("a", receipt);
        Assert.False(history.HasCapacity());
        Assert.Throws<InvalidOperationException>(() => history.Add("b", new Receipt()));
        Assert.True(history.TryGetValue("a", out var retained));
        Assert.Same(receipt, retained);
    }

    [Fact]
    public void RetentionStartsAtObservedCompletionNotAcceptance()
    {
        var now = DateTimeOffset.UnixEpoch;
        var history = new BoundedActionHistory<Receipt>(1, TimeSpan.FromMinutes(30), r => r.Terminal, r => r.Protected, () => now);
        var receipt = new Receipt();
        history.Add("a", receipt);
        now = now.AddHours(2);
        Assert.False(history.HasCapacity());
        receipt.Terminal = true;
        history.RefreshTerminals();
        Assert.Empty(history.ActiveValues);
        now = now.AddMinutes(29);
        Assert.True(history.TryGetValue("a", out _));
        now = now.AddMinutes(1);
        Assert.True(history.HasCapacity());
        Assert.False(history.TryGetValue("a", out _));
    }

    [Fact]
    public void ProtectedTerminalAndActiveRecordsNeverExpire()
    {
        var now = DateTimeOffset.UnixEpoch;
        var history = new BoundedActionHistory<Receipt>(2, TimeSpan.FromMinutes(1), r => r.Terminal, r => r.Protected, () => now);
        history.Add("unknown", new Receipt { Terminal = true, Protected = true });
        history.Add("active", new Receipt());
        now = now.AddDays(10);
        Assert.False(history.HasCapacity());
        Assert.True(history.TryGetValue("unknown", out _));
        Assert.True(history.TryGetValue("active", out _));
        Assert.Single(history.ActiveValues);
    }

    [Fact]
    public void FrameRefreshDoesNotVisitTerminalHistory()
    {
        var visits = 0;
        var history = new BoundedActionHistory<Receipt>(100, TimeSpan.FromMinutes(30), r => { visits++; return r.Terminal; }, r => r.Protected);
        for (var i = 0; i < 90; i++) history.Add(i.ToString(), new Receipt { Terminal = true });
        history.Add("active", new Receipt());
        visits = 0;
        history.RefreshTerminals();
        Assert.Equal(1, visits);
        Assert.Single(history.ActiveValues);
        Assert.Equal(91, history.Count);
    }

    [Fact]
    public void ClockMovingBackwardDoesNotDiscardEvidence()
    {
        var now = DateTimeOffset.UnixEpoch.AddHours(1);
        var history = new BoundedActionHistory<Receipt>(1, TimeSpan.FromMinutes(1), r => r.Terminal, r => r.Protected, () => now);
        history.Add("a", new Receipt { Terminal = true });
        now = now.AddHours(-1);
        Assert.False(history.HasCapacity());
        Assert.True(history.TryGetValue("a", out _));
    }

    [Fact]
    public void DuplicateIdentityDoesNotReplaceReceipt()
    {
        var history = new BoundedActionHistory<Receipt>(2, TimeSpan.FromMinutes(1), r => r.Terminal, r => r.Protected);
        var first = new Receipt();
        history.Add("a", first);
        Assert.Throws<ArgumentException>(() => history.Add("a", new Receipt()));
        Assert.True(history.TryGetValue("a", out var current));
        Assert.Same(first, current);
    }
}
