using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using BepInEx.Logging;
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
        long minimumGameTick)
    {
        Arm(
            ownedSaveName,
            sessionId,
            planetId,
            minimumGameTick,
            quarantineActionId: string.Empty);
        _logger.LogInfo("Spherewright armed a one-time exact planned-restart ticket from a healthy owned save");
    }

    public void ArmFromQuarantinedOwnedSession(
        string ownedSaveName,
        string sessionId,
        int planetId,
        long minimumGameTick,
        string quarantineActionId)
    {
        if (string.IsNullOrWhiteSpace(quarantineActionId))
        {
            throw new InvalidOperationException("A quarantined owned session requires its exact action identity.");
        }

        Arm(ownedSaveName, sessionId, planetId, minimumGameTick, quarantineActionId);
        _logger.LogInfo("Spherewright armed a one-time exact quarantine-recovery ticket");
    }

    private void Arm(
        string ownedSaveName,
        string sessionId,
        int planetId,
        long minimumGameTick,
        string quarantineActionId)
    {
        if (string.IsNullOrWhiteSpace(ownedSaveName)
            || string.IsNullOrWhiteSpace(sessionId)
            || planetId <= 0
            || minimumGameTick < 0)
        {
            throw new InvalidOperationException("A complete owned-session identity is required to arm restart-resume.");
        }

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
            QuarantineActionId = quarantineActionId,
            IssuedAtUtc = issuedAt,
            ExpiresAtUtc = issuedAt.AddHours(24),
        };
        Persist(ticket);
        _currentTicket = ticket;
        _currentTicketPath = _ticketPath;
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

        if (candidate.Version != TicketVersion
            || !FixedTimeEquals(candidate.ResumeToken, resumeToken)
            || !string.Equals(candidate.GameVersion, _gameVersion, StringComparison.Ordinal)
            || candidate.ExpiresAtUtc <= DateTimeOffset.UtcNow
            || string.IsNullOrWhiteSpace(candidate.OwnedSaveName)
            || string.IsNullOrWhiteSpace(candidate.SourceSessionId)
            || candidate.ExpectedPlanetId <= 0
            || candidate.MinimumGameTick < 0)
        {
            rejection = "The one-time owned-world resume ticket is invalid, expired, or belongs to another game version.";
            return false;
        }

        _currentTicket = candidate;
        ticket = candidate;
        return true;
    }

    public void Consume(string resumeToken)
    {
        var current = _currentTicket;
        if (current is not null && !FixedTimeEquals(current.ResumeToken, resumeToken))
        {
            return;
        }

        _currentTicket = null;
        var consumedPath = _currentTicketPath ?? _ticketPath;
        _currentTicketPath = null;
        DeleteTicketPath(consumedPath);
        if (!string.Equals(consumedPath, _ticketPath, StringComparison.OrdinalIgnoreCase)
            && TicketPathMatchesToken(_ticketPath, resumeToken))
        {
            DeleteTicketPath(_ticketPath);
        }

        if (!string.Equals(consumedPath, _handoffTicketPath, StringComparison.OrdinalIgnoreCase)
            && TicketPathMatchesToken(_handoffTicketPath, resumeToken))
        {
            DeleteTicketPath(_handoffTicketPath);
        }
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
        var ticket = ReadFromPath(_ticketPath);
        if (ticket is not null)
        {
            _currentTicketPath = _ticketPath;
            return ticket;
        }

        ticket = ReadFromPath(_handoffTicketPath);
        if (ticket is not null)
        {
            _currentTicketPath = _handoffTicketPath;
        }

        return ticket;
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
        PersistAtPath(_ticketPath, _runtimeDirectory, ticket);
        var handoffDirectory = Path.GetDirectoryName(_handoffTicketPath)
            ?? throw new InvalidOperationException("The owned-world handoff directory is unavailable.");
        WindowsCurrentUserSecurity.EnsureSecureDirectory(handoffDirectory);
        PersistAtPath(_handoffTicketPath, handoffDirectory, ticket);
    }

    private static void PersistAtPath(
        string destinationPath,
        string directory,
        OwnedWorldResumeTicket ticket)
    {
        var temporaryPath = Path.Combine(directory, $".owned-world-resume-{Guid.NewGuid():N}.tmp");
        var bytes = new UTF8Encoding(false).GetBytes(PluginJson.Serialize(ticket));
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

    public string QuarantineActionId { get; set; } = string.Empty;

    public DateTimeOffset IssuedAtUtc { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }
}
