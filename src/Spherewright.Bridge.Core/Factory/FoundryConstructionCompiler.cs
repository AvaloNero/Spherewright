using System.Globalization;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

// Composes the existing material compiler and native finite blueprint shape.
// No alternate executor, guessed placement, recipe table or autonomous planner.
public static class FoundryConstructionCompiler
{
    public const int MaximumRoutes = 128;

    public static FoundryConstructionPlan Compile(FoundryPlanSnapshot material, BlueprintSiteSnapshot site,
        IReadOnlyList<FoundryBoundaryPort> ports, IReadOnlyList<BuildCatalogItem> catalog)
    {
        ValidateInputs(material, site, ports, catalog);
        var buildings = catalog.ToDictionary(b => b.ItemId);
        var channelBudget = FoundryTransportPlanner.Assess(site, catalog, Array.Empty<FoundryRoutedFlow>());
        var channelCapacities = channelBudget.Channels.ToDictionary(c => c.ObjectIndex,
            c => c.RatedSingleItemRatePerMinute ?? 0m);
        var result = new FoundryConstructionPlan
        {
            MaterialPlanHash = material.PlanHash, BlueprintHash = site.BlueprintHash,
            ImmutableSiteHash = BlueprintSitePolicy.AssessmentHash(site, "foundry-approved-layout-v1"),
            TargetItemId = material.TargetItemId, TargetRatePerMinute = material.TargetRatePerMinute,
            ProductionDepth = material.ProductionDepth,
            ConstructionCost = site.Objects.GroupBy(o => o.ItemId).OrderBy(g => g.Key)
                .Select(g => new FoundryMaterialCost { ItemId = g.Key, Count = g.Count() }).ToList(),
            Steps = site.Objects.Select(o => new FoundryConstructionStep
            {
                ObjectIndex = o.Index, ItemId = o.ItemId, Role = Role(o.ItemId),
                Dependencies = o.Dependencies.OrderBy(i => i).ToList(),
            }).ToList(),
        };
        var claimed = new HashSet<int>();
        foreach (var stage in material.Stages)
        {
            if (stage.Outputs.Count != 1 || stage.Outputs[0].ItemId != stage.ItemId)
                throw Reject("construction_coproduct_layout_unsupported", "This finite composition requires single-output stages; coproducts are not silently discarded or credited.");
            var indices = site.Objects.Where(o => o.ItemId == stage.BuildingItemId && o.RecipeId == stage.RecipeId)
                .Select(o => o.Index).OrderBy(i => i).ToList();
            if (indices.Count != stage.MachineCount || indices.Any(i => !claimed.Add(i)))
                throw Reject("construction_machine_mismatch", "Every material stage requires its exact recipe, building family and machine count in the chosen blueprint.");
            result.Stages.Add(new FoundryStageBinding
            {
                StageId = stage.StageId, ItemId = stage.ItemId, RecipeId = stage.RecipeId,
                BuildingItemId = stage.BuildingItemId, RequiredRatePerMinute = stage.RequiredRatePerMinute,
                InstalledRatePerMinute = stage.InstalledRatePerMinute, ObjectIndices = indices,
            });
        }
        if (site.Objects.Any(o => IsMachine(o.ItemId) && !claimed.Contains(o.Index)))
            throw Reject("construction_unplanned_machine", "The blueprint contains a production machine outside the approved material graph.");
        var boundaryNeeds = material.ExternalInputs.Select(f => (Item: f.ItemId, Direction: "input", Rate: f.RatePerMinute))
            .Concat(new[] { (Item: material.TargetItemId, Direction: "output", Rate: material.TargetRatePerMinute) }).ToArray();
        if (ports.Count != boundaryNeeds.Length || ports.GroupBy(p => (p.ItemId, p.Direction)).Any(g => g.Count() != 1)
            || ports.GroupBy(p => (p.ObjectIndex, p.Slot)).Any(g => g.Count() != 1))
            throw Reject("construction_boundary_mismatch", "Exactly one distinct free port is required for each external input and the target output.");
        foreach (var need in boundaryNeeds.OrderBy(n => n.Direction, StringComparer.Ordinal).ThenBy(n => n.Item))
        {
            var port = ports.SingleOrDefault(p => p.ItemId == need.Item && p.Direction == need.Direction);
            if (port is null) throw Reject("construction_boundary_mismatch", "A required external boundary is missing.");
            var obj = site.Objects[port.ObjectIndex];
            if (!(IsBelt(obj.ItemId) || obj.ItemId == 2101) || port.Slot < 0
                || (IsBelt(obj.ItemId) ? port.Slot != (port.Direction == "input" ? 1 : 0)
                    : port.Slot >= buildings[obj.ItemId].SlotCount)
                || site.Connections.Any(e => (e.FromIndex == port.ObjectIndex && e.FromSlot == port.Slot)
                    || (e.ToIndex == port.ObjectIndex && e.ToSlot == port.Slot)))
                throw Reject("construction_boundary_not_free", "Boundaries must use explicit free native belt ends or storage slots, never occupied ports or machine centres.");
            result.Boundaries.Add(new FoundryBoundaryFlow
            {
                ItemId = port.ItemId, Direction = port.Direction, ObjectIndex = port.ObjectIndex,
                Slot = port.Slot, RequiredRatePerMinute = need.Rate,
            });
        }
        foreach (var itemId in material.Stages.Select(s => s.ItemId).Concat(material.ExternalInputs.Select(f => f.ItemId)).Distinct().OrderBy(i => i))
        {
            var sources = new Dictionary<int, decimal>();
            var sinks = new Dictionary<int, decimal>();
            for (var stageIndex = 0; stageIndex < material.Stages.Count; stageIndex++)
            {
                var stage = material.Stages[stageIndex];
                var indices = result.Stages[stageIndex].ObjectIndices;
                var input = stage.Inputs.SingleOrDefault(f => f.ItemId == itemId);
                if (input is not null) Distribute(sinks, indices, input.RatePerMinute);
                if (stage.ItemId == itemId) Distribute(sources, indices, stage.RequiredRatePerMinute);
            }
            foreach (var port in result.Boundaries.Where(p => p.ItemId == itemId))
                Add(port.Direction == "input" ? sources : sinks, port.ObjectIndex, port.RequiredRatePerMinute);
            if (sources.Values.Sum() != sinks.Values.Sum())
                throw Reject("construction_flow_budget_mismatch", "Source and consumer demand must balance exactly before route allocation.");
            var flow = new ItemFlow(site, itemId, sources, sinks, channelCapacities);
            if (!flow.Route()) result.Blockers.Add("unrouted_item:" + itemId.ToString(CultureInfo.InvariantCulture));
            result.Routes.AddRange(flow.Decompose());
            if (result.Routes.Count > MaximumRoutes)
                throw Reject("construction_route_limit", "The chosen bounded module exceeds128 allocated paths.");
        }
        // A connected graph can still mix items onto a belt or let an unfiltered
        // sorter pull the wrong item from a shared buffer. Reject those layouts.
        var carried = result.Routes.SelectMany(r => r.ObjectIndices.Select(i => (Index: i, r.ItemId)))
            .GroupBy(x => x.Index).ToDictionary(g => g.Key, g => g.Select(x => x.ItemId).Distinct().ToArray());
        foreach (var obj in site.Objects)
        {
            if ((IsBelt(obj.ItemId) || IsSorter(obj.ItemId)) && carried.TryGetValue(obj.Index, out var items) && items.Length > 1)
                result.Blockers.Add("mixed_transport_channel_unsupported:" + obj.Index.ToString(CultureInfo.InvariantCulture));
            if (IsSorter(obj.ItemId) && obj.FilterItemId == 0)
            {
                var input = site.Connections.Single(e => e.ToIndex == obj.Index);
                if (site.Objects[input.FromIndex].ItemId == 2101 && carried.TryGetValue(input.FromIndex, out var stored) && stored.Length > 1)
                    result.Blockers.Add("mixed_storage_requires_sorter_filter:" + obj.Index.ToString(CultureInfo.InvariantCulture));
            }
            if (Role(obj.ItemId) == "logistics" && !carried.ContainsKey(obj.Index))
                result.Blockers.Add("unused_logistics_object:" + obj.Index.ToString(CultureInfo.InvariantCulture));
        }
        result.InternalFlowsRouted = result.Blockers.Count == 0;
        result.TransportBudget = FoundryTransportPlanner.Assess(site, catalog, result.Routes);
        result.Blockers.AddRange(result.TransportBudget.Blockers);
        if (!site.NativeCheckPerformed || !site.NativeCheckPassed || !site.TechnologySatisfied || !site.InventorySufficient)
            result.Blockers.Add("native_site_technology_or_whole_inventory_not_ready");
        result.Blockers.AddRange(site.Blockers.Select(b => "native:" + b));
        if (site.Power is null || !site.Power.AllPlannedConsumersCovered || !site.Power.FullBaseLoadBudgetSatisfied
            || site.Power.GeometryBoundaryUncertain || site.Power.Blockers.Count != 0)
            result.Blockers.Add("full_base_load_power_not_ready");
        result.CanPrepare = result.Blockers.Count == 0;
        result.PlanHash = Fingerprint(result);
        return result;
    }

    public static string Fingerprint(FoundryConstructionPlan plan)
    {
        var fields = new List<object?> { 1, plan.MaterialPlanHash, plan.BlueprintHash, plan.ImmutableSiteHash,
            plan.TargetItemId, D(plan.TargetRatePerMinute), plan.ProductionDepth, plan.RateBasis,
            plan.CanPrepare, plan.InternalFlowsRouted, plan.TransportCapacityVerified,
            FoundryTransportPlanner.Fingerprint(plan.TransportBudget) };
        foreach (var s in plan.Stages)
            fields.Add(CanonicalStateHash.Combine("stage", s.StageId, s.ItemId, s.RecipeId, s.BuildingItemId,
                D(s.RequiredRatePerMinute), D(s.InstalledRatePerMinute), CanonicalStateHash.Combine("indices", s.ObjectIndices.Cast<object>().ToArray())));
        foreach (var p in plan.Boundaries)
            fields.Add(CanonicalStateHash.Combine("port", p.ItemId, p.Direction, p.ObjectIndex, p.Slot, D(p.RequiredRatePerMinute)));
        foreach (var r in plan.Routes)
            fields.Add(CanonicalStateHash.Combine("route", r.ItemId, D(r.RequiredRatePerMinute), CanonicalStateHash.Combine("path", r.ObjectIndices.Cast<object>().ToArray())));
        foreach (var c in plan.ConstructionCost) fields.Add(CanonicalStateHash.Combine("cost", c.ItemId, c.Count));
        foreach (var s in plan.Steps)
            fields.Add(CanonicalStateHash.Combine("step", s.ObjectIndex, s.ItemId, s.Role, CanonicalStateHash.Combine("dependencies", s.Dependencies.Cast<object>().ToArray())));
        foreach (var blocker in plan.Blockers) fields.Add(CanonicalStateHash.Combine("blocker", blocker));
        // Capture-time power counters/availability are separately rechecked, never
        // smuggled into the immutable construction graph as a per-tick stale hash.
        return CanonicalStateHash.Combine("foundry-construction-v1", fields.ToArray());
    }

    public static string IntentHash(FoundryConstructionIntent intent)
    {
        if (intent is null || intent.TargetItemId <= 0 || intent.TargetRatePerMinute <= 0 || intent.TargetRatePerMinute > 1000000
            || intent.ExternalSupplyItemIds is null || intent.ExternalSupplyItemIds.Count > 64
            || intent.RecipeChoices is null || intent.RecipeChoices.Count > 64 || intent.RecipeChoices.Any(c => c is null)
            || intent.BoundaryPorts is null || intent.BoundaryPorts.Count > 33 || intent.BoundaryPorts.Any(p => p is null))
            throw new InvalidDataException("Invalid bounded Foundry intent.");
        var fields = new List<object?> { intent.TargetItemId, D(intent.TargetRatePerMinute) };
        foreach (var id in intent.ExternalSupplyItemIds.OrderBy(i => i)) fields.Add(CanonicalStateHash.Combine("supply", id));
        foreach (var choice in intent.RecipeChoices.OrderBy(c => c.ItemId))
            fields.Add(CanonicalStateHash.Combine("choice", choice.ItemId, choice.RecipeId, choice.BuildingItemId));
        foreach (var p in intent.BoundaryPorts.OrderBy(p => p.Direction, StringComparer.Ordinal).ThenBy(p => p.ItemId))
            fields.Add(CanonicalStateHash.Combine("port", p.ItemId, p.Direction, p.ObjectIndex, p.Slot));
        return CanonicalStateHash.Combine("foundry-intent-v1", fields.ToArray());
    }

    public static void ValidateBinding(FoundryConstructionPlan? plan, BlueprintSiteSnapshot site, FoundryConstructionIntent? intent)
    {
        if (plan is null && intent is null) return;
        if (plan is null || intent is null || site is null || site.Objects is null || site.Objects.Any(o => o is null || o.Dependencies is null)
            || plan.SchemaVersion != 1 || !plan.CanPrepare || !plan.InternalFlowsRouted
            || plan.TransportBudget is null || !plan.TransportBudget.AllCapacitiesKnown || !plan.TransportBudget.Satisfied
            || plan.TargetItemId != intent.TargetItemId || plan.TargetRatePerMinute != intent.TargetRatePerMinute
            || plan.BlueprintHash != site.BlueprintHash || plan.Stages is null || plan.Stages.Count < 1 || plan.Stages.Count > 32
            || plan.Stages.Any(s => s is null || s.ObjectIndices is null || s.ObjectIndices.Count < 1 || s.ObjectIndices.Count > 32)
            || plan.Routes is null || plan.Routes.Count > MaximumRoutes
            || plan.Routes.Any(r => r is null || r.ObjectIndices is null || r.ObjectIndices.Count < 2 || r.ObjectIndices.Count > 32 || r.RequiredRatePerMinute <= 0)
            || plan.Boundaries is null || plan.Boundaries.Count > 33 || plan.Boundaries.Any(p => p is null)
            || plan.ConstructionCost is null || plan.ConstructionCost.Count > 32 || plan.ConstructionCost.Any(c => c is null || c.Count < 1)
            || plan.Steps is null || plan.Steps.Count != site.Objects.Count
            || plan.Steps.Where((s, i) => s is null || s.ObjectIndex != i || s.ItemId != site.Objects[i].ItemId
                || s.Dependencies is null || s.Dependencies.Count > 32 || !s.Dependencies.SequenceEqual(site.Objects[i].Dependencies.OrderBy(n => n))).Any()
            || plan.Blockers is null || plan.Blockers.Count != 0
            || plan.ImmutableSiteHash != BlueprintSitePolicy.AssessmentHash(site, "foundry-approved-layout-v1")
            || plan.PlanHash != Fingerprint(plan))
            throw new InvalidDataException("Foundry intent, whole graph or approved native site was changed.");
        IntentHash(intent);
    }

    private static void ValidateInputs(FoundryPlanSnapshot m, BlueprintSiteSnapshot s,
        IReadOnlyList<FoundryBoundaryPort> ports, IReadOnlyList<BuildCatalogItem> catalog)
    {
        if (m is null || s is null || ports is null || catalog is null
            || m.PlanetId <= 0 || m.PlanetId != s.PlanetId || m.SessionId != s.SessionId
            || string.IsNullOrWhiteSpace(m.PlanHash) || string.IsNullOrWhiteSpace(s.BlueprintHash)
            || m.Stages is null || m.Stages.Count < 1 || m.Stages.Count > 32 || m.MachineCount > 32
            || m.ExternalInputs is null || m.ExternalInputs.Count > 32 || m.Byproducts is null || m.Byproducts.Count != 0
            || s.Objects is null || s.Objects.Count < 1 || s.Objects.Count > 32
            || s.Connections is null || s.Connections.Count > 512 || s.ConstructionItems is null || s.ConstructionItems.Count > 32
            || s.Blockers is null || s.Blockers.Count > 64 || ports.Count > 33
            || catalog.Count > 512 || catalog.Any(c => c is null || c.ItemId <= 0)
            || catalog.Select(c => c.ItemId).Distinct().Count() != catalog.Count)
            throw Reject("construction_input_invalid", "Fresh bounded material/site/catalog data with single-output stages is required.");
        var ids = new HashSet<int>(catalog.Select(c => c.ItemId));
        if (s.Objects.Where((o, i) => o is null || o.Index != i || !BoundedBlueprintReader.SupportsItem(o.ItemId)
                || !ids.Contains(o.ItemId) || o.Dependencies is null || o.Dependencies.Count > 32
                || o.Dependencies.Any(d => d < 0 || d >= s.Objects.Count || d == i) || o.Parameters is null || o.Parameters.Length > 128).Any()
            || s.Connections.Any(e => e is null || e.FromIndex < 0 || e.ToIndex < 0 || e.FromIndex >= s.Objects.Count
                || e.ToIndex >= s.Objects.Count || e.FromIndex == e.ToIndex || e.FromSlot < -1 || e.ToSlot < -1 || e.FromSlot >= 16 || e.ToSlot >= 16)
            || s.Connections.GroupBy(e => (e.FromIndex, e.FromSlot, e.ToIndex, e.ToSlot)).Any(g => g.Count() != 1)
            || ports.Any(p => p is null || p.ItemId <= 0 || (p.Direction != "input" && p.Direction != "output")
                || p.ObjectIndex < 0 || p.ObjectIndex >= s.Objects.Count || p.Slot < 0 || p.Slot >= 16)
            || m.Stages.Any(t => t is null || !IsMachine(t.BuildingItemId) || t.MachineCount < 1
                || t.Inputs is null || t.Outputs is null || t.Inputs.Count > 32 || t.Outputs.Count > 32
                || t.Inputs.Any(f => f is null || f.ItemId <= 0 || f.RatePerMinute <= 0)
                || t.Outputs.Any(f => f is null || f.ItemId <= 0 || f.RatePerMinute <= 0)
                || t.Inputs.Select(f => f.ItemId).Distinct().Count() != t.Inputs.Count))
            throw Reject("construction_shape_invalid", "Invalid bounded object, flow or port identities.");
        foreach (var o in s.Objects)
        {
            if (o.ItemId == 2101 && (!BlueprintStoragePolicy.IsSupportedShape(o.Parameters)
                || o.Parameters[0] != 0 || o.Parameters[1] != 0))
                throw Reject("construction_storage_layout_unsupported", "Finite flow composition currently requires default unbanned storage; filters/bans are not silently ignored.");
            if (IsSorter(o.ItemId) && (s.Connections.Count(e => e.FromIndex == o.Index) != 1
                || s.Connections.Count(e => e.ToIndex == o.Index) != 1))
                throw Reject("construction_sorter_endpoints_invalid", "Each planned sorter requires both exact internal directed endpoints.");
        }
        var actual = s.Objects.GroupBy(o => o.ItemId).ToDictionary(g => g.Key, g => g.Count());
        if (s.ConstructionItems.Any(c => c is null) || s.ConstructionItems.Count != actual.Count
            || s.ConstructionItems.Select(c => c.ItemId).Distinct().Count() != actual.Count
            || s.ConstructionItems.Any(c => !actual.TryGetValue(c.ItemId, out var count) || c.RequiredCount != count))
            throw Reject("construction_cost_mismatch", "Whole construction budget must include every machine, logistic and power object exactly once.");
    }

    // Node-split, bounded per-item max flow. Machines can produce or consume, never
    // relay arbitrary goods. Residual rerouting avoids greedy source double-counting.
    private sealed class ItemFlow
    {
        private readonly int _item;
        private readonly int _source;
        private readonly int _sink;
        private readonly decimal[,] _capacity;
        private readonly decimal[,] _residual;
        private readonly decimal _demand;

        public ItemFlow(BlueprintSiteSnapshot site, int item, Dictionary<int, decimal> sources, Dictionary<int, decimal> sinks,
            IReadOnlyDictionary<int, decimal> channelCapacities)
        {
            _item = item; _source = site.Objects.Count * 2; _sink = _source + 1;
            _capacity = new decimal[_sink + 1, _sink + 1]; _demand = sinks.Values.Sum();
            foreach (var o in site.Objects)
                if (Role(o.ItemId) == "logistics" && (!IsSorter(o.ItemId) || o.FilterItemId == 0 || o.FilterItemId == item))
                    _capacity[o.Index * 2, o.Index * 2 + 1] = channelCapacities.TryGetValue(o.Index, out var limit)
                        ? Math.Min(_demand, limit) : _demand;
            foreach (var e in site.Connections) _capacity[e.FromIndex * 2 + 1, e.ToIndex * 2] = _demand;
            // Boundary belts must traverse their own capacity edge as well. Machines
            // produce from their out-node and consume at their in-node, never relaying.
            foreach (var p in sources) _capacity[_source, p.Key * 2 + (IsMachine(site.Objects[p.Key].ItemId) ? 1 : 0)] = p.Value;
            foreach (var p in sinks) _capacity[p.Key * 2 + (IsMachine(site.Objects[p.Key].ItemId) ? 0 : 1), _sink] = p.Value;
            _residual = (decimal[,])_capacity.Clone();
        }

        public bool Route()
        {
            var total = 0m; var iterations = 0;
            while (Path(_residual) is { } path)
            {
                if (++iterations > 4096) throw Reject("construction_flow_work_limit", "Bounded flow allocation exhausted its work budget.");
                var amount = path.Zip(path.Skip(1), (a, b) => _residual[a, b]).Min();
                for (var i = 1; i < path.Count; i++)
                { _residual[path[i - 1], path[i]] -= amount; _residual[path[i], path[i - 1]] += amount; }
                total += amount;
            }
            return total == _demand;
        }

        public List<FoundryRoutedFlow> Decompose()
        {
            var used = new decimal[_sink + 1, _sink + 1];
            for (var a = 0; a <= _sink; a++) for (var b = 0; b <= _sink; b++)
                if (_capacity[a, b] > 0) used[a, b] = Math.Max(0m, _capacity[a, b] - _residual[a, b]);
            var result = new List<FoundryRoutedFlow>();
            while (Path(used) is { } path)
            {
                if (result.Count >= MaximumRoutes) throw Reject("construction_route_limit", "Flow decomposition exceeded128 paths.");
                var amount = path.Zip(path.Skip(1), (a, b) => used[a, b]).Min();
                for (var i = 1; i < path.Count; i++) used[path[i - 1], path[i]] -= amount;
                result.Add(new FoundryRoutedFlow { ItemId = _item, RequiredRatePerMinute = amount,
                    ObjectIndices = path.Where(i => i < _source).Select(i => i / 2).Distinct().ToList() });
            }
            return result;
        }

        private List<int>? Path(decimal[,] capacities)
        {
            var previous = Enumerable.Repeat(-1, _sink + 1).ToArray();
            var queue = new Queue<int>(); queue.Enqueue(_source); previous[_source] = _source;
            while (queue.Count != 0 && previous[_sink] < 0)
            {
                var current = queue.Dequeue();
                for (var next = 0; next <= _sink; next++)
                    if (previous[next] < 0 && capacities[current, next] > 0)
                    { previous[next] = current; queue.Enqueue(next); }
            }
            if (previous[_sink] < 0) return null;
            var result = new List<int>();
            for (var at = _sink; at != _source; at = previous[at]) result.Add(at);
            result.Add(_source); result.Reverse(); return result;
        }
    }

    private static void Distribute(Dictionary<int, decimal> values, List<int> indices, decimal total)
    {
        var remaining = total;
        for (var i = 0; i < indices.Count; i++)
        {
            var share = i == indices.Count - 1 ? remaining : total / indices.Count;
            Add(values, indices[i], share); remaining -= share;
        }
    }

    private static void Add(Dictionary<int, decimal> values, int index, decimal amount) =>
        values[index] = checked((values.TryGetValue(index, out var old) ? old : 0m) + amount);
    private static bool IsMachine(int id) => id >= 2302 && id <= 2305;
    private static bool IsBelt(int id) => id >= 2001 && id <= 2003;
    private static bool IsSorter(int id) => id >= 2011 && id <= 2013;
    private static string Role(int id) => IsMachine(id) ? "production" : id == 2201 || id == 2203 ? "power" : "logistics";
    private static string D(decimal value) => value.ToString("G29", CultureInfo.InvariantCulture);
    private static FoundryPlanningException Reject(string reason, string message) => new FoundryPlanningException(reason, message);
}
