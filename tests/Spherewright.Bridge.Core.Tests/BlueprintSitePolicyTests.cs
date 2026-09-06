using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class BlueprintSitePolicyTests
{
    private static readonly Dictionary<int, int> Slots = new() { [2302] = 12, [2303] = 12, [2101] = 12 };

    [Fact]
    public void AdvisoryPowerSamplingDoesNotMakeUnchangedNativeSiteStaleEachTick()
    {
        var site = new BlueprintSiteSnapshot { SessionId = "owned", PlanetId = 104, BlueprintHash = "blueprint" };
        var before = BlueprintSitePolicy.AssessmentHash(site, "player");
        site.Power = new FoundryPowerAssessment { CapturedAtGameTick = 900, AssessmentHash = "power-only", State = "unavailable" };
        Assert.Equal(before, BlueprintSitePolicy.AssessmentHash(site, "player"));
        site.Power.CapturedAtGameTick++; site.Power.AssessmentHash = "next-power-capture";
        Assert.Equal(before, BlueprintSitePolicy.AssessmentHash(site, "player"));
    }

    [Fact]
    public void ClosedSorterModuleHasUniquePortsAndDependencyOrder()
    {
        var module = Module();
        var edges = BlueprintSitePolicy.BuildConnections(module, Slots);
        Assert.Equal(2, edges.Count);
        Assert.Equal(new[] { 0, 1, 2 }, BlueprintSitePolicy.ExecutionOrder(module));
        Assert.Equal(new[] { 0, 1 }, BlueprintSitePolicy.Dependencies(module, 2));
        Assert.Equal(-1, edges[0].FromSlot);
        Assert.Equal(3, edges[1].ToSlot);
    }

    [Theory]
    [InlineData(-1)] [InlineData(4)] [InlineData(int.MaxValue)]
    public void RejectsInvalidQuarterTurns(int turns) => Assert.Throws<BlueprintReadException>(() =>
        BlueprintSitePolicy.ValidateRequest(new BlueprintSiteRequest { QuarterTurns = turns, ExpectedPlayerStateHash = "fresh" }));

    [Theory]
    [InlineData(float.NaN)] [InlineData(float.PositiveInfinity)] [InlineData(10001)]
    public void RejectsUnboundedPosition(float x) => Assert.Throws<BlueprintReadException>(() =>
        BlueprintSitePolicy.ValidateRequest(new BlueprintSiteRequest { Position = new Vector3Snapshot { X = x }, ExpectedPlayerStateHash = "fresh" }));

    [Fact]
    public void RequiresFreshPlayerAndKnownHashVersion()
    {
        Assert.Throws<BlueprintReadException>(() => BlueprintSitePolicy.ValidateRequest(new BlueprintSiteRequest()));
        Assert.Throws<BlueprintReadException>(() => BlueprintSitePolicy.ValidateRequest(new BlueprintSiteRequest
            { ExpectedPlayerStateHash = "fresh", StateHashVersion = 2 }));
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void RejectsOpenSorterInsteadOfMatchingAnUnapprovedExternalEntity(bool input)
    {
        var module = Module();
        if (input) module.Objects[2].InputObjectIndex = -1; else module.Objects[2].OutputObjectIndex = -1;
        Assert.Equal("blueprint_site_open_sorter_unsupported", Reject(module));
    }

    [Theory]
    [InlineData(-1)] [InlineData(12)] [InlineData(15)]
    public void ChecksNativeMachineSlotLengthBeforeNativeTranslation(int slot)
    {
        var module = Module(); module.Objects[2].OutputToSlot = slot;
        Assert.Equal("blueprint_site_native_slot_invalid", Reject(module));
    }

    [Theory]
    [InlineData(0)] [InlineData(4)] [InlineData(11)]
    public void RequiresNativeVirtualBeltSlotForSorterEdges(int slot)
    {
        var module = Module(); module.Objects[2].InputFromSlot = slot;
        Assert.Equal("blueprint_site_native_slot_invalid", Reject(module));
    }

    [Fact]
    public void RejectsTwoSortersSharingSameMachineSlot()
    {
        var module = Module(); module.Objects.Add(Sorter(3, 0, 1, 3));
        Assert.Equal("blueprint_site_slot_conflict", Reject(module));
    }

    [Fact]
    public void EightVirtualSorterLinksFitButNinthIsRejected()
    {
        var module = Module(); module.Objects.RemoveAt(2);
        for (var i = 0; i < 8; i++) module.Objects.Add(Sorter(i + 2, 0, 1, i));
        Assert.Equal(16, BlueprintSitePolicy.BuildConnections(module, Slots).Count);
        module.Objects.Add(Sorter(10, 0, 1, 8));
        Assert.Equal("blueprint_site_virtual_slots_exhausted", Reject(module));
    }

    [Fact]
    public void RejectsCyclicBeltConstructionDependencies()
    {
        var module = new BlueprintInspection { Objects = new List<BlueprintObjectSnapshot> { Belt(0, 1), Belt(1, 0) } };
        Assert.Equal("blueprint_site_dependency_cycle_unsupported", Reject(module));
    }

    [Fact]
    public void DownstreamBeltIsBuiltBeforeUpstream()
    {
        var module = new BlueprintInspection { Objects = new List<BlueprintObjectSnapshot> { Belt(0, 1), Belt(1, 2), Belt(2, -1) } };
        Assert.Equal(2, BlueprintSitePolicy.BuildConnections(module, Slots).Count);
        Assert.Equal(new[] { 2, 1, 0 }, BlueprintSitePolicy.ExecutionOrder(module));
    }

    [Fact]
    public void RejectsUnsupportedTypesAndMoreThan32PlacementObjects()
    {
        var module = Module(); module.Objects[1].ItemId = 2103;
        Assert.Equal("blueprint_site_type_unsupported", Reject(module));
        module.Objects = Enumerable.Range(0, 33).Select(i => Belt(i, -1)).ToList();
        Assert.Equal("blueprint_site_object_limit", Reject(module));
    }

    [Fact]
    public void HashBindsSiteStateGeometryMaterialRecipeAndTopology()
    {
        var site = new BlueprintSiteSnapshot { SessionId = "owned", PlanetId = 104, BlueprintHash = "blueprint",
            Objects = new List<BlueprintSiteObject> { new() { ItemId = 2303, Parameters = new[] { 0 } } },
            ConstructionItems = new List<FoundryInventoryBudget> { new() { ItemId = 2303, RequiredCount = 1, PackageCount = 1 } } };
        var hash = BlueprintSitePolicy.AssessmentHash(site, "player");
        Assert.Equal(hash, BlueprintSitePolicy.AssessmentHash(site, "player"));
        site.CapturedAtGameTick++; Assert.Equal(hash, BlueprintSitePolicy.AssessmentHash(site, "player"));
        Assert.NotEqual(hash, BlueprintSitePolicy.AssessmentHash(site, "changed-player"));
        AssertChanged(() => site.Revision++, () => site.Revision--);
        AssertChanged(() => site.QuarterTurns++, () => site.QuarterTurns--);
        AssertChanged(() => site.Objects[0].RecipeId++, () => site.Objects[0].RecipeId--);
        AssertChanged(() => site.Objects[0].Position.X++, () => site.Objects[0].Position.X--);
        AssertChanged(() => site.Objects[0].Rotation.W++, () => site.Objects[0].Rotation.W--);
        AssertChanged(() => site.Objects[0].Parameters[0]++, () => site.Objects[0].Parameters[0]--);
        AssertChanged(() => site.ConstructionItems[0].PackageCount--, () => site.ConstructionItems[0].PackageCount++);
        site.Connections.Add(new BlueprintPlanConnection());
        Assert.NotEqual(hash, BlueprintSitePolicy.AssessmentHash(site, "player"));
        Assert.False(site.Executable);
        void AssertChanged(Action change, Action undo) { change(); Assert.NotEqual(hash, BlueprintSitePolicy.AssessmentHash(site, "player")); undo(); }
    }

    private static string Reject(BlueprintInspection module) => Assert.Throws<BlueprintReadException>(() =>
        BlueprintSitePolicy.BuildConnections(module, Slots)).Reason;

    [Fact]
    public void ClosedStorageProductionModuleBuildsDevicesBeforeSorters()
    {
        var module = StorageModule();
        var site = Site(module);
        Assert.Equal(4, site.Connections.Count);
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, BlueprintSitePolicy.ExecutionOrder(module));
        foreach (var index in new[] { 0, 1, 2 })
        {
            Assert.Null(BlueprintSitePolicy.CreationInput(site, index));
            Assert.Null(BlueprintSitePolicy.CreationOutput(site, index));
        }
        Assert.Equal((0, 3, 3, 1), (site.Connections[0].FromIndex, site.Connections[0].FromSlot,
            site.Connections[0].ToIndex, site.Connections[0].ToSlot));
        Assert.Equal(2, BlueprintSitePolicy.CreationOutput(site, 4)!.ToIndex);
    }

    [Theory]
    [InlineData(-1)] [InlineData(12)] [InlineData(14)] [InlineData(15)]
    public void StorageRequiresOrdinaryNativePortsNotVirtualOrStackLinks(int port)
    {
        var module = StorageModule(); module.Objects[3].InputFromSlot = port;
        Assert.Equal("blueprint_site_native_slot_invalid", Reject(module));
    }

    [Fact]
    public void RejectsStorageStackingAndDuplicateSorterPort()
    {
        var module = StorageModule(); module.Objects[0].OutputObjectIndex = 2;
        Assert.Equal("blueprint_site_device_link_unsupported", Reject(module));
        module.Objects[0].OutputObjectIndex = -1;
        module.Objects[4].OutputObjectIndex = 0; module.Objects[4].OutputToSlot = 3;
        Assert.Equal("blueprint_site_slot_conflict", Reject(module));
    }

    private static BlueprintInspection StorageModule() => new()
    {
        Objects = new List<BlueprintObjectSnapshot>
        {
            new() { Index = 0, ItemId = 2101, InputObjectIndex = -1, OutputObjectIndex = -1, Parameters = new int[110] },
            new() { Index = 1, ItemId = 2302, InputObjectIndex = -1, OutputObjectIndex = -1 },
            new() { Index = 2, ItemId = 2101, InputObjectIndex = -1, OutputObjectIndex = -1, Parameters = new int[110] },
            new() { Index = 3, ItemId = 2011, InputObjectIndex = 0, InputFromSlot = 3, InputToSlot = 1,
                OutputObjectIndex = 1, OutputFromSlot = 0, OutputToSlot = 8 },
            new() { Index = 4, ItemId = 2011, InputObjectIndex = 1, InputFromSlot = 0, InputToSlot = 1,
                OutputObjectIndex = 2, OutputFromSlot = 0, OutputToSlot = 4 },
        },
    };

    [Theory]
    [InlineData(false, 1)] [InlineData(true, 1)]
    [InlineData(false, 2)] [InlineData(true, 2)]
    public void BeltCreationKeepsDownstreamOutputWithPickupSortersInAnyEdgeOrder(bool reverse, int sorterCount)
    {
        var module = Module();
        module.Objects.Add(Belt(3, -1));
        module.Objects[0].OutputObjectIndex = 3;
        if (sorterCount == 2) module.Objects.Add(Sorter(4, 0, 1, 4));
        var site = Site(module);
        if (reverse) site.Connections.Reverse();
        var before = site.Connections.ToArray();
        var output = BlueprintSitePolicy.CreationOutput(site, 0);
        Assert.NotNull(output);
        Assert.Equal((0, 0, 3, 1), (output.FromIndex, output.FromSlot, output.ToIndex, output.ToSlot));
        Assert.Null(BlueprintSitePolicy.CreationInput(site, 0));
        Assert.Null(BlueprintSitePolicy.CreationInput(site, 3)); // Upstream belt owns this link.
        Assert.Equal(before, site.Connections);
    }

    [Fact]
    public void FreeBeltAndMachineDoNotInstallTheirAttachedSorterLinks()
    {
        var site = Site(Module());
        Assert.Null(BlueprintSitePolicy.CreationOutput(site, 0));
        Assert.Null(BlueprintSitePolicy.CreationInput(site, 0));
        Assert.Null(BlueprintSitePolicy.CreationInput(site, 1));
        site.Connections.Add(new BlueprintPlanConnection { FromIndex = 1, FromSlot = 2, ToIndex = 2, ToSlot = 1 });
        Assert.Null(BlueprintSitePolicy.CreationOutput(site, 1));
    }

    [Fact]
    public void SorterCreationInstallsBothApprovedEndsIncludingVirtualBeltSlot()
    {
        var site = Site(Module());
        var input = BlueprintSitePolicy.CreationInput(site, 2);
        var output = BlueprintSitePolicy.CreationOutput(site, 2);
        Assert.NotNull(input); Assert.NotNull(output);
        Assert.Equal((0, -1, 2, 1), (input.FromIndex, input.FromSlot, input.ToIndex, input.ToSlot));
        Assert.Equal((2, 0, 1, 3), (output.FromIndex, output.FromSlot, output.ToIndex, output.ToSlot));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void RejectsAmbiguousCreationOutputInsteadOfLastEdgeWins(bool sorter)
    {
        var site = Site(Module());
        if (!sorter)
        {
            site.Objects.Add(new BlueprintSiteObject { Index = 3, ItemId = 2001 });
            site.Connections.Add(new BlueprintPlanConnection { FromIndex = 0, FromSlot = 0, ToIndex = 3, ToSlot = 1 });
        }
        var duplicate = sorter ? site.Connections[1] : site.Connections[2];
        site.Connections.Add(duplicate);
        Assert.Equal("blueprint_site_creation_edge_ambiguous", Assert.Throws<BlueprintReadException>(() =>
            BlueprintSitePolicy.CreationOutput(site, sorter ? 2 : 0)).Reason);
    }

    [Fact]
    public void RejectsAmbiguousSorterCreationInput()
    {
        var site = Site(Module()); site.Connections.Add(site.Connections[0]);
        Assert.Equal("blueprint_site_creation_edge_ambiguous", Assert.Throws<BlueprintReadException>(() =>
            BlueprintSitePolicy.CreationInput(site, 2)).Reason);
    }

    [Theory]
    [InlineData(-1)] [InlineData(3)]
    public void RejectsInvalidCreationEndpoint(int target)
    {
        var site = Site(Module()); site.Connections[0].ToIndex = target;
        Assert.Equal("blueprint_site_creation_graph_invalid", Assert.Throws<BlueprintReadException>(() =>
            BlueprintSitePolicy.CreationOutput(site, 0)).Reason);
    }

    private static BlueprintSiteSnapshot Site(BlueprintInspection inspection) => new()
    {
        Objects = inspection.Objects.Select(o => new BlueprintSiteObject { Index = o.Index, ItemId = o.ItemId }).ToList(),
        Connections = BlueprintSitePolicy.BuildConnections(inspection, Slots),
    };
    private static BlueprintInspection Module() => new()
    {
        Objects = new List<BlueprintObjectSnapshot> { Belt(0, -1),
            new() { Index = 1, ItemId = 2303, InputObjectIndex = -1, OutputObjectIndex = -1 }, Sorter(2, 0, 1, 3) },
    };
    private static BlueprintObjectSnapshot Belt(int index, int output) => new()
    { Index = index, ItemId = 2001, InputObjectIndex = -1, OutputObjectIndex = output, OutputFromSlot = 0, OutputToSlot = 1 };
    private static BlueprintObjectSnapshot Sorter(int index, int input, int output, int slot) => new()
    { Index = index, ItemId = 2011, InputObjectIndex = input, OutputObjectIndex = output,
        InputToSlot = 1, InputFromSlot = -1, OutputFromSlot = 0, OutputToSlot = slot };
}
