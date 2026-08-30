using System.Globalization;
using BepInEx.Logging;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Factory;
using Spherewright.Contracts.Sessions;
using UnityEngine;

namespace Spherewright.Plugin.Game;

internal sealed class BasicProductionLineCoordinator
{
    private const int MaximumInputItems = 100;
    private readonly bool _writesConfigured;
    private readonly GameSessionTracker _sessions;
    private readonly PreparedPlanStore<LinePlanPayload> _plans;
    private readonly IdempotencyCache<BasicProductionLineResult> _idempotency;
    private readonly Dictionary<string, LineRecord> _records = new Dictionary<string, LineRecord>(StringComparer.Ordinal);
    private readonly ManualLogSource _logger;

    public BasicProductionLineCoordinator(
        bool writesConfigured,
        int planLifetimeSeconds,
        int idempotencyCapacity,
        GameSessionTracker sessions,
        ManualLogSource logger)
    {
        _writesConfigured = writesConfigured;
        _sessions = sessions;
        _plans = new PreparedPlanStore<LinePlanPayload>(TimeSpan.FromSeconds(planLifetimeSeconds), 16);
        _idempotency = new IdempotencyCache<BasicProductionLineResult>(idempotencyCapacity);
        _logger = logger;
    }

    public GameCallResult<BasicProductionLinePlan> PrepareOnMainThread(
        string? requestedSessionId,
        PrepareBasicProductionLineRequest request)
    {
        var accessError = ValidateOwnedSessionOnMainThread(
            requestedSessionId,
            request.ExpectedRevision,
            requireCurrentRevision: true,
            out var state,
            out var factory,
            out var player);
        if (accessError is not null)
        {
            return GameCallResult<BasicProductionLinePlan>.Failed(accessError);
        }

        if (request.InputItemCount < 1 || request.InputItemCount > MaximumInputItems)
        {
            return GameCallResult<BasicProductionLinePlan>.Failed(BridgeError.Create(
                BridgeErrorCodes.InvalidRequest,
                $"Input item count must be between 1 and {MaximumInputItems}.",
                false,
                "Choose a bounded test input quantity and retry."));
        }

        if (!GameMain.sandboxToolsEnabled)
        {
            return GameCallResult<BasicProductionLinePlan>.Failed(BridgeError.Create(
                BridgeErrorCodes.ActionRejected,
                "The basic-line bootstrap requires DSP sandbox tools in the dedicated test world.",
                false,
                "Create a fresh Spherewright sandbox test world and retry."));
        }

        var selection = SelectBasicLineParts();
        if (selection is null)
        {
            return GameCallResult<BasicProductionLinePlan>.Failed(BridgeError.Create(
                BridgeErrorCodes.ActionRejected,
                "The current runtime prototypes do not contain a complete basic smelting line.",
                false,
                "Inspect spherewright_get_build_catalog and the local game-version research log."));
        }

        if (!TryFindLayout(factory!, player!, selection, out var layout, out var rejection))
        {
            return GameCallResult<BasicProductionLinePlan>.Failed(BridgeError.Create(
                BridgeErrorCodes.ActionRejected,
                "No buildable basic-line layout was found inside the player's current construction radius. " + rejection,
                true,
                "Move only within the dedicated test world to a clearer area, then prepare a fresh plan."));
        }

        var payload = new LinePlanPayload(
            state!.SessionId!,
            state.Revision,
            request.InputItemCount,
            selection,
            layout!);
        PreparedPlan<LinePlanPayload> plan;
        try
        {
            plan = _plans.Add(payload.Fingerprint, payload);
        }
        catch (InvalidOperationException)
        {
            return GameCallResult<BasicProductionLinePlan>.Failed(BridgeError.Create(
                BridgeErrorCodes.ServerBusy,
                "Too many unconsumed basic-line plans are active.",
                true,
                "Wait for existing plans to expire and prepare again."));
        }

        var result = new BasicProductionLinePlan
        {
            PlanToken = plan.Token,
            ExpiresAtUtc = plan.ExpiresAtUtc,
            SessionId = payload.SessionId,
            ExpectedRevision = payload.Revision,
            DryRun = true,
            CommitAllowed = _writesConfigured && state.WritesAllowed,
            RecipeId = selection.Recipe.ID,
            RecipeName = selection.Recipe.name ?? string.Empty,
            InputItemId = selection.Recipe.Items[0],
            InputItemName = LDB.items.Select(selection.Recipe.Items[0])?.name ?? string.Empty,
            InputItemCount = request.InputItemCount,
            OutputItemId = selection.Recipe.Results[0],
            OutputItemName = LDB.items.Select(selection.Recipe.Results[0])?.name ?? string.Empty,
        };
        AddPlanBuilding(result, "input-storage", selection.Storage, layout!.InputStoragePosition);
        AddPlanBuilding(result, "smelter", selection.Smelter, layout.SmelterPosition);
        AddPlanBuilding(result, "output-storage", selection.Storage, layout.OutputStoragePosition);
        AddPlanBuilding(result, "input-inserter", selection.Inserter, layout.InputInserterPosition);
        AddPlanBuilding(result, "output-inserter", selection.Inserter, layout.OutputInserterPosition);
        AddPlanBuilding(result, "wind-power", selection.PowerGenerator, layout.PowerPosition);
        result.Warnings.Add("Dry-run only: no inventory, factory, terrain, recipe, or save state was changed.");
        result.Warnings.Add("Commit grants exactly the six sandbox test buildings, consumes them through DSP's normal build flow, and injects only the requested recipe input into the new input storage.");
        if (!result.CommitAllowed)
        {
            result.Warnings.Add("Commit is currently blocked by the write or peaceful-save safety gate.");
        }

        return GameCallResult<BasicProductionLinePlan>.Succeeded(result);
    }

    public GameCallResult<BasicProductionLineResult> CommitOnMainThread(
        string? requestedSessionId,
        CommitBasicProductionLineRequest request)
    {
        if (!Guid.TryParse(request.IdempotencyKey, out _))
        {
            return GameCallResult<BasicProductionLineResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.InvalidRequest,
                "A UUID idempotency key is required.",
                false,
                "Generate one UUID and reuse it for retries of this exact commit."));
        }

        var fingerprint = "commit-basic-line|" + request.PlanToken;
        if (_idempotency.TryGet(request.IdempotencyKey, fingerprint, out var replay, out var conflict))
        {
            return GameCallResult<BasicProductionLineResult>.Succeeded(CloneAsReplay(replay!));
        }

        if (conflict)
        {
            return GameCallResult<BasicProductionLineResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.IdempotencyConflict,
                "The idempotency key is already bound to a different request.",
                false,
                "Use the original request or generate a new idempotency key."));
        }

        if (!_writesConfigured)
        {
            return GameCallResult<BasicProductionLineResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.WritesDisabled,
                "Basic-line construction is blocked because Safety.AllowWrites is false.",
                false,
                "Enable writes, restart DSP, create a fresh owned test world, and prepare again."));
        }

        if (!_plans.TryTake(request.PlanToken, out var prepared, out var expired))
        {
            return GameCallResult<BasicProductionLineResult>.Failed(BridgeError.Create(
                expired ? BridgeErrorCodes.PlanExpired : BridgeErrorCodes.PlanNotFound,
                expired ? "The basic-line plan expired." : "The basic-line plan was not found or was already consumed.",
                true,
                "Prepare a fresh basic-line plan and commit it once."));
        }

        var payload = prepared!.Payload;
        if (!string.Equals(requestedSessionId, payload.SessionId, StringComparison.Ordinal))
        {
            return GameCallResult<BasicProductionLineResult>.Failed(StaleSession());
        }

        var accessError = ValidateOwnedSessionOnMainThread(
            requestedSessionId,
            payload.Revision,
            requireCurrentRevision: true,
            out var state,
            out var factory,
            out var player);
        if (accessError is not null)
        {
            return GameCallResult<BasicProductionLineResult>.Failed(accessError);
        }

        if (!state!.WritesAllowed)
        {
            return GameCallResult<BasicProductionLineResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.PeacefulModeRequired,
                "The active owned session no longer satisfies the peaceful write gate.",
                false,
                "Do not write this session; create a fresh peaceful Spherewright test world."));
        }

        if (!GameMain.sandboxToolsEnabled)
        {
            return GameCallResult<BasicProductionLineResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.ActionRejected,
                "DSP sandbox tools are no longer enabled for the prepared test-world action.",
                false,
                "Create a fresh Spherewright sandbox test world and prepare again."));
        }

        if (!RevalidatePlan(factory!, player!, payload, out var rejection))
        {
            return GameCallResult<BasicProductionLineResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.ActionRejected,
                "The prepared build area changed before commit. " + rejection,
                true,
                "Prepare a fresh layout from the current state."));
        }

        if (player!.inhandItemId != 0 || player.inhandItemCount != 0)
        {
            return GameCallResult<BasicProductionLineResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.ActionRejected,
                "The player is holding an item, so an exact sandbox inventory transaction cannot be guaranteed.",
                true,
                "Put the held item away in the dedicated test world, then prepare a fresh plan."));
        }

        var activeFactory = factory!;
        var activePlayer = player;

        var touchedItemIds = new[]
        {
            payload.Selection.Storage.ID,
            payload.Selection.Smelter.ID,
            payload.Selection.Inserter.ID,
            payload.Selection.PowerGenerator.ID,
            payload.Selection.Recipe.Items[0],
        }.Distinct().ToArray();
        var packageBaseline = touchedItemIds.ToDictionary(itemId => itemId, itemId => activePlayer.package.GetItemCount(itemId));
        var createdEntities = new List<int>();
        var mutationStarted = false;

        try
        {
            var inputStorage = BuildCoreEntity(activeFactory, activePlayer, payload.Selection.Storage, payload.Layout.InputStoragePosition, payload.Layout.InputStorageRotation);
            mutationStarted = true;
            createdEntities.Add(inputStorage);
            var assembler = BuildCoreEntity(activeFactory, activePlayer, payload.Selection.Smelter, payload.Layout.SmelterPosition, payload.Layout.SmelterRotation);
            createdEntities.Add(assembler);
            var outputStorage = BuildCoreEntity(activeFactory, activePlayer, payload.Selection.Storage, payload.Layout.OutputStoragePosition, payload.Layout.OutputStorageRotation);
            createdEntities.Add(outputStorage);
            var power = BuildCoreEntity(activeFactory, activePlayer, payload.Selection.PowerGenerator, payload.Layout.PowerPosition, payload.Layout.PowerRotation);
            createdEntities.Add(power);

            var inputInserter = BuildInserter(activeFactory, activePlayer, payload.Selection.Inserter, inputStorage, assembler);
            createdEntities.Add(inputInserter);
            var outputInserter = BuildInserter(activeFactory, activePlayer, payload.Selection.Inserter, assembler, outputStorage);
            createdEntities.Add(outputInserter);

            SetAndVerifyRecipe(activeFactory, assembler, payload.Selection.Recipe);
            var inputItemId = payload.Selection.Recipe.Items[0];
            var beforeInput = GetStorageItemCount(activeFactory, inputStorage, inputItemId);
            var inserted = activeFactory.InsertIntoStorage(
                inputStorage,
                inputItemId,
                payload.InputItemCount,
                0,
                out var remainingInc,
                useBan: false);
            var afterInput = GetStorageItemCount(activeFactory, inputStorage, inputItemId);
            if (inserted != payload.InputItemCount
                || remainingInc != 0
                || afterInput != beforeInput + payload.InputItemCount)
            {
                throw new InvalidOperationException("DSP did not accept and reread the exact requested input stock.");
            }

            if (!VerifyInserter(activeFactory, inputInserter, inputStorage, assembler)
                || !VerifyInserter(activeFactory, outputInserter, assembler, outputStorage))
            {
                throw new InvalidOperationException("Inserter connection readback did not match the planned source and destination entities.");
            }

            _sessions.IncrementRevisionOnMainThread();
            var actionId = Guid.NewGuid().ToString("D");
            var entities = new BasicProductionLineEntities
            {
                InputStorageEntityId = inputStorage,
                AssemblerEntityId = assembler,
                OutputStorageEntityId = outputStorage,
                InputInserterEntityId = inputInserter,
                OutputInserterEntityId = outputInserter,
                PowerGeneratorEntityId = power,
            };
            var saved = TrySaveOwnedSession();
            var result = new BasicProductionLineResult
            {
                ActionId = actionId,
                Completed = true,
                Changed = true,
                IdempotentReplay = false,
                SessionId = payload.SessionId,
                Revision = _sessions.Revision,
                RecipeId = payload.Selection.Recipe.ID,
                InputItemId = inputItemId,
                InputItemCount = payload.InputItemCount,
                OutputItemId = payload.Selection.Recipe.Results[0],
                Entities = entities,
                RecipeVerified = true,
                ConnectionsVerified = true,
                InputStockVerified = true,
                Saved = saved,
                ProductionState = "waiting_for_output",
            };
            _records[actionId] = new LineRecord(
                actionId,
                payload.SessionId,
                payload.Selection,
                entities,
                GetStorageItemCount(activeFactory, outputStorage, payload.Selection.Recipe.Results[0]));
            if (!_idempotency.TryAdd(request.IdempotencyKey, fingerprint, result))
            {
                _logger.LogError($"Spherewright basic-line action {actionId} completed but could not enter the idempotency cache");
                return GameCallResult<BasicProductionLineResult>.Failed(BridgeError.Create(
                    BridgeErrorCodes.IdempotencyCapacityExceeded,
                    "The line was built, but the idempotency result cache is full.",
                    false,
                    $"Do not retry with a new key; inspect action {actionId}."));
            }

            _logger.LogInfo(
                $"Spherewright basic-line action completed actionId={actionId} sessionId={payload.SessionId} " +
                $"entities={inputStorage},{assembler},{outputStorage},{inputInserter},{outputInserter},{power} " +
                $"recipe={payload.Selection.Recipe.ID} input={inputItemId}x{payload.InputItemCount} saved={saved}");
            return GameCallResult<BasicProductionLineResult>.Succeeded(result);
        }
        catch (Exception exception)
        {
            RollBackCreatedEntities(activeFactory, activePlayer, createdEntities);
            RestorePackageBaseline(activePlayer, packageBaseline);
            if (mutationStarted)
            {
                _sessions.IncrementRevisionOnMainThread();
            }

            _logger.LogError(
                $"Spherewright basic-line action failed and rollback was attempted: {exception.GetType().Name}: {exception.Message}");
            return GameCallResult<BasicProductionLineResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.ActionFailed,
                "Basic-line construction failed; all created entities were passed through best-effort rollback.",
                false,
                "Inspect the local Spherewright log before preparing another plan."));
        }
    }

    public GameCallResult<BasicProductionLineSnapshot> InspectOnMainThread(
        string? requestedSessionId,
        InspectBasicProductionLineRequest request)
    {
        var accessError = ValidateOwnedSessionOnMainThread(
            requestedSessionId,
            0,
            requireCurrentRevision: false,
            out var state,
            out var factory,
            out _);
        if (accessError is not null)
        {
            return GameCallResult<BasicProductionLineSnapshot>.Failed(accessError);
        }

        if (!_records.TryGetValue(request.ActionId, out var record)
            || !string.Equals(record.SessionId, state!.SessionId, StringComparison.Ordinal))
        {
            return GameCallResult<BasicProductionLineSnapshot>.Failed(BridgeError.Create(
                BridgeErrorCodes.ActionNotFound,
                "The basic-line action is unknown in the active owned session.",
                false,
                "Use an actionId returned by a successful commit in this Plugin process."));
        }

        var activeFactory = factory!;

        var entities = record.Entities;
        var structureValid = ValidateEntity(activeFactory, entities.InputStorageEntityId, record.Selection.Storage.ID, entity => entity.storageId > 0)
            && ValidateEntity(activeFactory, entities.AssemblerEntityId, record.Selection.Smelter.ID, entity => entity.assemblerId > 0)
            && ValidateEntity(activeFactory, entities.OutputStorageEntityId, record.Selection.Storage.ID, entity => entity.storageId > 0)
            && ValidateEntity(activeFactory, entities.InputInserterEntityId, record.Selection.Inserter.ID, entity => entity.inserterId > 0)
            && ValidateEntity(activeFactory, entities.OutputInserterEntityId, record.Selection.Inserter.ID, entity => entity.inserterId > 0)
            && ValidateEntity(activeFactory, entities.PowerGeneratorEntityId, record.Selection.PowerGenerator.ID, entity => entity.powerGenId > 0);

        var connectionsValid = structureValid
            && VerifyInserter(activeFactory, entities.InputInserterEntityId, entities.InputStorageEntityId, entities.AssemblerEntityId)
            && VerifyInserter(activeFactory, entities.OutputInserterEntityId, entities.AssemblerEntityId, entities.OutputStorageEntityId);
        var recipeValid = structureValid && GetAssemblerRecipe(activeFactory, entities.AssemblerEntityId) == record.Selection.Recipe.ID;
        var powerValid = structureValid && VerifyPowerNetwork(activeFactory, entities.AssemblerEntityId, entities.PowerGeneratorEntityId);
        var inputCount = structureValid
            ? GetStorageItemCount(activeFactory, entities.InputStorageEntityId, record.Selection.Recipe.Items[0])
            : 0;
        var outputCount = structureValid
            ? GetStorageItemCount(activeFactory, entities.OutputStorageEntityId, record.Selection.Recipe.Results[0])
            : 0;
        var working = structureValid && IsAssemblerWorking(activeFactory, entities.AssemblerEntityId);
        var producing = structureValid && outputCount > record.InitialOutputCount;
        var productionState = !structureValid || !connectionsValid || !recipeValid
            ? "invalid"
            : !powerValid
                ? "waiting_for_power_network"
                : producing
                    ? "producing"
                    : working
                        ? "working"
                        : "waiting_for_output";

        return GameCallResult<BasicProductionLineSnapshot>.Succeeded(new BasicProductionLineSnapshot
        {
            ActionId = record.ActionId,
            SessionId = record.SessionId,
            Revision = _sessions.Revision,
            StructureValid = structureValid,
            ConnectionsValid = connectionsValid,
            RecipeValid = recipeValid,
            PowerNetworkValid = powerValid,
            Producing = producing,
            ProductionState = productionState,
            RecipeId = record.Selection.Recipe.ID,
            InputItemId = record.Selection.Recipe.Items[0],
            InputStorageCount = inputCount,
            OutputItemId = record.Selection.Recipe.Results[0],
            OutputStorageCount = outputCount,
            AssemblerWorking = working,
            Entities = CloneEntities(record.Entities),
        });
    }

    private BridgeError? ValidateOwnedSessionOnMainThread(
        string? requestedSessionId,
        long expectedRevision,
        bool requireCurrentRevision,
        out SessionState? state,
        out PlanetFactory? factory,
        out Player? player)
    {
        state = _sessions.CaptureOnMainThread();
        factory = null;
        player = null;
        if (!state.GameLoaded)
        {
            return BridgeError.Create(
                BridgeErrorCodes.GameNotLoaded,
                "No game session is currently loaded.",
                true,
                "Create and load a dedicated Spherewright test world, then retry.");
        }

        if (!state.OwnedBySpherewright)
        {
            return BridgeError.Create(
                BridgeErrorCodes.SessionNotOwned,
                "Factory access is restricted because this session was not created by the current Spherewright Plugin process.",
                false,
                "Return to the main menu and create a dedicated Spherewright test world.");
        }

        if (string.IsNullOrWhiteSpace(requestedSessionId)
            || !string.Equals(requestedSessionId, state.SessionId, StringComparison.Ordinal))
        {
            return StaleSession();
        }

        if (requireCurrentRevision && expectedRevision != state.Revision)
        {
            return BridgeError.Create(
                BridgeErrorCodes.StaleRevision,
                "The expected factory revision does not match the active owned session.",
                true,
                "Refresh session state and prepare a new plan from its current revision.");
        }

        factory = GameMain.data?.localLoadedPlanetFactory;
        player = GameMain.mainPlayer;
        if (factory is null || player is null || player.controller?.actionBuild?.clickTool is null)
        {
            return BridgeError.Create(
                BridgeErrorCodes.BridgeNotReady,
                "The local factory or player build system is not ready.",
                true,
                "Wait for the dedicated test world to finish loading and retry.");
        }

        if (!string.Equals(state.PeacefulMode, PeacefulModeStates.ConfirmedPeaceful, StringComparison.Ordinal))
        {
            return BridgeError.Create(
                BridgeErrorCodes.PeacefulModeRequired,
                "Basic-line construction is allowed only in a confirmed peaceful owned save.",
                false,
                "Create a fresh peaceful Spherewright test world.");
        }

        return null;
    }

    private static BridgeError StaleSession()
    {
        return BridgeError.Create(
            BridgeErrorCodes.StaleSession,
            "The supplied session ID does not match the active owned session.",
            true,
            "Refresh session state and retry with its current sessionId.");
    }

    private static LineSelection? SelectBasicLineParts()
    {
        var history = GameMain.history;
        var available = new Func<ItemProto, bool>(item =>
            item is not null
            && item.CanBuild
            && item.prefabDesc is not null
            && (GameMain.sandboxToolsEnabled || history.ItemUnlocked(item.ID)));
        var storage = LDB.items.dataArray
            .Where(item => available(item)
                && item.prefabDesc.isStorage
                && !item.prefabDesc.isTank
                && !item.prefabDesc.isStation
                && !item.prefabDesc.isBattleBase)
            .OrderBy(item => item.Grade)
            .ThenBy(item => item.ID)
            .FirstOrDefault();
        var smelter = LDB.items.dataArray
            .Where(item => available(item)
                && item.prefabDesc.isAssembler
                && item.prefabDesc.assemblerRecipeType == ERecipeType.Smelt)
            .OrderBy(item => item.Grade)
            .ThenBy(item => item.ID)
            .FirstOrDefault();
        var inserter = LDB.items.dataArray
            .Where(item => available(item) && item.prefabDesc.isInserter)
            .OrderBy(item => item.Grade)
            .ThenBy(item => item.ID)
            .FirstOrDefault();
        var power = LDB.items.dataArray
            .Where(item => available(item)
                && item.prefabDesc.isPowerGen
                && item.prefabDesc.isPowerNode
                && item.prefabDesc.windForcedPower)
            .OrderBy(item => item.Grade)
            .ThenBy(item => item.ID)
            .FirstOrDefault();
        var recipe = LDB.recipes.dataArray
            .Where(item => item is not null
                && history.RecipeUnlocked(item.ID)
                && item.Type == ERecipeType.Smelt
                && item.Items is not null
                && item.ItemCounts is not null
                && item.Results is not null
                && item.ResultCounts is not null
                && item.Items.Length == 1
                && item.Results.Length == 1)
            .OrderByDescending(item => LDB.items.Select(item.Items[0])?.isRaw ?? false)
            .ThenBy(item => item.ID)
            .FirstOrDefault();
        return storage is null || smelter is null || inserter is null || power is null || recipe is null
            ? null
            : new LineSelection(storage, smelter, inserter, power, recipe);
    }

    private static bool TryFindLayout(
        PlanetFactory factory,
        Player player,
        LineSelection selection,
        out LineLayout? layout,
        out string rejection)
    {
        layout = null;
        rejection = "DSP rejected every conservative candidate.";
        var build = player.controller.actionBuild;
        if (build.active || build.templatePreviews.Count != 0 || build.clickTool.buildPreviews.Count != 0)
        {
            rejection = "The player's build UI is active or already owns preview state.";
            return false;
        }

        build.SetFactoryReferences();
        var tool = new SpherewrightClickBuildTool();
        tool._Init(GameMain.data!);
        tool.SetFactoryReferences();
        try
        {
            if (!ReferenceEquals(tool.factory, factory))
            {
                rejection = "DSP's isolated click-build validator is not bound to the current local factory.";
                return false;
            }

            var storageSpacing = Math.Max(
                4.5f,
                HorizontalRadius(selection.Storage.prefabDesc) + HorizontalRadius(selection.Smelter.prefabDesc) + 0.75f);
            if (storageSpacing > 7.25f)
            {
                rejection = "The current storage and smelter footprints cannot be joined by a direct sorter.";
                return false;
            }

            var minimumPowerSpacing = HorizontalRadius(selection.PowerGenerator.prefabDesc)
                + HorizontalRadius(selection.Smelter.prefabDesc)
                + 0.75f;
            var powerSpacing = Math.Max(4.5f, minimumPowerSpacing);
            if (powerSpacing > selection.PowerGenerator.prefabDesc.powerCoverRadius - 0.2f)
            {
                rejection = "The current wind generator cannot clear the smelter footprint while covering it.";
                return false;
            }

            var forwardOffsets = new[] { 16f, 23f, 30f, 38f };
            var lateralOffsets = new[] { 0f, 9f, -9f, 18f, -18f };
            for (var yaw = 0f; yaw < 360f; yaw += 30f)
            {
                var originRotation = Maths.SphericalRotation(player.position, yaw);
                var forward = originRotation * Vector3.forward;
                var right = originRotation * Vector3.right;
                foreach (var forwardOffset in forwardOffsets)
                {
                    foreach (var lateralOffset in lateralOffsets)
                    {
                        var smelterPosition = factory.planet.aux.Snap(
                            player.position + forward * forwardOffset + right * lateralOffset,
                            onTerrain: true);
                        var inputPosition = factory.planet.aux.Snap(smelterPosition - right * storageSpacing, onTerrain: true);
                        var outputPosition = factory.planet.aux.Snap(smelterPosition + right * storageSpacing, onTerrain: true);
                        var powerPosition = factory.planet.aux.Snap(smelterPosition + forward * powerSpacing, onTerrain: true);
                        var candidate = new LineLayout(
                            inputPosition,
                            Maths.SphericalRotation(inputPosition, yaw),
                            smelterPosition,
                            Maths.SphericalRotation(smelterPosition, yaw),
                            outputPosition,
                            Maths.SphericalRotation(outputPosition, yaw),
                            powerPosition,
                            Maths.SphericalRotation(powerPosition, yaw));

                        if (!CoreFootprintsClear(selection, candidate)
                            || !ValidateCorePreview(tool, selection.Storage, candidate.InputStoragePosition, candidate.InputStorageRotation, yaw, out rejection)
                            || !ValidateCorePreview(tool, selection.Smelter, candidate.SmelterPosition, candidate.SmelterRotation, yaw, out rejection)
                            || !ValidateCorePreview(tool, selection.Storage, candidate.OutputStoragePosition, candidate.OutputStorageRotation, yaw, out rejection)
                            || !ValidateCorePreview(tool, selection.PowerGenerator, candidate.PowerPosition, candidate.PowerRotation, yaw, out rejection))
                        {
                            continue;
                        }

                        var inputStoragePreview = CreateCorePreview(selection.Storage, candidate.InputStoragePosition, candidate.InputStorageRotation);
                        var smelterPreview = CreateCorePreview(selection.Smelter, candidate.SmelterPosition, candidate.SmelterRotation);
                        var outputStoragePreview = CreateCorePreview(selection.Storage, candidate.OutputStoragePosition, candidate.OutputStorageRotation);
                        if (!TryCreateInserterPreview(selection.Inserter, inputStoragePreview, smelterPreview, out var inputInserter)
                            || !ValidatePreview(tool, inputInserter!, yaw, out rejection)
                            || !TryCreateInserterPreview(selection.Inserter, smelterPreview, outputStoragePreview, out var outputInserter)
                            || !ValidatePreview(tool, outputInserter!, yaw, out rejection))
                        {
                            continue;
                        }

                        candidate.InputInserterPosition = Vector3.Lerp(inputInserter!.lpos, inputInserter.lpos2, 0.5f);
                        candidate.OutputInserterPosition = Vector3.Lerp(outputInserter!.lpos, outputInserter.lpos2, 0.5f);
                        layout = candidate;
                        return true;
                    }
                }
            }

            return false;
        }
        finally
        {
            tool.buildPreviews.Clear();
            tool.ReleaseSnapshot();
            tool._Free();
        }
    }

    private static bool RevalidatePlan(PlanetFactory factory, Player player, LinePlanPayload payload, out string rejection)
    {
        var build = player.controller.actionBuild;
        rejection = string.Empty;
        if (build.active || build.templatePreviews.Count != 0 || build.clickTool.buildPreviews.Count != 0)
        {
            rejection = "The player's build UI acquired preview state.";
            return false;
        }

        build.SetFactoryReferences();
        var tool = new SpherewrightClickBuildTool();
        tool._Init(GameMain.data!);
        tool.SetFactoryReferences();
        try
        {
            return ReferenceEquals(tool.factory, factory)
                && CoreFootprintsClear(payload.Selection, payload.Layout)
                && ValidateCorePreview(tool, payload.Selection.Storage, payload.Layout.InputStoragePosition, payload.Layout.InputStorageRotation, 0f, out rejection)
                && ValidateCorePreview(tool, payload.Selection.Smelter, payload.Layout.SmelterPosition, payload.Layout.SmelterRotation, 0f, out rejection)
                && ValidateCorePreview(tool, payload.Selection.Storage, payload.Layout.OutputStoragePosition, payload.Layout.OutputStorageRotation, 0f, out rejection)
                && ValidateCorePreview(tool, payload.Selection.PowerGenerator, payload.Layout.PowerPosition, payload.Layout.PowerRotation, 0f, out rejection);
        }
        finally
        {
            tool.buildPreviews.Clear();
            tool.ReleaseSnapshot();
            tool._Free();
        }
    }

    private static bool ValidateCorePreview(
        SpherewrightClickBuildTool tool,
        ItemProto item,
        Vector3 position,
        Quaternion rotation,
        float yaw,
        out string rejection)
    {
        return ValidatePreview(tool, CreateCorePreview(item, position, rotation), yaw, out rejection);
    }

    private static bool ValidatePreview(SpherewrightClickBuildTool tool, BuildPreview preview, float yaw, out string rejection)
    {
        tool.buildPreviews.Clear();
        tool.handItem = preview.item;
        tool.handPrefabDesc = preview.desc;
        tool.yaw = yaw;
        if (!tool.SnapshotPlayerInventory(preview.item.ID, 1))
        {
            rejection = $"The player package has no room to stage {preview.item.name}.";
            return false;
        }

        tool.buildPreviews.Add(preview);
        try
        {
            var accepted = tool.CheckBuildConditions();
            rejection = accepted && preview.condition == EBuildCondition.Ok
                ? string.Empty
                : $"{preview.item.name} was rejected with {preview.condition}.";
            return accepted && preview.condition == EBuildCondition.Ok;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("DSP build validation raised an exception.", exception);
        }
        finally
        {
            tool.buildPreviews.Clear();
        }
    }

    private static BuildPreview CreateCorePreview(ItemProto item, Vector3 position, Quaternion rotation)
    {
        return new BuildPreview
        {
            item = item,
            desc = item.prefabDesc,
            lpos = position,
            lpos2 = position,
            lrot = rotation,
            lrot2 = rotation,
            condition = EBuildCondition.Ok,
            needModel = false,
        };
    }

    private static bool TryCreateInserterPreview(
        ItemProto inserter,
        BuildPreview source,
        BuildPreview destination,
        out BuildPreview? preview)
    {
        preview = null;
        if (!TryFindFacingSlots(
                source.desc,
                source.lpos,
                source.lrot,
                destination.desc,
                destination.lpos,
                destination.lrot,
                out var sourceSlot,
                out var destinationSlot,
                out var sourcePose,
                out var destinationPose))
        {
            return false;
        }

        preview = new BuildPreview
        {
            item = inserter,
            desc = inserter.prefabDesc,
            lpos = sourcePose.position,
            lrot = sourcePose.rotation,
            lpos2 = destinationPose.position,
            lrot2 = destinationPose.rotation * Quaternion.Euler(0f, 180f, 0f),
            input = source,
            inputFromSlot = sourceSlot,
            inputToSlot = 1,
            output = destination,
            outputToSlot = destinationSlot,
            outputFromSlot = 0,
            inputOffset = 0,
            outputOffset = 0,
            condition = EBuildCondition.Ok,
            needModel = false,
        };
        return true;
    }

    private static bool TryCreateInserterPreview(
        PlanetFactory factory,
        ItemProto inserter,
        int sourceEntityId,
        int destinationEntityId,
        out BuildPreview? preview)
    {
        preview = null;
        ref var source = ref factory.entityPool[sourceEntityId];
        ref var destination = ref factory.entityPool[destinationEntityId];
        var sourceItem = LDB.items.Select(source.protoId);
        var destinationItem = LDB.items.Select(destination.protoId);
        if (sourceItem?.prefabDesc is null || destinationItem?.prefabDesc is null)
        {
            return false;
        }

        if (!TryFindFacingSlots(
                sourceItem.prefabDesc,
                source.pos,
                source.rot,
                destinationItem.prefabDesc,
                destination.pos,
                destination.rot,
                out var sourceSlot,
                out var destinationSlot,
                out var sourcePose,
                out var destinationPose))
        {
            return false;
        }

        preview = new BuildPreview
        {
            item = inserter,
            desc = inserter.prefabDesc,
            lpos = sourcePose.position,
            lrot = sourcePose.rotation,
            lpos2 = destinationPose.position,
            lrot2 = destinationPose.rotation * Quaternion.Euler(0f, 180f, 0f),
            inputObjId = sourceEntityId,
            inputFromSlot = sourceSlot,
            inputToSlot = 1,
            outputObjId = destinationEntityId,
            outputToSlot = destinationSlot,
            outputFromSlot = 0,
            inputOffset = 0,
            outputOffset = 0,
            condition = EBuildCondition.Ok,
            needModel = false,
        };
        return true;
    }

    private static bool TryFindFacingSlots(
        PrefabDesc sourceDescription,
        Vector3 sourcePosition,
        Quaternion sourceRotation,
        PrefabDesc destinationDescription,
        Vector3 destinationPosition,
        Quaternion destinationRotation,
        out int sourceSlot,
        out int destinationSlot,
        out Pose sourcePose,
        out Pose destinationPose)
    {
        sourceSlot = -1;
        destinationSlot = -1;
        sourcePose = default(Pose);
        destinationPose = default(Pose);
        var bestDistance = float.MaxValue;
        var sourceSlots = sourceDescription.slotPoses ?? Array.Empty<Pose>();
        var destinationSlots = destinationDescription.slotPoses ?? Array.Empty<Pose>();
        for (var sourceIndex = 0; sourceIndex < sourceSlots.Length; sourceIndex++)
        {
            var candidateSource = new Pose(
                sourcePosition + sourceRotation * sourceSlots[sourceIndex].position,
                sourceRotation * sourceSlots[sourceIndex].rotation);
            for (var destinationIndex = 0; destinationIndex < destinationSlots.Length; destinationIndex++)
            {
                var candidateDestination = new Pose(
                    destinationPosition + destinationRotation * destinationSlots[destinationIndex].position,
                    destinationRotation * destinationSlots[destinationIndex].rotation);
                var delta = candidateDestination.position - candidateSource.position;
                var distance = delta.magnitude;
                if (distance < 0.9f || distance > 7.5f)
                {
                    continue;
                }

                var direction = delta.normalized;
                var sourceInward = candidateSource.rotation * Vector3.back;
                var destinationInward = candidateDestination.rotation * Vector3.back;
                if (Vector3.Dot(sourceInward, -direction) < 0.94f
                    || Vector3.Dot(destinationInward, direction) < 0.94f
                    || distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                sourceSlot = sourceIndex;
                destinationSlot = destinationIndex;
                sourcePose = candidateSource;
                destinationPose = candidateDestination;
            }
        }

        return sourceSlot >= 0 && destinationSlot >= 0;
    }

    private static bool CoreFootprintsClear(LineSelection selection, LineLayout layout)
    {
        var objects = new[]
        {
            new Footprint(layout.InputStoragePosition, HorizontalRadius(selection.Storage.prefabDesc)),
            new Footprint(layout.SmelterPosition, HorizontalRadius(selection.Smelter.prefabDesc)),
            new Footprint(layout.OutputStoragePosition, HorizontalRadius(selection.Storage.prefabDesc)),
            new Footprint(layout.PowerPosition, HorizontalRadius(selection.PowerGenerator.prefabDesc)),
        };
        for (var i = 0; i < objects.Length; i++)
        {
            for (var j = i + 1; j < objects.Length; j++)
            {
                if (Vector3.Distance(objects[i].Position, objects[j].Position)
                    <= objects[i].Radius + objects[j].Radius + 0.35f)
                {
                    return false;
                }
            }
        }

        return Vector3.Distance(layout.SmelterPosition, layout.PowerPosition)
            <= selection.PowerGenerator.prefabDesc.powerCoverRadius - 0.1f;
    }

    private static float HorizontalRadius(PrefabDesc description)
    {
        var ext = description.buildCollider.ext;
        return (float)Math.Sqrt(ext.x * ext.x + ext.z * ext.z);
    }

    private static int BuildCoreEntity(
        PlanetFactory factory,
        Player player,
        ItemProto item,
        Vector3 position,
        Quaternion rotation)
    {
        return BuildEntity(factory, player, CreateCorePreview(item, position, rotation));
    }

    private static int BuildInserter(
        PlanetFactory factory,
        Player player,
        ItemProto item,
        int sourceEntityId,
        int destinationEntityId)
    {
        if (!TryCreateInserterPreview(factory, item, sourceEntityId, destinationEntityId, out var preview))
        {
            throw new InvalidOperationException("No compatible facing slots were found for the sorter connection.");
        }

        return BuildEntity(factory, player, preview!);
    }

    private static int BuildEntity(PlanetFactory factory, Player player, BuildPreview preview)
    {
        var build = player.controller.actionBuild;
        if (build.active || build.templatePreviews.Count != 0 || build.clickTool.buildPreviews.Count != 0)
        {
            throw new InvalidOperationException("The player's build UI acquired preview state during commit.");
        }

        build.SetFactoryReferences();
        var tool = new SpherewrightClickBuildTool();
        tool._Init(GameMain.data!);
        tool.SetFactoryReferences();
        var baseline = player.package.GetItemCount(preview.item.ID);
        var preexistingEntityId = FindBuiltEntity(factory, preview.item.ID, preview.lpos);
        var builtEntityId = 0;
        try
        {
            if (!ReferenceEquals(tool.factory, factory))
            {
                throw new InvalidOperationException("The isolated build tool is no longer bound to the active factory.");
            }

            if (preexistingEntityId > 0)
            {
                throw new InvalidOperationException($"A {preview.item.name} already occupies the exact planned position.");
            }

            var granted = player.TryAddItemToPackage(preview.item.ID, 1, 0, throwTrash: false);
            if (granted != 1 || player.package.GetItemCount(preview.item.ID) != baseline + 1)
            {
                throw new InvalidOperationException($"Could not stage one {preview.item.name} in the player package.");
            }

            tool.handItem = preview.item;
            tool.handPrefabDesc = preview.desc;
            if (!tool.SnapshotPlayerInventory())
            {
                throw new InvalidOperationException("Could not snapshot the player package for build validation.");
            }

            tool.buildPreviews.Add(preview);
            if (!tool.CheckBuildConditions() || preview.condition != EBuildCondition.Ok)
            {
                throw new InvalidOperationException($"DSP rejected {preview.item.name} with {preview.condition} during commit.");
            }

            tool.CreatePrebuilds();
            if (preview.objId >= 0)
            {
                throw new InvalidOperationException($"DSP did not create a prebuild for {preview.item.name}.");
            }

            var prebuildId = -preview.objId;
            factory.BuildFinally(player, prebuildId, autoRefresh: true, flattenTerrain: true);
            builtEntityId = FindBuiltEntity(factory, preview.item.ID, preview.lpos);
            if (builtEntityId <= 0
                || builtEntityId >= factory.entityCursor
                || factory.entityPool[builtEntityId].id != builtEntityId
                || factory.entityPool[builtEntityId].protoId != preview.item.ID)
            {
                throw new InvalidOperationException($"DSP entity readback failed for {preview.item.name}.");
            }

            if (player.package.GetItemCount(preview.item.ID) != baseline)
            {
                throw new InvalidOperationException($"The exact inventory consumption check failed for {preview.item.name}.");
            }

            return builtEntityId;
        }
        catch
        {
            if (builtEntityId <= 0 && preexistingEntityId == 0)
            {
                builtEntityId = FindBuiltEntity(factory, preview.item.ID, preview.lpos);
            }

            if (builtEntityId > 0)
            {
                RollBackCreatedEntities(factory, player, new List<int> { builtEntityId });
            }

            throw;
        }
        finally
        {
            tool.buildPreviews.Clear();
            tool.ReleaseSnapshot();
            tool._Free();
            var excess = player.package.GetItemCount(preview.item.ID) - baseline;
            if (excess > 0)
            {
                player.package.TakeItem(preview.item.ID, excess, out _);
            }
        }
    }

    private static int FindBuiltEntity(PlanetFactory factory, int itemId, Vector3 position)
    {
        var bestEntityId = 0;
        var bestDistance = 0.01f;
        for (var entityId = 1; entityId < factory.entityCursor; entityId++)
        {
            ref var entity = ref factory.entityPool[entityId];
            if (entity.id != entityId || entity.protoId != itemId)
            {
                continue;
            }

            var distance = (entity.pos - position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestEntityId = entityId;
            }
        }

        return bestEntityId;
    }

    private static void SetAndVerifyRecipe(PlanetFactory factory, int assemblerEntityId, RecipeProto recipe)
    {
        ref var entity = ref factory.entityPool[assemblerEntityId];
        if (entity.assemblerId <= 0 || entity.assemblerId >= factory.factorySystem.assemblerCursor)
        {
            throw new InvalidOperationException("The new smelter does not have a valid assembler component.");
        }

        ref var assembler = ref factory.factorySystem.assemblerPool[entity.assemblerId];
        if (assembler.id != entity.assemblerId || assembler.entityId != assemblerEntityId)
        {
            throw new InvalidOperationException("The new smelter component identity is inconsistent.");
        }

        if (assembler.recipeId != 0
            || assembler.replicating
            || assembler.time != 0
            || HasNonZero(assembler.served)
            || HasNonZero(assembler.produced))
        {
            throw new InvalidOperationException("The new smelter is not empty, so recipe mutation was refused.");
        }

        if (recipe.Type != assembler.recipeType && assembler.recipeType != ERecipeType.None)
        {
            throw new InvalidOperationException("The selected recipe is not supported by the smelter.");
        }

        assembler.SetRecipe(recipe.ID, factory.entitySignPool);
        var executeData = assembler.recipeExecuteData;
        GameMain.gameScenario?.NotifyOnAssemblerRecipePick(
            factory.index,
            assembler.id,
            assembler.recipeId,
            executeData?.requires,
            executeData?.requireCounts,
            executeData?.products,
            executeData?.productCounts);
        GameMain.history.RegFeatureKey(1000109);
        if (assembler.recipeId != recipe.ID || assembler.recipeExecuteData is null)
        {
            throw new InvalidOperationException("The smelter recipe readback did not match the selected recipe.");
        }
    }

    private static bool HasNonZero(int[]? values)
    {
        return values is not null && values.Any(value => value != 0);
    }

    private static int GetStorageItemCount(PlanetFactory factory, int entityId, int itemId)
    {
        if (entityId <= 0 || entityId >= factory.entityCursor)
        {
            return 0;
        }

        var storageId = factory.entityPool[entityId].storageId;
        if (storageId <= 0 || storageId >= factory.factoryStorage.storageCursor)
        {
            return 0;
        }

        var storage = factory.factoryStorage.storagePool[storageId];
        return storage?.GetItemCount(itemId) ?? 0;
    }

    private static bool VerifyInserter(
        PlanetFactory factory,
        int inserterEntityId,
        int expectedSourceEntityId,
        int expectedDestinationEntityId)
    {
        if (inserterEntityId <= 0 || inserterEntityId >= factory.entityCursor)
        {
            return false;
        }

        var componentId = factory.entityPool[inserterEntityId].inserterId;
        if (componentId <= 0 || componentId >= factory.factorySystem.inserterCursor)
        {
            return false;
        }

        ref var inserter = ref factory.factorySystem.inserterPool[componentId];
        return inserter.id == componentId
            && inserter.entityId == inserterEntityId
            && inserter.pickTarget == expectedSourceEntityId
            && inserter.insertTarget == expectedDestinationEntityId;
    }

    private static bool VerifyPowerNetwork(PlanetFactory factory, int assemblerEntityId, int generatorEntityId)
    {
        ref var assemblerEntity = ref factory.entityPool[assemblerEntityId];
        ref var generatorEntity = ref factory.entityPool[generatorEntityId];
        if (assemblerEntity.powerConId <= 0
            || generatorEntity.powerGenId <= 0
            || assemblerEntity.powerConId >= factory.powerSystem.consumerCursor
            || generatorEntity.powerGenId >= factory.powerSystem.genCursor)
        {
            return false;
        }

        ref var consumer = ref factory.powerSystem.consumerPool[assemblerEntity.powerConId];
        ref var generator = ref factory.powerSystem.genPool[generatorEntity.powerGenId];
        return consumer.id == assemblerEntity.powerConId
            && generator.id == generatorEntity.powerGenId
            && consumer.networkId > 0
            && consumer.networkId == generator.networkId;
    }

    private static int GetAssemblerRecipe(PlanetFactory factory, int entityId)
    {
        var componentId = factory.entityPool[entityId].assemblerId;
        return componentId > 0 && componentId < factory.factorySystem.assemblerCursor
            ? factory.factorySystem.assemblerPool[componentId].recipeId
            : 0;
    }

    private static bool IsAssemblerWorking(PlanetFactory factory, int entityId)
    {
        var componentId = factory.entityPool[entityId].assemblerId;
        return componentId > 0
            && componentId < factory.factorySystem.assemblerCursor
            && factory.factorySystem.assemblerPool[componentId].replicating;
    }

    private static bool ValidateEntity(
        PlanetFactory factory,
        int entityId,
        int expectedProtoId,
        Func<EntityData, bool> componentPredicate)
    {
        if (entityId <= 0 || entityId >= factory.entityCursor)
        {
            return false;
        }

        var entity = factory.entityPool[entityId];
        return entity.id == entityId
            && entity.protoId == expectedProtoId
            && componentPredicate(entity);
    }

    private static void RollBackCreatedEntities(PlanetFactory factory, Player player, List<int> createdEntities)
    {
        for (var index = createdEntities.Count - 1; index >= 0; index--)
        {
            var entityId = createdEntities[index];
            if (entityId <= 0 || entityId >= factory.entityCursor || factory.entityPool[entityId].id != entityId)
            {
                continue;
            }

            try
            {
                var protoId = 0;
                factory.DismantleFinally(player, entityId, ref protoId);
            }
            catch
            {
            }
        }
    }

    private static void RestorePackageBaseline(Player player, Dictionary<int, int> baseline)
    {
        foreach (var pair in baseline)
        {
            var excess = player.package.GetItemCount(pair.Key) - pair.Value;
            if (excess > 0)
            {
                player.package.TakeItem(pair.Key, excess, out _);
            }
        }

        if (player.inhandItemId != 0 || player.inhandItemCount != 0)
        {
            player.SetHandItems(0, 0);
        }
    }

    private static bool TrySaveOwnedSession()
    {
        try
        {
            return !string.IsNullOrWhiteSpace(GameMain.gameName)
                && GameSave.SaveCurrentGame(GameMain.gameName);
        }
        catch
        {
            return false;
        }
    }

    private static void AddPlanBuilding(BasicProductionLinePlan plan, string role, ItemProto item, Vector3 position)
    {
        plan.Buildings.Add(new PlannedBuildingSnapshot
        {
            Role = role,
            ItemId = item.ID,
            ItemName = item.name ?? string.Empty,
            Position = Snapshot(position),
        });
    }

    private static Vector3Snapshot Snapshot(Vector3 position)
    {
        return new Vector3Snapshot { X = position.x, Y = position.y, Z = position.z };
    }

    private static BasicProductionLineResult CloneAsReplay(BasicProductionLineResult result)
    {
        return new BasicProductionLineResult
        {
            ActionId = result.ActionId,
            Completed = result.Completed,
            Changed = result.Changed,
            IdempotentReplay = true,
            SessionId = result.SessionId,
            Revision = result.Revision,
            RecipeId = result.RecipeId,
            InputItemId = result.InputItemId,
            InputItemCount = result.InputItemCount,
            OutputItemId = result.OutputItemId,
            Entities = CloneEntities(result.Entities),
            RecipeVerified = result.RecipeVerified,
            ConnectionsVerified = result.ConnectionsVerified,
            InputStockVerified = result.InputStockVerified,
            Saved = result.Saved,
            ProductionState = result.ProductionState,
        };
    }

    private static BasicProductionLineEntities CloneEntities(BasicProductionLineEntities entities)
    {
        return new BasicProductionLineEntities
        {
            InputStorageEntityId = entities.InputStorageEntityId,
            AssemblerEntityId = entities.AssemblerEntityId,
            OutputStorageEntityId = entities.OutputStorageEntityId,
            InputInserterEntityId = entities.InputInserterEntityId,
            OutputInserterEntityId = entities.OutputInserterEntityId,
            PowerGeneratorEntityId = entities.PowerGeneratorEntityId,
        };
    }

    private readonly struct Footprint
    {
        public Footprint(Vector3 position, float radius)
        {
            Position = position;
            Radius = radius;
        }

        public Vector3 Position { get; }

        public float Radius { get; }
    }

    private sealed class LineSelection
    {
        public LineSelection(ItemProto storage, ItemProto smelter, ItemProto inserter, ItemProto powerGenerator, RecipeProto recipe)
        {
            Storage = storage;
            Smelter = smelter;
            Inserter = inserter;
            PowerGenerator = powerGenerator;
            Recipe = recipe;
        }

        public ItemProto Storage { get; }

        public ItemProto Smelter { get; }

        public ItemProto Inserter { get; }

        public ItemProto PowerGenerator { get; }

        public RecipeProto Recipe { get; }
    }

    private sealed class LineLayout
    {
        public LineLayout(
            Vector3 inputStoragePosition,
            Quaternion inputStorageRotation,
            Vector3 smelterPosition,
            Quaternion smelterRotation,
            Vector3 outputStoragePosition,
            Quaternion outputStorageRotation,
            Vector3 powerPosition,
            Quaternion powerRotation)
        {
            InputStoragePosition = inputStoragePosition;
            InputStorageRotation = inputStorageRotation;
            SmelterPosition = smelterPosition;
            SmelterRotation = smelterRotation;
            OutputStoragePosition = outputStoragePosition;
            OutputStorageRotation = outputStorageRotation;
            PowerPosition = powerPosition;
            PowerRotation = powerRotation;
        }

        public Vector3 InputStoragePosition { get; }

        public Quaternion InputStorageRotation { get; }

        public Vector3 SmelterPosition { get; }

        public Quaternion SmelterRotation { get; }

        public Vector3 OutputStoragePosition { get; }

        public Quaternion OutputStorageRotation { get; }

        public Vector3 PowerPosition { get; }

        public Quaternion PowerRotation { get; }

        public Vector3 InputInserterPosition { get; set; }

        public Vector3 OutputInserterPosition { get; set; }
    }

    private sealed class LinePlanPayload
    {
        public LinePlanPayload(
            string sessionId,
            long revision,
            int inputItemCount,
            LineSelection selection,
            LineLayout layout)
        {
            SessionId = sessionId;
            Revision = revision;
            InputItemCount = inputItemCount;
            Selection = selection;
            Layout = layout;
            Fingerprint = string.Join(
                "|",
                "basic-line",
                sessionId,
                revision.ToString(CultureInfo.InvariantCulture),
                inputItemCount.ToString(CultureInfo.InvariantCulture),
                selection.Storage.ID.ToString(CultureInfo.InvariantCulture),
                selection.Smelter.ID.ToString(CultureInfo.InvariantCulture),
                selection.Inserter.ID.ToString(CultureInfo.InvariantCulture),
                selection.PowerGenerator.ID.ToString(CultureInfo.InvariantCulture),
                selection.Recipe.ID.ToString(CultureInfo.InvariantCulture),
                layout.SmelterPosition.x.ToString("R", CultureInfo.InvariantCulture),
                layout.SmelterPosition.y.ToString("R", CultureInfo.InvariantCulture),
                layout.SmelterPosition.z.ToString("R", CultureInfo.InvariantCulture));
        }

        public string SessionId { get; }

        public long Revision { get; }

        public int InputItemCount { get; }

        public LineSelection Selection { get; }

        public LineLayout Layout { get; }

        public string Fingerprint { get; }
    }

    private sealed class LineRecord
    {
        public LineRecord(
            string actionId,
            string sessionId,
            LineSelection selection,
            BasicProductionLineEntities entities,
            int initialOutputCount)
        {
            ActionId = actionId;
            SessionId = sessionId;
            Selection = selection;
            Entities = CloneEntities(entities);
            InitialOutputCount = initialOutputCount;
        }

        public string ActionId { get; }

        public string SessionId { get; }

        public LineSelection Selection { get; }

        public BasicProductionLineEntities Entities { get; }

        public int InitialOutputCount { get; }
    }
}
