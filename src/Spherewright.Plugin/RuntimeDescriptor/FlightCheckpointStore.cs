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
        _logger.LogInfo("Spherewright initialized the protected reusable flight-checkpoint store");
    }

    public FlightCheckpointTicket? CurrentTicket => IsStructurallyValid(_currentTicket)
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
            IssuedAtUtc = DateTimeOffset.UtcNow,
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
        if (!IsStructurallyValid(candidate)
            || candidate is null
            || !FixedTimeEquals(candidate.ReloadToken, reloadToken))
        {
            rejection = "The reusable flight-checkpoint ticket is missing, invalid, or belongs to another game version.";
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
        && !string.IsNullOrWhiteSpace(ticket.PlayerStateHash)
        && !string.IsNullOrWhiteSpace(ticket.StarSystemStateHash);

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
}
