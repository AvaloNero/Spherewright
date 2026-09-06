using Spherewright.Bridge.Core.Factory;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Diagnostics;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class GovernorSourceBindingTests
{
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
