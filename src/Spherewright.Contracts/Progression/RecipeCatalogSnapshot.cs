namespace Spherewright.Contracts.Progression;

public sealed class RecipeCatalogSnapshot
{
    public string SessionId { get; set; } = string.Empty;

    public int PlanetId { get; set; }

    public long CapturedAtGameTick { get; set; }

    public List<ItemCatalogEntry> Items { get; set; } = new List<ItemCatalogEntry>();

    public List<RecipeCatalogEntry> Recipes { get; set; } = new List<RecipeCatalogEntry>();

    public RuntimeDependencyGraph FirstRedMatrixDependencies { get; set; } = new RuntimeDependencyGraph();
}

public sealed class ItemCatalogEntry
{
    public int ItemId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int StackSize { get; set; }

    public bool IsRaw { get; set; }

    public bool CanBuild { get; set; }

    public bool Unlocked { get; set; }

    public int? HandcraftRecipeId { get; set; }
}

public sealed class RecipeCatalogEntry
{
    public int RecipeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string RecipeType { get; set; } = string.Empty;

    public bool Handcraft { get; set; }

    public bool Unlocked { get; set; }

    public int TimeSpend { get; set; }

    public int? PrerequisiteTechId { get; set; }

    public string? PrerequisiteTechName { get; set; }

    public List<CatalogItemAmount> Inputs { get; set; } = new List<CatalogItemAmount>();

    public List<CatalogItemAmount> Outputs { get; set; } = new List<CatalogItemAmount>();
}

public sealed class CatalogItemAmount
{
    public int ItemId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Count { get; set; }
}

public sealed class RuntimeDependencyGraph
{
    public int TargetItemId { get; set; }

    public string TargetItemName { get; set; } = string.Empty;

    public List<int> ItemIds { get; set; } = new List<int>();

    public List<int> RecipeIds { get; set; } = new List<int>();

    public List<RuntimeDependencyEdge> Edges { get; set; } = new List<RuntimeDependencyEdge>();
}

public sealed class RuntimeDependencyEdge
{
    public string FromKind { get; set; } = string.Empty;

    public int FromId { get; set; }

    public string ToKind { get; set; } = string.Empty;

    public int ToId { get; set; }
}
