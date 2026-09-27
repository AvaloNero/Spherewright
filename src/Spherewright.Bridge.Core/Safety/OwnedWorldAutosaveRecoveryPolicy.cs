using Spherewright.Contracts.Sessions;

namespace Spherewright.Bridge.Core.Safety;

/// <summary>Explicit expired-ticket recovery from DSP's fixed AutoSave0 slot only.</summary>
public static class OwnedWorldAutosaveRecoveryPolicy
{
    public const int EvidenceVersion = 3;
    public const string ConfirmationPrompt =
        "Confirm after reading this prepared plan: load only the exact protected AutoSave0 candidate "
        + "at the approved newer tick, preserving its owned identity and existing gameplay journal. "
        + "No primary, LastExit, other autosave or imported copy will be loaded. The expired credential "
        + "will be durably consumed before native loading; a crash or failed load after consumption "
        + "requires manual investigation, not replay. The source, current runtime and candidate must be "
        + "the same supported native version; migration is unavailable for this mode. A new restart "
        + "credential is issued only after successful adoption and normal saving. Continue?";

    public static string? ValidateRequest(PrepareOwnedWorldResumeRequest request)
    {
        if (request.UserConfirmedInConversation)
            return "Expired-AutoSave0 prepare is read-only disclosure; confirmation belongs to a subsequent commit, not prepare.";
        if (request.MinimumRecoveryGameTick is not long known || known < 0
            || request.ExpectedRecoveryGameTick is not long expected || expected < known)
            return "Expired-AutoSave0 recovery requires the known progress floor and exact approved AutoSave0 candidate tick.";
        return null;
    }

    public static string? ValidateCandidate(PrepareOwnedWorldResumeRequest request,
        long ticketMinimumGameTick, DateTimeOffset ticketIssuedAtUtc,
        string sourceGameVersion, string currentGameVersion,
        OwnedSavePrefixEvidence evidence, DateTimeOffset writtenAtUtc, long? primaryGameTick)
    {
        if (request.RecoveryMode != OwnedWorldResumeModes.ReauthorizeExpiredAutosave0)
            return "Expired-AutoSave0 validation requires its exact recovery mode.";
        var invalid = ValidateRequest(request);
        if (invalid is not null) return invalid;

        if (!IsSupportedSameVersion(sourceGameVersion, currentGameVersion)
            || !string.Equals(evidence.GameVersion, currentGameVersion, StringComparison.Ordinal))
            return "Expired-AutoSave0 recovery requires matching supported source, runtime and candidate game versions; migration is unavailable.";
        if (!evidence.MatchesExpectedIdentity || !evidence.Peaceful)
            return "Expired-AutoSave0 recovery requires complete owned identity and peaceful evidence.";
        if (request.MinimumRecoveryGameTick < ticketMinimumGameTick
            || evidence.GameTick != request.ExpectedRecoveryGameTick
            || evidence.GameTick < request.MinimumRecoveryGameTick
            || evidence.GameTick <= ticketMinimumGameTick
            || !primaryGameTick.HasValue
            || primaryGameTick.Value != ticketMinimumGameTick)
            return "The fixed AutoSave0 and exact protected primary do not prove the approved newer candidate and ticket progress floor.";
        if (writtenAtUtc < ticketIssuedAtUtc - TimeSpan.FromSeconds(2)
            || evidence.SavedAtUtc < ticketIssuedAtUtc - TimeSpan.FromSeconds(2))
            return "The fixed AutoSave0 candidate predates its protected ticket.";
        return null;
    }

    public static bool HasMatchingEcho(PrepareOwnedWorldResumeRequest request,
        PreparedOwnedWorldResumePlan plan) =>
        request.RecoveryMode == OwnedWorldResumeModes.ReauthorizeExpiredAutosave0
        && request.MinimumRecoveryGameTick is long known && known >= 0
        && request.ExpectedRecoveryGameTick is long expected && expected >= known
        && plan.Prepared
        && plan.RecoveryMode == OwnedWorldResumeModes.ReauthorizeExpiredAutosave0
        && plan.RecoveryEvidenceVersion == EvidenceVersion
        && plan.ExactEmbeddedIdentityVerified
        && plan.CandidateGameTick == expected
        && plan.MinimumGameTick == expected
        && plan.UserConfirmationRequired
        && !plan.CommitAllowedNow
        && plan.ConfirmationPrompt == ConfirmationPrompt
        && !string.IsNullOrWhiteSpace(plan.ConfirmationDigest)
        && IsSupportedSameVersion(plan.SourceGameVersion, plan.TargetGameVersion);

    private static bool IsSupportedSameVersion(string sourceGameVersion, string currentGameVersion) =>
        string.Equals(sourceGameVersion, currentGameVersion, StringComparison.Ordinal)
        && OwnedWorldVersionCompatibilityPolicy.IsResearchedTarget(currentGameVersion);
}
