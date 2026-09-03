using Spherewright.Contracts.Journals;

namespace Spherewright.Contracts.Sessions;

public sealed class PrepareUserSaveImportRequest
{
    public long ExpectedRevision { get; set; }
}

public sealed class CommitUserSaveImportRequest
{
    public string PlanToken { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = string.Empty;

    public bool UserConfirmedInConversation { get; set; }

    public bool AcknowledgeOriginalSaveRemainsUnchanged { get; set; }

    public bool AcknowledgeJournalStartsAtImport { get; set; }
}

public sealed class PreparedUserSaveImportPlan
{
    public bool Prepared { get; set; }

    public string PlanToken { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public long ExpectedRevision { get; set; }

    public bool OriginalSavePreserved { get; set; } = true;

    public string JournalTrackingMode { get; set; } = GameplayJournalTrackingModes.AttachedExistingSave;

    public bool HistoricalCoverageComplete { get; set; }

    public bool UserConfirmationRequired { get; set; } = true;

    public string ConfirmationPrompt { get; set; } = string.Empty;

    public bool CommitAllowedNow { get; set; }

    public List<WriteBlocker> CommitBlockers { get; set; } = new List<WriteBlocker>();

    public string CompletionCondition { get; set; } = string.Empty;
}

public sealed class UserSaveImportResult
{
    public string ActionId { get; set; } = string.Empty;

    public bool Accepted { get; set; }

    public bool IdempotentReplay { get; set; }

    public string State { get; set; } = string.Empty;

    public string? SessionId { get; set; }

    public int? PlanetId { get; set; }

    public long? SavedGameTick { get; set; }

    public bool OriginalSavePreserved { get; set; } = true;

    public string JournalTrackingMode { get; set; } = GameplayJournalTrackingModes.AttachedExistingSave;

    public bool HistoricalCoverageComplete { get; set; }
}
