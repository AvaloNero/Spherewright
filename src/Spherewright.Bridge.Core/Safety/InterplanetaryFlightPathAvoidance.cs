namespace Spherewright.Bridge.Core.Safety;

public readonly struct FlightPathPoint
{
    public FlightPathPoint(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public double X { get; }

    public double Y { get; }

    public double Z { get; }
}

public readonly struct FlightPathDetour
{
    public FlightPathDetour(
        int obstacleBodyId,
        FlightPathPoint aimPoint,
        double alongRouteDistance,
        double directClearance,
        double requiredClearance,
        double detourRadius)
    {
        ObstacleBodyId = obstacleBodyId;
        AimPoint = aimPoint;
        AlongRouteDistance = alongRouteDistance;
        DirectClearance = directClearance;
        RequiredClearance = requiredClearance;
        DetourRadius = detourRadius;
    }

    public int ObstacleBodyId { get; }

    public FlightPathPoint AimPoint { get; }

    public double AlongRouteDistance { get; }

    public double DirectClearance { get; }

    public double RequiredClearance { get; }

    public double DetourRadius { get; }
}

public static class InterplanetaryFlightPathAvoidance
{
    // Current GameData.GetNearestStarPlanet adopts a non-local planet when
    // its surface distance is below 1000 m. Keep the planned centerline
    // outside that native capture envelope before adding route margin.
    public const double MinimumSafetyMargin = 1000d;
    public const double RelativeSafetyMargin = 0.75d;
    public const double DetourRadiusFactor = 1.5d;

    private const double MinimumRouteLength = 1d;
    private const double MinimumVectorLengthSquared = 1e-12d;
    private const double SegmentSafetyFactor = 1.05d;
    private const int MaximumDetourExpansionAttempts = 8;

    public static bool TryCreateDetour(
        FlightPathPoint currentPosition,
        FlightPathPoint destinationPosition,
        int obstacleBodyId,
        FlightPathPoint obstacleCenter,
        double obstacleRadius,
        out FlightPathDetour detour)
    {
        ValidateFinite(currentPosition, nameof(currentPosition));
        ValidateFinite(destinationPosition, nameof(destinationPosition));
        detour = default;
        if (obstacleBodyId <= 0
            || !IsFinite(obstacleRadius)
            || obstacleRadius <= 0d
            || !IsFinite(obstacleCenter))
        {
            return false;
        }

        var route = Subtract(destinationPosition, currentPosition);
        var routeLengthSquared = LengthSquared(route);
        if (routeLengthSquared < MinimumRouteLength * MinimumRouteLength)
        {
            return false;
        }

        var fromCurrentToObstacle = Subtract(obstacleCenter, currentPosition);
        var routeFraction = Dot(fromCurrentToObstacle, route) / routeLengthSquared;
        if (routeFraction <= 0d || routeFraction >= 1d)
        {
            return false;
        }

        var closestPoint = Add(currentPosition, Scale(route, routeFraction));
        var directOffset = Subtract(closestPoint, obstacleCenter);
        var directClearance = Length(directOffset);
        var requiredClearance = obstacleRadius
                                + Math.Max(MinimumSafetyMargin, obstacleRadius * RelativeSafetyMargin);
        if (directClearance >= requiredClearance)
        {
            return false;
        }

        var routeLength = Math.Sqrt(routeLengthSquared);
        var routeDirection = Scale(route, 1d / routeLength);
        var sideDirection = TryNormalize(directOffset, out var normalizedOffset)
            ? normalizedOffset
            : SelectStablePerpendicular(routeDirection);
        var detourRadius = requiredClearance * DetourRadiusFactor;
        var requiredSegmentClearance = requiredClearance * SegmentSafetyFactor;
        var aimPoint = Add(obstacleCenter, Scale(sideDirection, detourRadius));
        for (var attempt = 0; attempt < MaximumDetourExpansionAttempts; attempt++)
        {
            var inboundClearance = DistanceToSegment(obstacleCenter, currentPosition, aimPoint);
            var outboundClearance = DistanceToSegment(obstacleCenter, aimPoint, destinationPosition);
            if (inboundClearance >= requiredSegmentClearance
                && outboundClearance >= requiredSegmentClearance)
            {
                detour = new FlightPathDetour(
                    obstacleBodyId,
                    aimPoint,
                    routeFraction * routeLength,
                    directClearance,
                    requiredClearance,
                    detourRadius);
                return true;
            }

            detourRadius *= 1.25d;
            aimPoint = Add(obstacleCenter, Scale(sideDirection, detourRadius));
        }

        return false;
    }

    public static bool IsPreferred(FlightPathDetour candidate, FlightPathDetour? current)
    {
        if (!current.HasValue)
        {
            return true;
        }

        var distanceComparison = candidate.AlongRouteDistance.CompareTo(current.Value.AlongRouteDistance);
        return distanceComparison != 0
            ? distanceComparison < 0
            : candidate.ObstacleBodyId < current.Value.ObstacleBodyId;
    }

    private static FlightPathPoint SelectStablePerpendicular(FlightPathPoint direction)
    {
        var axis = Math.Abs(direction.X) <= Math.Abs(direction.Y)
                   && Math.Abs(direction.X) <= Math.Abs(direction.Z)
            ? new FlightPathPoint(1d, 0d, 0d)
            : Math.Abs(direction.Y) <= Math.Abs(direction.Z)
                ? new FlightPathPoint(0d, 1d, 0d)
                : new FlightPathPoint(0d, 0d, 1d);
        var perpendicular = Cross(direction, axis);
        return TryNormalize(perpendicular, out var normalized)
            ? normalized
            : new FlightPathPoint(0d, 1d, 0d);
    }

    private static double DistanceToSegment(
        FlightPathPoint point,
        FlightPathPoint segmentStart,
        FlightPathPoint segmentEnd)
    {
        var segment = Subtract(segmentEnd, segmentStart);
        var lengthSquared = LengthSquared(segment);
        if (lengthSquared < MinimumVectorLengthSquared)
        {
            return Length(Subtract(point, segmentStart));
        }

        var fraction = Dot(Subtract(point, segmentStart), segment) / lengthSquared;
        fraction = Math.Max(0d, Math.Min(1d, fraction));
        var closest = Add(segmentStart, Scale(segment, fraction));
        return Length(Subtract(point, closest));
    }

    private static bool TryNormalize(FlightPathPoint value, out FlightPathPoint normalized)
    {
        var lengthSquared = LengthSquared(value);
        if (lengthSquared < MinimumVectorLengthSquared)
        {
            normalized = default;
            return false;
        }

        normalized = Scale(value, 1d / Math.Sqrt(lengthSquared));
        return true;
    }

    private static FlightPathPoint Add(FlightPathPoint left, FlightPathPoint right) =>
        new FlightPathPoint(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    private static FlightPathPoint Subtract(FlightPathPoint left, FlightPathPoint right) =>
        new FlightPathPoint(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    private static FlightPathPoint Scale(FlightPathPoint value, double scale) =>
        new FlightPathPoint(value.X * scale, value.Y * scale, value.Z * scale);

    private static FlightPathPoint Cross(FlightPathPoint left, FlightPathPoint right) =>
        new FlightPathPoint(
            left.Y * right.Z - left.Z * right.Y,
            left.Z * right.X - left.X * right.Z,
            left.X * right.Y - left.Y * right.X);

    private static double Dot(FlightPathPoint left, FlightPathPoint right) =>
        left.X * right.X + left.Y * right.Y + left.Z * right.Z;

    private static double Length(FlightPathPoint value) => Math.Sqrt(LengthSquared(value));

    private static double LengthSquared(FlightPathPoint value) => Dot(value, value);

    private static void ValidateFinite(FlightPathPoint value, string parameterName)
    {
        if (!IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static bool IsFinite(FlightPathPoint value) =>
        IsFinite(value.X) && IsFinite(value.Y) && IsFinite(value.Z);

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
