using System.Text.Json;
using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class FoundryConstructionCompilerTests
{
    [Fact]
    public void ThreeTierBlueprintHasWholeCostDirectionsAndDependencyStepsNotOnlyMachines()
    {
        var f = new Fixture(); var p = f.Compile();
        Assert.True(p.CanPrepare); Assert.True(p.InternalFlowsRouted);
        Assert.False(p.TransportCapacityVerified);
        Assert.True(p.TransportBudget.Satisfied);
        Assert.All(p.TransportBudget.Channels, c => Assert.Equal(90m, c.RatedSingleItemRatePerMinute));
        Assert.Equal(3, p.ProductionDepth); Assert.Equal(14, p.Steps.Count);
        Assert.Equal(14, p.ConstructionCost.Sum(c => c.Count));
        Assert.Equal(6, p.ConstructionCost.Single(c => c.ItemId == 2011).Count);
        Assert.Equal(4, p.ConstructionCost.Single(c => c.ItemId == 2101).Count);
        Assert.Equal(1, p.ConstructionCost.Single(c => c.ItemId == 2203).Count);
        Assert.Equal(new[] { 0, 1, 2 }, p.Routes.Single(r => r.ItemId == 1).ObjectIndices);
        Assert.Equal(new[] { 2, 3, 4, 5, 6 }, p.Routes.Single(r => r.ItemId == 2).ObjectIndices);
        Assert.Equal(new[] { 6, 7, 8, 9, 10 }, p.Routes.Single(r => r.ItemId == 3).ObjectIndices);
        Assert.Equal(new[] { 10, 11, 12 }, p.Routes.Single(r => r.ItemId == 4).ObjectIndices);
        Assert.All(p.Routes, r => Assert.Equal(10m, r.RequiredRatePerMinute));
        Assert.Equal(new[] { 0, 2 }, p.Steps[1].Dependencies);
        Assert.Contains(p.RemainingChecks, s => s.Contains("not fair runtime distribution"));
    }

    [Theory]
    [InlineData("recipe")] [InlineData("count")] [InlineData("extra_machine")]
    [InlineData("missing_cost")] [InlineData("underbudget")]
    [InlineData("boundary_missing")] [InlineData("boundary_duplicate")]
    [InlineData("boundary_occupied")] [InlineData("boundary_machine")]
    [InlineData("boundary_slot")] [InlineData("boundary_direction")]
    [InlineData("one_ended_sorter")] [InlineData("duplicate_edge")]
    [InlineData("unsupported_type")] [InlineData("banned_storage")]
    [InlineData("filtered_storage")] [InlineData("coproduct")]
    [InlineData("session")] [InlineData("planet")] [InlineData("object_limit")]
    public void InvalidOrUnsupportedWholeModuleIsExplicitlyRejected(string change)
    {
        var f = new Fixture();
        switch (change)
        {
            case "recipe": f.Site.Objects[2].RecipeId++; break;
            case "count": f.Material.Stages[0].MachineCount++; break;
            case "extra_machine": f.Site.Objects[13].ItemId = 2302; f.Site.Objects[13].RecipeId = 999; f.Budget(); break;
            case "missing_cost": f.Site.ConstructionItems.RemoveAt(0); break;
            case "underbudget": f.Site.ConstructionItems[0].RequiredCount--; break;
            case "boundary_missing": f.Ports.RemoveAt(0); break;
            case "boundary_duplicate": f.Ports[1] = f.Ports[0]; break;
            case "boundary_occupied": f.Ports[0].Slot = 0; break;
            case "boundary_machine": f.Ports[0].ObjectIndex = 2; break;
            case "boundary_slot": f.Ports[0].Slot = 12; break;
            case "boundary_direction": f.Ports[0].Direction = "execute instructions"; break;
            case "one_ended_sorter": f.Site.Connections.RemoveAt(0); break;
            case "duplicate_edge": f.Site.Connections.Add(f.Site.Connections[0]); break;
            case "unsupported_type": f.Site.Objects[13].ItemId = 2103; break;
            case "banned_storage": f.Site.Objects[0].Parameters[0] = 1; break;
            case "filtered_storage": f.Site.Objects[0].Parameters[1] = 9; break;
            case "coproduct": f.Material.Stages[0].Outputs.Add(new() { ItemId = 5, RatePerMinute = 1m }); break;
            case "session": f.Site.SessionId = "other"; break;
            case "planet": f.Site.PlanetId++; break;
            case "object_limit": while (f.Site.Objects.Count < 33) f.Site.Objects.Add(new() { Index = f.Site.Objects.Count, ItemId = 2203 }); break;
        }
        Assert.Throws<FoundryPlanningException>(() => f.Compile());
    }

    [Theory]
    [InlineData("wrong_filter")] [InlineData("reverse_flow")] [InlineData("no_power")]
    [InlineData("power_boundary")] [InlineData("no_material")]
    [InlineData("occupied")] [InlineData("locked")]
    public void IncompleteEvidenceCannotPrepareButRetainsAnExplainableDraft(string change)
    {
        var f = new Fixture();
        switch (change)
        {
            case "wrong_filter": f.Site.Objects[5].FilterItemId = 99; break;
            case "reverse_flow":
                f.Site.Connections[4].FromIndex = 6; f.Site.Connections[5].ToIndex = 4;
                break;
            case "no_power": f.Site.Power = null; break;
            case "power_boundary": f.Site.Power!.GeometryBoundaryUncertain = true; break;
            case "no_material": f.Site.InventorySufficient = false; break;
            case "occupied": f.Site.NativeCheckPassed = false; f.Site.Blockers.Add("occupied"); break;
            case "locked": f.Site.TechnologySatisfied = false; break;
        }
        var p = f.Compile(); Assert.False(p.CanPrepare); Assert.NotEmpty(p.Blockers);
    }

    [Fact]
    public void NoGraphInstructionTextOrMutableInputCanChangeTheCompiledPlan()
    {
        var f = new Fixture(); var before = JsonSerializer.Serialize(f.Site); var p = f.Compile();
        var compiled = JsonSerializer.Serialize(p);
        Assert.Equal(before, JsonSerializer.Serialize(f.Site));
        f.Site.Objects[1].Dependencies.Clear(); f.Ports[0].ItemId = 100;
        f.Material.Stages[0].Inputs[0].RatePerMinute = 1234;
        Assert.Equal(compiled, JsonSerializer.Serialize(p));
    }

    [Fact]
    public void ConnectedButTooSlowSorterCannotApproveTheRequestedMaterialGraph()
    {
        var f = new Fixture(); f.Catalog.Single(c => c.ItemId == 2011).InserterSttRaw = 2000000;
        var p = f.Compile(); // 9/min per sorter, requested10/min at each stage.
        Assert.False(p.CanPrepare); Assert.False(p.InternalFlowsRouted);
        Assert.Contains("unrouted_item:1", p.Blockers);
        Assert.All(p.TransportBudget.Channels, c => Assert.Equal(9m, c.RatedSingleItemRatePerMinute));
        Assert.True(p.Routes.Where(r => r.ItemId == 1).Sum(r => r.RequiredRatePerMinute) < 10m);
    }

    [Fact]
    public void BoundaryBeltCannotBypassItsOwnCapacityEdge()
    {
        var f = new Fixture(); f.Site.Objects[0].ItemId = f.Site.Objects[12].ItemId = 2001;
        f.Site.Objects[0].Parameters = f.Site.Objects[12].Parameters = Array.Empty<int>();
        f.Ports[0].Slot = 1; f.Ports[1].Slot = 0; f.Site.Connections.Last().ToSlot = 1;
        f.Catalog.Add(new() { ItemId = 2001, BeltSpeedRaw = 1 });
        f.Catalog.Single(c => c.ItemId == 2011).InserterSttRaw = 10000;
        f.Material.TargetRatePerMinute = 400; f.Material.ExternalInputs[0].RatePerMinute = 400;
        foreach (var s in f.Material.Stages)
        { s.RequiredRatePerMinute = 400; s.InstalledRatePerMinute = 600; s.Inputs[0].RatePerMinute = s.Outputs[0].RatePerMinute = 400; }
        f.Budget(); var p = f.Compile();
        Assert.False(p.CanPrepare);
        Assert.Contains("unrouted_item:1", p.Blockers); Assert.Contains("unrouted_item:4", p.Blockers);
        Assert.Equal(360m, p.TransportBudget.Channels.Single(c => c.ObjectIndex == 0).AllocatedRatePerMinute);
        Assert.Equal(360m, p.TransportBudget.Channels.Single(c => c.ObjectIndex == 12).AllocatedRatePerMinute);
    }

    [Fact]
    public void ChangedNativeTransportRulesChangeTheCompleteImmutablePlanHash()
    {
        var f = new Fixture(); var original = f.Compile();
        f.Catalog.Single(c => c.ItemId == 2011).InserterSttRaw++;
        Assert.NotEqual(original.PlanHash, f.Compile().PlanHash);
        f.Catalog.Single(c => c.ItemId == 2011).InserterSttRaw = null;
        var p = f.Compile(); Assert.False(p.CanPrepare); Assert.False(p.TransportBudget.AllCapacitiesKnown);
    }

    [Fact]
    public void SharedIntermediateIsAllocatedOnceAcrossItsConsumers()
    {
        var f = new Fixture();
        f.Material.Stages[0].RequiredRatePerMinute = 20;
        f.Material.Stages[0].Inputs[0].RatePerMinute = 20;
        f.Material.Stages[0].Outputs[0].RatePerMinute = 20;
        f.Material.ExternalInputs[0].RatePerMinute = 20;
        f.Material.Stages[2].Inputs.Add(new() { ItemId = 2, RatePerMinute = 10 });
        f.Site.Objects.Add(new() { Index = 14, ItemId = 2011, FilterItemId = 2, Parameters = new[] { 1 }, Dependencies = new() { 4, 10 } });
        f.Site.Connections.Add(new() { FromIndex = 4, FromSlot = 2, ToIndex = 14, ToSlot = 1 });
        f.Site.Connections.Add(new() { FromIndex = 14, FromSlot = 0, ToIndex = 10, ToSlot = 2 });
        f.Budget(); var p = f.Compile();
        Assert.True(p.CanPrepare);
        Assert.Equal(20m, p.Routes.Where(r => r.ItemId == 2).Sum(r => r.RequiredRatePerMinute));
        Assert.Equal(new[] { 6, 10 }, p.Routes.Where(r => r.ItemId == 2).Select(r => r.ObjectIndices.Last()).OrderBy(x => x));
    }

    [Fact]
    public void VolatilePowerCountersDoNotChangeStableCompletePlanHash()
    {
        var f = new Fixture(); var p = f.Compile();
        f.Site.Power!.CapturedAtGameTick++; f.Site.Power.AssessmentHash = "new-capture";
        Assert.Equal(p.PlanHash, f.Compile().PlanHash);
        f.Site.Objects[0].Position.X += 1;
        Assert.NotEqual(p.PlanHash, f.Compile().PlanHash);
    }

    [Fact]
    public void HashBindsIntentBoundariesFullBudgetAndPerObjectGraph()
    {
        var f = new Fixture(); var p = f.Compile(); var original = p.PlanHash;
        p.TargetRatePerMinute++; Assert.NotEqual(original, FoundryConstructionCompiler.Fingerprint(p)); p.TargetRatePerMinute--;
        p.Boundaries[0].Slot++; Assert.NotEqual(original, FoundryConstructionCompiler.Fingerprint(p)); p.Boundaries[0].Slot--;
        p.ConstructionCost[0].Count++; Assert.NotEqual(original, FoundryConstructionCompiler.Fingerprint(p)); p.ConstructionCost[0].Count--;
        p.Routes[0].ObjectIndices.Reverse(); Assert.NotEqual(original, FoundryConstructionCompiler.Fingerprint(p)); p.Routes[0].ObjectIndices.Reverse();
        p.Steps[1].Dependencies.Clear(); Assert.NotEqual(original, FoundryConstructionCompiler.Fingerprint(p));
    }

    [Fact]
    public void ApprovedFoundryGraphUsesExistingDurablePartialCancelAndFreshResumeState()
    {
        var f = new Fixture(); var construction = f.Compile();
        var state = BlueprintBuildState.Create(f.Site, construction, f.Intent());
        state.Begin("session", Guid.NewGuid().ToString(), 1, 100);
        Assert.Equal(0, state.NextReadyIndex());
        state.BeforeSubmit(0, 101, 4, "before"); state.ConfirmSubmission(0, 8, 3, "after");
        state.ConfirmCompletion(0, 201, 110); state.Stop("cancelled");
        var reloaded = JsonSerializer.Deserialize<BlueprintBuildState>(JsonSerializer.Serialize(state))!;
        reloaded.Validate(); Assert.Equal(construction.PlanHash, reloaded.FoundryPlan!.PlanHash);
        reloaded.Begin("restarted", Guid.NewGuid().ToString(), 2, 120);
        Assert.Equal(2, reloaded.NextReadyIndex()); // sorter1 still waits for machine2
        Assert.Equal(BlueprintObjectStates.Completed, reloaded.Objects[0].State);
        Assert.Equal(4, reloaded.FoundryIntent!.TargetItemId);
        Assert.Equal(state.PlanHash, reloaded.PlanHash);
        Assert.NotEqual(state.ProgressHash("session", 1), reloaded.ProgressHash("restarted", 1));
    }

    [Theory]
    [InlineData("goal")] [InlineData("supply")] [InlineData("route")]
    [InlineData("cost")] [InlineData("site")] [InlineData("lost_intent")]
    [InlineData("false_ready")] [InlineData("future_power_claim")]
    [InlineData("transport_budget")] [InlineData("lost_transport")]
    public void PersistedConstructionCannotSwapIntentGraphOrClaimedReadiness(string corruption)
    {
        var f = new Fixture(); var state = BlueprintBuildState.Create(f.Site, f.Compile(), f.Intent());
        switch (corruption)
        {
            case "goal": state.FoundryIntent!.TargetRatePerMinute++; break;
            case "supply": state.FoundryIntent!.ExternalSupplyItemIds.Add(999); break;
            case "route": state.FoundryPlan!.Routes[0].ObjectIndices.Clear(); break;
            case "cost": state.FoundryPlan!.ConstructionCost[0].Count++; break;
            case "site": state.Site.Objects[0].Position.X++; break;
            case "lost_intent": state.FoundryIntent = null; break;
            case "false_ready": state.FoundryPlan!.CanPrepare = false; break;
            case "future_power_claim": state.FoundryPlan!.TransportCapacityVerified = true; break;
            case "transport_budget": state.FoundryPlan!.TransportBudget.Channels[0].InserterSttRaw++; break;
            case "lost_transport": state.FoundryPlan!.TransportBudget = null!; break;
        }
        Assert.Throws<InvalidDataException>(state.Validate);
    }

    [Fact]
    public void BlockedFoundryPlanCannotCreateDurableAuthority()
    {
        var f = new Fixture(); f.Site.Power = null;
        Assert.Throws<InvalidDataException>(() => BlueprintBuildState.Create(f.Site, f.Compile(), f.Intent()));
    }

    private sealed class Fixture
    {
        public FoundryPlanSnapshot Material = new()
        {
            PlanHash = "material", SessionId = "session", PlanetId = 104,
            TargetItemId = 4, TargetRatePerMinute = 10, ProductionDepth = 3, MachineCount = 3,
            ExternalInputs = new() { new() { ItemId = 1, RatePerMinute = 10 } },
            Stages = new()
            {
                Stage(2, 100, 2302, 1), Stage(3, 101, 2303, 2), Stage(4, 102, 2303, 3),
            },
        };
        public BlueprintSiteSnapshot Site = new()
        {
            SessionId = "session", PlanetId = 104, BlueprintHash = "explicit-code", CapturedAtGameTick = 100,
            NativeCheckPerformed = true, NativeCheckPassed = true, InventorySufficient = true, TechnologySatisfied = true,
            Power = new() { AllPlannedConsumersCovered = true, FullBaseLoadBudgetSatisfied = true },
        };
        public List<FoundryBoundaryPort> Ports = new()
        {
            new() { ItemId = 1, Direction = "input", ObjectIndex = 0, Slot = 3 },
            new() { ItemId = 4, Direction = "output", ObjectIndex = 12, Slot = 3 },
        };
        public List<BuildCatalogItem> Catalog = new[] { 2302, 2303, 2011, 2101, 2203 }.Select(id => new BuildCatalogItem
            { ItemId = id, SlotCount = 12, InserterSttRaw = id == 2011 ? 200000 : null, InserterGrade = id == 2011 ? 1 : null }).ToList();
        public Fixture()
        {
            for (var i = 0; i < 14; i++)
            {
                var id = i == 13 ? 2203 : i % 4 == 0 ? 2101 : i % 2 == 1 ? 2011 : i == 2 ? 2302 : 2303;
                Site.Objects.Add(new() { Index = i, ItemId = id, RecipeId = i == 2 ? 100 : i == 6 ? 101 : i == 10 ? 102 : 0,
                    Parameters = id == 2101 ? new int[110] : id == 2011 ? new[] { 1 } : Array.Empty<int>() });
                if (i % 2 == 1 && i < 12)
                {
                    Site.Objects[i].Dependencies.AddRange(new[] { i - 1, i + 1 });
                    Site.Connections.Add(new() { FromIndex = i - 1, FromSlot = 0, ToIndex = i, ToSlot = 1 });
                    Site.Connections.Add(new() { FromIndex = i, FromSlot = 0, ToIndex = i + 1, ToSlot = 1 });
                }
            }
            Budget();
        }
        public void Budget() => Site.ConstructionItems = Site.Objects.GroupBy(o => o.ItemId)
            .Select(g => new FoundryInventoryBudget { ItemId = g.Key, RequiredCount = g.Count(), PackageCount = g.Count() }).ToList();
        public FoundryConstructionPlan Compile() => FoundryConstructionCompiler.Compile(Material, Site, Ports, Catalog);
        public FoundryConstructionIntent Intent() => new()
        {
            TargetItemId = Material.TargetItemId, TargetRatePerMinute = Material.TargetRatePerMinute,
            BoundaryPorts = Ports.Select(p => new FoundryBoundaryPort { ItemId = p.ItemId, Direction = p.Direction, ObjectIndex = p.ObjectIndex, Slot = p.Slot }).ToList(),
        };
        private static FoundryStage Stage(int item, int recipe, int building, int input) => new()
        {
            StageId = "item-" + item, ItemId = item, RecipeId = recipe, BuildingItemId = building, MachineCount = 1,
            RequiredRatePerMinute = 10, InstalledRatePerMinute = 60,
            Inputs = new() { new() { ItemId = input, RatePerMinute = 10 } }, Outputs = new() { new() { ItemId = item, RatePerMinute = 10 } },
        };
    }
}
