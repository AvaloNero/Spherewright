namespace Spherewright.Contracts.Factory;

public sealed class AssemblerSnapshot
{
    public int PlanetId { get; set; }

    public int EntityId { get; set; }

    public int ComponentId { get; set; }

    public int BuildingItemId { get; set; }

    public string BuildingName { get; set; } = string.Empty;

    public int RecipeId { get; set; }

    public string? RecipeName { get; set; }

    public int TimeSpent { get; set; }

    public int TimeRequired { get; set; }

    public bool IsWorking { get; set; }

    public Vector3Snapshot Position { get; set; } = new Vector3Snapshot();

    public long Revision { get; set; }
}
