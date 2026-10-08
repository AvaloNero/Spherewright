using System.ComponentModel;
using System.Reflection;
using Spherewright.Mcp.Resources;
using Spherewright.Mcp.Tools;
using Xunit;

namespace Spherewright.Mcp.Tests;

public sealed class EmptyBeltDismantleGuidanceTests
{
    [Fact]
    public void PrepareDisclosesCompletePathBoundsAndDoesNotAuthorizeOtherBeltRemoval()
    {
        var description = Describe(nameof(SpherewrightTools.PrepareDismantleAsync));
        Assert.Contains("isolated empty2001 chain head", description);
        Assert.Contains("complete same-tick empty open independent path", description);
        Assert.Contains("at most16 basic2001 belts/512 cells", description);
        Assert.Contains("no external entity/prebuild/cached references", description);
        Assert.Contains("Middle/tail, cargo, joins and higher belt grades reject", description);
        Assert.Contains("Prepare removes nothing", description);
    }

    [Fact]
    public void CommitAndEmbeddedGuideDiscloseNormalRefundPreservationAndUnknownStop()
    {
        var description = Describe(nameof(SpherewrightTools.CommitDismantleAsync));
        Assert.Contains("DoDismantleObject once", description);
        Assert.Contains("returns exactly one2001", description);
        Assert.Contains("complete remaining empty chain", description);
        Assert.Contains("only the removed head edge clears", description);
        Assert.Contains("exact native renderer/collider proof", description);
        Assert.Contains("Missing outcome evidence quarantines writes", description);
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("empty2001 chain head", guide);
        Assert.Contains("at most16 belts/512 cells", guide);
        Assert.Contains("Disconnect external sorters normally first", guide);
        Assert.Contains("never remove a middle or connected tail", guide);
    }

    private static string Describe(string name) => typeof(SpherewrightTools).GetMethod(name)!
        .GetCustomAttribute<DescriptionAttribute>()!.Description;
}
