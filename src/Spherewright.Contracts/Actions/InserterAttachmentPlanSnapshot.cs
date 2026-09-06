using Spherewright.Contracts.Factory;

namespace Spherewright.Contracts.Actions;

/// <summary>Auditable pose/offset choice, not a token or a completed connection.</summary>
public sealed class InserterAttachmentPlanSnapshot
{
    public string Mode { get; set; } = string.Empty;
    public int SourceSlot { get; set; }
    public int DestinationSlot { get; set; }
    public int InputOffset { get; set; }
    public int OutputOffset { get; set; }
    public Vector3Snapshot SourcePosition { get; set; } = new();
    public Vector3Snapshot DestinationPosition { get; set; } = new();
}
