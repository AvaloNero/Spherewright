using Spherewright.Bridge.Core.Progression;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Factory;
using Spherewright.Contracts.Progression;

namespace Spherewright.Bridge.Core.Factory;

public sealed class FoundryPlanningException : Exception
{
    public FoundryPlanningException(string reason, string message) : base(message) => Reason = reason;

    public string Reason { get; }
}

public static class FoundryPlanCompiler
{
    public const int MaximumStages = 64;
    public const int MaximumDepth = 16;
    public const int MaximumMachines = 256;

    public static FoundryPlanSnapshot Compile(
        GetFoundryPlanRequest request,
        RecipeCatalogSnapshot catalog,
        IReadOnlyList<BuildCatalogItem> buildings)
    {
        if (request is null || catalog is null || buildings is null)
            throw new ArgumentNullException(request is null ? nameof(request) : catalog is null ? nameof(catalog) : nameof(buildings));
        if (request.PlanetId <= 0 || request.PlanetId != catalog.PlanetId
            || string.IsNullOrWhiteSpace(catalog.SessionId) || request.TargetItemId <= 0
            || request.TargetRatePerMinute <= 0m || request.TargetRatePerMinute > 1000000m
            || request.ExternalSupplyItemIds is null || request.RecipeChoices is null
            || request.ExternalSupplyItemIds.Count > MaximumStages || request.RecipeChoices.Count > MaximumStages)
            throw Reject("invalid_request", "A local planet, target item, rate in (0, 1000000], and bounded choices are required.");
        if (catalog.Items is null || catalog.Recipes is null || catalog.Items.Count > 12000 || catalog.Recipes.Count > 12000
            || buildings.Count > 512 || catalog.Items.Any(x => x is null) || catalog.Recipes.Any(x => x is null)
            || buildings.Any(x => x is null) || request.RecipeChoices.Any(x => x is null))
            throw Reject("catalog_invalid", "The runtime catalog is missing or exceeds its bound.");
        if (catalog.Items.Any(x => x.ItemId <= 0) || catalog.Items.Select(x => x.ItemId).Distinct().Count() != catalog.Items.Count
            || catalog.Recipes.Any(x => x.RecipeId <= 0) || catalog.Recipes.Select(x => x.RecipeId).Distinct().Count() != catalog.Recipes.Count
            || buildings.Any(x => x.ItemId <= 0) || buildings.Select(x => x.ItemId).Distinct().Count() != buildings.Count)
            throw Reject("catalog_invalid", "Runtime identities must be positive and unique.");

        var items = catalog.Items.ToDictionary(x => x.ItemId);
        if (!items.TryGetValue(request.TargetItemId, out var target) || !target.Unlocked)
            throw Reject("target_locked", "The target item must exist and be unlocked.");
        var external = new HashSet<int>(request.ExternalSupplyItemIds);
        if (external.Count != request.ExternalSupplyItemIds.Count || external.Any(id => !items.ContainsKey(id))
            || external.Contains(request.TargetItemId))
            throw Reject("invalid_supply", "External supplies must be unique current items and cannot include the target.");
        if (request.RecipeChoices.Select(x => x.ItemId).Distinct().Count() != request.RecipeChoices.Count)
            throw Reject("invalid_choice", "Only one recipe/building choice is allowed per item.");
        var choices = request.RecipeChoices.ToDictionary(x => x.ItemId);
        if (choices.Values.Any(x => !items.ContainsKey(x.ItemId) || x.RecipeId <= 0 || x.BuildingItemId < 0
            || external.Contains(x.ItemId)))
            throw Reject("invalid_choice", "Each choice must identify an internal item and a positive recipe.");

        foreach (var recipe in catalog.Recipes)
        {
            if (recipe.Inputs is null || recipe.Outputs is null || recipe.Inputs.Count > 32 || recipe.Outputs.Count > 32
                || recipe.Inputs.Any(x => x is null || x.Count <= 0 || !items.ContainsKey(x.ItemId))
                || recipe.Outputs.Any(x => x is null || x.Count <= 0 || !items.ContainsKey(x.ItemId))
                || recipe.Inputs.Select(x => x.ItemId).Distinct().Count() != recipe.Inputs.Count
                || recipe.Outputs.Select(x => x.ItemId).Distinct().Count() != recipe.Outputs.Count)
                throw Reject("catalog_invalid", "Every recipe amount must reference one unique positive runtime item/count.");
        }

        // Reuse the shared runtime dependency graph; do not maintain a second
        // hard-coded DSP recipe model or manufacture unavailable recipes.
        var graph = RuntimeDependencyGraphBuilder.Build(target.ItemId, target.Name, catalog.Recipes);
        var reachable = new HashSet<int>(graph.RecipeIds);
        var producers = catalog.Recipes.Where(r => reachable.Contains(r.RecipeId) && r.Unlocked && r.TimeSpend > 0)
            .SelectMany(r => r.Outputs.Select(o => new { o.ItemId, Recipe = r }))
            .GroupBy(x => x.ItemId).ToDictionary(g => g.Key, g => g.Select(x => x.Recipe).ToList());
        var selected = new Dictionary<int, (RecipeCatalogEntry Recipe, BuildCatalogItem Building)>();
        var visiting = new HashSet<int>();
        var visited = new HashSet<int>();
        var order = new List<int>();
        var depth = new Dictionary<int, int>();
        var usedExternal = new HashSet<int>();

        void Visit(int itemId, int level)
        {
            if (level > MaximumDepth) throw Reject("depth_limit", "The selected chain exceeds 16 recipe levels.");
            if (external.Contains(itemId) || (items[itemId].IsRaw && !choices.ContainsKey(itemId) && itemId != target.ItemId))
            {
                usedExternal.Add(itemId);
                depth[itemId] = 0;
                return;
            }
            if (visiting.Contains(itemId)) throw Reject("recipe_cycle", "The selected recipes form a cycle; select another recipe or an explicit external supply.");
            if (visited.Contains(itemId)) return;
            visiting.Add(itemId);
            if (selected.Count >= MaximumStages) throw Reject("stage_limit", "The selected plan exceeds 64 stages.");
            var candidates = producers.TryGetValue(itemId, out var available) ? available : new List<RecipeCatalogEntry>();
            choices.TryGetValue(itemId, out var choice);
            var compatible = candidates.Where(r => choice is null || r.RecipeId == choice.RecipeId)
                .SelectMany(r => buildings.Where(b => Supports(b, r) && (choice is null || choice.BuildingItemId == 0 || choice.BuildingItemId == b.ItemId))
                    .Select(b => (Recipe: r, Building: b)))
                .OrderBy(x => x.Recipe.Outputs.Count == 1 ? 0 : 1)
                .ThenBy(x => x.Recipe.RecipeId).ThenBy(x => x.Building.Grade).ThenBy(x => x.Building.ItemId).ToList();
            if (compatible.Count == 0)
                throw Reject("recipe_unavailable", $"Item {itemId} has no selected unlocked recipe and supported production building; an external supply must be explicit.");
            selected.Add(itemId, compatible[0]);
            foreach (var input in compatible[0].Recipe.Inputs.OrderBy(x => x.ItemId)) Visit(input.ItemId, level + 1);
            depth[itemId] = 1 + compatible[0].Recipe.Inputs.Select(x => depth[x.ItemId]).DefaultIfEmpty(0).Max();
            visiting.Remove(itemId);
            visited.Add(itemId);
            order.Add(itemId);
        }

        Visit(request.TargetItemId, 0);
        if (choices.Keys.Any(id => !selected.ContainsKey(id)) || external.Any(id => !usedExternal.Contains(id)))
            throw Reject("unused_choice", "A recipe choice or external supply is outside the selected chain.");

        var demand = new Dictionary<int, decimal> { [request.TargetItemId] = request.TargetRatePerMinute };
        var stages = new Dictionary<int, FoundryStage>();
        var byproducts = new Dictionary<int, decimal>();
        var plan = new FoundryPlanSnapshot
        {
            SessionId = catalog.SessionId, PlanetId = catalog.PlanetId, CapturedAtGameTick = catalog.CapturedAtGameTick,
            TargetItemId = request.TargetItemId, TargetRatePerMinute = request.TargetRatePerMinute,
            ProductionDepth = depth[request.TargetItemId],
            RemainingChecks = new List<string>
            {
                "Bind the draft to a fresh site; validate terrain, every entity footprint and exact logistics endpoints.",
                "Budget and validate belts, sorters, storage and power infrastructure in addition to MachineCost.",
                "Prove external automatic supplies and byproduct sinks at the declared rates; finite inventory is not sustained supply.",
                "Full-power unproliferated capacity is theoretical; verify actual production with Overseer after ordinary construction.",
            },
        };
        try
        {
            // Reverse topological order aggregates all consumers of a shared
            // intermediate before sizing its producer (no per-branch undercount).
            foreach (var itemId in order.AsEnumerable().Reverse())
            {
                var (recipe, building) = selected[itemId];
                var amount = recipe.Outputs.Single(x => x.ItemId == itemId).Count;
                var executions = demand[itemId] / amount;
                if (executions <= 0m)
                    throw Reject("budget_underflow", "The requested rate is too small to represent every recipe flow safely.");
                var rate = checked(3600m * building.ProductionSpeedRaw!.Value * amount / (recipe.TimeSpend * 10000m));
                var countDecimal = decimal.Ceiling(checked(demand[itemId] * recipe.TimeSpend * 10000m)
                    / (3600m * building.ProductionSpeedRaw.Value * amount));
                if (countDecimal > MaximumMachines || plan.MachineCount + countDecimal > MaximumMachines)
                    throw Reject("machine_limit", "The requested rate requires more than 256 production machines.");
                var count = (int)countDecimal;
                var power = checked(building.WorkEnergyPerTick!.Value * 60 * count);
                var stage = new FoundryStage
                {
                    StageId = "item-" + itemId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ItemId = itemId, RecipeId = recipe.RecipeId, RecipeName = recipe.Name, BuildingItemId = building.ItemId,
                    RecipeExecutionsPerMinute = executions, RequiredRatePerMinute = demand[itemId],
                    PerMachineRatePerMinute = rate, InstalledRatePerMinute = rate * count, MachineCount = count,
                    MachineWorkPowerWatts = power, ProductionDepth = depth[itemId],
                    Dependencies = recipe.Inputs.Where(x => selected.ContainsKey(x.ItemId)).Select(x => "item-" + x.ItemId.ToString(System.Globalization.CultureInfo.InvariantCulture)).OrderBy(x => x, StringComparer.Ordinal).ToList(),
                };
                foreach (var input in recipe.Inputs.OrderBy(x => x.ItemId))
                {
                    var required = checked(executions * input.Count);
                    if (required <= 0m)
                        throw Reject("budget_underflow", "The requested rate is too small to represent every recipe flow safely.");
                    stage.Inputs.Add(new FoundryFlow { ItemId = input.ItemId, RatePerMinute = required });
                    Add(demand, input.ItemId, required);
                }
                foreach (var output in recipe.Outputs.OrderBy(x => x.ItemId))
                {
                    var produced = checked(executions * output.Count);
                    stage.Outputs.Add(new FoundryFlow { ItemId = output.ItemId, RatePerMinute = produced });
                    if (output.ItemId != itemId) Add(byproducts, output.ItemId, produced);
                }
                plan.MachineCount = checked(plan.MachineCount + count);
                plan.MachineWorkPowerWatts = checked(plan.MachineWorkPowerWatts + power);
                stages.Add(itemId, stage);
            }
        }
        catch (OverflowException) { throw Reject("budget_overflow", "The requested recipe and rate exceed the numeric budget."); }
        plan.Stages = order.Select(id => stages[id]).ToList();
        plan.ExternalInputs = usedExternal.OrderBy(id => id).Select(id => new FoundryFlow { ItemId = id, RatePerMinute = demand[id] }).ToList();
        plan.Byproducts = byproducts.OrderBy(x => x.Key).Select(x => new FoundryFlow { ItemId = x.Key, RatePerMinute = x.Value }).ToList();
        plan.MachineCost = plan.Stages.GroupBy(x => x.BuildingItemId).OrderBy(g => g.Key)
            .Select(g => new FoundryMaterialCost { ItemId = g.Key, Count = g.Sum(x => x.MachineCount) }).ToList();
        // Coproducts are explicit extra outputs, never silently credited as a
        // second free supply or silently discarded by the recipe graph.
        plan.PlanHash = Fingerprint(plan, selected);
        return plan;
    }

    private static bool Supports(BuildCatalogItem building, RecipeCatalogEntry recipe) =>
        building.Unlocked && building.Available && building.ProductionSpeedRaw > 0 && building.WorkEnergyPerTick >= 0
        && ((building.Role == "matrix-lab" && recipe.RecipeType == "Research")
            || ((building.Role == "assembler" || building.Role == "smelter" || building.Role == "refinery")
                && string.Equals(building.RecipeType, recipe.RecipeType, StringComparison.Ordinal)));

    private static void Add(Dictionary<int, decimal> totals, int id, decimal amount) =>
        totals[id] = checked((totals.TryGetValue(id, out var previous) ? previous : 0m) + amount);

    private static string Fingerprint(FoundryPlanSnapshot plan, Dictionary<int, (RecipeCatalogEntry Recipe, BuildCatalogItem Building)> selected)
    {
        var fields = new List<object?> { "foundry-material-v1", plan.RateBasis, plan.PlanetId, plan.TargetItemId, plan.TargetRatePerMinute };
        foreach (var stage in plan.Stages)
        {
            var entry = selected[stage.ItemId];
            fields.Add(CanonicalStateHash.Combine("stage", stage.ItemId, stage.RecipeId, stage.BuildingItemId,
                entry.Recipe.RecipeType, entry.Recipe.TimeSpend, entry.Building.ProductionSpeedRaw, entry.Building.WorkEnergyPerTick,
                D(stage.RequiredRatePerMinute), stage.MachineCount));
            // Proportional batch-size changes can preserve demand and machine
            // count while changing capacity; hash the recipe, not only totals.
            foreach (var input in entry.Recipe.Inputs.OrderBy(x => x.ItemId))
                fields.Add(CanonicalStateHash.Combine("recipe-input", input.ItemId, input.Count));
            foreach (var output in entry.Recipe.Outputs.OrderBy(x => x.ItemId))
                fields.Add(CanonicalStateHash.Combine("recipe-output", output.ItemId, output.Count));
            foreach (var flow in stage.Inputs) fields.Add(CanonicalStateHash.Combine("input", flow.ItemId, D(flow.RatePerMinute)));
            foreach (var flow in stage.Outputs) fields.Add(CanonicalStateHash.Combine("output", flow.ItemId, D(flow.RatePerMinute)));
        }
        foreach (var flow in plan.ExternalInputs) fields.Add(CanonicalStateHash.Combine("supply", flow.ItemId, D(flow.RatePerMinute)));
        return CanonicalStateHash.Combine("foundry-material-plan", fields.Select(value => value is decimal number
            ? (object)number.ToString("G29", System.Globalization.CultureInfo.InvariantCulture) : value).ToArray());
    }

    private static FoundryPlanningException Reject(string code, string message) => new FoundryPlanningException(code, message);

    private static string D(decimal value) => value.ToString("G29", System.Globalization.CultureInfo.InvariantCulture);
}
