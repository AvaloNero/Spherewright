using Spherewright.Bridge.Core.Diagnostics;
using Spherewright.Contracts.Diagnostics;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class ProductionLogisticsBoundaryTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(200)]
    public void StockedRouteRetainsConsumerShortageWithoutClaimingAnUpstreamCause(long stock)
    {
        var consumer = Consumer();
        var material = Assert.Single(consumer.Inputs);
        material.SourceInventoryCount = stock;

        var finding = ProductionRootCauseTracer.TracePrimary(consumer, _ =>
            throw new InvalidOperationException("Do not cross a stocked logistics boundary."));

        AssertBoundary(finding!, stock);
        Assert.Equal(530, finding!.ObjectId);
        Assert.Equal(104, finding.PlanetId);
        Assert.Equal(4, finding.UpstreamPath.Count);
        Assert.Contains(finding.UpstreamPath, node => node.Kind == "logistics_demand" && node.ObjectId == 1657);
        Assert.Contains(finding.UpstreamPath, node => node.Kind == "logistics_supply" && node.ObjectId == 44);
        // Even one item does not prove a full recipe, an available unreserved
        // shipment, or that native dispatch conditions would pass.
        Assert.DoesNotContain(finding.Evidence, evidence => evidence.Metric == "output_buffer_count");
    }

    [Theory]
    [InlineData(true, 0, true)]
    [InlineData(false, 200, true)]
    [InlineData(true, 200, false)]
    public void EmptyUnknownOrNonLogisticsSourcesKeepExistingProducerTracing(
        bool inventoryKnown, long stock, bool logisticsExpected)
    {
        var consumer = Consumer();
        var material = Assert.Single(consumer.Inputs);
        material.SourceInventoryKnown = inventoryKnown;
        material.SourceInventoryCount = stock;
        material.LogisticsExpected = logisticsExpected;
        var calls = 0;

        var finding = ProductionRootCauseTracer.TracePrimary(consumer, _ =>
        {
            calls++;
            return BlockedExtractor();
        });

        Assert.Equal(1, calls);
        Assert.Equal(OverseerFindingKinds.OutputBlocked, finding!.Kind);
        Assert.Equal(1, finding.ObjectId);
        Assert.DoesNotContain(finding.Evidence, evidence => evidence.Metric == "logistics_dispatch_state");
    }

    [Fact]
    public void ExistingNoFleetDiagnosisIsNotReplacedByAnUnprovenDispatchBoundary()
    {
        var consumer = Consumer();
        Assert.Single(consumer.Inputs).LogisticsCarrierCount = 0;

        var finding = ProductionRootCauseTracer.TracePrimary(consumer, _ => throw new InvalidOperationException());

        Assert.Equal(OverseerFindingKinds.LogisticsBlocked, finding!.Kind);
        Assert.Equal(OverseerFindingConfidences.Confirmed, finding.Confidence);
        Assert.DoesNotContain(finding.Evidence, evidence => evidence.Metric == "logistics_dispatch_state");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PendingOrProgressingOrdersRemainNonFindings(bool progressObserved)
    {
        var consumer = Consumer();
        var material = Assert.Single(consumer.Inputs);
        material.LogisticsOrderOutstanding = true;
        material.LogisticsProgressObserved = progressObserved;
        material.LogisticsProgressStateKnown = progressObserved;

        Assert.Null(ProductionRootCauseTracer.TracePrimary(consumer, _ => throw new InvalidOperationException()));
    }

    [Fact]
    public void QualifiedTemporalStallStillUsesExistingSuspectedLogisticsDiagnosis()
    {
        var consumer = Consumer();
        var material = Assert.Single(consumer.Inputs);
        material.LogisticsOrderOutstanding = true;
        material.LogisticsProgressStateKnown = true;
        material.LogisticsProgressWindowElapsedGameTicks = 600;

        var finding = ProductionRootCauseTracer.TracePrimary(consumer, _ => throw new InvalidOperationException());

        Assert.Equal(OverseerFindingKinds.LogisticsBlocked, finding!.Kind);
        Assert.Equal(OverseerFindingConfidences.Suspected, finding.Confidence);
        Assert.Contains(finding.Evidence, evidence => evidence.Metric == "logistics_progress_window" && evidence.NumericValue == 600);
    }

    [Fact]
    public void MissingProducerReferenceStillReportsTheObservedStockBoundary()
    {
        var consumer = Consumer();
        Assert.Single(consumer.Inputs).UpstreamProducers = Array.Empty<ProductionUpstreamReference>();

        var finding = ProductionRootCauseTracer.TracePrimary(consumer, _ => throw new InvalidOperationException());

        AssertBoundary(finding!, 200);
    }

    [Fact]
    public void NestedTraceKeepsTheStockedBoundaryAndItsExactExistingPath()
    {
        var smelter = Consumer();
        var root = Consumer();
        root.ObjectId = 767;
        root.TargetItemId = 1118;
        root.Inputs = new[]
        {
            new ProductionMaterialInput
            {
                ItemId = 1106,
                RequiredPerCycle = 1,
                UpstreamProducers = new[] { new ProductionUpstreamReference { PlanetId = 104, ObjectId = 530, ItemId = 1106 } },
            },
        };
        var calls = 0;

        var finding = ProductionRootCauseTracer.TracePrimary(root, reference =>
        {
            calls++;
            Assert.Equal(530, reference.ObjectId);
            return smelter;
        });

        Assert.Equal(1, calls);
        AssertBoundary(finding!, 200);
        Assert.Equal(530, finding!.ObjectId);
        Assert.Equal(767, finding.UpstreamPath[0].ObjectId);
        Assert.Equal(6, finding.UpstreamPath.Count);
        Assert.DoesNotContain(finding.UpstreamPath, node => node.ObjectId == 1);
    }

    private static void AssertBoundary(OverseerFindingSnapshot finding, long stock)
    {
        Assert.Equal(OverseerFindingKinds.MaterialShortage, finding.Kind);
        Assert.Equal(OverseerFindingConfidences.Confirmed, finding.Confidence);
        Assert.Contains(finding.Evidence, evidence => evidence.Metric == "source_inventory" && evidence.NumericValue == stock);
        Assert.Contains(finding.Evidence, evidence => evidence.Metric == "source_inventory_scope" && evidence.TextValue == "configured_route_supply_total");
        Assert.Contains(finding.Evidence, evidence => evidence.Metric == "logistics_dispatch_state" && evidence.TextValue == "unproven");
        Assert.Contains(finding.Evidence, evidence => evidence.Metric == "upstream_trace_stop_reason" && evidence.TextValue == "stocked_logistics_boundary");
    }

    private static ProductionFaultInput Consumer() => new()
    {
        PlanetId = 104,
        ObjectId = 530,
        TargetItemId = 1106,
        ProductionUnitKind = "assembler",
        WindowState = OverseerWindowStates.Ready,
        WindowElapsedGameTicks = 600,
        ExpectedCycleGameTicks = 60,
        IsConfigured = true,
        PowerNetworkId = 1,
        PowerServeRatio = 1,
        Inputs = new[]
        {
            new ProductionMaterialInput
            {
                ItemId = 1004,
                RequiredPerCycle = 4,
                LogisticsExpected = true,
                LogisticsConfigured = true,
                SourceInventoryKnown = true,
                SourceInventoryCount = 200,
                LogisticsCarrierStateKnown = true,
                LogisticsCarrierCount = 1,
                LogisticsDemandPlanetId = 104,
                LogisticsDemandObjectId = 1657,
                LogisticsSupplyPlanetId = 102,
                LogisticsSupplyObjectId = 44,
                UpstreamProducers = new[] { new ProductionUpstreamReference { PlanetId = 102, ObjectId = 1, ItemId = 1004 } },
            },
        },
    };

    private static ProductionFaultInput BlockedExtractor() => new()
    {
        PlanetId = 102,
        ObjectId = 1,
        TargetItemId = 1004,
        ProductionUnitKind = "resource_extractor",
        WindowState = OverseerWindowStates.Ready,
        WindowElapsedGameTicks = 600,
        ExpectedCycleGameTicks = 60,
        IsConfigured = true,
        PowerNetworkId = 1,
        PowerServeRatio = 1,
        Outputs = new[] { new ProductionOutputState { ItemId = 1004, BufferedCount = 50, BufferCapacity = 50 } },
    };
}
