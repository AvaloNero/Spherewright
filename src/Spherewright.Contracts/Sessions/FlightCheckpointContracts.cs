using Spherewright.Contracts.Actions;

namespace Spherewright.Contracts.Sessions;

public sealed class PrepareFlightCheckpointReloadRequest
{
    public string ReloadToken { get; set; } = string.Empty;
}

public sealed class CommitFlightCheckpointReloadRequest
{
    public string PlanToken { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class PreparedFlightCheckpointReloadPlan
{
    public bool Prepared { get; set; }

    public string PlanToken { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public string CheckpointId { get; set; } = string.Empty;

    public int OriginPlanetId { get; set; }

    public int DestinationPlanetId { get; set; }

    public long SavedGameTick { get; set; }

    public bool CommitAllowedNow { get; set; }

    public List<WriteBlocker> CommitBlockers { get; set; } = new List<WriteBlocker>();

    public string CompletionCondition { get; set; } = string.Empty;
}

public sealed class FlightCheckpointReloadResult
{
    public string ActionId { get; set; } = string.Empty;

    public string CheckpointId { get; set; } = string.Empty;

    public bool Accepted { get; set; }

    public bool IdempotentReplay { get; set; }

    public string State { get; set; } = string.Empty;
}
