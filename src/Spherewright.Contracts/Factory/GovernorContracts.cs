using Spherewright.Contracts.Diagnostics;

namespace Spherewright.Contracts.Factory;

public sealed class GetGovernorPlanRequest
{
    public int PlanetId { get; set; }
    public int TargetItemId { get; set; }
    public decimal TargetRatePerMinute { get; set; }
    public decimal ToleranceFraction { get; set; } = .1m;
    public int ValidationGameTicks { get; set; } = 36000;
    public List<BlueprintSelectedEntity> SourceEntities { get; set; } = new List<BlueprintSelectedEntity>();
    public List<int> ExternalSupplyItemIds { get; set; } = new List<int>();
    public List<FoundryRecipeChoice> RecipeChoices { get; set; } = new List<FoundryRecipeChoice>();
    // Optional exact pre-execution ready proposal previously returned in this session.
    // The server retains its baseline/target; callers cannot supply measured history.
    public string? ValidationBaselineProposalHash { get; set; }
}

public sealed class GovernorBaselineSnapshot
{
    public string State { get; set; } = "warming_up";
    public int IndependentWindowCount { get; set; }
    public long? StartGameTick { get; set; }
    public long? EndGameTick { get; set; }
    public decimal? ProductionPerMinute { get; set; }
    public decimal? MinimumPerMinute { get; set; }
    public decimal? MaximumPerMinute { get; set; }
}

public sealed class GovernorPlanSnapshot
{
    public string Phase { get; set; } = "governor_proposal";
    public bool Executable { get; set; }
    public bool Balanced { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public int PlanetId { get; set; }
    public long CapturedAtGameTick { get; set; }
    public long Revision { get; set; }
    public string ProposalHash { get; set; } = string.Empty;
    public string SourceStateHash { get; set; } = string.Empty;
    public bool ValidationBaselineAvailable { get; set; }
    public int TargetItemId { get; set; }
    public decimal TargetRatePerMinute { get; set; }
    public decimal ToleranceFraction { get; set; }
    public int ValidationGameTicks { get; set; }
    public string MeasurementScope { get; set; } = "local_planet_item_not_selected_entity_counters";
    public bool SelectionContainsAllTargetProducers { get; set; }
    public OverseerPowerSummarySnapshot Power { get; set; } = new OverseerPowerSummarySnapshot();
    public OverseerLogisticsSummarySnapshot Logistics { get; set; } = new OverseerLogisticsSummarySnapshot();
    public bool FindingsTruncated { get; set; }
    public GovernorBaselineSnapshot Baseline { get; set; } = new GovernorBaselineSnapshot();
    public OverseerWindowSnapshot CurrentWindow { get; set; } = new OverseerWindowSnapshot();
    public List<GovernorSupplyBalance> Supply { get; set; } = new List<GovernorSupplyBalance>();
    public List<GovernorAlternative> Alternatives { get; set; } = new List<GovernorAlternative>();
    public List<OverseerFindingSnapshot> Findings { get; set; } = new List<OverseerFindingSnapshot>();
    // Findings on the target's diagnostic path or an explicitly selected object.
    // Unrelated/unproven global branches stay in Findings, not silently discarded.
    public List<OverseerFindingSnapshot> TargetChainFindings { get; set; } = new List<OverseerFindingSnapshot>();
    public int UnattributedPlanetFindingCount { get; set; }
    public List<string> Blockers { get; set; } = new List<string>();
    public List<string> RemainingChecks { get; set; } = new List<string>();
    public FoundryPlanSnapshot FullTargetScale { get; set; } = new FoundryPlanSnapshot();
    public GovernorThroughputValidationSnapshot? ThroughputValidation { get; set; }
}

public sealed class GovernorThroughputValidationSnapshot
{
    public string State { get; set; } = "waiting_for_target";
    public string BaselineProposalHash { get; set; } = string.Empty;
    public long DeclaredAtGameTick { get; set; }
    public long? LockedAtGameTick { get; set; }
    public decimal BaselineProductionPerMinute { get; set; }
    public decimal TargetRatePerMinute { get; set; }
    public decimal TargetMultiplier { get; set; }
    public decimal ToleranceFraction { get; set; }
    public int RequiredGameTicks { get; set; }
    public long ObservedContiguousGameTicks { get; set; }
    public long? StartGameTick { get; set; }
    public long? EndGameTick { get; set; }
    public decimal? MinimumWindowRatePerMinute { get; set; }
    public decimal? MaximumWindowRatePerMinute { get; set; }
    public long ObservationCount { get; set; }
    public string? ResetReason { get; set; }
    public bool ThroughputTargetObserved { get; set; }
    public bool DoubleThroughputTargetObserved { get; set; }
    public bool Durable { get; set; }
    public string MeasurementBasis { get; set; } = "overlapping_native_600_tick_windows_no_unobserved_tick_gaps";
    public string HealthEvidenceBasis { get; set; } = "sampled_power_diagnostics_and_write_health_not_continuous_tick_health";
    public List<string> RemainingChecks { get; set; } = new List<string>
    {
        "Only a measured throughput window, not complete balance or a release-gate certificate; verify upstream automatic supply, logistics, power and material conservation separately.",
        "Power/diagnostic/write-health checks are sampled. An unobserved short health interruption cannot be ruled out by this record.",
        "The exact baseline, target, tolerance and duration are immutable; a new session discards this non-durable validation. Never submit a claimed historical baseline.",
    };
}

public sealed class GovernorSupplyBalance
{
    public int ItemId { get; set; }
    public decimal TargetChainDemandPerMinute { get; set; }
    public decimal ActualProductionPerMinute { get; set; }
    public decimal ActualConsumptionPerMinute { get; set; }
    public decimal AllocatableSurplusPerMinute { get; set; }
    public decimal AdditionalChainDemandPerMinute { get; set; }
    public decimal MinimumAdditionalAutomaticSupplyPerMinute { get; set; }
    // P-C is not inventory change: transfers, transport and manual intervention differ.
    public decimal ProductionMinusConsumptionPerMinute { get; set; }
    public long SelectedBufferItemCount { get; set; }
    public long? SelectedBufferItemDelta { get; set; }
    public long? InventoryObservationStartGameTick { get; set; }
    public long InventoryObservationEndGameTick { get; set; }
    public bool DemandIsLowerBound { get; set; } = true;
}

public sealed class GovernorAlternative
{
    public string Kind { get; set; } = string.Empty;
    public string Status { get; set; } = "requires_fresh_native_preparation";
    public decimal? TheoreticalTargetCapacityGainPerMinute { get; set; }
    public decimal? ActualTargetGainPerMinute { get; set; } // Always unknown before execution/measurement.
    public long? AdditionalMachineWorkPowerWatts { get; set; }
    public List<FoundryMaterialCost> Consume { get; set; } = new List<FoundryMaterialCost>();
    public List<FoundryMaterialCost> Refund { get; set; } = new List<FoundryMaterialCost>();
    public List<GovernorUpgradeCandidate> Upgrades { get; set; } = new List<GovernorUpgradeCandidate>();
    public List<string> Conditions { get; set; } = new List<string>();
}

public sealed class GovernorUpgradeCandidate
{
    public int ObjectId { get; set; }
    public int CurrentItemId { get; set; }
    public int TargetItemId { get; set; }
    public int RecipeId { get; set; }
}
