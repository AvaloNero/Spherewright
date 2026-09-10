using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class FoundryPowerPlannerTests
{
    [Fact]
    public void ExistingFullWorkDemandNotJustIdleLoadReducesHeadroom()
    {
        var context = Context(); context.Networks[0].ReservedDemandPerTick = 990;
        var result = Assess(context);
        Assert.True(result.AllPlannedConsumersCovered);
        Assert.False(result.FullBaseLoadBudgetSatisfied);
        Assert.Equal(90, Assert.Single(result.Components).DeficitPerTick);
        Assert.Equal(6000, result.AdditionalBaseWorkPowerWatts);
    }

    [Fact]
    public void CoverageAndCapacityAreDistinct()
    {
        var context = Context(); context.Nodes[0].ProjectedPosition = V(50);
        var result = Assess(context);
        Assert.Equal(0, Assert.Single(result.UncoveredObjectIndices));
        Assert.False(result.FullBaseLoadBudgetSatisfied);
        Assert.Empty(result.Components);
    }

    [Fact]
    public void MultipleNodesOnOneNativeNetworkDoNotDoubleCapacityOrDemand()
    {
        var context = Context(); context.Nodes.Add(Node(2, 1, 2));
        var result = Assess(context);
        var budget = Assert.Single(result.Components);
        Assert.Equal(1000, budget.ExistingGenerationCapacityPerTick);
        Assert.Equal(200, budget.ExistingReservedDemandPerTick);
        Assert.Equal(100, budget.AddedBaseDemandPerTick);
        Assert.Equal(700, budget.HeadroomAfterPlanPerTick);
        Assert.True(result.FullBaseLoadBudgetSatisfied);
    }

    [Fact]
    public void ConsumerInTwoUnconnectedNetworksIsNotArbitrarilyAssigned()
    {
        var result = Assess(TwoNetworks());
        Assert.Equal(0, Assert.Single(result.AmbiguousObjectIndices));
        Assert.False(result.AllPlannedConsumersCovered);
        Assert.False(result.FullBaseLoadBudgetSatisfied);
        Assert.Empty(result.Components);
    }

    [Fact]
    public void PlannedNativeNodeCanJoinTwoExistingNetworks()
    {
        var result = Assess(TwoNetworks(), new[] { Obj(0, 2302), Obj(1, 2201) }, Catalog(wind: 500));
        var budget = Assert.Single(result.Components);
        Assert.Equal(new[] { 1, 2 }, budget.ExistingNetworkIds);
        Assert.Equal(2000, budget.ExistingGenerationCapacityPerTick);
        Assert.Equal(400, budget.ExistingReservedDemandPerTick);
        Assert.Equal(500, budget.AddedWindGenerationPerTick);
        Assert.Equal(2000, budget.HeadroomAfterPlanPerTick);
        Assert.True(result.FullBaseLoadBudgetSatisfied);
    }

    [Fact]
    public void NativeConnectDistanceIsMaximumNotSumOfRadii()
    {
        var context = Context(); context.Nodes[0].ProjectedPosition = V(15);
        context.Nodes[0].ConnectionDistanceSquared = 100; context.Nodes[0].CoverRadiusSquared = 4;
        var result = Assess(context, new[] { Obj(0, 2302), Obj(1, 2201) }, Catalog(wind: 0));
        var budget = Assert.Single(result.Components);
        Assert.Empty(budget.ExistingNetworkIds);
        Assert.Equal(100, budget.DeficitPerTick);
        Assert.False(result.FullBaseLoadBudgetSatisfied);
    }

    [Fact]
    public void UnknownGenerationIsNotCreditedAsWind()
    {
        var result = Assess(new FoundryPowerContext { PlacementShellRadius = 200 },
            new[] { Obj(0, 2302), Obj(1, 2201) }, Catalog(wind: null));
        Assert.Equal(0, Assert.Single(result.Components).AddedWindGenerationPerTick);
        Assert.False(result.FullBaseLoadBudgetSatisfied);
    }

    [Fact]
    public void FuelCatalogRatingDoesNotCreditUnfuelledPlannedGenerationOrChangeAssessmentHash()
    {
        var context = new FoundryPowerContext { PlacementShellRadius = 200 };
        var catalog = Catalog(wind: null);
        catalog[1].ItemId = 2211;
        var objects = new[] { Obj(0, 2302), Obj(1, 2211) };
        var before = Assess(context, objects, catalog);
        catalog[1].FuelPowerProfile = FuelPowerCatalogPolicy.Capture(2211, true, 50000, 60000, 2);
        var after = Assess(context, objects, catalog);
        Assert.Equal(before.AssessmentHash, after.AssessmentHash);
        Assert.Equal(0, Assert.Single(after.Components).AddedWindGenerationPerTick);
        Assert.Equal(100, Assert.Single(after.Components).DeficitPerTick);
        Assert.False(after.FullBaseLoadBudgetSatisfied);
    }

    [Fact]
    public void PlannedChargerReservesNativeFullWorkingDemandEvenWithoutConsumerComponent()
    {
        var catalog = Catalog(); var charger = catalog[1];
        charger.IsPowerCharger = true; charger.WorkEnergyPerTick = 300; charger.IdleEnergyPerTick = 10;
        var result = Assess(Context(), new[] { Obj(0, 2201) }, catalog);
        Assert.Equal(18000, result.AdditionalBaseWorkPowerWatts);
        Assert.Equal(300, Assert.Single(result.Components).AddedBaseDemandPerTick);
    }

    [Fact]
    public void NewNodePicksUpAndBudgetsOldUnpoweredConsumers()
    {
        var context = Context(); context.UnassignedConsumers.Add(new FoundryUnassignedPowerConsumer
        { ConsumerId = 9, ProjectedPosition = V(2), ReservedDemandPerTick = 900 });
        var result = Assess(context, new[] { Obj(0, 2302), Obj(1, 2201) });
        var budget = Assert.Single(result.Components);
        Assert.Equal(9, Assert.Single(budget.NewlyCoveredExistingConsumerIds));
        Assert.Equal(900, budget.NewlyCoveredExistingDemandPerTick);
        Assert.Equal(200, budget.DeficitPerTick);
    }

    [Fact]
    public void FarUnpoweredConsumerIsNotAnUnrelatedGlobalBlocker()
    {
        var context = Context(); context.UnassignedConsumers.Add(new FoundryUnassignedPowerConsumer
        { ConsumerId = 9, ProjectedPosition = V(100), ReservedDemandPerTick = 900 });
        var result = Assess(context, new[] { Obj(0, 2302), Obj(1, 2201) });
        Assert.Empty(Assert.Single(result.Components).NewlyCoveredExistingConsumerIds);
        Assert.True(result.FullBaseLoadBudgetSatisfied);
    }

    [Fact]
    public void TwoNewDisconnectedNodesCannotSilentlyChooseAnExistingUnpoweredConsumerNetwork()
    {
        var context = new FoundryPowerContext { PlacementShellRadius = 200 };
        context.UnassignedConsumers.Add(new FoundryUnassignedPowerConsumer
        { ConsumerId = 9, ProjectedPosition = V(), ReservedDemandPerTick = 900 });
        var catalog = Catalog(wind: 2000); catalog[1].PowerConnectDistance = 1;
        var result = Assess(context, new[] { Obj(0, 2201, -3), Obj(1, 2201, 3) }, catalog);
        Assert.Equal(9, Assert.Single(result.AmbiguousExistingConsumerIds));
        Assert.False(result.FullBaseLoadBudgetSatisfied);
    }

    [Fact]
    public void NativeProjectionUsesSurfaceShellNotAltitudeDifference()
    {
        var obj = Obj(0, 2302); obj.Position = new Vector3Snapshot { Y = 210 };
        var result = Assess(Context(), new[] { obj });
        Assert.True(result.AllPlannedConsumersCovered);
    }

    [Theory]
    [InlineData(9.999f)] [InlineData(10f)] [InlineData(10.001f)]
    public void ConsumerCoverageBoundaryIsExplicitlyUncertain(float x)
    {
        var context = Context(); context.Nodes[0].ProjectedPosition = V(x); context.Nodes[0].CoverRadiusSquared = 100;
        var result = Assess(context);
        Assert.True(result.GeometryBoundaryUncertain);
        Assert.False(result.FullBaseLoadBudgetSatisfied);
    }

    [Fact]
    public void PotentialNearBoundaryNetworkMergeCannotSilentlyIgnoreItsHeavyLoad()
    {
        var context = Context(); context.Nodes[0].ProjectedPosition = V(10); context.Nodes[0].ConnectionDistanceSquared = 100;
        context.Nodes[0].CoverRadiusSquared = 1; context.Networks[0].ReservedDemandPerTick = 100000;
        var result = Assess(context, new[] { Obj(0, 2302), Obj(1, 2201) }, Catalog(wind: 1000));
        Assert.True(result.GeometryBoundaryUncertain);
        Assert.False(result.FullBaseLoadBudgetSatisfied);
    }

    [Fact]
    public void NativeExportAlsoConsumesPowerBudget()
    {
        var context = Context(); context.Networks[0].ExportPerTick = 750;
        Assert.Equal(50, Assert.Single(Assess(context).Components).DeficitPerTick);
    }

    [Theory]
    [InlineData("capacity")] [InlineData("demand")] [InlineData("export")] [InlineData("position")]
    [InlineData("coverage")] [InlineData("tick")] [InlineData("unassigned")]
    public void AllRelevantCapturedEvidenceChangesAssessmentHash(string change)
    {
        var context = Context(); var before = Assess(context).AssessmentHash;
        if (change == "capacity") context.Networks[0].GenerationCapacityPerTick++;
        if (change == "demand") context.Networks[0].ReservedDemandPerTick++;
        if (change == "export") context.Networks[0].ExportPerTick++;
        if (change == "position") context.Nodes[0].ProjectedPosition.X++;
        if (change == "coverage") context.Nodes[0].CoverRadiusSquared++;
        if (change == "tick") context.CapturedAtGameTick++;
        if (change == "unassigned") context.UnassignedConsumers.Add(new FoundryUnassignedPowerConsumer
        { ConsumerId = 9, ProjectedPosition = V(100), ReservedDemandPerTick = 1 });
        Assert.NotEqual(before, Assess(context).AssessmentHash);
    }

    [Fact]
    public void NativeInputEnumerationOrderDoesNotChangeAssessmentHash()
    {
        var context = TwoNetworks(); var before = Assess(context).AssessmentHash;
        context.Nodes.Reverse(); context.Networks.Reverse();
        Assert.Equal(before, Assess(context).AssessmentHash);
    }

    [Theory]
    [InlineData("node_identity")] [InlineData("network_identity")] [InlineData("negative_energy")]
    [InlineData("overflow_energy")] [InlineData("nan_position")] [InlineData("unknown_profile")]
    [InlineData("unknown_work")] [InlineData("unknown_idle")] [InlineData("negative_wind")]
    [InlineData("missing_nodes")] [InlineData("object_index")] [InlineData("object_limit")]
    [InlineData("node_limit")] [InlineData("unassigned_limit")]
    public void IncompleteAmbiguousOrOutOfBoundsEvidenceFailsClosed(string change)
    {
        var context = Context(); var catalog = Catalog(); var objects = new[] { Obj(0, 2302), Obj(1, 2201) };
        if (change == "node_identity") context.Nodes.Add(context.Nodes[0]);
        if (change == "network_identity") context.Networks.Add(context.Networks[0]);
        if (change == "negative_energy") context.Networks[0].ReservedDemandPerTick = -1;
        if (change == "overflow_energy") context.Networks[0].GenerationCapacityPerTick = long.MaxValue;
        if (change == "nan_position") objects[0].Position.X = float.NaN;
        if (change == "unknown_profile") catalog[0].NativePowerProfileKnown = false;
        if (change == "unknown_work") catalog[0].WorkEnergyPerTick = null;
        if (change == "unknown_idle") catalog[0].IdleEnergyPerTick = null;
        if (change == "negative_wind") catalog[1].WindGenerationAtCurrentPlanetPerTick = -1;
        if (change == "missing_nodes") context.Nodes.Clear();
        if (change == "object_index") objects[0].Index = 2;
        if (change == "object_limit") objects = Enumerable.Range(0, 33).Select(i => Obj(i, 2302)).ToArray();
        if (change == "node_limit") context.Nodes = Enumerable.Range(1, 513).Select(i => Node(i, 1, i)).ToList();
        if (change == "unassigned_limit") context.UnassignedConsumers = Enumerable.Range(1, 2049).Select(i =>
            new FoundryUnassignedPowerConsumer { ConsumerId = i, ProjectedPosition = V() }).ToList();
        Assert.Throws<FoundryPlanningException>(() => Assess(context, objects, catalog));
    }

    private static FoundryPowerAssessment Assess(FoundryPowerContext context,
        IReadOnlyList<BlueprintSiteObject>? objects = null, IReadOnlyList<BuildCatalogItem>? catalog = null) =>
        FoundryPowerPlanner.Assess(objects ?? new[] { Obj(0, 2302) }, catalog ?? Catalog(), context);
    private static FoundryPowerContext Context() => new FoundryPowerContext
    {
        PlacementShellRadius = 200, CapturedAtGameTick = 900,
        Nodes = new List<FoundryExistingPowerNode> { Node(1, 1, 0) },
        Networks = new List<FoundryExistingPowerNetwork> { Network(1) },
    };
    private static FoundryPowerContext TwoNetworks()
    {
        var context = Context(); context.Nodes[0].ProjectedPosition = V(-3);
        context.Nodes.Add(Node(2, 2, 3)); context.Networks.Add(Network(2)); return context;
    }
    private static FoundryExistingPowerNetwork Network(int id) => new FoundryExistingPowerNetwork
    { NetworkId = id, GenerationCapacityPerTick = 1000, ReservedDemandPerTick = 200 };
    private static FoundryExistingPowerNode Node(int id, int network, float x) => new FoundryExistingPowerNode
    { NodeId = id, NetworkId = network, ProjectedPosition = V(x), ConnectionDistanceSquared = 100, CoverRadiusSquared = 64 };
    private static BuildCatalogItem[] Catalog(long? wind = 0) => new[]
    {
        new BuildCatalogItem { ItemId = 2302, NativePowerProfileKnown = true, IsPowerConsumer = true, WorkEnergyPerTick = 100, IdleEnergyPerTick = 10 },
        new BuildCatalogItem { ItemId = 2201, NativePowerProfileKnown = true, IsPowerNode = true, PowerCoverRadius = 8, PowerConnectDistance = 10,
            WindGenerationAtCurrentPlanetPerTick = wind },
    };
    private static BlueprintSiteObject Obj(int index, int item, float x = 0) => new BlueprintSiteObject { Index = index, ItemId = item, Position = V(x) };
    private static Vector3Snapshot V(float x = 0) => new Vector3Snapshot { X = x, Y = 200 };
}
