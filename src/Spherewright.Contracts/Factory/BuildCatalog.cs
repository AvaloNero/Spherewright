namespace Spherewright.Contracts.Factory;

public sealed class BuildCatalog
{
    public int PlanetId { get; set; }

    public long Revision { get; set; }

    public Vector3Snapshot PlayerPosition { get; set; } = new Vector3Snapshot();

    public float PlayerBuildArea { get; set; }

    public bool SandboxToolsEnabled { get; set; }

    public List<BuildCatalogItem> Buildings { get; set; } = new List<BuildCatalogItem>();

    public List<BuildCatalogRecipe> Recipes { get; set; } = new List<BuildCatalogRecipe>();

    public BasicLineRecommendation? RecommendedBasicLine { get; set; }
}

public sealed class BuildCatalogItem
{
    public int ItemId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public int ModelIndex { get; set; }

    public int Grade { get; set; }

    public int BuildMode { get; set; }

    public bool Unlocked { get; set; }

    public bool Available { get; set; }

    public string RecipeType { get; set; } = string.Empty;

    public int SlotCount { get; set; }

    public float RoughRadius { get; set; }

    public float PowerConnectDistance { get; set; }

    public float PowerCoverRadius { get; set; }

    public int? ProductionSpeedRaw { get; set; }

    public long? WorkEnergyPerTick { get; set; }
}

public sealed class BuildCatalogRecipe
{
    public int RecipeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string RecipeType { get; set; } = string.Empty;

    public bool Unlocked { get; set; }

    public int TimeSpend { get; set; }

    public List<BuildCatalogIngredient> Inputs { get; set; } = new List<BuildCatalogIngredient>();

    public List<BuildCatalogIngredient> Outputs { get; set; } = new List<BuildCatalogIngredient>();
}

public sealed class BuildCatalogIngredient
{
    public int ItemId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Count { get; set; }

    public bool RawMaterial { get; set; }
}

public sealed class BasicLineRecommendation
{
    public int StorageItemId { get; set; }

    public int AssemblerItemId { get; set; }

    public int InserterItemId { get; set; }

    public int PowerGeneratorItemId { get; set; }

    public int RecipeId { get; set; }

    public int InputItemId { get; set; }

    public int OutputItemId { get; set; }
}
