using System.ComponentModel;
using System.Reflection;
using Spherewright.Mcp.Resources;
using Spherewright.Mcp.Tools;
using Xunit;

namespace Spherewright.Mcp.Tests;

public sealed class FuelPowerStateGuidanceTests
{
    [Fact]
    public void ExistingReadDisclosesStockEnergyAndUnknownWithoutNewAction()
    {
        var method = typeof(SpherewrightTools).GetMethod(nameof(SpherewrightTools.InspectFactoryEntityAsync))!;
        var text = method.GetCustomAttribute<DescriptionAttribute>()!.Description;
        Assert.Contains("fuelPowerState", text);
        Assert.Contains("bufferedFuelCount is items", text);
        Assert.Contains("loadedFuelEnergyJoules is energy", text);
        Assert.Contains("Buffered and loaded fuel IDs may differ", text);
        Assert.Contains("Null/unavailable is unknown", text);
        Assert.Contains("does not change action hashes or material-cut support", text);
        Assert.DoesNotContain("prepare_fuel_power", text);
    }

    [Fact]
    public void EmbeddedGuideSeparatesNativeFuelStockFromLoadedHeatAndFlow()
    {
        var text = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("inspect_factory_entity.fuelPowerState", text);
        Assert.Contains("not extra fuel items", text);
        Assert.Contains("Zero buffered items do not mean zero residual energy", text);
        Assert.Contains("not actual burn or sustainable allocation", text);
        Assert.Contains("never invoke native mutating energy/generation methods", text);
        Assert.Contains("Research labs are also unsupported", text);
    }
}
