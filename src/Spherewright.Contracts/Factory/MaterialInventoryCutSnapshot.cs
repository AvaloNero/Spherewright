namespace Spherewright.Contracts.Factory;

/// <summary>An explicitly selected stock observation, not production, allocation or flow.</summary>
public sealed class MaterialInventoryCutSnapshot
{
    public string State { get; set; } = "unavailable";
    public string Coverage { get; set; } = "explicit_objects_and_complete_native_cargo_paths";
    public string? ReasonCode { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public int PlanetId { get; set; }
    public long CapturedAtGameTick { get; set; }
    public List<int> RequestedObjectIds { get; set; } = new List<int>();
    public List<FactoryEntitySnapshot> Objects { get; set; } = new List<FactoryEntitySnapshot>();
    public List<MaterialCargoPathSnapshot> CargoPaths { get; set; } = new List<MaterialCargoPathSnapshot>();
}

/// <summary>One entire identity-verified native path; never add it once per belt.</summary>
public sealed class MaterialCargoPathSnapshot
{
    public int PathId { get; set; }
    public int PathLengthCells { get; set; }
    public bool PathClosed { get; set; }
    public int OutputPathId { get; set; }
    public List<int> InputPathIds { get; set; } = new List<int>();
    public long CapturedAtGameTick { get; set; }
    // All members, including those NOT in RequestedObjectIds. No prorating.
    public List<int> BeltObjectIds { get; set; } = new List<int>();
    public int CargoStackCount { get; set; }
    public int ItemCount { get; set; }
    public List<BeltCargoItemSnapshot> Items { get; set; } = new List<BeltCargoItemSnapshot>();
}
