using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

/// <summary>Copied identity evidence for the sole permitted device feed of an empty SOURCE path.</summary>
public sealed class UnpoweredMinerFeedEvidence
{
    public int ObjectId { get; set; }
    public int ItemId { get; set; }
    public int EntityMinerId { get; set; }
    public int MinerId { get; set; }
    public int MinerEntityId { get; set; }
    public int EntityConsumerId { get; set; }
    public int MinerConsumerId { get; set; }
    public int ConsumerId { get; set; }
    public int ConsumerEntityId { get; set; }
    public int NetworkId { get; set; }
    public float ServedRatio { get; set; }
    public bool IsVeinMiner { get; set; }
    public int ProductId { get; set; }
    public int ProductCount { get; set; }
    public int InsertTargetObjectId { get; set; }
    public int HeadObjectId { get; set; }
    public int HeadBeltId { get; set; }
    public int ResourceNodeCount { get; set; }
    public IReadOnlyList<UnpoweredMinerVeinEvidence> ResourceNodes { get; set; } = Array.Empty<UnpoweredMinerVeinEvidence>();
    public IReadOnlyList<FactoryConnectionSnapshot> MinerConnections { get; set; } = Array.Empty<FactoryConnectionSnapshot>();
    public IReadOnlyList<FactoryConnectionSnapshot> HeadConnections { get; set; } = Array.Empty<FactoryConnectionSnapshot>();
}

public sealed class UnpoweredMinerVeinEvidence
{
    public int NodeId { get; set; }
    public int NativeId { get; set; }
    public bool IsTitanium { get; set; }
    public int ProductId { get; set; }
    public int Amount { get; set; }
}

/// <summary>This exception never permits a destination feed, powered device, stock, or direct device-to-target join.</summary>
public static class UnpoweredMinerFeedPolicy
{
    public const int MaximumResourceNodes = 64;

    public static bool Supports(bool isSourcePath, UnpoweredMinerFeedEvidence? evidence)
    {
        if (!isSourcePath || evidence is null || !Positive(evidence.ObjectId) || evidence.ItemId != 2301
            || !Positive(evidence.EntityMinerId) || evidence.MinerId != evidence.EntityMinerId
            || evidence.MinerEntityId != evidence.ObjectId || !Positive(evidence.EntityConsumerId)
            || evidence.MinerConsumerId != evidence.EntityConsumerId || evidence.ConsumerId != evidence.EntityConsumerId
            || evidence.ConsumerEntityId != evidence.ObjectId || evidence.NetworkId != 0 || evidence.ServedRatio != 0 || !evidence.IsVeinMiner
            || evidence.ProductCount != 0 || (evidence.ProductId != 0 && evidence.ProductId != 1004)
            || !Positive(evidence.HeadObjectId) || evidence.HeadObjectId == evidence.ObjectId
            || !Positive(evidence.HeadBeltId) || evidence.InsertTargetObjectId != evidence.HeadObjectId
            || evidence.ResourceNodeCount < 1 || evidence.ResourceNodeCount > MaximumResourceNodes
            || evidence.ResourceNodes is null || evidence.ResourceNodes.Count != evidence.ResourceNodeCount
            || !Complete(evidence.MinerConnections) || !Complete(evidence.HeadConnections)) return false;

        var seen = new HashSet<int>();
        foreach (var vein in evidence.ResourceNodes)
            if (vein is null || !Positive(vein.NodeId) || !seen.Add(vein.NodeId) || vein.NativeId != vein.NodeId
                || !vein.IsTitanium || vein.ProductId != 1004 || vein.Amount <= 0) return false;

        var outlet = evidence.MinerConnections[0];
        var inlet = evidence.HeadConnections[1];
        return outlet.IsOutput && outlet.OtherObjectId == evidence.HeadObjectId && outlet.OtherSlot == 1
            && !inlet.IsOutput && inlet.OtherObjectId == evidence.ObjectId && inlet.OtherSlot == 0
            && evidence.MinerConnections.Skip(1).All(connection => connection.OtherObjectId == 0)
            && evidence.HeadConnections.Skip(2).Take(2).All(connection => connection.OtherObjectId == 0)
            && evidence.HeadConnections.Where(connection => connection.Slot != 1)
                .All(connection => connection.OtherObjectId == 0 || connection.IsOutput);
    }

    private static bool Positive(int id) => id > 0 && id <= BeltBuildOccupancyPolicy.MaximumFactorySlots;
    private static bool Complete(IReadOnlyList<FactoryConnectionSnapshot>? connections) => connections is not null
        && connections.Count == 16 && connections.Select((connection, slot) => connection is not null
            && connection.Slot == slot && connection.OtherObjectId >= 0
            && connection.OtherObjectId <= BeltBuildOccupancyPolicy.MaximumFactorySlots
            && connection.OtherSlot >= 0 && connection.OtherSlot < 16).All(valid => valid);
}
