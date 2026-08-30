using BepInEx.Logging;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Sessions;

namespace Spherewright.Plugin.Game;

internal sealed class GameSessionTracker
{
    private readonly bool _writesConfigured;
    private readonly string _gameVersion;
    private readonly ManualLogSource _logger;
    private GameData? _observedData;
    private GameData? _ownedData;
    private string? _expectedOwnedSaveName;
    private string? _ownedSaveName;
    private string? _sessionId;
    private int _lastPlanetId;
    private long _revision;
    private long _ownedSessionStartTick;
    private long? _lastOwnedSaveGameTick;
    private string _ownedSaveState = OwnedSaveStates.None;
    private string? _ownedSaveError;
    private string _writeHealth = WriteHealthStates.Healthy;
    private string? _writeQuarantineReason;

    public GameSessionTracker(bool writesConfigured, string gameVersion, ManualLogSource logger)
    {
        _writesConfigured = writesConfigured;
        _gameVersion = gameVersion;
        _logger = logger;
    }

    public bool GameLoaded { get; private set; }

    public bool IsCurrentSessionOwned =>
        GameLoaded
        && _ownedData is not null
        && ReferenceEquals(_ownedData, _observedData);

    public string? SessionId => _sessionId;

    public long Revision => _revision;

    public string WriteHealth => _writeHealth;

    public string? WriteQuarantineReason => _writeQuarantineReason;

    public void ExpectNextSessionToBeOwned(string saveName)
    {
        if (string.IsNullOrWhiteSpace(saveName))
        {
            throw new ArgumentException("An owned save name is required.", nameof(saveName));
        }

        if (((GameMain.data is not null || GameMain.isRunning) && !DSPGame.IsMenuDemo)
            || _expectedOwnedSaveName is not null)
        {
            throw new InvalidOperationException("An owned new world can only be armed from an idle main menu.");
        }

        _expectedOwnedSaveName = saveName;
        _ownedSaveState = OwnedSaveStates.WaitingForWorld;
        _ownedSaveError = null;
    }

    public void CancelExpectedOwnedSession()
    {
        _expectedOwnedSaveName = null;
        if (!IsCurrentSessionOwned)
        {
            _ownedSaveState = OwnedSaveStates.None;
            _ownedSaveError = null;
        }
    }

    public void UpdateOnMainThread()
    {
        var running = GameMain.isRunning && GameMain.data is not null;
        var currentData = running ? GameMain.data : null;
        if (!running || currentData is null)
        {
            if (GameLoaded || _observedData is not null)
            {
                _observedData = null;
                _ownedData = null;
                _ownedSaveName = null;
                _sessionId = null;
                _lastPlanetId = 0;
                _revision = 0;
                _lastOwnedSaveGameTick = null;
                _writeHealth = WriteHealthStates.Healthy;
                _writeQuarantineReason = null;
            }

            GameLoaded = false;
            return;
        }

        if (!GameLoaded || !ReferenceEquals(_observedData, currentData))
        {
            _observedData = currentData;
            _sessionId = Guid.NewGuid().ToString("D");
            _revision = 1;
            _writeHealth = WriteHealthStates.Healthy;
            _writeQuarantineReason = null;
            _lastPlanetId = 0;
            GameLoaded = true;

            if (_expectedOwnedSaveName is not null)
            {
                _ownedData = currentData;
                _ownedSaveName = _expectedOwnedSaveName;
                _expectedOwnedSaveName = null;
                _ownedSaveState = OwnedSaveStates.WaitingToSave;
                _ownedSessionStartTick = GameMain.gameTick;
                _lastOwnedSaveGameTick = null;
                _logger.LogInfo("Spherewright adopted the newly created ordinary peaceful world");
            }
            else
            {
                _ownedData = null;
                _ownedSaveName = null;
                _ownedSaveState = OwnedSaveStates.None;
                _ownedSaveError = null;
                _logger.LogWarning("Spherewright detected an unowned game session; save and factory reads are blocked");
            }
        }

        if (!IsCurrentSessionOwned)
        {
            return;
        }

        var localPlanetId = currentData.localPlanet?.id ?? 0;
        if (_lastPlanetId != 0 && localPlanetId != _lastPlanetId)
        {
            _revision++;
        }

        _lastPlanetId = localPlanetId;
        TrySaveOwnedWorldOnMainThread(currentData);
    }

    public SessionState CaptureOnMainThread()
    {
        UpdateOnMainThread();
        if (!GameLoaded || _observedData is null)
        {
            return new SessionState
            {
                BridgeConnected = true,
                GameLoaded = false,
                OwnedBySpherewright = false,
                AccessRestricted = false,
                GameVersion = _gameVersion,
                Revision = 0,
                PeacefulMode = PeacefulModeStates.Unknown,
                SandboxMode = SandboxModeStates.Unknown,
                WritesAllowed = false,
                WriteHealth = _writeHealth,
                OwnedSaveState = _ownedSaveState,
                OwnedSaveError = _ownedSaveError,
                Capabilities = new List<string> { "bridge.status", "new-game.create", "action.read" },
            };
        }

        if (!IsCurrentSessionOwned)
        {
            return new SessionState
            {
                BridgeConnected = true,
                GameLoaded = true,
                OwnedBySpherewright = false,
                AccessRestricted = true,
                GameVersion = _gameVersion,
                SessionId = _sessionId,
                Revision = _revision,
                PeacefulMode = PeacefulModeStates.Unknown,
                SandboxMode = SandboxModeStates.Unknown,
                WritesAllowed = false,
                WriteHealth = _writeHealth,
                OwnedSaveState = OwnedSaveStates.None,
                Capabilities = new List<string> { "bridge.status", "session.safe-status" },
            };
        }

        var descriptor = _observedData.gameDesc;
        var peacefulState = descriptor is null
            ? PeacefulModeStates.Unknown
            : descriptor.isPeaceMode
                ? PeacefulModeStates.ConfirmedPeaceful
                : PeacefulModeStates.ConfirmedCombat;
        var sandboxState = descriptor is null
            ? SandboxModeStates.Unknown
            : descriptor.isSandboxMode || GameMain.sandboxToolsEnabled
                ? SandboxModeStates.Enabled
                : SandboxModeStates.ConfirmedDisabled;
        var localPlanet = _observedData.localPlanet;
        var blockers = CreateWriteBlockers(descriptor, peacefulState, sandboxState);
        var writesAllowed = blockers.Count == 0;

        return new SessionState
        {
            BridgeConnected = true,
            GameLoaded = true,
            OwnedBySpherewright = true,
            AccessRestricted = false,
            GameVersion = _gameVersion,
            SessionId = _sessionId,
            SaveName = _ownedSaveName,
            GameTick = GameMain.gameTick,
            Revision = _revision,
            LocalPlanetId = localPlanet?.id,
            LocalPlanetName = localPlanet?.displayName,
            PeacefulMode = peacefulState,
            SandboxMode = sandboxState,
            ResourceMultiplier = descriptor?.resourceMultiplier,
            WritesAllowed = writesAllowed,
            WriteHealth = _writeHealth,
            WriteBlockers = blockers,
            OwnedSaveState = _ownedSaveState,
            OwnedSaveError = _ownedSaveError,
            LastOwnedSaveGameTick = _lastOwnedSaveGameTick,
            Capabilities = CreateOwnedCapabilities(writesAllowed),
        };
    }

    public void IncrementRevisionOnMainThread()
    {
        if (IsCurrentSessionOwned)
        {
            _revision++;
        }
    }

    private static List<string> CreateOwnedCapabilities(bool writesAllowed)
    {
        var capabilities = new List<string>
        {
            "bridge.status",
            "session.read",
            "player.read",
            "progression.read",
            "assembler.read",
            "build-catalog.read",
            "recipe-catalog.read",
            "resource.read",
            "factory.read",
            "power.read",
            "action.read",
        };
        if (writesAllowed)
        {
            capabilities.Add("normal-game.prepare");
        }

        return capabilities;
    }

    private void TrySaveOwnedWorldOnMainThread(GameData currentData)
    {
        if (!string.Equals(_ownedSaveState, OwnedSaveStates.WaitingToSave, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(_ownedSaveName)
            || currentData.localLoadedPlanetFactory is null
            || GameMain.gameTick < _ownedSessionStartTick + 30)
        {
            return;
        }

        TrySaveOwnedWorldNowOnMainThread(out _);
    }

    public bool TrySaveOwnedWorldNowOnMainThread(out string? error)
    {
        error = null;
        if (!IsCurrentSessionOwned
            || string.IsNullOrWhiteSpace(_ownedSaveName)
            || GameMain.data?.localLoadedPlanetFactory is null)
        {
            error = "The exact owned world or local factory is unavailable.";
            return false;
        }

        try
        {
            GameMain.gameName = _ownedSaveName;
            if (!GameSave.SaveCurrentGame(_ownedSaveName))
            {
                _ownedSaveState = OwnedSaveStates.SaveFailed;
                _ownedSaveError = "The game save API returned false.";
                error = _ownedSaveError;
                _logger.LogError("Spherewright could not save the owned ordinary world");
                return false;
            }

            _ownedSaveState = OwnedSaveStates.Saved;
            _ownedSaveError = null;
            _lastOwnedSaveGameTick = GameMain.gameTick;
            _logger.LogInfo("Spherewright saved the owned ordinary world");
            return true;
        }
        catch (Exception exception)
        {
            _ownedSaveState = OwnedSaveStates.SaveFailed;
            _ownedSaveError = exception.GetType().Name + ": " + exception.Message;
            error = _ownedSaveError;
            _logger.LogError($"Spherewright owned-world save failed: {_ownedSaveError}");
            return false;
        }
    }

    public void QuarantineWritesOnMainThread(string reason)
    {
        if (!IsCurrentSessionOwned || string.Equals(_writeHealth, WriteHealthStates.Quarantined, StringComparison.Ordinal))
        {
            return;
        }

        _writeHealth = WriteHealthStates.Quarantined;
        _writeQuarantineReason = string.IsNullOrWhiteSpace(reason) ? "A write outcome could not be proven." : reason;
        _revision++;
        _logger.LogError("Spherewright quarantined writes for the current owned session");
    }

    private List<WriteBlocker> CreateWriteBlockers(
        GameDesc? descriptor,
        string peacefulState,
        string sandboxState)
    {
        var blockers = new List<WriteBlocker>();
        if (string.Equals(_writeHealth, WriteHealthStates.Quarantined, StringComparison.Ordinal))
        {
            blockers.Add(new WriteBlocker
            {
                Code = BridgeErrorCodes.WriteSubsystemQuarantined,
                Message = _writeQuarantineReason ?? "The current session write subsystem is quarantined.",
            });
        }
        if (!_writesConfigured)
        {
            blockers.Add(new WriteBlocker
            {
                Code = BridgeErrorCodes.WritesDisabled,
                Message = "Writes are disabled by configuration.",
            });
        }

        if (string.Equals(peacefulState, PeacefulModeStates.Unknown, StringComparison.Ordinal))
        {
            blockers.Add(new WriteBlocker
            {
                Code = BridgeErrorCodes.PeacefulModeUnknown,
                Message = "Peaceful mode could not be confirmed.",
            });
        }
        else if (!string.Equals(peacefulState, PeacefulModeStates.ConfirmedPeaceful, StringComparison.Ordinal))
        {
            blockers.Add(new WriteBlocker
            {
                Code = BridgeErrorCodes.PeacefulModeRequired,
                Message = "M0 writes require a peaceful world.",
            });
        }

        if (string.Equals(sandboxState, SandboxModeStates.Unknown, StringComparison.Ordinal))
        {
            blockers.Add(new WriteBlocker
            {
                Code = BridgeErrorCodes.SandboxModeUnknown,
                Message = "Sandbox mode could not be confirmed disabled.",
            });
        }
        else if (!string.Equals(sandboxState, SandboxModeStates.ConfirmedDisabled, StringComparison.Ordinal))
        {
            blockers.Add(new WriteBlocker
            {
                Code = BridgeErrorCodes.SandboxModeActive,
                Message = "M0 writes are forbidden while DSP sandbox tools are active.",
            });
        }

        if (descriptor is not null && Math.Abs(descriptor.resourceMultiplier - 1f) > 0.0001f)
        {
            blockers.Add(new WriteBlocker
            {
                Code = BridgeErrorCodes.NormalResourceMultiplierRequired,
                Message = "M0 requires the normal 1x resource multiplier.",
            });
        }

        return blockers;
    }
}
