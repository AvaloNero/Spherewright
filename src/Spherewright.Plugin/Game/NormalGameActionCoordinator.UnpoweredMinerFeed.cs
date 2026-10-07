using Spherewright.Bridge.Core.Factory;
using Spherewright.Bridge.Core.Safety;

namespace Spherewright.Plugin.Game;

internal sealed partial class NormalGameActionCoordinator
{
    // Value-only proof. The source miner is never removed, powered, or configured by a belt action.
    private sealed class UnpoweredMinerFeedState
    {
        internal int ObjectId;
        internal int HeadObjectId;
        internal string BindingHash = string.Empty;
    }

    private static bool TryCaptureUnpoweredMinerFeed(PlanetFactory factory, int headObjectId,
        out UnpoweredMinerFeedState? state)
    {
        state = null;
        var fields = new List<object?> { factory.planetId, factory.index };
        // The whole-path proof separately binds the head pose and validates its
        // native completion rotation. The feed identity must not freeze that rotation.
        if (!AppendBeltSourceObject(factory, headObjectId, new List<object?>())) return false;
        factory.ReadObjectConn(headObjectId, 1, out var output, out var objectId, out var otherSlot);
        if (output || otherSlot != 0 || !AppendBeltSourceObject(factory, objectId, fields)) return false;
        var entity = factory.entityPool[objectId];
        var head = factory.entityPool[headObjectId];
        var system = factory.factorySystem;
        var power = factory.powerSystem;
        if (entity.protoId != 2301 || head.protoId != 2001 || head.beltId <= 0
            || system?.minerPool is null || system.minerCursor < 1 || system.minerCursor > system.minerPool.Length
            || entity.minerId <= 0 || entity.minerId >= system.minerCursor
            || power?.consumerPool is null || power.consumerCursor < 1 || power.consumerCursor > power.consumerPool.Length
            || entity.powerConId <= 0 || entity.powerConId >= power.consumerCursor
            || power.networkServes is null || power.networkServes.Length < 1
            || factory.veinPool is null || factory.veinCursor < 1 || factory.veinCursor > factory.veinPool.Length) return false;
        var miner = system.minerPool[entity.minerId];
        var consumer = power.consumerPool[entity.powerConId];
        if (miner.veins is null || miner.veinCount < 1 || miner.veinCount > miner.veins.Length
            || miner.veinCount > UnpoweredMinerFeedPolicy.MaximumResourceNodes) return false;
        var veins = new List<UnpoweredMinerVeinEvidence>();
        for (var index = 0; index < miner.veinCount; index++)
        {
            var id = miner.veins[index];
            if (id <= 0 || id >= factory.veinCursor) return false;
            var vein = factory.veinPool[id];
            if (!IsFinite(vein.pos.x) || !IsFinite(vein.pos.y) || !IsFinite(vein.pos.z) || vein.pos.sqrMagnitude < 1) return false;
            veins.Add(new UnpoweredMinerVeinEvidence { NodeId = id, NativeId = vein.id,
                IsTitanium = vein.type == EVeinType.Titanium, ProductId = vein.productId, Amount = vein.amount });
            fields.Add(id); fields.Add(vein.id); fields.Add((int)vein.type); fields.Add(vein.productId);
            fields.Add(vein.groupIndex); fields.Add(vein.amount);
            fields.Add(vein.pos.x); fields.Add(vein.pos.y); fields.Add(vein.pos.z);
        }
        var evidence = new UnpoweredMinerFeedEvidence { ObjectId = objectId, ItemId = entity.protoId,
            EntityMinerId = entity.minerId, MinerId = miner.id, MinerEntityId = miner.entityId,
            EntityConsumerId = entity.powerConId, MinerConsumerId = miner.pcId,
            ConsumerId = consumer.id, ConsumerEntityId = consumer.entityId, NetworkId = consumer.networkId, ServedRatio = power.networkServes[0],
            IsVeinMiner = miner.type == EMinerType.Vein, ProductId = miner.productId, ProductCount = miner.productCount,
            InsertTargetObjectId = miner.insertTarget, HeadObjectId = headObjectId, HeadBeltId = head.beltId,
            ResourceNodeCount = miner.veinCount, ResourceNodes = veins,
            MinerConnections = ReadBeltJoinConnections(factory, objectId), HeadConnections = ReadBeltJoinConnections(factory, headObjectId) };
        if (!UnpoweredMinerFeedPolicy.Supports(true, evidence)) return false;
        fields.Add(entity.minerId); fields.Add(miner.id); fields.Add(miner.entityId); fields.Add(miner.pcId);
        fields.Add(entity.powerConId); fields.Add(consumer.id); fields.Add(consumer.entityId); fields.Add(consumer.networkId);
        fields.Add(consumer.requiredEnergy); fields.Add(power.networkServes[0]); fields.Add((int)miner.type); fields.Add(miner.productId);
        fields.Add(miner.productCount); fields.Add(miner.insertTarget); fields.Add(miner.veinCount);
        fields.Add(headObjectId); fields.Add(head.beltId);
        foreach (var connection in evidence.MinerConnections)
        { fields.Add(connection.Slot); fields.Add(connection.IsOutput); fields.Add(connection.OtherObjectId); fields.Add(connection.OtherSlot); }
        state = new UnpoweredMinerFeedState { ObjectId = objectId, HeadObjectId = headObjectId,
            BindingHash = CanonicalStateHash.Combine("empty-source-unpowered-ti-miner-v1", fields.ToArray()) };
        return true;
    }

    private static bool SameUnpoweredMinerFeed(UnpoweredMinerFeedState? before, UnpoweredMinerFeedState? after) =>
        before is null ? after is null : after is not null && before.ObjectId == after.ObjectId
            && before.HeadObjectId == after.HeadObjectId && BeltSourceReusePolicy.SameEvidence(before.BindingHash, after.BindingHash);
}
