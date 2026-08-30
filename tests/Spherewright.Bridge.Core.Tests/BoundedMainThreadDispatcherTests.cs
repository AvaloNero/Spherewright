using Spherewright.Bridge.Core.Abstractions;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class BoundedMainThreadDispatcherTests
{
    [Fact]
    public async Task Pump_ExecutesQueuedWork()
    {
        using var dispatcher = new BoundedMainThreadDispatcher(2);
        Assert.True(dispatcher.TryEnqueue(() => 42, out var completion));

        var executed = dispatcher.Pump(1, TimeSpan.FromSeconds(1));

        Assert.Equal(1, executed);
        Assert.Equal(42, await completion);
    }

    [Fact]
    public async Task FullQueue_RejectsAdditionalWork()
    {
        using var dispatcher = new BoundedMainThreadDispatcher(1);
        Assert.True(dispatcher.TryEnqueue(() => 1, out _));
        Assert.False(dispatcher.TryEnqueue(() => 2, out var rejected));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await rejected);
    }

    [Fact]
    public async Task Dispose_CancelsPendingWork()
    {
        var dispatcher = new BoundedMainThreadDispatcher(1);
        Assert.True(dispatcher.TryEnqueue(() => 1, out var pending));

        dispatcher.Dispose();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pending);
    }
}
