namespace Spherewright.Contracts.Factory;

/// <summary>A bounded single-tick inventory observation, not a flow or upgrade proof.</summary>
public sealed class BeltCargoSnapshot
{
    public string State { get; set; } = "unavailable";

    public string Coverage { get; set; } = "unique_stacks_touching_selected_belt_segment";

    public string? ReasonCode { get; set; }

    public long CapturedAtGameTick { get; set; }

    public int? PathId { get; set; }

    public int? PathLengthCells { get; set; }

    public int? SegmentStartCell { get; set; }

    public int? SegmentLengthCells { get; set; }

    public bool? PathClosed { get; set; }

    // Null when unavailable. Adjacent belt observations may include the SAME stack.
    public int? CargoStackCount { get; set; }

    public int? ItemCount { get; set; }

    public List<BeltCargoItemSnapshot> Items { get; set; } = new List<BeltCargoItemSnapshot>();

    // Optional, detail-only: a legacy reader leaves this null, not observed empty.
    public BeltRearPickupSnapshot? RearPickup { get; set; }
}

/// <summary>The open path's aligned native pickup packet, not queue/flow or station demand.</summary>
public sealed class BeltRearPickupSnapshot
{
    // observed | no_aligned_packet | not_applicable | unavailable
    public string State { get; set; } = "unavailable";
    public string Coverage { get; set; } = "open_path_rear_pickup_aligned_packet";
    public string? ReasonCode { get; set; }
    public long CapturedAtGameTick { get; set; }
    public int? MarkerPathCell { get; set; }
    public int? ItemId { get; set; }
    public string? Name { get; set; }
    public int? Count { get; set; }
    public int? Inc { get; set; }
}

public sealed class BeltCargoItemSnapshot
{
    public int ItemId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int CargoStackCount { get; set; }

    public int Count { get; set; }

    public int Inc { get; set; }
}
