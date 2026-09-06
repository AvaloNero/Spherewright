namespace Spherewright.Contracts.Factory;

// Explicit blueprint data only. Neither the code nor its text can select a goal.
public sealed class FoundryBlueprintRequest
{
    public string BlueprintCode { get; set; } = string.Empty;
    public BlueprintSiteRequest Site { get; set; } = new BlueprintSiteRequest();
    public List<FoundryBoundaryPort> BoundaryPorts { get; set; } = new List<FoundryBoundaryPort>();
}

public sealed class FoundryBoundaryPort
{
    public int ItemId { get; set; }
    public string Direction { get; set; } = string.Empty;
    public int ObjectIndex { get; set; }
    public int Slot { get; set; }
}

// Optional intent for the EXISTING finite blueprint prepare/commit executor.
// For continuation the durable original intent is used; replacements are rejected.
public sealed class FoundryConstructionIntent
{
    public int TargetItemId { get; set; }
    public decimal TargetRatePerMinute { get; set; }
    public List<int> ExternalSupplyItemIds { get; set; } = new List<int>();
    public List<FoundryRecipeChoice> RecipeChoices { get; set; } = new List<FoundryRecipeChoice>();
    public List<FoundryBoundaryPort> BoundaryPorts { get; set; } = new List<FoundryBoundaryPort>();
}

public sealed class FoundryConstructionPlan
{
    public int SchemaVersion { get; set; } = 1;
    public string PlanHash { get; set; } = string.Empty;
    public string MaterialPlanHash { get; set; } = string.Empty;
    public string BlueprintHash { get; set; } = string.Empty;
    public string ImmutableSiteHash { get; set; } = string.Empty;
    public int TargetItemId { get; set; }
    public decimal TargetRatePerMinute { get; set; }
    public int ProductionDepth { get; set; }
    public bool CanPrepare { get; set; }
    public bool InternalFlowsRouted { get; set; }
    public bool TransportCapacityVerified { get; set; }
    public FoundryTransportBudget TransportBudget { get; set; } = new FoundryTransportBudget();
    public string RateBasis { get; set; } = "declared_flow_allocation_not_measured_throughput_v1";
    public List<FoundryStageBinding> Stages { get; set; } = new List<FoundryStageBinding>();
    public List<FoundryRoutedFlow> Routes { get; set; } = new List<FoundryRoutedFlow>();
    public List<FoundryBoundaryFlow> Boundaries { get; set; } = new List<FoundryBoundaryFlow>();
    public List<FoundryMaterialCost> ConstructionCost { get; set; } = new List<FoundryMaterialCost>();
    public List<FoundryConstructionStep> Steps { get; set; } = new List<FoundryConstructionStep>();
    public List<string> Blockers { get; set; } = new List<string>();
    public List<string> RemainingChecks { get; set; } = new List<string>
    {
        "The blueprint supplies the explicitly chosen layout; the machine-grid draft does not invent routed buildings.",
        "TransportBudget checks native full-power single-item belt/basic-sorter capacity, not fair runtime distribution or sustained throughput. TransportCapacityVerified remains false until independently measured.",
        "Boundary ports are unconnected conditions: prove automatic external sources and a lasting output sink at the declared rates. Empty or finite storage is not sustained supply.",
        "Native placement, whole inventory and separate full-base-load power checks must still pass on fresh prepare/commit. This read creates no authority or persistent build.",
        "Execute only through finite blueprint prepare/commit with this intent/hash, poll terminal and read per-object progress. Cancellation retains submitted objects; restart resumes the same buildId with fresh authority.",
        "After ordinary drones complete, recheck recipes, filters, reciprocal logistics, power and continuous nonzero actual production. A complete construction graph is not a throughput acceptance result.",
    };
}

public sealed class FoundryTransportBudget
{
    public string Basis { get; set; } = "native_full_power_single_item_no_backpressure_v1";
    public bool AllCapacitiesKnown { get; set; }
    public bool Satisfied { get; set; }
    public List<FoundryTransportChannel> Channels { get; set; } = new List<FoundryTransportChannel>();
    public List<string> Blockers { get; set; } = new List<string>();
}

public sealed class FoundryTransportChannel
{
    public int ObjectIndex { get; set; }
    public int BuildingItemId { get; set; }
    // Allocated flow, not the entire requested flow when routing is blocked.
    public decimal AllocatedRatePerMinute { get; set; }
    public decimal? RatedSingleItemRatePerMinute { get; set; }
    public int? BeltSpeedRaw { get; set; }
    public int? InserterSttRaw { get; set; }
    public int? InserterSpan { get; set; }
    public int? IdealCycleGameTicks { get; set; }
}

public sealed class FoundryStageBinding
{
    public string StageId { get; set; } = string.Empty;
    public int ItemId { get; set; }
    public int RecipeId { get; set; }
    public int BuildingItemId { get; set; }
    public decimal RequiredRatePerMinute { get; set; }
    public decimal InstalledRatePerMinute { get; set; }
    public List<int> ObjectIndices { get; set; } = new List<int>();
}

public sealed class FoundryRoutedFlow
{
    public int ItemId { get; set; }
    public decimal RequiredRatePerMinute { get; set; }
    public List<int> ObjectIndices { get; set; } = new List<int>();
}

public sealed class FoundryBoundaryFlow
{
    public int ItemId { get; set; }
    public string Direction { get; set; } = string.Empty;
    public int ObjectIndex { get; set; }
    public int Slot { get; set; }
    public decimal RequiredRatePerMinute { get; set; }
}

public sealed class FoundryConstructionStep
{
    public int ObjectIndex { get; set; }
    public int ItemId { get; set; }
    public string Role { get; set; } = string.Empty;
    public List<int> Dependencies { get; set; } = new List<int>();
}
