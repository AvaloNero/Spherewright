using Spherewright.Bridge.Core.Factory;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Diagnostics;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class GovernorSourceBindingTests
{
    [Fact]
    public void MeasurementBindingCanonicalizesItemOrderButKeepsScopeAndActualSource()
    {
        var hash = GovernorSourceBinding.CreateMeasurementBinding("series", "source", new[] { 1, 2 });
        Assert.Equal(hash, GovernorSourceBinding.CreateMeasurementBinding("series", "source", new[] { 2, 1 }));
        Assert.NotEqual(hash, GovernorSourceBinding.CreateMeasurementBinding("new-series", "source", new[] { 1, 2 }));
        Assert.NotEqual(hash, GovernorSourceBinding.CreateMeasurementBinding("series", "changed-source", new[] { 1, 2 }));
        Assert.NotEqual(hash, GovernorSourceBinding.CreateMeasurementBinding("series", "source", new[] { 1, 3 }));
    }

    [Theory]
    [InlineData("key")] [InlineData("source")] [InlineData("null")]
    [InlineData("empty")] [InlineData("limit")] [InlineData("negative")] [InlineData("duplicate")]
    public void MeasurementBindingRejectsMissingOrUnboundedEvidence(string invalid)
    {
        var items = invalid switch
        {
            "null" => null,
            "empty" => Array.Empty<int>(),
            "limit" => Enumerable.Range(1, 65).ToArray(),
            "negative" => new[] { -1 },
            "duplicate" => new[] { 1, 1 },
            _ => new[] { 1, 2 },
        };
        var error = Assert.Throws<FoundryPlanningException>(() => GovernorSourceBinding.CreateMeasurementBinding(
            invalid == "key" ? "" : "series", invalid == "source" ? "" : "source", items!));
        Assert.Equal("governor_measurement_scope_invalid", error.Reason);
    }

    [Theory]
    [InlineData("key")] [InlineData("source")] [InlineData("items")]
    public void RealMeasurementScopeChangeStillDiscardsTheBaseline(string change)
    {
        var series = new GovernorMeasurementSeries();
        var binding = GovernorSourceBinding.CreateMeasurementBinding("series", "source", new[] { 1, 2 });
        for (var tick = 600; tick <= 2400; tick += 600)
            ObserveMeasurement(series, binding, tick, 30);
        Assert.Equal("ready", series.Baseline(.1m).State);
        var changed = GovernorSourceBinding.CreateMeasurementBinding(change == "key" ? "new-series" : "series",
            change == "source" ? "changed-source" : "source", change == "items" ? new[] { 1, 2, 3 } : new[] { 1, 2 });
        ObserveMeasurement(series, changed, 2410, 30);
        Assert.Equal(0, series.Baseline(.1m).IndependentWindowCount);
        Assert.Equal("warming_up", series.Baseline(.1m).State);
        Assert.Null(series.PreviousStockTick);
    }

    [Fact]
    public void RetainingMeasurementScopeDoesNotMakeTheLiveUnstablePatternReady()
    {
        var series = new GovernorMeasurementSeries();
        var binding = GovernorSourceBinding.CreateMeasurementBinding("series", "source", new[] { 1006, 1109 });
        ObserveMeasurement(series, binding, 600, 42);
        ObserveMeasurement(series, binding, 1200, 42);
        ObserveMeasurement(series, binding, 1800, 36);
        ObserveMeasurement(series, binding, 2400, 36);
        ObserveMeasurement(series, binding, 2410, 36); // Overlap is still not a new independent sample.
        var result = series.Baseline(.1m);
        Assert.Equal("unstable", result.State);
        Assert.Equal(38, result.ProductionPerMinute);
        Assert.Equal(3, result.IndependentWindowCount);
    }

    private static void ObserveMeasurement(GovernorMeasurementSeries series, string binding, long tick, decimal rate) =>
        series.Observe(binding, new OverseerWindowSnapshot { State = "ready", StartGameTick = tick - 599,
            EndGameTick = tick, ElapsedGameTicks = 600 }, rate, new Dictionary<int, long> { [1] = 5 });

    [Fact]
    public void NormalProductionAndStorageGrowthCanEstablishThreeIndependentWindows()
    {
        var source = Source(); var series = new GovernorMeasurementSeries();
        var binding = GovernorSourceBinding.Create(source);
        for (var i = 1; i <= 4; i++)
        {
            source[0].Progress = i * 1000; source[0].IsWorking = i % 2 == 0;
            source[0].Buffers[0].Count = i % 3; source[0].Buffers[0].Inc = i;
            source[1].Buffers[0].Count += 5; source[2].InserterStackCount = i % 2;
            source[2].InserterStage = i % 2 == 0 ? "Picking" : "Sending";
            foreach (var e in source)
            {
                e.CapturedAtGameTick = i * 600; e.PowerServeRatio = i / 4d;
                e.StateHash = CanonicalStateHash.Factory(e); e.ConfigurationStateHash = CanonicalStateHash.FactoryConfiguration(e);
            }
            Assert.Equal(binding, GovernorSourceBinding.Create(source));
            series.Observe(GovernorSourceBinding.Create(source), new OverseerWindowSnapshot
                { State = "ready", StartGameTick = i * 600 - 599, EndGameTick = i * 600, ElapsedGameTicks = 600 },
                30, new Dictionary<int, long> { [1112] = source[1].Buffers[0].Count });
        }
        Assert.Equal("ready", series.Baseline(.1m).State);
        Assert.Equal(3, series.Baseline(.1m).IndependentWindowCount);
        Assert.Equal(30, series.Baseline(.1m).ProductionPerMinute);
        Assert.Equal(15, series.PreviousStocks![1112]); // Inventory is still independently observed.
    }

    [Theory]
    [InlineData("recipe")] [InlineData("filter")] [InlineData("acceleration")] [InlineData("power-network")]
    [InlineData("pose")] [InlineData("connection")] [InlineData("storage-bans")]
    [InlineData("storage-filter")] [InlineData("prototype")] [InlineData("session")]
    public void RealConfigurationChangesInvalidateTheSource(string field)
    {
        var source = Source(); var hash = GovernorSourceBinding.Create(source);
        switch (field)
        {
            case "recipe": source[0].RecipeId++; break;
            case "filter": source[2].FilterItemId = 1112; break;
            case "acceleration": source[0].ForceAccelerationMode = true; break;
            case "power-network": source[0].PowerNetworkId++; break;
            case "pose": source[0].Position.X++; break;
            case "connection": source[0].Connections.Add(new FactoryConnectionSnapshot { Slot = 0, OtherObjectId = 3 }); break;
            case "storage-bans": source[1].StorageConfiguration!.BannedGridCount++; break;
            case "storage-filter": source[1].StorageConfiguration!.GridFilterItemIds[0] = 1109; break;
            case "prototype": source[0].ItemId++; break;
            case "session": foreach (var e in source) e.SessionId = "new-session"; break;
        }
        Assert.NotEqual(hash, GovernorSourceBinding.Create(source));
    }

    [Fact]
    public void SelectionOrderDoesNotChangeBindingButMissingOrDuplicateSourcesAreRejected()
    {
        var source = Source(); var hash = GovernorSourceBinding.Create(source);
        source.Reverse(); Assert.Equal(hash, GovernorSourceBinding.Create(source));
        source.Add(source[0]); Assert.Throws<FoundryPlanningException>(() => GovernorSourceBinding.Create(source));
        Assert.Throws<FoundryPlanningException>(() => GovernorSourceBinding.Create(Array.Empty<FactoryEntitySnapshot>()));
    }

    [Fact]
    public void BlueprintSelectionEndpointAlsoBindsManualFilterAndAccelerationSettings()
    {
        var source = Source();
        var machine = CanonicalStateHash.FactoryEndpoint(source[0]);
        source[0].ForceAccelerationMode = true;
        Assert.NotEqual(machine, CanonicalStateHash.FactoryEndpoint(source[0]));
        var sorter = CanonicalStateHash.FactoryEndpoint(source[2]);
        source[2].FilterItemId = 1112;
        Assert.NotEqual(sorter, CanonicalStateHash.FactoryEndpoint(source[2]));
    }

    private static List<FactoryEntitySnapshot> Source() => new()
    {
        new() { SessionId = "owned", PlanetId = 104, ObjectId = 1, ItemId = 2302, ComponentKind = "assembler",
            RecipeId = 60, ForceAccelerationMode = false, PowerNetworkId = 2,
            Buffers = new List<FactoryBufferSnapshot> { new() { ItemId = 1109, Count = 1 } } },
        new() { SessionId = "owned", PlanetId = 104, ObjectId = 2, ItemId = 2101, ComponentKind = "storage",
            StorageConfiguration = new StorageConfigurationSnapshot { GridCount = 1, Mode = "default", GridFilterItemIds = new List<int> { 0 } },
            Buffers = new List<FactoryBufferSnapshot> { new() { ItemId = 1112 } } },
        new() { SessionId = "owned", PlanetId = 104, ObjectId = 3, ItemId = 2011, ComponentKind = "inserter",
            PickTargetObjectId = 1, InsertTargetObjectId = 2, PowerNetworkId = 2 },
    };
}
