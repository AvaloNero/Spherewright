using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Actions;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Sessions;
using Spherewright.Plugin.RuntimeDescriptor;
using UnityEngine;

namespace Spherewright.Plugin.Game;

internal sealed partial class NormalGameActionCoordinator
{
    private const long FlightLaunchTimeoutTicks = 3600;
    private const long MinimumFlightTimeoutTicks = 216000;

    public GameCallResult<PreparedNormalAction> PrepareInterplanetaryFlightOnMainThread(
        string? requestedSessionId,
        PrepareInterplanetaryFlightRequest request)
    {
        var common = ValidatePrepareCommon(requestedSessionId, request.PlanetId, request.StateHashVersion);
        if (common.Error is not null)
        {
            return GameCallResult<PreparedNormalAction>.Failed(common.Error);
        }

        if (request.MinimumCoreEnergyRatio < 0.8d || request.MinimumCoreEnergyRatio > 1d)
        {
            return InvalidPlan("Minimum core-energy ratio must be from 0.8 through 1.0 for an interplanetary flight.");
        }

        var playerResult = _reader.GetPlayerStateOnMainThread(
            requestedSessionId,
            new LocalPlanetRequest { PlanetId = request.PlanetId });
        if (!playerResult.Success || playerResult.Value is null)
        {
            return GameCallResult<PreparedNormalAction>.Failed(playerResult.Error!);
        }

        var starResult = _reader.GetLocalStarSystemOnMainThread(
            requestedSessionId,
            new LocalPlanetRequest { PlanetId = request.PlanetId });
        if (!starResult.Success || starResult.Value is null)
        {
            return GameCallResult<PreparedNormalAction>.Failed(starResult.Error!);
        }

        var playerSnapshot = playerResult.Value;
        var starSnapshot = starResult.Value;
        if (!string.Equals(request.ExpectedPlayerStateHash, playerSnapshot.StateHash, StringComparison.Ordinal)
            || !string.Equals(request.ExpectedStarSystemStateHash, starSnapshot.StateHash, StringComparison.Ordinal))
        {
            return StalePlan("Player or local-star identity changed after inspection; inspect both and prepare again.");
        }

        var player = GameMain.mainPlayer;
        var localPlanet = GameMain.localPlanet;
        var destination = GameMain.galaxy?.PlanetById(request.DestinationPlanetId);
        if (player?.mecha is null || player.controller is null || localPlanet is null || destination is null)
        {
            return NotReadyPlan("The player, local planet, or destination planet is not ready.");
        }

        if (!player.isAlive || player.movementState != EMovementState.Walk || player.planetId != request.PlanetId)
        {
            return GameCallResult<PreparedNormalAction>.Failed(BridgeError.Create(
                BridgeErrorCodes.ActionRejected,
                "Interplanetary flight must start alive and grounded on the inspected origin planet.",
                true,
                "Land and stop on the current owned planet, then inspect the player and prepare again."));
        }

        if (player.currentOrder is not null)
        {
            return GameCallResult<PreparedNormalAction>.Failed(BridgeError.Create(
                BridgeErrorCodes.ServerBusy,
                "A DSP player order is still active, so launch would replace or race it.",
                true,
                "Wait for the exact move or harvest order to end, then inspect and prepare again."));
        }

        if (destination.id == localPlanet.id
            || destination.star?.id != localPlanet.star?.id)
        {
            return InvalidPlan("Destination must be a different planet in the current star system.");
        }

        if (destination.type == EPlanetType.Gas)
        {
            return InvalidPlan("The bounded first interplanetary-flight action does not land on gas giants.");
        }

        if (player.mecha.thrusterLevel < 2)
        {
            return GameCallResult<PreparedNormalAction>.Failed(BridgeError.Create(
                BridgeErrorCodes.ActionRejected,
                "Drive Engine level 2 is required before normal sail mode can begin.",
                true,
                "Complete the runtime technology that raises mecha thrusterLevel to at least 2, then inspect and prepare again."));
        }

        var coreRatio = player.mecha.coreEnergyCap > 0d
            ? player.mecha.coreEnergy / player.mecha.coreEnergyCap
            : 0d;
        if (coreRatio + 1e-9d < request.MinimumCoreEnergyRatio)
        {
            return GameCallResult<PreparedNormalAction>.Failed(BridgeError.Create(
                BridgeErrorCodes.ActionRejected,
                $"Core energy ratio {coreRatio:F3} is below the prepared minimum {request.MinimumCoreEnergyRatio:F3}.",
                true,
                "Recharge at a verified wireless tower, then inspect and prepare again."));
        }

        var distance = Math.Max(0d, (destination.uPosition - player.uPosition).magnitude - destination.realRadius);
        var requiredEnergy = Math.Max(player.mecha.coreEnergyCap * 1.5d, distance * 1000d);
        var availableEnergy = CalculateAvailableFlightEnergy(player.mecha);
        if (availableEnergy + 1d < requiredEnergy
            || (player.mecha.reactorEnergy <= 0.5d && player.mecha.reactorItemId <= 0
                && !HasUsableFuel(player.mecha.reactorStorage)))
        {
            return GameCallResult<PreparedNormalAction>.Failed(BridgeError.Create(
                BridgeErrorCodes.ActionRejected,
                $"Normal core and fuel energy reserve {availableEnergy:F0} J is below the conservative flight budget {requiredEnergy:F0} J, or no usable fuel remains after the core.",
                true,
                "Transfer ordinary fuel into the player inventory, refuel through the normal refuel action, recharge the core, then inspect and prepare again."));
        }

        var playerActionHash = CanonicalStateHash.PlayerAction(playerSnapshot);
        var expectedHash = CanonicalStateHash.Combine(
            NormalActionKinds.InterplanetaryFlight,
            _sessions.SessionId,
            request.PlanetId,
            request.DestinationPlanetId,
            playerActionHash,
            starSnapshot.StateHash,
            request.MinimumCoreEnergyRatio,
            requiredEnergy);
        var estimatedTicks = Math.Max(
            7200L,
            (long)Math.Ceiling(distance / Math.Max(300d, player.mecha.maxSailSpeed) * 60d) + 7200L);
        var payload = NormalActionPlanPayload.InterplanetaryFlight(
            _sessions.SessionId!,
            request.PlanetId,
            request.DestinationPlanetId,
            expectedHash,
            playerActionHash,
            starSnapshot.StateHash,
            distance,
            request.MinimumCoreEnergyRatio,
            requiredEnergy);
        return AddPreparedPlan(
            payload,
            common.Session!,
            estimatedTicks,
            $"DSP enters native sail mode, approaches planet {destination.id}, and returns the living player to Walk state on that planet without fast travel or teleportation.");
    }

    private BridgeError? RevalidateInterplanetaryFlightPlanOnMainThread(NormalActionPlanPayload plan)
    {
        var playerResult = _reader.GetPlayerStateOnMainThread(
            plan.SessionId,
            new LocalPlanetRequest { PlanetId = plan.PlanetId });
        var starResult = _reader.GetLocalStarSystemOnMainThread(
            plan.SessionId,
            new LocalPlanetRequest { PlanetId = plan.PlanetId });
        if (!playerResult.Success || playerResult.Value is null)
        {
            return playerResult.Error;
        }

        if (!starResult.Success || starResult.Value is null)
        {
            return starResult.Error;
        }

        if (!string.Equals(CanonicalStateHash.PlayerAction(playerResult.Value), plan.PlayerStateHash, StringComparison.Ordinal)
            || !string.Equals(starResult.Value.StateHash, plan.StarSystemStateHash, StringComparison.Ordinal))
        {
            return BridgeError.Create(
                BridgeErrorCodes.StaleState,
                "Player or local-star state changed after the flight plan was prepared.",
                true,
                "Inspect the current player and star system, then prepare a new flight plan.");
        }

        var player = GameMain.mainPlayer;
        var destination = GameMain.galaxy?.PlanetById(plan.DestinationPlanetId);
        var coreRatio = player?.mecha?.coreEnergyCap > 0d
            ? player.mecha.coreEnergy / player.mecha.coreEnergyCap
            : 0d;
        if (player?.mecha is null || destination is null || player.movementState != EMovementState.Walk
            || player.currentOrder is not null || player.mecha.thrusterLevel < 2
            || coreRatio + 1e-9d < plan.MinimumCoreEnergyRatio
            || CalculateAvailableFlightEnergy(player.mecha) + 1d < plan.RequiredFlightEnergy)
        {
            return BridgeError.Create(
                BridgeErrorCodes.StaleState,
                "Flight prerequisites changed before commit.",
                true,
                "Land, recharge, refuel, inspect the current states, and prepare again.");
        }

        return null;
    }

    private void StartInterplanetaryFlightOnMainThread(ActionRecord action)
    {
        if (!EnsureFlightCheckpointOnMainThread(action))
        {
            return;
        }

        var player = GameMain.mainPlayer;
        action.FlightBestDistance = action.Plan.EstimatedDistance;
        action.FlightBestDistanceAtGameTick = GameMain.gameTick;
        action.FlightLastControlGameTick = -1L;
        EnterNativeFlight(player);
        action.State = NormalActionStates.WaitingForGame;
        action.Message = $"A separate pre-flight checkpoint was confirmed at tick {action.FlightCheckpointGameTick}; DSP then accepted native launch toward planet {action.Plan.DestinationPlanetId}.";
    }

    private bool EnsureFlightCheckpointOnMainThread(ActionRecord action)
    {
        var existing = _flightCheckpoints.CurrentTicket;
        if (existing is not null
            && existing.OriginPlanetId == action.PlanetId
            && existing.DestinationPlanetId == action.Plan.DestinationPlanetId
            && _sessions.CanReuseFlightCheckpointForCurrentSession(existing)
            && _flightCheckpoints.TryValidateCheckpointFile(existing, out _))
        {
            BindFlightCheckpoint(action, existing);
            return true;
        }

        var session = _sessions.CaptureOnMainThread();
        if (!session.OwnedBySpherewright
            || string.IsNullOrWhiteSpace(session.SessionId)
            || string.IsNullOrWhiteSpace(session.SaveName)
            || session.LocalPlanetId != action.PlanetId)
        {
            Fail(action, "Native launch was not started because the exact primary owned-save identity was unavailable for a pre-flight checkpoint.");
            return false;
        }

        try
        {
            var ticket = _flightCheckpoints.CreateDraft(
                session.SaveName!,
                session.SessionId!,
                session.Revision,
                action.PlanetId,
                action.Plan.DestinationPlanetId,
                action.Plan.PlayerStateHash,
                action.Plan.StarSystemStateHash);
            GameMain.gameName = session.SaveName;
            if (!GameSave.SaveCurrentGame(ticket.CheckpointSaveName))
            {
                Fail(action, "Native launch was not started because DSP did not confirm the separate pre-flight save.");
                return false;
            }

            var savedGameTick = GameMain.gameTick;
            GameSave.ReadHeader(ticket.CheckpointSaveName, false, out var header);
            if (header is null || header.gameTick != savedGameTick)
            {
                Fail(action, "Native launch was not started because the pre-flight save header did not prove the exact saved game tick.");
                return false;
            }

            _flightCheckpoints.PersistCompletedCheckpoint(ticket, savedGameTick);
            _sessions.MarkCurrentSessionFlightCheckpoint(ticket);
            BindFlightCheckpoint(action, ticket);
            return true;
        }
        catch (Exception exception)
        {
            Fail(action, $"Native launch was not started because the protected pre-flight checkpoint could not be completed ({exception.GetType().Name}).");
            return false;
        }
    }

    private static void BindFlightCheckpoint(ActionRecord action, FlightCheckpointTicket ticket)
    {
        action.FlightCheckpointId = ticket.CheckpointId;
        action.FlightCheckpointReloadToken = ticket.ReloadToken;
        action.FlightCheckpointGameTick = ticket.SavedGameTick;
    }

    private void UpdateInterplanetaryFlight(ActionRecord action)
    {
        var player = GameMain.mainPlayer;
        var destination = GameMain.galaxy?.PlanetById(action.Plan.DestinationPlanetId);
        if (player?.mecha is null || player.controller is null || destination is null || !player.isAlive)
        {
            Fail(action, "The player or bound destination became unavailable during interplanetary flight.");
            return;
        }

        if (action.FlightLastControlGameTick == GameMain.gameTick)
        {
            return;
        }

        action.FlightLastControlGameTick = GameMain.gameTick;
        var localPlanet = GameMain.localPlanet;
        if (localPlanet?.id == destination.id
            && player.planetId == destination.id
            && player.movementState == EMovementState.Walk)
        {
            var after = _reader.GetPlayerStateOnMainThread(
                action.SessionId,
                new LocalPlanetRequest { PlanetId = destination.id });
            if (after.Success && after.Value is not null)
            {
                action.AfterStateHash = after.Value.StateHash;
                Complete(action, $"Normal flight landed alive on planet {destination.id} and returned the player to Walk state.");
            }

            return;
        }

        if (player.movementState == EMovementState.Walk)
        {
            if (localPlanet is not null
                && localPlanet.id != action.PlanetId
                && localPlanet.id != destination.id)
            {
                Fail(action, $"Native flight landed on unexpected planet {localPlanet.id}; reload the bound pre-flight checkpoint before retrying.");
                return;
            }

            if (GameMain.gameTick > action.StartedAtGameTick + FlightLaunchTimeoutTicks)
            {
                Fail(action, "DSP did not enter native flight mode within the bounded launch window; the bound pre-flight checkpoint remains reusable.");
                return;
            }

            EnterNativeFlight(player);
            return;
        }

        if (player.movementState == EMovementState.Fly)
        {
            if (localPlanet?.id == destination.id || player.planetId == destination.id)
            {
                player.controller.actionFly.targetAltitude = 1f;
                player.controller.actionFly.moveVelocity = Vector3.zero;
                player.controller.actionFly.rtsVelocity = Vector3.zero;
            }
            else
            {
                player.controller.actionFly.targetAltitude = 50f;
                var tangent = player.forward;
                tangent -= Vector3.Dot(tangent, player.position.normalized) * player.position.normalized;
                if (tangent.sqrMagnitude < 0.01f)
                {
                    tangent = Vector3.Cross(player.position.normalized, Vector3.up);
                }

                player.controller.actionFly.moveVelocity = tangent.normalized * Math.Max(13f, player.mecha.walkSpeed * 2.5f);
            }

            return;
        }

        if (player.movementState < EMovementState.Sail)
        {
            return;
        }

        ControlNativeSailTowardPlanet(player, destination, out var surfaceDistance, out var relativeSpeed);
        if (surfaceDistance + 1d < action.FlightBestDistance)
        {
            action.FlightBestDistance = surfaceDistance;
            action.FlightBestDistanceAtGameTick = GameMain.gameTick;
        }

        if (GameMain.gameTick % 300L == 0L)
        {
            action.Message = $"Native sail is {surfaceDistance:F0} m from planet {destination.id}'s surface at {relativeSpeed:F1} m/s; core energy {player.mecha.coreEnergy:F0}/{player.mecha.coreEnergyCap:F0} J.";
        }

        var timeout = Math.Max(MinimumFlightTimeoutTicks, action.Plan.EstimatedTicks * 6L);
        if (GameMain.gameTick > action.StartedAtGameTick + timeout)
        {
            action.Message = $"Native sail exceeded its conservative estimate at {surfaceDistance:F0} m from planet {destination.id}; control remains active and the bound checkpoint can be reloaded if progress has actually failed.";
        }
    }

    private static void ControlNativeSailTowardPlanet(
        Player player,
        PlanetData destination,
        out double surfaceDistance,
        out double relativeSpeed)
    {
        var sail = player.controller.actionSail;
        var toDestination = destination.uPosition - player.uPosition;
        var centerDistance = toDestination.magnitude;
        surfaceDistance = Math.Max(0d, centerDistance - destination.realRadius);
        var direction = centerDistance > 1e-6d ? toDestination / centerDistance : player.uVelocity.normalized;

        var astro = GameMain.galaxy.astrosData[destination.id];
        var destinationVelocity = (astro.uPosNext - astro.uPos) * 60d;
        var relativeVelocity = player.uVelocity - destinationVelocity;
        relativeSpeed = relativeVelocity.magnitude;

        if (relativeSpeed > 1e-6d)
        {
            var angle = VectorLF3.AngleDEG(relativeVelocity, direction);
            var turn = (float)(1.6d / Math.Max(10d, angle));
            var desiredRelative = direction * relativeSpeed;
            var steered = (VectorLF3)Vector3.Slerp((Vector3)relativeVelocity, (Vector3)desiredRelative, turn);
            var steeringDelta = steered - relativeVelocity;
            sail.UseSailEnergy(ref steeringDelta, 0.36d);
            player.uVelocity += steeringDelta;
            relativeVelocity = player.uVelocity - destinationVelocity;
            relativeSpeed = relativeVelocity.magnitude;
        }

        var brakingDistance = Math.Max(1500d, relativeSpeed * 6d);
        var approachSpeed = Math.Max(25d, Math.Min(200d, surfaceDistance * 0.15d));
        if (surfaceDistance <= brakingDistance && relativeSpeed > approachSpeed)
        {
            var brakingDelta = relativeVelocity * 0.008d;
            sail.UseSailEnergy(ref brakingDelta, 1.5d);
            player.uVelocity -= brakingDelta;
            return;
        }

        if (surfaceDistance > brakingDistance && relativeSpeed < sail.maxSailSpeed)
        {
            var acceleration = Math.Max(7d, Math.Min(sail.max_acc, Math.Max(1d, relativeSpeed) * 0.02d));
            acceleration = Math.Min(acceleration, sail.maxSailSpeed - relativeSpeed);
            if (acceleration > 0d)
            {
                var ratio = sail.UseSailEnergy(acceleration);
                player.uVelocity += direction * (acceleration * ratio);
            }
        }
    }

    private static void EnterNativeFlight(Player player)
    {
        var previous = player.movementState;
        player.controller.actionWalk.SwitchToFly();
        var next = player.controller.movementStateInFrame;
        player.movementState = next;
        if (next != previous)
        {
            player.controller.NotifyMovementStateChange(previous, next);
        }
    }

    private static double CalculateAvailableFlightEnergy(Mecha mecha)
    {
        var available = Math.Max(0d, mecha.coreEnergy) + Math.Max(0d, mecha.reactorEnergy);
        var storage = mecha.reactorStorage;
        var grids = storage?.grids ?? Array.Empty<StorageComponent.GRID>();
        var limit = Math.Min(storage?.size ?? 0, grids.Length);
        for (var index = 0; index < limit; index++)
        {
            var grid = grids[index];
            var item = grid.itemId > 0 && grid.count > 0 ? LDB.items.Select(grid.itemId) : null;
            if (item is not null && item.HeatValue > 0L && item.FuelType > 0)
            {
                available += item.HeatValue * (double)grid.count;
            }
        }

        return available;
    }
}
