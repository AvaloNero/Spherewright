using Spherewright.Bridge.Core.Factory;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Actions;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Factory;
using Spherewright.Contracts.Players;
using Spherewright.Contracts.Sessions;
using UnityEngine;

namespace Spherewright.Plugin.Game;

internal sealed partial class NormalGameActionCoordinator
{
    public GameCallResult<PreparedNormalAction> PrepareUpgradeOnMainThread(
        string? sessionId, PrepareUpgradeRequest request)
    {
        var common = ValidatePrepareCommon(sessionId, request.PlanetId, request.StateHashVersion);
        if (common.Error is not null) return GameCallResult<PreparedNormalAction>.Failed(common.Error);
        if (_actions.Values.Any(a => !a.Terminal))
            return GameCallResult<PreparedNormalAction>.Failed(UpgradeError(BridgeErrorCodes.PlayerBusy, "Wait for all accepted actions to reach terminal before upgrading."));
        if (request.ObjectId <= 0 || request.ExpectedRecipeId < 0 || request.ExpectedFilterItemId < 0
            || string.IsNullOrWhiteSpace(request.ExpectedEndpointStateHash)
            || string.IsNullOrWhiteSpace(request.ExpectedPlayerStateHash))
            return InvalidPlan("Upgrade requires a positive inspected entity, its recipe/endpoint and fresh player hash.");
        var player = _reader.GetPlayerStateOnMainThread(sessionId, new LocalPlanetRequest { PlanetId = request.PlanetId });
        if (!player.Success || player.Value is null) return GameCallResult<PreparedNormalAction>.Failed(player.Error!);
        if (request.ExpectedPlayerStateHash != player.Value.StateHash) return StalePlan("Player changed after inspection.");
        var target = _reader.InspectFactoryEntityOnMainThread(sessionId,
            new InspectFactoryEntityRequest { PlanetId = request.PlanetId, ObjectId = request.ObjectId });
        if (!target.Success || target.Value is null) return GameCallResult<PreparedNormalAction>.Failed(target.Error!);
        if (target.Value.EndpointStateHash != request.ExpectedEndpointStateHash || target.Value.RecipeId != request.ExpectedRecipeId
            || (target.Value.FilterItemId ?? 0) != request.ExpectedFilterItemId)
            return StalePlan("Upgrade target identity, pose, connections or recipe changed after inspection.");
        var error = ValidateUpgradeTarget(player.Value, target.Value, request.TargetItemId);
        if (error is not null) return GameCallResult<PreparedNormalAction>.Failed(error);
        var binding = CaptureUpgradeBinding(target.Value);
        var playerHash = CanonicalStateHash.PlayerAction(player.Value);
        var plan = NormalActionPlanPayload.Upgrade(sessionId!, request.PlanetId, playerHash, binding,
            request.ObjectId, target.Value.ItemId, request.TargetItemId, common.Session!.Revision);
        var result = AddPreparedPlan(plan, common.Session, 1,
            "One native DoUpgradeObject call consumes a higher-grade device and refunds the old one; immediate readback proves identity, recipe/filter, cargo and reciprocal connections. Assemblers reset current/extra production progress; Mk.I to Mk.II sorters retain the native cycle fraction. No downgrade or batch upgrade.");
        if (result.Value is not null)
        {
            result.Value.TargetObjectId = request.ObjectId;
            result.Value.ItemBudget.Add(new ActionItemBudget { ItemId = request.TargetItemId,
                Name = LDB.items.Select(request.TargetItemId).name, Count = 1, Direction = "consume" });
            result.Value.ItemBudget.Add(new ActionItemBudget { ItemId = target.Value.ItemId,
                Name = target.Value.Name, Count = 1, Direction = "upgrade-refund" });
        }
        return result;
    }

    private BridgeError? RevalidateUpgradeOnMainThread(NormalActionPlanPayload plan)
    {
        if (_actions.Values.Any(a => !a.Terminal))
            return UpgradeError(BridgeErrorCodes.PlayerBusy, "An accepted action is still active; prepare again after it is terminal.");
        if (_sessions.CaptureOnMainThread().Revision != plan.UpgradeRevision)
            return Stale("Revision changed after upgrade prepare; inspect and prepare again.");
        var player = _reader.GetPlayerStateOnMainThread(plan.SessionId, new LocalPlanetRequest { PlanetId = plan.PlanetId });
        if (!player.Success || player.Value is null) return player.Error;
        if (CanonicalStateHash.PlayerAction(player.Value) != plan.PlayerStateHash) return Stale("Upgrade player state changed.");
        var target = _reader.InspectFactoryEntityOnMainThread(plan.SessionId,
            new InspectFactoryEntityRequest { PlanetId = plan.PlanetId, ObjectId = plan.EntityId });
        if (!target.Success || target.Value is null) return target.Error;
        var error = ValidateUpgradeTarget(player.Value, target.Value, plan.UpgradeTargetItemId);
        if (error is not null) return error;
        return target.Value.ItemId == plan.BuildingItemId
            && CaptureUpgradeBinding(target.Value) == plan.FactoryStateHash
            ? null : Stale("Upgrade identity, connections, recipe or acceleration mode changed.");
    }

    private static BridgeError? ValidateUpgradeTarget(PlayerStateSnapshot player, FactoryEntitySnapshot target, int targetItemId)
    {
        var source = LDB.items.Select(target.ItemId);
        var destination = LDB.items.Select(targetItemId);
        var isAssembler = target.ComponentKind == "assembler";
        if (target.ObjectKind != FactoryObjectKinds.Entity || (!isAssembler && target.ComponentKind != "inserter")
            || source?.prefabDesc is null || destination?.prefabDesc is null)
            return UpgradeError(BridgeErrorCodes.InvalidRequest, "Only completed ordinary manufacturing assemblers and Mk.I to Mk.II sorters are supported.");
        var nativeFamily = source.canUpgrade && source.Upgrades is not null && destination.Upgrades is not null
            && source.Upgrades.SequenceEqual(destination.Upgrades) && destination.IsUpgradeOf(source)
            && source.GetGradeItem(destination.Grade)?.ID == destination.ID
            && (isAssembler
                ? source.prefabDesc.isAssembler && destination.prefabDesc.isAssembler
                    && source.prefabDesc.assemblerRecipeType == destination.prefabDesc.assemblerRecipeType
                : source.prefabDesc.isInserter && destination.prefabDesc.isInserter
                    && source.Grade == 1 && destination.Grade == 2);
        var available = player.Inventory.Where(i => i.ItemId == targetItemId).Sum(i => i.Count)
            + (player.InHandItem?.ItemId == targetItemId ? player.InHandItem.Count : 0);
        var code = BuildingUpgradePolicy.Validate(source.ID, destination.ID, nativeFamily,
            source.Grade, destination.Grade, GameMain.history.ItemUnlocked(destination.ID), available,
            player.InventorySlotCount - player.InventoryOccupiedSlotCount);
        if (code is not null) return UpgradeError(code, "Upgrade requires a supported unlocked higher native family grade, one new building item and one empty refund slot.");
        if (!player.IsAlive || !player.IsOnPlanet || player.MovementState != "Walk" || player.Speed > 0.1f
            || GameMain.mainPlayer.controller.cmd.type == ECommand.Build)
            return UpgradeError(BridgeErrorCodes.PlayerBusy, "Upgrade requires a settled living Walk player with no active manual build tool.");
        if (Vector3.Distance(ToVector(player.Position), ToVector(target.Position)) > player.BuildArea)
            return UpgradeError(BridgeErrorCodes.TargetOutOfRange, "Upgrade target is outside the native build area.");
        var factory = GameMain.data.localLoadedPlanetFactory;
        if (factory?.factorySystem is null || factory.planet?.physics is null || factory.entityCursor > 131072)
            return UpgradeError(BridgeErrorCodes.ServerBusy, "Upgrade factory or bounded identity readback is unavailable.");
        if (isAssembler)
        {
            var id = factory.entityPool[target.ObjectId].assemblerId;
            if (id <= 0 || id >= factory.factorySystem.assemblerCursor
                || id >= factory.factorySystem.assemblerPool.Length
                || factory.factorySystem.assemblerPool[id].id != id
                || factory.factorySystem.assemblerPool[id].entityId != target.ObjectId)
                return UpgradeError(BridgeErrorCodes.TargetIdentityMismatch, "Upgrade assembler component identity is not proven.");
        }
        else
        {
            var id = factory.entityPool[target.ObjectId].inserterId;
            if (id <= 0 || id >= factory.factorySystem.inserterCursor
                || id >= factory.factorySystem.inserterPool.Length || id >= factory.factorySystem.inserterPosePool.Length)
                return UpgradeError(BridgeErrorCodes.TargetIdentityMismatch, "Upgrade sorter component is unavailable.");
            var inserter = factory.factorySystem.inserterPool[id];
            if (inserter.id != id || inserter.entityId != target.ObjectId || inserter.grade != 1
                || inserter.pickTarget <= 0 || inserter.insertTarget <= 0
                || !BuildingUpgradePolicy.TryGetBasicInserterTiming(inserter.stt, inserter.time,
                    source.prefabDesc.inserterSTT, destination.prefabDesc.inserterSTT, out _, out _))
                return UpgradeError(BridgeErrorCodes.BuildConnectionInvalid, "Mk.I sorter needs proven completed endpoints and a normal one-to-three-cell cycle.");
        }
        if (target.RecipeId > 0 && (!GameMain.history.RecipeUnlocked(target.RecipeId)
            || LDB.recipes.Select(target.RecipeId)?.Type != destination.prefabDesc.assemblerRecipeType))
            return UpgradeError(BridgeErrorCodes.RecipeNotSupportedByBuilding, "Current recipe must remain unlocked and valid for the higher-grade device.");
        return VerifyUpgradeConnections(factory, target) ? null
            : UpgradeError(BridgeErrorCodes.BuildConnectionInvalid, "Both ends of every existing connection must agree before upgrading.");
    }

    private static BridgeError UpgradeError(string code, string message) => BridgeError.Create(code, message,
        false, "Fresh read the player, target and runtime catalog; resolve the blocker before preparing again.");

    private static bool VerifyUpgradeConnections(PlanetFactory factory, FactoryEntitySnapshot target)
    {
        if (target.ComponentKind == "inserter" && !BuildingUpgradePolicy.HasCompleteInserterConnections(target))
            return false;
        foreach (var connection in target.Connections)
        {
            if (connection.OtherObjectId <= 0 || connection.OtherObjectId >= factory.entityCursor
                || connection.OtherObjectId >= factory.entityPool.Length || connection.OtherSlot < 0
                || connection.OtherSlot >= 16 || factory.entityPool[connection.OtherObjectId].id != connection.OtherObjectId)
                return false;
            factory.ReadObjectConn(connection.OtherObjectId, connection.OtherSlot, out var output, out var other, out var slot);
            if (other != target.ObjectId || slot != connection.Slot || output == connection.IsOutput) return false;
        }
        return true;
    }

    private void ExecuteUpgradeOnMainThread(ActionRecord action)
    {
        var factory = GameMain.data.localLoadedPlanetFactory;
        var before = _reader.InspectFactoryEntityOnMainThread(action.SessionId,
            new InspectFactoryEntityRequest { PlanetId = action.PlanetId, ObjectId = action.Plan.EntityId }).Value
            ?? throw new InvalidOperationException("Upgrade target disappeared before execution.");
        var isAssembler = before.ComponentKind == "assembler";
        var beforeExtra = CaptureUpgradeData(factory, before);
        var destination = LDB.items.Select(action.Plan.UpgradeTargetItemId);
        var expectedStt = 0;
        var expectedTime = 0;
        if (!isAssembler)
        {
            var inserter = factory.factorySystem.inserterPool[factory.entityPool[before.ObjectId].inserterId];
            if (!BuildingUpgradePolicy.TryGetBasicInserterTiming(inserter.stt, inserter.time,
                LDB.items.Select(before.ItemId).prefabDesc.inserterSTT, destination.prefabDesc.inserterSTT,
                out expectedStt, out expectedTime))
                throw new InvalidOperationException("Native sorter timing is no longer provable.");
        }
        // Sorters can share a centre with older objects. Exclude all pre-existing higher-grade IDs.
        var oldTargetIds = new HashSet<int>();
        for (var id = 1; id < factory.entityCursor && id < factory.entityPool.Length; id++)
            if (factory.entityPool[id].id == id && factory.entityPool[id].protoId == destination.ID)
                oldTargetIds.Add(id);
        // Exact native business entry used by BuildTool_Upgrade; never call UpgradeFinally directly.
        if (!GameMain.mainPlayer.controller.actionBuild.DoUpgradeObject(before.ObjectId, destination.Grade, 0, out var error))
            throw new InvalidOperationException($"Native upgrade rejected the operation ({error}); do not replay.");
        factory.planet.physics.SetPlanetPhysicsColliderDirty();
        var candidates = new List<int>();
        for (var id = 1; id < factory.entityCursor && id < factory.entityPool.Length; id++)
        {
            ref var entity = ref factory.entityPool[id];
            if (entity.id == id && entity.protoId == destination.ID && !oldTargetIds.Contains(id)
                && (entity.pos - ToVector(before.Position)).sqrMagnitude <= 0.000001f)
                candidates.Add(id);
        }
        if (candidates.Count != 1 || (candidates[0] != before.ObjectId && factory.entityPool[before.ObjectId].id != 0))
            throw new InvalidOperationException("Native upgrade result identity is ambiguous.");
        var after = _reader.InspectFactoryEntityOnMainThread(action.SessionId,
            new InspectFactoryEntityRequest { PlanetId = action.PlanetId, ObjectId = candidates[0] }).Value
            ?? throw new InvalidOperationException("Upgraded entity readback failed.");
        if (!BuildingUpgradePolicy.ProvesPreservation(before, after, destination.ID)
            || CaptureUpgradeData(factory, after) != beforeExtra || !VerifyUpgradeConnections(factory, after)
            || !VerifyUpgradeRuntime(factory, after, destination, expectedStt, expectedTime)
            || !BuildingUpgradePolicy.ProvesInventory(action.BeforeInventory, CaptureInventory(GameMain.mainPlayer), before.ItemId, destination.ID))
            throw new InvalidOperationException("Native upgrade configuration, cargo, topology or inventory conservation could not be proven.");
        action.TargetObjectId = after.ObjectId;
        action.TargetObjectIds = new List<int> { after.ObjectId };
        action.TargetItemId = destination.ID;
        action.UpgradeReadback = new UpgradeReadback
        {
            CapturedAtGameTick = GameMain.gameTick, SourceObjectId = before.ObjectId, ResultObjectId = after.ObjectId,
            SourceItemId = before.ItemId, TargetItemId = destination.ID, RecipeId = after.RecipeId,
            FilterItemId = after.FilterItemId ?? 0, VerifiedConnectionCount = after.Connections.Count,
            NativeTimingPolicy = isAssembler ? "assembler_progress_reset" : "basic_sorter_cycle_fraction_retained",
            ProgressBefore = before.Progress, ProgressAfter = after.Progress,
            ProgressRequiredBefore = before.ProgressRequired, ProgressRequiredAfter = after.ProgressRequired,
            BuffersBefore = before.Buffers.ToList(), BuffersAfter = after.Buffers.ToList(),
        };
        Complete(action, $"Native upgrade {before.ObjectId} → {after.ObjectId} verified: one new device consumed, old device refunded; recipe/filter, cargo and both connection ends preserved. "
            + (isAssembler ? "Native production progress reset to zero." : "Native sorter cycle fraction and new span timing verified."));
    }

    private static string CaptureUpgradeBinding(FactoryEntitySnapshot target)
    {
        var factory = GameMain.data.localLoadedPlanetFactory;
        var entity = factory.entityPool[target.ObjectId];
        var binding = BuildingUpgradePolicy.BindingHash(target,
            target.ComponentKind == "assembler" && factory.factorySystem.assemblerPool[entity.assemblerId].forceAccMode);
        if (target.ComponentKind != "inserter") return binding;
        var inserter = factory.factorySystem.inserterPool[entity.inserterId];
        return CanonicalStateHash.Combine("upgrade-sorter-binding-v1", binding, inserter.stt,
            inserter.pickOffset, inserter.insertOffset,
            CaptureUpgradeInserterPose(factory.factorySystem.inserterPosePool[entity.inserterId]));
    }

    private static string CaptureUpgradeData(PlanetFactory factory, FactoryEntitySnapshot target)
    {
        var entity = factory.entityPool[target.ObjectId];
        if (target.ComponentKind == "assembler")
            return CaptureUpgradeAssemblerData(factory.factorySystem.assemblerPool[entity.assemblerId]);
        var inserter = factory.factorySystem.inserterPool[entity.inserterId];
        return CanonicalStateHash.Combine("upgrade-sorter-cargo-v1", inserter.stage, inserter.careNeeds,
            inserter.pickTarget, inserter.insertTarget, inserter.pickTargetTypedId, inserter.insertTargetTypedId,
            inserter.filter, inserter.itemId, inserter.stackCount, inserter.itemCount, inserter.itemInc,
            inserter.pickOffset, inserter.insertOffset, inserter.requireEnergy,
            CaptureUpgradeInserterPose(factory.factorySystem.inserterPosePool[entity.inserterId]));
    }

    private static string CaptureUpgradeInserterPose(InserterPose pose) =>
        CanonicalStateHash.Combine("upgrade-sorter-pose-v1", pose.pos2.x, pose.pos2.y, pose.pos2.z,
            pose.rot2.x, pose.rot2.y, pose.rot2.z, pose.rot2.w, pose.t1, pose.t2);

    private static bool VerifyUpgradeRuntime(PlanetFactory factory, FactoryEntitySnapshot target,
        ItemProto destination, int expectedStt, int expectedTime)
    {
        var entity = factory.entityPool[target.ObjectId];
        if (entity.powerConId <= 0 || entity.powerConId >= factory.powerSystem.consumerPool.Length) return false;
        var consumer = factory.powerSystem.consumerPool[entity.powerConId];
        if (consumer.id != entity.powerConId || consumer.entityId != target.ObjectId
            || consumer.idleEnergyPerTick != destination.prefabDesc.idleEnergyPerTick
            || consumer.workEnergyPerTick != destination.prefabDesc.workEnergyPerTick) return false;
        if (target.ComponentKind == "assembler")
        {
            var assembler = factory.factorySystem.assemblerPool[entity.assemblerId];
            return assembler.speed == destination.prefabDesc.assemblerSpeed
                && assembler.speedOverride == destination.prefabDesc.assemblerSpeed
                && assembler.time == 0 && assembler.extraTime == 0;
        }
        var inserter = factory.factorySystem.inserterPool[entity.inserterId];
        return inserter.stt == expectedStt && inserter.time == expectedTime && inserter.grade == 2
            && inserter.canStack == destination.prefabDesc.inserterCanStack && !inserter.bidirectional
            && inserter.stackInput == 1 && inserter.stackOutput == 1 && inserter.delay == 0;
    }

    private static string CaptureUpgradeAssemblerData(AssemblerComponent assembler) =>
        CanonicalStateHash.Combine("upgrade-assembler-cargo-v1", assembler.recipeId, assembler.forceAccMode,
            assembler.recipeType, assembler.incUsed, assembler.cycleCount, assembler.extraCycleCount,
            assembler.extraSpeed, assembler.extraPowerRatio, assembler.replicating,
            string.Join(",", assembler.served ?? Array.Empty<int>()),
            string.Join(",", assembler.incServed ?? Array.Empty<int>()),
            string.Join(",", assembler.needs ?? Array.Empty<int>()),
            string.Join(",", assembler.produced ?? Array.Empty<int>()));

    private sealed partial class NormalActionPlanPayload
    {
        public int UpgradeTargetItemId { get; private set; }
        public long UpgradeRevision { get; private set; }

        public static NormalActionPlanPayload Upgrade(string sessionId, int planetId, string playerHash,
            string binding, int objectId, int sourceItemId, int targetItemId, long revision) => new NormalActionPlanPayload
        {
            ActionKind = NormalActionKinds.Upgrade, SessionId = sessionId, PlanetId = planetId,
            PlayerStateHash = playerHash, FactoryStateHash = binding, EntityId = objectId,
            BuildingItemId = sourceItemId, UpgradeTargetItemId = targetItemId, UpgradeRevision = revision, Count = 1,
            ExpectedStateHash = CanonicalStateHash.Combine("upgrade-v1", sessionId, planetId, revision,
                playerHash, binding, targetItemId),
        };
    }
}
