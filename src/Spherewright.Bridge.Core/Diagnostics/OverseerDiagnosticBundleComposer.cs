using Spherewright.Contracts.Diagnostics;

namespace Spherewright.Bridge.Core.Diagnostics;

public static class OverseerDiagnosticBundleComposer
{
    public static OverseerDiagnosticBundlePlanetSnapshot ComposePlanet(
        OverseerPlanetProductionSnapshot production,
        OverseerPlanetSummarySnapshot summary)
    {
        if (production is null) throw new ArgumentNullException(nameof(production));
        if (summary is null) throw new ArgumentNullException(nameof(summary));

        if (production.FactoryIndex < 0
            || production.FactoryIndex != summary.FactoryIndex
            || production.PlanetId <= 0
            || production.PlanetId != summary.PlanetId
            || production.IsLocalPlanet != summary.IsLocalPlanet
            || production.FactoryDisplayLoaded != summary.FactoryDisplayLoaded
            || production.CapturedAtGameTick < 0
            || production.CapturedAtGameTick != summary.CapturedAtGameTick
            || !string.Equals(production.PlanetName, summary.PlanetName, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Overseer domains must share one exact owned-factory identity and capture tick.",
                nameof(summary));
        }

        if (production.Production is null
            || production.InfrastructureFindings is null
            || summary.Power is null
            || summary.Logistics is null)
        {
            throw new ArgumentException(
                "Overseer domains must contain complete public diagnostic collections.",
                nameof(summary));
        }

        return new OverseerDiagnosticBundlePlanetSnapshot
        {
            FactoryIndex = production.FactoryIndex,
            PlanetId = production.PlanetId,
            PlanetName = production.PlanetName,
            IsLocalPlanet = production.IsLocalPlanet,
            FactoryDisplayLoaded = production.FactoryDisplayLoaded,
            CapturedAtGameTick = production.CapturedAtGameTick,
            Power = summary.Power,
            Logistics = summary.Logistics,
            Production = production.Production,
            InfrastructureFindingCount = production.InfrastructureFindingCount,
            InfrastructureFindingsTruncated = production.InfrastructureFindingsTruncated,
            InfrastructureFindings = production.InfrastructureFindings,
        };
    }
}
