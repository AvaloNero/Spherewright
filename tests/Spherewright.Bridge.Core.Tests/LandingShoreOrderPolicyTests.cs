using Spherewright.Bridge.Core.Safety;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class LandingShoreOrderPolicyTests
{
    [Theory]
    [InlineData(false, false, false, false, true)] // Ordinary landing, no shore order.
    [InlineData(false, true, false, false, false)] // A foreign order appeared.
    [InlineData(true, true, true, false, false)] // Keep walking even on transient Walk.
    [InlineData(true, true, true, true, true)] // Only this reached exact order may be stopped.
    [InlineData(true, false, false, true, true)] // Native achievement dequeued the reached order.
    [InlineData(true, false, false, false, false)] // Missing before arrival is not success.
    [InlineData(true, true, false, false, false)] // Foreign replacement, no abort permission.
    [InlineData(true, true, false, true, false)] // Arrival flag does not authorize a foreign abort.
    public void StableWalkGateRequiresReachedExactOrderOrNoOrder(bool tracked, bool current,
        bool exact, bool reached, bool expected) => Assert.Equal(expected,
        LandingShoreOrderPolicy.MayVerifyStableWalk(tracked, current, exact, reached));

    [Theory]
    [InlineData(false, false, false, true)]
    [InlineData(false, false, true, false)]
    [InlineData(false, true, true, true)]
    [InlineData(true, false, true, true)]
    public void InconsistentEvidenceCannotStartStableVerification(bool tracked, bool current,
        bool exact, bool reached) => Assert.False(
        LandingShoreOrderPolicy.MayVerifyStableWalk(tracked, current, exact, reached));

    [Fact]
    public void TransientWalkCannotAbortTheUnreachedOrderBeforeLaterDrift()
    {
        // Old adapter aborted on its first Walk tick, leaving an unreached tracked node.
        // The gate must continue exact movement on every transient Walk instead.
        foreach (var _ in new[] { "Walk", "Walk", "Walk" })
            Assert.False(LandingShoreOrderPolicy.MayVerifyStableWalk(true, true, true, false));
        Assert.True(LandingShoreOrderPolicy.MayVerifyStableWalk(true, false, false, true));
    }

    [Fact]
    public void AnExternallyClearedUnreachedOrderCannotBecomeSuccessfulByWaiting()
    {
        Assert.False(LandingShoreOrderPolicy.MayVerifyStableWalk(true, true, true, false));
        for (var i = 0; i < 601; i++)
            Assert.False(LandingShoreOrderPolicy.MayVerifyStableWalk(true, false, false, false));
    }
}
