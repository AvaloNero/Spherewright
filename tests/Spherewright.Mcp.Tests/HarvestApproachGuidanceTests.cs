using Spherewright.Mcp.Resources;
using Xunit;

namespace Spherewright.Mcp.Tests;

public sealed class HarvestApproachGuidanceTests
{
    [Fact]
    public void EmbeddedPlaybookDistinguishesApproachStallsFromStationaryMining()
    {
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;

        Assert.Contains("A harvest approach uses the same bounded **180/600 game-tick**", guide);
        Assert.Contains("Do not call ordinary stationary mining a stall", guide);
        Assert.Contains("reset it after recovery", guide);
        Assert.Contains("rather than replaying, teleporting, or pathfinding", guide);
    }
}
