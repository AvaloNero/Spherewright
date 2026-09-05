using System.Globalization;
using System.Text.Json;
using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;
using Spherewright.Contracts.Progression;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class FoundryPlanCompilerTests
{
    [Fact]
    public void SharedIntermediateDemandIsAggregatedBeforeMachinesAreRounded()
    {
        var (request, catalog, machines) = Chain();
        var plan = FoundryPlanCompiler.Compile(request, catalog, machines);
        Assert.Equal(3, plan.ProductionDepth);
        Assert.Equal(new[] { 2, 3, 4 }, plan.Stages.Select(x => x.ItemId));
        // 30 motors need 60 direct ingots and 30 gears, each gear takes 2 ingots.
        Assert.Equal(120m, plan.Stages[0].RequiredRatePerMinute);
        Assert.Equal(2, plan.Stages[0].MachineCount);
        Assert.Equal(1, plan.Stages[1].MachineCount);
        Assert.Equal(2, plan.Stages[2].MachineCount);
        Assert.Equal(120m, Assert.Single(plan.ExternalInputs).RatePerMinute);
        Assert.Equal(5, plan.MachineCount);
        Assert.Equal(1319940, plan.MachineWorkPowerWatts);
        Assert.Equal(3, plan.MachineCost.Single(x => x.ItemId == 20).Count);
        Assert.False(plan.Executable);
        Assert.Contains(plan.RemainingChecks, x => x.Contains("belts"));
    }

    [Fact]
    public void SpeedAndOutputBatchSizeAreUsedInsteadOfAssumingOneItemPerCycle()
    {
        var (request, catalog, machines) = Chain();
        request.TargetItemId = 3;
        request.TargetRatePerMinute = 91m;
        catalog.Recipes[1].Outputs[0].Count = 2;
        machines[1].ProductionSpeedRaw = 7500;
        var plan = FoundryPlanCompiler.Compile(request, catalog, machines);
        var stage = plan.Stages.Last();
        Assert.Equal(90m, stage.PerMachineRatePerMinute);
        Assert.Equal(2, stage.MachineCount);
        Assert.Equal(91m, stage.Inputs.Single().RatePerMinute);
    }

    [Fact]
    public void ExplicitSupplyStopsRecursionWithoutInventingAutomaticInventory()
    {
        var (request, catalog, machines) = Chain();
        request.ExternalSupplyItemIds.Add(2);
        var plan = FoundryPlanCompiler.Compile(request, catalog, machines);
        Assert.Equal(new[] { 3, 4 }, plan.Stages.Select(x => x.ItemId));
        Assert.Equal(2, Assert.Single(plan.ExternalInputs).ItemId);
        Assert.Equal(120m, plan.ExternalInputs[0].RatePerMinute);
        Assert.Contains(plan.RemainingChecks, x => x.Contains("finite inventory"));
    }

    [Fact]
    public void LockedIntermediateMustNotSilentlyBecomeAnExternalSupply()
    {
        var (request, catalog, machines) = Chain();
        catalog.Recipes[1].Unlocked = false;
        Assert.Equal("recipe_unavailable", Assert.Throws<FoundryPlanningException>(() => FoundryPlanCompiler.Compile(request, catalog, machines)).Reason);
    }

    [Fact]
    public void UnavailableSpeedNeverBecomesAnInventedOneTimesSpeed()
    {
        var (request, catalog, machines) = Chain();
        machines[1].ProductionSpeedRaw = null;
        Assert.Equal("recipe_unavailable", Assert.Throws<FoundryPlanningException>(() => FoundryPlanCompiler.Compile(request, catalog, machines)).Reason);
    }

    [Fact]
    public void ExplicitRecipeSelectionAndBuildingChoiceAreHonored()
    {
        var (request, catalog, machines) = Chain();
        catalog.Recipes.Add(Recipe(40, 4, 60, "Assemble", (1, 1)));
        machines.Add(new BuildCatalogItem { ItemId = 21, Role = "assembler", RecipeType = "Assemble", Grade = 2,
            Unlocked = true, Available = true, ProductionSpeedRaw = 20000, WorkEnergyPerTick = 5000 });
        request.RecipeChoices.Add(new FoundryRecipeChoice { ItemId = 4, RecipeId = 40, BuildingItemId = 21 });
        var plan = FoundryPlanCompiler.Compile(request, catalog, machines);
        Assert.Equal(40, Assert.Single(plan.Stages).RecipeId);
        Assert.Equal(21, plan.Stages[0].BuildingItemId);
        Assert.Equal(120m, plan.Stages[0].PerMachineRatePerMinute);
    }

    [Fact]
    public void CyclesFailWithoutGrowingThePlanOrGuessingAFeed()
    {
        var (request, catalog, machines) = Chain();
        catalog.Recipes[0].Inputs = new List<CatalogItemAmount> { new CatalogItemAmount { ItemId = 4, Count = 1 } };
        Assert.Equal("recipe_cycle", Assert.Throws<FoundryPlanningException>(() => FoundryPlanCompiler.Compile(request, catalog, machines)).Reason);
    }

    [Fact]
    public void CoproductsHaveExplicitRequiredSinksAndDoNotCancelFeedRequirements()
    {
        var (request, catalog, machines) = Chain();
        catalog.Recipes[2].Outputs.Add(new CatalogItemAmount { ItemId = 1, Count = 2 });
        var plan = FoundryPlanCompiler.Compile(request, catalog, machines);
        Assert.Equal(60m, Assert.Single(plan.Byproducts).RatePerMinute);
        Assert.Equal(120m, Assert.Single(plan.ExternalInputs).RatePerMinute);
    }

    [Fact]
    public void EquivalentCatalogOrderAndDifferentCaptureSessionHaveSameMaterialHash()
    {
        var (request, catalog, machines) = Chain();
        var first = FoundryPlanCompiler.Compile(request, catalog, machines);
        catalog.Recipes.Reverse(); catalog.Items.Reverse(); machines.Reverse();
        foreach (var r in catalog.Recipes) r.Inputs.Reverse();
        catalog.SessionId = "after-restart"; catalog.CapturedAtGameTick += 100;
        var next = FoundryPlanCompiler.Compile(request, catalog, machines);
        Assert.Equal(first.PlanHash, next.PlanHash);
        Assert.Equal(first.Stages.Select(x => x.ItemId), next.Stages.Select(x => x.ItemId));
        machines[0].ProductionSpeedRaw = 10000;
        Assert.NotEqual(first.PlanHash, FoundryPlanCompiler.Compile(request, catalog, machines).PlanHash);
    }

    [Fact]
    public void HashIsIndependentOfCultureAndEquivalentDecimalScale()
    {
        var (request, catalog, machines) = Chain();
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            var first = FoundryPlanCompiler.Compile(request, catalog, machines);
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            request.TargetRatePerMinute = 30.000m;
            Assert.Equal(first.PlanHash, FoundryPlanCompiler.Compile(request, catalog, machines).PlanHash);
        }
        finally { CultureInfo.CurrentCulture = originalCulture; }
    }

    [Fact]
    public void CompilationDoesNotRetainMutableInputOrChangeCatalog()
    {
        var (request, catalog, machines) = Chain();
        var before = JsonSerializer.Serialize(catalog);
        var plan = FoundryPlanCompiler.Compile(request, catalog, machines);
        Assert.Equal(before, JsonSerializer.Serialize(catalog));
        catalog.Recipes[0].Inputs[0].Count = 99;
        Assert.Equal(120m, plan.Stages[0].Inputs[0].RatePerMinute);
    }

    [Fact]
    public void HashIncludesBatchCapacityEvenWhenFlowAndMachineCountDoNotChange()
    {
        var (request, catalog, machines) = Chain();
        request.TargetItemId = 3;
        var first = FoundryPlanCompiler.Compile(request, catalog, machines);
        catalog.Recipes[1].Inputs[0].Count *= 2;
        catalog.Recipes[1].Outputs[0].Count *= 2;
        var second = FoundryPlanCompiler.Compile(request, catalog, machines);
        Assert.Equal(first.MachineCount, second.MachineCount);
        Assert.Equal(first.ExternalInputs[0].RatePerMinute, second.ExternalInputs[0].RatePerMinute);
        Assert.NotEqual(first.Stages.Last().PerMachineRatePerMinute, second.Stages.Last().PerMachineRatePerMinute);
        Assert.NotEqual(first.PlanHash, second.PlanHash);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void TinyRateCannotCreateZeroMachinesOrEraseAnInputByDecimalUnderflow(int batch)
    {
        var (request, catalog, machines) = Chain();
        request.TargetRatePerMinute = 0.0000000000000000000000000001m;
        catalog.Recipes[2].Outputs[0].Count = batch;
        Assert.Equal("budget_underflow", Assert.Throws<FoundryPlanningException>(() =>
            FoundryPlanCompiler.Compile(request, catalog, machines)).Reason);
    }

    [Fact]
    public void PreviouslyVisitedSharedSubtreeCannotBypassSixteenLevelDepthLimit()
    {
        var (request, catalog, machines) = Chain();
        catalog.Items = Enumerable.Range(1, 20).Select(id => new ItemCatalogEntry
            { ItemId = id, Name = "item " + id, Unlocked = true }).ToList();
        // Visit 1 -> 2 -> 3 -> 4 first (four levels), then a second
        // fourteen-level path which reaches already visited item 1.
        catalog.Recipes = new List<RecipeCatalogEntry>();
        for (var id = 1; id < 4; id++) catalog.Recipes.Add(Recipe(id, id, 60, "Assemble", (id + 1, 1)));
        catalog.Recipes.Add(Recipe(4, 4, 60, "Assemble"));
        for (var id = 5; id < 18; id++) catalog.Recipes.Add(Recipe(id, id, 60, "Assemble", (id + 1, 1)));
        catalog.Recipes.Add(Recipe(18, 18, 60, "Assemble", (1, 1)));
        catalog.Recipes.Add(Recipe(20, 20, 60, "Assemble", (1, 1), (5, 1)));
        request.TargetItemId = 20;
        Assert.Equal("depth_limit", Assert.Throws<FoundryPlanningException>(() =>
            FoundryPlanCompiler.Compile(request, catalog, machines)).Reason);
    }

    [Theory]
    [InlineData(0, "invalid_request")]
    [InlineData(-1, "invalid_request")]
    [InlineData(1000001, "invalid_request")]
    [InlineData(1000000, "machine_limit")]
    public void TargetRatesAreBounded(int rate, string reason)
    {
        var (request, catalog, machines) = Chain(); request.TargetRatePerMinute = rate;
        Assert.Equal(reason, Assert.Throws<FoundryPlanningException>(() => FoundryPlanCompiler.Compile(request, catalog, machines)).Reason);
    }

    [Fact]
    public void StalePlanetAndUnusedChoicesAreRejected()
    {
        var (request, catalog, machines) = Chain(); request.PlanetId++;
        Assert.Equal("invalid_request", Assert.Throws<FoundryPlanningException>(() => FoundryPlanCompiler.Compile(request, catalog, machines)).Reason);
        request.PlanetId = catalog.PlanetId;
        request.RecipeChoices.Add(new FoundryRecipeChoice { ItemId = 1, RecipeId = 999 });
        Assert.Equal("recipe_unavailable", Assert.Throws<FoundryPlanningException>(() => FoundryPlanCompiler.Compile(request, catalog, machines)).Reason);
    }

    [Fact]
    public void OverflowingPowerAndDuplicateRuntimeIdentityFailClosed()
    {
        var (request, catalog, machines) = Chain(); machines[1].WorkEnergyPerTick = long.MaxValue;
        Assert.Equal("budget_overflow", Assert.Throws<FoundryPlanningException>(() => FoundryPlanCompiler.Compile(request, catalog, machines)).Reason);
        catalog.Items.Add(catalog.Items[0]);
        Assert.Equal("catalog_invalid", Assert.Throws<FoundryPlanningException>(() => FoundryPlanCompiler.Compile(request, catalog, machines)).Reason);
    }

    private static (GetFoundryPlanRequest, RecipeCatalogSnapshot, List<BuildCatalogItem>) Chain() =>
        (new GetFoundryPlanRequest { PlanetId = 104, TargetItemId = 4, TargetRatePerMinute = 30 },
        new RecipeCatalogSnapshot
        {
            SessionId = "owned-session", PlanetId = 104, CapturedAtGameTick = 100,
            Items = Enumerable.Range(1, 4).Select(id => new ItemCatalogEntry { ItemId = id, Name = "item " + id, Unlocked = true, IsRaw = id == 1 }).ToList(),
            Recipes = new List<RecipeCatalogEntry>
            {
                Recipe(10, 2, 60, "Smelt", (1, 1)),
                Recipe(20, 3, 60, "Assemble", (2, 2)),
                Recipe(30, 4, 120, "Assemble", (2, 2), (3, 1)),
            },
        },
        new List<BuildCatalogItem>
        {
            new BuildCatalogItem { ItemId = 10, Role = "smelter", RecipeType = "Smelt", Unlocked = true, Available = true, ProductionSpeedRaw = 10000, WorkEnergyPerTick = 6000 },
            new BuildCatalogItem { ItemId = 20, Role = "assembler", RecipeType = "Assemble", Unlocked = true, Available = true, ProductionSpeedRaw = 7500, WorkEnergyPerTick = 3333 },
        });

    private static RecipeCatalogEntry Recipe(int id, int output, int ticks, string type, params (int Item, int Count)[] inputs) => new RecipeCatalogEntry
    {
        RecipeId = id, Name = "recipe " + id, RecipeType = type, Unlocked = true, TimeSpend = ticks,
        Inputs = inputs.Select(x => new CatalogItemAmount { ItemId = x.Item, Count = x.Count }).ToList(),
        Outputs = new List<CatalogItemAmount> { new CatalogItemAmount { ItemId = output, Count = 1 } },
    };
}
