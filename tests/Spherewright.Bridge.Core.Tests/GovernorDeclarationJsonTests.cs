using System.Globalization;
using Spherewright.Bridge.Core.Factory;
using Spherewright.Plugin.Transport;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class GovernorDeclarationJsonTests
{
    private static GovernorValidationCheckpoint Saved(int version, decimal baseline, decimal target) => Seal(new()
    {
        Version = version, OwnedIdentityHash = "test-owned-identity", GameVersion = "test-game-version",
        SourceSessionId = "original-session", BaselineProposalHash = "original-pre-expansion-proposal",
        SourceStateHash = "original-source", ScalePlanHash = "original-scale", PlanetId = 104,
        TargetItemId = 1109, DeclaredAtGameTick = 2400, DeclarationRevision = 5, LockedAtGameTick = 2410,
        BaselineRatePerMinute = baseline, TargetRatePerMinute = target, ToleranceFraction = .1m,
        RequiredGameTicks = 36000, MeasurementGameTicks = version == 1 ? 600 : 3600,
    });

    private static GovernorValidationCheckpoint Seal(GovernorValidationCheckpoint saved)
    {
        saved.IntegrityHash = saved.CalculateIntegrityHash(); saved.Validate(); return saved;
    }

    private static GovernorDeclarationArchive Archive(GovernorValidationCheckpoint saved)
    {
        var archive = new GovernorDeclarationArchive { IdentityHash = saved.OwnedIdentityHash, GameVersion = saved.GameVersion };
        archive.AddLockedDeclaration(saved); return archive;
    }

    [Theory]
    [InlineData(1, "31", "62")] [InlineData(1, "31", "62.0")]
    [InlineData(1, "31.0", "62")] [InlineData(1, "31.0", "62.0")]
    [InlineData(2, "31", "62")] [InlineData(2, "31", "62.0")]
    [InlineData(2, "31.0", "62")] [InlineData(2, "31.0", "62.0")]
    public void LegacyIntegerDecimalsSurviveTheExactPluginSerializerWithoutReplacingTheDeclaration(int version, string baseline, string target)
    {
        var saved = Saved(version, decimal.Parse(baseline, CultureInfo.InvariantCulture), decimal.Parse(target, CultureInfo.InvariantCulture));
        var json = PluginJson.Serialize(Archive(saved));
        var restored = PluginJson.Deserialize<GovernorDeclarationArchive>(json)!;
        var entry = Assert.Single(restored.Declarations);
        if (baseline == "31" || target == "62") Assert.NotEqual(saved.IntegrityHash, entry.CalculateIntegrityHash());
        restored.Validate(saved.OwnedIdentityHash, saved.GameVersion);
        Assert.Equal(saved.IntegrityHash, entry.IntegrityHash); // No in-memory rewrite or migration.
        Assert.Equal(json, PluginJson.Serialize(restored));
        var run = GovernorThroughputValidation.Restore(entry, saved.OwnedIdentityHash, saved.GameVersion, "resumed-session", 2500, 2600);
        Assert.Equal(31, run.Snapshot().BaselineProductionPerMinute);
        Assert.Equal(62, run.Snapshot().TargetRatePerMinute);
        Assert.Equal(saved.BaselineProposalHash, run.Snapshot().BaselineProposalHash);
        Assert.Equal(saved.LockedAtGameTick, run.Snapshot().LockedAtGameTick);
        Assert.Equal(0, run.Snapshot().ObservedContiguousGameTicks);
        Assert.False(run.Snapshot().Durable); // The Plugin store, not this DTO round-trip, proves disk durability.
        Assert.False(run.Snapshot().DoubleThroughputTargetObserved);
    }

    [Theory]
    [InlineData("31", "62", "0.1")] [InlineData("31.0", "62.00", "0.10")]
    [InlineData("31.000000000000000000000000000", "62", "0.1000")]
    public void V3HashesNumericDecimalValuesNotScale(string baseline, string target, string tolerance)
    {
        var saved = Saved(3, decimal.Parse(baseline, CultureInfo.InvariantCulture), decimal.Parse(target, CultureInfo.InvariantCulture));
        saved.ToleranceFraction = decimal.Parse(tolerance, CultureInfo.InvariantCulture); Seal(saved);
        Assert.Equal(Saved(3, 31, 62).IntegrityHash, saved.IntegrityHash);
        var restored = PluginJson.Deserialize<GovernorDeclarationArchive>(PluginJson.Serialize(Archive(saved)))!;
        restored.Validate(saved.OwnedIdentityHash, saved.GameVersion);
        Assert.Equal(saved.IntegrityHash, Assert.Single(restored.Declarations).CalculateIntegrityHash());
    }

    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void FractionalRatesAndCultureSurvivePluginJson(int version)
    {
        var saved = Saved(version, 100m / 3, (100m / 3) * 2);
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var after = PluginJson.Deserialize<GovernorDeclarationArchive>(PluginJson.Serialize(Archive(saved)))!;
            after.Validate(saved.OwnedIdentityHash, saved.GameVersion);
            Assert.Equal(saved.IntegrityHash, after.Declarations[0].IntegrityHash);
            Assert.Equal(saved.BaselineRatePerMinute, after.Declarations[0].BaselineRatePerMinute);
        }
        finally { CultureInfo.CurrentCulture = culture; }
    }

    [Theory]
    [InlineData("baseline")] [InlineData("target")] [InlineData("tolerance")]
    [InlineData("owner")] [InlineData("game")] [InlineData("source")] [InlineData("scale")]
    [InlineData("lock")] [InlineData("hash")] [InlineData("period")]
    public void LegacyFormattingCompatibilityStillRejectsChangedValuesAndMetadata(string change)
    {
        var saved = PluginJson.Deserialize<GovernorValidationCheckpoint>(PluginJson.Serialize(Saved(2, 31, 62.0m)))!;
        switch (change)
        {
            case "baseline": saved.BaselineRatePerMinute += 1; break;
            case "target": saved.TargetRatePerMinute += 1; break;
            case "tolerance": saved.ToleranceFraction += .01m; break;
            case "owner": saved.OwnedIdentityHash += "other"; break;
            case "game": saved.GameVersion += "other"; break;
            case "source": saved.SourceStateHash += "other"; break;
            case "scale": saved.ScalePlanHash += "other"; break;
            case "lock": saved.LockedAtGameTick++; break;
            case "hash": saved.IntegrityHash += "other"; break;
            case "period": saved.MeasurementGameTicks = 600; break;
        }
        Assert.Throws<FoundryPlanningException>(saved.Validate);
    }

    [Fact]
    public void CompatibilityDoesNotSearchArbitraryLegacyScaleChanges()
    {
        var saved = Saved(2, 31.00m, 62);
        saved.BaselineRatePerMinute = 31.0m;
        Assert.Throws<FoundryPlanningException>(saved.Validate);
        saved = Saved(2, 31, 62); saved.BaselineRatePerMinute = 31.00m;
        Assert.Throws<FoundryPlanningException>(saved.Validate);
    }
}
