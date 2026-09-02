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
    private GameCallResult<PreparedNormalAction> PrepareDismantlePlanOnMainThread(
        string? requestedSessionId,
        PrepareDismantleRequest request)
    {
        var common = ValidatePrepareCommon(requestedSessionId, request.PlanetId, request.StateHashVersion);
        if (common.Error is not null)
        {
            return GameCallResult<PreparedNormalAction>.Failed(common.Error);
        }

        if (request.ObjectId <= 0
            || string.IsNullOrWhiteSpace(request.ExpectedEndpointStateHash)
            || string.IsNullOrWhiteSpace(request.ExpectedPlayerStateHash))
        {
            return InvalidPlan("Dismantle requires one positive inspected entity ID plus exact endpoint and player state hashes.");
        }

        var playerResult = _reader.GetPlayerStateOnMainThread(
            requestedSessionId,
            new LocalPlanetRequest { PlanetId = request.PlanetId });
        if (!playerResult.Success || playerResult.Value is null)
        {
            return GameCallResult<PreparedNormalAction>.Failed(playerResult.Error!);
        }

        if (!string.Equals(request.ExpectedPlayerStateHash, playerResult.Value.StateHash, StringComparison.Ordinal))
        {
            return StalePlan("Player position, package, hand, queue, or construction state changed after inspection.");
        }

        var targetResult = _reader.InspectFactoryEntityOnMainThread(
            requestedSessionId,
            new InspectFactoryEntityRequest { PlanetId = request.PlanetId, ObjectId = request.ObjectId });
        if (!targetResult.Success || targetResult.Value is null)
        {
            return GameCallResult<PreparedNormalAction>.Failed(targetResult.Error!);
        }

        var target = targetResult.Value;
        var targetError = ValidateDismantleTarget(playerResult.Value, target, request.ExpectedEndpointStateHash);
        if (targetError is not null)
        {
            return GameCallResult<PreparedNormalAction>.Failed(targetError);
        }

        var playerActionHash = CanonicalStateHash.PlayerAction(playerResult.Value);
        var expectedHash = CanonicalStateHash.Combine(
            NormalActionKinds.Dismantle,
            _sessions.SessionId,
            request.PlanetId,
            playerActionHash,
            target.EndpointStateHash,
            target.ObjectId,
            target.ItemId);
        var payload = NormalActionPlanPayload.Dismantle(
            _sessions.SessionId!,
            request.PlanetId,
            expectedHash,
            playerActionHash,
            target.EndpointStateHash,
            target.ObjectId,
            target.ItemId);
        var prepared = AddPreparedPlan(
            payload,
            common.Session!,
            1,
            "DSP's normal PlayerAction_Build.DoDismantleObject removes the exact resource miner, returns its building item and live internal cargo, and readback proves both recovery and disappearance.");
        if (prepared.Success && prepared.Value is not null)
        {
            prepared.Value.TargetObjectId = target.ObjectId;
            prepared.Value.ItemBudget.Add(new ActionItemBudget
            {
                ItemId = target.ItemId,
                Name = target.Name,
                Count = 1,
                Direction = "dismantle-recovery",
            });
        }

        return prepared;
    }

    private BridgeError? RevalidateDismantlePlanOnMainThread(NormalActionPlanPayload plan)
    {
        var playerResult = _reader.GetPlayerStateOnMainThread(
            plan.SessionId,
            new LocalPlanetRequest { PlanetId = plan.PlanetId });
        if (!playerResult.Success || playerResult.Value is null)
        {
            return playerResult.Error;
        }

        if (!string.Equals(CanonicalStateHash.PlayerAction(playerResult.Value), plan.PlayerStateHash, StringComparison.Ordinal))
        {
            return Stale("Player position, package, hand, queue, or construction state changed after dismantle preparation.");
        }

        var targetResult = _reader.InspectFactoryEntityOnMainThread(
            plan.SessionId,
            new InspectFactoryEntityRequest { PlanetId = plan.PlanetId, ObjectId = plan.EntityId });
        if (!targetResult.Success || targetResult.Value is null)
        {
            return targetResult.Error;
        }

        if (targetResult.Value.ItemId != plan.BuildingItemId)
        {
            return Stale("The dismantle target item identity changed after preparation.");
        }

        return ValidateDismantleTarget(playerResult.Value, targetResult.Value, plan.FactoryStateHash);
    }

    private static BridgeError? ValidateDismantleTarget(
        PlayerStateSnapshot player,
        FactoryEntitySnapshot target,
        string expectedEndpointStateHash)
    {
        if (target.ObjectKind != FactoryObjectKinds.Entity
            || target.ObjectId <= 0
            || target.ItemId <= 0
            || !string.Equals(target.ComponentKind, "miner", StringComparison.Ordinal))
        {
            return BridgeError.Create(
                BridgeErrorCodes.InvalidRequest,
                "The current dismantle slice accepts only a completed resource-miner entity.",
                false,
                "Inspect a positive resource-miner entity and prepare again.");
        }

        var item = LDB.items.Select(target.ItemId);
        if (item?.prefabDesc is null
            || (!item.prefabDesc.veinMiner
                && !item.prefabDesc.oilMiner
                && item.prefabDesc.minerType != EMinerType.Vein
                && item.prefabDesc.minerType != EMinerType.Oil))
        {
            return BridgeError.Create(
                BridgeErrorCodes.InvalidRequest,
                "The inspected entity is not a current-version solid-vein miner or oil extractor.",
                false,
                "Use a supported resource-miner entity.");
        }

        if (!string.Equals(target.EndpointStateHash, expectedEndpointStateHash, StringComparison.Ordinal))
        {
            return Stale("Dismantle target identity, pose, or connections changed after inspection.");
        }

        if (!player.IsAlive || !player.IsOnPlanet || player.MovementState != "Walk" || player.Speed > 0.1f)
        {
            return BridgeError.Create(
                BridgeErrorCodes.PlayerBusy,
                "Dismantle requires a living, settled player on the local planet.",
                true,
                "Wait for Walk state at no more than 0.1 m/s, inspect again, and prepare a new plan.");
        }

        var distance = Vector3.Distance(ToVector(player.Position), ToVector(target.Position));
        if (distance > player.BuildArea + 0.1f)
        {
            return BridgeError.Create(
                BridgeErrorCodes.TargetOutOfRange,
                $"The dismantle target is {distance:F2} metres away, outside the player's {player.BuildArea:F2}-metre build area.",
                true,
                "Move into normal construction range and prepare again.");
        }

        var requiredSlots = CountConservativeRecoverySlots(target);
        var freeSlots = player.InventorySlotCount - player.InventoryOccupiedSlotCount;
        if (freeSlots < requiredSlots)
        {
            return BridgeError.Create(
                BridgeErrorCodes.InventoryFull,
                $"Normal dismantle recovery may require {requiredSlots} empty package slots, but only {freeSlots} are free.",
                true,
                "Free package slots, inspect the player and target again, then prepare a new dismantle plan.");
        }

        return null;
    }

    private static int CountConservativeRecoverySlots(FactoryEntitySnapshot target)
    {
        var recovery = CaptureExpectedDismantleRecovery(target);
        var slots = 0;
        foreach (var pair in recovery)
        {
            var stackSize = Math.Max(1, LDB.items.Select(pair.Key)?.StackSize ?? 1);
            slots += (pair.Value + stackSize - 1) / stackSize;
        }

        return slots;
    }

    private static Dictionary<int, int> CaptureExpectedDismantleRecovery(FactoryEntitySnapshot target)
    {
        var recovery = new Dictionary<int, int> { [target.ItemId] = 1 };
        foreach (var buffer in target.Buffers.Where(buffer => buffer.ItemId > 0 && buffer.Count > 0))
        {
            recovery[buffer.ItemId] = GetCount(recovery, buffer.ItemId) + buffer.Count;
        }

        return recovery;
    }

    private void ExecuteDismantleOnMainThread(ActionRecord action)
    {
        var player = GameMain.mainPlayer
            ?? throw new InvalidOperationException("The player is unavailable during dismantle.");
        var targetResult = _reader.InspectFactoryEntityOnMainThread(
            action.SessionId,
            new InspectFactoryEntityRequest { PlanetId = action.PlanetId, ObjectId = action.Plan.EntityId });
        if (!targetResult.Success || targetResult.Value is null)
        {
            throw new InvalidOperationException("The exact dismantle target disappeared before execution.");
        }

        var expectedRecovery = CaptureExpectedDismantleRecovery(targetResult.Value);
        if (!player.controller.actionBuild.DoDismantleObject(action.Plan.EntityId))
        {
            throw new InvalidOperationException("DSP's normal dismantle path rejected the exact entity.");
        }

        var afterTarget = _reader.InspectFactoryEntityOnMainThread(
            action.SessionId,
            new InspectFactoryEntityRequest { PlanetId = action.PlanetId, ObjectId = action.Plan.EntityId });
        if (afterTarget.Success || afterTarget.Error?.Code != BridgeErrorCodes.InvalidEntity)
        {
            throw new InvalidOperationException("The target entity still exists or its disappearance could not be proven after dismantle.");
        }

        var afterInventory = CaptureInventory(player);
        var changedItemIds = action.BeforeInventory.Keys
            .Concat(afterInventory.Keys)
            .Concat(expectedRecovery.Keys)
            .Distinct()
            .ToArray();
        foreach (var itemId in changedItemIds)
        {
            var actualDelta = GetCount(afterInventory, itemId) - GetCount(action.BeforeInventory, itemId);
            var expectedDelta = GetCount(expectedRecovery, itemId);
            if (actualDelta != expectedDelta)
            {
                throw new InvalidOperationException(
                    $"Normal dismantle inventory recovery for item {itemId} was {actualDelta}, not the expected {expectedDelta}.");
            }
        }

        action.TargetObjectId = action.Plan.EntityId;
        action.TargetItemId = action.Plan.BuildingItemId;
        Complete(action,
            $"DSP's normal dismantle path removed resource miner {action.Plan.EntityId} and returned its building item plus all live internal cargo with exact inventory conservation.");
    }

    private sealed partial class NormalActionPlanPayload
    {
        public static NormalActionPlanPayload Dismantle(
            string sessionId,
            int planetId,
            string expectedStateHash,
            string playerStateHash,
            string endpointStateHash,
            int entityId,
            int buildingItemId) => new NormalActionPlanPayload
            {
                ActionKind = NormalActionKinds.Dismantle,
                SessionId = sessionId,
                PlanetId = planetId,
                ExpectedStateHash = expectedStateHash,
                PlayerStateHash = playerStateHash,
                FactoryStateHash = endpointStateHash,
                EntityId = entityId,
                BuildingItemId = buildingItemId,
                Count = 1,
                EstimatedTicks = 1,
            };
    }
}
