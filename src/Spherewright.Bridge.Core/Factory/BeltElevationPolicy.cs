using Spherewright.Contracts.Actions;
using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

/// <summary>Guards one native free-to-free span, never generates a route or grants placement permission.</summary>
public static class BeltElevationPolicy
{
    public const float NativeLayerHeight = 1.3333333f;
    public const float MaximumNativeSlope = .5f;
    public const int MaximumAltitudeLevel = 3;
    public const int MaximumPoints = 64;
    public const string Recovery = "Choose one explicit free-to-free 2001 native_elevated_grid span with start/end levels 0..3 (at least one raised), at most 30 m, flat ends and a native gentle slope. Do not concatenate routes, drop points, omit endpoint bindings or replay rejected coordinates. Complete native placement, material and connection checks remain mandatory.";
    private const double RadiusTolerance = .01;

    public static string? ValidateRequest(PrepareBuildRequest request, bool nativeBelt)
    {
        if (request.BeltPathMode != BeltPathModes.NativeElevatedGrid)
            return request.BeltStartAltitudeLevel.HasValue || request.BeltEndAltitudeLevel.HasValue
                ? "belt_elevation_requires_explicit_mode" : null;
        if (!nativeBelt || request.BuildingItemId != 2001 || request.SourceObjectId.HasValue
            || request.DestinationObjectId.HasValue || request.ResourceNodeId.HasValue
            || !string.IsNullOrEmpty(request.ExpectedSourceStateHash)
            || !string.IsNullOrEmpty(request.ExpectedDestinationStateHash)
            || !string.IsNullOrEmpty(request.ExpectedResourceStateHash)
            || !Valid(request.PreferredPosition) || !Valid(request.PathEnd))
            return "belt_elevation_requires_explicit_free_endpoints";
        return ValidLevels(request.BeltStartAltitudeLevel, request.BeltEndAltitudeLevel)
            ? null : "belt_elevation_levels_unsupported";
    }

    public static bool ValidLevels(int? start, int? end) => start.HasValue && end.HasValue
        && start.Value >= 0 && start.Value <= MaximumAltitudeLevel
        && end.Value >= 0 && end.Value <= MaximumAltitudeLevel && (start.Value > 0 || end.Value > 0);

    public static bool TryGetLevel(Vector3Snapshot? point, float groundRadius, out int level)
    {
        level = -1;
        if (!Valid(point) || !Finite(groundRadius) || groundRadius <= 1 || groundRadius > 10000) return false;
        var raw = (Radius(point!) - groundRadius) / NativeLayerHeight;
        if (raw < -.01 || raw > MaximumAltitudeLevel + .01) return false;
        var rounded = (int)Math.Round(raw);
        if (rounded < 0 || rounded > MaximumAltitudeLevel
            || Math.Abs(Radius(point!) - (groundRadius + rounded * (double)NativeLayerHeight)) > RadiusTolerance)
            return false;
        level = rounded;
        return true;
    }

    public static bool ValidEndpoints(Vector3Snapshot? start, Vector3Snapshot? end, float groundRadius) =>
        TryGetLevel(start, groundRadius, out var a) && TryGetLevel(end, groundRadius, out var b)
        && ValidLevels(a, b) && DistanceSquared(start!, end!) >= 1.5 * 1.5
        && DistanceSquared(start!, end!) <= 30 * 30;

    public static bool ValidNativeSlope(float value) => Finite(value) && value >= 0 && value <= MaximumNativeSlope;

    public static bool CompleteNativePath(IReadOnlyList<Vector3Snapshot>? points, int nativeBufferLength,
        Vector3Snapshot start, Vector3Snapshot end, float groundRadius)
    {
        if (!ValidEndpoints(start, end, groundRadius) || points is null || points.Count < 4
            || points.Count > MaximumPoints || nativeBufferLength > BeltBuildOccupancyPolicy.MaximumPathPoints
            || nativeBufferLength <= BeltPathRoutingPolicy.NativeReservedPoints + 4
            || points.Count >= nativeBufferLength - BeltPathRoutingPolicy.NativeReservedPoints
            || points.Any(p => !Valid(p)) || DistanceSquared(points[0], start) > .0001
            || DistanceSquared(points[points.Count - 1], end) > .0001) return false;
        var first = Radius(start); var last = Radius(end); var direction = Math.Sign(last - first);
        var minimum = Math.Min(first, last) - RadiusTolerance;
        var maximum = Math.Max(first, last) + RadiusTolerance;
        for (var i = 0; i < points.Count; i++)
        {
            var radius = Radius(points[i]);
            if (radius < minimum || radius > maximum) return false;
            if (i == 0) continue;
            var delta = radius - Radius(points[i - 1]);
            var distance = DistanceSquared(points[i - 1], points[i]);
            // At least four points are required above, so only native's <.28f
            // branch applies. Preserve its float operation order at the boundary;
            // the two-point <.5f rule must not reject otherwise legal grid points.
            if (NativeDistanceSquared(points[i - 1], points[i]) < .28f || distance > 4
                || Math.Abs(delta) > NativeLayerHeight / 2 + RadiusTolerance
                || delta * direction < -RadiusTolerance) return false;
        }
        return Math.Abs(Radius(points[1]) - first) <= RadiusTolerance
            && Math.Abs(Radius(points[points.Count - 2]) - last) <= RadiusTolerance;
    }

    public static bool ConfirmsPlanEcho(int? requestedStart, int? requestedEnd, BeltPathPlanSnapshot? echo) =>
        ValidLevels(requestedStart, requestedEnd) && echo is not null
        && echo.RoutingMode == BeltPathModes.NativeElevatedGrid && echo.NativeValidationMode == "full_path_stage1"
        && echo.StartAltitudeLevel == requestedStart && echo.EndAltitudeLevel == requestedEnd
        && echo.NewObjectCount >= 4 && echo.NewObjectCount <= MaximumPoints
        && echo.SourceBindingMode == "none" && echo.DestinationBindingMode == "none"
        && !echo.ReusedSourceObjectId.HasValue && !echo.ReusedDestinationObjectId.HasValue
        && echo.SourcePreservationMode is null && echo.DestinationPreservationMode is null;

    private static bool Valid(Vector3Snapshot? p) => p is not null && Finite(p.X) && Finite(p.Y) && Finite(p.Z)
        && Math.Abs(p.X) <= 10000 && Math.Abs(p.Y) <= 10000 && Math.Abs(p.Z) <= 10000
        && DistanceSquared(p, new Vector3Snapshot()) >= 1;
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static double Radius(Vector3Snapshot point) => Math.Sqrt(DistanceSquared(point, new Vector3Snapshot()));
    private static float NativeDistanceSquared(Vector3Snapshot a, Vector3Snapshot b)
    {
        var x = b.X - a.X; var y = b.Y - a.Y; var z = b.Z - a.Z;
        return x * x + y * y + z * z;
    }
    private static double DistanceSquared(Vector3Snapshot a, Vector3Snapshot b) =>
        Math.Pow((double)a.X - b.X, 2) + Math.Pow((double)a.Y - b.Y, 2) + Math.Pow((double)a.Z - b.Z, 2);
}
