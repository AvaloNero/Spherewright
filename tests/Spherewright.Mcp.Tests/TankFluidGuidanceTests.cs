using System.ComponentModel;
using System.Reflection;
using Spherewright.Mcp.Resources;
using Spherewright.Mcp.Tools;
using Xunit;

namespace Spherewright.Mcp.Tests;

public sealed class TankFluidGuidanceTests
{
    [Fact]
    public void EmbeddedPlaybookDistinguishesVerifiedZeroUnknownAndSustainedSink()
    {
        var text = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("tankFluidCount", text, StringComparison.Ordinal);
        Assert.Contains("null/missing is unknown", text, StringComparison.Ordinal);
        Assert.Contains("Empty `buffers` alone is not zero", text, StringComparison.Ordinal);
        Assert.Contains("not proof of a sustained hydrogen sink", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(nameof(SpherewrightTools.ListFactoryEntitiesAsync))]
    [InlineData(nameof(SpherewrightTools.InspectFactoryEntityAsync))]
    public void ExistingEntityToolsDiscloseOptionalObservation(string method)
    {
        var text = typeof(SpherewrightTools).GetMethod(method)!.GetCustomAttribute<DescriptionAttribute>()!.Description;
        Assert.Contains("tankFluidCount", text, StringComparison.Ordinal);
        Assert.Contains("null/missing is unknown", text, StringComparison.Ordinal);
    }
}
