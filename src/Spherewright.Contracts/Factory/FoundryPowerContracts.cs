namespace Spherewright.Contracts.Factory;

public sealed class FoundryPowerAssessment
{
    public string State { get; set; } = "unavailable";
    public string AssessmentHash { get; set; } = string.Empty;
    public long CapturedAtGameTick { get; set; }
    public double GeometryMarginMetres { get; set; }
    public bool GeometryBoundaryUncertain { get; set; }
    public bool AllPlannedConsumersCovered { get; set; }
    public bool FullBaseLoadBudgetSatisfied { get; set; }
    public long AdditionalBaseWorkPowerWatts { get; set; }
    public List<int> UncoveredObjectIndices { get; set; } = new List<int>();
    public List<int> AmbiguousObjectIndices { get; set; } = new List<int>();
    public List<int> AmbiguousExistingConsumerIds { get; set; } = new List<int>();
    public List<FoundryPowerComponentBudget> Components { get; set; } = new List<FoundryPowerComponentBudget>();
    public List<string> Blockers { get; set; } = new List<string>();
    public List<string> RemainingChecks { get; set; } = new List<string>
    {
        "Advisory full base-load budget, not a native build condition or write token. Fresh native preparation and completed-object power readback remain required.",
        "Existing consumers reserve max(work, idle, current required) rather than idle instantaneous load; chargers reserve their native working demand.",
        "New loads are unproliferated prefab work/idle maxima. Future proliferation, fuel depletion and weather changes require fresh observation.",
        "Existing generation capacity is a current snapshot, not proof of sustainable fuel. New generating credit is native wind only.",
        "Existing unfinished prebuilds make this advisory capture unavailable: their later loads/network joins must not be silently ignored.",
        "Ambiguous disconnected covering networks are not silently assigned. Planned power nodes may connect networks but are not built by this assessment.",
        "A planet-size-scaled geometry margin of at least 2mm marks boundary evidence uncertain, not free capacity. Fresh native construction and power readback remain authoritative. Newly covered existing unpowered consumers also reserve their full demand.",
    };
}

public sealed class FoundryPowerComponentBudget
{
    public List<int> ExistingNetworkIds { get; set; } = new List<int>();
    public List<int> PlannedNodeIndices { get; set; } = new List<int>();
    public List<int> PlannedConsumerIndices { get; set; } = new List<int>();
    public List<int> NewlyCoveredExistingConsumerIds { get; set; } = new List<int>();
    public long ExistingGenerationCapacityPerTick { get; set; }
    public long ExistingReservedDemandPerTick { get; set; }
    public long ExistingExportPerTick { get; set; }
    public long AddedWindGenerationPerTick { get; set; }
    public long AddedBaseDemandPerTick { get; set; }
    public long NewlyCoveredExistingDemandPerTick { get; set; }
    public long DeficitPerTick { get; set; }
    public long HeadroomAfterPlanPerTick { get; set; }
}
