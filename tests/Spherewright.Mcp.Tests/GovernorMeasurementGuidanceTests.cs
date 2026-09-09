using System.ComponentModel;
using System.Reflection;
using Spherewright.Mcp.Resources;
using Spherewright.Mcp.Tools;
using Xunit;

namespace Spherewright.Mcp.Tests;

public sealed class GovernorMeasurementGuidanceTests
{
    [Fact]
    public void ToolExplainsRepricingBeforeLockWithoutRetargetingAnExperiment()
    {
        var text = typeof(SpherewrightTools).GetMethod(nameof(SpherewrightTools.GetGovernorPlanAsync))!
            .GetCustomAttribute<DescriptionAttribute>()!.Description;
        Assert.Contains("repricing only the proposed target rate preserves unchanged-source measurement history", text);
        Assert.Contains("NEW proposalHash", text);
        Assert.Contains("BEFORE any expansion/write", text);
        Assert.Contains("An unstable baseline stays unstable", text);
        Assert.Contains("already locked target/tolerance/scale cannot be changed", text);
        Assert.Contains("observed item scope", text);
    }

    [Fact]
    public void EmbeddedPlaybookKeepsFixedAcceptanceAndNoFavorableWindowRetries()
    {
        var text = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("planning-only target-rate change", text);
        Assert.Contains("request twice its measured rate", text);
        Assert.Contains("NEW proposalHash", text);
        Assert.Contains("before any expansion/write", text);
        Assert.Contains("do not widen tolerance or retry until favorable samples appear", text);
        Assert.Contains("original target/tolerance/scale remain immutable", text);
    }
}
