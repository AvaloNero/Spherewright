namespace Spherewright.Contracts.Factory;

public sealed class BasicProductionLineResult
{
    public string ActionId { get; set; } = string.Empty;

    public bool Completed { get; set; }

    public bool Changed { get; set; }

    public bool IdempotentReplay { get; set; }

    public string SessionId { get; set; } = string.Empty;

    public long Revision { get; set; }

    public int RecipeId { get; set; }

    public int InputItemId { get; set; }

    public int InputItemCount { get; set; }

    public int OutputItemId { get; set; }

    public BasicProductionLineEntities Entities { get; set; } = new BasicProductionLineEntities();

    public bool RecipeVerified { get; set; }

    public bool ConnectionsVerified { get; set; }

    public bool InputStockVerified { get; set; }

    public bool Saved { get; set; }

    public string ProductionState { get; set; } = string.Empty;
}

public sealed class BasicProductionLineEntities
{
    public int InputStorageEntityId { get; set; }

    public int AssemblerEntityId { get; set; }

    public int OutputStorageEntityId { get; set; }

    public int InputInserterEntityId { get; set; }

    public int OutputInserterEntityId { get; set; }

    public int PowerGeneratorEntityId { get; set; }
}
