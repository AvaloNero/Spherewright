namespace Spherewright.Bridge.Core.Safety;

/// <summary>
/// Watches only the approach portion of a normal harvest order. Once DSP reaches
/// the bound approach point or harvesting has made observable progress, mining may
/// legitimately remain stationary and this watchdog is permanently retired.
/// </summary>
public sealed class HarvestApproachProgressWatchdog
{
    private readonly double _approachX;
    private readonly double _approachY;
    private readonly double _approachZ;
    private readonly MovementProgressWatchdog _movement;

    public HarvestApproachProgressWatchdog(
        long startedAtGameTick,
        double initialX,
        double initialY,
        double initialZ,
        double approachX,
        double approachY,
        double approachZ)
    {
        _approachX = approachX;
        _approachY = approachY;
        _approachZ = approachZ;
        _movement = new MovementProgressWatchdog(
            startedAtGameTick,
            initialX,
            initialY,
            initialZ,
            RemainingDistance(initialX, initialY, initialZ));
    }

    public bool IsApproachMonitoringComplete { get; private set; }

    public MovementProgressObservation Observe(
        long gameTick,
        double x,
        double y,
        double z,
        bool targetReached,
        bool hasObservedYield,
        bool hasObservedNodeReduction)
    {
        var remainingDistance = RemainingDistance(x, y, z);
        if (targetReached || hasObservedYield || hasObservedNodeReduction)
        {
            IsApproachMonitoringComplete = true;
        }

        return IsApproachMonitoringComplete
            ? new MovementProgressObservation(MovementProgressStatus.Progressing, 0, remainingDistance)
            : _movement.Observe(gameTick, x, y, z, remainingDistance);
    }

    public void ResetWindow(long gameTick, double x, double y, double z)
    {
        if (IsApproachMonitoringComplete)
        {
            return;
        }

        _movement.ResetWindow(gameTick, x, y, z, RemainingDistance(x, y, z));
    }

    private double RemainingDistance(double x, double y, double z)
    {
        var deltaX = x - _approachX;
        var deltaY = y - _approachY;
        var deltaZ = z - _approachZ;
        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
    }
}
