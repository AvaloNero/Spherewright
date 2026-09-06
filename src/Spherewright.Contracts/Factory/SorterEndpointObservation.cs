namespace Spherewright.Contracts.Factory;

// Detail-only native poses, not a placement approval or a new state-hash domain.
public sealed class SorterEndpointObservation
{
    public string State { get; set; } = "unavailable";
    public string? ReasonCode { get; set; }
    public string Kind { get; set; } = string.Empty;
    public long CapturedAtGameTick { get; set; }
    public List<SorterEndpointSnapshot> Endpoints { get; set; } = new List<SorterEndpointSnapshot>();
}

public sealed class SorterEndpointSnapshot
{
    public int Index { get; set; }
    // -1 is a belt's virtual attachment pose, never a claim that a real slot is free.
    public int Slot { get; set; }
    public Vector3Snapshot Position { get; set; } = new Vector3Snapshot();
    public Vector3Snapshot Outward { get; set; } = new Vector3Snapshot();
    public bool? Occupied { get; set; }
    public int? OtherObjectId { get; set; }
    public int? OtherSlot { get; set; }
}
