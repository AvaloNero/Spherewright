using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class FoundryTransportPlannerTests
{
    [Theory]
    [InlineData(1, 360)] [InlineData(2, 720)] [InlineData(5, 1800)]
    public void BeltCapacityComesFromRuntimeCellsPerTickAndTenCellSingleCargo(int speed, int rate)
    {
        var (site, catalog) = Channel(2001, speed: speed);
        var result = FoundryTransportPlanner.Assess(site, catalog, Array.Empty<FoundryRoutedFlow>());
        Assert.True(result.AllCapacitiesKnown); Assert.True(result.Satisfied);
        Assert.Equal(rate, result.Channels.Single().RatedSingleItemRatePerMinute);
        Assert.Contains("single_item", result.Basis);
    }

    [Theory]
    [InlineData(2011, 200000, 1, 40, 90)]
    [InlineData(2011, 200000, 2, 80, 45)]
    [InlineData(2011, 200000, 3, 120, 30)]
    [InlineData(2012, 100000, 1, 20, 180)]
    [InlineData(2012, 100000, 3, 60, 60)]
    public void NativeBasicSorterRoundTripIncludesThePreviewSpan(int item, int stt, int span, int ticks, int rate)
    {
        var (site, catalog) = Channel(item, stt: stt, span: span);
        var c = FoundryTransportPlanner.Assess(site, catalog, Array.Empty<FoundryRoutedFlow>()).Channels.Single();
        Assert.Equal(ticks, c.IdealCycleGameTicks); Assert.Equal(rate, c.RatedSingleItemRatePerMinute);
        Assert.Equal(stt, c.InserterSttRaw); Assert.Equal(span, c.InserterSpan);
    }

    [Fact]
    public void QuantizedRoundTripAgreesWithIndependentFourPhaseArithmeticIncludingShortSpans()
    {
        // Synthetic arithmetic oracle, not a game execution claim. Includes native
        // stt clamp, fractions of the 10000 increment and the one-tick phase minima.
        for (var raw = 1; raw <= 200000; raw += 137)
        for (var span = 1; span <= 3; span++)
        {
            var stt = Math.Max(10000, raw * span); var time = 10000; var ticks = 1;
            do { time += 10000; ticks++; } while (time < stt);
            time -= stt; time += 10000; ticks++;
            do { time += 10000; ticks++; } while (time < stt);
            Assert.Equal(ticks, FoundryTransportPlanner.BasicSorterCycleTicks(raw, span));
        }
    }

    [Theory]
    [InlineData("missing_belt")] [InlineData("zero_belt")] [InlineData("too_fast_belt")]
    [InlineData("missing_stt")] [InlineData("zero_stt")] [InlineData("overflow")]
    [InlineData("span")] [InlineData("grade")] [InlineData("stacking_sorter")]
    public void MissingOrUnsupportedNativeEvidenceNeverBecomesFreeCapacity(string change)
    {
        var belt = change.Contains("belt");
        var (site, catalog) = Channel(belt ? 2001 : 2011, speed: 1, stt: 200000);
        switch (change)
        {
            case "missing_belt": catalog[0].BeltSpeedRaw = null; break;
            case "zero_belt": catalog[0].BeltSpeedRaw = 0; break;
            case "too_fast_belt": catalog[0].BeltSpeedRaw = 21; break;
            case "missing_stt": catalog[0].InserterSttRaw = null; break;
            case "zero_stt": catalog[0].InserterSttRaw = 0; break;
            case "overflow": catalog[0].InserterSttRaw = int.MaxValue; site.Objects[0].Parameters[0] = 3; break;
            case "span": site.Objects[0].Parameters = new[] { 4 }; break;
            case "grade": catalog[0].InserterGrade = 2; break;
            case "stacking_sorter": catalog[0].ItemId = site.Objects[0].ItemId = 2013; catalog[0].InserterGrade = 3; break;
        }
        var r = FoundryTransportPlanner.Assess(site, catalog, Array.Empty<FoundryRoutedFlow>());
        Assert.False(r.AllCapacitiesKnown); Assert.False(r.Satisfied);
        Assert.Null(r.Channels.Single().RatedSingleItemRatePerMinute);
        Assert.Contains("native_transport_capacity_unavailable:0", r.Blockers);
    }

    [Fact]
    public void SharedChannelCountsEveryAllocatedRouteAndNeverCreditsCargoStacks()
    {
        var (site, catalog) = Channel(2011, stt: 200000);
        var routes = new[]
        {
            new FoundryRoutedFlow { ItemId = 1, RequiredRatePerMinute = 60, ObjectIndices = new() { 0 } },
            new FoundryRoutedFlow { ItemId = 1, RequiredRatePerMinute = 40, ObjectIndices = new() { 0 } },
        };
        var r = FoundryTransportPlanner.Assess(site, catalog, routes);
        Assert.True(r.AllCapacitiesKnown); Assert.False(r.Satisfied);
        Assert.Equal(100m, r.Channels.Single().AllocatedRatePerMinute);
        Assert.Contains("native_transport_capacity_exceeded:0", r.Blockers);
    }

    [Fact]
    public void BudgetFingerprintBindsNativeSpeedSpanAllocatedFlowAndAvailability()
    {
        var (site, catalog) = Channel(2011, stt: 200000);
        var a = FoundryTransportPlanner.Assess(site, catalog, Array.Empty<FoundryRoutedFlow>());
        var before = FoundryTransportPlanner.Fingerprint(a);
        a.Channels[0].InserterSpan = 2; Assert.NotEqual(before, FoundryTransportPlanner.Fingerprint(a));
        a.Channels[0].InserterSpan = 1; a.Channels[0].AllocatedRatePerMinute = 1;
        Assert.NotEqual(before, FoundryTransportPlanner.Fingerprint(a));
        a.Channels[0].AllocatedRatePerMinute = 0; a.Channels[0].InserterSttRaw = 200001;
        Assert.NotEqual(before, FoundryTransportPlanner.Fingerprint(a));
        a.Channels[0].InserterSttRaw = 200000; a.Satisfied = false;
        Assert.NotEqual(before, FoundryTransportPlanner.Fingerprint(a));
    }

    [Theory]
    [InlineData("routes")] [InlineData("index")] [InlineData("duplicate_index")]
    [InlineData("objects")] [InlineData("catalog")] [InlineData("rate")]
    public void BoundedInputsRejectMalformedData(string change)
    {
        var (site, catalog) = Channel(2011, stt: 200000);
        var routes = new List<FoundryRoutedFlow> { new() { ItemId = 1, RequiredRatePerMinute = 10, ObjectIndices = new() { 0 } } };
        switch (change)
        {
            case "routes": while (routes.Count <= 128) routes.Add(routes[0]); break;
            case "index": routes[0].ObjectIndices[0] = 1; break;
            case "duplicate_index": routes[0].ObjectIndices.Add(0); break;
            case "objects": while (site.Objects.Count <= 32) site.Objects.Add(new() { Index = site.Objects.Count, ItemId = 2001 }); break;
            case "catalog": catalog.Add(catalog[0]); break;
            case "rate": routes[0].RequiredRatePerMinute = 0; break;
        }
        Assert.Throws<InvalidDataException>(() => FoundryTransportPlanner.Assess(site, catalog, routes));
    }

    private static (BlueprintSiteSnapshot Site, List<BuildCatalogItem> Catalog) Channel(int item, int? speed = null, int? stt = null, int span = 1) =>
        (new() { Objects = new() { new() { Index = 0, ItemId = item, Parameters = item >= 2011 ? new[] { span } : Array.Empty<int>() } } },
         new() { new() { ItemId = item, BeltSpeedRaw = speed, InserterSttRaw = stt, InserterGrade = item >= 2011 ? item - 2010 : null } });
}
