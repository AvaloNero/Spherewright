using System.ComponentModel;
using System.Diagnostics;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Actions;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Sessions;
using Spherewright.Plugin.RuntimeDescriptor;

namespace Spherewright.Plugin.Game;

internal sealed class FlightCheckpointReloadCoordinator
{
    private const string IdempotencyScope = "reload-flight-checkpoint";
    private readonly bool _writesConfigured;
    private readonly GameSessionTracker _sessions;
    private readonly FlightCheckpointStore _tickets;
    private readonly NormalGameActionCoordinator _normalActions;
    private readonly PreparedPlanStore<ReloadPlanPayload> _plans;
    private readonly IdempotencyCache<FlightCheckpointReloadResult> _idempotency;
    private readonly Dictionary<string, ReloadAction> _actions =
        new Dictionary<string, ReloadAction>(StringComparer.Ordinal);

    public FlightCheckpointReloadCoordinator(
        bool writesConfigured,
        int planLifetimeSeconds,
        int idempotencyRetentionMinutes,
        int idempotencyCapacity,
        GameSessionTracker sessions,
        FlightCheckpointStore tickets,
        NormalGameActionCoordinator normalActions)
    {
        _writesConfigured = writesConfigured;
        _sessions = sessions;
        _tickets = tickets;
        _normalActions = normalActions;
        _plans = new PreparedPlanStore<ReloadPlanPayload>(TimeSpan.FromSeconds(planLifetimeSeconds), 4);
        _idempotency = new IdempotencyCache<FlightCheckpointReloadResult>(
            idempotencyCapacity,
            TimeSpan.FromMinutes(idempotencyRetentionMinutes));
    }

    public GameCallResult<PreparedFlightCheckpointReloadPlan> PrepareOnMainThread(
        PrepareFlightCheckpointReloadRequest request)
    {
        if (!_tickets.TryGetActiveTicket(request.ReloadToken, out var ticket, out var ticketRejection)
            || ticket is null)
        {
            return GameCallResult<PreparedFlightCheckpointReloadPlan>.Failed(BridgeError.Create(
                BridgeErrorCodes.SessionNotOwned,
                ticketRejection,
                false,
                "Use only the reusable reload token exposed for the exact pre-flight checkpoint."));
        }

        if (!TryValidateReloadContext(ticket, out var rejection))
        {
            return GameCallResult<PreparedFlightCheckpointReloadPlan>.Failed(BridgeError.Create(
                BridgeErrorCodes.StaleState,
                rejection,
                true,
                "Wait for a clear flight failure or an idle main menu, then prepare the same exact checkpoint again."));
        }

        var payload = new ReloadPlanPayload(ticket);
        PreparedPlan<ReloadPlanPayload> plan;
        try
        {
            plan = _plans.Add(payload.Fingerprint, payload);
        }
        catch (InvalidOperationException)
        {
            return GameCallResult<PreparedFlightCheckpointReloadPlan>.Failed(BridgeError.Create(
                BridgeErrorCodes.ServerBusy,
                "Too many flight-checkpoint reload plans are active.",
                true,
                "Wait for old plans to expire, then prepare the same checkpoint again."));
        }

        var blockers = new List<WriteBlocker>();
        if (!_writesConfigured)
        {
            blockers.Add(new WriteBlocker
            {
                Code = BridgeErrorCodes.WritesDisabled,
                Message = "Flight-checkpoint reload is blocked because Safety.AllowWrites is false.",
            });
        }

        return GameCallResult<PreparedFlightCheckpointReloadPlan>.Succeeded(new PreparedFlightCheckpointReloadPlan
        {
            Prepared = true,
            PlanToken = plan.Token,
            ExpiresAtUtc = plan.ExpiresAtUtc,
            CheckpointId = ticket.CheckpointId,
            OriginPlanetId = ticket.OriginPlanetId,
            DestinationPlanetId = ticket.DestinationPlanetId,
            SavedGameTick = ticket.SavedGameTick,
            CommitAllowedNow = blockers.Count == 0,
            CommitBlockers = blockers,
            CompletionCondition = "DSP loads only the internally generated pre-flight save whose exact name and game tick match the protected reusable ticket; the embedded primary owned-save identity, origin planet, peaceful mode, non-sandbox mode, and 1x resources must all match before adoption.",
        });
    }

    public GameCallResult<FlightCheckpointReloadResult> CommitOnMainThread(
        CommitFlightCheckpointReloadRequest request)
    {
        if (!Guid.TryParse(request.IdempotencyKey, out _))
        {
            return GameCallResult<FlightCheckpointReloadResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.InvalidRequest,
                "A UUID idempotency key is required.",
                false,
                "Generate one UUID and reuse it only for retries of this exact checkpoint reload commit."));
        }

        var fingerprint = "commit-reload-flight-checkpoint|" + request.PlanToken;
        if (_idempotency.TryGet(
            IdempotencyScope,
            request.IdempotencyKey,
            fingerprint,
            out var replay,
            out var conflict))
        {
            return GameCallResult<FlightCheckpointReloadResult>.Succeeded(CloneAsReplay(replay!));
        }

        if (conflict)
        {
            return GameCallResult<FlightCheckpointReloadResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.IdempotencyConflict,
                "The idempotency key is already bound to another checkpoint reload.",
                false,
                "Reuse it only for the original reload or generate a new UUID after preparing a new retry."));
        }

        if (!_writesConfigured)
        {
            return GameCallResult<FlightCheckpointReloadResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.WritesDisabled,
                "Flight-checkpoint reload is blocked because Safety.AllowWrites is false.",
                false,
                "Enable writes and prepare the exact checkpoint again."));
        }

        if (!_idempotency.HasCapacity(IdempotencyScope))
        {
            return GameCallResult<FlightCheckpointReloadResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.IdempotencyCapacityExceeded,
                "The idempotency cache has no capacity for another flight-checkpoint reload.",
                false,
                "Restart the Plugin without loading another save, then use the same protected checkpoint token."));
        }

        if (!_plans.TryTake(request.PlanToken, out var plan, out var expired) || plan is null)
        {
            return GameCallResult<FlightCheckpointReloadResult>.Failed(BridgeError.Create(
                expired ? BridgeErrorCodes.PlanExpired : BridgeErrorCodes.PlanNotFound,
                expired ? "The flight-checkpoint reload plan expired." : "The flight-checkpoint reload plan was not found or was already consumed.",
                true,
                "Prepare the same exact checkpoint again and commit it once."));
        }

        var payload = plan.Payload;
        var rejection = "The reusable flight-checkpoint ticket changed after prepare.";
        if (!_tickets.TryGetActiveTicket(payload.Ticket.ReloadToken, out var current, out _)
            || current is null
            || !string.Equals(new ReloadPlanPayload(current).Fingerprint, payload.Fingerprint, StringComparison.Ordinal)
            || !TryValidateReloadContext(current, out rejection))
        {
            return GameCallResult<FlightCheckpointReloadResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.StaleState,
                rejection,
                true,
                "Do not load another save; inspect the exact checkpoint state and prepare it again."));
        }

        var action = new ReloadAction
        {
            ActionId = Guid.NewGuid().ToString("D"),
            Ticket = current,
        };
        try
        {
            _normalActions.NotifyFlightCheckpointReloadStartingOnMainThread(current);
            _sessions.ExpectNextSessionToBeLoadedFromFlightCheckpoint(current);
            DSPGame.StartGame(current.CheckpointSaveName);
        }
        catch (Exception exception)
        {
            _sessions.CancelExpectedFlightCheckpointSession();
            return GameCallResult<FlightCheckpointReloadResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.ActionFailed,
                $"DSP rejected the exact pre-flight checkpoint through its normal loader ({exception.GetType().Name}).",
                false,
                "Keep the same checkpoint token, inspect local logs, and do not load another save."));
        }

        var result = new FlightCheckpointReloadResult
        {
            ActionId = action.ActionId,
            CheckpointId = current.CheckpointId,
            Accepted = true,
            IdempotentReplay = false,
            State = NormalActionStates.WaitingForGame,
        };
        if (!_idempotency.TryAdd(IdempotencyScope, request.IdempotencyKey, fingerprint, result))
        {
            return GameCallResult<FlightCheckpointReloadResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.IdempotencyCapacityExceeded,
                "The idempotency cache reached capacity after DSP accepted the exact checkpoint load.",
                false,
                "Do not retry with a new key; inspect session state and the returned action ID."));
        }

        _actions[action.ActionId] = action;
        return GameCallResult<FlightCheckpointReloadResult>.Succeeded(result);
    }

    public bool TryGetActionResultOnMainThread(string actionId, out ActionResultSnapshot? result)
    {
        if (!_actions.TryGetValue(actionId, out var action))
        {
            result = null;
            return false;
        }

        var session = _sessions.CaptureOnMainThread();
        result = new ActionResultSnapshot
        {
            ActionId = action.ActionId,
            ActionKind = "reload-flight-checkpoint",
            State = NormalActionStates.WaitingForGame,
            Terminal = false,
            Succeeded = false,
            FlightCheckpointId = action.Ticket.CheckpointId,
            FlightCheckpointReloadToken = action.Ticket.ReloadToken,
            FlightCheckpointGameTick = action.Ticket.SavedGameTick,
            Message = "DSP accepted the exact internally named pre-flight checkpoint; Spherewright is validating its reusable provenance proof.",
        };
        if (session.OwnedBySpherewright
            && session.CurrentSessionLoadedFromFlightCheckpoint
            && string.Equals(session.FlightCheckpointId, action.Ticket.CheckpointId, StringComparison.Ordinal)
            && session.LocalPlanetId == action.Ticket.OriginPlanetId
            && session.GameTick >= action.Ticket.SavedGameTick)
        {
            result.State = NormalActionStates.Completed;
            result.Terminal = true;
            result.Succeeded = true;
            result.SessionId = session.SessionId;
            result.PlanetId = session.LocalPlanetId;
            result.Message = "The exact pre-flight checkpoint passed provenance checks and is ready to retry the same flight; the primary owned save was not replaced.";
        }
        else if (!string.IsNullOrWhiteSpace(_sessions.FlightCheckpointAdoptionError))
        {
            result.State = NormalActionStates.ActionFailed;
            result.Terminal = true;
            result.Message = _sessions.FlightCheckpointAdoptionError;
        }

        return true;
    }

    private bool TryValidateReloadContext(FlightCheckpointTicket ticket, out string rejection)
    {
        if (!_tickets.TryValidateCheckpointFile(ticket, out rejection))
        {
            return false;
        }

        var session = _sessions.CaptureOnMainThread();
        if (session.GameLoaded)
        {
            if (!session.OwnedBySpherewright
                || !string.Equals(session.SaveName, ticket.OwnedSaveName, StringComparison.Ordinal))
            {
                rejection = "An active game may be replaced only by a checkpoint bound to that exact owned save.";
                return false;
            }

            if (!FlightCheckpointStore.IsRecoveryRequired(ticket))
            {
                rejection = "The bound flight has not reached a persisted recovery-required state.";
                return false;
            }

            if (!session.CurrentSessionLoadedFromFlightCheckpoint
                && !_normalActions.HasRecoveryRequiredFlightOnMainThread(ticket.CheckpointId))
            {
                rejection = "The current process has no terminal failed-flight evidence for this checkpoint.";
                return false;
            }
        }
        else
        {
            var readiness = TestWorldCoordinator.ValidateMainMenuReady();
            if (readiness is not null)
            {
                rejection = readiness.Message;
                return false;
            }

            if (!FlightCheckpointStore.IsRecoveryRequired(ticket)
                && !(FlightCheckpointStore.IsAttemptInFlight(ticket)
                     && !IsLiveDspProcess(ticket.SourceProcessId)))
            {
                rejection = "The checkpoint is neither recovery-required nor an interrupted in-flight attempt from a terminated DSP process.";
                return false;
            }
        }

        return _normalActions.CanReloadFlightCheckpointOnMainThread(ticket, out rejection);
    }

    private static bool IsLiveDspProcess(int processId)
    {
        try
        {
            using (var process = Process.GetProcessById(processId))
            {
                return !process.HasExited
                    && string.Equals(process.ProcessName, "DSPGAME", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is Win32Exception)
        {
            return false;
        }
    }

    private static FlightCheckpointReloadResult CloneAsReplay(FlightCheckpointReloadResult result) =>
        new FlightCheckpointReloadResult
        {
            ActionId = result.ActionId,
            CheckpointId = result.CheckpointId,
            Accepted = result.Accepted,
            IdempotentReplay = true,
            State = result.State,
        };

    private sealed class ReloadPlanPayload
    {
        public ReloadPlanPayload(FlightCheckpointTicket ticket)
        {
            Ticket = ticket;
            Fingerprint = CanonicalStateHash.Combine(
                "reload-flight-checkpoint",
                ticket.CheckpointId,
                ticket.ReloadToken,
                ticket.CheckpointSaveName,
                ticket.OwnedSaveName,
                ticket.SourceSessionId,
                ticket.SourceRevision,
                ticket.GameVersion,
                ticket.OriginPlanetId,
                ticket.DestinationPlanetId,
                ticket.SavedGameTick,
                ticket.PlayerStateHash,
                ticket.StarSystemStateHash,
                ticket.IssuedAtUtc);
        }

        public FlightCheckpointTicket Ticket { get; }

        public string Fingerprint { get; }
    }

    private sealed class ReloadAction
    {
        public string ActionId { get; set; } = string.Empty;

        public FlightCheckpointTicket Ticket { get; set; } = null!;
    }
}
