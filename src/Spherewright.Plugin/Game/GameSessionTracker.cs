using BepInEx.Logging;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Sessions;
using Spherewright.Plugin.RuntimeDescriptor;

namespace Spherewright.Plugin.Game;

internal sealed class GameSessionTracker
{
    private readonly bool _writesConfigured;
    private readonly bool _userSaveImportConfigured;
    private readonly string _gameVersion;
    private readonly ManualLogSource _logger;
    private readonly OwnedWorldResumeTicketStore _resumeTickets;
    private readonly FlightCheckpointStore _flightCheckpoints;
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
    private string? _writeQuarantineActionId;
    private string? _writeQuarantineReason;
    private OwnedWorldResumeTicket? _expectedResumeTicket;
    private string? _resumeAdoptionError;
    private FlightCheckpointTicket? _expectedFlightCheckpoint;
    private string? _currentFlightCheckpointId;
    private bool _currentSessionLoadedFromFlightCheckpoint;
    private string? _flightCheckpointAdoptionError;

    public GameSessionTracker(
        bool writesConfigured,
        bool userSaveImportConfigured,
        string gameVersion,
        OwnedWorldResumeTicketStore resumeTickets,
        FlightCheckpointStore flightCheckpoints,
        ManualLogSource logger)
    {
        _writesConfigured = writesConfigured;
        _userSaveImportConfigured = userSaveImportConfigured;
        _gameVersion = gameVersion;
        _resumeTickets = resumeTickets;
        _flightCheckpoints = flightCheckpoints;
        _logger = logger;
    }

    public bool GameLoaded { get; private set; }

    public bool IsCurrentSessionOwned =>
        GameLoaded
        && _ownedData is not null
        && ReferenceEquals(_ownedData, _observedData);

    public string? SessionId => _sessionId;

    public string? OwnedSaveName => IsCurrentSessionOwned ? _ownedSaveName : null;

    public bool CurrentOwnedSessionStartedAsNewGame { get; private set; }

    public long Revision => _revision;

    public string WriteHealth => _writeHealth;

    public string? WriteQuarantineActionId => _writeQuarantineActionId;

    public string? WriteQuarantineReason => _writeQuarantineReason;

    public string? ResumeAdoptionError => _resumeAdoptionError;

    public string? FlightCheckpointAdoptionError => _flightCheckpointAdoptionError;

    public string? CurrentFlightCheckpointId => _currentFlightCheckpointId;

    public bool CurrentSessionLoadedFromFlightCheckpoint => _currentSessionLoadedFromFlightCheckpoint;

    public void ExpectNextSessionToBeOwned(string saveName)
    {
        if (string.IsNullOrWhiteSpace(saveName))
        {
            throw new ArgumentException("An owned save name is required.", nameof(saveName));
        }

        if (((GameMain.data is not null || GameMain.isRunning) && !DSPGame.IsMenuDemo)
            || _expectedOwnedSaveName is not null
            || _expectedResumeTicket is not null
            || _expectedFlightCheckpoint is not null)
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
        // DSP keeps a synthetic GameData alive for the animated main-menu demo.
        // Treating that demo as a loaded world can consume a pending exact-load
        // expectation before DSPGame.StartGame has created the real save loader.
        var running = GameMain.isRunning && GameMain.data is not null && !DSPGame.IsMenuDemo;
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
                _writeQuarantineActionId = null;
                _writeQuarantineReason = null;
                _currentFlightCheckpointId = null;
                _currentSessionLoadedFromFlightCheckpoint = false;
                CurrentOwnedSessionStartedAsNewGame = false;
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
            _writeQuarantineActionId = null;
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
                CurrentOwnedSessionStartedAsNewGame = true;
                _logger.LogInfo("Spherewright adopted the newly created ordinary peaceful world");
            }
            else if (_expectedResumeTicket is not null)
            {
                _ownedData = null;
                _ownedSaveName = null;
                _ownedSaveState = OwnedSaveStates.WaitingForWorld;
                _ownedSaveError = null;
                _logger.LogInfo("Spherewright detected the exact one-time owned-world resume load and is validating provenance");
            }
            else if (_expectedFlightCheckpoint is not null)
            {
                _ownedData = null;
                _ownedSaveName = null;
                _ownedSaveState = OwnedSaveStates.WaitingForWorld;
                _ownedSaveError = null;
                _logger.LogInfo("Spherewright detected an exact flight-checkpoint reload and is validating provenance");
            }
            else
            {
                _ownedData = null;
                _ownedSaveName = null;
                _ownedSaveState = OwnedSaveStates.None;
                _ownedSaveError = null;
                _currentFlightCheckpointId = null;
                _currentSessionLoadedFromFlightCheckpoint = false;
                CurrentOwnedSessionStartedAsNewGame = false;
                _logger.LogWarning("Spherewright detected an unowned game session; save and factory reads are blocked");
            }
        }

        if (_expectedFlightCheckpoint is not null && !IsCurrentSessionOwned)
        {
            if (!TryValidateFlightCheckpointCandidate(currentData, _expectedFlightCheckpoint, out var pending, out var rejection))
            {
                if (pending)
                {
                    return;
                }

                _flightCheckpointAdoptionError = rejection;
                _expectedFlightCheckpoint = null;
                _ownedSaveState = OwnedSaveStates.None;
                _logger.LogError("Spherewright rejected a flight-checkpoint reload because provenance did not match");
                return;
            }

            var ticket = _expectedFlightCheckpoint;
            _ownedData = currentData;
            _ownedSaveName = ticket.OwnedSaveName;
            _expectedFlightCheckpoint = null;
            _ownedSaveState = OwnedSaveStates.Saved;
            _ownedSaveError = null;
            _ownedSessionStartTick = GameMain.gameTick;
            _lastOwnedSaveGameTick = ticket.SavedGameTick;
            _currentFlightCheckpointId = ticket.CheckpointId;
            _currentSessionLoadedFromFlightCheckpoint = true;
            CurrentOwnedSessionStartedAsNewGame = false;
            _logger.LogInfo("Spherewright adopted the exact reusable pre-flight checkpoint without replacing the primary owned save");
        }

        if (_expectedResumeTicket is not null && !IsCurrentSessionOwned)
        {
            if (!TryValidateResumeCandidate(currentData, _expectedResumeTicket, out var pending, out var rejection))
            {
                if (pending)
                {
                    return;
                }

                _resumeAdoptionError = rejection;
                _resumeTickets.Consume(_expectedResumeTicket.ResumeToken);
                _expectedResumeTicket = null;
                _ownedSaveState = OwnedSaveStates.None;
                _logger.LogError("Spherewright rejected an owned-world resume candidate because provenance did not match");
                return;
            }

            var ticket = _expectedResumeTicket;
            _ownedData = currentData;
            _ownedSaveName = ticket.OwnedSaveName;
            _expectedResumeTicket = null;
            _ownedSaveState = OwnedSaveStates.WaitingToSave;
            _ownedSaveError = null;
            _ownedSessionStartTick = GameMain.gameTick;
            _lastOwnedSaveGameTick = null;
            _resumeTickets.Consume(ticket.ResumeToken);
            CurrentOwnedSessionStartedAsNewGame = false;
            _logger.LogInfo("Spherewright adopted the exact normally saved owned world through one-time restart-resume proof");
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
            var idle = new SessionState
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
                RestartResumeAvailable = _resumeTickets.HasCurrentTicket,
                RestartResumeToken = _resumeTickets.CurrentResumeToken,
                UserSaveImportConfigured = _userSaveImportConfigured,
                Capabilities = CreateIdleCapabilities(),
            };
            ApplyFlightCheckpointState(idle);
            return idle;
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
                WriteQuarantineActionId = _writeQuarantineActionId,
                OwnedSaveState = OwnedSaveStates.None,
                UserSaveImportConfigured = _userSaveImportConfigured,
                Capabilities = CreateUnownedCapabilities(),
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

        var owned = new SessionState
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
            WriteQuarantineActionId = _writeQuarantineActionId,
            WriteBlockers = blockers,
            OwnedSaveState = _ownedSaveState,
            OwnedSaveError = _ownedSaveError,
            LastOwnedSaveGameTick = _lastOwnedSaveGameTick,
            RestartResumeAvailable = _resumeTickets.HasCurrentTicket,
            RestartResumeToken = _resumeTickets.CurrentResumeToken,
            CurrentSessionLoadedFromFlightCheckpoint = _currentSessionLoadedFromFlightCheckpoint,
            UserSaveImportConfigured = _userSaveImportConfigured,
            Capabilities = CreateOwnedCapabilities(writesAllowed, _writeHealth),
        };
        ApplyFlightCheckpointState(owned);
        return owned;
    }

    private List<string> CreateIdleCapabilities()
    {
        var capabilities = new List<string> { "bridge.status", "new-game.create", "owned-game.resume", "action.read" };
        if (_flightCheckpoints.HasCurrentTicket)
        {
            capabilities.Add("flight-checkpoint.reload");
        }

        return capabilities;
    }

    private List<string> CreateUnownedCapabilities()
    {
        var capabilities = new List<string> { "bridge.status", "session.safe-status" };
        if (_userSaveImportConfigured
            && string.Equals(_writeHealth, WriteHealthStates.Healthy, StringComparison.Ordinal))
        {
            capabilities.Add("user-save.import.prepare");
        }

        return capabilities;
    }

    public bool TryGetCurrentUnownedImportCandidateOnMainThread(
        string? requestedSessionId,
        out GameData? data,
        out string rejection)
    {
        UpdateOnMainThread();
        data = null;
        rejection = string.Empty;
        if (!GameLoaded || _observedData is null || GameMain.data is null)
        {
            rejection = "No ordinary game is loaded.";
            return false;
        }

        if (IsCurrentSessionOwned)
        {
            rejection = "The current world is already Spherewright-owned.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(requestedSessionId)
            || !string.Equals(requestedSessionId, _sessionId, StringComparison.Ordinal))
        {
            rejection = "The requested unowned session is stale.";
            return false;
        }

        if (_expectedOwnedSaveName is not null
            || _expectedResumeTicket is not null
            || _expectedFlightCheckpoint is not null)
        {
            rejection = "Another protected world-adoption flow is active.";
            return false;
        }

        if (!ReferenceEquals(_observedData, GameMain.data))
        {
            rejection = "The current world identity changed.";
            return false;
        }

        data = _observedData;
        return true;
    }

    private void ApplyFlightCheckpointState(SessionState state)
    {
        var ticket = _flightCheckpoints.CurrentTicket;
        if (ticket is null
            || (state.OwnedBySpherewright
                && !string.Equals(state.SaveName, ticket.OwnedSaveName, StringComparison.Ordinal)))
        {
            return;
        }

        state.FlightCheckpointAvailable = true;
        state.FlightCheckpointId = ticket.CheckpointId;
        state.FlightCheckpointReloadToken = ticket.ReloadToken;
        state.FlightCheckpointOriginPlanetId = ticket.OriginPlanetId;
        state.FlightCheckpointDestinationPlanetId = ticket.DestinationPlanetId;
        state.FlightCheckpointGameTick = ticket.SavedGameTick;
        if (!state.Capabilities.Contains("flight-checkpoint.reload"))
        {
            state.Capabilities.Add("flight-checkpoint.reload");
        }
    }

    public void IncrementRevisionOnMainThread()
    {
        if (IsCurrentSessionOwned)
        {
            _revision++;
        }
    }

    public bool TryImportCurrentSessionAsOwnedCopyOnMainThread(
        string expectedSessionId,
        long expectedRevision,
        GameData expectedData,
        string newOwnedSaveName,
        string actionId,
        out long? savedGameTick,
        out bool outcomeUnknown,
        out string? rejection)
    {
        savedGameTick = null;
        outcomeUnknown = false;
        rejection = null;
        if (!UserSaveImportSafetyPolicy.IsEnabled(_writesConfigured, _userSaveImportConfigured)
            || !string.Equals(_writeHealth, WriteHealthStates.Healthy, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(newOwnedSaveName)
            || newOwnedSaveName.Length > 96
            || !Guid.TryParse(actionId, out _))
        {
            rejection = "The generated owned-copy identity is invalid or import is disabled.";
            return false;
        }

        if (!TryGetCurrentUnownedImportCandidateOnMainThread(expectedSessionId, out var currentData, out var candidateRejection)
            || currentData is null
            || !UserSaveImportSafetyPolicy.MatchesPreparedCandidate(
                expectedSessionId,
                _sessionId,
                expectedRevision,
                _revision,
                expectedData,
                currentData))
        {
            rejection = string.IsNullOrWhiteSpace(candidateRejection)
                ? "The exact confirmed world or revision changed before save."
                : candidateRejection;
            return false;
        }

        var localPlanet = currentData.localPlanet;
        if (localPlanet is null
            || currentData.localLoadedPlanetFactory is null
            || UnityEngine.Object.FindObjectOfType<GameLoader>() is not null)
        {
            rejection = "The exact confirmed world is no longer ready for a normal save.";
            return false;
        }

        var descriptor = currentData.gameDesc;
        if (descriptor is null
            || !descriptor.isPeaceMode
            || descriptor.isSandboxMode
            || GameMain.sandboxToolsEnabled
            || Math.Abs(descriptor.resourceMultiplier - 1f) > 0.0001f)
        {
            rejection = "The exact confirmed world no longer satisfies peaceful, non-sandbox, 1x import policy.";
            return false;
        }

        try
        {
            var candidatePath = GameSave.SavePath(newOwnedSaveName);
            if (string.IsNullOrWhiteSpace(candidatePath) || File.Exists(candidatePath))
            {
                rejection = "The generated owned-copy identity is not unused.";
                return false;
            }
        }
        catch (Exception exception) when (
            exception is IOException
            || exception is UnauthorizedAccessException
            || exception is ArgumentException
            || exception is NotSupportedException)
        {
            rejection = $"The generated owned-copy target could not be checked ({exception.GetType().Name}).";
            return false;
        }

        var originalGameName = currentData.gameName;
        if (string.IsNullOrWhiteSpace(originalGameName))
        {
            rejection = "The loaded world's internal original identity is unavailable; no save was attempted.";
            return false;
        }

        var expectedSavedTick = GameMain.gameTick;
        var saveReturnedTrue = false;
        try
        {
            GameMain.gameName = newOwnedSaveName;
            if (!GameSave.SaveCurrentGame(newOwnedSaveName))
            {
                GameMain.gameName = originalGameName;
                rejection = "DSP's normal save API returned false; the current world remains unowned.";
                return false;
            }

            saveReturnedTrue = true;
            GameSave.ReadHeader(newOwnedSaveName, false, out var header);
            if (!UserSaveImportSafetyPolicy.HasVerifiedCopyHeader(
                    saveReturnedTrue,
                    expectedSavedTick,
                    header?.gameTick))
            {
                GameMain.gameName = originalGameName;
                outcomeUnknown = true;
                rejection = "The newly saved copy could not prove its exact game tick; no ownership was adopted.";
                QuarantineUnownedImport(actionId, rejection);
                return false;
            }

            _ownedData = currentData;
            _ownedSaveName = newOwnedSaveName;
            _ownedSaveState = OwnedSaveStates.Saved;
            _ownedSaveError = null;
            _ownedSessionStartTick = expectedSavedTick;
            _lastOwnedSaveGameTick = expectedSavedTick;
            _lastPlanetId = localPlanet.id;
            _writeHealth = WriteHealthStates.Healthy;
            _writeQuarantineActionId = null;
            _writeQuarantineReason = null;
            _currentFlightCheckpointId = null;
            _currentSessionLoadedFromFlightCheckpoint = false;
            CurrentOwnedSessionStartedAsNewGame = false;
            _revision++;
            savedGameTick = expectedSavedTick;

            try
            {
                _resumeTickets.ArmFromHealthySavedOwnedSession(
                    newOwnedSaveName,
                    expectedSessionId,
                    localPlanet.id,
                    expectedSavedTick);
            }
            catch (Exception exception)
            {
                _logger.LogWarning($"Spherewright imported the owned copy but could not arm restart-resume ({exception.GetType().Name})");
            }

            _logger.LogInfo("Spherewright adopted an explicitly confirmed normal-save copy; the original save identity was not logged or modified");
            return true;
        }
        catch (Exception exception)
        {
            if (!IsCurrentSessionOwned && ReferenceEquals(GameMain.data, expectedData))
            {
                GameMain.gameName = originalGameName;
            }

            outcomeUnknown = saveReturnedTrue;
            rejection = saveReturnedTrue
                ? $"The owned-copy save completed but verification failed ({exception.GetType().Name}); no ownership was adopted."
                : $"DSP rejected the normal owned-copy save ({exception.GetType().Name}); the current world remains unowned.";
            if (outcomeUnknown)
            {
                QuarantineUnownedImport(actionId, rejection);
            }

            _logger.LogError($"Spherewright user-save import failed without exposing either save identity ({exception.GetType().Name})");
            return false;
        }
    }

    private void QuarantineUnownedImport(string actionId, string reason)
    {
        _writeHealth = WriteHealthStates.Quarantined;
        _writeQuarantineActionId = actionId;
        _writeQuarantineReason = reason;
        _logger.LogError("Spherewright quarantined current-session save import after an unproved owned-copy outcome");
    }

    private static List<string> CreateOwnedCapabilities(bool writesAllowed, string writeHealth)
    {
        var capabilities = new List<string>
        {
            "bridge.status",
            "session.read",
            "player.read",
            "progression.read",
            "gameplay-journal.read",
            "assembler.read",
            "build-catalog.read",
            "recipe-catalog.read",
            "resource.read",
            "factory.read",
            "power.read",
            "overseer.read",
            "action.read",
        };
        if (writesAllowed)
        {
            capabilities.Add("normal-game.prepare");
        }

        if (string.Equals(writeHealth, WriteHealthStates.Quarantined, StringComparison.Ordinal))
        {
            capabilities.Add("quarantine.reconcile");
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
            if (!_flightCheckpoints.TryRetireAfterPrimarySave(
                    _ownedSaveName!,
                    _lastOwnedSaveGameTick.Value,
                    out _,
                    out var checkpointRetirementError))
            {
                _logger.LogWarning($"Spherewright could not finalize flight-checkpoint retirement after the covering primary save: {checkpointRetirementError}");
            }

            if (string.Equals(_writeHealth, WriteHealthStates.Healthy, StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(_sessionId)
                && GameMain.localPlanet?.id is int localPlanetId
                && localPlanetId > 0)
            {
                try
                {
                    _resumeTickets.ArmFromHealthySavedOwnedSession(
                        _ownedSaveName!,
                        _sessionId!,
                        localPlanetId,
                        _lastOwnedSaveGameTick.Value);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning($"Spherewright could not arm planned restart-resume after the healthy save ({exception.GetType().Name})");
                }
            }

            _logger.LogInfo("Spherewright saved the owned ordinary world");
            return true;
        }
        catch (Exception exception)
        {
            _ownedSaveState = OwnedSaveStates.SaveFailed;
            _ownedSaveError = exception.GetType().Name;
            error = _ownedSaveError;
            _logger.LogError($"Spherewright owned-world save failed ({_ownedSaveError})");
            return false;
        }
    }

    public void QuarantineWritesOnMainThread(string actionId, string reason)
    {
        if (!IsCurrentSessionOwned || string.Equals(_writeHealth, WriteHealthStates.Quarantined, StringComparison.Ordinal))
        {
            return;
        }

        _writeHealth = WriteHealthStates.Quarantined;
        _writeQuarantineActionId = string.IsNullOrWhiteSpace(actionId) ? null : actionId;
        _writeQuarantineReason = string.IsNullOrWhiteSpace(reason) ? "A write outcome could not be proven." : reason;
        _revision++;
        try
        {
            if (!string.IsNullOrWhiteSpace(_ownedSaveName)
                && !string.IsNullOrWhiteSpace(_sessionId)
                && _lastPlanetId > 0
                && !string.IsNullOrWhiteSpace(_writeQuarantineActionId))
            {
                _resumeTickets.ArmFromQuarantinedOwnedSession(
                    _ownedSaveName!,
                    _sessionId!,
                    _lastPlanetId,
                    GameMain.gameTick,
                    _writeQuarantineActionId!);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning($"Spherewright could not arm restart-resume after quarantine ({exception.GetType().Name})");
        }
        _logger.LogError("Spherewright quarantined writes for the current owned session");
    }

    public void ExpectNextSessionToBeResumed(OwnedWorldResumeTicket ticket)
    {
        if (ticket is null)
        {
            throw new ArgumentNullException(nameof(ticket));
        }

        if (((GameMain.data is not null || GameMain.isRunning) && !DSPGame.IsMenuDemo)
            || _expectedOwnedSaveName is not null
            || _expectedResumeTicket is not null
            || _expectedFlightCheckpoint is not null)
        {
            throw new InvalidOperationException("An owned world can only be resumed from an idle main menu.");
        }

        _expectedResumeTicket = ticket;
        _ownedSaveState = OwnedSaveStates.WaitingForWorld;
        _ownedSaveError = null;
        _resumeAdoptionError = null;
    }

    public void CancelExpectedResumedSession()
    {
        _expectedResumeTicket = null;
        if (!IsCurrentSessionOwned)
        {
            _ownedSaveState = OwnedSaveStates.None;
            _ownedSaveError = null;
        }
    }

    public void MarkCurrentSessionFlightCheckpoint(FlightCheckpointTicket ticket)
    {
        if (ticket is null
            || !IsCurrentSessionOwned
            || !string.Equals(_sessionId, ticket.SourceSessionId, StringComparison.Ordinal)
            || !string.Equals(_ownedSaveName, ticket.OwnedSaveName, StringComparison.Ordinal)
            || _revision != ticket.SourceRevision
            || GameMain.localPlanet?.id != ticket.OriginPlanetId
            || GameMain.gameTick < ticket.SavedGameTick)
        {
            throw new InvalidOperationException("The completed flight checkpoint does not match the current owned session.");
        }

        _currentFlightCheckpointId = ticket.CheckpointId;
        _currentSessionLoadedFromFlightCheckpoint = false;
        _flightCheckpointAdoptionError = null;
    }

    public void ForgetCurrentFlightCheckpoint(string checkpointId)
    {
        if (!string.IsNullOrWhiteSpace(checkpointId)
            && string.Equals(_currentFlightCheckpointId, checkpointId, StringComparison.Ordinal))
        {
            _currentFlightCheckpointId = null;
            _currentSessionLoadedFromFlightCheckpoint = false;
            _flightCheckpointAdoptionError = null;
        }
    }

    public bool CanReuseFlightCheckpointForCurrentSession(FlightCheckpointTicket ticket)
    {
        if (ticket is null
            || !IsCurrentSessionOwned
            || !string.Equals(_ownedSaveName, ticket.OwnedSaveName, StringComparison.Ordinal)
            || !string.Equals(_currentFlightCheckpointId, ticket.CheckpointId, StringComparison.Ordinal)
            || GameMain.localPlanet?.id != ticket.OriginPlanetId)
        {
            return false;
        }

        if (_currentSessionLoadedFromFlightCheckpoint)
        {
            return _revision == 1 && GameMain.gameTick >= ticket.SavedGameTick;
        }

        return string.Equals(_sessionId, ticket.SourceSessionId, StringComparison.Ordinal)
            && _revision == ticket.SourceRevision + 1
            && GameMain.gameTick >= ticket.SavedGameTick;
    }

    public void ExpectNextSessionToBeLoadedFromFlightCheckpoint(FlightCheckpointTicket ticket)
    {
        if (ticket is null)
        {
            throw new ArgumentNullException(nameof(ticket));
        }

        var activeOrdinaryGame = (GameMain.data is not null || GameMain.isRunning) && !DSPGame.IsMenuDemo;
        if (_expectedOwnedSaveName is not null
            || _expectedResumeTicket is not null
            || _expectedFlightCheckpoint is not null
            || (activeOrdinaryGame
                && (!IsCurrentSessionOwned
                    || !string.Equals(_ownedSaveName, ticket.OwnedSaveName, StringComparison.Ordinal))))
        {
            throw new InvalidOperationException("The exact flight checkpoint can only replace its owned game or load from an idle main menu.");
        }

        _expectedFlightCheckpoint = ticket;
        _ownedSaveState = OwnedSaveStates.WaitingForWorld;
        _ownedSaveError = null;
        _flightCheckpointAdoptionError = null;
    }

    public void CancelExpectedFlightCheckpointSession()
    {
        _expectedFlightCheckpoint = null;
        if (!IsCurrentSessionOwned)
        {
            _ownedSaveState = OwnedSaveStates.None;
            _ownedSaveError = null;
        }
        else
        {
            _ownedSaveState = _lastOwnedSaveGameTick.HasValue
                ? OwnedSaveStates.Saved
                : OwnedSaveStates.WaitingToSave;
        }
    }

    public bool TryClearQuarantineOnMainThread(
        string expectedActionId,
        string expectedReason,
        out string? rejection)
    {
        rejection = null;
        if (!IsCurrentSessionOwned
            || !string.Equals(_writeHealth, WriteHealthStates.Quarantined, StringComparison.Ordinal))
        {
            rejection = "The current owned session is not quarantined.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_writeQuarantineActionId)
            || !string.Equals(_writeQuarantineActionId, expectedActionId, StringComparison.Ordinal)
            || !string.Equals(_writeQuarantineReason, expectedReason, StringComparison.Ordinal))
        {
            rejection = "The quarantined action identity or reason changed after reconciliation was prepared.";
            return false;
        }

        var resumeToken = _resumeTickets.CurrentResumeToken;
        _writeHealth = WriteHealthStates.Healthy;
        _writeQuarantineActionId = null;
        _writeQuarantineReason = null;
        _revision++;
        if (!string.IsNullOrWhiteSpace(resumeToken))
        {
            _resumeTickets.Consume(resumeToken!);
        }
        _logger.LogInfo("Spherewright cleared write quarantine after exact action reconciliation");
        return true;
    }

    private static bool TryValidateFlightCheckpointCandidate(
        GameData currentData,
        FlightCheckpointTicket ticket,
        out bool pending,
        out string rejection)
    {
        pending = false;
        rejection = string.Empty;
        if (UnityEngine.Object.FindObjectOfType<GameLoader>() is not null)
        {
            pending = true;
            rejection = "DSP is still running the exact flight-checkpoint loader.";
            return false;
        }

        var localPlanet = currentData.localPlanet;
        if (localPlanet is null)
        {
            // GameLoader publishes a new GameData before it has populated the
            // save identity, local planet, and final DSPGame.LoadFile state.
            // Keep the exact expectation armed until the world has crossed
            // that native readiness boundary; validating earlier rejects a
            // legitimate checkpoint on its transient loader values.
            pending = true;
            rejection = "The flight-checkpoint origin planet is still loading.";
            return false;
        }

        if (!OwnedWorldProvenancePolicy.MatchesProtectedSaveIdentity(
                ticket.OwnedSaveName,
                currentData.gameName))
        {
            rejection = "The flight checkpoint did not contain the exact primary owned-save identity.";
            return false;
        }

        if (GameMain.gameTick < ticket.SavedGameTick)
        {
            rejection = "The loaded flight checkpoint is older than its protected ticket.";
            return false;
        }

        // GameData.Import deliberately clears DSPGame.LoadFile before it reads
        // the saved tick, so that transient field cannot be a post-load proof.
        // Commit already revalidates the exact internal file/header and is the
        // only path that arms this ticket before calling StartGame with that
        // name. Bound adoption to the first minute of resumed simulation as an
        // additional final-state guard against a different later save of the
        // same primary owned world.
        if (GameMain.gameTick > ticket.SavedGameTick + 3600L)
        {
            rejection = "The loaded flight-checkpoint candidate advanced beyond the bounded adoption window.";
            return false;
        }

        var descriptor = currentData.gameDesc;
        if (descriptor is null
            || !descriptor.isPeaceMode
            || descriptor.isSandboxMode
            || GameMain.sandboxToolsEnabled
            || Math.Abs(descriptor.resourceMultiplier - 1f) > 0.0001f)
        {
            rejection = "The flight checkpoint did not prove peaceful, non-sandbox, normal 1x settings.";
            return false;
        }

        if (localPlanet.id != ticket.OriginPlanetId)
        {
            rejection = "The loaded planet does not match the protected flight-checkpoint origin.";
            return false;
        }

        return true;
    }

    private static bool TryValidateResumeCandidate(
        GameData currentData,
        OwnedWorldResumeTicket ticket,
        out bool pending,
        out string rejection)
    {
        pending = false;
        rejection = string.Empty;
        if (!OwnedWorldProvenancePolicy.MatchesProtectedSaveIdentity(
                ticket.OwnedSaveName,
                currentData.gameName))
        {
            rejection = "The resumed payload did not contain the exact high-entropy owned save identity.";
            return false;
        }

        if (GameMain.gameTick < ticket.MinimumGameTick)
        {
            rejection = "The resumed payload is older than the authenticated source-session ticket.";
            return false;
        }

        var descriptor = currentData.gameDesc;
        if (descriptor is null
            || !descriptor.isPeaceMode
            || descriptor.isSandboxMode
            || GameMain.sandboxToolsEnabled
            || Math.Abs(descriptor.resourceMultiplier - 1f) > 0.0001f)
        {
            rejection = "The resumed payload did not prove peaceful, non-sandbox, normal 1x settings.";
            return false;
        }

        var localPlanet = currentData.localPlanet;
        if (localPlanet is null)
        {
            pending = true;
            rejection = "The resumed local planet is still loading.";
            return false;
        }

        if (localPlanet.id != ticket.ExpectedPlanetId)
        {
            rejection = "The resumed local planet does not match the authenticated source-session ticket.";
            return false;
        }

        return true;
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
