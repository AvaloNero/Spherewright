namespace Spherewright.Contracts.Factory;

public static class FactoryObjectKinds
{
    public const string Entity = "entity";
    public const string Prebuild = "prebuild";
}

public sealed class ListFactoryEntitiesRequest
{
    public int PlanetId { get; set; }

    public string? ObjectKind { get; set; }

    public string? ComponentKind { get; set; }

    public int? ItemId { get; set; }

    public int Limit { get; set; } = 50;

    public string? Cursor { get; set; }
}

public sealed class ListFactoryEntitiesResult
{
    public string SessionId { get; set; } = string.Empty;

    public int PlanetId { get; set; }

    public long CapturedAtGameTick { get; set; }

    public string SnapshotId { get; set; } = string.Empty;

    public DateTimeOffset SnapshotExpiresAtUtc { get; set; }

    public List<FactoryEntitySnapshot> Entities { get; set; } = new List<FactoryEntitySnapshot>();

    public string? NextCursor { get; set; }
}

public sealed class InspectFactoryEntityRequest
{
    public int PlanetId { get; set; }

    public int ObjectId { get; set; }
}

public sealed class FactoryEntitySnapshot
{
    public string SessionId { get; set; } = string.Empty;

    public int PlanetId { get; set; }

    public int ObjectId { get; set; }

    public string ObjectKind { get; set; } = string.Empty;

    public int ItemId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ComponentKind { get; set; } = string.Empty;

    public Vector3Snapshot Position { get; set; } = new Vector3Snapshot();

    public QuaternionSnapshot Rotation { get; set; } = new QuaternionSnapshot();

    public int RecipeId { get; set; }

    public string? RecipeName { get; set; }

    public bool IsWorking { get; set; }

    public int Progress { get; set; }

    public int ProgressRequired { get; set; }

    public int? PowerNetworkId { get; set; }

    public long? PowerDemandPerTick { get; set; }

    public double? PowerServeRatio { get; set; }

    public List<FactoryConnectionSnapshot> Connections { get; set; } = new List<FactoryConnectionSnapshot>();

    public List<FactoryBufferSnapshot> Buffers { get; set; } = new List<FactoryBufferSnapshot>();

    public List<int> ResourceNodeIds { get; set; } = new List<int>();

    public int? PickTargetObjectId { get; set; }

    public int? InsertTargetObjectId { get; set; }

    public int? FilterItemId { get; set; }

    public string? FilterItemName { get; set; }

    public string? InserterStage { get; set; }

    public int? InserterStackCount { get; set; }

    public int? RequiredBuildItemCount { get; set; }

    public float? ConstructionProgress { get; set; }

    public long CapturedAtGameTick { get; set; }

    public string StateHash { get; set; } = string.Empty;

    public int StateHashVersion { get; set; } = 1;

    public string EndpointStateHash { get; set; } = string.Empty;

    public int EndpointStateHashVersion { get; set; } = 1;
}

public sealed class QuaternionSnapshot
{
    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    public float W { get; set; }
}

public sealed class FactoryConnectionSnapshot
{
    public int Slot { get; set; }

    public bool IsOutput { get; set; }

    public int OtherObjectId { get; set; }

    public int OtherSlot { get; set; }
}

public sealed class FactoryBufferSnapshot
{
    public string Role { get; set; } = string.Empty;

    public int ItemId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Count { get; set; }

    public int Inc { get; set; }
}
