namespace Spherewright.Contracts.Sessions;

public sealed class PrepareOwnedWorldResumeRequest
{
    public string ResumeToken { get; set; } = string.Empty;
}

public sealed class CommitOwnedWorldResumeRequest
{
    public string PlanToken { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = string.Empty;
}

public sealed class PreparedOwnedWorldResumePlan
{
    public bool Prepared { get; set; }

    public string PlanToken { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public int ExpectedPlanetId { get; set; }

    public long MinimumGameTick { get; set; }

    public bool CommitAllowedNow { get; set; }

    public List<WriteBlocker> CommitBlockers { get; set; } = new List<WriteBlocker>();

    public string CompletionCondition { get; set; } = string.Empty;
}

public sealed class OwnedWorldResumeResult
{
    public string ActionId { get; set; } = string.Empty;

    public bool Accepted { get; set; }

    public bool IdempotentReplay { get; set; }

    public string State { get; set; } = string.Empty;
}
