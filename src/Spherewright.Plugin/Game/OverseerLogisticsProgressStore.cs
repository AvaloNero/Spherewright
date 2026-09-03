using System.Security.Cryptography;
using System.Text;
using System.ComponentModel;
using BepInEx.Logging;
using Spherewright.Bridge.Core.Diagnostics;
using Spherewright.Plugin.RuntimeDescriptor;
using Spherewright.Plugin.Security;
using Spherewright.Plugin.Transport;

namespace Spherewright.Plugin.Game;

internal sealed class OverseerLogisticsProgressStore : IDisposable
{
    private const int DocumentVersion = 1;
    private const int MaximumRouteCount = 4096;
    private const long MaximumDocumentBytes = 16L * 1024L * 1024L;
    private readonly string _directory;
    private readonly string _gameVersion;
    private readonly GameSessionTracker _sessions;
    private readonly ManualLogSource _logger;
    private string? _activeSessionId;
    private string? _activePath;
    private OverseerLogisticsProgressDocument? _document;

    public OverseerLogisticsProgressStore(
        string configuredRuntimeDirectory,
        string gameVersion,
        GameSessionTracker sessions,
        ManualLogSource logger)
    {
        var runtimeDirectory = RuntimeDescriptorPublisher.ResolveRuntimeDirectory(configuredRuntimeDirectory);
        _directory = Path.Combine(runtimeDirectory, "overseer");
        _gameVersion = gameVersion;
        _sessions = sessions;
        _logger = logger;
    }

    public bool TryObserveBatchOnMainThread(
        IReadOnlyList<LogisticsProgressSample> observations,
        out IReadOnlyDictionary<string, LogisticsProgressWindowAnalysis>? analyses)
    {
        analyses = null;
        if (observations is null)
        {
            throw new ArgumentNullException(nameof(observations));
        }

        if (observations.Count == 0)
        {
            analyses = new Dictionary<string, LogisticsProgressWindowAnalysis>(StringComparer.Ordinal);
            return true;
        }

        if (!EnsureAttachedToCurrentOwnedSession()
            || _document is null
            || string.IsNullOrWhiteSpace(_activePath))
        {
            return false;
        }

        try
        {
            if (observations.Count > MaximumRouteCount
                || observations.Any(observation => observation is null
                    || string.IsNullOrWhiteSpace(observation.RouteKey)
                    || observation.RouteKey.Length > 128)
                || observations.Select(observation => observation.RouteKey)
                    .Distinct(StringComparer.Ordinal)
                    .Count() != observations.Count)
            {
                throw new ArgumentException(
                    "A bounded batch of unique logistics route observations is required.",
                    nameof(observations));
            }

            var proposedRoutes = _document.Routes.ToDictionary(
                route => route.RouteKey,
                route => route,
                StringComparer.Ordinal);
            var proposedAnalyses = new Dictionary<string, LogisticsProgressWindowAnalysis>(
                StringComparer.Ordinal);
            foreach (var observation in observations)
            {
                var current = CloneObservation(
                    observation,
                    _document.OwnedSaveIdentityHash,
                    _sessions.SessionId!);
                proposedRoutes.TryGetValue(current.RouteKey, out var previous);
                var analysis = LogisticsProgressWindowAnalyzer.Analyze(previous, current);
                proposedRoutes[current.RouteKey] = analysis.NextState;
                proposedAnalyses.Add(current.RouteKey, analysis);
            }

            var observedRouteKeys = new HashSet<string>(
                proposedAnalyses.Keys,
                StringComparer.Ordinal);
            while (proposedRoutes.Count > MaximumRouteCount)
            {
                var oldest = proposedRoutes.Values
                    .OrderBy(route => observedRouteKeys.Contains(route.RouteKey))
                    .ThenBy(route => route.LastSample.GameTick)
                    .ThenBy(route => route.RouteKey, StringComparer.Ordinal)
                    .First();
                proposedRoutes.Remove(oldest.RouteKey);
            }

            var orderedRoutes = proposedRoutes.Values
                .OrderBy(route => route.RouteKey, StringComparer.Ordinal)
                .ToList();
            var proposed = new OverseerLogisticsProgressDocument
            {
                Version = _document.Version,
                OwnedSaveIdentityHash = _document.OwnedSaveIdentityHash,
                GameVersion = _document.GameVersion,
                Routes = orderedRoutes,
            };
            Persist(proposed, _activePath!);
            _document = proposed;
            analyses = proposedAnalyses;
            return true;
        }
        catch (Exception exception) when (
            exception is IOException
            || exception is UnauthorizedAccessException
            || exception is InvalidDataException
            || exception is Newtonsoft.Json.JsonException
            || exception is ArgumentException
            || exception is ArithmeticException
            || exception is Win32Exception)
        {
            analyses = null;
            _logger.LogError($"Spherewright Overseer logistics-window observation batch failed ({exception.GetType().Name})");
            return false;
        }
    }

    public void Dispose()
    {
        _activeSessionId = null;
        _activePath = null;
        _document = null;
    }

    private bool EnsureAttachedToCurrentOwnedSession()
    {
        if (!_sessions.IsCurrentSessionOwned
            || string.IsNullOrWhiteSpace(_sessions.SessionId)
            || string.IsNullOrWhiteSpace(_sessions.OwnedSaveName))
        {
            Dispose();
            return false;
        }

        if (string.Equals(_activeSessionId, _sessions.SessionId, StringComparison.Ordinal)
            && _document is not null)
        {
            return true;
        }

        Dispose();
        var sessionId = _sessions.SessionId!;
        try
        {
            WindowsCurrentUserSecurity.EnsureSecureDirectory(_directory);
            var identityHash = HashOwnedIdentity(_sessions.OwnedSaveName!);
            var path = Path.Combine(_directory, $"logistics-{identityHash}.json");
            OverseerLogisticsProgressDocument? document = null;
            if (File.Exists(path))
            {
                var length = new FileInfo(path).Length;
                if (length < 0 || length > MaximumDocumentBytes)
                {
                    throw new InvalidDataException("The protected logistics-window document exceeds its size bound.");
                }

                document = PluginJson.Deserialize<OverseerLogisticsProgressDocument>(File.ReadAllText(path));
                ValidateDocument(document, identityHash);
            }

            _activeSessionId = sessionId;
            _activePath = path;
            _document = document ?? new OverseerLogisticsProgressDocument
            {
                Version = DocumentVersion,
                OwnedSaveIdentityHash = identityHash,
                GameVersion = _gameVersion,
            };
            _logger.LogInfo("Spherewright attached the protected per-save Overseer logistics window");
            return true;
        }
        catch (Exception exception) when (
            exception is IOException
            || exception is UnauthorizedAccessException
            || exception is InvalidDataException
            || exception is Newtonsoft.Json.JsonException
            || exception is ArgumentException
            || exception is ArithmeticException
            || exception is Win32Exception)
        {
            _activeSessionId = sessionId;
            _activePath = null;
            _document = null;
            _logger.LogError($"Spherewright Overseer logistics-window attachment failed ({exception.GetType().Name})");
            return false;
        }
    }

    private void ValidateDocument(OverseerLogisticsProgressDocument? document, string identityHash)
    {
        if (document is null
            || document.Version != DocumentVersion
            || !string.Equals(document.OwnedSaveIdentityHash, identityHash, StringComparison.Ordinal)
            || !string.Equals(document.GameVersion, _gameVersion, StringComparison.Ordinal)
            || document.Routes is null
            || document.Routes.Count > MaximumRouteCount
            || document.Routes.Any(route => route is null)
            || document.Routes.Select(route => route.RouteKey).Distinct(StringComparer.Ordinal).Count()
                != document.Routes.Count)
        {
            throw new InvalidDataException("The protected logistics-window document has an invalid identity or bound.");
        }

        foreach (var route in document.Routes)
        {
            if (string.IsNullOrWhiteSpace(route.RouteKey)
                || route.RouteKey.Length > 128
                || !string.Equals(route.ProtectedSaveKey, identityHash, StringComparison.Ordinal))
            {
                throw new InvalidDataException("A protected logistics-window route has an invalid identity.");
            }

            // An exact same-tick re-analysis performs the Core validator without
            // advancing or weakening the persisted stagnant baseline.
            LogisticsProgressWindowAnalyzer.Analyze(route, route.LastSample);
        }
    }

    private void Persist(OverseerLogisticsProgressDocument document, string destinationPath)
    {
        WindowsCurrentUserSecurity.EnsureSecureDirectory(_directory);
        var temporaryPath = Path.Combine(_directory, $".logistics-{Guid.NewGuid():N}.tmp");
        try
        {
            var bytes = new UTF8Encoding(false).GetBytes(PluginJson.Serialize(document));
            if (bytes.LongLength > MaximumDocumentBytes)
            {
                throw new InvalidDataException("The protected logistics-window document exceeds its size bound.");
            }

            WindowsCurrentUserSecurity.WriteSecureNewFile(temporaryPath, bytes);
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
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                _logger.LogWarning($"Spherewright could not remove a logistics-window temporary file ({exception.GetType().Name})");
            }
        }
    }

    private static LogisticsProgressSample CloneObservation(
        LogisticsProgressSample observation,
        string protectedSaveKey,
        string sessionId)
    {
        return new LogisticsProgressSample
        {
            ProtectedSaveKey = protectedSaveKey,
            RouteKey = observation.RouteKey,
            SessionId = sessionId,
            GameTick = observation.GameTick,
            CapturedAtUtc = observation.CapturedAtUtc,
            OrderOutstanding = observation.OrderOutstanding,
            OutstandingOrderMagnitude = observation.OutstandingOrderMagnitude,
            ConsumerInputMissing = observation.ConsumerInputMissing,
            DemandInventoryCount = observation.DemandInventoryCount,
            SourceInventoryCount = observation.SourceInventoryCount,
            CarrierFleetCount = observation.CarrierFleetCount,
            ActiveRouteCarrierCount = observation.ActiveRouteCarrierCount,
            CarrierProgressFingerprint = observation.CarrierProgressFingerprint,
        };
    }

    private static string HashOwnedIdentity(string ownedSaveName)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(
                $"spherewright-overseer-logistics-window-v1\n{ownedSaveName}"));
            return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}

internal sealed class OverseerLogisticsProgressDocument
{
    public int Version { get; set; }

    public string OwnedSaveIdentityHash { get; set; } = string.Empty;

    public string GameVersion { get; set; } = string.Empty;

    public List<LogisticsProgressWindowState> Routes { get; set; } =
        new List<LogisticsProgressWindowState>();
}
