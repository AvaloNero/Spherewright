using Spherewright.Bridge.Core.Diagnostics;
using Spherewright.Contracts.Diagnostics;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class OverseerDiagnosticBundleComposerTests
{
    [Fact]
    public void ComposePlanet_JoinsMatchingSameTickDomains()
    {
        var production = Production();
        production.Production.Add(new ProductionRateSnapshot { PlanetId = 104, ItemId = 6003 });
        var summary = Summary();
        summary.Power.MinimumConsumerRatio = 0.75;
        summary.Logistics.InterstellarStationCount = 2;

        var result = OverseerDiagnosticBundleComposer.ComposePlanet(production, summary);

        Assert.Equal(104, result.PlanetId);
        Assert.Equal(42, result.CapturedAtGameTick);
        Assert.Equal(0.75, result.Power.MinimumConsumerRatio);
        Assert.Equal(2, result.Logistics.InterstellarStationCount);
        Assert.Equal(6003, Assert.Single(result.Production).ItemId);
    }

    [Theory]
    [InlineData(1, 104, 42, "Planet")]
    [InlineData(0, 102, 42, "Planet")]
    [InlineData(0, 104, 43, "Planet")]
    [InlineData(0, 104, 42, "Renamed")]
    public void ComposePlanet_RejectsCrossFactoryOrCrossTickJoins(
        int factoryIndex,
        int planetId,
        long capturedAtGameTick,
        string planetName)
    {
        var summary = Summary();
        summary.FactoryIndex = factoryIndex;
        summary.PlanetId = planetId;
        summary.CapturedAtGameTick = capturedAtGameTick;
        summary.PlanetName = planetName;

        Assert.Throws<ArgumentException>(() =>
            OverseerDiagnosticBundleComposer.ComposePlanet(Production(), summary));
    }

    [Fact]
    public void ComposePlanet_RejectsMismatchedRuntimeFlags()
    {
        var summary = Summary();
        summary.FactoryDisplayLoaded = false;

        Assert.Throws<ArgumentException>(() =>
            OverseerDiagnosticBundleComposer.ComposePlanet(Production(), summary));
    }

    [Fact]
    public void ComposePlanet_RejectsMissingPublicDomainCollections()
    {
        var summary = Summary();
        summary.Power = null!;

        Assert.Throws<ArgumentException>(() =>
            OverseerDiagnosticBundleComposer.ComposePlanet(Production(), summary));
    }

    private static OverseerPlanetProductionSnapshot Production() => new OverseerPlanetProductionSnapshot
    {
        FactoryIndex = 0,
        PlanetId = 104,
        PlanetName = "Planet",
        IsLocalPlanet = true,
        FactoryDisplayLoaded = true,
        CapturedAtGameTick = 42,
    };

    private static OverseerPlanetSummarySnapshot Summary() => new OverseerPlanetSummarySnapshot
    {
        FactoryIndex = 0,
        PlanetId = 104,
        PlanetName = "Planet",
        IsLocalPlanet = true,
        FactoryDisplayLoaded = true,
        CapturedAtGameTick = 42,
    };
}
