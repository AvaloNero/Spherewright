namespace Spherewright.Contracts.Actions;

/// <summary>Explicit current-Plugin echo; a reused source is not a NEW object or an item cost.</summary>
public sealed class BeltPathPlanSnapshot
{
    public string NativeValidationMode { get; set; } = string.Empty;
    public string SourceBindingMode { get; set; } = string.Empty;
    public int? ReusedSourceObjectId { get; set; }
    public string? SourcePreservationMode { get; set; }
    public int NewObjectCount { get; set; }
    /// <summary>Null in legacy responses; a non-default request requires an exact explicit echo.</summary>
    public string? RoutingMode { get; set; }
}

public static class BeltPathModes
{
    public const string NativeGrid = "native_grid";
    public const string NativeGeodesic = "native_geodesic";
}
