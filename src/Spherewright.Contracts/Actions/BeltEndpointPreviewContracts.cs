using Spherewright.Contracts.Factory;

namespace Spherewright.Contracts.Actions;

// Qualification data, not a build token or permission to execute a route.
public sealed class BeltEndpointPreviewRequest
{
    public int SorterItemId { get; set; } = 2011;
    public int FilterItemId { get; set; }
    public PlannedBeltEndpointBinding? Source { get; set; }
    public PlannedBeltEndpointBinding? Destination { get; set; }
}

public sealed class PlannedBeltEndpointBinding
{
    public int ExistingObjectId { get; set; }
    public string ExpectedEndpointStateHash { get; set; } = string.Empty;
    public int ExistingSlot { get; set; }
    // For an existing belt only: its virtual slot is -1, with one explicit orientation.
    public int ExistingBeltQuarterTurns { get; set; }
    // Outward slot orientation of the NEW belt pose, before the inserter's
    // destination-end 180-degree transform. Source role faces the existing
    // source; destination role faces the existing destination.
    public int PlannedBeltQuarterTurns { get; set; }
}

public sealed class BeltEndpointPreviewSnapshot
{
    public string Phase { get; set; } = "read_only_belt_endpoint_preview";
    public bool Executable => false;
    public string SessionId { get; set; } = string.Empty;
    public int PlanetId { get; set; }
    public long Revision { get; set; }
    public long CapturedAtGameTick { get; set; }
    public bool NativeCheckPerformed { get; set; }
    public bool NativeCheckPassed { get; set; }
    public string AssessmentHash { get; set; } = string.Empty;
    public List<PlannedBeltEndpointAttachment> Attachments { get; set; } = new();
    public List<string> Blockers { get; set; } = new();
}

public sealed class PlannedBeltEndpointAttachment
{
    public string Role { get; set; } = string.Empty;
    public int ExistingObjectId { get; set; }
    public string EndpointStateHash { get; set; } = string.Empty;
    public int ExistingItemId { get; set; }
    public int ObservedRecipeId { get; set; }
    public int PlannedBeltIndex { get; set; }
    public int SorterItemId { get; set; }
    public int FilterItemId { get; set; }
    public int ExistingBeltQuarterTurns { get; set; }
    public int PlannedBeltQuarterTurns { get; set; }
    public string NativeCondition { get; set; } = "not_checked";
    public int NativeSpan { get; set; }
    public InserterAttachmentPlanSnapshot Attachment { get; set; } = new();
}
