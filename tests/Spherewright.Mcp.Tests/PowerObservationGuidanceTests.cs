using System.ComponentModel;
using System.Reflection;
using Spherewright.Mcp.Resources;
using Spherewright.Mcp.Tools;
using Xunit;

namespace Spherewright.Mcp.Tests;

public sealed class PowerObservationGuidanceTests
{
    [Fact]
    public void EmbeddedGuideSeparatesGenerationFuelAndUnpopulatedWorkingFlag()
    {
        var text = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("never fuel inventory", text, StringComparison.Ordinal);
        Assert.Contains("zero does not prove empty fuel", text, StringComparison.Ordinal);
        Assert.Contains("`isWorking=false` is not authoritative evidence of a stopped generator", text, StringComparison.Ordinal);
        Assert.Contains("fuel quantity is unknown", text, StringComparison.Ordinal);
        Assert.Contains("existing full-base load plus all remaining planned demand", text, StringComparison.Ordinal);
        Assert.Contains("do not wait for a favorable capacity spike", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(nameof(SpherewrightTools.ListFactoryEntitiesAsync))]
    [InlineData(nameof(SpherewrightTools.InspectFactoryEntityAsync))]
    public void BothEntityToolsDiscloseGeneratorObservationLimits(string method)
    {
        var text = typeof(SpherewrightTools).GetMethod(method)!
            .GetCustomAttribute<DescriptionAttribute>()!.Description;
        Assert.Contains("power-generation-current-tick", text, StringComparison.Ordinal);
        Assert.Contains("never fuel inventory", text, StringComparison.Ordinal);
        Assert.Contains("isWorking=false is not authoritative evidence of a stopped generator", text, StringComparison.Ordinal);
    }

    [Fact]
    public void PowerSummaryDoesNotPresentInstantaneousServiceAsFullLoadHeadroom()
    {
        var text = typeof(SpherewrightTools).GetMethod(nameof(SpherewrightTools.GetPowerSummaryAsync))!
            .GetCustomAttribute<DescriptionAttribute>()!.Description;
        Assert.Contains("full-base load plus all remaining planned demand", text, StringComparison.Ordinal);
        Assert.Contains("do not wait for a favorable capacity spike", text, StringComparison.Ordinal);
    }
}
