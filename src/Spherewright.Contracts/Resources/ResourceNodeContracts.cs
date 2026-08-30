using Spherewright.Contracts.Factory;

namespace Spherewright.Contracts.Resources;

public static class ResourceNodeKinds
{
    public const string Vein = "vein";
    public const string Vegetation = "vegetation";
}

public sealed class ListResourceNodesRequest
{
    public int PlanetId { get; set; }

    public string? Kind { get; set; }

    public string? ResourceType { get; set; }

    public int? ProductItemId { get; set; }

    public int Limit { get; set; } = 50;

    public string? Cursor { get; set; }
}

public sealed class ListResourceNodesResult
{
    public string SessionId { get; set; } = string.Empty;

    public int PlanetId { get; set; }

    public long CapturedAtGameTick { get; set; }

    public string SnapshotId { get; set; } = string.Empty;

    public DateTimeOffset SnapshotExpiresAtUtc { get; set; }

    public List<ResourceNodeSnapshot> Nodes { get; set; } = new List<ResourceNodeSnapshot>();

    public string? NextCursor { get; set; }
}

public sealed class InspectResourceNodeRequest
{
    public int PlanetId { get; set; }

    public string Kind { get; set; } = string.Empty;

    public int NodeId { get; set; }
}

public sealed class ResourceNodeSnapshot
{
    public string SessionId { get; set; } = string.Empty;

    public int PlanetId { get; set; }

    public string Kind { get; set; } = string.Empty;

    public int NodeId { get; set; }

    public string ResourceType { get; set; } = string.Empty;

    public int ProtoId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int RemainingAmount { get; set; }

    public int GroupIndex { get; set; }

    public int MinerCount { get; set; }

    public Vector3Snapshot Position { get; set; } = new Vector3Snapshot();

    public float DistanceFromPlayer { get; set; }

    public bool SameLocalPlanet { get; set; }

    public bool WithinPlayerBuildArea { get; set; }

    public List<ResourceYieldSnapshot> Yields { get; set; } = new List<ResourceYieldSnapshot>();

    public long CapturedAtGameTick { get; set; }

    public string StateHash { get; set; } = string.Empty;

    public int StateHashVersion { get; set; } = 1;
}

public sealed class ResourceYieldSnapshot
{
    public int ItemId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Count { get; set; }

    public float Chance { get; set; }
}
