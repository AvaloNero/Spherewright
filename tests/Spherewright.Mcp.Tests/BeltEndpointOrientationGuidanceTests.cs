using System.ComponentModel;
using System.Reflection;
using Spherewright.Mcp.Resources;
using Spherewright.Mcp.Tools;
using Xunit;

namespace Spherewright.Mcp.Tests;

public sealed class BeltEndpointOrientationGuidanceTests
{
    [Fact]
    public void EmbeddedPlaybookExplainsOutwardPoseWithoutWeakeningNativeQualification()
    {
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("outward slot pose", guide, StringComparison.Ordinal);
        Assert.Contains("faces back toward the existing source", guide, StringComparison.Ordinal);
        Assert.Contains("faces toward the existing destination", guide, StringComparison.Ordinal);
        Assert.Contains("do not apply that transform twice", guide, StringComparison.Ordinal);
        Assert.Contains("do not copy its quarter-turn number", guide, StringComparison.Ordinal);
        Assert.Contains("do not bypass the fresh native angle, span, occupancy or material checks", guide, StringComparison.Ordinal);
    }

    [Fact]
    public void PublicToolParameterDescriptionIsDiscoverableAndStillReadOnly()
    {
        var parameter = typeof(SpherewrightTools).GetMethods()
            .SelectMany(method => method.GetParameters()).Single(p => p.Name == "beltEndpointPreview");
        var description = parameter.GetCustomAttribute<DescriptionAttribute>()!.Description;
        Assert.Contains("outward slot pose", description, StringComparison.Ordinal);
        Assert.Contains("Source role faces back toward the existing source", description, StringComparison.Ordinal);
        Assert.Contains("Destination role faces toward the existing destination", description, StringComparison.Ordinal);
        Assert.Contains("already applies the destination-end180-degree transform", description, StringComparison.Ordinal);
        Assert.Contains("no token/action/prebuild", description, StringComparison.Ordinal);
        Assert.Contains("does NOT extend blueprint external matching or authorize construction", description, StringComparison.Ordinal);
    }
}
