namespace Spherewright.Contracts.Factory;

public sealed class BasicProductionLinePlan
{
    public string PlanToken { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public string SessionId { get; set; } = string.Empty;

    public long ExpectedRevision { get; set; }

    public bool DryRun { get; set; } = true;

    public bool CommitAllowed { get; set; }

    public int RecipeId { get; set; }

    public string RecipeName { get; set; } = string.Empty;

    public int InputItemId { get; set; }

    public string InputItemName { get; set; } = string.Empty;

    public int InputItemCount { get; set; }

    public int OutputItemId { get; set; }

    public string OutputItemName { get; set; } = string.Empty;

    public List<PlannedBuildingSnapshot> Buildings { get; set; } = new List<PlannedBuildingSnapshot>();

    public List<string> Warnings { get; set; } = new List<string>();
}

public sealed class PlannedBuildingSnapshot
{
    public string Role { get; set; } = string.Empty;

    public int ItemId { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public Vector3Snapshot Position { get; set; } = new Vector3Snapshot();
}
