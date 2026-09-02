namespace Spherewright.Contracts.Progression;

public sealed class ProgressionStateSnapshot
{
    public string SessionId { get; set; } = string.Empty;

    public int PlanetId { get; set; }

    public long CapturedAtGameTick { get; set; }

    public string StateHash { get; set; } = string.Empty;

    public int StateHashVersion { get; set; } = 1;

    public string SelectionStateHash { get; set; } = string.Empty;

    public int SelectionStateHashVersion { get; set; } = 1;

    public int CurrentTechId { get; set; }

    public string? CurrentTechName { get; set; }

    public List<int> TechQueue { get; set; } = new List<int>();

    public List<TechStateSnapshot> Technologies { get; set; } = new List<TechStateSnapshot>();
}

public sealed class TechStateSnapshot
{
    public int TechId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool Unlocked { get; set; }

    public int CurrentLevel { get; set; }

    public int MaximumLevel { get; set; }

    public long HashUploaded { get; set; }

    public long HashRequired { get; set; }

    public long UnlockTick { get; set; }

    public bool IsLabTech { get; set; }

    public bool IsQueued { get; set; }

    public List<int> PrerequisiteTechIds { get; set; } = new List<int>();

    public List<int> UnlockRecipeIds { get; set; } = new List<int>();

    public List<TechMatrixRequirement> MatrixRequirements { get; set; } = new List<TechMatrixRequirement>();

    public List<TechItemRequirement> ItemRequirements { get; set; } = new List<TechItemRequirement>();
}

public sealed class TechMatrixRequirement
{
    public int ItemId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int PointsPerHash { get; set; }

    public long RequiredItemCount { get; set; }
}

public sealed class TechItemRequirement
{
    public int ItemId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int PointsPerHash { get; set; }

    public long RequiredItemCount { get; set; }

    public bool IsMatrix { get; set; }
}
