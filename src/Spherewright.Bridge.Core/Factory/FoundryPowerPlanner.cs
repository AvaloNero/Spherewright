using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

// DTO-only copies of actual native network coordinates and bounded load budgets.
public sealed class FoundryPowerContext
{
    public float PlacementShellRadius { get; set; }
    public long CapturedAtGameTick { get; set; }
    public List<FoundryExistingPowerNode> Nodes { get; set; } = new List<FoundryExistingPowerNode>();
    public List<FoundryExistingPowerNetwork> Networks { get; set; } = new List<FoundryExistingPowerNetwork>();
    public List<FoundryUnassignedPowerConsumer> UnassignedConsumers { get; set; } = new List<FoundryUnassignedPowerConsumer>();
}
public sealed class FoundryExistingPowerNode
{
    public int NodeId { get; set; }
    public int NetworkId { get; set; }
    public Vector3Snapshot ProjectedPosition { get; set; } = new Vector3Snapshot();
    public float ConnectionDistanceSquared { get; set; }
    public float CoverRadiusSquared { get; set; }
}
public sealed class FoundryExistingPowerNetwork
{
    public int NetworkId { get; set; }
    public long GenerationCapacityPerTick { get; set; }
    public long ReservedDemandPerTick { get; set; }
    public long ExportPerTick { get; set; }
}
public sealed class FoundryUnassignedPowerConsumer
{
    public int ConsumerId { get; set; }
    public Vector3Snapshot ProjectedPosition { get; set; } = new Vector3Snapshot();
    public long ReservedDemandPerTick { get; set; }
}

public static class FoundryPowerPlanner
{
    public static FoundryPowerAssessment Assess(IReadOnlyList<BlueprintSiteObject> objects,
        IReadOnlyList<BuildCatalogItem> catalog, FoundryPowerContext context)
    {
        if (objects is null || objects.Count < 1 || objects.Count > 32
            || objects.Where((o, i) => o is null || o.Index != i || !Finite(o.Position)).Any()
            || catalog is null || catalog.Count > 512 || catalog.Any(b => b is null)
            || catalog.Select(b => b.ItemId).Distinct().Count() != catalog.Count
            || context is null || !Finite(context.PlacementShellRadius) || context.PlacementShellRadius < 1
            || context.PlacementShellRadius > 10000 || context.CapturedAtGameTick < 0
            || context.Nodes is null || context.Nodes.Count > 512 || context.Nodes.Any(n => n is null)
            || context.Networks is null || context.Networks.Count > 256 || context.Networks.Any(n => n is null)
            || context.UnassignedConsumers is null || context.UnassignedConsumers.Count > 2048
            || context.UnassignedConsumers.Any(c => c is null || c.ConsumerId <= 0 || !Finite(c.ProjectedPosition) || !Energy(c.ReservedDemandPerTick))
            || context.UnassignedConsumers.Select(c => c.ConsumerId).Distinct().Count() != context.UnassignedConsumers.Count)
            throw Reject("bounded_objects_or_networks_invalid");
        var networks = context.Networks;
        if (networks.Any(n => n.NetworkId <= 0 || !Energy(n.GenerationCapacityPerTick)
            || !Energy(n.ReservedDemandPerTick) || !Energy(n.ExportPerTick))
            || networks.Select(n => n.NetworkId).Distinct().Count() != networks.Count
            || context.Nodes.Any(n => n.NodeId <= 0 || !networks.Any(g => g.NetworkId == n.NetworkId)
                || !Finite(n.ProjectedPosition) || !RadiusSquared(n.ConnectionDistanceSquared) || !RadiusSquared(n.CoverRadiusSquared))
            || context.Nodes.Select(n => n.NodeId).Distinct().Count() != context.Nodes.Count
            || networks.Any(n => !context.Nodes.Any(node => node.NetworkId == n.NetworkId)))
            throw Reject("native_network_evidence_invalid");
        var byItem = catalog.ToDictionary(b => b.ItemId);
        var placements = objects.Select(o => byItem.TryGetValue(o.ItemId, out var item) ? item : throw Reject("missing_building_catalog")).ToArray();
        if (placements.Any(p => !p.NativePowerProfileKnown || p.IsPowerCharger && !p.IsPowerNode
            || p.IsPowerCharger && p.IsPowerConsumer || p.WindGenerationAtCurrentPlanetPerTick.HasValue && !p.IsPowerNode))
            throw Reject("planned_power_profile_unknown_or_unsupported");
        var positions = objects.Select(o => Project(o.Position, context.PlacementShellRadius)).ToArray();
        var margin = Math.Max(.002, context.PlacementShellRadius * .00001);
        var geometryUncertain = false;
        bool Covered(Vector3Snapshot a, Vector3Snapshot b, float radius2)
        {
            var distance = Math.Sqrt(((double)a.X - b.X) * (a.X - b.X) + ((double)a.Y - b.Y) * (a.Y - b.Y)
                + ((double)a.Z - b.Z) * (a.Z - b.Z));
            var radius = Math.Sqrt(radius2);
            if (Math.Abs(distance - radius) <= margin) geometryUncertain = true;
            return radius2 > 0 && distance < radius - margin;
        }
        var nodes = context.Nodes.Select(n => new Node(n.ProjectedPosition, n.ConnectionDistanceSquared,
            n.CoverRadiusSquared, n.NetworkId, -1)).ToList();
        var existingNodeCount = nodes.Count;
        foreach (var obj in objects)
        {
            var item = placements[obj.Index];
            if (!item.IsPowerNode) continue;
            if (!Radius(item.PowerConnectDistance) || !Radius(item.PowerCoverRadius)) throw Reject("planned_node_radius_invalid");
            nodes.Add(new Node(positions[obj.Index], item.PowerConnectDistance * item.PowerConnectDistance,
                item.PowerCoverRadius * item.PowerCoverRadius, 0, obj.Index));
        }
        var roots = Enumerable.Range(0, nodes.Count).ToArray();
        int Root(int i) { while (roots[i] != i) { roots[i] = roots[roots[i]]; i = roots[i]; } return i; }
        void Union(int a, int b) { roots[Root(b)] = Root(a); }
        // Existing native network identities already prove internal connectivity. Never
        // merge two old networks merely by rescanning proximity; only new nodes add edges.
        foreach (var group in Enumerable.Range(0, existingNodeCount).GroupBy(i => nodes[i].NetworkId))
            foreach (var index in group.Skip(1)) Union(group.First(), index);
        for (var i = existingNodeCount; i < nodes.Count; i++)
            for (var j = 0; j < i; j++)
                if (Covered(nodes[i].Position, nodes[j].Position, Math.Max(nodes[i].Connection2, nodes[j].Connection2))) Union(i, j);

        var budgets = new Dictionary<int, FoundryPowerComponentBudget>();
        foreach (var group in Enumerable.Range(0, nodes.Count).GroupBy(Root))
        {
            var oldIds = group.Select(i => nodes[i].NetworkId).Where(id => id > 0).Distinct().OrderBy(id => id).ToList();
            var newIndices = group.Select(i => nodes[i].ObjectIndex).Where(i => i >= 0).OrderBy(i => i).ToList();
            var oldNetworks = networks.Where(n => oldIds.Contains(n.NetworkId)).ToArray();
            var wind = newIndices.Select(i => placements[i].WindGenerationAtCurrentPlanetPerTick ?? 0).ToArray();
            if (wind.Any(w => !Energy(w))) throw Reject("planned_generation_invalid");
            budgets.Add(group.Key, new FoundryPowerComponentBudget
            {
                ExistingNetworkIds = oldIds, PlannedNodeIndices = newIndices,
                ExistingGenerationCapacityPerTick = oldNetworks.Sum(n => n.GenerationCapacityPerTick),
                ExistingReservedDemandPerTick = oldNetworks.Sum(n => n.ReservedDemandPerTick),
                ExistingExportPerTick = oldNetworks.Sum(n => n.ExportPerTick), AddedWindGenerationPerTick = wind.Sum(),
            });
        }
        var result = new FoundryPowerAssessment { CapturedAtGameTick = context.CapturedAtGameTick, GeometryMarginMetres = margin };
        foreach (var obj in objects)
        {
            var item = placements[obj.Index];
            if (!item.IsPowerConsumer && !item.IsPowerCharger) continue;
            if (item.WorkEnergyPerTick is not long work || item.IdleEnergyPerTick is not long idle || !Energy(work) || !Energy(idle))
                throw Reject("planned_consumer_demand_unknown");
            var demand = Math.Max(work, idle);
            result.AdditionalBaseWorkPowerWatts = checked(result.AdditionalBaseWorkPowerWatts + demand * 60);
            var covering = Enumerable.Range(0, nodes.Count)
                .Where(i => item.IsPowerCharger ? nodes[i].ObjectIndex == obj.Index
                    : Covered(positions[obj.Index], nodes[i].Position, nodes[i].Cover2))
                .Select(Root).Distinct().ToArray();
            if (covering.Length == 0) { result.UncoveredObjectIndices.Add(obj.Index); continue; }
            if (covering.Length > 1) { result.AmbiguousObjectIndices.Add(obj.Index); continue; }
            var budget = budgets[covering[0]];
            budget.PlannedConsumerIndices.Add(obj.Index); budget.AddedBaseDemandPerTick += demand;
        }
        // OnNodeAdded can energize already-existing net0 consumers. Ignoring those
        // newly attached loads would overstate the headroom of a copied power module.
        foreach (var consumer in context.UnassignedConsumers)
        {
            var covering = Enumerable.Range(existingNodeCount, nodes.Count - existingNodeCount)
                .Where(i => Covered(consumer.ProjectedPosition, nodes[i].Position, nodes[i].Cover2))
                .Select(Root).Distinct().ToArray();
            if (covering.Length > 1) result.AmbiguousExistingConsumerIds.Add(consumer.ConsumerId);
            else if (covering.Length == 1)
            {
                var budget = budgets[covering[0]];
                budget.NewlyCoveredExistingConsumerIds.Add(consumer.ConsumerId);
                budget.NewlyCoveredExistingDemandPerTick += consumer.ReservedDemandPerTick;
            }
        }
        foreach (var budget in budgets.Values.Where(b => b.PlannedConsumerIndices.Count > 0 || b.PlannedNodeIndices.Count > 0))
        {
            var remaining = budget.ExistingGenerationCapacityPerTick + budget.AddedWindGenerationPerTick
                - budget.ExistingReservedDemandPerTick - budget.ExistingExportPerTick - budget.AddedBaseDemandPerTick
                - budget.NewlyCoveredExistingDemandPerTick;
            budget.DeficitPerTick = Math.Max(0, -remaining); budget.HeadroomAfterPlanPerTick = Math.Max(0, remaining);
            result.Components.Add(budget);
        }
        if (result.UncoveredObjectIndices.Count > 0) result.Blockers.Add("planned_consumers_without_power_coverage");
        result.GeometryBoundaryUncertain = geometryUncertain;
        if (geometryUncertain) result.Blockers.Add("native_power_geometry_boundary_uncertain");
        if (result.AmbiguousObjectIndices.Count > 0) result.Blockers.Add("ambiguous_disconnected_covering_networks");
        if (result.AmbiguousExistingConsumerIds.Count > 0) result.Blockers.Add("ambiguous_newly_covered_existing_consumers");
        if (result.Components.Any(b => b.DeficitPerTick > 0)) result.Blockers.Add("full_base_load_generation_deficit");
        result.AllPlannedConsumersCovered = result.UncoveredObjectIndices.Count == 0 && result.AmbiguousObjectIndices.Count == 0;
        result.FullBaseLoadBudgetSatisfied = !geometryUncertain && result.AllPlannedConsumersCovered && result.AmbiguousExistingConsumerIds.Count == 0
            && result.Components.All(b => b.DeficitPerTick == 0);
        result.State = result.FullBaseLoadBudgetSatisfied ? "base_load_budget_ready_at_capture" : "power_plan_incomplete";
        result.AssessmentHash = Fingerprint(objects, placements, context, result);
        return result;
    }

    private static string Fingerprint(IReadOnlyList<BlueprintSiteObject> objects, IReadOnlyList<BuildCatalogItem> placements,
        FoundryPowerContext context, FoundryPowerAssessment result)
    {
        var values = new List<object> { context.PlacementShellRadius, context.CapturedAtGameTick, result.State };
        foreach (var obj in objects)
        {
            var item = placements[obj.Index];
            values.Add(CanonicalStateHash.Combine("planned-power", obj.Index, obj.ItemId, obj.Position.X, obj.Position.Y, obj.Position.Z,
                item.NativePowerProfileKnown, item.IsPowerNode, item.IsPowerConsumer, item.IsPowerCharger,
                item.PowerCoverRadius, item.PowerConnectDistance, item.WorkEnergyPerTick,
                item.IdleEnergyPerTick, item.WindGenerationAtCurrentPlanetPerTick));
        }
        foreach (var node in context.Nodes.OrderBy(n => n.NodeId))
            values.Add(CanonicalStateHash.Combine("native-node", node.NodeId, node.NetworkId, node.ProjectedPosition.X,
                node.ProjectedPosition.Y, node.ProjectedPosition.Z, node.ConnectionDistanceSquared, node.CoverRadiusSquared));
        foreach (var network in context.Networks.OrderBy(n => n.NetworkId))
            values.Add(CanonicalStateHash.Combine("native-network", network.NetworkId, network.GenerationCapacityPerTick,
                network.ReservedDemandPerTick, network.ExportPerTick));
        foreach (var consumer in context.UnassignedConsumers.OrderBy(c => c.ConsumerId))
            values.Add(CanonicalStateHash.Combine("unassigned-consumer", consumer.ConsumerId, consumer.ProjectedPosition.X,
                consumer.ProjectedPosition.Y, consumer.ProjectedPosition.Z, consumer.ReservedDemandPerTick));
        return CanonicalStateHash.Combine("foundry-base-power-v1", values.ToArray());
    }

    private sealed class Node
    {
        public Node(Vector3Snapshot position, float connection2, float cover2, int networkId, int objectIndex)
        { Position = position; Connection2 = connection2; Cover2 = cover2; NetworkId = networkId; ObjectIndex = objectIndex; }
        public Vector3Snapshot Position { get; }
        public float Connection2 { get; }
        public float Cover2 { get; }
        public int NetworkId { get; }
        public int ObjectIndex { get; }
    }
    private static Vector3Snapshot Project(Vector3Snapshot point, float shell)
    {
        var length = Math.Sqrt((double)point.X * point.X + (double)point.Y * point.Y + (double)point.Z * point.Z);
        if (length < 1 || length > 10000) throw Reject("planned_power_position_invalid");
        return new Vector3Snapshot { X = (float)(point.X / length * shell), Y = (float)(point.Y / length * shell), Z = (float)(point.Z / length * shell) };
    }
    private static bool Finite(Vector3Snapshot? v) => v is not null && new[] { v.X, v.Y, v.Z }.All(x => Finite(x) && Math.Abs(x) <= 10000);
    private static bool Finite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
    private static bool Radius(float f) => Finite(f) && f >= 0 && f <= 1000;
    private static bool RadiusSquared(float f) => Finite(f) && f >= 0 && f <= 1000000;
    private static bool Energy(long value) => value >= 0 && value <= 1000000000000L;
    private static FoundryPlanningException Reject(string reason) => new FoundryPlanningException("power_" + reason, "Bounded native power geometry and full base-load evidence are required.");
}
