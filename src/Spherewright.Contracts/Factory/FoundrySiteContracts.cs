namespace Spherewright.Contracts.Factory;

public sealed class FoundrySiteRequest
{
    public Vector3Snapshot Origin { get; set; } = new Vector3Snapshot();
    public float YawDegrees { get; set; }
    public int Columns { get; set; } = 4;
    public float ColumnSpacing { get; set; } = 12f;
    public float RowSpacing { get; set; } = 12f;
}

// A one-tick machine-placement assessment, never a write token or a complete
// construction plan. Logistics, electricity and ongoing supply are separate.
public sealed class FoundrySiteSnapshot
{
    public int SchemaVersion { get; set; } = 1;
    public string SessionId { get; set; } = string.Empty;
    public int PlanetId { get; set; }
    public long Revision { get; set; }
    public long CapturedAtGameTick { get; set; }
    public string MaterialPlanHash { get; set; } = string.Empty;
    public string AssessmentHash { get; set; } = string.Empty;
    public string Status { get; set; } = "not_checked";
    public bool MachinePreviewsClear { get; set; }
    public bool MachineInventorySufficient { get; set; }
    public Vector3Snapshot Origin { get; set; } = new Vector3Snapshot();
    public float YawDegrees { get; set; }
    public int Columns { get; set; }
    public float ColumnSpacing { get; set; }
    public float RowSpacing { get; set; }
    public string ClearanceBasis { get; set; } = "build_collider_bounding_spheres_plus_0.5m_v1";
    public List<FoundrySiteMachine> Machines { get; set; } = new List<FoundrySiteMachine>();
    public List<FoundryInventoryBudget> MachineInventory { get; set; } = new List<FoundryInventoryBudget>();
}

public sealed class FoundrySiteMachine
{
    public string PlacementId { get; set; } = string.Empty;
    public string StageId { get; set; } = string.Empty;
    public int MachineIndex { get; set; }
    public int BuildingItemId { get; set; }
    public int RecipeId { get; set; }
    public Vector3Snapshot Position { get; set; } = new Vector3Snapshot();
    public float YawDegrees { get; set; }
    public float PlacementRadius { get; set; }
    public bool NativeCheckPerformed { get; set; }
    public bool NativeCheckPassed { get; set; }
    public string? NativeBuildCondition { get; set; }
    public int? OccupiedObjectId { get; set; }
    public string? Rejection { get; set; }
    public List<string> OverlappingPlacementIds { get; set; } = new List<string>();
}

public sealed class FoundryInventoryBudget
{
    public int ItemId { get; set; }
    public int RequiredCount { get; set; }
    public int PackageCount { get; set; }
    public int MissingCount { get; set; }
}
