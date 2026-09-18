namespace Spherewright.Contracts.Actions;

/// <summary>Explicit current-Plugin echo; reused source/destination covers are not NEW objects or item costs.</summary>
public sealed class BeltPathPlanSnapshot
{
    public string NativeValidationMode { get; set; } = string.Empty;
    public string SourceBindingMode { get; set; } = string.Empty;
    public int? ReusedSourceObjectId { get; set; }
    public string? SourcePreservationMode { get; set; }
    public string DestinationBindingMode { get; set; } = "none";
    public int? ReusedDestinationObjectId { get; set; }
    public string? DestinationPreservationMode { get; set; }
    public int NewObjectCount { get; set; }
    /// <summary>Null in legacy responses; a non-default request requires an exact explicit echo.</summary>
    public string? RoutingMode { get; set; }
    public int? StartAltitudeLevel { get; set; }
    public int? EndAltitudeLevel { get; set; }
}

public static class BeltPathModes
{
    public const string NativeGrid = "native_grid";
    public const string NativeGeodesic = "native_geodesic";
    public const string NativeElevatedGrid = "native_elevated_grid";
}
