namespace Spherewright.Contracts.Actions;

/// <summary>Explicit current-Plugin echo; a reused source is not a NEW object or an item cost.</summary>
public sealed class BeltPathPlanSnapshot
{
    public string NativeValidationMode { get; set; } = string.Empty;
    public string SourceBindingMode { get; set; } = string.Empty;
    public int? ReusedSourceObjectId { get; set; }
    public string? SourcePreservationMode { get; set; }
    public int NewObjectCount { get; set; }
}
