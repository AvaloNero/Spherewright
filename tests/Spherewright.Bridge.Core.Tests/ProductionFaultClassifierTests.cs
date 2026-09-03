using Spherewright.Bridge.Core.Diagnostics;
using Spherewright.Contracts.Diagnostics;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class ProductionFaultClassifierTests
{
    [Fact]
    public void ClassifyPrimary_DoesNotDiagnoseBeforeOneCompleteCycle()
    {
        var input = BaseInput();
        input.WindowElapsedGameTicks = 59;
        input.ExpectedCycleGameTicks = 60;
        input.Inputs = new[] { MissingMaterial() };

        Assert.Null(ProductionFaultClassifier.ClassifyPrimary(input));
    }

    [Fact]
    public void ClassifyPrimary_DistinguishesInsufficientPower()
    {
        var input = BaseInput();
        input.PowerServeRatio = 0.5;
        input.Inputs = new[] { MissingMaterial() };

        var finding = ProductionFaultClassifier.ClassifyPrimary(input);

        Assert.Equal(OverseerFindingKinds.InsufficientPower, finding?.Kind);
        Assert.Equal(OverseerFindingConfidences.Confirmed, finding?.Confidence);
    }

    [Fact]
    public void ClassifyPrimary_DistinguishesFullOutputBuffer()
    {
        var input = BaseInput();
        input.Outputs = new[]
        {
            new ProductionOutputState
            {
                ItemId = 6003,
                ItemName = "Structure matrix",
                BufferedCount = 10,
                BufferCapacity = 10,
            },
        };

        var finding = ProductionFaultClassifier.ClassifyPrimary(input);

        Assert.Equal(OverseerFindingKinds.OutputBlocked, finding?.Kind);
    }

    [Fact]
    public void ClassifyPrimary_DistinguishesExhaustedVeinFromGenericShortage()
    {
        var input = BaseInput();
        input.Inputs = new[]
        {
            new ProductionMaterialInput
            {
                ItemId = 1004,
                ItemName = "Silicon ore",
                RequiredPerCycle = 2,
                SourceResourceStateKnown = true,
                SourceResourceRemaining = 0,
            },
        };

        var finding = ProductionFaultClassifier.ClassifyPrimary(input);

        Assert.Equal(OverseerFindingKinds.VeinExhausted, finding?.Kind);
    }

    [Fact]
    public void ClassifyPrimary_DistinguishesMissingLogisticsConfiguration()
    {
        var input = BaseInput();
        var material = MissingMaterial();
        material.LogisticsExpected = true;
        material.LogisticsConfigured = false;
        input.Inputs = new[] { material };

        var finding = ProductionFaultClassifier.ClassifyPrimary(input);

        Assert.Equal(OverseerFindingKinds.LogisticsBlocked, finding?.Kind);
        Assert.Equal(OverseerFindingConfidences.Confirmed, finding?.Confidence);
    }

    [Fact]
    public void ClassifyPrimary_MarksStalledOrderAsSuspectedLogisticsBlock()
    {
        var input = BaseInput();
        var material = MissingMaterial();
        material.LogisticsExpected = true;
        material.LogisticsConfigured = true;
        material.LogisticsOrderOutstanding = true;
        material.LogisticsProgressStateKnown = true;
        material.LogisticsCarrierStateKnown = true;
        material.LogisticsCarrierCount = 4;
        material.LogisticsActiveRouteCarrierCount = 1;
        material.LogisticsProgressWindowElapsedGameTicks = 600;
        material.SourceInventoryKnown = true;
        material.SourceInventoryCount = 100;
        input.Inputs = new[] { material };

        var finding = ProductionFaultClassifier.ClassifyPrimary(input);

        Assert.Equal(OverseerFindingKinds.LogisticsBlocked, finding?.Kind);
        Assert.Equal(OverseerFindingConfidences.Suspected, finding?.Confidence);
    }

    [Fact]
    public void ClassifyPrimary_DoesNotInventShortageWhileLogisticsWindowWarmsUp()
    {
        var input = BaseInput();
        var material = MissingMaterial();
        material.LogisticsExpected = true;
        material.LogisticsConfigured = true;
        material.LogisticsOrderOutstanding = true;
        material.LogisticsCarrierStateKnown = true;
        material.LogisticsCarrierCount = 4;
        material.SourceInventoryKnown = true;
        material.SourceInventoryCount = 100;
        input.Inputs = new[] { material };

        var finding = ProductionFaultClassifier.ClassifyPrimary(input);

        Assert.Null(finding);
    }

    [Fact]
    public void ClassifyPrimary_DoesNotReportAFaultWhileLogisticsProgresses()
    {
        var input = BaseInput();
        var material = MissingMaterial();
        material.LogisticsExpected = true;
        material.LogisticsConfigured = true;
        material.LogisticsOrderOutstanding = true;
        material.LogisticsCarrierStateKnown = true;
        material.LogisticsCarrierCount = 4;
        material.LogisticsActiveRouteCarrierCount = 1;
        material.LogisticsProgressStateKnown = true;
        material.LogisticsProgressObserved = true;
        material.SourceInventoryKnown = true;
        material.SourceInventoryCount = 100;
        input.Inputs = new[] { material };

        Assert.Null(ProductionFaultClassifier.ClassifyPrimary(input));
    }

    [Fact]
    public void ClassifyPrimary_DoesNotTrustAKnownNoProgressFlagBeforeTheMinimumWindow()
    {
        var input = BaseInput();
        var material = MissingMaterial();
        material.LogisticsExpected = true;
        material.LogisticsConfigured = true;
        material.LogisticsOrderOutstanding = true;
        material.LogisticsCarrierStateKnown = true;
        material.LogisticsCarrierCount = 4;
        material.LogisticsProgressStateKnown = true;
        material.LogisticsProgressWindowElapsedGameTicks = 599;
        material.SourceInventoryKnown = true;
        material.SourceInventoryCount = 100;
        input.Inputs = new[] { material };

        Assert.Null(ProductionFaultClassifier.ClassifyPrimary(input));
    }

    [Fact]
    public void ClassifyPrimary_ConfirmsConfiguredRouteWithoutCarriers()
    {
        var input = BaseInput();
        var material = MissingMaterial();
        material.LogisticsExpected = true;
        material.LogisticsConfigured = true;
        material.LogisticsCarrierStateKnown = true;
        material.LogisticsCarrierCount = 0;
        material.SourceInventoryKnown = true;
        material.SourceInventoryCount = 100;
        material.LogisticsDemandPlanetId = 104;
        material.LogisticsDemandObjectId = 1657;
        material.LogisticsSupplyPlanetId = 102;
        material.LogisticsSupplyObjectId = 42;
        input.Inputs = new[] { material };

        var finding = ProductionFaultClassifier.ClassifyPrimary(input);

        Assert.Equal(OverseerFindingKinds.LogisticsBlocked, finding?.Kind);
        Assert.Contains(finding!.UpstreamPath, node => node.Kind == "logistics_demand" && node.ObjectId == 1657);
        Assert.Contains(finding.UpstreamPath, node => node.Kind == "logistics_supply" && node.PlanetId == 102);
        Assert.Contains(
            finding.Evidence,
            item => item.Metric == "source_inventory" && item.NumericValue == 100);
    }

    [Fact]
    public void ClassifyPrimary_DoesNotCallConsumedInputsAShortageDuringActiveCycle()
    {
        var input = BaseInput();
        input.IsWorking = true;
        input.Inputs = new[] { MissingMaterial() };

        Assert.Null(ProductionFaultClassifier.ClassifyPrimary(input));
    }

    [Fact]
    public void ClassifyPrimary_ReportsDepletedExtractorWhenProductIdentityWasLost()
    {
        var input = BaseInput();
        input.TargetItemId = 0;
        input.TargetItemName = string.Empty;
        input.IsResourceExtractor = true;
        input.ResourceStateKnown = true;
        input.RemainingResourceAmount = 0;

        var finding = ProductionFaultClassifier.ClassifyPrimary(input);

        Assert.Equal(OverseerFindingKinds.VeinExhausted, finding?.Kind);
        Assert.Null(finding?.ItemId);
        Assert.Null(finding?.UpstreamPath.Single().ItemId);
    }

    [Fact]
    public void ClassifyPrimary_DoesNotBlameCarrierFleetWhenSourceIsEmpty()
    {
        var input = BaseInput();
        var material = MissingMaterial();
        material.LogisticsExpected = true;
        material.LogisticsConfigured = true;
        material.LogisticsCarrierStateKnown = true;
        material.LogisticsCarrierCount = 0;
        material.SourceInventoryKnown = true;
        material.SourceInventoryCount = 0;
        input.Inputs = new[] { material };

        var finding = ProductionFaultClassifier.ClassifyPrimary(input);

        Assert.Equal(OverseerFindingKinds.MaterialShortage, finding?.Kind);
    }

    [Fact]
    public void ClassifyPrimary_UsesMaterialShortageWhenNoNarrowerCauseIsProven()
    {
        var input = BaseInput();
        input.Inputs = new[] { MissingMaterial() };

        var finding = ProductionFaultClassifier.ClassifyPrimary(input);

        Assert.Equal(OverseerFindingKinds.MaterialShortage, finding?.Kind);
        Assert.Equal(OverseerFindingConfidences.Confirmed, finding?.Confidence);
    }

    [Fact]
    public void ClassifyPrimary_DoesNotCallAProducingUnitBlocked()
    {
        var input = BaseInput();
        input.ActualProductionPerMinute = 60;
        input.Inputs = new[] { MissingMaterial() };

        Assert.Null(ProductionFaultClassifier.ClassifyPrimary(input));
    }

    private static ProductionFaultInput BaseInput()
    {
        return new ProductionFaultInput
        {
            PlanetId = 104,
            ObjectId = 774,
            TargetItemId = 6003,
            TargetItemName = "Structure matrix",
            ProductionUnitKind = "matrix_lab",
            ProductionUnitName = "Matrix lab 774",
            WindowState = OverseerWindowStates.Ready,
            WindowElapsedGameTicks = 600,
            ExpectedCycleGameTicks = 60,
            IsConfigured = true,
            PowerNetworkId = 1,
            PowerServeRatio = 1,
        };
    }

    private static ProductionMaterialInput MissingMaterial()
    {
        return new ProductionMaterialInput
        {
            ItemId = 1112,
            ItemName = "Diamond",
            AvailableCount = 0,
            RequiredPerCycle = 1,
        };
    }
}
