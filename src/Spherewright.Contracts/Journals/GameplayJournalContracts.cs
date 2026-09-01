namespace Spherewright.Contracts.Journals;

public static class GameplayJournalEventKinds
{
    public const string ManualItemFirst = "manual_item_first";
    public const string ProductionLineItemFirst = "production_line_item_first";
    public const string TechnologyFirstSelected = "technology_first_selected";
    public const string UpgradeFirstSelected = "upgrade_first_selected";
}

public static class GameplayJournalTrackingModes
{
    public const string FromNewGame = "from_new_game";
    public const string AttachedExistingSave = "attached_existing_save";
}

public sealed class GameplayJournalSnapshot
{
    public string SessionId { get; set; } = string.Empty;

    public string JournalId { get; set; } = string.Empty;

    public string TrackingMode { get; set; } = string.Empty;

    public bool HistoricalCoverageComplete { get; set; }

    public string CreatedAtActualTime { get; set; } = string.Empty;

    public long TrackingStartedAtGameTick { get; set; }

    public string TrackingStartedAtGameTime { get; set; } = string.Empty;

    public long CapturedAtGameTick { get; set; }

    public long DurableThroughSequence { get; set; }

    public bool PersistencePending { get; set; }

    public string? PersistenceError { get; set; }

    public List<GameplayJournalEntry> Entries { get; set; } = new List<GameplayJournalEntry>();
}

public sealed class GameplayJournalEntry
{
    public long Sequence { get; set; }

    public string Kind { get; set; } = string.Empty;

    public int ItemId { get; set; }

    public int TechId { get; set; }

    public string Name { get; set; } = string.Empty;

    public long ObservedCount { get; set; }

    public string ActualTime { get; set; } = string.Empty;

    public long GameTick { get; set; }

    public string GameTime { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;
}
