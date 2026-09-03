namespace Spherewright.Contracts.Diagnostics;

public static class OverseerWindowStates
{
    public const string WarmingUp = "warming_up";
    public const string Ready = "ready";
    public const string Discontinuous = "discontinuous";
}

public static class OverseerWindowResetReasons
{
    public const string InitialSample = "initial_sample";
    public const string SameGameTick = "same_game_tick";
    public const string OwnedSaveChanged = "owned_save_changed";
    public const string GameTickRegressed = "game_tick_regressed";
    public const string CounterRegressed = "counter_regressed";
    public const string CounterAdvancedWithoutGameTime = "counter_advanced_without_game_time";
    public const string SampleGapExceeded = "sample_gap_exceeded";
}

public static class OverseerFindingKinds
{
    public const string MaterialShortage = "material_shortage";
    public const string OutputBlocked = "output_blocked";
    public const string InsufficientPower = "insufficient_power";
    public const string LogisticsBlocked = "logistics_blocked";
    public const string VeinExhausted = "vein_exhausted";
}

public static class OverseerFindingConfidences
{
    public const string Confirmed = "confirmed";
    public const string Suspected = "suspected";
}

public static class OverseerFindingSeverities
{
    public const string Warning = "warning";
    public const string Stopped = "stopped";
}

public sealed class OverseerWindowSnapshot
{
    public string State { get; set; } = OverseerWindowStates.WarmingUp;

    public string? ResetReason { get; set; }

    public long? StartGameTick { get; set; }

    public long EndGameTick { get; set; }

    public long ElapsedGameTicks { get; set; }

    public double ElapsedGameSeconds { get; set; }

    public double WallClockElapsedSeconds { get; set; }

    public double ExcludedNonGameSeconds { get; set; }

    public bool CrossedSessionBoundary { get; set; }
}

public sealed class ProductionRateSnapshot
{
    public int PlanetId { get; set; }

    public int ItemId { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public long ProducedCount { get; set; }

    public long ConsumedCount { get; set; }

    public double ActualProductionPerMinute { get; set; }

    public double ActualConsumptionPerMinute { get; set; }

    public double? TheoreticalProductionPerMinute { get; set; }

    public double? Utilization { get; set; }
}

public sealed class OverseerFindingSnapshot
{
    public string Kind { get; set; } = string.Empty;

    public string Confidence { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public int PlanetId { get; set; }

    public int ObjectId { get; set; }

    public int? ItemId { get; set; }

    public string Summary { get; set; } = string.Empty;

    public List<OverseerEvidenceSnapshot> Evidence { get; set; } = new List<OverseerEvidenceSnapshot>();

    public List<OverseerPathNodeSnapshot> UpstreamPath { get; set; } = new List<OverseerPathNodeSnapshot>();
}

public sealed class OverseerEvidenceSnapshot
{
    public string Metric { get; set; } = string.Empty;

    public double? NumericValue { get; set; }

    public string? TextValue { get; set; }

    public string? Unit { get; set; }
}

public sealed class OverseerPathNodeSnapshot
{
    public int PlanetId { get; set; }

    public int? ObjectId { get; set; }

    public int? ItemId { get; set; }

    public string Kind { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}
