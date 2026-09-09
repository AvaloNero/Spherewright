using System.ComponentModel;
using System.Reflection;
using Spherewright.Mcp.Resources;
using Spherewright.Mcp.Tools;
using Xunit;

namespace Spherewright.Mcp.Tests;

public sealed class ProductionWindowGuidanceTests
{
    [Fact]
    public void EmbeddedPlaybookExplainsSamplingLimitsWithoutWaivingAcceptance()
    {
        var text = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("Do not sum overlapping windows", text, StringComparison.Ordinal);
        Assert.Contains("to gapped windows", text, StringComparison.Ordinal);
        Assert.Contains("inconclusive, not an upper-bound failure", text, StringComparison.Ordinal);
        Assert.Contains("zero `P=C` or full stock alone never proves sustainable supply", text, StringComparison.Ordinal);
        Assert.Contains("without retroactively passing old windows", text, StringComparison.Ordinal);
        Assert.Contains("fixed target/tolerance and gap-reset rules remain unchanged", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ExistingProductionToolDisclosesNonAdditiveWindowSemantics()
    {
        var text = typeof(SpherewrightTools).GetMethod(nameof(SpherewrightTools.GetOverseerProductionAsync))!
            .GetCustomAttribute<DescriptionAttribute>()!.Description;
        Assert.Contains("not an interval ledger", text, StringComparison.Ordinal);
        Assert.Contains("overlapping or gapped windows", text, StringComparison.Ordinal);
        Assert.Contains("inventory intervals", text, StringComparison.Ordinal);
    }
}
