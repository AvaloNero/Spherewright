namespace Spherewright.Contracts.Factory;

public sealed class GetFoundryPlanRequest
{
    public int PlanetId { get; set; }

    public int TargetItemId { get; set; }

    public decimal TargetRatePerMinute { get; set; }

    public List<int> ExternalSupplyItemIds { get; set; } = new List<int>();

    public List<FoundryRecipeChoice> RecipeChoices { get; set; } = new List<FoundryRecipeChoice>();

    public FoundrySiteRequest? Site { get; set; }
}

public sealed class FoundryRecipeChoice
{
    public int ItemId { get; set; }

    public int RecipeId { get; set; }

    public int BuildingItemId { get; set; }
}

// This is a material/scale draft. Placement and native action tokens are a
// separate concern; a deterministic draft never grants permission to build.
public sealed class FoundryPlanSnapshot
{
    public int SchemaVersion { get; set; } = 1;

    public string SessionId { get; set; } = string.Empty;

    public int PlanetId { get; set; }

    public long CapturedAtGameTick { get; set; }

    public string PlanHash { get; set; } = string.Empty;

    public bool Executable { get; set; }

    public string Phase { get; set; } = "material_plan";

    public int TargetItemId { get; set; }

    public decimal TargetRatePerMinute { get; set; }

    public int ProductionDepth { get; set; }

    public int MachineCount { get; set; }

    public long MachineWorkPowerWatts { get; set; }

    public string RateBasis { get; set; } = "unproliferated_prefab_speed_at_full_power_v1";

    public List<FoundryStage> Stages { get; set; } = new List<FoundryStage>();

    public List<FoundryFlow> ExternalInputs { get; set; } = new List<FoundryFlow>();

    public List<FoundryFlow> Byproducts { get; set; } = new List<FoundryFlow>();

    public List<FoundryMaterialCost> MachineCost { get; set; } = new List<FoundryMaterialCost>();

    public List<string> RemainingChecks { get; set; } = new List<string>();

    public FoundrySiteSnapshot? Site { get; set; }
}

public sealed class FoundryStage
{
    public string StageId { get; set; } = string.Empty;

    public int ItemId { get; set; }

    public int RecipeId { get; set; }

    public string RecipeName { get; set; } = string.Empty;

    public int BuildingItemId { get; set; }

    public decimal RecipeExecutionsPerMinute { get; set; }

    public decimal RequiredRatePerMinute { get; set; }

    public decimal PerMachineRatePerMinute { get; set; }

    public decimal InstalledRatePerMinute { get; set; }

    public int MachineCount { get; set; }

    public int ProductionDepth { get; set; }

    public long MachineWorkPowerWatts { get; set; }

    public List<string> Dependencies { get; set; } = new List<string>();

    public List<FoundryFlow> Inputs { get; set; } = new List<FoundryFlow>();

    public List<FoundryFlow> Outputs { get; set; } = new List<FoundryFlow>();
}

public sealed class FoundryFlow
{
    public int ItemId { get; set; }

    public decimal RatePerMinute { get; set; }
}

public sealed class FoundryMaterialCost
{
    public int ItemId { get; set; }

    public int Count { get; set; }
}
