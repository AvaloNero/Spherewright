using Spherewright.Bridge.Core.Safety;

namespace Spherewright.Bridge.Core.Factory;

// Private server persistence data, never a request DTO or caller-supplied history.
// Contains the locked declaration only: no samples, action tokens or saved-world access.
public sealed class GovernorValidationCheckpoint
{
    public int Version { get; set; } = 1;
    public string OwnedIdentityHash { get; set; } = string.Empty;
    public string GameVersion { get; set; } = string.Empty;
    public string SourceSessionId { get; set; } = string.Empty;
    public string BaselineProposalHash { get; set; } = string.Empty;
    public string SourceStateHash { get; set; } = string.Empty;
    public string ScalePlanHash { get; set; } = string.Empty;
    public int PlanetId { get; set; }
    public int TargetItemId { get; set; }
    public long DeclaredAtGameTick { get; set; }
    public long DeclarationRevision { get; set; }
    public long LockedAtGameTick { get; set; }
    public decimal BaselineRatePerMinute { get; set; }
    public decimal TargetRatePerMinute { get; set; }
    public decimal ToleranceFraction { get; set; }
    public int RequiredGameTicks { get; set; }
    public int MeasurementGameTicks { get; set; } = 600; // Absent in v1: original ten-second semantics.
    public string IntegrityHash { get; set; } = string.Empty;

    public void Validate()
    {
        if ((Version != 1 && Version != 2) || !Bounded(OwnedIdentityHash) || !Bounded(GameVersion)
            || !Diagnostics.NativeProductionRateCalculator.IsSupportedWindow(MeasurementGameTicks)
            || (Version == 1 && MeasurementGameTicks != 600)
            || !Bounded(SourceSessionId) || !Bounded(BaselineProposalHash)
            || !Bounded(SourceStateHash) || !Bounded(ScalePlanHash)
            || PlanetId <= 0 || TargetItemId <= 0 || DeclaredAtGameTick < 0 || DeclarationRevision < 0
            || LockedAtGameTick < DeclaredAtGameTick || LockedAtGameTick - DeclaredAtGameTick > 3600
            || BaselineRatePerMinute <= 0 || TargetRatePerMinute <= BaselineRatePerMinute
            || TargetRatePerMinute > 1000000 || ToleranceFraction <= 0 || ToleranceFraction > .5m
            || RequiredGameTicks < 36000 || RequiredGameTicks > 216000
            || IntegrityHash != CalculateIntegrityHash())
            throw new FoundryPlanningException("governor_validation_checkpoint_invalid",
                "The bounded server declaration is incomplete or its integrity check failed.");
    }

    // Detects corruption; filesystem access control, not this public hash, establishes trust.
    public string CalculateIntegrityHash()
    {
        var legacy = CanonicalStateHash.Combine("governor-locked-declaration-v1",
        Version, OwnedIdentityHash, GameVersion, SourceSessionId, BaselineProposalHash,
        SourceStateHash, ScalePlanHash, PlanetId, TargetItemId, DeclaredAtGameTick, DeclarationRevision, LockedAtGameTick,
        BaselineRatePerMinute, TargetRatePerMinute, ToleranceFraction, RequiredGameTicks);
        return Version == 1 ? legacy : CanonicalStateHash.Combine("governor-locked-declaration-v2", legacy, MeasurementGameTicks);
    }

    private static bool Bounded(string? value) => !string.IsNullOrWhiteSpace(value) && value!.Length <= 256;
}
