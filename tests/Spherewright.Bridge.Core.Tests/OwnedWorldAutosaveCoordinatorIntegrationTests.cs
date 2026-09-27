using System.Text;
using BepInEx.Logging;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Journals;
using Spherewright.Contracts.Sessions;
using Spherewright.Plugin.Game;
using Spherewright.Plugin.RuntimeDescriptor;
using Spherewright.Plugin.Transport;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

[CollectionDefinition("Owned resume native substitutes", DisableParallelization = true)]
public sealed class OwnedResumeNativeSubstitutesCollection { }

[Collection("Owned resume native substitutes")]
public sealed class OwnedWorldAutosaveCoordinatorIntegrationTests
{
    private const string Identity = "synthetic-autosave-owned-identity";
    private const string Version = "0.10.35.29088";
    private const long SavedTick = 12345;
    private const long CandidateTick = 20000;

    [Fact]
    public void PrepareDisclosesWithoutLoadOrConsumptionAndCommitNeedsSubsequentConsent()
    {
        using var fixture = new Fixture();
        var plan = fixture.Prepare();
        Assert.True(plan.UserConfirmationRequired);
        Assert.False(plan.CommitAllowedNow);
        Assert.Equal(CandidateTick, plan.CandidateGameTick);
        Assert.Equal(0, DSPGame.LoadCalls);
        Assert.True(fixture.HasProvenance());
        var rejected = fixture.Coordinator.CommitOnMainThread(new CommitOwnedWorldResumeRequest
        {
            PlanToken = plan.PlanToken, IdempotencyKey = Guid.NewGuid().ToString(),
            ConfirmationDigest = plan.ConfirmationDigest,
        });
        Assert.False(rejected.Success);
        Assert.Equal(BridgeErrorCodes.UserConfirmationRequired, rejected.Error!.Code);
        Assert.True(fixture.HasProvenance());
        Assert.Equal(0, DSPGame.LoadCalls);
    }

    [Fact]
    public void CommitConsumesBeforeExactLoaderAndIdempotencyDoesNotLoadTwice()
    {
        using var fixture = new Fixture();
        var request = fixture.Confirmed(fixture.Prepare());
        DSPGame.BeforeLoad = () =>
        {
            Assert.False(fixture.HasProvenance());
            Assert.Throws<IOException>(() => File.Open(fixture.PrimaryPath, FileMode.Open, FileAccess.Write).Dispose());
        };
        var result = fixture.Coordinator.CommitOnMainThread(request);
        Assert.True(result.Success, result.Error?.Message);
        Assert.True(result.Value!.Accepted);
        Assert.Equal(GameSave.AutoSave0, DSPGame.LoadedName);
        Assert.Equal(1, DSPGame.LoadCalls);
        Assert.True(fixture.Sessions.Reauthorizing);
        Assert.True(fixture.Sessions.FixedAutosave);
        Assert.Equal(CandidateTick, fixture.Sessions.SourceLease!.Prefix.GameTick);
        Assert.NotNull(fixture.Sessions.JournalLease);
        Assert.NotNull(fixture.Sessions.PrimaryLease);
        Assert.True(fixture.Coordinator.CommitOnMainThread(request).Value!.IdempotentReplay);
        Assert.Equal(1, DSPGame.LoadCalls);
        Assert.False(fixture.NewCoordinator().PrepareOnMainThread(fixture.Request()).Success);
        Assert.True(fixture.Coordinator.TryGetActionResultOnMainThread(result.Value.ActionId, out var pending));
        Assert.False(pending!.Terminal); // Fake session has not adopted or saved anything.
    }

    [Theory]
    [InlineData("file")]
    [InlineData("journal")]
    [InlineData("revision")]
    [InlineData("slot-missing")]
    [InlineData("primary-file")]
    [InlineData("primary-identity")]
    [InlineData("primary-newer-than-ticket")]
    public void PreparedEvidenceDriftNeverLoadsOrConsumes(string change)
    {
        using var fixture = new Fixture();
        var request = fixture.Confirmed(fixture.Prepare());
        switch (change)
        {
            case "file":
                var bytes = File.ReadAllBytes(fixture.CandidatePath);
                bytes[^1]++;
                File.WriteAllBytes(fixture.CandidatePath, bytes);
                break;
            case "journal": fixture.WriteJournal("changed valid content"); break;
            case "revision": fixture.Sessions.Revision++; break;
            case "slot-missing": File.Move(fixture.CandidatePath, GameSave.SavePath("_autosave_1")); break;
            case "primary-file":
                var primaryBytes = File.ReadAllBytes(fixture.PrimaryPath);
                primaryBytes[^1]++;
                File.WriteAllBytes(fixture.PrimaryPath, primaryBytes);
                break;
            case "primary-identity": Fixture.WriteSave(fixture.PrimaryPath, "different-world", SavedTick); break;
            case "primary-newer-than-ticket": Fixture.WriteSave(fixture.PrimaryPath, Identity, CandidateTick - 1); break;
        }
        var result = fixture.Coordinator.CommitOnMainThread(request);
        Assert.False(result.Success);
        Assert.Equal(BridgeErrorCodes.StaleState, result.Error!.Code);
        Assert.True(fixture.HasProvenance());
        Assert.Equal(0, DSPGame.LoadCalls);
    }

    [Fact]
    public void MissingFixedSlotNeverFallsBackToPrimaryLastExitOrAnotherAutosave()
    {
        using var fixture = new Fixture();
        File.Copy(fixture.CandidatePath, GameSave.SavePath(Identity), overwrite: true);
        File.Copy(fixture.CandidatePath, GameSave.SavePath(GameSave.LastExit));
        File.Move(fixture.CandidatePath, GameSave.SavePath("_autosave_1"));
        Assert.False(fixture.Coordinator.PrepareOnMainThread(fixture.Request()).Success);
        Assert.Equal(0, DSPGame.LoadCalls);
        Assert.True(fixture.HasProvenance());
    }

    [Fact]
    public void NativeFailureAfterConsumptionDoesNotReviveProvenanceAndReleasesLeases()
    {
        using var fixture = new Fixture();
        var request = fixture.Confirmed(fixture.Prepare());
        DSPGame.BeforeLoad = () =>
        {
            Assert.False(fixture.HasProvenance());
            throw new IOException("test-only native loader failure");
        };
        Assert.False(fixture.Coordinator.CommitOnMainThread(request).Success);
        fixture.ReopenStore(); // Actual on-disk tombstones/attempts, not just memory state.
        Assert.False(fixture.HasProvenance());
        Assert.False(fixture.NewCoordinator().PrepareOnMainThread(fixture.Request()).Success);
        Assert.Null(fixture.Sessions.SourceLease);
        Assert.Null(fixture.Sessions.JournalLease);
        Assert.Null(fixture.Sessions.PrimaryLease);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("corrupt")]
    [InlineData("identity")]
    [InlineData("unreadable")]
    [InlineData("newer-than-ticket")]
    public void UnknownOrDifferentOriginalPrimaryCannotBecomeAnOverwriteTarget(string change)
    {
        using var fixture = new Fixture();
        FileStream? lockedPrimary = null;
        try
        {
            switch (change)
            {
                case "missing": File.Delete(fixture.PrimaryPath); break;
                case "corrupt": File.WriteAllText(fixture.PrimaryPath, "not a save"); break;
                case "identity": Fixture.WriteSave(fixture.PrimaryPath, "different-world", SavedTick); break;
                case "newer-than-ticket": Fixture.WriteSave(fixture.PrimaryPath, Identity, CandidateTick - 1); break;
                case "unreadable": lockedPrimary = File.Open(fixture.PrimaryPath, FileMode.Open, FileAccess.Read, FileShare.None); break;
            }
            Assert.False(fixture.Coordinator.PrepareOnMainThread(fixture.Request()).Success);
            Assert.True(fixture.HasProvenance());
            Assert.Equal(0, DSPGame.LoadCalls);
        }
        finally { lockedPrimary?.Dispose(); }
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "Spherewright", "autosave-coordinator", Guid.NewGuid().ToString("N"));
        private string Runtime => Path.Combine(_root, "runtime");
        private string Handoff => Path.Combine(_root, "handoff");
        private string JournalId => GameplayJournalIdentity.HashOwnedSaveIdentity(Identity);
        internal string CandidatePath => GameSave.SavePath(GameSave.AutoSave0);
        internal string PrimaryPath => GameSave.SavePath(Identity);
        internal OwnedWorldResumeTicketStore Store { get; private set; } = null!;
        internal string Token { get; }
        internal GameSessionTracker Sessions { get; } = new();
        internal OwnedWorldResumeCoordinator Coordinator { get; }

        internal Fixture()
        {
            Directory.CreateDirectory(Runtime);
            Directory.CreateDirectory(Handoff);
            GameSave.Root = Path.Combine(_root, "saves");
            Directory.CreateDirectory(GameSave.Root);
            DSPGame.LoadCalls = 0;
            DSPGame.LoadedName = null;
            DSPGame.BeforeLoad = null;
            TestWorldCoordinator.ReadinessError = null;
            ReopenStore();
            Store.ArmFromHealthySavedOwnedSession(Identity, "synthetic-session", 104, SavedTick,
                new OwnedWorldGameplayJournalCheckpoint
                {
                    Version = 1, JournalId = JournalId, TrackingMode = GameplayJournalTrackingModes.AttachedExistingSave,
                    HistoricalCoverageComplete = false, TrackingStartedAtGameTick = SavedTick, MinimumDurableThroughSequence = 2,
                });
            Token = Store.CurrentResumeToken!;
            WriteJournal("original");
            var issuedAt = DateTimeOffset.UtcNow.AddDays(-2);
            var expiresAt = issuedAt.AddDays(1);
            foreach (var directory in new[] { Runtime, Handoff })
            {
                var path = Path.Combine(directory, "owned-world-resume.json");
                var ticket = PluginJson.Deserialize<OwnedWorldResumeTicket>(File.ReadAllText(path))!;
                ticket.IssuedAtUtc = issuedAt;
                ticket.ExpiresAtUtc = expiresAt;
                File.WriteAllText(path, PluginJson.Serialize(ticket));
            }
            ReopenStore();
            WriteSave(CandidatePath, Identity, CandidateTick);
            WriteSave(PrimaryPath, Identity, SavedTick);
            Coordinator = NewCoordinator();
        }

        internal static void WriteSave(string path, string identity, long gameTick)
        {
            using var stream = OwnedSavePrefixReaderTests.Fixture(identity: identity,
                patch: 23, versionPatch: 35, versionBuild: 29088, gameDescVersion: 10);
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                stream.Position = 36;
                writer.Write(gameTick);
                writer.Write(DateTime.UtcNow.Ticks);
            }
            File.WriteAllBytes(path, stream.ToArray());
        }

        internal void ReopenStore() => Store = new(Runtime, Handoff, "synthetic-bridge", Version, new ManualLogSource());
        internal OwnedWorldResumeCoordinator NewCoordinator() => new(true, 60, 10, 16, Sessions, Store);
        internal bool HasProvenance() => Store.TryGetExpiredPrimaryProvenance(Token, out _, out _, out _);
        internal PrepareOwnedWorldResumeRequest Request() => new()
        {
            ResumeToken = Token, RecoveryMode = OwnedWorldResumeModes.ReauthorizeExpiredAutosave0,
            MinimumRecoveryGameTick = CandidateTick - 10, ExpectedRecoveryGameTick = CandidateTick,
        };
        internal PreparedOwnedWorldResumePlan Prepare()
        {
            var result = Coordinator.PrepareOnMainThread(Request());
            Assert.True(result.Success, result.Error?.Message);
            return result.Value!;
        }
        internal CommitOwnedWorldResumeRequest Confirmed(PreparedOwnedWorldResumePlan plan) => new()
        {
            PlanToken = plan.PlanToken, IdempotencyKey = Guid.NewGuid().ToString(),
            UserConfirmedInConversation = true, ConfirmationDigest = plan.ConfirmationDigest,
        };
        internal void WriteJournal(string creationMarker)
        {
            var path = Path.Combine(Runtime, "journals", "gameplay-" + JournalId + ".json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, PluginJson.Serialize(new GameplayJournalDocument
            {
                Version = 1, JournalId = JournalId, OwnedSaveIdentityHash = JournalId, GameVersion = Version,
                TrackingMode = GameplayJournalTrackingModes.AttachedExistingSave, HistoricalCoverageComplete = false,
                TrackingStartedAtGameTick = SavedTick, CreatedAtActualTime = creationMarker,
                Entries = new() { new() { Sequence = 1 }, new() { Sequence = 2 } },
            }));
        }
        public void Dispose()
        {
            Sessions.Dispose();
            DSPGame.BeforeLoad = null;
            Directory.Delete(_root, recursive: true); // Only this test's newly created GUID-owned tree.
        }
    }
}
