using System.Globalization;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

// Rated single-item capacity only. See game-api-foundry.md for the current DLL
// call chain; source starvation, merging/fairness, backpressure and power outages
// still require real observations. No stack or proliferation bonus is credited.
public static class FoundryTransportPlanner
{
    public static FoundryTransportBudget Assess(BlueprintSiteSnapshot site,
        IReadOnlyList<BuildCatalogItem> catalog, IReadOnlyList<FoundryRoutedFlow> routes)
    {
        if (site is null || site.Objects is null || site.Objects.Count > 32
            || site.Objects.Where((o, i) => o is null || o.Index != i || o.Parameters is null).Any()
            || catalog is null || catalog.Count > 512 || catalog.Any(c => c is null)
            || catalog.GroupBy(c => c.ItemId).Any(g => g.Count() != 1)
            || routes is null || routes.Count > FoundryConstructionCompiler.MaximumRoutes
            || routes.Any(r => r is null || r.RequiredRatePerMinute <= 0 || r.RequiredRatePerMinute > 1000000
                || r.ObjectIndices is null || r.ObjectIndices.Count > 32
                || r.ObjectIndices.Distinct().Count() != r.ObjectIndices.Count
                || r.ObjectIndices.Any(i => i < 0 || i >= site.Objects.Count)))
            throw new InvalidDataException("Invalid bounded transport budget inputs.");
        var items = catalog.ToDictionary(c => c.ItemId);
        var result = new FoundryTransportBudget();
        foreach (var obj in site.Objects.Where(o => IsChannel(o.ItemId)))
        {
            var channel = new FoundryTransportChannel
            {
                ObjectIndex = obj.Index, BuildingItemId = obj.ItemId,
                AllocatedRatePerMinute = routes.Where(r => r.ObjectIndices.Contains(obj.Index)).Sum(r => r.RequiredRatePerMinute),
            };
            result.Channels.Add(channel);
            if (items.TryGetValue(obj.ItemId, out var item))
            {
                if (obj.ItemId >= 2001 && obj.ItemId <= 2003)
                {
                    channel.BeltSpeedRaw = item.BeltSpeedRaw;
                    // CargoPath.kCargoLength = 10; native chunks advance speed cells/tick.
                    // Bound to the native maximum 120 cargo/s, not an item-name table.
                    if (item.BeltSpeedRaw >= 1 && item.BeltSpeedRaw <= 20)
                        channel.RatedSingleItemRatePerMinute = item.BeltSpeedRaw.Value * 360m;
                }
                else if ((obj.ItemId == 2011 || obj.ItemId == 2012) && item.InserterGrade == obj.ItemId - 2010)
                {
                    channel.InserterSttRaw = item.InserterSttRaw;
                    if (obj.Parameters.Length == 1 && obj.Parameters[0] >= 1 && obj.Parameters[0] <= 3)
                    {
                        channel.InserterSpan = obj.Parameters[0];
                        channel.IdealCycleGameTicks = BasicSorterCycleTicks(item.InserterSttRaw, obj.Parameters[0]);
                        if (channel.IdealCycleGameTicks.HasValue)
                            channel.RatedSingleItemRatePerMinute = 3600m / channel.IdealCycleGameTicks.Value;
                    }
                }
            }
            if (!channel.RatedSingleItemRatePerMinute.HasValue)
                result.Blockers.Add("native_transport_capacity_unavailable:" + obj.Index.ToString(CultureInfo.InvariantCulture));
            else if (channel.AllocatedRatePerMinute > channel.RatedSingleItemRatePerMinute.Value)
                result.Blockers.Add("native_transport_capacity_exceeded:" + obj.Index.ToString(CultureInfo.InvariantCulture));
        }
        result.AllCapacitiesKnown = result.Channels.All(c => c.RatedSingleItemRatePerMinute.HasValue);
        result.Satisfied = result.Blockers.Count == 0;
        return result;
    }

    public static int? BasicSorterCycleTicks(int? nativeStt, int span)
    {
        if (!nativeStt.HasValue || nativeStt <= 0 || span < 1 || span > 3) return null;
        var stt = Math.Max(10000L, (long)nativeStt.Value * span);
        if (stt > int.MaxValue) return null;
        // Full power advances 10000/time tick. Picking and inserting also advance
        // time, but Sending and Returning each execute at least once. The native
        // remainder after Sending is retained through Inserting, then reset on return.
        return (int)Math.Max(4L, (2L * stt + 9999L) / 10000L);
    }

    public static string Fingerprint(FoundryTransportBudget budget)
    {
        if (budget is null || budget.Channels is null || budget.Channels.Count > 32
            || budget.Channels.Any(c => c is null) || budget.Blockers is null || budget.Blockers.Count > 32)
            throw new InvalidDataException("Invalid bounded transport budget record.");
        var fields = new List<object?> { budget.Basis, budget.AllCapacitiesKnown, budget.Satisfied };
        foreach (var c in budget.Channels)
            fields.Add(CanonicalStateHash.Combine("channel", c.ObjectIndex, c.BuildingItemId,
                D(c.AllocatedRatePerMinute), c.RatedSingleItemRatePerMinute.HasValue ? D(c.RatedSingleItemRatePerMinute.Value) : null,
                c.BeltSpeedRaw, c.InserterSttRaw, c.InserterSpan, c.IdealCycleGameTicks));
        foreach (var b in budget.Blockers) fields.Add(CanonicalStateHash.Combine("blocker", b));
        return CanonicalStateHash.Combine("foundry-native-transport-v1", fields.ToArray());
    }

    private static bool IsChannel(int id) => (id >= 2001 && id <= 2003) || (id >= 2011 && id <= 2013);
    private static string D(decimal value) => value.ToString("G29", CultureInfo.InvariantCulture);
}
