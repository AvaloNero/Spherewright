namespace Spherewright.Contracts.Actions;

public sealed class GetActionResultRequest
{
    public string ActionId { get; set; } = string.Empty;
}

public sealed class ActionResultSnapshot
{
    public string ActionId { get; set; } = string.Empty;

    public string ActionKind { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public bool Terminal { get; set; }

    public bool Succeeded { get; set; }

    public string? SessionId { get; set; }

    public int? PlanetId { get; set; }

    public string? Message { get; set; }

    public string? IdempotencyKey { get; set; }

    public long? StartedAtGameTick { get; set; }

    public long? CompletedAtGameTick { get; set; }

    public string? BeforeStateHash { get; set; }

    public string? AfterStateHash { get; set; }

    public int? TargetObjectId { get; set; }

    public List<int> TargetObjectIds { get; set; } = new List<int>();

    public int? TargetItemId { get; set; }

    public int? RequestedCount { get; set; }

    public int? BeforeTargetAmount { get; set; }

    public int? AfterTargetAmount { get; set; }

    public List<ActionItemDelta> ItemDeltas { get; set; } = new List<ActionItemDelta>();
}
