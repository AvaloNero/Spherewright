using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using BepInEx.Logging;
using Spherewright.Plugin.Security;
using Spherewright.Plugin.Transport;

namespace Spherewright.Plugin.RuntimeDescriptor;

internal sealed class FlightCheckpointStore
{
    private const int TicketVersion = 1;
    private static readonly TimeSpan TicketLifetime = TimeSpan.FromHours(24);
    private readonly string _ticketPath;
    private readonly string _bridgeInstanceId;
    private readonly string _gameVersion;
    private readonly ManualLogSource _logger;
    private FlightCheckpointTicket? _currentTicket;

    public FlightCheckpointStore(
        string bridgeInstanceId,
        string gameVersion,
        ManualLogSource logger)
    {
        var pluginDirectory = Path.GetDirectoryName(typeof(FlightCheckpointStore).Assembly.Location)
            ?? throw new InvalidOperationException("The Spherewright Plugin directory is unavailable.");
        var handoffDirectory = Path.Combine(pluginDirectory, "runtime-handoff");
        WindowsCurrentUserSecurity.EnsureSecureDirectory(handoffDirectory);
        _ticketPath = Path.Combine(handoffDirectory, "flight-checkpoint.json");
        _bridgeInstanceId = bridgeInstanceId;
        _gameVersion = gameVersion;
        _logger = logger;
        _currentTicket = ReadFromDisk();
        RetireIfCoveredByNewerPrimarySave();
        _logger.LogInfo("Spherewright initialized the protected reusable flight-checkpoint store");
    }

    public FlightCheckpointTicket? CurrentTicket => IsReloadEligible(_currentTicket)
        ? _currentTicket
        : null;

    public bool HasCurrentTicket => CurrentTicket is not null;

    public FlightCheckpointTicket CreateDraft(
        string ownedSaveName,
        string sourceSessionId,
        long sourceRevision,
        int originPlanetId,
        int destinationPlanetId,
        string playerStateHash,
        string starSystemStateHash)
    {
        if (string.IsNullOrWhiteSpace(ownedSaveName)
            || string.IsNullOrWhiteSpace(sourceSessionId)
            || sourceRevision < 1
            || originPlanetId <= 0
            || destinationPlanetId <= 0
            || originPlanetId == destinationPlanetId
            || string.IsNullOrWhiteSpace(playerStateHash)
            || string.IsNullOrWhiteSpace(starSystemStateHash))
        {
            throw new InvalidOperationException("A complete owned flight identity is required to create a checkpoint.");
        }

        var checkpointId = Guid.NewGuid().ToString("N");
        var issuedAt = DateTimeOffset.UtcNow;
        return new FlightCheckpointTicket
        {
            Version = TicketVersion,
            CheckpointId = checkpointId,
            ReloadToken = CreateToken(),
            CheckpointSaveName = "Spherewright_PreFlight_" + checkpointId,
            OwnedSaveName = ownedSaveName,
            SourceSessionId = sourceSessionId,
            SourceRevision = sourceRevision,
            SourceProcessId = Process.GetCurrentProcess().Id,
            SourceBridgeInstanceId = _bridgeInstanceId,
            GameVersion = _gameVersion,
            OriginPlanetId = originPlanetId,
            DestinationPlanetId = destinationPlanetId,
            PlayerStateHash = playerStateHash,
            StarSystemStateHash = starSystemStateHash,
            IssuedAtUtc = issuedAt,
            ExpiresAtUtc = issuedAt.Add(TicketLifetime),
            LifecycleState = FlightCheckpointLifecycleStates.Active,
        };
    }

    public void PersistCompletedCheckpoint(FlightCheckpointTicket ticket, long savedGameTick)
    {
        if (ticket is null || savedGameTick < 0)
        {
            throw new InvalidOperationException("A completed flight checkpoint and game tick are required.");
        }

        ticket.SavedGameTick = savedGameTick;
        if (!IsStructurallyValid(ticket))
        {
            throw new InvalidOperationException("The completed flight-checkpoint identity is invalid.");
        }

        Persist(ticket);
        _currentTicket = ticket;
        _logger.LogInfo("Spherewright persisted an exact reusable pre-flight checkpoint ticket");
    }

    public bool TryMarkAttemptStarted(
        string checkpointId,
        string actionId,
        long gameTick,
        out string rejection)
    {
        rejection = string.Empty;
        var ticket = _currentTicket;
        if (!IsReloadEligible(ticket)
            || ticket is null
            || !string.Equals(ticket.CheckpointId, checkpointId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(actionId)
            || gameTick < ticket.SavedGameTick)
        {
            rejection = "The flight attempt does not match the current reloadable checkpoint.";
            return false;
        }

        ticket.LifecycleState = FlightCheckpointLifecycleStates.Active;
        ticket.LastAttemptActionId = actionId;
        ticket.LastAttemptStartedAtGameTick = gameTick;
        ticket.RecoveryRequiredAtGameTick = null;
        ticket.SuccessfulFlightAtGameTick = null;
        return TryPersistLifecycle(ticket, "start the bound flight attempt", out rejection);
    }

    public bool TryMarkRecoveryRequired(
        string checkpointId,
        string actionId,
        long gameTick,
        out string rejection)
    {
        rejection = string.Empty;
        var ticket = _currentTicket;
        if (!IsStructurallyValid(ticket)
            || ticket is null
            || !string.Equals(ticket.CheckpointId, checkpointId, StringComparison.Ordinal)
            || IsSuccessfulOrRetired(ticket)
            || string.IsNullOrWhiteSpace(actionId)
            || gameTick < ticket.SavedGameTick)
        {
            rejection = "The failed flight does not match the current checkpoint lifecycle.";
            return false;
        }

        ticket.LifecycleState = FlightCheckpointLifecycleStates.RecoveryRequired;
        ticket.LastAttemptActionId = actionId;
        ticket.RecoveryRequiredAtGameTick = gameTick;
        return TryPersistLifecycle(ticket, "mark the bound flight as recovery-required", out rejection);
    }

    public bool TryMarkFlightSucceeded(
        string checkpointId,
        string actionId,
        long gameTick,
        out string rejection)
    {
        rejection = string.Empty;
        var ticket = _currentTicket;
        if (!IsStructurallyValid(ticket)
            || ticket is null
            || !string.Equals(ticket.CheckpointId, checkpointId, StringComparison.Ordinal)
            || IsSuccessfulOrRetired(ticket)
            || string.IsNullOrWhiteSpace(actionId)
            || gameTick < ticket.SavedGameTick)
        {
            rejection = "The successful flight does not match the current checkpoint lifecycle.";
            return false;
        }

        ticket.LifecycleState = FlightCheckpointLifecycleStates.FlightSucceeded;
        ticket.LastAttemptActionId = actionId;
        ticket.SuccessfulFlightAtGameTick = gameTick;
        ticket.RecoveryRequiredAtGameTick = null;
        return TryPersistLifecycle(ticket, "seal the successful flight before its primary save", out rejection);
    }

    public bool TryRetireAfterPrimarySave(
        string ownedSaveName,
        long savedGameTick,
        out bool retired,
        out string rejection)
    {
        retired = false;
        rejection = string.Empty;
        var ticket = _currentTicket;
        if (!IsStructurallyValid(ticket) || ticket is null)
        {
            return true;
        }

        if (!string.Equals(EffectiveLifecycle(ticket), FlightCheckpointLifecycleStates.FlightSucceeded, StringComparison.Ordinal))
        {
            return true;
        }

        if (!string.Equals(ticket.OwnedSaveName, ownedSaveName, StringComparison.Ordinal)
            || !ticket.SuccessfulFlightAtGameTick.HasValue
            || savedGameTick < ticket.SuccessfulFlightAtGameTick.Value)
        {
            rejection = "The primary save does not cover the successful flight checkpoint timeline.";
            return false;
        }

        ticket.LifecycleState = FlightCheckpointLifecycleStates.Retired;
        ticket.RetiredAtGameTick = savedGameTick;
        ticket.RetiredAtUtc = DateTimeOffset.UtcNow;
        if (!TryPersistLifecycle(ticket, "retire the checkpoint after the covering primary save", out rejection))
        {
            return false;
        }

        retired = true;
        _logger.LogInfo("Spherewright retired the successful flight checkpoint after a covering primary save");
        return true;
    }

    public bool TryGetActiveTicket(
        string reloadToken,
        out FlightCheckpointTicket? ticket,
        out string rejection)
    {
        ticket = null;
        rejection = string.Empty;
        if (string.IsNullOrWhiteSpace(reloadToken))
        {
            rejection = "A flight-checkpoint reload token is required.";
            return false;
        }

        var candidate = _currentTicket ?? ReadFromDisk();
        if (!IsReloadEligible(candidate)
            || candidate is null
            || !FixedTimeEquals(candidate.ReloadToken, reloadToken))
        {
            rejection = "The flight-checkpoint ticket is missing, expired, retired, already succeeded, or belongs to another game version.";
            return false;
        }

        _currentTicket = candidate;
        ticket = candidate;
        return true;
    }

    public bool TryValidateCheckpointFile(FlightCheckpointTicket ticket, out string rejection)
    {
        rejection = string.Empty;
        if (!IsStructurallyValid(ticket))
        {
            rejection = "The reusable flight-checkpoint ticket is invalid.";
            return false;
        }

        GameSave.ReadHeader(ticket.CheckpointSaveName, false, out var header);
        if (header is null || header.gameTick != ticket.SavedGameTick)
        {
            rejection = "The exact pre-flight save is missing or its saved game tick no longer matches the protected ticket.";
            return false;
        }

        return true;
    }

    private bool IsStructurallyValid(FlightCheckpointTicket? ticket) =>
        ticket is not null
        && ticket.Version == TicketVersion
        && string.Equals(ticket.GameVersion, _gameVersion, StringComparison.Ordinal)
        && !string.IsNullOrWhiteSpace(ticket.CheckpointId)
        && !string.IsNullOrWhiteSpace(ticket.ReloadToken)
        && !string.IsNullOrWhiteSpace(ticket.CheckpointSaveName)
        && ticket.CheckpointSaveName.StartsWith("Spherewright_PreFlight_", StringComparison.Ordinal)
        && ticket.CheckpointSaveName.IndexOfAny(new[] { '/', '\\', ':' }) < 0
        && !string.IsNullOrWhiteSpace(ticket.OwnedSaveName)
        && !string.IsNullOrWhiteSpace(ticket.SourceSessionId)
        && ticket.SourceRevision >= 1
        && ticket.OriginPlanetId > 0
        && ticket.DestinationPlanetId > 0
        && ticket.OriginPlanetId != ticket.DestinationPlanetId
        && ticket.SavedGameTick >= 0
        && ticket.IssuedAtUtc != default
        && (ticket.ExpiresAtUtc == default || ticket.ExpiresAtUtc > ticket.IssuedAtUtc)
        && IsKnownLifecycle(ticket.LifecycleState)
        && !string.IsNullOrWhiteSpace(ticket.PlayerStateHash)
        && !string.IsNullOrWhiteSpace(ticket.StarSystemStateHash);

    private bool IsReloadEligible(FlightCheckpointTicket? ticket) =>
        IsStructurallyValid(ticket)
        && ticket is not null
        && EffectiveExpiresAt(ticket) > DateTimeOffset.UtcNow
        && !IsSuccessfulOrRetired(ticket);

    internal static bool IsRecoveryRequired(FlightCheckpointTicket ticket) =>
        string.Equals(
            EffectiveLifecycle(ticket),
            FlightCheckpointLifecycleStates.RecoveryRequired,
            StringComparison.Ordinal);

    internal static bool IsAttemptInFlight(FlightCheckpointTicket ticket) =>
        string.Equals(
            EffectiveLifecycle(ticket),
            FlightCheckpointLifecycleStates.Active,
            StringComparison.Ordinal);

    private static bool IsSuccessfulOrRetired(FlightCheckpointTicket ticket)
    {
        var lifecycle = EffectiveLifecycle(ticket);
        return string.Equals(lifecycle, FlightCheckpointLifecycleStates.FlightSucceeded, StringComparison.Ordinal)
            || string.Equals(lifecycle, FlightCheckpointLifecycleStates.Retired, StringComparison.Ordinal);
    }

    private static bool IsKnownLifecycle(string lifecycle) =>
        string.IsNullOrWhiteSpace(lifecycle)
        || string.Equals(lifecycle, FlightCheckpointLifecycleStates.Active, StringComparison.Ordinal)
        || string.Equals(lifecycle, FlightCheckpointLifecycleStates.RecoveryRequired, StringComparison.Ordinal)
        || string.Equals(lifecycle, FlightCheckpointLifecycleStates.FlightSucceeded, StringComparison.Ordinal)
        || string.Equals(lifecycle, FlightCheckpointLifecycleStates.Retired, StringComparison.Ordinal);

    private static string EffectiveLifecycle(FlightCheckpointTicket ticket) =>
        string.IsNullOrWhiteSpace(ticket.LifecycleState)
            ? FlightCheckpointLifecycleStates.Active
            : ticket.LifecycleState;

    private static DateTimeOffset EffectiveExpiresAt(FlightCheckpointTicket ticket) =>
        ticket.ExpiresAtUtc == default
            ? ticket.IssuedAtUtc.Add(TicketLifetime)
            : ticket.ExpiresAtUtc;

    private bool TryPersistLifecycle(
        FlightCheckpointTicket ticket,
        string operation,
        out string rejection)
    {
        _currentTicket = ticket;
        try
        {
            Persist(ticket);
            rejection = string.Empty;
            return true;
        }
        catch (Exception exception) when (
            exception is IOException
            || exception is UnauthorizedAccessException
            || exception is ArgumentException)
        {
            rejection = $"Spherewright could not {operation} ({exception.GetType().Name}).";
            _logger.LogError(rejection);
            return false;
        }
    }

    private FlightCheckpointTicket? ReadFromDisk()
    {
        try
        {
            string json;
            using (var stream = new FileStream(_ticketPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(stream, Encoding.UTF8, true))
            {
                json = reader.ReadToEnd();
            }

            var ticket = PluginJson.Deserialize<FlightCheckpointTicket>(json);
            if (!IsStructurallyValid(ticket))
            {
                _logger.LogWarning("Spherewright ignored an invalid reusable flight-checkpoint ticket");
                return null;
            }

            _logger.LogInfo("Spherewright loaded a reusable flight-checkpoint ticket from the protected handoff directory");
            return ticket;
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
        catch (Exception exception) when (
            exception is IOException
            || exception is UnauthorizedAccessException
            || exception is Newtonsoft.Json.JsonException
            || exception is ArgumentException)
        {
            _logger.LogWarning($"Spherewright ignored an unreadable flight-checkpoint ticket ({exception.GetType().Name})");
            return null;
        }
    }

    private void RetireIfCoveredByNewerPrimarySave()
    {
        var ticket = _currentTicket;
        if (!IsStructurallyValid(ticket)
            || ticket is null
            || string.Equals(EffectiveLifecycle(ticket), FlightCheckpointLifecycleStates.Retired, StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            GameSave.ReadHeader(ticket.OwnedSaveName, false, out var header);
            if (header is null || header.gameTick <= ticket.SavedGameTick)
            {
                return;
            }

            ticket.LifecycleState = FlightCheckpointLifecycleStates.Retired;
            ticket.RetiredAtGameTick = header.gameTick;
            ticket.RetiredAtUtc = DateTimeOffset.UtcNow;
            if (TryPersistLifecycle(
                    ticket,
                    "retire a checkpoint superseded by a newer exact primary save",
                    out _))
            {
                _logger.LogInfo("Spherewright retired a flight checkpoint whose exact primary save already covered a newer timeline");
            }
        }
        catch (Exception exception) when (
            exception is IOException
            || exception is UnauthorizedAccessException
            || exception is ArgumentException)
        {
            _logger.LogWarning($"Spherewright could not compare the exact primary save with its flight checkpoint ({exception.GetType().Name})");
        }
    }

    private void Persist(FlightCheckpointTicket ticket)
    {
        var directory = Path.GetDirectoryName(_ticketPath)
            ?? throw new InvalidOperationException("The flight-checkpoint handoff directory is unavailable.");
        WindowsCurrentUserSecurity.EnsureSecureDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".flight-checkpoint-{Guid.NewGuid():N}.tmp");
        var bytes = new UTF8Encoding(false).GetBytes(PluginJson.Serialize(ticket));
        WindowsCurrentUserSecurity.WriteSecureNewFile(temporaryPath, bytes);
        try
        {
            if (File.Exists(_ticketPath))
            {
                File.Replace(temporaryPath, _ticketPath, null, true);
            }
            else
            {
                File.Move(temporaryPath, _ticketPath);
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

internal sealed class FlightCheckpointTicket
{
    public int Version { get; set; }

    public string CheckpointId { get; set; } = string.Empty;

    public string ReloadToken { get; set; } = string.Empty;

    public string CheckpointSaveName { get; set; } = string.Empty;

    public string OwnedSaveName { get; set; } = string.Empty;

    public string SourceSessionId { get; set; } = string.Empty;

    public long SourceRevision { get; set; }

    public int SourceProcessId { get; set; }

    public string SourceBridgeInstanceId { get; set; } = string.Empty;

    public string GameVersion { get; set; } = string.Empty;

    public int OriginPlanetId { get; set; }

    public int DestinationPlanetId { get; set; }

    public long SavedGameTick { get; set; }

    public string PlayerStateHash { get; set; } = string.Empty;

    public string StarSystemStateHash { get; set; } = string.Empty;

    public DateTimeOffset IssuedAtUtc { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public string LifecycleState { get; set; } = string.Empty;

    public string LastAttemptActionId { get; set; } = string.Empty;

    public long? LastAttemptStartedAtGameTick { get; set; }

    public long? RecoveryRequiredAtGameTick { get; set; }

    public long? SuccessfulFlightAtGameTick { get; set; }

    public long? RetiredAtGameTick { get; set; }

    public DateTimeOffset? RetiredAtUtc { get; set; }
}

internal static class FlightCheckpointLifecycleStates
{
    public const string Active = "active";
    public const string RecoveryRequired = "recovery_required";
    public const string FlightSucceeded = "flight_succeeded";
    public const string Retired = "retired";
}
