using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Sessions;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class OwnedWorldRecoveryPolicyTests
{
    private const string Identity = "synthetic-protected-world-identity";
    private static readonly DateTimeOffset Issued = new(2026, 9, 16, 0, 0, 0, TimeSpan.Zero);

    private static PrepareOwnedWorldResumeRequest Request() => new()
    {
        RecoveryMode = OwnedWorldResumeModes.VerifiedNewerLastExit, UserConfirmedInConversation = true,
        MinimumRecoveryGameTick = 12000, ExpectedRecoveryGameTick = 12345,
    };

    [Fact]
    public void DefaultRemainsSeparateAndCannotHideExplicitRecoveryOptions()
    {
        Assert.Null(OwnedWorldRecoveryPolicy.ValidateRequest(new PrepareOwnedWorldResumeRequest()));
        foreach (var request in new[]
        {
            new PrepareOwnedWorldResumeRequest { UserConfirmedInConversation = true },
            new PrepareOwnedWorldResumeRequest { ExpectedRecoveryGameTick = 12345 },
            new PrepareOwnedWorldResumeRequest { MinimumRecoveryGameTick = 12000 },
            new PrepareOwnedWorldResumeRequest { RecoveryMode = "_autosave_0" },
            new PrepareOwnedWorldResumeRequest { RecoveryMode = "arbitrary-user-name" },
            new PrepareOwnedWorldResumeRequest { RecoveryMode = null! },
        }) Assert.NotNull(OwnedWorldRecoveryPolicy.ValidateRequest(request));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExactNewerCandidatePassesRegardlessOfSandboxState(bool sandbox)
    {
        using var stream = OwnedSavePrefixReaderTests.Fixture(sandbox: sandbox);
        var evidence = OwnedSavePrefixReader.Read(stream, Identity);
        Assert.Null(Validate(Request(), evidence));
    }

    [Theory]
    [InlineData("confirmation")]
    [InlineData("missing-floor")]
    [InlineData("missing-candidate")]
    [InlineData("negative-floor")]
    [InlineData("floor-before-ticket")]
    [InlineData("floor-after-candidate")]
    [InlineData("different-candidate")]
    [InlineData("quarantine")]
    [InlineData("legacy-journal")]
    [InlineData("version")]
    [InlineData("mtime")]
    [InlineData("primary-newer")]
    [InlineData("primary-equal")]
    [InlineData("not-newer-than-ticket")]
    public void MissingOrChangedProofRejectsCandidate(string changed)
    {
        using var stream = OwnedSavePrefixReaderTests.Fixture();
        var evidence = OwnedSavePrefixReader.Read(stream, Identity);
        var request = Request();
        switch (changed)
        {
            case "confirmation": request.UserConfirmedInConversation = false; break;
            case "missing-floor": request.MinimumRecoveryGameTick = null; break;
            case "missing-candidate": request.ExpectedRecoveryGameTick = null; break;
            case "negative-floor": request.MinimumRecoveryGameTick = -1; break;
            case "floor-before-ticket": request.MinimumRecoveryGameTick = 9999; break;
            case "floor-after-candidate": request.MinimumRecoveryGameTick = 12346; break;
            case "different-candidate": request.ExpectedRecoveryGameTick = 12346; break;
        }
        Assert.NotNull(OwnedWorldRecoveryPolicy.ValidateCandidate(request,
            healthyTicket: changed != "quarantine", journalCheckpointVerified: changed != "legacy-journal",
            ticketMinimumGameTick: changed == "not-newer-than-ticket" ? 12345 : 10000,
            Issued, currentGameVersion: changed == "version" ? "other-version" : evidence.GameVersion,
            evidence, writtenAtUtc: changed == "mtime" ? Issued.AddSeconds(-3) : Issued,
            primaryGameTick: changed == "primary-newer" ? 12346 : changed == "primary-equal" ? 12345 : 10000));
    }

    [Theory]
    [InlineData("identity")]
    [InlineData("peaceful")]
    [InlineData("saved-time")]
    public void WrongEmbeddedEvidenceFailsClosed(string changed)
    {
        using var stream = OwnedSavePrefixReaderTests.Fixture(identity: changed == "identity" ? "other-owned-like-name" : Identity);
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            if (changed == "peaceful") { stream.Position = 19; writer.Write(false); }
            if (changed == "saved-time") { stream.Position = 44; writer.Write(Issued.AddSeconds(-3).UtcTicks); }
        }
        stream.Position = 0;
        Assert.NotNull(Validate(Request(), OwnedSavePrefixReader.Read(stream, Identity)));
    }

    [Theory]
    [InlineData(10001, false)] // Would satisfy the old ticket, but not this candidate.
    [InlineData(12344, false)]
    [InlineData(12345, true)]
    [InlineData(15945, true)]
    [InlineData(15946, false)]
    [InlineData(long.MaxValue, false)]
    public void CandidateWatermarkMustSurviveAdoption(long loaded, bool allowed) =>
        Assert.Equal(allowed, OwnedWorldRecoveryPolicy.AllowsAdoption(loaded, 12345, verifiedRecovery: true));

    [Fact]
    public void DefaultAdoptionRetainsOriginalUnboundedMinimumRule() =>
        Assert.True(OwnedWorldRecoveryPolicy.AllowsAdoption(99999, 10000, verifiedRecovery: false));

    [Theory]
    [InlineData("mode")]
    [InlineData("version")]
    [InlineData("identity")]
    [InlineData("candidate")]
    [InlineData("minimum")]
    public void OldOrMismatchedPluginEchoCannotExposeAPlan(string changed)
    {
        var plan = new PreparedOwnedWorldResumePlan
        {
            RecoveryMode = OwnedWorldResumeModes.VerifiedNewerLastExit, RecoveryEvidenceVersion = 1,
            ExactEmbeddedIdentityVerified = true, CandidateGameTick = 12345, MinimumGameTick = 12345,
        };
        Assert.True(OwnedWorldRecoveryPolicy.HasMatchingEcho(Request(), plan));
        switch (changed)
        {
            case "mode": plan.RecoveryMode = OwnedWorldResumeModes.Default; break;
            case "version": plan.RecoveryEvidenceVersion = 0; break;
            case "identity": plan.ExactEmbeddedIdentityVerified = false; break;
            case "candidate": plan.CandidateGameTick = 12346; break;
            case "minimum": plan.MinimumGameTick = 10000; break;
        }
        Assert.False(OwnedWorldRecoveryPolicy.HasMatchingEcho(Request(), plan));
    }

    private static string? Validate(PrepareOwnedWorldResumeRequest request, OwnedSavePrefixEvidence evidence) =>
        OwnedWorldRecoveryPolicy.ValidateCandidate(request, true, true, 10000, Issued,
            "0.10.34.28529", evidence, Issued, 10000);
}
