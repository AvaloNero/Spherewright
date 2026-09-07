using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class FactoryBufferSemanticsTests
{
    [Theory]
    [InlineData(-1L, 0)]
    [InlineData(0L, 0)]
    [InlineData(18686L, 18686)]
    [InlineData(2147483648L, int.MaxValue)]
    [InlineData(long.MaxValue, int.MaxValue)]
    public void GenerationPreservesLegacyBoundedValueButNeverClaimsFuelItems(long energy, int expected)
    {
        var buffer = FactoryBufferSemantics.PowerGeneration(energy, 1120, "Hydrogen");
        Assert.Equal(expected, buffer.Count);
        Assert.Equal("power-generation-current-tick", buffer.Role);
        Assert.Equal("joules_per_tick", buffer.CountUnit);
        Assert.Equal(0, buffer.UnitsPerItem);
        Assert.Equal(1120, buffer.ItemId);
        Assert.Equal("Hydrogen", buffer.Name);
        Assert.False(FactoryBufferSemantics.IsItemCount(buffer));
    }

    [Theory]
    [InlineData("storage", "items", 1, true)]
    [InlineData("inserter-held", "items", 1, true)]
    [InlineData("input", "items", 1, true)]
    [InlineData("power-generation-current-tick", "items", 1, false)]
    [InlineData("power-generation-current-tick", "joules_per_tick", 0, false)]
    [InlineData("research-matrix", "research_matrix_points", 3600, false)]
    [InlineData("storage", "unknown", 1, false)]
    [InlineData("storage", "items", 0, false)]
    public void InventoryRequiresWholeItemUnitsAndExcludesLegacyGeneration(string role, string unit, int scale, bool expected)
    {
        var buffer = new FactoryBufferSnapshot { Role = role, CountUnit = unit, UnitsPerItem = scale, Count = 18686, ItemId = 1120 };
        Assert.Equal(expected, FactoryBufferSemantics.IsItemCount(buffer));
    }
}
