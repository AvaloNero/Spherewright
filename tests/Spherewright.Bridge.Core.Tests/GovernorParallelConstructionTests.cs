using Spherewright.Bridge.Core.Factory;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Factory;
using Spherewright.Contracts.Progression;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class GovernorParallelConstructionTests
{
    [Fact]
    public void AdditionalRateUsesMeasuredBaselineNotInstantaneousStarvationOrFullTarget()
    {
        var f = new Fixture();
        f.Proposal.Supply.Add(new() { ItemId = 2, ActualProductionPerMinute = 7 });
        var request = GovernorPlanCompiler.CreateParallelConstructionRequest(f.Request, f.Proposal)!;
        Assert.Equal(30, request.TargetRatePerMinute);
        Assert.Equal(2, request.TargetItemId);
        Assert.Same(f.Request.ParallelExpansionBlueprint, request.Blueprint);
        f.Request.RecipeChoices[0].RecipeId = 99;
        Assert.Equal(1, request.RecipeChoices[0].RecipeId);
    }

    [Theory]
    [InlineData("warming_up")] [InlineData("zero_baseline")] [InlineData("unstable")]
    public void UnprovedBaselineDoesNotUseAConvenientCurrentRate(string state)
    {
        var f = new Fixture(); f.Proposal.Baseline.State = state;
        Assert.Null(GovernorPlanCompiler.CreateParallelConstructionRequest(f.Request, f.Proposal));
    }

    [Fact]
    public void NoLayoutAndNoPositiveIncrementKeepOrdinaryProposalReadOnly()
    {
        var f = new Fixture(); f.Proposal.Baseline.ProductionPerMinute = 60;
        Assert.Null(GovernorPlanCompiler.CreateParallelConstructionRequest(f.Request, f.Proposal));
        f.Request.ParallelExpansionBlueprint = null;
        Assert.Null(GovernorPlanCompiler.CreateParallelConstructionRequest(f.Request, f.Proposal));
    }

    [Fact]
    public void FullComparisonIncludesEveryExplicitObjectAndReusesFoundryIntent()
    {
        var f = new Fixture(); var parallel = f.Parallel(); var before = GovernorPlanCompiler.Fingerprint(f.Proposal);
        f.Attach(parallel);
        var option = Assert.Single(f.Proposal.Alternatives);
        Assert.Equal("bounded_construction_ready_at_capture", option.Status);
        Assert.Equal(7, option.Consume.Sum(c => c.Count)); // machine + 2 boxes + 2 sorters + tower + wind
        Assert.Contains(option.Consume, c => c.ItemId == 2203 && c.Count == 1);
        Assert.Contains(option.Consume, c => c.ItemId == 2201 && c.Count == 1);
        Assert.Contains("external_infrastructure_excluded", option.CostScope);
        Assert.Null(option.ActualTargetGainPerMinute);
        Assert.False(f.Proposal.Executable); Assert.False(f.Proposal.Balanced);
        Assert.False(parallel.Executable); Assert.False(parallel.Construction!.TransportCapacityVerified);
        Assert.True(parallel.Construction.TransportBudget.Satisfied);
        Assert.Same(parallel, f.Proposal.ParallelExpansion!.Plan);
        Assert.Equal(30, f.Proposal.ParallelExpansion.Intent.TargetRatePerMinute);
        Assert.Equal(2, f.Proposal.ParallelExpansion.Intent.BoundaryPorts.Count);
        Assert.NotEqual(before, f.Proposal.ProposalHash);
        Assert.Equal(GovernorPlanCompiler.Fingerprint(f.Proposal), f.Proposal.ProposalHash);
        var approved = BlueprintBuildState.Create(parallel.BlueprintSite!, parallel.Construction, f.Proposal.ParallelExpansion.Intent);
        Assert.Equal(parallel.Construction.PlanHash, approved.FoundryPlan!.PlanHash);
    }

    [Theory]
    [InlineData("power")] [InlineData("inventory")] [InlineData("occupied")] [InlineData("native_unperformed")]
    [InlineData("technology")] [InlineData("transport")]
    public void FullBudgetsRemainBlockersRatherThanMachineOnlySuccess(string blocker)
    {
        var f = new Fixture();
        if (blocker == "power") f.Site.Power!.FullBaseLoadBudgetSatisfied = false;
        if (blocker == "inventory") f.Site.InventorySufficient = false;
        if (blocker == "occupied") f.Site.Blockers.Add("blueprint_site_occupied:0");
        if (blocker == "native_unperformed") f.Site.NativeCheckPerformed = false;
        if (blocker == "technology") f.Site.TechnologySatisfied = false;
        if (blocker == "transport") f.Buildings.Single(b => b.ItemId == 2011).InserterSttRaw = 2000000;
        f.Attach(f.Parallel());
        Assert.Equal("bounded_construction_blocked", f.Proposal.Alternatives[0].Status);
        Assert.Contains("parallel_construction_not_ready", f.Proposal.Blockers);
        Assert.False(f.Proposal.ParallelExpansion!.Plan.Construction!.CanPrepare);
    }

    [Theory]
    [InlineData("session")] [InlineData("planet")] [InlineData("tick")] [InlineData("revision")]
    [InlineData("power_tick")] [InlineData("target")] [InlineData("full_target_instead_of_delta")]
    [InlineData("material_hash")] [InlineData("cost")] [InlineData("flow")] [InlineData("executable")]
    [InlineData("blueprint_code")] [InlineData("position")] [InlineData("orientation")] [InlineData("assessment")]
    public void MixedCaptureOrChangedIntentGraphCannotBeAttached(string change)
    {
        var f = new Fixture(); var parallel = f.Parallel();
        if (change == "session") parallel.SessionId = "another";
        if (change == "planet") parallel.PlanetId++;
        if (change == "tick") parallel.CapturedAtGameTick++;
        if (change == "revision") parallel.BlueprintSite!.Revision++;
        if (change == "power_tick") parallel.BlueprintSite!.Power!.CapturedAtGameTick++;
        if (change == "target") parallel.TargetItemId++;
        if (change == "full_target_instead_of_delta") parallel.TargetRatePerMinute = 60;
        if (change == "material_hash") parallel.PlanHash = "unbound";
        if (change == "cost") parallel.Construction!.ConstructionCost[0].Count++;
        if (change == "flow") parallel.Construction!.Routes[0].RequiredRatePerMinute++;
        if (change == "executable") parallel.Executable = true;
        if (change == "blueprint_code") f.Request.ParallelExpansionBlueprint!.BlueprintCode += "changed";
        if (change == "position") f.Request.ParallelExpansionBlueprint!.Site.Position.X++;
        if (change == "orientation") f.Request.ParallelExpansionBlueprint!.Site.QuarterTurns++;
        if (change == "assessment") parallel.BlueprintSite!.AssessmentHash = "unbound";
        Assert.Throws<FoundryPlanningException>(() => f.Attach(parallel));
        Assert.Null(f.Proposal.ParallelExpansion);
    }

    [Theory]
    [InlineData("code")] [InlineData("utf8")] [InlineData("site")] [InlineData("ports")]
    [InlineData("null_port")] [InlineData("nan_position")] [InlineData("invalid_turns")] [InlineData("hash_version")]
    public void ExplicitLayoutEnvelopeIsBoundedBeforeNativeInspection(string change)
    {
        var f = new Fixture(); var layout = f.Request.ParallelExpansionBlueprint!;
        if (change == "code") layout.BlueprintCode = new string('a', BoundedBlueprintReader.MaximumCodeBytes + 1);
        if (change == "utf8") layout.BlueprintCode = new string('图', BoundedBlueprintReader.MaximumCodeBytes / 3 + 1);
        if (change == "site") layout.Site = null!;
        if (change == "ports") layout.BoundaryPorts = Enumerable.Range(0, 34).Select(_ => new FoundryBoundaryPort()).ToList();
        if (change == "null_port") layout.BoundaryPorts[0] = null!;
        if (change == "nan_position") layout.Site.Position.X = float.NaN;
        if (change == "invalid_turns") layout.Site.QuarterTurns = 4;
        if (change == "hash_version") layout.Site.StateHashVersion = 2;
        Assert.Throws<FoundryPlanningException>(() => GovernorPlanCompiler.ValidateRequest(f.Request));
    }

    [Theory]
    [InlineData("intent")] [InlineData("construction")] [InlineData("power")] [InlineData("cost_scope")]
    public void ProposalHashBindsFullConstructionEvidence(string change)
    {
        var f = new Fixture(); f.Attach(f.Parallel()); var before = f.Proposal.ProposalHash;
        if (change == "intent") f.Proposal.ParallelExpansion!.Intent.TargetRatePerMinute++;
        if (change == "construction") f.Proposal.ParallelExpansion!.Plan.Construction!.ConstructionCost[0].Count++;
        if (change == "power") f.Proposal.ParallelExpansion!.Plan.BlueprintSite!.Power!.AssessmentHash = "changed";
        if (change == "cost_scope") f.Proposal.Alternatives[0].CostScope = "unproved_full_factory";
        Assert.NotEqual(before, GovernorPlanCompiler.Fingerprint(f.Proposal));
    }

    private sealed class Fixture
    {
        public GetGovernorPlanRequest Request = new()
        {
            PlanetId = 104, TargetItemId = 2, TargetRatePerMinute = 60,
            SourceEntities = new() { new() { ObjectId = 20, ExpectedRecipeId = 1, ExpectedEndpointStateHash = "endpoint" } },
            RecipeChoices = new() { new() { ItemId = 2, RecipeId = 1, BuildingItemId = 2303 } },
            ParallelExpansionBlueprint = new()
            {
                BlueprintCode = "explicit test data; native import is not simulated here",
                Site = new() { ExpectedPlayerStateHash = "player", Position = new() { Y = 200 } },
                BoundaryPorts = new() { new() { ItemId = 1, Direction = "input", ObjectIndex = 0, Slot = 3 },
                    new() { ItemId = 2, Direction = "output", ObjectIndex = 4, Slot = 3 } },
            },
        };
        public GovernorPlanSnapshot Proposal = new()
        {
            SessionId = "session", PlanetId = 104, CapturedAtGameTick = 2400, Revision = 4,
            TargetItemId = 2, TargetRatePerMinute = 60,
            Baseline = new() { State = "ready", IndependentWindowCount = 3, ProductionPerMinute = 30 },
            Alternatives = new() { new() { Kind = "add_parallel_production" } },
        };
        public RecipeCatalogSnapshot Recipes = new()
        {
            SessionId = "session", PlanetId = 104, CapturedAtGameTick = 2400,
            Items = new() { new() { ItemId = 1, Unlocked = true, IsRaw = true }, new() { ItemId = 2, Unlocked = true } },
            Recipes = new() { new() { RecipeId = 1, Unlocked = true, RecipeType = "Assemble", TimeSpend = 60,
                Inputs = new() { new() { ItemId = 1, Count = 1 } }, Outputs = new() { new() { ItemId = 2, Count = 1 } } } },
        };
        public List<BuildCatalogItem> Buildings = new[] { 2303, 2011, 2101, 2201, 2203 }.Select(id => new BuildCatalogItem
        {
            ItemId = id, Unlocked = true, Available = true, SlotCount = 12,
            Role = id == 2303 ? "assembler" : "infrastructure", RecipeType = id == 2303 ? "Assemble" : string.Empty,
            ProductionSpeedRaw = id == 2303 ? 10000 : null, WorkEnergyPerTick = id == 2303 ? 100 : null,
            InserterGrade = id == 2011 ? 1 : null, InserterSttRaw = id == 2011 ? 200000 : null,
        }).ToList();
        public BlueprintSiteSnapshot Site = new()
        {
            SessionId = "session", PlanetId = 104, Revision = 4, CapturedAtGameTick = 2400,
            BlueprintHash = "blueprint", AssessmentHash = "native-site", Position = new() { Y = 200 },
            NativeCheckPerformed = true, NativeCheckPassed = true, InventorySufficient = true, TechnologySatisfied = true,
            Power = new() { CapturedAtGameTick = 2400, AssessmentHash = "power", AllPlannedConsumersCovered = true, FullBaseLoadBudgetSatisfied = true },
        };
        public Fixture()
        {
            Site.BlueprintHash = CanonicalStateHash.Combine("blueprint-code-v1", Request.ParallelExpansionBlueprint!.BlueprintCode);
            for (var i = 0; i < 7; i++)
            {
                var id = i == 5 ? 2201 : i == 6 ? 2203 : i == 2 ? 2303 : i % 2 == 0 ? 2101 : 2011;
                Site.Objects.Add(new() { Index = i, ItemId = id, RecipeId = id == 2303 ? 1 : 0,
                    Parameters = id == 2101 ? new int[110] : id == 2011 ? new[] { 1 } : Array.Empty<int>() });
                if (id == 2011)
                {
                    Site.Objects[i].Dependencies.AddRange(new[] { i - 1, i + 1 });
                    Site.Connections.Add(new() { FromIndex = i - 1, FromSlot = 0, ToIndex = i, ToSlot = 1 });
                    Site.Connections.Add(new() { FromIndex = i, FromSlot = 0, ToIndex = i + 1, ToSlot = 1 });
                }
            }
            Site.ConstructionItems = Site.Objects.GroupBy(o => o.ItemId)
                .Select(g => new FoundryInventoryBudget { ItemId = g.Key, RequiredCount = g.Count(), PackageCount = g.Count() }).ToList();
        }
        public FoundryPlanSnapshot Parallel()
        {
            var request = GovernorPlanCompiler.CreateParallelConstructionRequest(Request, Proposal)!;
            Site.AssessmentHash = BlueprintSitePolicy.AssessmentHash(Site, request.Blueprint!.Site.ExpectedPlayerStateHash);
            var result = FoundryPlanCompiler.Compile(request, Recipes, Buildings);
            result.Phase = "blueprint_construction_plan"; result.BlueprintSite = Site;
            result.Construction = FoundryConstructionCompiler.Compile(result, Site, request.Blueprint!.BoundaryPorts, Buildings);
            return result;
        }
        public void Attach(FoundryPlanSnapshot parallel) => GovernorPlanCompiler.AttachParallelConstruction(Request, Proposal, parallel, Recipes, Buildings);
    }
}
