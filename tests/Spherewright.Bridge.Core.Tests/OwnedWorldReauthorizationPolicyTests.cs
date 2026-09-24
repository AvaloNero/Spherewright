using System.Text;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Sessions;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class OwnedWorldReauthorizationPolicyTests
{
    private const string Identity = "synthetic-protected-world-identity";
    private const string ConfirmationDigest = "sha256:exact-owned-primary-plan";
    private static readonly DateTimeOffset Issued = new(2026, 9, 19, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("expired-healthy-complete", true)]
    [InlineData("active", false)]
    [InlineData("consumed", false)]
    [InlineData("quarantine", false)]
    [InlineData("legacy-journal", false)]
    [InlineData("future-issued", false)]
    [InlineData("bad-clock", false)]
    public void OnlyExpiredHealthyCompleteAndUnconsumedProvenanceCanPrepare(string state, bool allowed)
    {
        var healthy = true;
        var journalVerified = true;
        var consumed = false;
        var issued = Issued;
        var expires = Issued.AddHours(24);
        var now = expires;

        switch (state)
        {
            case "active": now = expires.AddTicks(-1); break;
            case "consumed": consumed = true; break;
            case "quarantine": healthy = false; break;
            case "legacy-journal": journalVerified = false; break;
            case "future-issued": issued = now.AddHours(1); expires = issued.AddHours(1); break;
            case "bad-clock": issued = expires; break;
        }

        Assert.Equal(allowed, OwnedWorldReauthorizationPolicy.AllowsExpiredProvenance(
            healthy, journalVerified, consumed, issued, expires, now));
    }

    [Fact]
    public void ExactOwnedPrimaryEvidenceCanBeReauthorized()
    {
        var evidence = ReadEvidence();

        Assert.Null(OwnedWorldReauthorizationPolicy.ValidateCandidate(
            evidence.GameTick, evidence.GameVersion, evidence));
    }

    [Theory]
    [InlineData("older-primary")]
    [InlineData("newer-primary")]
    [InlineData("identity")]
    [InlineData("version")]
    [InlineData("peaceful")]
    public void DifferentPrimaryOrEmbeddedIdentityEvidenceFailsClosed(string changed)
    {
        var evidence = ReadEvidence(
            identity: changed == "identity" ? "other-world" : Identity,
            peaceful: changed != "peaceful");
        var savedTick = evidence.GameTick;
        var gameVersion = evidence.GameVersion;
        if (changed == "older-primary") savedTick--;
        if (changed == "newer-primary") savedTick++;
        if (changed == "version") gameVersion = "other-version";

        Assert.NotNull(OwnedWorldReauthorizationPolicy.ValidateCandidate(savedTick, gameVersion, evidence));
    }

    [Theory]
    [InlineData("confirmation")]
    [InlineData("minimum-last-exit-bound")]
    [InlineData("expected-last-exit-bound")]
    public void ReauthorizationPrepareIsDisclosureOnlyAndCannotCarryConfirmationOrLastExitBounds(string changed)
    {
        var request = ReauthorizationRequest();
        Assert.Null(OwnedWorldRecoveryPolicy.ValidateRequest(request));

        switch (changed)
        {
            case "confirmation": request.UserConfirmedInConversation = true; break;
            case "minimum-last-exit-bound": request.MinimumRecoveryGameTick = 12345; break;
            case "expected-last-exit-bound": request.ExpectedRecoveryGameTick = 12345; break;
        }

        Assert.NotNull(OwnedWorldRecoveryPolicy.ValidateRequest(request));
    }

    [Theory]
    [InlineData("old-plugin")]
    [InlineData("wrong-mode")]
    [InlineData("confirmation-missing")]
    [InlineData("digest-missing")]
    [InlineData("commit-allowed")]
    public void ShortPreparedPlanMustEchoTheExactReauthorizationContract(string changed)
    {
        var plan = MatchingPlan();
        Assert.True(OwnedWorldReauthorizationPolicy.HasMatchingEcho(plan));

        switch (changed)
        {
            case "old-plugin": plan.RecoveryEvidenceVersion--; break;
            case "wrong-mode": plan.RecoveryMode = OwnedWorldResumeModes.VerifiedNewerLastExit; break;
            case "confirmation-missing": plan.UserConfirmationRequired = false; break;
            case "digest-missing": plan.ConfirmationDigest = string.Empty; break;
            case "commit-allowed": plan.CommitAllowedNow = true; break;
        }

        Assert.False(OwnedWorldReauthorizationPolicy.HasMatchingEcho(plan));
    }

    [Theory]
    [InlineData(true, ConfirmationDigest, ConfirmationDigest, true)]
    [InlineData(false, ConfirmationDigest, ConfirmationDigest, false)]
    [InlineData(true, ConfirmationDigest, "sha256:wrong-plan", false)]
    [InlineData(true, "", ConfirmationDigest, false)]
    public void ConfirmationMustBeExplicitAndMatchThePreparedDigest(
        bool confirmed, string expectedDigest, string receivedDigest, bool allowed) =>
        Assert.Equal(allowed, OwnedWorldReauthorizationPolicy.MatchesConfirmation(
            confirmed, expectedDigest, receivedDigest));

    [Theory]
    [InlineData(12344, true, false)]
    [InlineData(12345, true, true)]
    [InlineData(12346, true, false)]
    [InlineData(12344, false, false)]
    [InlineData(12345, false, false)]
    [InlineData(12346, false, true)]
    public void LeaseEqualityIsReservedForReauthorizationAndLastExitRemainsStrictlyNewer(
        long candidate, bool reauthorizing, bool allowed) =>
        Assert.Equal(allowed, OwnedWorldReauthorizationPolicy.AllowsLeaseTick(candidate, 12345, reauthorizing));

    private static PrepareOwnedWorldResumeRequest ReauthorizationRequest() => new()
    {
        RecoveryMode = OwnedWorldResumeModes.ReauthorizeExpiredPrimary,
    };

    private static PreparedOwnedWorldResumePlan MatchingPlan() => new()
    {
        Prepared = true,
        RecoveryMode = OwnedWorldResumeModes.ReauthorizeExpiredPrimary,
        RecoveryEvidenceVersion = OwnedWorldReauthorizationPolicy.EvidenceVersion,
        ExactEmbeddedIdentityVerified = true,
        CandidateGameTick = 12345,
        MinimumGameTick = 12345,
        UserConfirmationRequired = true,
        ConfirmationPrompt = OwnedWorldReauthorizationPolicy.ConfirmationPrompt,
        ConfirmationDigest = ConfirmationDigest,
        CommitAllowedNow = false,
    };

    private static OwnedSavePrefixEvidence ReadEvidence(string identity = Identity, bool peaceful = true)
    {
        using var stream = OwnedSavePrefixReaderTests.Fixture(identity: identity);
        if (!peaceful)
        {
            stream.Position = 19;
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            writer.Write(false);
        }
        stream.Position = 0;
        return OwnedSavePrefixReader.Read(stream, Identity);
    }
}
