using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using BepInEx.Logging;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Plugin.Game;
using Spherewright.Plugin.Security;
using Spherewright.Plugin.Transport;

namespace Spherewright.Plugin.RuntimeDescriptor;

internal sealed class OwnedWorldResumeTicketStore
{
    private const int TicketVersion = 1;
    private readonly string _ticketPath;
    private readonly string _handoffTicketPath;
    private readonly string _runtimeDirectory;
    private readonly string _bridgeInstanceId;
    private readonly string _gameVersion;
    private readonly ManualLogSource _logger;
    private OwnedWorldResumeTicket? _currentTicket;
    private string? _currentTicketPath;

    public OwnedWorldResumeTicketStore(
        string configuredRuntimeDirectory,
        string bridgeInstanceId,
        string gameVersion,
        ManualLogSource logger)
    {
        _runtimeDirectory = RuntimeDescriptorPublisher.ResolveRuntimeDirectory(configuredRuntimeDirectory);
        _ticketPath = Path.Combine(_runtimeDirectory, "owned-world-resume.json");
        var pluginDirectory = Path.GetDirectoryName(typeof(OwnedWorldResumeTicketStore).Assembly.Location)
            ?? throw new InvalidOperationException("The Spherewright Plugin directory is unavailable.");
        var handoffDirectory = Path.Combine(pluginDirectory, "runtime-handoff");
        WindowsCurrentUserSecurity.EnsureSecureDirectory(handoffDirectory);
        _handoffTicketPath = Path.Combine(handoffDirectory, "owned-world-resume.json");
        _bridgeInstanceId = bridgeInstanceId;
        _gameVersion = gameVersion;
        _logger = logger;
        _logger.LogInfo("Spherewright initialized the protected owned-world resume ticket store");
        _currentTicket = ReadFromDisk();
    }

    public string? CurrentResumeToken => _currentTicket?.ResumeToken;

    public bool HasCurrentTicket => _currentTicket is not null;

    public void ArmFromHealthySavedOwnedSession(
        string ownedSaveName,
        string sessionId,
        int planetId,
        long minimumGameTick,
        OwnedWorldGameplayJournalCheckpoint gameplayJournalCheckpoint)
    {
        Arm(
            ownedSaveName,
            sessionId,
            planetId,
            minimumGameTick,
            gameplayJournalCheckpoint,
            quarantineActionId: string.Empty);
        _logger.LogInfo("Spherewright armed a one-time exact planned-restart ticket from a healthy owned save");
    }

    public void ArmFromQuarantinedOwnedSession(
        string ownedSaveName,
        string sessionId,
        int planetId,
        long minimumGameTick,
        string quarantineActionId,
        OwnedWorldGameplayJournalCheckpoint gameplayJournalCheckpoint)
    {
        if (string.IsNullOrWhiteSpace(quarantineActionId))
        {
            throw new InvalidOperationException("A quarantined owned session requires its exact action identity.");
        }

        Arm(ownedSaveName, sessionId, planetId, minimumGameTick, gameplayJournalCheckpoint, quarantineActionId);
        _logger.LogInfo("Spherewright armed a one-time exact quarantine-recovery ticket");
    }

    private void Arm(
        string ownedSaveName,
        string sessionId,
        int planetId,
        long minimumGameTick,
        OwnedWorldGameplayJournalCheckpoint gameplayJournalCheckpoint,
        string quarantineActionId)
    {
        if (string.IsNullOrWhiteSpace(ownedSaveName)
            || string.IsNullOrWhiteSpace(sessionId)
            || planetId <= 0
            || minimumGameTick < 0
            || gameplayJournalCheckpoint is null
            || gameplayJournalCheckpoint.Version != OwnedWorldGameplayJournalCheckpoint.CurrentVersion
            || !string.Equals(
                gameplayJournalCheckpoint.JournalId,
                GameplayJournalIdentity.HashOwnedSaveIdentity(ownedSaveName),
                StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(gameplayJournalCheckpoint.TrackingMode)
            || gameplayJournalCheckpoint.TrackingStartedAtGameTick < 0
            || gameplayJournalCheckpoint.MinimumDurableThroughSequence < 0)
        {
            throw new InvalidOperationException("A complete owned-session identity is required to arm restart-resume.");
        }

        var supersededTokens = CaptureReplicaTokens();
        var issuedAt = DateTimeOffset.UtcNow;
        var ticket = new OwnedWorldResumeTicket
        {
            Version = TicketVersion,
            ResumeToken = CreateToken(),
            OwnedSaveName = ownedSaveName,
            SourceSessionId = sessionId,
            SourceProcessId = Process.GetCurrentProcess().Id,
            SourceBridgeInstanceId = _bridgeInstanceId,
            GameVersion = _gameVersion,
            ExpectedPlanetId = planetId,
            MinimumGameTick = minimumGameTick,
            GameplayJournalCheckpoint = gameplayJournalCheckpoint,
            QuarantineActionId = quarantineActionId,
            IssuedAtUtc = issuedAt,
            ExpiresAtUtc = issuedAt.AddHours(24),
        };
        Persist(ticket);
        _currentTicket = ticket;
        _currentTicketPath = null;
        foreach (var supersededToken in supersededTokens.Where(token =>
                     !FixedTimeEquals(token, ticket.ResumeToken)))
        {
            if (!PersistConsumptionTombstone(supersededToken))
            {
                throw new IOException("A superseded resume-ticket generation could not be durably tombstoned.");
            }

            DeleteTicketReplicaIfMatching(_ticketPath, supersededToken);
            DeleteTicketReplicaIfMatching(_handoffTicketPath, supersededToken);
        }
    }

    public bool TryGetActiveTicket(
        string resumeToken,
        out OwnedWorldResumeTicket? ticket,
        out string rejection)
    {
        ticket = null;
        rejection = string.Empty;
        if (string.IsNullOrWhiteSpace(resumeToken))
        {
            rejection = "A resume token is required.";
            return false;
        }

        var candidate = _currentTicket ?? ReadFromDisk();
        if (candidate is null)
        {
            rejection = "No one-time owned-world resume ticket exists.";
            return false;
        }

        if (IsConsumed(candidate.ResumeToken)
            || candidate.Version != TicketVersion
            || !FixedTimeEquals(candidate.ResumeToken, resumeToken)
            || !string.Equals(candidate.GameVersion, _gameVersion, StringComparison.Ordinal)
            || candidate.ExpiresAtUtc <= DateTimeOffset.UtcNow
            || string.IsNullOrWhiteSpace(candidate.OwnedSaveName)
            || string.IsNullOrWhiteSpace(candidate.SourceSessionId)
            || candidate.ExpectedPlanetId <= 0
            || candidate.MinimumGameTick < 0)
        {
            rejection = "The one-time owned-world resume ticket is consumed, invalid, expired, or belongs to another game version.";
            return false;
        }

        if (!TryValidateGameplayJournalContinuity(candidate, out rejection))
        {
            return false;
        }

        _currentTicket = candidate;
        ticket = candidate;
        return true;
    }

    private bool TryValidateGameplayJournalContinuity(
        OwnedWorldResumeTicket ticket,
        out string rejection)
    {
        rejection = string.Empty;
        var checkpoint = ticket.GameplayJournalCheckpoint;
        if (checkpoint is null)
        {
            // Version-1 tickets issued before the continuity checkpoint was
            // introduced remain compatible. Every newly armed ticket carries
            // the checkpoint and therefore takes the strict path below.
            return true;
        }

        try
        {
            var identityHash = GameplayJournalIdentity.HashOwnedSaveIdentity(ticket.OwnedSaveName);
            if (checkpoint.Version != OwnedWorldGameplayJournalCheckpoint.CurrentVersion
                || !string.Equals(checkpoint.JournalId, identityHash, StringComparison.Ordinal))
            {
                rejection = "The protected gameplay journal checkpoint does not match the owned-world ticket.";
                return false;
            }

            var path = Path.Combine(_runtimeDirectory, "journals", $"gameplay-{identityHash}.json");
            if (!File.Exists(path))
            {
                rejection = "The protected gameplay journal required by this resume ticket is unavailable.";
                return false;
            }

            var document = PluginJson.Deserialize<GameplayJournalDocument>(File.ReadAllText(path));
            if (document is null
                || document.Version != 1
                || !string.Equals(document.OwnedSaveIdentityHash, identityHash, StringComparison.Ordinal)
                || !string.Equals(document.GameVersion, _gameVersion, StringComparison.Ordinal)
                || document.Entries is null
                || document.Entries.Any(entry => entry is null)
                || !GameplayJournalContinuityPolicy.MatchesCheckpoint(
                    checkpoint.JournalId,
                    checkpoint.TrackingMode,
                    checkpoint.HistoricalCoverageComplete,
                    checkpoint.TrackingStartedAtGameTick,
                    checkpoint.MinimumDurableThroughSequence,
                    document.JournalId,
                    document.TrackingMode,
                    document.HistoricalCoverageComplete,
                    document.TrackingStartedAtGameTick,
                    document.Entries.Select(entry => entry.Sequence).ToArray()))
            {
                rejection = "The protected gameplay journal is missing, truncated, or does not match this resume ticket.";
                return false;
            }

            return true;
        }
        catch (Exception exception) when (
            exception is IOException
            || exception is UnauthorizedAccessException
            || exception is Newtonsoft.Json.JsonException
            || exception is ArgumentException)
        {
            rejection = $"The protected gameplay journal continuity check failed ({exception.GetType().Name}).";
            return false;
        }
    }

    public void Consume(string resumeToken)
    {
        var current = _currentTicket ?? ReadFromDisk();
        if (current is null || !FixedTimeEquals(current.ResumeToken, resumeToken))
        {
            return;
        }

        if (!PersistConsumptionTombstone(resumeToken))
        {
            _logger.LogError("Spherewright did not consume the owned-world resume ticket because no durable tombstone could be written");
            return;
        }

        _currentTicket = null;
        _currentTicketPath = null;
        DeleteTicketReplicaIfMatching(_ticketPath, resumeToken);
        DeleteTicketReplicaIfMatching(_handoffTicketPath, resumeToken);
    }

    private IReadOnlyList<string> CaptureReplicaTokens()
    {
        var tokens = new List<string>();
        if (_currentTicket is not null)
        {
            tokens.Add(_currentTicket.ResumeToken);
        }

        var runtime = ReadFromPath(_ticketPath);
        var handoff = ReadFromPath(_handoffTicketPath);
        if (runtime is not null)
        {
            tokens.Add(runtime.ResumeToken);
        }

        if (handoff is not null)
        {
            tokens.Add(handoff.ResumeToken);
        }

        return tokens
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private void DeleteTicketReplicaIfMatching(string path, string resumeToken)
    {
        if (TicketPathMatchesToken(path, resumeToken))
        {
            DeleteTicketPath(path);
        }
    }

    private bool PersistConsumptionTombstone(string resumeToken)
    {
        if (string.IsNullOrWhiteSpace(resumeToken))
        {
            return false;
        }

        var tokenHash = HashToken(resumeToken);
        var tombstone = new OwnedWorldResumeConsumptionTombstone
        {
            Version = 1,
            ResumeTokenHash = tokenHash,
            GameVersion = _gameVersion,
            ConsumedAtUtc = DateTimeOffset.UtcNow,
        };
        var handoffDirectory = Path.GetDirectoryName(_handoffTicketPath)
            ?? throw new InvalidOperationException("The owned-world handoff directory is unavailable.");
        WindowsCurrentUserSecurity.EnsureSecureDirectory(_runtimeDirectory);
        WindowsCurrentUserSecurity.EnsureSecureDirectory(handoffDirectory);
        var runtimePersisted = TryPersistAtPath(
            GetTombstonePath(_runtimeDirectory, tokenHash),
            _runtimeDirectory,
            tombstone,
            "runtime consumption tombstone");
        var handoffPersisted = TryPersistAtPath(
            GetTombstonePath(handoffDirectory, tokenHash),
            handoffDirectory,
            tombstone,
            "handoff consumption tombstone");
        return runtimePersisted || handoffPersisted;
    }

    private void DeleteTicketPath(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            _logger.LogWarning($"Spherewright could not consume its owned-world resume ticket ({exception.GetType().Name})");
        }
    }

    private static bool TicketPathMatchesToken(string path, string resumeToken)
    {
        try
        {
            if (!File.Exists(path))
            {
                return false;
            }

            var candidate = PluginJson.Deserialize<OwnedWorldResumeTicket>(File.ReadAllText(path));
            return candidate is not null && FixedTimeEquals(candidate.ResumeToken, resumeToken);
        }
        catch (Exception exception) when (
            exception is IOException
            || exception is UnauthorizedAccessException
            || exception is Newtonsoft.Json.JsonException
            || exception is ArgumentException)
        {
            return false;
        }
    }

    private OwnedWorldResumeTicket? ReadFromDisk()
    {
        var runtimeTicket = ReadFromPath(_ticketPath);
        var handoffTicket = ReadFromPath(_handoffTicketPath);
        var candidates = new[]
            {
                new { Ticket = runtimeTicket, Path = _ticketPath, Priority = 1 },
                new { Ticket = handoffTicket, Path = _handoffTicketPath, Priority = 0 },
            }
            .Where(candidate => candidate.Ticket is not null && !IsConsumed(candidate.Ticket.ResumeToken))
            .OrderByDescending(candidate => candidate.Ticket!.IssuedAtUtc)
            .ThenByDescending(candidate => candidate.Priority)
            .ToArray();
        if (candidates.Length == 0)
        {
            _currentTicketPath = null;
            return null;
        }

        _currentTicketPath = candidates[0].Path;
        return candidates[0].Ticket;
    }

    private bool IsConsumed(string resumeToken)
    {
        if (string.IsNullOrWhiteSpace(resumeToken))
        {
            return false;
        }

        var tokenHash = HashToken(resumeToken);
        var handoffDirectory = Path.GetDirectoryName(_handoffTicketPath);
        return TombstoneMatches(GetTombstonePath(_runtimeDirectory, tokenHash), tokenHash)
            || (!string.IsNullOrWhiteSpace(handoffDirectory)
                && TombstoneMatches(GetTombstonePath(handoffDirectory!, tokenHash), tokenHash));
    }

    private bool TombstoneMatches(string path, string tokenHash)
    {
        try
        {
            if (!File.Exists(path))
            {
                return false;
            }

            var tombstone = PluginJson.Deserialize<OwnedWorldResumeConsumptionTombstone>(File.ReadAllText(path));
            return tombstone is not null
                && tombstone.Version == 1
                && string.Equals(tombstone.GameVersion, _gameVersion, StringComparison.Ordinal)
                && FixedTimeEquals(tombstone.ResumeTokenHash, tokenHash);
        }
        catch (Exception exception) when (
            exception is IOException
            || exception is UnauthorizedAccessException
            || exception is Newtonsoft.Json.JsonException
            || exception is ArgumentException)
        {
            _logger.LogWarning($"Spherewright could not read an owned-world resume consumption tombstone ({exception.GetType().Name})");
            return false;
        }
    }

    private OwnedWorldResumeTicket? ReadFromPath(string path)
    {
        try
        {
            string json;
            using (var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read))
            using (var reader = new StreamReader(stream, Encoding.UTF8, true))
            {
                json = reader.ReadToEnd();
            }

            var ticket = PluginJson.Deserialize<OwnedWorldResumeTicket>(json);
            if (ticket is null)
            {
                _logger.LogWarning("Spherewright ignored an empty owned-world resume ticket payload");
                return null;
            }

            _logger.LogInfo("Spherewright loaded an owned-world restart-resume ticket from the protected runtime directory");
            return ticket;
        }
        catch (FileNotFoundException)
        {
            _logger.LogInfo("Spherewright found no owned-world resume ticket at a fixed protected path");
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            _logger.LogInfo("Spherewright found no owned-world resume directory at the fixed protected path");
            return null;
        }
        catch (Exception exception) when (
            exception is IOException
            || exception is UnauthorizedAccessException
            || exception is Newtonsoft.Json.JsonException
            || exception is ArgumentException)
        {
            _logger.LogWarning($"Spherewright ignored an unreadable owned-world resume ticket ({exception.GetType().Name})");
            return null;
        }
    }

    private void Persist(OwnedWorldResumeTicket ticket)
    {
        WindowsCurrentUserSecurity.EnsureSecureDirectory(_runtimeDirectory);
        var handoffDirectory = Path.GetDirectoryName(_handoffTicketPath)
            ?? throw new InvalidOperationException("The owned-world handoff directory is unavailable.");
        WindowsCurrentUserSecurity.EnsureSecureDirectory(handoffDirectory);
        var runtimePersisted = TryPersistAtPath(
            _ticketPath,
            _runtimeDirectory,
            ticket,
            "runtime resume-ticket replica");
        var handoffPersisted = TryPersistAtPath(
            _handoffTicketPath,
            handoffDirectory,
            ticket,
            "handoff resume-ticket replica");
        if (!runtimePersisted && !handoffPersisted)
        {
            throw new IOException("No protected owned-world resume ticket replica could be persisted.");
        }

        if (!runtimePersisted || !handoffPersisted)
        {
            _logger.LogWarning("Spherewright armed the resume ticket with one durable replica; startup generation selection will prefer the newest surviving ticket");
        }
    }

    private static void PersistAtPath(
        string destinationPath,
        string directory,
        object payload)
    {
        var temporaryPath = Path.Combine(directory, $".owned-world-resume-{Guid.NewGuid():N}.tmp");
        var bytes = new UTF8Encoding(false).GetBytes(PluginJson.Serialize(payload));
        WindowsCurrentUserSecurity.WriteSecureNewFile(temporaryPath, bytes);
        try
        {
            if (File.Exists(destinationPath))
            {
                File.Replace(temporaryPath, destinationPath, null, true);
            }
            else
            {
                File.Move(temporaryPath, destinationPath);
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private bool TryPersistAtPath(
        string destinationPath,
        string directory,
        object payload,
        string replicaName)
    {
        try
        {
            PersistAtPath(destinationPath, directory, payload);
            return true;
        }
        catch (Exception exception) when (
            exception is IOException
            || exception is UnauthorizedAccessException
            || exception is ArgumentException)
        {
            _logger.LogWarning($"Spherewright could not persist its {replicaName} ({exception.GetType().Name})");
            return false;
        }
    }

    private static string GetTombstonePath(string directory, string tokenHash) =>
        Path.Combine(directory, $"owned-world-resume-consumed-{tokenHash}.json");

    private static string HashToken(string token)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token ?? string.Empty));
            return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
        }
    }

    private static string CreateToken()
    {
        var bytes = new byte[32];
        using (var random = RandomNumberGenerator.Create())
        {
            random.GetBytes(bytes);
        }

        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left ?? string.Empty);
        var rightBytes = Encoding.UTF8.GetBytes(right ?? string.Empty);
        var difference = leftBytes.Length ^ rightBytes.Length;
        var length = Math.Max(leftBytes.Length, rightBytes.Length);
        for (var index = 0; index < length; index++)
        {
            var leftValue = index < leftBytes.Length ? leftBytes[index] : (byte)0;
            var rightValue = index < rightBytes.Length ? rightBytes[index] : (byte)0;
            difference |= leftValue ^ rightValue;
        }

        return difference == 0;
    }
}

internal sealed class OwnedWorldResumeTicket
{
    public int Version { get; set; }

    public string ResumeToken { get; set; } = string.Empty;

    public string OwnedSaveName { get; set; } = string.Empty;

    public string SourceSessionId { get; set; } = string.Empty;

    public int SourceProcessId { get; set; }

    public string SourceBridgeInstanceId { get; set; } = string.Empty;

    public string GameVersion { get; set; } = string.Empty;

    public int ExpectedPlanetId { get; set; }

    public long MinimumGameTick { get; set; }

    public OwnedWorldGameplayJournalCheckpoint? GameplayJournalCheckpoint { get; set; }

    public string QuarantineActionId { get; set; } = string.Empty;

    public DateTimeOffset IssuedAtUtc { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }
}

internal sealed class OwnedWorldGameplayJournalCheckpoint
{
    public const int CurrentVersion = 1;

    public int Version { get; set; }

    public string JournalId { get; set; } = string.Empty;

    public string TrackingMode { get; set; } = string.Empty;

    public bool HistoricalCoverageComplete { get; set; }

    public long TrackingStartedAtGameTick { get; set; }

    public long MinimumDurableThroughSequence { get; set; }
}

internal sealed class OwnedWorldResumeConsumptionTombstone
{
    public int Version { get; set; }

    public string ResumeTokenHash { get; set; } = string.Empty;

    public string GameVersion { get; set; } = string.Empty;

    public DateTimeOffset ConsumedAtUtc { get; set; }
}
