using Spherewright.Contracts.Sessions;

namespace Spherewright.Bridge.Core.Safety;

/// <summary>Expired provenance is not a capability. Only a fresh, explicitly confirmed plan may load it.</summary>
public static class OwnedWorldReauthorizationPolicy
{
    public const int EvidenceVersion = 2;
    public const string ConfirmationPrompt =
        "Confirm after reading this prepared plan: load only the exact previously owned primary save "
        + "at the verified candidate tick, preserving its owned identity and existing gameplay journal. "
        + "No import copy, LastExit or other save will be loaded. The expired credential will be durably "
        + "consumed before loading; a crash or failed load after consumption requires manual investigation, "
        + "not replay. Check the source and target game versions: a supported version change uses native loading "
        + "and preserves journal origin/history with a durable transition before normal saving. "
        + "A new restart credential is issued only after successful adoption and normal saving. Continue?";

    public static bool AllowsExpiredProvenance(bool healthy, bool journalVerified,
        bool consumed, DateTimeOffset issued, DateTimeOffset expires, DateTimeOffset now) =>
        healthy && journalVerified && !consumed && issued < expires && expires <= now;

    public static string? ValidateCandidate(long savedTick, string gameVersion, OwnedSavePrefixEvidence evidence) =>
        savedTick < 0 || evidence.GameTick != savedTick || !evidence.MatchesExpectedIdentity
        || !evidence.Peaceful || !string.Equals(gameVersion, evidence.GameVersion, StringComparison.Ordinal)
            ? "Reauthorization requires the exact saved tick, complete owned identity, recorded source version and peaceful primary."
            : null;

    public static bool AllowsLeaseTick(long candidate, long minimum, bool reauthorizing) =>
        reauthorizing ? candidate == minimum : candidate > minimum;

    public static bool HasMatchingEcho(PreparedOwnedWorldResumePlan plan) =>
        plan.Prepared && plan.RecoveryMode == OwnedWorldResumeModes.ReauthorizeExpiredPrimary
        && plan.RecoveryEvidenceVersion == EvidenceVersion && plan.ExactEmbeddedIdentityVerified
        && plan.CandidateGameTick >= 0 && plan.CandidateGameTick == plan.MinimumGameTick
        && plan.UserConfirmationRequired && !plan.CommitAllowedNow
        && OwnedWorldVersionCompatibilityPolicy.AllowsReauthorization(plan.SourceGameVersion, plan.TargetGameVersion)
        && !string.IsNullOrWhiteSpace(plan.ConfirmationDigest)
        && plan.ConfirmationPrompt == ConfirmationPrompt;

    public static bool MatchesConfirmation(bool confirmed, string expectedDigest, string receivedDigest) =>
        confirmed && !string.IsNullOrWhiteSpace(expectedDigest)
        && string.Equals(expectedDigest, receivedDigest, StringComparison.Ordinal);
}
