using System.ComponentModel;
using System.Text;
using BepInEx.Logging;
using Spherewright.Bridge.Core.Factory;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Plugin.RuntimeDescriptor;
using Spherewright.Plugin.Security;
using Spherewright.Plugin.Transport;

namespace Spherewright.Plugin.Game;

// Reuses the bounded per-owned ACL/flush/atomic-replace pattern of BlueprintBuildStore.
// Only immutable declarations are durable. Measurement windows never cross a restart.
internal sealed class GovernorDeclarationStore
{
    private const int MaximumBytes = 64 * 1024;
    private readonly string _directory;
    private readonly string _gameVersion;
    private readonly GameSessionTracker _sessions;
    private readonly ManualLogSource _logger;
    private string? _sessionId;
    private string? _path;
    private GovernorDeclarationArchive? _document;
    private bool _failed;

    public GovernorDeclarationStore(string runtimeDirectory, string gameVersion, GameSessionTracker sessions, ManualLogSource logger)
    {
        _directory = Path.Combine(RuntimeDescriptorPublisher.ResolveRuntimeDirectory(runtimeDirectory), "governor");
        _gameVersion = gameVersion; _sessions = sessions; _logger = logger;
    }

    public GovernorThroughputValidation? ReadForProtectedResume(string baselineHash, long gameTick)
    {
        if (!Attach()) throw Unavailable();
        var saved = _document!.Declarations.SingleOrDefault(d => d.BaselineProposalHash == baselineHash);
        if (saved is null) return null;
        return GovernorThroughputValidation.Restore(saved, _document.IdentityHash, _gameVersion,
            _sessions.SessionId!, _sessions.ConfirmedPlannedResumeMinimumGameTick, gameTick);
    }

    public bool TryPut(GovernorThroughputValidation validation)
    {
        if (!Attach()) return false;
        string? temporaryPath = null;
        try
        {
            var saved = validation.CreateCheckpoint(_document!.IdentityHash, _gameVersion);
            var proposed = Clone(_document);
            proposed.AddLockedDeclaration(saved);
            var json = PluginJson.Serialize(proposed);
            var bytes = new UTF8Encoding(false).GetBytes(json);
            if (bytes.Length > MaximumBytes) throw new InvalidDataException("Governor declaration size limit.");
            var verified = PluginJson.Deserialize<GovernorDeclarationArchive>(json);
            if (verified is null) throw new InvalidDataException("Governor declaration round-trip is missing.");
            verified.Validate(_document.IdentityHash, _gameVersion);
            WindowsCurrentUserSecurity.EnsureSecureDirectory(_directory);
            temporaryPath = Path.Combine(_directory, ".declarations-" + Guid.NewGuid().ToString("N") + ".tmp");
            WindowsCurrentUserSecurity.WriteSecureNewFile(temporaryPath, bytes);
            if (File.Exists(_path!)) File.Replace(temporaryPath, _path!, null, true);
            else File.Move(temporaryPath, _path!);
            _document = verified; // Publish only the exact round-tripped, validated durable data.
            return true;
        }
        catch (Exception exception) when (IsStorageError(exception))
        {
            _failed = true;
            _logger.LogError("Spherewright Governor declaration persistence failed (" + exception.GetType().Name + ")");
            return false;
        }
        finally
        {
            try { if (temporaryPath is not null && File.Exists(temporaryPath)) File.Delete(temporaryPath); }
            catch (Exception exception) when (IsStorageError(exception))
            { _logger.LogWarning("Spherewright Governor temporary-file cleanup failed (" + exception.GetType().Name + ")"); }
        }
    }

    private bool Attach()
    {
        var current = _sessions.CaptureOnMainThread();
        if (!current.OwnedBySpherewright || !current.WritesAllowed || string.IsNullOrEmpty(_sessions.OwnedSaveName)
            || string.IsNullOrEmpty(_sessions.SessionId)) return false;
        if (_sessionId == _sessions.SessionId) return !_failed && _document is not null;
        _sessionId = _sessions.SessionId; _document = null; _path = null; _failed = false;
        try
        {
            WindowsCurrentUserSecurity.EnsureSecureDirectory(_directory);
            var identity = CanonicalStateHash.Combine("spherewright-governor-owned-v1", _sessions.OwnedSaveName).Substring(7);
            _path = Path.Combine(_directory, "declarations-" + identity + ".json");
            if (File.Exists(_path))
            {
                if (new FileInfo(_path).Length > MaximumBytes) throw new InvalidDataException("Governor declaration size limit.");
                _document = PluginJson.Deserialize<GovernorDeclarationArchive>(File.ReadAllText(_path));
                if (_document is null) throw new InvalidDataException("Governor declaration archive is missing.");
                _document.Validate(identity, _gameVersion);
            }
            else _document = new GovernorDeclarationArchive { IdentityHash = identity, GameVersion = _gameVersion };
            return true;
        }
        catch (Exception exception) when (IsStorageError(exception))
        {
            _failed = true; _document = null;
            _logger.LogError("Spherewright Governor declaration attachment failed (" + exception.GetType().Name + ")");
            return false;
        }
    }

    private static GovernorDeclarationArchive Clone(GovernorDeclarationArchive value) =>
        PluginJson.Deserialize<GovernorDeclarationArchive>(PluginJson.Serialize(value))!;
    private static FoundryPlanningException Unavailable() => new FoundryPlanningException(
        "governor_validation_persistence_unavailable", "Protected declaration storage is unavailable; do not expand using an unconfirmed lock.");
    private static bool IsStorageError(Exception ex) => ex is IOException || ex is UnauthorizedAccessException
        || ex is InvalidDataException || ex is Newtonsoft.Json.JsonException || ex is ArgumentException
        || ex is ArithmeticException || ex is Win32Exception || ex is InvalidOperationException || ex is FoundryPlanningException;
}
