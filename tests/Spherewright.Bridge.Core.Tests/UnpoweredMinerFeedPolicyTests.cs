using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class UnpoweredMinerFeedPolicyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1004)]
    public void AllowsOnlyAnUnpoweredTitaniumVeinMinerAtTheSourceHead(int productId)
    {
        var evidence = Evidence();
        evidence.ProductId = productId;

        Assert.True(UnpoweredMinerFeedPolicy.Supports(isSourcePath: true, evidence));
        Assert.False(UnpoweredMinerFeedPolicy.Supports(isSourcePath: false, evidence));
    }

    [Fact]
    public void OrdinaryUnfedPathDoesNotNeedThisDeviceFeedException() =>
        Assert.False(UnpoweredMinerFeedPolicy.Supports(isSourcePath: true, evidence: null));

    [Theory]
    [InlineData("entity_miner")]
    [InlineData("miner_entity")]
    [InlineData("entity_consumer")]
    [InlineData("miner_consumer")]
    [InlineData("consumer_id")]
    [InlineData("consumer_entity")]
    [InlineData("head_identity")]
    [InlineData("insert_target_object_id")]
    [InlineData("insert_target_belt_id")]
    public void RejectsMismatchedMinerConsumerAndEndpointIdentity(string mismatch)
    {
        var evidence = Evidence();
        switch (mismatch)
        {
            case "entity_miner": evidence.EntityMinerId++; break;
            case "miner_entity": evidence.MinerEntityId++; break;
            case "entity_consumer": evidence.EntityConsumerId++; break;
            case "miner_consumer": evidence.MinerConsumerId++; break;
            case "consumer_id": evidence.ConsumerId++; break;
            case "consumer_entity": evidence.ConsumerEntityId++; break;
            case "head_identity": evidence.HeadObjectId++; break;
            case "insert_target_object_id": evidence.InsertTargetObjectId++; break;
            case "insert_target_belt_id": evidence.InsertTargetObjectId = evidence.HeadBeltId; break;
        }

        Assert.False(UnpoweredMinerFeedPolicy.Supports(isSourcePath: true, evidence));
    }

    [Theory]
    [InlineData("connected_network")]
    [InlineData("nonzero_serve")]
    [InlineData("nan_serve")]
    [InlineData("infinite_serve")]
    [InlineData("stocked_miner")]
    [InlineData("wrong_output_item")]
    [InlineData("wrong_build_item")]
    [InlineData("not_vein_miner")]
    public void RejectsPowerStockOrAnotherMinerKind(string fault)
    {
        var evidence = Evidence();
        switch (fault)
        {
            case "connected_network": evidence.NetworkId = 1; break;
            case "nonzero_serve": evidence.ServedRatio = 0.01f; break;
            case "nan_serve": evidence.ServedRatio = float.NaN; break;
            case "infinite_serve": evidence.ServedRatio = float.PositiveInfinity; break;
            case "stocked_miner": evidence.ProductCount = 50; break;
            case "wrong_output_item": evidence.ProductId = 1201; break;
            case "wrong_build_item": evidence.ItemId = 2302; break;
            case "not_vein_miner": evidence.IsVeinMiner = false; break;
        }

        Assert.False(UnpoweredMinerFeedPolicy.Supports(isSourcePath: true, evidence));
    }

    [Fact]
    public void AcceptsTheMaximumBoundedNonemptyTitaniumNodeSet()
    {
        var evidence = Evidence();
        evidence.ResourceNodes = Enumerable.Range(1, UnpoweredMinerFeedPolicy.MaximumResourceNodes)
            .Select(Node).ToArray();
        evidence.ResourceNodeCount = evidence.ResourceNodes.Count;

        Assert.True(UnpoweredMinerFeedPolicy.Supports(isSourcePath: true, evidence));
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("count_mismatch")]
    [InlineData("over_limit")]
    [InlineData("null_nodes")]
    [InlineData("duplicate_node")]
    [InlineData("wrong_native_id")]
    [InlineData("not_titanium")]
    [InlineData("wrong_product")]
    [InlineData("zero_amount")]
    [InlineData("negative_amount")]
    [InlineData("invalid_node_id")]
    public void RejectsEmptyMalformedOrNonTitaniumResourceEvidence(string fault)
    {
        var evidence = Evidence();
        switch (fault)
        {
            case "empty": evidence.ResourceNodes = Array.Empty<UnpoweredMinerVeinEvidence>(); evidence.ResourceNodeCount = 0; break;
            case "count_mismatch": evidence.ResourceNodeCount++; break;
            case "over_limit":
                evidence.ResourceNodes = Enumerable.Range(1, UnpoweredMinerFeedPolicy.MaximumResourceNodes + 1).Select(Node).ToArray();
                evidence.ResourceNodeCount = evidence.ResourceNodes.Count;
                break;
            case "null_nodes": evidence.ResourceNodes = null!; break;
            case "duplicate_node": evidence.ResourceNodes = new[] { Node(1), Node(1) }; evidence.ResourceNodeCount = 2; break;
            case "wrong_native_id": evidence.ResourceNodes = new[] { new UnpoweredMinerVeinEvidence { NodeId = 1, NativeId = 2, IsTitanium = true, ProductId = 1004, Amount = 1 } }; evidence.ResourceNodeCount = 1; break;
            case "not_titanium": evidence.ResourceNodes = new[] { new UnpoweredMinerVeinEvidence { NodeId = 1, NativeId = 1, IsTitanium = false, ProductId = 1004, Amount = 1 } }; evidence.ResourceNodeCount = 1; break;
            case "wrong_product": evidence.ResourceNodes = new[] { new UnpoweredMinerVeinEvidence { NodeId = 1, NativeId = 1, IsTitanium = true, ProductId = 1005, Amount = 1 } }; evidence.ResourceNodeCount = 1; break;
            case "zero_amount": evidence.ResourceNodes = new[] { new UnpoweredMinerVeinEvidence { NodeId = 1, NativeId = 1, IsTitanium = true, ProductId = 1004, Amount = 0 } }; evidence.ResourceNodeCount = 1; break;
            case "negative_amount": evidence.ResourceNodes = new[] { new UnpoweredMinerVeinEvidence { NodeId = 1, NativeId = 1, IsTitanium = true, ProductId = 1004, Amount = -1 } }; evidence.ResourceNodeCount = 1; break;
            case "invalid_node_id": evidence.ResourceNodes = new[] { new UnpoweredMinerVeinEvidence { NodeId = 0, NativeId = 0, IsTitanium = true, ProductId = 1004, Amount = 1 } }; evidence.ResourceNodeCount = 1; break;
        }

        Assert.False(UnpoweredMinerFeedPolicy.Supports(isSourcePath: true, evidence));
    }

    [Theory]
    [InlineData("wrong_source_direction")]
    [InlineData("wrong_source_peer")]
    [InlineData("wrong_source_peer_slot")]
    [InlineData("wrong_head_direction")]
    [InlineData("wrong_head_peer")]
    [InlineData("wrong_head_peer_slot")]
    [InlineData("extra_miner_input")]
    [InlineData("extra_miner_output")]
    [InlineData("extra_head_input")]
    [InlineData("prebuild_connection")]
    [InlineData("bad_slot_index")]
    [InlineData("null_miner_connections")]
    [InlineData("short_miner_connections")]
    [InlineData("short_head_connections")]
    [InlineData("null_head_connections")]
    public void RejectsNonreciprocalAdditionalOrMalformedDeviceConnections(string fault)
    {
        var evidence = Evidence();
        switch (fault)
        {
            case "wrong_source_direction": evidence.MinerConnections[0].IsOutput = false; break;
            case "wrong_source_peer": evidence.MinerConnections[0].OtherObjectId++; break;
            case "wrong_source_peer_slot": evidence.MinerConnections[0].OtherSlot = 2; break;
            case "wrong_head_direction": evidence.HeadConnections[1].IsOutput = true; break;
            case "wrong_head_peer": evidence.HeadConnections[1].OtherObjectId++; break;
            case "wrong_head_peer_slot": evidence.HeadConnections[1].OtherSlot = 2; break;
            case "extra_miner_input": evidence.MinerConnections[2].OtherObjectId = 901; evidence.MinerConnections[2].OtherSlot = 0; break;
            case "extra_miner_output": evidence.MinerConnections[2].IsOutput = true; evidence.MinerConnections[2].OtherObjectId = 901; evidence.MinerConnections[2].OtherSlot = 0; break;
            case "extra_head_input": evidence.HeadConnections[2].OtherObjectId = 901; evidence.HeadConnections[2].OtherSlot = 0; break;
            case "prebuild_connection": evidence.MinerConnections[2].OtherObjectId = -1; break;
            case "bad_slot_index": evidence.HeadConnections[1].Slot = 3; break;
            case "null_miner_connections": evidence.MinerConnections = null!; break;
            case "short_miner_connections": evidence.MinerConnections = evidence.MinerConnections.Take(15).ToArray(); break;
            case "short_head_connections": evidence.HeadConnections = evidence.HeadConnections.Take(15).ToArray(); break;
            case "null_head_connections": evidence.HeadConnections = null!; break;
        }

        Assert.False(UnpoweredMinerFeedPolicy.Supports(isSourcePath: true, evidence));
    }

    [Fact]
    public void AllowsExistingHeadOutputsButNoAdditionalHeadInputs()
    {
        var evidence = Evidence();
        evidence.HeadConnections[4].IsOutput = true;
        evidence.HeadConnections[4].OtherObjectId = 902;
        evidence.HeadConnections[4].OtherSlot = 0;

        Assert.True(UnpoweredMinerFeedPolicy.Supports(isSourcePath: true, evidence));

        evidence.HeadConnections[5].OtherObjectId = 903;
        evidence.HeadConnections[5].OtherSlot = 0;
        Assert.False(UnpoweredMinerFeedPolicy.Supports(isSourcePath: true, evidence));
    }

    [Fact]
    public void RejectsNativeBeltSideInputSlotAsAdditionalHeadOutput()
    {
        var evidence = Evidence();
        evidence.HeadConnections[3].IsOutput = true;
        evidence.HeadConnections[3].OtherObjectId = 902;
        evidence.HeadConnections[3].OtherSlot = 0;

        Assert.False(UnpoweredMinerFeedPolicy.Supports(isSourcePath: true, evidence));
    }

    private static UnpoweredMinerFeedEvidence Evidence() => new()
    {
        ObjectId = 754,
        ItemId = 2301,
        EntityMinerId = 601,
        MinerId = 601,
        MinerEntityId = 754,
        EntityConsumerId = 602,
        MinerConsumerId = 602,
        ConsumerId = 602,
        ConsumerEntityId = 754,
        NetworkId = 0,
        ServedRatio = 0,
        IsVeinMiner = true,
        ProductId = 0,
        ProductCount = 0,
        InsertTargetObjectId = 755,
        HeadObjectId = 755,
        HeadBeltId = 1755,
        ResourceNodeCount = 2,
        ResourceNodes = new[] { Node(101), Node(102) },
        MinerConnections = Connections(0, isOutput: true, 755, 1),
        HeadConnections = Connections(1, isOutput: false, 754, 0)
    };

    private static UnpoweredMinerVeinEvidence Node(int id) => new()
    {
        NodeId = id,
        NativeId = id,
        IsTitanium = true,
        ProductId = 1004,
        Amount = 1
    };

    private static List<FactoryConnectionSnapshot> Connections(int slot, bool isOutput, int otherObjectId, int otherSlot)
    {
        var connections = Enumerable.Range(0, 16).Select(index => new FactoryConnectionSnapshot { Slot = index }).ToList();
        connections[slot].IsOutput = isOutput;
        connections[slot].OtherObjectId = otherObjectId;
        connections[slot].OtherSlot = otherSlot;
        return connections;
    }
}
