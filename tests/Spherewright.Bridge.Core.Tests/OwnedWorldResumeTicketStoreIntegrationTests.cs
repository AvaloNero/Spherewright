using System.Security.Cryptography;
using System.Text;
using BepInEx.Logging;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Journals;
using Spherewright.Plugin.Game;
using Spherewright.Plugin.RuntimeDescriptor;
using Spherewright.Plugin.Transport;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class OwnedWorldResumeTicketStoreIntegrationTests
{
    private const string GameVersion = "0.10.34.28529";
    private const string OwnedSaveName = "synthetic-protected-world-identity";
    private const string SessionId = "synthetic-session";
    private const int PlanetId = 104;
    private const long MinimumTick = 12345;
    private const string ConfirmationDigest = "sha256:test-confirmation";

    [Fact]
    public void ExpiredTicketIsRejectedByDefaultButHealthyCompleteProvenanceCanBePrepared()
    {
        using var scenario = ExpiredScenario.Create();

        Assert.False(scenario.Store.TryGetActiveTicket(scenario.Token, out _, out _));
        Assert.True(scenario.Store.TryGetExpiredPrimaryProvenance(
            scenario.Token, out var ticket, out var fingerprint, out var rejection));
        Assert.NotNull(ticket);
        Assert.False(string.IsNullOrWhiteSpace(fingerprint));
        Assert.Equal(string.Empty, rejection);
    }

    [Fact]
    public void ReauthorizationRejectsInconsistentReplicas()
    {
        using var scenario = ExpiredScenario.Create();
        scenario.MutateReplica(scenario.HandoffTicketPath, ticket => ticket.SourceSessionId = "different-session");
        scenario.ReopenStore();

        Assert.False(scenario.Store.TryGetExpiredPrimaryProvenance(scenario.Token, out _, out _, out _));
    }

    [Fact]
    public void ReauthorizationRejectsConsumedOrCorruptTombstone()
    {
        using (var consumed = ExpiredScenario.Create())
        {
            consumed.Store.Consume(consumed.Token);
            consumed.ReopenStore();
            Assert.False(consumed.Store.TryGetExpiredPrimaryProvenance(consumed.Token, out _, out _, out _));
        }

        using var corrupt = ExpiredScenario.Create();
        File.WriteAllText(corrupt.RuntimeTombstonePath, "{not-json", Encoding.UTF8);
        corrupt.ReopenStore();
        Assert.False(corrupt.Store.TryGetExpiredPrimaryProvenance(corrupt.Token, out _, out _, out _));
    }

    [Theory]
    [InlineData("missing-journal")]
    [InlineData("missing-checkpoint")]
    [InlineData("extra-sequence")]
    public void ReauthorizationRejectsIncompleteOrChangedJournalContinuity(string change)
    {
        using var scenario = ExpiredScenario.Create();
        switch (change)
        {
            case "missing-journal": File.Delete(scenario.JournalPath); break;
            case "missing-checkpoint": scenario.MutateBothReplicas(ticket => ticket.GameplayJournalCheckpoint = null); break;
            case "extra-sequence": scenario.WriteJournal(1, 2, 3); break;
        }
        scenario.ReopenStore();

        Assert.False(scenario.Store.TryGetExpiredPrimaryProvenance(scenario.Token, out _, out _, out _));
    }

    [Fact]
    public void ChangedJournalContentInvalidatesThePreviouslyPreparedFingerprint()
    {
        using var scenario = ExpiredScenario.Create();
        Assert.True(scenario.Store.TryGetExpiredPrimaryProvenance(scenario.Token, out _, out var firstFingerprint, out _));

        scenario.WriteJournal(1, 2, createdAt: "changed-with-same-continuity");
        Assert.True(scenario.Store.TryGetExpiredPrimaryProvenance(scenario.Token, out _, out var changedFingerprint, out _));
        Assert.NotEqual(firstFingerprint, changedFingerprint);
        Assert.False(scenario.Store.TryConsumeReauthorization(
            scenario.Token, firstFingerprint, Guid.NewGuid().ToString(), ConfirmationDigest));
    }

    [Fact]
    public void ReauthorizationWritesAttemptAndTombstoneAndCannotReviveAfterReopen()
    {
        using var scenario = ExpiredScenario.Create();
        Assert.True(scenario.Store.TryGetExpiredPrimaryProvenance(scenario.Token, out _, out var fingerprint, out _));

        Assert.True(scenario.Store.TryConsumeReauthorization(
            scenario.Token, fingerprint, Guid.NewGuid().ToString(), ConfirmationDigest));
        Assert.True(File.Exists(scenario.RuntimeAttemptPath));
        Assert.True(File.Exists(scenario.HandoffAttemptPath));
        Assert.True(File.Exists(scenario.RuntimeTombstonePath));

        scenario.ReopenStore();
        Assert.False(scenario.Store.TryGetExpiredPrimaryProvenance(scenario.Token, out _, out _, out _));
        Assert.False(scenario.Store.TryGetActiveTicket(scenario.Token, out _, out _));
    }

    [Fact]
    public void ReauthorizationDoesNotPassWhenProtectedAttemptPersistenceFails()
    {
        using var scenario = ExpiredScenario.Create();
        Assert.True(scenario.Store.TryGetExpiredPrimaryProvenance(scenario.Token, out _, out var fingerprint, out _));
        scenario.EnableAttemptWriteFailure();

        Assert.False(scenario.Store.TryConsumeReauthorization(
            scenario.Token, fingerprint, Guid.NewGuid().ToString(), ConfirmationDigest));
        Assert.False(File.Exists(scenario.RuntimeTombstonePath));
        Assert.True(scenario.Store.TryGetExpiredPrimaryProvenance(scenario.Token, out _, out _, out _));
    }

    [Fact]
    public void SingleReplicaAttemptFailureLeavesAnAttemptFenceAfterReopen()
    {
        using var scenario = ExpiredScenario.Create();
        Assert.True(scenario.Store.TryGetExpiredPrimaryProvenance(scenario.Token, out _, out var fingerprint, out _));
        scenario.EnableAttemptWriteFailureOnHandoffOnly();

        Assert.False(scenario.Store.TryConsumeReauthorization(
            scenario.Token, fingerprint, Guid.NewGuid().ToString(), ConfirmationDigest));
        Assert.True(File.Exists(scenario.RuntimeAttemptPath));
        scenario.ReopenStore();
        Assert.False(scenario.Store.TryGetExpiredPrimaryProvenance(scenario.Token, out _, out _, out _));
    }

    [Fact]
    public void TombstonePersistenceFailureDoesNotPassAndAttemptFenceSurvivesReopen()
    {
        using var scenario = ExpiredScenario.Create();
        Assert.True(scenario.Store.TryGetExpiredPrimaryProvenance(scenario.Token, out _, out var fingerprint, out _));
        scenario.EnableTombstoneWriteFailure();

        Assert.False(scenario.Store.TryConsumeReauthorization(
            scenario.Token, fingerprint, Guid.NewGuid().ToString(), ConfirmationDigest));
        Assert.True(File.Exists(scenario.RuntimeAttemptPath));
        Assert.False(File.Exists(scenario.RuntimeTombstonePath));
        scenario.ReopenStore();
        Assert.False(scenario.Store.TryGetExpiredPrimaryProvenance(scenario.Token, out _, out _, out _));
    }

    [Fact]
    public void ReauthorizationJournalLeaseBlocksWritersUntilReleased()
    {
        using var scenario = ExpiredScenario.Create();
        Assert.True(scenario.Store.TryGetExpiredPrimaryProvenance(scenario.Token, out var ticket, out var fingerprint, out _));

        using (scenario.Store.OpenReauthorizationJournalLease(ticket!, fingerprint))
        {
            Assert.Throws<IOException>(() =>
            {
                using var writer = new FileStream(scenario.JournalPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
                writer.WriteByte(0x20);
            });
        }

        using (var writer = new FileStream(scenario.JournalPath, FileMode.Open, FileAccess.Write, FileShare.None))
        {
            writer.WriteByte(0x20);
        }
    }

    private sealed class ExpiredScenario : IDisposable
    {
        private readonly string _root;
        private readonly string _runtimeDirectory;
        private readonly string _handoffDirectory;

        private ExpiredScenario(string root)
        {
            _root = root;
            _runtimeDirectory = Path.Combine(root, "runtime");
            _handoffDirectory = Path.Combine(root, "handoff");
            Directory.CreateDirectory(_runtimeDirectory);
            Directory.CreateDirectory(_handoffDirectory);
            Store = NewStore();
        }

        public OwnedWorldResumeTicketStore Store { get; private set; }
        public string Token { get; private set; } = string.Empty;
        public string RuntimeTicketPath => Path.Combine(_runtimeDirectory, "owned-world-resume.json");
        public string HandoffTicketPath => Path.Combine(_handoffDirectory, "owned-world-resume.json");
        public string JournalPath => Path.Combine(_runtimeDirectory, "journals", $"gameplay-{JournalIdentity}.json");
        public string RuntimeTombstonePath => Path.Combine(_runtimeDirectory, $"owned-world-resume-consumed-{TokenHash}.json");
        public string RuntimeAttemptPath => Path.Combine(_runtimeDirectory, $"owned-world-reauthorization-attempt-{TokenHash}.json");
        public string HandoffAttemptPath => Path.Combine(_handoffDirectory, $"owned-world-reauthorization-attempt-{TokenHash}.json");
        private string JournalIdentity => GameplayJournalIdentity.HashOwnedSaveIdentity(OwnedSaveName);
        private string TokenHash => HashToken(Token);

        public static ExpiredScenario Create()
        {
            var scenario = new ExpiredScenario(Path.Combine(Path.GetTempPath(), "Spherewright", "owned-ticket-tests", Guid.NewGuid().ToString("N")));
            using var primary = OwnedSavePrefixReaderTests.Fixture(identity: OwnedSaveName);
            var evidence = OwnedSavePrefixReader.Read(primary, OwnedSaveName);
            Assert.Equal(MinimumTick, evidence.GameTick);
            Assert.Equal(GameVersion, evidence.GameVersion);
            var checkpoint = new OwnedWorldGameplayJournalCheckpoint
            {
                Version = OwnedWorldGameplayJournalCheckpoint.CurrentVersion,
                JournalId = scenario.JournalIdentity,
                TrackingMode = GameplayJournalTrackingModes.AttachedExistingSave,
                HistoricalCoverageComplete = true,
                TrackingStartedAtGameTick = MinimumTick,
                MinimumDurableThroughSequence = 2,
            };
            scenario.Store.ArmFromHealthySavedOwnedSession(OwnedSaveName, SessionId, PlanetId, MinimumTick, checkpoint);
            Assert.NotNull(scenario.Store.CurrentResumeToken);
            scenario.Token = scenario.Store.CurrentResumeToken!;
            scenario.WriteJournal(1, 2);
            var issued = DateTimeOffset.UtcNow.AddDays(-2);
            var expires = issued.AddDays(1);
            scenario.MutateBothReplicas(ticket =>
            {
                ticket.IssuedAtUtc = issued;
                ticket.ExpiresAtUtc = expires;
            });
            scenario.ReopenStore();
            return scenario;
        }

        public void ReopenStore() => Store = NewStore();

        public void MutateBothReplicas(Action<OwnedWorldResumeTicket> mutate)
        {
            MutateReplica(RuntimeTicketPath, mutate);
            MutateReplica(HandoffTicketPath, mutate);
        }

        public void MutateReplica(string path, Action<OwnedWorldResumeTicket> mutate)
        {
            var ticket = Assert.IsType<OwnedWorldResumeTicket>(PluginJson.Deserialize<OwnedWorldResumeTicket>(File.ReadAllText(path)));
            mutate(ticket);
            File.WriteAllText(path, PluginJson.Serialize(ticket), Encoding.UTF8);
        }

        public void WriteJournal(params long[] sequences) => WriteJournal(sequences, "synthetic-original");

        public void WriteJournal(long first, long second, string createdAt) => WriteJournal(new[] { first, second }, createdAt);

        public void EnableAttemptWriteFailure()
        {
            File.WriteAllText(Path.Combine(_runtimeDirectory, ".test-only-fail-secure-write"), "test");
            File.WriteAllText(Path.Combine(_handoffDirectory, ".test-only-fail-secure-write"), "test");
        }

        public void EnableAttemptWriteFailureOnHandoffOnly() =>
            File.WriteAllText(Path.Combine(_handoffDirectory, ".test-only-fail-secure-write"), "test");

        public void EnableTombstoneWriteFailure()
        {
            File.WriteAllText(Path.Combine(_runtimeDirectory, ".test-only-fail-second-secure-write"), "test");
            File.WriteAllText(Path.Combine(_handoffDirectory, ".test-only-fail-second-secure-write"), "test");
        }

        public void Dispose()
        {
            try { Directory.Delete(_root, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private OwnedWorldResumeTicketStore NewStore() => new(
            _runtimeDirectory, _handoffDirectory, "test-bridge", GameVersion, new ManualLogSource());

        private void WriteJournal(IReadOnlyList<long> sequences, string createdAt)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(JournalPath)!);
            var document = new GameplayJournalDocument
            {
                Version = 1,
                JournalId = JournalIdentity,
                OwnedSaveIdentityHash = JournalIdentity,
                GameVersion = GameVersion,
                TrackingMode = GameplayJournalTrackingModes.AttachedExistingSave,
                HistoricalCoverageComplete = true,
                CreatedAtActualTime = createdAt,
                TrackingStartedAtGameTick = MinimumTick,
                Entries = sequences.Select(sequence => new GameplayJournalEntry { Sequence = sequence }).ToList(),
            };
            File.WriteAllText(JournalPath, PluginJson.Serialize(document), Encoding.UTF8);
        }

        private static string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            return Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
        }
    }
}
