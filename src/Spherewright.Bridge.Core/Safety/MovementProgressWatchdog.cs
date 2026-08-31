namespace Spherewright.Bridge.Core.Safety;

public enum MovementProgressStatus
{
    Progressing,
    PositionStalled,
    RouteStalled,
}

public readonly struct MovementProgressObservation
{
    public MovementProgressObservation(
        MovementProgressStatus status,
        long stalledGameTicks,
        double remainingDistance)
    {
        Status = status;
        StalledGameTicks = stalledGameTicks;
        RemainingDistance = remainingDistance;
    }

    public MovementProgressStatus Status { get; }

    public long StalledGameTicks { get; }

    public double RemainingDistance { get; }
}

public sealed class MovementProgressWatchdog
{
    public const long DefaultPositionStallTicks = 180;
    public const long DefaultRouteStallTicks = 600;
    public const double DefaultMinimumDisplacement = 0.75d;
    public const double DefaultMinimumTargetProgress = 1d;

    private readonly long _positionStallTicks;
    private readonly long _routeStallTicks;
    private readonly double _minimumDisplacementSquared;
    private readonly double _minimumTargetProgress;
    private double _checkpointX;
    private double _checkpointY;
    private double _checkpointZ;
    private long _lastDisplacementGameTick;
    private double _bestRemainingDistance;
    private long _lastTargetProgressGameTick;

    public MovementProgressWatchdog(
        long startedAtGameTick,
        double initialX,
        double initialY,
        double initialZ,
        double initialRemainingDistance,
        long positionStallTicks = DefaultPositionStallTicks,
        long routeStallTicks = DefaultRouteStallTicks,
        double minimumDisplacement = DefaultMinimumDisplacement,
        double minimumTargetProgress = DefaultMinimumTargetProgress)
    {
        if (startedAtGameTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startedAtGameTick));
        }

        if (positionStallTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(positionStallTicks));
        }

        if (routeStallTicks < positionStallTicks)
        {
            throw new ArgumentOutOfRangeException(nameof(routeStallTicks));
        }

        ValidateFinite(initialX, nameof(initialX));
        ValidateFinite(initialY, nameof(initialY));
        ValidateFinite(initialZ, nameof(initialZ));
        ValidateNonNegativeFinite(initialRemainingDistance, nameof(initialRemainingDistance));
        ValidatePositiveFinite(minimumDisplacement, nameof(minimumDisplacement));
        ValidatePositiveFinite(minimumTargetProgress, nameof(minimumTargetProgress));

        _positionStallTicks = positionStallTicks;
        _routeStallTicks = routeStallTicks;
        _minimumDisplacementSquared = minimumDisplacement * minimumDisplacement;
        _minimumTargetProgress = minimumTargetProgress;
        _checkpointX = initialX;
        _checkpointY = initialY;
        _checkpointZ = initialZ;
        _lastDisplacementGameTick = startedAtGameTick;
        _bestRemainingDistance = initialRemainingDistance;
        _lastTargetProgressGameTick = startedAtGameTick;
    }

    public MovementProgressObservation Observe(
        long gameTick,
        double x,
        double y,
        double z,
        double remainingDistance)
    {
        if (gameTick < _lastDisplacementGameTick || gameTick < _lastTargetProgressGameTick)
        {
            throw new ArgumentOutOfRangeException(nameof(gameTick));
        }

        ValidateFinite(x, nameof(x));
        ValidateFinite(y, nameof(y));
        ValidateFinite(z, nameof(z));
        ValidateNonNegativeFinite(remainingDistance, nameof(remainingDistance));

        var deltaX = x - _checkpointX;
        var deltaY = y - _checkpointY;
        var deltaZ = z - _checkpointZ;
        var displacementSquared = deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ;
        if (displacementSquared >= _minimumDisplacementSquared)
        {
            _checkpointX = x;
            _checkpointY = y;
            _checkpointZ = z;
            _lastDisplacementGameTick = gameTick;
        }

        if (remainingDistance <= _bestRemainingDistance - _minimumTargetProgress)
        {
            _bestRemainingDistance = remainingDistance;
            _lastTargetProgressGameTick = gameTick;
        }

        var positionStallDuration = gameTick - _lastDisplacementGameTick;
        if (positionStallDuration >= _positionStallTicks)
        {
            return new MovementProgressObservation(
                MovementProgressStatus.PositionStalled,
                positionStallDuration,
                remainingDistance);
        }

        var routeStallDuration = gameTick - _lastTargetProgressGameTick;
        return routeStallDuration >= _routeStallTicks
            ? new MovementProgressObservation(
                MovementProgressStatus.RouteStalled,
                routeStallDuration,
                remainingDistance)
            : new MovementProgressObservation(
                MovementProgressStatus.Progressing,
                0,
                remainingDistance);
    }

    private static void ValidateFinite(double value, string parameterName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void ValidateNonNegativeFinite(double value, string parameterName)
    {
        ValidateFinite(value, parameterName);
        if (value < 0d)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void ValidatePositiveFinite(double value, string parameterName)
    {
        ValidateFinite(value, parameterName);
        if (value <= 0d)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
