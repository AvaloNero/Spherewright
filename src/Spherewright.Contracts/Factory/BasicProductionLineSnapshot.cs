namespace Spherewright.Contracts.Factory;

public sealed class BasicProductionLineSnapshot
{
    public string ActionId { get; set; } = string.Empty;

    public string SessionId { get; set; } = string.Empty;

    public long Revision { get; set; }

    public bool StructureValid { get; set; }

    public bool ConnectionsValid { get; set; }

    public bool RecipeValid { get; set; }

    public bool PowerNetworkValid { get; set; }

    public bool Producing { get; set; }

    public string ProductionState { get; set; } = string.Empty;

    public int RecipeId { get; set; }

    public int InputItemId { get; set; }

    public int InputStorageCount { get; set; }

    public int OutputItemId { get; set; }

    public int OutputStorageCount { get; set; }

    public bool AssemblerWorking { get; set; }

    public BasicProductionLineEntities Entities { get; set; } = new BasicProductionLineEntities();
}
