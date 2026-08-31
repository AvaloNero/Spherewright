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
    private const long FlightLandingStableTicks = 600;
    private const long FlightLandingTimeoutTicks = 7200;
    private const float NativeSailEntryTargetAltitude = 100f;

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

        if (!BuildUiIsIdle(player))
        {
            return GameCallResult<PreparedNormalAction>.Failed(BridgeError.Create(
                BridgeErrorCodes.ServerBusy,
                "The normal build UI still owns an active preview, so native flight controls are not isolated.",
                true,
                "Finish or cancel the current build preview, then inspect and prepare the flight again."));
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
            || !BuildUiIsIdle(player)
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
        action.FlightDestinationContactAtGameTick = -1L;
        action.FlightStableLandingAtGameTick = -1L;
        // PlayerMove_Fly caps targetAltitude below its own sail threshold while
        // blueprint mode is retained. This is the same public setter the
        // current-version native sail-entry branch uses before switching modes.
        player.controller.actionBuild.blueprintMode = EBlueprintMode.None;
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
        var atDestination = localPlanet?.id == destination.id || player.planetId == destination.id;
        if (atDestination)
        {
            ReleaseNativeAscentInput(action);
            if (action.FlightDestinationContactAtGameTick < 0L)
            {
                action.FlightDestinationContactAtGameTick = GameMain.gameTick;
            }

            if (GameMain.gameTick > action.FlightDestinationContactAtGameTick + FlightLandingTimeoutTicks)
            {
                Fail(action, $"Native landing on planet {destination.id} did not remain grounded within the bounded settling window; reload the bound pre-flight checkpoint before retrying.");
                return;
            }

            if (player.movementState == EMovementState.Walk)
            {
                if (player.speed <= 0.1f)
                {
                    if (action.FlightStableLandingAtGameTick < 0L)
                    {
                        action.FlightStableLandingAtGameTick = GameMain.gameTick;
                    }

                    var stableTicks = GameMain.gameTick - action.FlightStableLandingAtGameTick;
                    action.Message = $"Native landing on planet {destination.id} is grounded at {player.speed:F2} m/s for {stableTicks}/{FlightLandingStableTicks} verification ticks.";
                    if (stableTicks >= FlightLandingStableTicks)
                    {
                        var after = _reader.GetPlayerStateOnMainThread(
                            action.SessionId,
                            new LocalPlanetRequest { PlanetId = destination.id });
                        if (after.Success && after.Value is not null)
                        {
                            action.AfterStateHash = after.Value.StateHash;
                            Complete(action, $"Normal flight remained alive, grounded, and in Walk state on planet {destination.id} for {FlightLandingStableTicks} verification ticks.");
                        }

                        return;
                    }
                }
                else
                {
                    action.FlightStableLandingAtGameTick = -1L;
                    action.Message = $"Native landing touched planet {destination.id} in Walk state but is still settling at {player.speed:F2} m/s.";
                }

                return;
            }

            action.FlightStableLandingAtGameTick = -1L;
        }
        else
        {
            action.FlightStableLandingAtGameTick = -1L;
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
                ReleaseNativeAscentInput(action);
                player.controller.actionFly.targetAltitude = 1f;
                player.controller.actionFly.moveVelocity = Vector3.zero;
                player.controller.actionFly.rtsVelocity = Vector3.zero;
            }
            else
            {
                if (GameMain.gameTick > action.StartedAtGameTick + FlightLaunchTimeoutTicks)
                {
                    Fail(action, "DSP did not enter native sail mode within the bounded launch window; the bound pre-flight checkpoint remains reusable.");
                    return;
                }

                ApplyNativeAscentInput(action, player);
                // PlayerMove_Fly.GameTick lowers an unattended targetAltitude by
                // 0.1 before checking its >= 50 sail-entry threshold. Reasserting
                // exactly 50 after each tick therefore converges to a permanent
                // 49.9/50 oscillation. A modest margin lets DSP's own GameTick
                // satisfy altitude, horizontal-speed, and thruster checks and run
                // its native ResetSailState/camera/scenario transition.
                player.controller.actionFly.targetAltitude = NativeSailEntryTargetAltitude;
                var tangent = player.forward;
                tangent -= Vector3.Dot(tangent, player.position.normalized) * player.position.normalized;
                if (tangent.sqrMagnitude < 0.01f)
                {
                    tangent = Vector3.Cross(player.position.normalized, Vector3.up);
                }

                player.controller.actionFly.moveVelocity = tangent.normalized * Math.Max(13f, player.mecha.walkSpeed * 2.5f);
                action.Message = $"Native fly launch is at {player.controller.actionFly.currentAltitude:F1}/{player.controller.actionFly.targetAltitude:F1} m with {player.controller.horzSpeed:F1} m/s horizontal and {player.controller.vertSpeed:F1} m/s vertical speed; blueprint={player.controller.actionBuild.blueprintMode}, thruster={player.mecha.thrusterLevel}, frame-state={player.controller.movementStateInFrame}.";
                if (TryEnterCurrentVersionNativeSail(player))
                {
                    ReleaseNativeAscentInput(action);
                    var originPlanet = GameMain.galaxy?.PlanetById(action.PlanetId);
                    if (originPlanet is not null
                        && ControlNativeSailDeparture(player, originPlanet, destination, out var initialSurfaceDistance, out var initialRelativeSpeed))
                    {
                        action.Message = $"DSP's current-version native Fly-to-Sail branch accepted; immediate origin clearance began at {initialSurfaceDistance:F0} m and {initialRelativeSpeed:F1} m/s.";
                    }
                    else
                    {
                        action.Message = "DSP's current-version native Fly-to-Sail branch accepted the verified altitude, horizontal-speed, and thruster conditions.";
                    }
                }
            }

            return;
        }

        if (player.movementState < EMovementState.Sail)
        {
            if (atDestination && GameMain.gameTick % 120L == 0L)
            {
                action.Message = $"Native landing has contacted planet {destination.id} in {player.movementState} state and is waiting for a stable grounded Walk state.";
            }

            return;
        }

        ReleaseNativeAscentInput(action);
        var origin = GameMain.galaxy?.PlanetById(action.PlanetId);
        if (origin is not null
            && ControlNativeSailDeparture(player, origin, destination, out var departureSurfaceDistance, out var departureSpeed))
        {
            action.Message = $"Native sail is clearing the origin planet at {departureSurfaceDistance:F0} m above its surface and {departureSpeed:F1} m/s relative speed before turning toward planet {destination.id}.";
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

    private static bool ControlNativeSailDeparture(
        Player player,
        PlanetData origin,
        PlanetData destination,
        out double surfaceDistance,
        out double relativeSpeed)
    {
        var fromOrigin = player.uPosition - origin.uPosition;
        var centerDistance = fromOrigin.magnitude;
        surfaceDistance = Math.Max(0d, centerDistance - origin.realRadius);
        if (centerDistance <= 1e-6d)
        {
            relativeSpeed = 0d;
            return false;
        }

        var outward = (Vector3)(fromOrigin / centerDistance);
        var toDestination = destination.uPosition - player.uPosition;
        var destinationDirection = toDestination.magnitude > 1e-6d
            ? (Vector3)(toDestination / toDestination.magnitude)
            : outward;
        var inwardProjection = Vector3.Dot(destinationDirection, outward);
        var closestApproach = inwardProjection < 0f
            ? centerDistance * Math.Sqrt(Math.Max(0d, 1d - inwardProjection * inwardProjection))
            : double.MaxValue;
        var lineOfSightBlocked = inwardProjection < 0f
                                 && closestApproach < origin.realRadius + 100d;
        if (surfaceDistance >= 500d && !lineOfSightBlocked)
        {
            relativeSpeed = 0d;
            return false;
        }

        var tangent = destinationDirection - outward * inwardProjection;
        if (tangent.sqrMagnitude < 0.01f)
        {
            tangent = Vector3.Cross(outward, player.forward);
            if (tangent.sqrMagnitude < 0.01f)
            {
                tangent = Vector3.Cross(outward, Vector3.up);
            }
        }

        var outwardWeight = surfaceDistance < 500d ? 1f : 0.25f;
        var departureDirection = (outward * outwardWeight + tangent.normalized).normalized;
        var astro = GameMain.galaxy.astrosData[origin.id];
        var originVelocity = (astro.uPosNext - astro.uPos) * 60d;
        var relativeVelocity = player.uVelocity - originVelocity;
        relativeSpeed = relativeVelocity.magnitude;
        if (relativeSpeed > 1e-6d)
        {
            var angle = VectorLF3.AngleDEG(relativeVelocity, (VectorLF3)departureDirection);
            var turn = (float)(1.6d / Math.Max(10d, angle));
            var desiredRelative = (VectorLF3)departureDirection * relativeSpeed;
            var steered = (VectorLF3)Vector3.Slerp((Vector3)relativeVelocity, (Vector3)desiredRelative, turn);
            var steeringDelta = steered - relativeVelocity;
            player.controller.actionSail.UseSailEnergy(ref steeringDelta, 0.36d);
            player.uVelocity += steeringDelta;
            relativeVelocity = player.uVelocity - originVelocity;
            relativeSpeed = relativeVelocity.magnitude;
        }

        var departureSpeed = Math.Min(200d, player.controller.actionSail.maxSailSpeed);
        if (relativeSpeed < departureSpeed)
        {
            var acceleration = Math.Min(7d, departureSpeed - relativeSpeed);
            var ratio = player.controller.actionSail.UseSailEnergy(acceleration);
            player.uVelocity += (VectorLF3)departureDirection * (acceleration * ratio);
        }

        return true;
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

    private static bool TryEnterCurrentVersionNativeSail(Player player)
    {
        var controller = player.controller;
        var fly = controller.actionFly;
        if (player.movementState != EMovementState.Fly
            || fly.targetAltitude < 50f
            || fly.currentAltitude <= 49f
            || controller.horzSpeed <= 12.5f
            || player.mecha.thrusterLevel < 2
            || GameCamera.instance is null
            || GameMain.gameScenario is null)
        {
            return false;
        }

        // Assembly-CSharp exposes no SwitchToSail method. These are the exact
        // side effects in PlayerMove_Fly.GameTick after its four guards above;
        // velocity, position, energy, gravity, and collision remain untouched.
        if (controller.cmd.type == ECommand.Build)
        {
            controller.cmd.SetNoneCommand();
            controller.actionBuild.blueprintMode = EBlueprintMode.None;
        }

        var previous = player.movementState;
        controller.movementStateInFrame = EMovementState.Sail;
        controller.actionSail.ResetSailState();
        GameCamera.instance.SyncForSailMode();
        GameMain.gameScenario.NotifyOnSailModeEnter();
        player.movementState = controller.movementStateInFrame;
        if (player.movementState != previous)
        {
            controller.NotifyMovementStateChange(previous, player.movementState);
        }

        return true;
    }

    private static void ApplyNativeAscentInput(ActionRecord action, Player player)
    {
        if (!action.FlightAscentInputOwned)
        {
            action.FlightOriginalVerticalInput = player.controller.input1.y;
            action.FlightOriginalForwardInput = player.controller.input0.y;
            action.FlightAscentInputOwned = true;
        }

        // This is the same vertical-control channel PlayerMove_Fly.GameTick
        // reads from ordinary player input. Keeping it positive across native
        // ticks is required when rendering and simulation advance at different
        // rates; targetAltitude alone decays back toward 15 between frames.
        player.controller.input1.y = 1f;
        // Sail entry also requires PlayerController.horzSpeed > 12.5. The
        // actionFly.moveVelocity state decays toward zero when input0 is idle,
        // so keep the ordinary forward-control channel asserted until DSP's
        // native GameTick performs the Fly -> Sail transition.
        player.controller.input0.y = 1f;
    }

    private static void ReleaseNativeAscentInput(ActionRecord action)
    {
        if (!action.FlightAscentInputOwned)
        {
            return;
        }

        var controller = GameMain.mainPlayer?.controller;
        if (controller is not null && Math.Abs(controller.input1.y - 1f) < 0.0001f)
        {
            controller.input1.y = action.FlightOriginalVerticalInput;
        }

        if (controller is not null && Math.Abs(controller.input0.y - 1f) < 0.0001f)
        {
            controller.input0.y = action.FlightOriginalForwardInput;
        }

        action.FlightAscentInputOwned = false;
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
