using System.Text.Json;
using Spherewright.Bridge.Core.Factory;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class FuelPowerStatePolicyTests
{
    private static FuelPowerStateSnapshot Read(int building = 2204, int mask = 1,
        int item = 1109, int count = 3, int inc = 0, long heat = 6750000,
        int loadedItem = 1120, long energy = 1000000, int level = 0, bool productive = false,
        long rated = 36000, long use = 45000, long capacity = 36000, long generation = 1000) =>
        FuelPowerStatePolicy.Capture(building, mask, item, count, inc, heat,
            loadedItem, energy, level, productive, rated, use, capacity, generation);

    [Fact]
    public void FuelSwitchKeepsBufferedItemsSeparateFromLoadedHeat()
    {
        var state = Read(inc: 9, level: 2);
        Assert.Equal("observed", state.State);
        Assert.Null(state.ReasonCode);
        Assert.Equal(1109, state.BufferedFuelItemId);
        Assert.Equal(3, state.BufferedFuelCount);
        Assert.Equal(9, state.BufferedFuelInc);
        Assert.Equal(6750000L, state.BufferedFuelHeatPerItemJoules);
        Assert.Equal(1120, state.LoadedFuelItemId);
        Assert.Equal(1000000L, state.LoadedFuelEnergyJoules);
        Assert.Equal(2, state.LoadedFuelIncLevel);
        Assert.False(state.LoadedFuelProductive);
    }

    [Fact]
    public void EmptyQueueCanStillHaveLoadedFuelEnergyAndZeroGeneration()
    {
        var state = Read(item: 0, count: 0, heat: 0, generation: 0);
        Assert.Equal("observed", state.State);
        Assert.Equal(0, state.BufferedFuelCount);
        Assert.Equal(1000000L, state.LoadedFuelEnergyJoules);
        Assert.Equal(0L, state.GeneratedEnergyPerTick);
    }

    [Fact]
    public void VerifiedEmptyIsDistinctFromMissingOrUnavailable()
    {
        var empty = Read(item: 0, count: 0, heat: 0, loadedItem: 0, energy: 0, capacity: 0, generation: 0);
        Assert.Equal("observed", empty.State);
        Assert.Equal(0L, empty.LoadedFuelEnergyJoules);
        var unknown = FuelPowerStatePolicy.Unavailable("generator_component_unavailable");
        Assert.Equal("unavailable", unknown.State);
        Assert.Null(unknown.BufferedFuelCount);
        Assert.Null(unknown.LoadedFuelEnergyJoules);
        Assert.Null(new FactoryEntitySnapshot().FuelPowerState);
    }

    [Fact]
    public void FusionProliferationAndLongEnergyAreCopiedWithoutItemConversion()
    {
        var state = Read(building: 2211, mask: 2, item: 1802, loadedItem: 1802,
            heat: 600000000, energy: 500000000, level: 10, productive: true,
            rated: 250000, use: 250000, capacity: 500000, generation: (long)int.MaxValue + 1);
        Assert.Equal("observed", state.State);
        Assert.True(state.LoadedFuelProductive);
        Assert.Equal(500000L, state.CapacityEnergyPerTick);
        Assert.Equal((long)int.MaxValue + 1, state.GeneratedEnergyPerTick);
        Assert.Equal(3, state.BufferedFuelCount); // Energy has not been counted again as fuel items.
    }

    [Theory]
    [InlineData(2203, 0)]
    [InlineData(2204, 2)]
    [InlineData(2211, 1)]
    [InlineData(2212, 4)]
    public void UnsupportedKindOrMismatchedNativeMaskIsUnknown(int item, int mask)
    {
        var state = Read(building: item, mask: mask);
        Assert.Equal("unavailable", state.State);
        Assert.Equal("unsupported_fuel_generator", state.ReasonCode);
        Assert.Null(state.BufferedFuelCount);
    }

    [Fact]
    public void InconsistentOrOutOfRangeRawScalarsFailClosed()
    {
        var invalid = new[]
        {
            Read(item: -1), Read(count: -1), Read(count: short.MaxValue + 1),
            Read(inc: -1), Read(inc: short.MaxValue + 1), Read(heat: -1),
            Read(loadedItem: -1), Read(energy: -1), Read(level: -1), Read(level: 11),
            Read(rated: 0), Read(use: 0), Read(capacity: -1), Read(generation: -1),
            Read(item: 0), Read(heat: 0), Read(loadedItem: 0),
        };
        foreach (var state in invalid)
        {
            Assert.Equal("unavailable", state.State);
            Assert.Equal("invalid_native_fuel_state", state.ReasonCode);
            Assert.Null(state.LoadedFuelEnergyJoules);
            Assert.Null(state.BufferedFuelItemId);
        }
    }

    [Fact]
    public void SerializationPreservesUnknownAndDistinctStockEnergyUnits()
    {
        var source = new FactoryEntitySnapshot { ObjectId = 183, FuelPowerState = Read() };
        var result = JsonSerializer.Deserialize<FactoryEntitySnapshot>(JsonSerializer.Serialize(source))!;
        Assert.Equal(3, result.FuelPowerState!.BufferedFuelCount);
        Assert.Equal(1000000L, result.FuelPowerState.LoadedFuelEnergyJoules);
        source.FuelPowerState = FuelPowerStatePolicy.Unavailable("unsupported_fuel_generator");
        result = JsonSerializer.Deserialize<FactoryEntitySnapshot>(JsonSerializer.Serialize(source))!;
        Assert.Null(result.FuelPowerState!.BufferedFuelCount);
        Assert.Equal("unavailable", result.FuelPowerState.State);
    }

    [Fact]
    public void DetailFuelMetadataDoesNotChangeAnyExistingActionHashOrBuffers()
    {
        var entity = new FactoryEntitySnapshot { ObjectId = 183, ItemId = 2204, ComponentKind = "power-generator" };
        entity.Buffers.Add(FactoryBufferSemantics.PowerGeneration(1000, 1120, "hydrogen"));
        var full = CanonicalStateHash.Factory(entity);
        var config = CanonicalStateHash.FactoryConfiguration(entity);
        var endpoint = CanonicalStateHash.FactoryEndpoint(entity);
        entity.FuelPowerState = Read();
        Assert.Equal(full, CanonicalStateHash.Factory(entity));
        Assert.Equal(config, CanonicalStateHash.FactoryConfiguration(entity));
        Assert.Equal(endpoint, CanonicalStateHash.FactoryEndpoint(entity));
        Assert.Single(entity.Buffers);
        Assert.False(FactoryBufferSemantics.IsItemCount(entity.Buffers[0]));
        entity.FuelPowerState = Read(count: 1, energy: 1, capacity: 0);
        Assert.Equal(full, CanonicalStateHash.Factory(entity));
        Assert.Equal(config, CanonicalStateHash.FactoryConfiguration(entity));
        Assert.Equal(endpoint, CanonicalStateHash.FactoryEndpoint(entity));
        entity.FuelPowerState = FuelPowerStatePolicy.Unavailable("invalid_native_fuel_state");
        Assert.Equal(full, CanonicalStateHash.Factory(entity));
    }
}
