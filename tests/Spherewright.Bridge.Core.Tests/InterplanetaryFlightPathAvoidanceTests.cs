using Spherewright.Bridge.Core.Safety;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class InterplanetaryFlightPathAvoidanceTests
{
    [Fact]
    public void TryCreateDetour_ObservedBlockedMoonGeometry_AvoidsGasGiant()
    {
        var origin = new FlightPathPoint(0d, 0d, 0d);
        var destination = new FlightPathPoint(49236d, 3507d, -49947d);
        var gasGiant = new FlightPathPoint(1731d, 25d, -1954d);

        var created = InterplanetaryFlightPathAvoidance.TryCreateDetour(
            origin,
            destination,
            obstacleBodyId: 103,
            gasGiant,
            obstacleRadius: 800d,
            out var detour);

        Assert.True(created);
        Assert.Equal(103, detour.ObstacleBodyId);
        Assert.True(detour.DirectClearance < detour.RequiredClearance);
        Assert.True(detour.RequiredClearance >= 1800d);
        Assert.True(detour.DetourRadius >= detour.RequiredClearance * 1.5d);
        Assert.True(DistanceToSegment(gasGiant, origin, detour.AimPoint) >= detour.RequiredClearance * 1.05d);
        Assert.True(DistanceToSegment(gasGiant, detour.AimPoint, destination) >= detour.RequiredClearance * 1.05d);
        Assert.All(
            new[] { detour.AimPoint.X, detour.AimPoint.Y, detour.AimPoint.Z },
            value => Assert.True(double.IsFinite(value)));
    }

    [Fact]
    public void TryCreateDetour_ClearRoute_DoesNotInventWaypoint()
    {
        var created = InterplanetaryFlightPathAvoidance.TryCreateDetour(
            new FlightPathPoint(0d, 0d, 0d),
            new FlightPathPoint(10000d, 0d, 0d),
            obstacleBodyId: 7,
            new FlightPathPoint(5000d, 5000d, 0d),
            obstacleRadius: 200d,
            out _);

        Assert.False(created);
    }

    [Fact]
    public void TryCreateDetour_CenteredObstacle_UsesStableFiniteSide()
    {
        var created = InterplanetaryFlightPathAvoidance.TryCreateDetour(
            new FlightPathPoint(0d, 0d, 0d),
            new FlightPathPoint(10000d, 0d, 0d),
            obstacleBodyId: 7,
            new FlightPathPoint(5000d, 0d, 0d),
            obstacleRadius: 800d,
            out var detour);

        Assert.True(created);
        Assert.Equal(5000d, detour.AimPoint.X, precision: 6);
        Assert.NotEqual(0d, Math.Abs(detour.AimPoint.Y) + Math.Abs(detour.AimPoint.Z));
    }

    [Fact]
    public void IsPreferred_SelectsFirstObstacleAlongRouteThenStableId()
    {
        var farther = new FlightPathDetour(
            8,
            new FlightPathPoint(0d, 1d, 0d),
            alongRouteDistance: 200d,
            directClearance: 0d,
            requiredClearance: 100d,
            detourRadius: 150d);
        var nearer = new FlightPathDetour(
            9,
            new FlightPathPoint(0d, 1d, 0d),
            alongRouteDistance: 100d,
            directClearance: 0d,
            requiredClearance: 100d,
            detourRadius: 150d);
        var tiedLowerId = new FlightPathDetour(
            7,
            new FlightPathPoint(0d, 1d, 0d),
            alongRouteDistance: 200d,
            directClearance: 0d,
            requiredClearance: 100d,
            detourRadius: 150d);

        Assert.True(InterplanetaryFlightPathAvoidance.IsPreferred(nearer, farther));
        Assert.True(InterplanetaryFlightPathAvoidance.IsPreferred(tiedLowerId, farther));
        Assert.False(InterplanetaryFlightPathAvoidance.IsPreferred(farther, nearer));
    }

    private static double DistanceToSegment(
        FlightPathPoint point,
        FlightPathPoint start,
        FlightPathPoint end)
    {
        var segmentX = end.X - start.X;
        var segmentY = end.Y - start.Y;
        var segmentZ = end.Z - start.Z;
        var lengthSquared = segmentX * segmentX + segmentY * segmentY + segmentZ * segmentZ;
        var fraction = ((point.X - start.X) * segmentX
                        + (point.Y - start.Y) * segmentY
                        + (point.Z - start.Z) * segmentZ) / lengthSquared;
        fraction = Math.Max(0d, Math.Min(1d, fraction));
        var closestX = start.X + segmentX * fraction;
        var closestY = start.Y + segmentY * fraction;
        var closestZ = start.Z + segmentZ * fraction;
        var deltaX = point.X - closestX;
        var deltaY = point.Y - closestY;
        var deltaZ = point.Z - closestZ;
        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
    }
}
