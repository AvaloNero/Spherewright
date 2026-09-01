using System.ComponentModel;
using System.Diagnostics;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Actions;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Sessions;
using Spherewright.Plugin.RuntimeDescriptor;

namespace Spherewright.Plugin.Game;

internal sealed class OwnedWorldResumeCoordinator
{
    private const string IdempotencyScope = "resume-owned-world";
    private readonly bool _writesConfigured;
    private readonly GameSessionTracker _sessions;
    private readonly OwnedWorldResumeTicketStore _tickets;
    private readonly PreparedPlanStore<OwnedWorldResumePlanPayload> _plans;
    private readonly IdempotencyCache<OwnedWorldResumeResult> _idempotency;
    private readonly Dictionary<string, OwnedWorldResumeAction> _actions =
        new Dictionary<string, OwnedWorldResumeAction>(StringComparer.Ordinal);

    public OwnedWorldResumeCoordinator(
        bool writesConfigured,
        int planLifetimeSeconds,
        int idempotencyRetentionMinutes,
        int idempotencyCapacity,
        GameSessionTracker sessions,
        OwnedWorldResumeTicketStore tickets)
    {
        _writesConfigured = writesConfigured;
        _sessions = sessions;
        _tickets = tickets;
        _plans = new PreparedPlanStore<OwnedWorldResumePlanPayload>(
            TimeSpan.FromSeconds(planLifetimeSeconds),
            4);
        _idempotency = new IdempotencyCache<OwnedWorldResumeResult>(
            idempotencyCapacity,
            TimeSpan.FromMinutes(idempotencyRetentionMinutes));
    }

    public GameCallResult<PreparedOwnedWorldResumePlan> PrepareOnMainThread(PrepareOwnedWorldResumeRequest request)
    {
        var readinessError = TestWorldCoordinator.ValidateMainMenuReady();
        if (readinessError is not null)
        {
            return GameCallResult<PreparedOwnedWorldResumePlan>.Failed(readinessError);
        }

        if (!_tickets.TryGetActiveTicket(request.ResumeToken, out var ticket, out var ticketRejection)
            || ticket is null)
        {
            return GameCallResult<PreparedOwnedWorldResumePlan>.Failed(BridgeError.Create(
                BridgeErrorCodes.SessionNotOwned,
                ticketRejection,
                false,
                "Use only the one-time restartResumeToken issued for the exact quarantined Spherewright-owned session."));
        }

        if (!TryResolveResumeSource(ticket, out var resumeSource, out var rejection))
        {
            return GameCallResult<PreparedOwnedWorldResumePlan>.Failed(BridgeError.Create(
                BridgeErrorCodes.StaleState,
                rejection,
                false,
                "Keep the ticket and inspect the exact normal shutdown or latest healthy owned-save evidence."));
        }

        var payload = new OwnedWorldResumePlanPayload(ticket, resumeSource);
        PreparedPlan<OwnedWorldResumePlanPayload> plan;
        try
        {
            plan = _plans.Add(payload.Fingerprint, payload);
        }
        catch (InvalidOperationException)
        {
            return GameCallResult<PreparedOwnedWorldResumePlan>.Failed(BridgeError.Create(
                BridgeErrorCodes.ServerBusy,
                "Too many owned-world resume plans are active.",
                true,
                "Wait for old plans to expire, then prepare the one-time resume again."));
        }

        var blockers = new List<WriteBlocker>();
        if (!_writesConfigured)
        {
            blockers.Add(new WriteBlocker
            {
                Code = BridgeErrorCodes.WritesDisabled,
                Message = "Owned-world resume is blocked because Safety.AllowWrites is false.",
            });
        }

        return GameCallResult<PreparedOwnedWorldResumePlan>.Succeeded(new PreparedOwnedWorldResumePlan
        {
            Prepared = true,
            PlanToken = plan.Token,
            ExpiresAtUtc = plan.ExpiresAtUtc,
            ExpectedPlanetId = ticket.ExpectedPlanetId,
            MinimumGameTick = ticket.MinimumGameTick,
            CommitAllowedNow = blockers.Count == 0,
            CommitBlockers = blockers,
            CompletionCondition = string.IsNullOrWhiteSpace(ticket.QuarantineActionId)
                ? "A healthy planned restart loads only the exact primary owned save named inside the protected ticket after its header proves the minimum game tick; adoption still requires the embedded high-entropy owned name, planet, peaceful mode, non-sandbox mode, and 1x resources."
                : "Quarantine recovery loads only the fresh fixed LastExit slot after its header proves the minimum game tick; adoption still requires the embedded high-entropy owned name, planet, peaceful mode, non-sandbox mode, and 1x resources.",
        });
    }

    public GameCallResult<OwnedWorldResumeResult> CommitOnMainThread(CommitOwnedWorldResumeRequest request)
    {
        if (!Guid.TryParse(request.IdempotencyKey, out _))
        {
            return GameCallResult<OwnedWorldResumeResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.InvalidRequest,
                "A UUID idempotency key is required.",
                false,
                "Generate one UUID and reuse it for retries of this exact resume commit."));
        }

        var fingerprint = "commit-resume-owned-world|" + request.PlanToken;
        if (_idempotency.TryGet(
            IdempotencyScope,
            request.IdempotencyKey,
            fingerprint,
            out var replay,
            out var conflict))
        {
            return GameCallResult<OwnedWorldResumeResult>.Succeeded(CloneAsReplay(replay!));
        }

        if (conflict)
        {
            return GameCallResult<OwnedWorldResumeResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.IdempotencyConflict,
                "The idempotency key is already bound to a different owned-world resume request.",
                false,
                "Reuse it only for the original resume commit."));
        }

        if (!_writesConfigured)
        {
            return GameCallResult<OwnedWorldResumeResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.WritesDisabled,
                "Owned-world resume is blocked because Safety.AllowWrites is false.",
                false,
                "Enable writes, restart DSP at the main menu, and prepare the one-time resume again."));
        }

        if (!_idempotency.HasCapacity(IdempotencyScope))
        {
            return GameCallResult<OwnedWorldResumeResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.IdempotencyCapacityExceeded,
                "The idempotency cache has no capacity for another owned-world resume action.",
                false,
                "Restart the Plugin at the main menu before preparing the one-time resume again; no LastExit load was started."));
        }

        if (!_plans.TryTake(request.PlanToken, out var plan, out var expired) || plan is null)
        {
            return GameCallResult<OwnedWorldResumeResult>.Failed(BridgeError.Create(
                expired ? BridgeErrorCodes.PlanExpired : BridgeErrorCodes.PlanNotFound,
                expired ? "The owned-world resume plan expired." : "The owned-world resume plan was not found or was already consumed.",
                true,
                "Prepare the exact one-time resume again and commit it once."));
        }

        var readinessError = TestWorldCoordinator.ValidateMainMenuReady();
        var payload = plan.Payload;
        var rejection = "The one-time resume ticket changed after prepare.";
        if (readinessError is not null
            || !_tickets.TryGetActiveTicket(payload.Ticket.ResumeToken, out var currentTicket, out _)
            || currentTicket is null
            || !TryResolveResumeSource(currentTicket, out var currentResumeSource, out rejection)
            || currentResumeSource != payload.ResumeSource
            || !string.Equals(new OwnedWorldResumePlanPayload(currentTicket, currentResumeSource).Fingerprint, payload.Fingerprint, StringComparison.Ordinal))
        {
            return GameCallResult<OwnedWorldResumeResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.StaleState,
                readinessError?.Message ?? rejection,
                false,
                "Do not load a save; return to an idle main menu and prepare from the exact current ticket."));
        }

        var action = new OwnedWorldResumeAction
        {
            ActionId = Guid.NewGuid().ToString("D"),
            Ticket = currentTicket,
            ResumeSource = currentResumeSource,
        };
        try
        {
            _sessions.ExpectNextSessionToBeResumed(currentTicket);
            DSPGame.StartGame(
                currentResumeSource == OwnedWorldResumeSourceKind.LastExit
                    ? GameSave.LastExit
                    : currentTicket.OwnedSaveName);
        }
        catch (Exception exception)
        {
            _sessions.CancelExpectedResumedSession();
            return GameCallResult<OwnedWorldResumeResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.ActionFailed,
                $"DSP rejected the exact protected owned-world resume through its normal loader ({exception.GetType().Name}).",
                false,
                "Inspect the local Spherewright and Unity logs; do not load another save."));
        }

        var result = new OwnedWorldResumeResult
        {
            ActionId = action.ActionId,
            Accepted = true,
            IdempotentReplay = false,
            State = NormalActionStates.WaitingForGame,
        };
        if (!_idempotency.TryAdd(IdempotencyScope, request.IdempotencyKey, fingerprint, result))
        {
            return GameCallResult<OwnedWorldResumeResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.IdempotencyCapacityExceeded,
                "The idempotency cache reached capacity after DSP accepted the resume.",
                false,
                "Do not retry with a new key; poll session state and the returned action ID."));
        }

        _actions[action.ActionId] = action;
        return GameCallResult<OwnedWorldResumeResult>.Succeeded(result);
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
            ActionKind = "resume-owned-game",
            State = NormalActionStates.WaitingForGame,
            Terminal = false,
            Succeeded = false,
            Message = action.ResumeSource == OwnedWorldResumeSourceKind.LastExit
                ? "DSP accepted the fixed LastExit load; Spherewright is validating the one-time owned-world provenance proof."
                : "DSP accepted the exact ticket-bound primary owned save because LastExit was not refreshed; Spherewright is validating the same one-time provenance proof.",
        };
        if (session.OwnedBySpherewright
            && session.LocalPlanetId == action.Ticket.ExpectedPlanetId
            && session.GameTick >= action.Ticket.MinimumGameTick)
        {
            result.SessionId = session.SessionId;
            result.PlanetId = session.LocalPlanetId;
            if (string.Equals(session.OwnedSaveState, OwnedSaveStates.Saved, StringComparison.Ordinal))
            {
                result.State = NormalActionStates.Completed;
                result.Terminal = true;
                result.Succeeded = true;
                result.Message = action.ResumeSource == OwnedWorldResumeSourceKind.LastExit
                    ? "The exact owned LastExit payload passed provenance checks and was resaved under its high-entropy Spherewright name."
                    : "The exact ticket-bound primary owned save passed provenance checks and was resaved under the same high-entropy Spherewright name.";
            }
            else
            {
                result.Message = "The exact owned payload was adopted and is waiting for its high-entropy normal save to complete.";
            }
        }
        else if (!string.IsNullOrWhiteSpace(_sessions.ResumeAdoptionError))
        {
            result.State = NormalActionStates.ActionFailed;
            result.Terminal = true;
            result.Message = _sessions.ResumeAdoptionError;
        }

        return true;
    }

    private static bool TryResolveResumeSource(
        OwnedWorldResumeTicket ticket,
        out OwnedWorldResumeSourceKind resumeSource,
        out string rejection)
    {
        resumeSource = OwnedWorldResumeSourceKind.None;
        rejection = string.Empty;
        if (IsLiveDspProcess(ticket.SourceProcessId))
        {
            rejection = "The source DSP process is still running; no restart source can yet prove a completed shutdown.";
            return false;
        }

        TryReadSaveEvidence(GameSave.LastExit, out var lastExitWrittenAt, out var lastExitGameTick);
        TryReadSaveEvidence(ticket.OwnedSaveName, out var ownedPrimaryWrittenAt, out var ownedPrimaryGameTick);
        resumeSource = OwnedWorldResumeSourceSelector.Select(
            !string.IsNullOrWhiteSpace(ticket.QuarantineActionId),
            ticket.MinimumGameTick,
            ticket.IssuedAtUtc,
            lastExitWrittenAt,
            lastExitGameTick,
            ownedPrimaryWrittenAt,
            ownedPrimaryGameTick,
            TimeSpan.FromSeconds(2));
        if (resumeSource == OwnedWorldResumeSourceKind.None)
        {
            rejection = string.IsNullOrWhiteSpace(ticket.QuarantineActionId)
                ? "The exact ticket-bound primary owned save is not fresh enough or its header is older than the planned-restart minimum game tick."
                : "DSP's fixed LastExit slot is not fresh enough or its header is older than the quarantine-recovery minimum game tick.";
            return false;
        }

        return true;
    }

    private static void TryReadSaveEvidence(
        string saveName,
        out DateTimeOffset? writtenAtUtc,
        out long? gameTick)
    {
        writtenAtUtc = null;
        gameTick = null;
        try
        {
            var path = GameSave.SavePath(saveName);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return;
            }

            GameSave.ReadHeader(saveName, false, out var header);
            if (header is null || header.gameTick < 0)
            {
                return;
            }

            writtenAtUtc = new DateTimeOffset(File.GetLastWriteTimeUtc(path), TimeSpan.Zero);
            gameTick = header.gameTick;
        }
        catch (Exception exception) when (
            exception is IOException
            || exception is UnauthorizedAccessException
            || exception is ArgumentException
            || exception is NotSupportedException)
        {
            writtenAtUtc = null;
            gameTick = null;
        }
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

    private static OwnedWorldResumeResult CloneAsReplay(OwnedWorldResumeResult result) =>
        new OwnedWorldResumeResult
        {
            ActionId = result.ActionId,
            Accepted = result.Accepted,
            IdempotentReplay = true,
            State = result.State,
        };

    private sealed class OwnedWorldResumePlanPayload
    {
        public OwnedWorldResumePlanPayload(
            OwnedWorldResumeTicket ticket,
            OwnedWorldResumeSourceKind resumeSource)
        {
            Ticket = ticket;
            ResumeSource = resumeSource;
            Fingerprint = CanonicalStateHash.Combine(
                "resume-owned-world",
                ticket.ResumeToken,
                ticket.OwnedSaveName,
                ticket.SourceSessionId,
                ticket.SourceProcessId,
                ticket.SourceBridgeInstanceId,
                ticket.GameVersion,
                ticket.ExpectedPlanetId,
                ticket.MinimumGameTick,
                ticket.QuarantineActionId,
                ticket.IssuedAtUtc,
                ticket.ExpiresAtUtc,
                resumeSource);
        }

        public OwnedWorldResumeTicket Ticket { get; }

        public OwnedWorldResumeSourceKind ResumeSource { get; }

        public string Fingerprint { get; }
    }

    private sealed class OwnedWorldResumeAction
    {
        public string ActionId { get; set; } = string.Empty;

        public OwnedWorldResumeTicket Ticket { get; set; } = null!;

        public OwnedWorldResumeSourceKind ResumeSource { get; set; }
    }
}
