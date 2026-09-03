using Spherewright.Bridge.Core.Diagnostics;
using Spherewright.Contracts.Diagnostics;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class ProductionRootCauseTracerTests
{
    [Fact]
    public void TracePrimary_AppendsPhysicallyBoundUpstreamProducerAndRootMaterial()
    {
        var diamondReference = Reference(104, 715, 1112);
        var root = Input(104, 774, 6003, "matrix_lab", "Matrix lab 774", 1112, diamondReference);
        var diamond = Input(104, 715, 1112, "assembler", "Smelter 715", 1109);

        var finding = ProductionRootCauseTracer.TracePrimary(
            root,
            reference => Matches(reference, diamond) ? diamond : null);

        Assert.Equal(OverseerFindingKinds.MaterialShortage, finding?.Kind);
        Assert.Equal(1112, finding?.ItemId);
        Assert.Equal(715, finding?.ObjectId);
        Assert.Collection(
            finding!.UpstreamPath,
            node => AssertNode(node, "matrix_lab", 774, 6003),
            node => AssertNode(node, "material", null, 1112),
            node => AssertNode(node, "assembler", 715, 1112),
            node => AssertNode(node, "material", null, 1109));
    }

    [Fact]
    public void TracePrimary_FollowsAnExactProducerReferenceAcrossPlanets()
    {
        var titaniumIngotReference = Reference(102, 301, 1106);
        var root = Input(104, 530, 1126, "assembler", "Assembler 530", 1106, titaniumIngotReference);
        var rootMaterial = Assert.Single(root.Inputs);
        rootMaterial.LogisticsExpected = true;
        rootMaterial.LogisticsConfigured = true;
        rootMaterial.LogisticsDemandPlanetId = 104;
        rootMaterial.LogisticsDemandObjectId = 1657;
        rootMaterial.LogisticsSupplyPlanetId = 102;
        rootMaterial.LogisticsSupplyObjectId = 44;
        var remoteSmelter = Input(102, 301, 1106, "assembler", "Smelter 301", 1006);

        var finding = ProductionRootCauseTracer.TracePrimary(
            root,
            reference => Matches(reference, remoteSmelter) ? remoteSmelter : null);

        Assert.Equal(OverseerFindingKinds.MaterialShortage, finding?.Kind);
        Assert.Equal(102, finding?.PlanetId);
        Assert.Equal(301, finding?.ObjectId);
        Assert.Equal(1106, finding?.ItemId);
        Assert.Collection(
            finding!.UpstreamPath,
            node => AssertNode(node, "assembler", 530, 1126, 104),
            node => AssertNode(node, "material", null, 1106, 104),
            node => AssertNode(node, "logistics_demand", 1657, 1106, 104),
            node => AssertNode(node, "logistics_supply", 44, 1106, 102),
            node => AssertNode(node, "assembler", 301, 1106, 102),
            node => AssertNode(node, "material", null, 1006, 102));
    }

    [Fact]
    public void TracePrimary_UsesDeeperOutputBlockAsThePrimaryRootCause()
    {
        var reference = Reference(104, 715, 1112);
        var root = Input(104, 774, 6003, "matrix_lab", "Matrix lab 774", 1112, reference);
        var diamond = Input(104, 715, 1112, "assembler", "Smelter 715", 1109);
        diamond.Inputs = Array.Empty<ProductionMaterialInput>();
        diamond.Outputs = new[]
        {
            new ProductionOutputState
            {
                ItemId = 1112,
                BufferedCount = 100,
                BufferCapacity = 100,
            },
        };

        var finding = ProductionRootCauseTracer.TracePrimary(
            root,
            candidate => Matches(candidate, diamond) ? diamond : null);

        Assert.Equal(OverseerFindingKinds.OutputBlocked, finding?.Kind);
        Assert.Equal(715, finding?.ObjectId);
        Assert.Equal(3, finding?.UpstreamPath.Count);
    }

    [Fact]
    public void TracePrimary_StopsAtCyclesWithoutDuplicatingThePath()
    {
        var rootReference = Reference(104, 10, 1001);
        var secondReference = Reference(104, 20, 1002);
        var first = Input(104, 10, 1001, "assembler", "Assembler 10", 1002, secondReference);
        var second = Input(104, 20, 1002, "assembler", "Assembler 20", 1001, rootReference);

        var finding = ProductionRootCauseTracer.TracePrimary(
            first,
            reference => reference.ObjectId == 10 ? first : reference.ObjectId == 20 ? second : null);

        Assert.Equal(OverseerFindingKinds.MaterialShortage, finding?.Kind);
        Assert.Equal(20, finding?.ObjectId);
        Assert.Equal(4, finding?.UpstreamPath.Count);
        Assert.Contains(
            finding!.Evidence,
            evidence => evidence.Metric == "upstream_trace_stop_reason"
                && evidence.TextValue == "cycle_detected");
    }

    [Fact]
    public void TracePrimary_DoesNotFollowUnresolvedOrMismatchedReferences()
    {
        var reference = Reference(104, 715, 1112);
        var root = Input(104, 774, 6003, "matrix_lab", "Matrix lab 774", 1112, reference);
        var wrong = Input(102, 715, 1112, "assembler", "Wrong planet", 1109);

        var finding = ProductionRootCauseTracer.TracePrimary(root, _ => wrong);

        Assert.Equal(774, finding?.ObjectId);
        Assert.Equal(2, finding?.UpstreamPath.Count);
        Assert.Contains(
            finding!.Evidence,
            evidence => evidence.Metric == "upstream_trace_stop_reason"
                && evidence.TextValue == "unresolved_producer_reference");
    }

    [Fact]
    public void TracePrimary_CanClassifyCurrentUpstreamStateWithoutAnItemRate()
    {
        var reference = Reference(104, 715, 1112);
        var root = Input(104, 774, 6003, "matrix_lab", "Matrix lab 774", 1112, reference);
        var diamond = Input(104, 715, 1112, "assembler", "Smelter 715", 1109);
        diamond.ActualProductionPerMinute = 60d;
        diamond.ActualProductionStateKnown = false;

        var finding = ProductionRootCauseTracer.TracePrimary(root, _ => diamond);

        Assert.Equal(715, finding?.ObjectId);
        Assert.Equal(OverseerFindingKinds.MaterialShortage, finding?.Kind);
        Assert.Equal(OverseerFindingSeverities.Stopped, finding?.Severity);
    }

    [Fact]
    public void TracePrimary_ReportsMaximumDepthInsteadOfClaimingACompleteRoot()
    {
        var reference = Reference(104, 715, 1112);
        var root = Input(104, 774, 6003, "matrix_lab", "Matrix lab 774", 1112, reference);

        var finding = ProductionRootCauseTracer.TracePrimary(
            root,
            _ => throw new InvalidOperationException("The depth-zero trace must not resolve an upstream producer."),
            maximumDepth: 0);

        Assert.Equal(774, finding?.ObjectId);
        Assert.Contains(
            finding!.Evidence,
            evidence => evidence.Metric == "upstream_trace_stop_reason"
                && evidence.TextValue == "maximum_depth");
    }

    [Fact]
    public void TracePrimary_ReportsVisitedProducerLimitBeforeResolvingAnotherProducer()
    {
        var reference = Reference(104, 715, 1112);
        var root = Input(104, 774, 6003, "matrix_lab", "Matrix lab 774", 1112, reference);

        var finding = ProductionRootCauseTracer.TracePrimary(
            root,
            _ => throw new InvalidOperationException("The root already consumes the one-producer budget."),
            maximumVisitedProducers: 1);

        Assert.Equal(774, finding?.ObjectId);
        Assert.Contains(
            finding!.Evidence,
            evidence => evidence.Metric == "upstream_trace_stop_reason"
                && evidence.TextValue == "maximum_visited_producers");
    }

    private static ProductionFaultInput Input(
        int planetId,
        int objectId,
        int itemId,
        string kind,
        string name,
        int missingItemId,
        params ProductionUpstreamReference[] upstream)
    {
        return new ProductionFaultInput
        {
            PlanetId = planetId,
            ObjectId = objectId,
            TargetItemId = itemId,
            TargetItemName = $"item {itemId}",
            ProductionUnitKind = kind,
            ProductionUnitName = name,
            WindowState = OverseerWindowStates.Ready,
            WindowElapsedGameTicks = 600,
            ExpectedCycleGameTicks = 60,
            ActualProductionPerMinute = 0d,
            ActualProductionStateKnown = objectId == 774,
            IsConfigured = true,
            PowerNetworkId = 1,
            PowerServeRatio = 1d,
            Inputs = new[]
            {
                new ProductionMaterialInput
                {
                    ItemId = missingItemId,
                    ItemName = $"item {missingItemId}",
                    AvailableCount = 0,
                    RequiredPerCycle = 1,
                    UpstreamProducers = upstream,
                },
            },
        };
    }

    private static ProductionUpstreamReference Reference(int planetId, int objectId, int itemId) =>
        new ProductionUpstreamReference
        {
            PlanetId = planetId,
            ObjectId = objectId,
            ItemId = itemId,
        };

    private static bool Matches(ProductionUpstreamReference reference, ProductionFaultInput input) =>
        reference.PlanetId == input.PlanetId
        && reference.ObjectId == input.ObjectId
        && reference.ItemId == input.TargetItemId;

    private static void AssertNode(
        OverseerPathNodeSnapshot node,
        string kind,
        int? objectId,
        int itemId,
        int? planetId = null)
    {
        if (planetId.HasValue) Assert.Equal(planetId.Value, node.PlanetId);
        Assert.Equal(kind, node.Kind);
        Assert.Equal(objectId, node.ObjectId);
        Assert.Equal(itemId, node.ItemId);
    }
}
