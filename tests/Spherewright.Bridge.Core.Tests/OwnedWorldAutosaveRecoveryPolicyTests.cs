using System.Text;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Sessions;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class OwnedWorldAutosaveRecoveryPolicyTests
{
    private const string Identity = "synthetic-protected-world-identity";
    private const string Version = "0.10.35.29088";
    private const string ConfirmationDigest = "sha256:exact-autosave0-plan";
    private static readonly DateTimeOffset Issued = new(2026, 9, 16, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ExactNewerSameVersionAutoSave0CandidatePasses()
    {
        var evidence = ReadEvidence();

        Assert.Null(Validate(Request(), evidence));
    }

    [Theory]
    [InlineData("confirmation")]
    [InlineData("missing-known")]
    [InlineData("missing-expected")]
    [InlineData("negative-known")]
    [InlineData("expected-before-known")]
    public void AutoSave0PrepareIsDisclosureOnlyAndRequiresExactBounds(string changed)
    {
        var request = Request();
        switch (changed)
        {
            case "confirmation": request.UserConfirmedInConversation = true; break;
            case "missing-known": request.MinimumRecoveryGameTick = null; break;
            case "missing-expected": request.ExpectedRecoveryGameTick = null; break;
            case "negative-known": request.MinimumRecoveryGameTick = -1; break;
            case "expected-before-known": request.ExpectedRecoveryGameTick = 11999; break;
        }

        Assert.NotNull(OwnedWorldRecoveryPolicy.ValidateRequest(request));
    }

    [Theory]
    [InlineData("ticket-floor")]
    [InlineData("candidate")]
    [InlineData("not-newer")]
    [InlineData("primary-missing")]
    [InlineData("primary-older")]
    [InlineData("primary-equal")]
    [InlineData("primary-newer")]
    [InlineData("source-migration")]
    [InlineData("unsupported-same-version")]
    [InlineData("written-before-ticket")]
    [InlineData("saved-before-ticket")]
    public void ProgressVersionAndFreshnessChangesRejectAutoSave0Candidate(string changed)
    {
        var request = Request();
        var evidence = ReadEvidence();
        var ticketMinimum = 12000L;
        var ticketIssued = Issued;
        var source = Version;
        var current = Version;
        var written = Issued;
        long? primary = 12000;

        switch (changed)
        {
            case "ticket-floor": request.MinimumRecoveryGameTick = 11999; break;
            case "candidate": request.ExpectedRecoveryGameTick = 12346; break;
            case "not-newer": ticketMinimum = 12345; break;
            case "primary-missing": primary = null; break;
            case "primary-older": primary = 11999; break;
            case "primary-equal": primary = 12345; break;
            case "primary-newer": primary = 12346; break;
            case "source-migration": source = "0.10.34.28529"; break;
            case "unsupported-same-version": source = current = "0.10.33.00000"; break;
            case "written-before-ticket": written = Issued.AddSeconds(-3); break;
            case "saved-before-ticket": ticketIssued = evidence.SavedAtUtc.AddSeconds(3); break;
        }

        Assert.NotNull(OwnedWorldAutosaveRecoveryPolicy.ValidateCandidate(request, ticketMinimum,
            ticketIssued, source, current, evidence, written, primary));
    }

    [Theory]
    [InlineData("identity")]
    [InlineData("peaceful")]
    [InlineData("candidate-version")]
    public void EmbeddedEvidenceMustRemainExactAndPeaceful(string changed)
    {
        var identity = changed == "identity" ? "different-world" : Identity;
        var evidence = ReadEvidence(identity: identity,
            versionBuild: changed == "candidate-version" ? 29057 : 29088);
        if (changed == "peaceful")
        {
            using var stream = OwnedSavePrefixReaderTests.Fixture(versionPatch: 35, versionBuild: 29088,
                patch: 23, gameDescVersion: 10);
            stream.Position = 19;
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true)) writer.Write(false);
            stream.Position = 0;
            evidence = OwnedSavePrefixReader.Read(stream, Identity);
        }

        Assert.NotNull(Validate(Request(), evidence));
    }

    [Theory]
    [InlineData("old-plugin")]
    [InlineData("wrong-mode")]
    [InlineData("not-prepared")]
    [InlineData("identity")]
    [InlineData("candidate")]
    [InlineData("minimum")]
    [InlineData("confirmation-missing")]
    [InlineData("commit-allowed")]
    [InlineData("prompt")]
    [InlineData("digest")]
    [InlineData("migration")]
    [InlineData("unsupported-same-version")]
    public void PluginEchoMustBindTheExactAutoSave0Disclosure(string changed)
    {
        var plan = MatchingPlan();
        Assert.True(OwnedWorldAutosaveRecoveryPolicy.HasMatchingEcho(Request(), plan));

        switch (changed)
        {
            case "old-plugin": plan.RecoveryEvidenceVersion--; break;
            case "wrong-mode": plan.RecoveryMode = OwnedWorldResumeModes.ReauthorizeExpiredPrimary; break;
            case "not-prepared": plan.Prepared = false; break;
            case "identity": plan.ExactEmbeddedIdentityVerified = false; break;
            case "candidate": plan.CandidateGameTick = 12346; break;
            case "minimum": plan.MinimumGameTick = 12000; break;
            case "confirmation-missing": plan.UserConfirmationRequired = false; break;
            case "commit-allowed": plan.CommitAllowedNow = true; break;
            case "prompt": plan.ConfirmationPrompt = "other"; break;
            case "digest": plan.ConfirmationDigest = string.Empty; break;
            case "migration": plan.SourceGameVersion = "0.10.34.28529"; break;
            case "unsupported-same-version": plan.SourceGameVersion = plan.TargetGameVersion = "0.10.33.00000"; break;
        }

        Assert.False(OwnedWorldAutosaveRecoveryPolicy.HasMatchingEcho(Request(), plan));
    }

    [Theory]
    [InlineData(12345, true, true, false)]
    [InlineData(12346, true, true, true)]
    [InlineData(12346, false, true, false)]
    [InlineData(12345, true, false, true)]
    [InlineData(12346, false, false, true)]
    public void AutoSave0LeaseRequiresReauthorizationAndStrictAdvanceWithoutChangingOldModes(
        long candidate, bool reauthorizing, bool reauthorizingAutosave0, bool allowed) =>
        Assert.Equal(allowed, OwnedWorldReauthorizationPolicy.AllowsLeaseTick(
            candidate, 12345, reauthorizing, reauthorizingAutosave0));

    private static PrepareOwnedWorldResumeRequest Request() => new()
    {
        RecoveryMode = OwnedWorldResumeModes.ReauthorizeExpiredAutosave0,
        MinimumRecoveryGameTick = 12000,
        ExpectedRecoveryGameTick = 12345,
    };

    private static PreparedOwnedWorldResumePlan MatchingPlan() => new()
    {
        Prepared = true,
        RecoveryMode = OwnedWorldResumeModes.ReauthorizeExpiredAutosave0,
        RecoveryEvidenceVersion = OwnedWorldAutosaveRecoveryPolicy.EvidenceVersion,
        ExactEmbeddedIdentityVerified = true,
        CandidateGameTick = 12345,
        MinimumGameTick = 12345,
        UserConfirmationRequired = true,
        ConfirmationPrompt = OwnedWorldAutosaveRecoveryPolicy.ConfirmationPrompt,
        ConfirmationDigest = ConfirmationDigest,
        SourceGameVersion = Version,
        TargetGameVersion = Version,
        CommitAllowedNow = false,
    };

    private static string? Validate(PrepareOwnedWorldResumeRequest request, OwnedSavePrefixEvidence evidence) =>
        OwnedWorldAutosaveRecoveryPolicy.ValidateCandidate(request, 12000, Issued,
            Version, Version, evidence, Issued, 12000);

    private static OwnedSavePrefixEvidence ReadEvidence(string identity = Identity, int versionBuild = 29088)
    {
        using var stream = OwnedSavePrefixReaderTests.Fixture(identity: identity, versionPatch: 35,
            versionBuild: versionBuild, patch: 23, gameDescVersion: 10);
        return OwnedSavePrefixReader.Read(stream, Identity);
    }
}
