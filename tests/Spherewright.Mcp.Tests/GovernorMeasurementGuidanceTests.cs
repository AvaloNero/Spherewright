using System.ComponentModel;
using System.Reflection;
using Spherewright.Mcp.Resources;
using Spherewright.Mcp.Tools;
using Xunit;

namespace Spherewright.Mcp.Tests;

public sealed class GovernorMeasurementGuidanceTests
{
    [Fact]
    public void ToolAndEmbeddedPlaybookDistinguishLastResetFromCurrentWindowFailure()
    {
        var tool = typeof(SpherewrightTools).GetMethod(nameof(SpherewrightTools.GetGovernorPlanAsync))!
            .GetCustomAttribute<DescriptionAttribute>()!.Description;
        Assert.Contains("resetReason is historical", tool);
        Assert.Contains("not a current-failure flag", tool);
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("resetReason is historical", guide);
        Assert.Contains("not a current-failure flag", guide);
        Assert.Contains("distinct independent windows", guide);
        Assert.Contains("current measured rate", guide);
        Assert.Contains("never restore credit across a sampling gap", guide);
    }

    [Fact]
    public void PlaybookAndToolExposePeriodChoiceBeforeBaselineAndNeverAfterLock()
    {
        var method = typeof(SpherewrightTools).GetMethod(nameof(SpherewrightTools.GetGovernorPlanAsync))!;
        var parameter = method.GetParameters().Single(p => p.Name == "measurementGameTicks");
        Assert.Equal(600, parameter.DefaultValue);
        Assert.Contains("BEFORE", parameter.GetCustomAttribute<DescriptionAttribute>()!.Description);
        Assert.Contains("3600", parameter.GetCustomAttribute<DescriptionAttribute>()!.Description);
        Assert.Contains("locked measurement period cannot change", method.GetCustomAttribute<DescriptionAttribute>()!.Description);
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("matching echo", guide); Assert.Contains("measurement-period", guide);
        Assert.Contains("not permission to loosen10%", guide); Assert.Contains("native six-tick-aligned", guide);
    }

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
