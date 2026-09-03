using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Actions;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Journals;
using Spherewright.Contracts.Sessions;

namespace Spherewright.Plugin.Game;

internal sealed class UserSaveImportCoordinator
{
    private const string ConfirmationPrompt =
        "Please confirm in this conversation: import the currently loaded world as a new Spherewright-managed copy. "
        + "The original save will not be overwritten, renamed, or deleted. "
        + "The gameplay journal begins at the import point and will not reconstruct earlier first-time events. Shall I continue?";

    private readonly bool _enabled;
    private readonly bool _writesConfigured;
    private readonly GameSessionTracker _sessions;
    private readonly PreparedPlanStore<UserSaveImportPlanPayload> _plans;
    private readonly IdempotencyCache<UserSaveImportResult> _idempotency;
    private readonly Dictionary<string, ActionResultSnapshot> _actions =
        new Dictionary<string, ActionResultSnapshot>(StringComparer.Ordinal);

    public UserSaveImportCoordinator(
        bool enabled,
        bool writesConfigured,
        int planLifetimeSeconds,
        int idempotencyRetentionMinutes,
        int idempotencyCapacity,
        GameSessionTracker sessions)
    {
        _enabled = enabled;
        _writesConfigured = writesConfigured;
        _sessions = sessions;
        _plans = new PreparedPlanStore<UserSaveImportPlanPayload>(
            TimeSpan.FromSeconds(planLifetimeSeconds),
            8);
        _idempotency = new IdempotencyCache<UserSaveImportResult>(
            idempotencyCapacity,
            TimeSpan.FromMinutes(idempotencyRetentionMinutes));
    }

    public GameCallResult<PreparedUserSaveImportPlan> PrepareOnMainThread(
        string? requestedSessionId,
        PrepareUserSaveImportRequest request)
    {
        if (!_enabled)
        {
            return GameCallResult<PreparedUserSaveImportPlan>.Failed(BridgeError.Create(
                BridgeErrorCodes.ActionRejected,
                "User-save import is disabled by configuration.",
                false,
                "Set Safety.AllowUserSaveImport to true and restart DSP before preparing an import."));
        }

        if (!_writesConfigured)
        {
            return GameCallResult<PreparedUserSaveImportPlan>.Failed(BridgeError.Create(
                BridgeErrorCodes.WritesDisabled,
                "User-save import is blocked because Safety.AllowWrites is false.",
                false,
                "Enable writes and restart DSP before preparing an import."));
        }

        if (!_sessions.TryGetCurrentUnownedImportCandidateOnMainThread(
                requestedSessionId,
                out var candidateData,
                out var candidateRejection)
            || candidateData is null)
        {
            return GameCallResult<PreparedUserSaveImportPlan>.Failed(BridgeError.Create(
                BridgeErrorCodes.SessionNotOwned,
                candidateRejection,
                true,
                "Manually load the intended save, read its restricted session state, and prepare the exact session."));
        }

        if (string.Equals(_sessions.WriteHealth, WriteHealthStates.Quarantined, StringComparison.Ordinal))
        {
            return GameCallResult<PreparedUserSaveImportPlan>.Failed(BridgeError.Create(
                BridgeErrorCodes.WriteSubsystemQuarantined,
                "Save import is quarantined after an earlier unproved copy outcome in this loaded session.",
                false,
                "Do not retry the import in this session; inspect the retained action and manually reload the intended original world before starting a new flow."));
        }

        if (request.ExpectedRevision != _sessions.Revision)
        {
            return GameCallResult<PreparedUserSaveImportPlan>.Failed(BridgeError.Create(
                BridgeErrorCodes.StaleRevision,
                "The unowned session revision changed after it was inspected.",
                true,
                "Read restricted session state and prepare a fresh plan for its exact revision."));
        }

        var generatedSaveName = $"Spherewright_Imported_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}";
        var payload = new UserSaveImportPlanPayload(
            requestedSessionId!,
            request.ExpectedRevision,
            candidateData,
            generatedSaveName);
        PreparedPlan<UserSaveImportPlanPayload> plan;
        try
        {
            plan = _plans.Add(payload.Fingerprint, payload);
        }
        catch (InvalidOperationException)
        {
            return GameCallResult<PreparedUserSaveImportPlan>.Failed(BridgeError.Create(
                BridgeErrorCodes.ServerBusy,
                "Too many unconsumed save-import plans are active.",
                true,
                "Wait for existing plans to expire, then prepare this exact loaded session again."));
        }

        return GameCallResult<PreparedUserSaveImportPlan>.Succeeded(new PreparedUserSaveImportPlan
        {
            Prepared = true,
            PlanToken = plan.Token,
            ExpiresAtUtc = plan.ExpiresAtUtc,
            ExpectedRevision = request.ExpectedRevision,
            OriginalSavePreserved = true,
            JournalTrackingMode = GameplayJournalTrackingModes.AttachedExistingSave,
            HistoricalCoverageComplete = false,
            UserConfirmationRequired = true,
            ConfirmationPrompt = ConfirmationPrompt,
            CommitAllowedNow = false,
            CommitBlockers = new List<WriteBlocker>
            {
                new WriteBlocker
                {
                    Code = BridgeErrorCodes.UserConfirmationRequired,
                    Message = "A subsequent explicit confirmation from the user in the current conversation is required.",
                },
            },
            CompletionCondition = "After a subsequent explicit user confirmation, DSP's normal save API creates a new internally named copy and its exact header tick must be reread before the current GameData becomes Spherewright-owned. The original save is never addressed or overwritten, and journal coverage starts at import.",
        });
    }

    public GameCallResult<UserSaveImportResult> CommitOnMainThread(
        string? requestedSessionId,
        CommitUserSaveImportRequest request)
    {
        if (!Guid.TryParse(request.IdempotencyKey, out _))
        {
            return GameCallResult<UserSaveImportResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.InvalidRequest,
                "A UUID idempotency key is required.",
                false,
                "Generate one UUID and reuse it for retries of this exact import commit."));
        }

        if (string.IsNullOrWhiteSpace(requestedSessionId))
        {
            return GameCallResult<UserSaveImportResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.StaleSession,
                "The import commit requires the exact prepared session ID.",
                true,
                "Read restricted session state and repeat the confirmation flow for that exact session."));
        }

        var fingerprint = CanonicalStateHash.Combine(
            "commit-user-save-import",
            requestedSessionId,
            request.PlanToken);
        if (_idempotency.TryGet(
            requestedSessionId!,
            request.IdempotencyKey,
            fingerprint,
            out var replay,
            out var conflict))
        {
            return GameCallResult<UserSaveImportResult>.Succeeded(CloneAsReplay(replay!));
        }

        if (conflict)
        {
            return GameCallResult<UserSaveImportResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.IdempotencyConflict,
                "The idempotency key is already bound to another save-import request.",
                false,
                "Reuse it only for the original import commit."));
        }

        if (!_enabled)
        {
            return GameCallResult<UserSaveImportResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.ActionRejected,
                "User-save import is disabled by configuration.",
                false,
                "Enable it, restart DSP, and repeat the explicit conversation-confirmation flow."));
        }

        if (!_writesConfigured)
        {
            return GameCallResult<UserSaveImportResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.WritesDisabled,
                "User-save import is blocked because Safety.AllowWrites is false.",
                false,
                "Enable writes, restart DSP, and repeat the explicit conversation-confirmation flow."));
        }

        if (string.Equals(_sessions.WriteHealth, WriteHealthStates.Quarantined, StringComparison.Ordinal))
        {
            return GameCallResult<UserSaveImportResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.WriteSubsystemQuarantined,
                "Save import is quarantined after an earlier unproved copy outcome in this loaded session.",
                false,
                "Do not retry with a new key; inspect the retained action and manually reload the intended original world before starting a new flow."));
        }

        if (!_idempotency.HasCapacity(requestedSessionId!))
        {
            return GameCallResult<UserSaveImportResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.IdempotencyCapacityExceeded,
                "The idempotency cache has no capacity for another import action.",
                false,
                "Restart the Plugin before preparing another import; no save was attempted."));
        }

        if (!_plans.TryGet(request.PlanToken, out var preparedPlan, out var preparedPlanExpired)
            || preparedPlan is null)
        {
            return GameCallResult<UserSaveImportResult>.Failed(BridgeError.Create(
                preparedPlanExpired ? BridgeErrorCodes.PlanExpired : BridgeErrorCodes.PlanNotFound,
                preparedPlanExpired ? "The save-import plan expired." : "The save-import plan was not found or was already consumed.",
                true,
                "Prepare a fresh import plan and obtain a new explicit confirmation in the conversation."));
        }

        if (!string.Equals(preparedPlan.Payload.SessionId, requestedSessionId, StringComparison.Ordinal))
        {
            return GameCallResult<UserSaveImportResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.StaleSession,
                "The import plan belongs to another loaded-world session.",
                false,
                "Do not reuse the plan; prepare and confirm the current loaded session."));
        }

        if (!UserSaveImportConfirmationPolicy.IsCommitDeclared(
                request.UserConfirmedInConversation,
                request.AcknowledgeOriginalSaveRemainsUnchanged,
                request.AcknowledgeJournalStartsAtImport))
        {
            return GameCallResult<UserSaveImportResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.UserConfirmationRequired,
                "The import requires a subsequent explicit user confirmation in the current conversation plus both boundary acknowledgements.",
                false,
                ConfirmationPrompt));
        }

        if (!_plans.TryTake(request.PlanToken, out var plan, out var expired) || plan is null)
        {
            return GameCallResult<UserSaveImportResult>.Failed(BridgeError.Create(
                expired ? BridgeErrorCodes.PlanExpired : BridgeErrorCodes.PlanNotFound,
                expired ? "The save-import plan expired." : "The save-import plan was already consumed.",
                true,
                "Prepare a fresh import plan and obtain a new explicit confirmation in the conversation."));
        }

        var payload = plan.Payload;
        var readinessError = ValidateConfirmedWorldOnMainThread(
            payload.SessionId,
            payload.Revision,
            payload.Data);
        if (readinessError is not null)
        {
            return GameCallResult<UserSaveImportResult>.Failed(readinessError);
        }

        var planetId = payload.Data.localPlanet!.id;
        var actionId = Guid.NewGuid().ToString("D");
        var accepted = new UserSaveImportResult
        {
            ActionId = actionId,
            Accepted = true,
            IdempotentReplay = false,
            State = NormalActionStates.Executing,
            OriginalSavePreserved = true,
            JournalTrackingMode = GameplayJournalTrackingModes.AttachedExistingSave,
            HistoricalCoverageComplete = false,
        };
        if (!_idempotency.TryAdd(requestedSessionId!, request.IdempotencyKey, fingerprint, accepted))
        {
            return GameCallResult<UserSaveImportResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.IdempotencyCapacityExceeded,
                "The idempotency cache reached capacity before the save attempt.",
                false,
                "Do not retry with a new key until the Plugin is restarted."));
        }

        var action = new ActionResultSnapshot
        {
            ActionId = actionId,
            ActionKind = NormalActionKinds.UserSaveImport,
            State = NormalActionStates.Executing,
            Terminal = false,
            Succeeded = false,
            SessionId = payload.SessionId,
            PlanetId = planetId,
            IdempotencyKey = request.IdempotencyKey,
            StartedAtGameTick = GameMain.gameTick,
            Message = "The explicitly confirmed normal-save copy is being created and verified.",
        };
        _actions.Add(actionId, action);

        var imported = _sessions.TryImportCurrentSessionAsOwnedCopyOnMainThread(
            payload.SessionId,
            payload.Revision,
            payload.Data,
            payload.GeneratedSaveName,
            actionId,
            out var savedGameTick,
            out var outcomeUnknown,
            out var rejection);
        action.CompletedAtGameTick = GameMain.gameTick;
        action.Terminal = true;
        action.Succeeded = imported;
        action.State = imported
            ? NormalActionStates.Completed
            : outcomeUnknown
                ? NormalActionStates.OutcomeUnknown
                : NormalActionStates.ActionFailed;
        action.Message = imported
            ? "The explicitly confirmed world was normally saved under a new internal owned identity, its exact header tick was proved, and the original save remained unchanged."
            : rejection;

        accepted.State = action.State;
        accepted.SessionId = imported ? payload.SessionId : null;
        accepted.PlanetId = imported ? planetId : null;
        accepted.SavedGameTick = imported ? savedGameTick : null;
        return GameCallResult<UserSaveImportResult>.Succeeded(CloneAsReplay(accepted, false));
    }

    public bool TryGetActionResultOnMainThread(string actionId, out ActionResultSnapshot? result)
    {
        if (_actions.TryGetValue(actionId, out var found))
        {
            result = found;
            return true;
        }

        result = null;
        return false;
    }

    private BridgeError? ValidateConfirmedWorldOnMainThread(
        string sessionId,
        long revision,
        GameData expectedData)
    {
        if (!_sessions.TryGetCurrentUnownedImportCandidateOnMainThread(sessionId, out var currentData, out var rejection)
            || currentData is null
            || !ReferenceEquals(currentData, expectedData))
        {
            return BridgeError.Create(
                BridgeErrorCodes.StaleSession,
                rejection,
                true,
                "The confirmation cannot cross a loaded-world change; prepare the current session and ask again.");
        }

        if (_sessions.Revision != revision)
        {
            return BridgeError.Create(
                BridgeErrorCodes.StaleRevision,
                "The exact unowned session revision changed after prepare.",
                true,
                "Prepare a fresh plan for the current revision and ask for confirmation again.");
        }

        if (UnityEngine.Object.FindObjectOfType<GameLoader>() is not null
            || currentData.localPlanet is null
            || currentData.localLoadedPlanetFactory is null)
        {
            return BridgeError.Create(
                BridgeErrorCodes.BridgeNotReady,
                "The confirmed world is loading or has no ready local factory.",
                true,
                "Wait until the world is stable, then prepare and confirm again.");
        }

        var descriptor = currentData.gameDesc;
        if (descriptor is null)
        {
            return BridgeError.Create(
                BridgeErrorCodes.PeacefulModeUnknown,
                "The confirmed world has no readable game descriptor.",
                false,
                "Do not import this world.");
        }

        if (!descriptor.isPeaceMode)
        {
            return BridgeError.Create(
                BridgeErrorCodes.PeacefulModeRequired,
                "Only a confirmed peaceful world can become Spherewright-owned.",
                false,
                "Load a peaceful world manually, then prepare and confirm it.");
        }

        if (descriptor.isSandboxMode || GameMain.sandboxToolsEnabled)
        {
            return BridgeError.Create(
                BridgeErrorCodes.SandboxModeActive,
                "Sandbox mode or sandbox tools are active in the confirmed world.",
                false,
                "Use a non-sandbox save, then prepare and confirm it.");
        }

        if (Math.Abs(descriptor.resourceMultiplier - 1f) > 0.0001f)
        {
            return BridgeError.Create(
                BridgeErrorCodes.NormalResourceMultiplierRequired,
                "Only a normal 1x-resource world can become Spherewright-owned.",
                false,
                "Load a 1x-resource world manually, then prepare and confirm it.");
        }

        return null;
    }

    private static UserSaveImportResult CloneAsReplay(UserSaveImportResult result, bool replay = true) =>
        new UserSaveImportResult
        {
            ActionId = result.ActionId,
            Accepted = result.Accepted,
            IdempotentReplay = replay,
            State = result.State,
            SessionId = result.SessionId,
            PlanetId = result.PlanetId,
            SavedGameTick = result.SavedGameTick,
            OriginalSavePreserved = result.OriginalSavePreserved,
            JournalTrackingMode = result.JournalTrackingMode,
            HistoricalCoverageComplete = result.HistoricalCoverageComplete,
        };

    private sealed class UserSaveImportPlanPayload
    {
        public UserSaveImportPlanPayload(
            string sessionId,
            long revision,
            GameData data,
            string generatedSaveName)
        {
            SessionId = sessionId;
            Revision = revision;
            Data = data;
            GeneratedSaveName = generatedSaveName;
            Fingerprint = CanonicalStateHash.Combine(
                "user-save-import",
                sessionId,
                revision,
                generatedSaveName);
        }

        public string SessionId { get; }

        public long Revision { get; }

        public GameData Data { get; }

        public string GeneratedSaveName { get; }

        public string Fingerprint { get; }
    }
}
