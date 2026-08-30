using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Actions;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Sessions;
using Spherewright.Contracts.Testing;

namespace Spherewright.Plugin.Game;

internal sealed class TestWorldCoordinator
{
    private readonly bool _writesConfigured;
    private readonly GameSessionTracker _sessions;
    private readonly PreparedPlanStore<TestWorldPlanPayload> _plans;
    private readonly IdempotencyCache<TestWorldCreationResult> _idempotency;
    private readonly Dictionary<string, TestWorldCreationResult> _actions =
        new Dictionary<string, TestWorldCreationResult>(StringComparer.Ordinal);

    public TestWorldCoordinator(
        bool writesConfigured,
        int planLifetimeSeconds,
        int idempotencyCapacity,
        GameSessionTracker sessions)
    {
        _writesConfigured = writesConfigured;
        _sessions = sessions;
        _plans = new PreparedPlanStore<TestWorldPlanPayload>(
            TimeSpan.FromSeconds(planLifetimeSeconds),
            16);
        _idempotency = new IdempotencyCache<TestWorldCreationResult>(idempotencyCapacity);
    }

    public GameCallResult<PreparedTestWorldPlan> PrepareOnMainThread(PrepareTestWorldRequest request)
    {
        if (request.GalaxySeed < 0 || request.GalaxySeed > 99999999)
        {
            return GameCallResult<PreparedTestWorldPlan>.Failed(BridgeError.Create(
                BridgeErrorCodes.InvalidRequest,
                "Galaxy seed must be between 0 and 99999999.",
                false,
                "Choose an eight-digit non-negative seed."));
        }

        if (request.StarCount < 20 || request.StarCount > 80)
        {
            return GameCallResult<PreparedTestWorldPlan>.Failed(BridgeError.Create(
                BridgeErrorCodes.InvalidRequest,
                "Star count must be between 20 and 80.",
                false,
                "Choose a supported star count."));
        }

        var readinessError = ValidateMainMenuReady();
        if (readinessError is not null)
        {
            return GameCallResult<PreparedTestWorldPlan>.Failed(readinessError);
        }

        var saveName = $"Spherewright_M0_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}";
        var payload = new TestWorldPlanPayload(saveName, request.GalaxySeed, request.StarCount);
        PreparedPlan<TestWorldPlanPayload> plan;
        try
        {
            plan = _plans.Add(payload.Fingerprint, payload);
        }
        catch (InvalidOperationException)
        {
            return GameCallResult<PreparedTestWorldPlan>.Failed(BridgeError.Create(
                BridgeErrorCodes.ServerBusy,
                "Too many unconsumed new-world plans are active.",
                true,
                "Wait for existing plans to expire and retry."));
        }

        var result = new PreparedTestWorldPlan
        {
            PlanToken = plan.Token,
            ExpiresAtUtc = plan.ExpiresAtUtc,
            SaveName = saveName,
            GalaxySeed = request.GalaxySeed,
            StarCount = request.StarCount,
            ResourceMultiplier = 1f,
            PeacefulMode = true,
            SandboxMode = false,
            CommitAllowed = _writesConfigured,
        };
        result.Warnings.Add("This plan creates a standard peaceful 1x world with DSP sandbox tools disabled.");
        if (!_writesConfigured)
        {
            result.Warnings.Add("Commit is blocked because Safety.AllowWrites is false.");
        }

        return GameCallResult<PreparedTestWorldPlan>.Succeeded(result);
    }

    public GameCallResult<TestWorldCreationResult> CommitOnMainThread(CommitTestWorldRequest request)
    {
        if (!Guid.TryParse(request.IdempotencyKey, out _))
        {
            return GameCallResult<TestWorldCreationResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.InvalidRequest,
                "A UUID idempotency key is required.",
                false,
                "Generate one UUID and reuse it for retries of this exact commit."));
        }

        var fingerprint = "commit-new-world|" + request.PlanToken;
        if (_idempotency.TryGet(request.IdempotencyKey, fingerprint, out var replay, out var conflict))
        {
            return GameCallResult<TestWorldCreationResult>.Succeeded(CloneAsReplay(replay!));
        }

        if (conflict)
        {
            return GameCallResult<TestWorldCreationResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.IdempotencyConflict,
                "The idempotency key is already bound to a different request.",
                false,
                "Use the original request or generate a new idempotency key."));
        }

        if (!_writesConfigured)
        {
            return GameCallResult<TestWorldCreationResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.WritesDisabled,
                "New-world creation is blocked because Safety.AllowWrites is false.",
                false,
                "Enable writes in the Spherewright Plugin config, restart DSP, prepare a new plan, and retry."));
        }

        if (!_plans.TryTake(request.PlanToken, out var plan, out var expired))
        {
            return GameCallResult<TestWorldCreationResult>.Failed(BridgeError.Create(
                expired ? BridgeErrorCodes.PlanExpired : BridgeErrorCodes.PlanNotFound,
                expired ? "The new-world plan expired." : "The new-world plan was not found or was already consumed.",
                true,
                "Prepare a fresh new-world plan and commit it once."));
        }

        var readinessError = ValidateMainMenuReady();
        if (readinessError is not null)
        {
            return GameCallResult<TestWorldCreationResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.ActionRejected,
                "Game or main-menu loader state changed after prepare; new-world creation is no longer safe to start.",
                true,
                "Wait for the main menu to become idle, then prepare a fresh plan."));
        }

        var payload = plan!.Payload;
        try
        {
            _sessions.ExpectNextSessionToBeOwned(payload.SaveName);
            var descriptor = new GameDesc();
            descriptor.SetForNewGame(
                UniverseGen.algoVersion,
                payload.GalaxySeed,
                payload.StarCount,
                1,
                1f);
            descriptor.isPeaceMode = true;
            descriptor.isSandboxMode = false;
            descriptor.goalLevel = EGoalLevel.Off;
            descriptor.combatSettings.SetDefault();
            DSPGame.StartGameSkipPrologue(descriptor);
        }
        catch (Exception exception)
        {
            _sessions.CancelExpectedOwnedSession();
            return GameCallResult<TestWorldCreationResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.ActionFailed,
                $"The game rejected ordinary peaceful new-world creation: {exception.GetType().Name}: {exception.Message}",
                false,
                "Inspect the local Spherewright and Unity logs before preparing another plan."));
        }

        var result = new TestWorldCreationResult
        {
            ActionId = Guid.NewGuid().ToString("D"),
            Accepted = true,
            IdempotentReplay = false,
            SaveName = payload.SaveName,
            GalaxySeed = payload.GalaxySeed,
            StarCount = payload.StarCount,
            State = OwnedSaveStates.WaitingForWorld,
        };
        if (!_idempotency.TryAdd(request.IdempotencyKey, fingerprint, result))
        {
            return GameCallResult<TestWorldCreationResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.IdempotencyCapacityExceeded,
                "The idempotency cache reached its configured capacity after the action started.",
                false,
                "Do not retry with a new key; poll session state for the accepted world creation."));
        }

        _actions[result.ActionId] = result;

        return GameCallResult<TestWorldCreationResult>.Succeeded(result);
    }

    public GameCallResult<ActionResultSnapshot> GetActionResultOnMainThread(GetActionResultRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ActionId) || !_actions.TryGetValue(request.ActionId, out var action))
        {
            return GameCallResult<ActionResultSnapshot>.Failed(BridgeError.Create(
                BridgeErrorCodes.ActionNotFound,
                "The requested action is not retained by this Plugin process.",
                false,
                "Use the actionId returned by a commit accepted during the current Plugin process."));
        }

        var session = _sessions.CaptureOnMainThread();
        var result = new ActionResultSnapshot
        {
            ActionId = action.ActionId,
            ActionKind = "new-game",
            State = "executing",
            Terminal = false,
            Succeeded = false,
        };
        if (session.OwnedBySpherewright
            && string.Equals(session.SaveName, action.SaveName, StringComparison.Ordinal))
        {
            result.SessionId = session.SessionId;
            result.PlanetId = session.LocalPlanetId;
            if (string.Equals(session.OwnedSaveState, OwnedSaveStates.Saved, StringComparison.Ordinal))
            {
                result.State = "completed";
                result.Terminal = true;
                result.Succeeded = true;
                result.Message = "The ordinary peaceful world loaded and was saved under its Spherewright-owned name.";
            }
            else if (string.Equals(session.OwnedSaveState, OwnedSaveStates.SaveFailed, StringComparison.Ordinal))
            {
                result.State = "failed";
                result.Terminal = true;
                result.Message = "The ordinary world loaded, but its initial owned save could not be persisted.";
            }
            else
            {
                result.Message = "The owned ordinary world is loading or waiting for its initial save.";
            }
        }
        else
        {
            result.Message = "DSP accepted the new-game action and the Plugin is waiting to adopt the exact GameData instance.";
        }

        return GameCallResult<ActionResultSnapshot>.Succeeded(result);
    }

    private static TestWorldCreationResult CloneAsReplay(TestWorldCreationResult result)
    {
        return new TestWorldCreationResult
        {
            ActionId = result.ActionId,
            Accepted = result.Accepted,
            IdempotentReplay = true,
            SaveName = result.SaveName,
            GalaxySeed = result.GalaxySeed,
            StarCount = result.StarCount,
            State = result.State,
        };
    }

    private static BridgeError? ValidateMainMenuReady()
    {
        var hasGameObject = GameMain.data is not null || GameMain.isRunning || DSPGame.Game != null;
        if (hasGameObject && !DSPGame.IsMenuDemo)
        {
            return BridgeError.Create(
                BridgeErrorCodes.ActionRejected,
                "A Spherewright-owned new world can only be created with no loaded or loading game.",
                false,
                "Return to the main menu without loading a save, then retry.");
        }

        var modelArray = LDB.models?.modelArray;
        if (!VFPreload.done
            || !VFPreload.dbDone
            || modelArray is null
            || modelArray.Length == 0)
        {
            return BridgeError.Create(
                BridgeErrorCodes.BridgeNotReady,
                "DSP prototype and model preloading has not completed yet.",
                true,
                "Wait for the DSP startup preload to finish, then retry.");
        }

        if (UIRoot.instance is null
            || UIRoot.instance.uiMainMenu is null
            || !UIRoot.instance.uiMainMenu.active
            || UnityEngine.Object.FindObjectOfType<GameLoader>() != null)
        {
            return BridgeError.Create(
                BridgeErrorCodes.BridgeNotReady,
                "The DSP main menu is still initializing or another game loader is active.",
                true,
                "Wait for the main menu to finish loading, then retry.");
        }

        return null;
    }

    private sealed class TestWorldPlanPayload
    {
        public TestWorldPlanPayload(string saveName, int galaxySeed, int starCount)
        {
            SaveName = saveName;
            GalaxySeed = galaxySeed;
            StarCount = starCount;
            Fingerprint = $"new-world|{saveName}|{galaxySeed}|{starCount}|peaceful|standard|resources-1x";
        }

        public string SaveName { get; }

        public int GalaxySeed { get; }

        public int StarCount { get; }

        public string Fingerprint { get; }
    }
}
