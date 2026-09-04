using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Actions;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class MovementProgressWatchdogTests
{
    [Fact]
    public void Observe_FlagsPositionStallAtBound()
    {
        var watchdog = CreateWatchdog(positionStallTicks: 10, routeStallTicks: 30);

        Assert.Equal(MovementProgressStatus.Progressing, watchdog.Observe(9, 0.2, 0, 0, 99.8).Status);

        var observation = watchdog.Observe(10, 0.2, 0, 0, 99.8);

        Assert.Equal(MovementProgressStatus.PositionStalled, observation.Status);
        Assert.Equal(10, observation.StalledGameTicks);
    }

    [Fact]
    public void Observe_DefaultWindowFlagsPositionStallAt180Ticks()
    {
        var watchdog = new MovementProgressWatchdog(0, 0, 0, 0, 100);

        Assert.Equal(
            MovementProgressStatus.Progressing,
            watchdog.Observe(179, 0.2, 0, 0, 99.8).Status);

        var observation = watchdog.Observe(180, 0.2, 0, 0, 99.8);

        Assert.Equal(MovementProgressStatus.PositionStalled, observation.Status);
        Assert.Equal(180, observation.StalledGameTicks);
        Assert.Equal(99.8, observation.RemainingDistance);
    }

    [Fact]
    public void Observe_MeaningfulDisplacementResetsPositionWindow()
    {
        var watchdog = CreateWatchdog(positionStallTicks: 10, routeStallTicks: 30);

        Assert.Equal(MovementProgressStatus.Progressing, watchdog.Observe(8, 0.5, 0, 0, 99.5).Status);
        Assert.Equal(MovementProgressStatus.Progressing, watchdog.Observe(17, 0.5, 0, 0, 99.5).Status);

        Assert.Equal(MovementProgressStatus.PositionStalled, watchdog.Observe(18, 0.5, 0, 0, 99.5).Status);
    }

    [Fact]
    public void Observe_FlagsRouteStallWhilePlayerKeepsMovingSideways()
    {
        var watchdog = CreateWatchdog(positionStallTicks: 10, routeStallTicks: 30);

        for (var tick = 5; tick < 30; tick += 5)
        {
            Assert.Equal(
                MovementProgressStatus.Progressing,
                watchdog.Observe(tick, tick / 5d, 0, 0, 100).Status);
        }

        var observation = watchdog.Observe(30, 6, 0, 0, 100);

        Assert.Equal(MovementProgressStatus.RouteStalled, observation.Status);
        Assert.Equal(30, observation.StalledGameTicks);
    }

    [Fact]
    public void Observe_DefaultWindowFlagsRouteStallAt600TicksWhilePositionChanges()
    {
        var watchdog = new MovementProgressWatchdog(0, 0, 0, 0, 100);

        for (var tick = 100; tick < 600; tick += 100)
        {
            Assert.Equal(
                MovementProgressStatus.Progressing,
                watchdog.Observe(tick, tick / 100d, 0, 0, 100).Status);
        }

        var observation = watchdog.Observe(600, 6, 0, 0, 100);

        Assert.Equal(MovementProgressStatus.RouteStalled, observation.Status);
        Assert.Equal(600, observation.StalledGameTicks);
        Assert.Equal(100, observation.RemainingDistance);
    }

    [Theory]
    [InlineData(MovementProgressStatus.PositionStalled, MovementFailureKinds.PositionStalled)]
    [InlineData(MovementProgressStatus.RouteStalled, MovementFailureKinds.RouteStalled)]
    public void RecoveryAdvisor_ReturnsStructuredBoundedGuidance(
        MovementProgressStatus status,
        string expectedFailureKind)
    {
        var advice = MovementFailureRecoveryAdvisor.ForStall(
            new MovementProgressObservation(status, 180, 12.5));

        Assert.Equal(expectedFailureKind, advice.FailureKind);
        Assert.Equal(180, advice.StalledGameTicks);
        Assert.Equal(12.5, advice.RemainingDistance);
        Assert.True(advice.DoNotRetrySameTarget);
        Assert.Equal(5, advice.RecommendedShortMoveDistanceMeters);
        Assert.Equal(4, advice.OrthogonalProbeDistanceMeters);
        Assert.Equal(4, advice.MaximumOrthogonalProbeAttempts);
        Assert.Contains("each direction once", advice.RecommendedRecovery, StringComparison.Ordinal);
        Assert.Contains("Poll every returned actionId to terminal", advice.RecommendedRecovery, StringComparison.Ordinal);
    }

    [Fact]
    public void Observe_TargetProgressResetsRouteWindow()
    {
        var watchdog = CreateWatchdog(positionStallTicks: 10, routeStallTicks: 30);

        Assert.Equal(MovementProgressStatus.Progressing, watchdog.Observe(25, 1, 0, 0, 98.9).Status);
        Assert.Equal(MovementProgressStatus.Progressing, watchdog.Observe(54, 4, 0, 0, 98.9).Status);

        Assert.Equal(MovementProgressStatus.RouteStalled, watchdog.Observe(55, 5, 0, 0, 98.9).Status);
    }

    [Fact]
    public void ResetWindow_DiscardsPausedTicksBeforeEnergyRecovery()
    {
        var watchdog = CreateWatchdog(positionStallTicks: 10, routeStallTicks: 30);

        watchdog.ResetWindow(300, 0.2, 0, 0, 99.8);

        Assert.Equal(
            MovementProgressStatus.Progressing,
            watchdog.Observe(309, 0.2, 0, 0, 99.8).Status);
        Assert.Equal(
            MovementProgressStatus.PositionStalled,
            watchdog.Observe(310, 0.2, 0, 0, 99.8).Status);
    }

    [Fact]
    public void Constructor_RejectsRouteWindowShorterThanPositionWindow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateWatchdog(positionStallTicks: 30, routeStallTicks: 10));
    }

    private static MovementProgressWatchdog CreateWatchdog(long positionStallTicks, long routeStallTicks) =>
        new MovementProgressWatchdog(
            0,
            0,
            0,
            0,
            100,
            positionStallTicks,
            routeStallTicks,
            minimumDisplacement: 0.5,
            minimumTargetProgress: 1);
}
