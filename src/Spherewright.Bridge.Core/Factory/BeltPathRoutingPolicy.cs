using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Actions;
using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

/// <summary>Bounds an opt-in native point generator; not a route planner or placement approval.</summary>
public static class BeltPathRoutingPolicy
{
    public const int NativeReservedPoints = 10;
    public const string Recovery = "Choose one explicit free-ground route and fresh-prepare it; do not omit existing endpoint bindings to bypass rejection. Native placement and both later sorter attachments still require proof.";

    public static string? ValidateRequest(PrepareBuildRequest request, bool nativeBelt)
    {
        if (request.BeltPathMode == BeltPathModes.NativeGrid) return null;
        if (request.BeltPathMode != BeltPathModes.NativeGeodesic) return "belt_routing_mode_unknown";
        if (!nativeBelt || request.BuildingItemId < 2001 || request.BuildingItemId > 2003
            || request.SourceObjectId.HasValue || request.DestinationObjectId.HasValue || request.ResourceNodeId.HasValue
            || !Valid(request.PreferredPosition) || !Valid(request.PathEnd))
            return "belt_geodesic_requires_explicit_free_endpoints";
        return null;
    }

    public static bool ValidGroundEndpoints(Vector3Snapshot? start, Vector3Snapshot? end, float groundRadius)
    {
        if (!Finite(groundRadius) || groundRadius <= 1 || !OnGround(start, groundRadius) || !OnGround(end, groundRadius))
            return false;
        var distance = DistanceSquared(start!, end!);
        return distance >= 1.5 * 1.5 && distance <= 30 * 30;
    }

    public static bool CompleteGroundPath(IReadOnlyList<Vector3Snapshot>? points, int nativeBufferLength,
        Vector3Snapshot start, Vector3Snapshot end, float groundRadius)
    {
        // Native SnapLineNonAlloc reserves ten points and can stop before reaching
        // the requested end. Never overwrite its last point to hide truncation.
        return ValidGroundEndpoints(start, end, groundRadius)
            && nativeBufferLength > NativeReservedPoints + 2
            && nativeBufferLength <= BeltBuildOccupancyPolicy.MaximumPathPoints
            && points is not null && points.Count >= 2 && points.Count < nativeBufferLength - NativeReservedPoints
            && points.All(point => OnGround(point, groundRadius))
            && DistanceSquared(points[0], start) <= .0001
            && DistanceSquared(points[points.Count - 1], end) <= .0001;
    }

    public static bool ConfirmsPlanEcho(string requestedMode, BeltPathPlanSnapshot? echo)
    {
        if (echo is null) return false;
        if (requestedMode == BeltPathModes.NativeGrid)
            return echo.RoutingMode is null || echo.RoutingMode == BeltPathModes.NativeGrid;
        return requestedMode == BeltPathModes.NativeGeodesic && echo.RoutingMode == requestedMode
            && echo.NativeValidationMode == "full_path_stage1" && echo.SourceBindingMode == "none"
            && !echo.ReusedSourceObjectId.HasValue && echo.SourcePreservationMode is null;
    }

    public static string BindGeometry(string mode, string geometryHash)
    {
        if (mode == BeltPathModes.NativeGrid) return geometryHash; // Preserve the old default fingerprint.
        if (mode != BeltPathModes.NativeGeodesic) throw new ArgumentException("Unknown belt routing mode.", nameof(mode));
        return CanonicalStateHash.Combine("belt-routing-v1", mode, geometryHash);
    }

    private static bool OnGround(Vector3Snapshot? p, float radius) => Valid(p)
        && Math.Abs(Math.Sqrt(DistanceSquared(p!, new Vector3Snapshot())) - radius) <= .05;
    private static bool Valid(Vector3Snapshot? p) => p is not null && Finite(p.X) && Finite(p.Y) && Finite(p.Z)
        && Math.Abs(p.X) <= 10000 && Math.Abs(p.Y) <= 10000 && Math.Abs(p.Z) <= 10000
        && DistanceSquared(p, new Vector3Snapshot()) >= 1;
    private static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
    private static double DistanceSquared(Vector3Snapshot a, Vector3Snapshot b) =>
        Math.Pow((double)a.X - b.X, 2) + Math.Pow((double)a.Y - b.Y, 2) + Math.Pow((double)a.Z - b.Z, 2);
}
