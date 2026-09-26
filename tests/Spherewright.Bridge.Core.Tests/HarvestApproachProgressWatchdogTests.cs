using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Actions;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class HarvestApproachProgressWatchdogTests
{
    [Fact]
    public void Observe_FlagsPositionStallAt180TicksWhenApproachDoesNotMove()
    {
        var watchdog = CreateWatchdog();

        Assert.Equal(
            MovementProgressStatus.Progressing,
            watchdog.Observe(179, 100, 0, 0, false, false, false).Status);

        var observation = watchdog.Observe(180, 100, 0, 0, false, false, false);

        Assert.Equal(MovementProgressStatus.PositionStalled, observation.Status);
        Assert.False(watchdog.IsApproachMonitoringComplete);
    }

    [Fact]
    public void Observe_FlagsRouteStallAt600TicksWhenPlayerCirclesApproach()
    {
        var watchdog = CreateWatchdog();

        var circle = new[]
        {
            (X: 0d, Y: 100d),
            (X: -100d, Y: 0d),
            (X: 0d, Y: -100d),
            (X: 100d, Y: 0d),
            (X: 0d, Y: 100d),
        };
        for (var index = 0; index < circle.Length; index++)
        {
            var tick = (index + 1) * 100L;
            Assert.Equal(
                MovementProgressStatus.Progressing,
                watchdog.Observe(tick, circle[index].X, circle[index].Y, 0, false, false, false).Status);
        }

        var observation = watchdog.Observe(600, -100, 0, 0, false, false, false);

        Assert.Equal(MovementProgressStatus.RouteStalled, observation.Status);
        Assert.False(watchdog.IsApproachMonitoringComplete);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void Observe_PermanentlyRetiresApproachMonitoringWhenMiningCanProceed(
        bool targetReached,
        bool hasObservedYield,
        bool hasObservedNodeReduction)
    {
        var watchdog = CreateWatchdog();

        var first = watchdog.Observe(
            180,
            100,
            0,
            0,
            targetReached,
            hasObservedYield,
            hasObservedNodeReduction);
        var later = watchdog.Observe(2_000, 100, 0, 0, false, false, false);

        Assert.True(watchdog.IsApproachMonitoringComplete);
        Assert.Equal(MovementProgressStatus.Progressing, first.Status);
        Assert.Equal(MovementProgressStatus.Progressing, later.Status);
    }

    [Fact]
    public void ResetWindowAfterPowerRecovery_DoesNotImmediatelyStall()
    {
        var watchdog = CreateWatchdog();

        watchdog.ResetWindow(1_000, 100, 0, 0);

        Assert.Equal(
            MovementProgressStatus.Progressing,
            watchdog.Observe(1_179, 100, 0, 0, false, false, false).Status);
        Assert.Equal(
            MovementProgressStatus.PositionStalled,
            watchdog.Observe(1_180, 100, 0, 0, false, false, false).Status);
    }

    [Fact]
    public void Observe_PositionStallMapsItsTickAndRemainingDistanceToExistingRecoveryAdvice()
    {
        var watchdog = CreateWatchdog();

        var observation = watchdog.Observe(180, 100, 0, 0, false, false, false);
        var advice = MovementFailureRecoveryAdvisor.ForStall(observation);

        Assert.Equal(MovementFailureKinds.PositionStalled, advice.FailureKind);
        Assert.Equal(180, advice.StalledGameTicks);
        Assert.Equal(100d, advice.RemainingDistance);
        Assert.True(advice.DoNotRetrySameTarget);
    }

    [Fact]
    public void ExistingMoveWatchdog_RemainsAtIts180TickPositionBound()
    {
        var movement = new MovementProgressWatchdog(0, 0, 0, 0, 10);

        Assert.Equal(
            MovementProgressStatus.Progressing,
            movement.Observe(179, 0, 0, 0, 10).Status);
        Assert.Equal(
            MovementProgressStatus.PositionStalled,
            movement.Observe(180, 0, 0, 0, 10).Status);
    }

    private static HarvestApproachProgressWatchdog CreateWatchdog() =>
        new HarvestApproachProgressWatchdog(0, 100, 0, 0, 0, 0, 0);
}
