using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Actions;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Factory;
using Spherewright.Contracts.Resources;
using Spherewright.Contracts.Sessions;
using UnityEngine;

namespace Spherewright.Plugin.Game;

internal sealed partial class NormalGameActionCoordinator
{
    private GameCallResult<PreparedNormalAction> PrepareStructuredBuildOnMainThread(
        string? requestedSessionId,
        PrepareBuildRequest request)
    {
        var common = ValidatePrepareCommon(requestedSessionId, request.PlanetId, request.StateHashVersion);
        if (common.Error is not null)
        {
            return GameCallResult<PreparedNormalAction>.Failed(common.Error);
        }

        if (request.PreferredDistance < 5f || request.PreferredDistance > 30f)
        {
            return InvalidPlan("Preferred build distance must be from 5 through 30 metres.");
        }

        if (request.PathLength < 1.5f || request.PathLength > 30f)
        {
            return InvalidPlan("A belt path length must be from 1.5 through 30 metres.");
        }

        var item = LDB.items.Select(request.BuildingItemId);
        if (item?.prefabDesc is null || !item.CanBuild)
        {
            return InvalidPlan("The requested runtime item is not a placeable building.");
        }

        var player = GameMain.mainPlayer;
        var factory = GameMain.localPlanet?.factory;
        if (player?.package is null || factory is null || player.controller?.actionBuild is null)
        {
            return NotReadyPlan("The local player build system or factory is not ready.");
        }

        if (!GameMain.history.ItemUnlocked(item.ID))
        {
            return GameCallResult<PreparedNormalAction>.Failed(BridgeError.Create(
                BridgeErrorCodes.ActionRejected,
                "The requested building item is not unlocked in the current ordinary world.",
                false,
                "Complete the normal prerequisite technology before preparing construction."));
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
            return StalePlan("Player inventory, construction queue, or position changed after inspection.");
        }

        BuildPreparation preparation;
        if (item.prefabDesc.isBelt)
        {
            preparation = TryPrepareBeltBuild(factory, player, item, request);
        }
        else if (item.prefabDesc.isInserter)
        {
            preparation = TryPrepareInserterBuild(factory, player, item, request);
        }
        else if (item.prefabDesc.veinMiner || item.prefabDesc.oilMiner
                 || item.prefabDesc.minerType == EMinerType.Vein
                 || item.prefabDesc.minerType == EMinerType.Oil)
        {
            preparation = TryPrepareResourceBuild(factory, player, item, request, requestedSessionId!);
        }
        else
        {
            preparation = TryPrepareCoreBuild(factory, player, item, request);
        }

        if (!preparation.Success)
        {
            return GameCallResult<PreparedNormalAction>.Failed(BridgeError.Create(
                preparation.ErrorCode,
                preparation.Rejection,
                true,
                "Inspect the bound state, move into construction range or choose another candidate, then prepare again."));
        }

        if (preparation.Steps.Count == 0)
        {
            return InvalidPlan("DSP did not produce any validated construction step.");
        }

        if (player.package.GetItemCount(item.ID) < preparation.Steps.Count)
        {
            return GameCallResult<PreparedNormalAction>.Failed(BridgeError.Create(
                BridgeErrorCodes.ActionRejected,
                $"The validated construction requires {preparation.Steps.Count} {item.name}, but the player owns fewer.",
                true,
                "Handcraft the exact missing building count through normal gameplay and prepare again."));
        }

        var playerActionHash = CanonicalStateHash.PlayerAction(playerResult.Value);
        var buildFingerprint = BuildPlanFingerprint(preparation);
        var expectedHash = CanonicalStateHash.Combine(
            NormalActionKinds.Build,
            _sessions.SessionId,
            request.PlanetId,
            playerActionHash,
            item.ID,
            preparation.Kind,
            preparation.ResourceStateHash,
            preparation.SourceEndpointHash,
            preparation.DestinationEndpointHash,
            buildFingerprint);
        var payload = NormalActionPlanPayload.StructuredBuild(
            _sessions.SessionId!,
            request.PlanetId,
            expectedHash,
            playerActionHash,
            item.ID,
            preparation);
        var prepared = AddPreparedPlan(
            payload,
            common.Session!,
            Math.Max(3600, preparation.Steps.Count * 900L),
            $"DSP creates {preparation.Steps.Count} ordinary {preparation.Kind} prebuild(s), consumes exactly that many owned items, and construction drones replace every prebuild with a reread matching entity.");
        if (prepared.Success && prepared.Value is not null)
        {
            prepared.Value.BuildKind = preparation.Kind;
            prepared.Value.SourceObjectId = preparation.SourceObjectId > 0 ? preparation.SourceObjectId : (int?)null;
            prepared.Value.DestinationObjectId = preparation.DestinationObjectId > 0
                ? preparation.DestinationObjectId
                : (int?)null;
            prepared.Value.PlannedPosition = Snapshot(preparation.Steps[0].Position);
            prepared.Value.PlannedYaw = preparation.Steps[0].Yaw;
            prepared.Value.PlannedPath = preparation.Steps.Select(step => Snapshot(step.Position)).ToList();
            prepared.Value.ItemBudget.Add(new ActionItemBudget
            {
                ItemId = item.ID,
                Name = item.name ?? string.Empty,
                Count = preparation.Steps.Count,
                Direction = "construction-consumption",
            });
        }

        return prepared;
    }

    private BuildPreparation TryPrepareCoreBuild(
        PlanetFactory factory,
        Player player,
        ItemProto item,
        PrepareBuildRequest request)
    {
        if (request.ResourceNodeId.HasValue || request.SourceObjectId.HasValue
            || request.DestinationObjectId.HasValue || request.PathEnd is not null)
        {
            return BuildPreparation.Failed(
                BridgeErrorCodes.InvalidRequest,
                "A core building request cannot bind resource, connection, or belt-path targets.");
        }

        var candidates = new List<BuildStepPlan>();
        if (request.PreferredPosition is not null)
        {
            var requested = ToVector(request.PreferredPosition);
            if (!IsFinite(requested.x) || !IsFinite(requested.y) || !IsFinite(requested.z)
                || requested.sqrMagnitude < 1f)
            {
                return BuildPreparation.Failed(BridgeErrorCodes.InvalidRequest, "Preferred building coordinates are invalid.");
            }

            var position = factory.planet.aux.Snap(requested, onTerrain: true);
            var yaw = Mathf.Repeat(request.PreferredYaw ?? 0f, 360f);
            candidates.Add(BuildStepPlan.Core(item.ID, position, Maths.SphericalRotation(position, yaw), yaw));
        }
        else
        {
            var distances = new[]
            {
                request.PreferredDistance,
                Math.Min(30f, request.PreferredDistance + 5f),
                Math.Max(5f, request.PreferredDistance - 4f),
            }.Distinct().ToArray();
            var lateralOffsets = new[] { 0f, 5f, -5f, 10f, -10f, 15f, -15f };
            for (var yaw = 0f; yaw < 360f; yaw += 30f)
            {
                var basis = Maths.SphericalRotation(player.position, yaw);
                foreach (var distance in distances)
                {
                    foreach (var lateral in lateralOffsets)
                    {
                        var position = factory.planet.aux.Snap(
                            player.position + basis * Vector3.forward * distance + basis * Vector3.right * lateral,
                            onTerrain: true);
                        candidates.Add(BuildStepPlan.Core(item.ID, position, Maths.SphericalRotation(position, yaw), yaw));
                    }
                }
            }
        }

        var last = "No core-building candidate was accepted.";
        foreach (var candidate in candidates)
        {
            if (TryValidateClickBuild(factory, player, item, candidate, 0, out var accepted, out last))
            {
                return BuildPreparation.Succeeded(NormalBuildKinds.Core, new[] { accepted });
            }
        }

        return BuildPreparation.Failed(BridgeErrorCodes.BuildLocationInvalid, last);
    }

    private BuildPreparation TryPrepareResourceBuild(
        PlanetFactory factory,
        Player player,
        ItemProto item,
        PrepareBuildRequest request,
        string requestedSessionId)
    {
        if (!request.ResourceNodeId.HasValue || request.ResourceNodeId.Value <= 0
            || string.IsNullOrWhiteSpace(request.ExpectedResourceStateHash))
        {
            return BuildPreparation.Failed(
                BridgeErrorCodes.InvalidRequest,
                "A vein miner or oil extractor requires one inspected vein node and its exact state hash.");
        }

        if (request.SourceObjectId.HasValue || request.DestinationObjectId.HasValue || request.PathEnd is not null)
        {
            return BuildPreparation.Failed(
                BridgeErrorCodes.InvalidRequest,
                "A resource-building request cannot also be a connection or belt-path request.");
        }

        var resourceResult = _reader.InspectResourceNodeOnMainThread(
            requestedSessionId,
            new InspectResourceNodeRequest
            {
                PlanetId = request.PlanetId,
                Kind = ResourceNodeKinds.Vein,
                NodeId = request.ResourceNodeId.Value,
            });
        if (!resourceResult.Success || resourceResult.Value is null)
        {
            return BuildPreparation.Failed(
                resourceResult.Error?.Code ?? BridgeErrorCodes.InvalidResourceTarget,
                resourceResult.Error?.Message ?? "The bound vein no longer exists.");
        }

        var resource = resourceResult.Value;
        if (!string.Equals(resource.StateHash, request.ExpectedResourceStateHash, StringComparison.Ordinal))
        {
            return BuildPreparation.Failed(
                BridgeErrorCodes.StaleState,
                "The bound vein amount, identity, group, or miner count changed after inspection.");
        }

        var isOil = string.Equals(resource.ResourceType, EVeinType.Oil.ToString(), StringComparison.OrdinalIgnoreCase);
        if ((item.prefabDesc.oilMiner || item.prefabDesc.minerType == EMinerType.Oil) != isOil)
        {
            return BuildPreparation.Failed(
                BridgeErrorCodes.InvalidResourceTarget,
                isOil
                    ? "The selected building is not an oil extractor."
                    : "An oil extractor cannot target a solid mineral vein.");
        }

        var target = ToVector(resource.Position);
        var candidates = new List<BuildStepPlan>();
        if (request.PreferredPosition is not null)
        {
            var requested = factory.planet.aux.Snap(ToVector(request.PreferredPosition), onTerrain: true);
            var yaw = Mathf.Repeat(request.PreferredYaw ?? 0f, 360f);
            candidates.Add(BuildStepPlan.Core(item.ID, requested, Maths.SphericalRotation(requested, yaw), yaw));
        }
        else if (isOil)
        {
            var position = factory.planet.aux.Snap(target, onTerrain: true);
            for (var yaw = 0f; yaw < 360f; yaw += 30f)
            {
                candidates.Add(BuildStepPlan.Core(item.ID, position, Maths.SphericalRotation(position, yaw), yaw));
            }
        }
        else
        {
            var offsets = new[] { 3.5f, 4.5f, 5.5f, 6.5f, 7.25f };
            for (var yaw = 0f; yaw < 360f; yaw += 15f)
            {
                var basis = Maths.SphericalRotation(target, yaw);
                foreach (var offset in offsets)
                {
                    var position = factory.planet.aux.Snap(
                        target + basis * Vector3.forward * offset,
                        onTerrain: true);
                    candidates.Add(BuildStepPlan.Core(item.ID, position, Maths.SphericalRotation(position, yaw), yaw));
                }
            }
        }

        var last = "No resource-building candidate was accepted.";
        foreach (var candidate in candidates)
        {
            if (TryValidateClickBuild(
                    factory,
                    player,
                    item,
                    candidate,
                    resource.NodeId,
                    out var accepted,
                    out last))
            {
                var result = BuildPreparation.Succeeded(NormalBuildKinds.Resource, new[] { accepted });
                result.ResourceNodeId = resource.NodeId;
                result.ResourceStateHash = resource.StateHash;
                return result;
            }
        }

        return BuildPreparation.Failed(BridgeErrorCodes.BuildLocationInvalid, last);
    }

    private BuildPreparation TryPrepareInserterBuild(
        PlanetFactory factory,
        Player player,
        ItemProto item,
        PrepareBuildRequest request)
    {
        if (!request.SourceObjectId.HasValue || request.SourceObjectId.Value <= 0
            || !request.DestinationObjectId.HasValue || request.DestinationObjectId.Value <= 0
            || string.IsNullOrWhiteSpace(request.ExpectedSourceStateHash)
            || string.IsNullOrWhiteSpace(request.ExpectedDestinationStateHash))
        {
            return BuildPreparation.Failed(
                BridgeErrorCodes.InvalidRequest,
                "An inserter requires exact inspected source and destination entities plus both state hashes.");
        }

        if (request.SourceObjectId == request.DestinationObjectId)
        {
            return BuildPreparation.Failed(BridgeErrorCodes.BuildConnectionInvalid, "An inserter cannot connect an entity to itself.");
        }

        FactoryEntitySnapshot? source;
        FactoryEntitySnapshot? destination;
        string? sourceError = null;
        string? destinationError = null;
        if (!TryReadBuildEndpoint(request, request.SourceObjectId.Value, request.ExpectedSourceStateHash!, out source, out sourceError)
            || !TryReadBuildEndpoint(request, request.DestinationObjectId.Value, request.ExpectedDestinationStateHash!, out destination, out destinationError))
        {
            return BuildPreparation.Failed(
                BridgeErrorCodes.StaleState,
                sourceError ?? destinationError ?? "A bound inserter endpoint changed after inspection.");
        }

        var sourcePoints = GetInserterEndpointPoints(factory, source!);
        var destinationPoints = GetInserterEndpointPoints(factory, destination!);
        if (sourcePoints.Count == 0 || destinationPoints.Count == 0)
        {
            return BuildPreparation.Failed(
                BridgeErrorCodes.BuildConnectionInvalid,
                "One bound endpoint exposes no current-version inserter slot or belt attachment pose.");
        }

        var last = "DSP rejected every bounded inserter endpoint pair.";
        foreach (var sourcePoint in sourcePoints)
        {
            foreach (var destinationPoint in destinationPoints)
            {
                var step = BuildStepPlan.Inserter(
                    item.ID,
                    sourcePoint.Pose,
                    destinationPoint.Pose,
                    sourcePoint.ObjectId,
                    sourcePoint.Slot,
                    destinationPoint.ObjectId,
                    destinationPoint.Slot);
                if (TryValidateInserterBuild(factory, player, item, step, out var accepted, out last))
                {
                    var result = BuildPreparation.Succeeded(NormalBuildKinds.Inserter, new[] { accepted });
                    result.SourceObjectId = source!.ObjectId;
                    result.DestinationObjectId = destination!.ObjectId;
                    result.SourceEndpointHash = BuildEndpointHash(source);
                    result.DestinationEndpointHash = BuildEndpointHash(destination);
                    return result;
                }
            }
        }

        return BuildPreparation.Failed(BridgeErrorCodes.BuildConnectionInvalid, last);
    }

    private BuildPreparation TryPrepareBeltBuild(
        PlanetFactory factory,
        Player player,
        ItemProto item,
        PrepareBuildRequest request)
    {
        FactoryEntitySnapshot? source = null;
        FactoryEntitySnapshot? destination = null;
        if (request.SourceObjectId.HasValue)
        {
            string? sourceError = null;
            if (request.SourceObjectId.Value <= 0 || string.IsNullOrWhiteSpace(request.ExpectedSourceStateHash)
                || !TryReadBuildEndpoint(request, request.SourceObjectId.Value, request.ExpectedSourceStateHash!, out source, out sourceError))
            {
                return BuildPreparation.Failed(
                    BridgeErrorCodes.StaleState,
                    sourceError ?? "The bound belt source is invalid or changed after inspection.");
            }
        }

        if (request.DestinationObjectId.HasValue)
        {
            string? destinationError = null;
            if (request.DestinationObjectId.Value <= 0 || string.IsNullOrWhiteSpace(request.ExpectedDestinationStateHash)
                || !TryReadBuildEndpoint(request, request.DestinationObjectId.Value, request.ExpectedDestinationStateHash!, out destination, out destinationError))
            {
                return BuildPreparation.Failed(
                    BridgeErrorCodes.StaleState,
                    destinationError ?? "The bound belt destination is invalid or changed after inspection.");
            }
        }

        var sourcePorts = source is null
            ? new List<EndpointPoint> { new EndpointPoint(0, -1, ResolveFreeBeltStart(factory, player, request)) }
            : GetFreePortPoints(factory, source, requireOutput: true);
        var destinationPorts = destination is null
            ? new List<EndpointPoint>()
            : GetFreePortPoints(factory, destination, requireOutput: false);
        if (sourcePorts.Count == 0 || (destination is not null && destinationPorts.Count == 0))
        {
            return BuildPreparation.Failed(
                BridgeErrorCodes.BuildConnectionInvalid,
                "The bound source or destination exposes no free current-version belt port.");
        }

        var last = "DSP rejected every bounded belt path candidate.";
        foreach (var sourcePort in sourcePorts)
        {
            var endCandidates = new List<EndpointPoint>();
            if (destination is not null)
            {
                endCandidates.AddRange(destinationPorts);
            }
            else if (request.PathEnd is not null)
            {
                var end = factory.planet.aux.Snap(ToVector(request.PathEnd), onTerrain: true);
                endCandidates.Add(new EndpointPoint(0, -1, new Pose(end, Maths.SphericalRotation(end, 0f))));
            }
            else
            {
                var baseForward = ProjectTangent(sourcePort.Pose.rotation * Vector3.forward, sourcePort.Pose.position);
                var baseRight = ProjectTangent(sourcePort.Pose.rotation * Vector3.right, sourcePort.Pose.position);
                var directions = new[] { baseForward, -baseForward, baseRight, -baseRight };
                foreach (var direction in directions.Where(direction => direction.sqrMagnitude > 0.5f))
                {
                    var end = factory.planet.aux.Snap(
                        sourcePort.Pose.position + direction.normalized * request.PathLength,
                        onTerrain: true);
                    endCandidates.Add(new EndpointPoint(0, -1, new Pose(end, Maths.SphericalRotation(end, 0f))));
                }
            }

            foreach (var endPort in endCandidates)
            {
                if (!TryCreateBeltSteps(
                        factory,
                        item,
                        sourcePort,
                        endPort,
                        out var candidate,
                        out last))
                {
                    continue;
                }

                if (TryValidateBeltBuild(factory, player, item, candidate, out var accepted, out last))
                {
                    var result = BuildPreparation.Succeeded(NormalBuildKinds.Belt, accepted);
                    if (source is not null)
                    {
                        result.SourceObjectId = source.ObjectId;
                        result.SourceEndpointHash = BuildEndpointHash(source);
                    }

                    if (destination is not null)
                    {
                        result.DestinationObjectId = destination.ObjectId;
                        result.DestinationEndpointHash = BuildEndpointHash(destination);
                    }

                    return result;
                }
            }
        }

        return BuildPreparation.Failed(BridgeErrorCodes.BuildLocationInvalid, last);
    }

    private bool TryReadBuildEndpoint(
        PrepareBuildRequest request,
        int objectId,
        string expectedStateHash,
        out FactoryEntitySnapshot? snapshot,
        out string? error)
    {
        var result = _reader.InspectFactoryEntityOnMainThread(
            _sessions.SessionId,
            new InspectFactoryEntityRequest { PlanetId = request.PlanetId, ObjectId = objectId });
        snapshot = result.Value;
        error = result.Error?.Message;
        if (!result.Success || snapshot is null || snapshot.ObjectKind != FactoryObjectKinds.Entity)
        {
            return false;
        }

        if (!string.Equals(snapshot.EndpointStateHash, expectedStateHash, StringComparison.Ordinal)
            && !string.Equals(snapshot.StateHash, expectedStateHash, StringComparison.Ordinal))
        {
            error = $"Bound endpoint {objectId} identity, pose, or connections changed after inspection.";
            return false;
        }

        return true;
    }

    private static Pose ResolveFreeBeltStart(PlanetFactory factory, Player player, PrepareBuildRequest request)
    {
        if (request.PreferredPosition is not null)
        {
            var requested = factory.planet.aux.Snap(ToVector(request.PreferredPosition), onTerrain: true);
            return new Pose(requested, Maths.SphericalRotation(requested, request.PreferredYaw ?? 0f));
        }

        var yaw = request.PreferredYaw ?? 0f;
        var basis = Maths.SphericalRotation(player.position, yaw);
        var position = factory.planet.aux.Snap(
            player.position + basis * Vector3.forward * request.PreferredDistance,
            onTerrain: true);
        return new Pose(position, Maths.SphericalRotation(position, yaw));
    }

    private static bool TryCreateBeltSteps(
        PlanetFactory factory,
        ItemProto item,
        EndpointPoint source,
        EndpointPoint destination,
        out List<BuildStepPlan> steps,
        out string rejection)
    {
        steps = new List<BuildStepPlan>();
        rejection = string.Empty;
        var points = new Vector3[256];
        var maxSlope = 0f;
        var count = factory.planet.aux.SnapLineNonAlloc(
            source.Pose.position,
            destination.Pose.position,
            1,
            geodesic: false,
            begin_flat: source.ObjectId == 0,
            points,
            forceVertical: false,
            ref maxSlope,
            useOldPath: false);
        if (count < 2)
        {
            rejection = "DSP's terrain grid did not return a belt path with at least two segments.";
            return false;
        }

        points[0] = source.Pose.position;
        points[count - 1] = destination.Pose.position;
        for (var index = 0; index < count; index++)
        {
            var step = BuildStepPlan.Belt(item.ID, points[index]);
            step.InputStepIndex = index > 0 ? index - 1 : -1;
            step.OutputStepIndex = index + 1 < count ? index + 1 : -1;
            if (index == 0 && source.ObjectId > 0)
            {
                step.InputObjectId = source.ObjectId;
                step.InputFromSlot = source.Slot;
                step.InputToSlot = 1;
            }

            if (index == count - 1 && destination.ObjectId > 0)
            {
                step.OutputObjectId = destination.ObjectId;
                step.OutputFromSlot = 0;
                step.OutputToSlot = destination.Slot;
            }

            steps.Add(step);
        }

        return true;
    }

    private static bool TryValidateClickBuild(
        PlanetFactory factory,
        Player player,
        ItemProto item,
        BuildStepPlan candidate,
        int requiredResourceNodeId,
        out BuildStepPlan accepted,
        out string rejection)
    {
        accepted = candidate;
        rejection = string.Empty;
        if (!BuildUiIsIdle(player))
        {
            rejection = "The player's normal build UI owns preview state.";
            return false;
        }

        // BuildTool_Click normally relies on the live UI preview/collider
        // lifecycle.  An isolated validator can otherwise miss an already
        // built collider even though its remaining DSP checks pass.  Bind the
        // exact current-version build colliders to both poses and perform the
        // same conservative occupied-volume check before calling the native
        // validator.  This also covers prebuilds created by ordinary gameplay.
        if (OverlapsExistingFactoryObject(factory, item.prefabDesc, candidate.Position, candidate.Rotation,
                out var occupiedObjectId))
        {
            rejection = $"The exact build-collider volume overlaps existing factory object {occupiedObjectId}.";
            return false;
        }

        var tool = new SpherewrightClickBuildTool();
        tool._Init(GameMain.data!);
        tool.SetFactoryReferences();
        try
        {
            if (!ReferenceEquals(tool.factory, factory))
            {
                rejection = "The isolated click-build validator is not bound to the local factory.";
                return false;
            }

            tool.handItem = item;
            tool.handPrefabDesc = item.prefabDesc;
            tool.yaw = candidate.Yaw;
            tool.SnapshotPlayerInventory();
            var preview = CreatePreview(candidate, item);
            preview.parameters = null;
            preview.paramCount = 0;
            tool.buildPreviews.Add(preview);
            var valid = tool.CheckBuildConditions();
            if (!valid || preview.condition != EBuildCondition.Ok || preview.coverObjId != 0)
            {
                rejection = $"DSP click-build validation returned {preview.condition}.";
                return false;
            }

            if (requiredResourceNodeId > 0
                && (preview.parameters is null
                    || !preview.parameters.Take(preview.paramCount).Contains(requiredResourceNodeId)))
            {
                rejection = "DSP accepted the position but did not bind the requested exact resource node.";
                return false;
            }

            accepted = BuildStepPlan.FromPreview(candidate, preview);
            return true;
        }
        finally
        {
            tool.buildPreviews.Clear();
            tool.ReleaseSnapshot();
            tool._Free();
        }
    }

    private static bool OverlapsExistingFactoryObject(
        PlanetFactory factory,
        PrefabDesc candidateDescription,
        Vector3 candidatePosition,
        Quaternion candidateRotation,
        out int occupiedObjectId)
    {
        occupiedObjectId = 0;
        var candidateColliders = CreateWorldBuildColliders(
            candidateDescription,
            candidatePosition,
            candidateRotation);
        if (candidateColliders.Count == 0)
        {
            return false;
        }

        var entityLimit = Math.Min(factory.entityCursor, factory.entityPool.Length);
        for (var entityId = 1; entityId < entityLimit; entityId++)
        {
            ref var entity = ref factory.entityPool[entityId];
            if (entity.id != entityId || entity.protoId <= 0)
            {
                continue;
            }

            var description = LDB.items.Select(entity.protoId)?.prefabDesc;
            if (description is null)
            {
                continue;
            }

            if (BuildColliderSetsOverlap(
                    candidateColliders,
                    CreateWorldBuildColliders(description, entity.pos, entity.rot)))
            {
                occupiedObjectId = entityId;
                return true;
            }
        }

        var prebuildLimit = Math.Min(factory.prebuildCursor, factory.prebuildPool.Length);
        for (var prebuildId = 1; prebuildId < prebuildLimit; prebuildId++)
        {
            ref var prebuild = ref factory.prebuildPool[prebuildId];
            if (prebuild.id != prebuildId || prebuild.isDestroyed || prebuild.protoId <= 0)
            {
                continue;
            }

            var description = LDB.items.Select(prebuild.protoId)?.prefabDesc;
            if (description is null)
            {
                continue;
            }

            if (BuildColliderSetsOverlap(
                    candidateColliders,
                    CreateWorldBuildColliders(description, prebuild.pos, prebuild.rot)))
            {
                occupiedObjectId = -prebuildId;
                return true;
            }
        }

        return false;
    }

    private static List<WorldBuildCollider> CreateWorldBuildColliders(
        PrefabDesc description,
        Vector3 position,
        Quaternion rotation)
    {
        var result = new List<WorldBuildCollider>();
        var colliders = description.buildColliders;
        if (colliders is null || colliders.Length == 0)
        {
            if (!description.hasBuildCollider)
            {
                return result;
            }

            colliders = new[] { description.buildCollider };
        }

        foreach (var collider in colliders)
        {
            var localRotation = Quaternion.Dot(collider.q, collider.q) > 0.01f
                ? collider.q
                : Quaternion.identity;
            var worldRotation = rotation * localRotation;
            Vector3 extents;
            switch (collider.shape)
            {
                case EColliderShape.Box:
                    extents = new Vector3(
                        Math.Abs(collider.ext.x),
                        Math.Abs(collider.ext.y),
                        Math.Abs(collider.ext.z));
                    break;
                case EColliderShape.Sphere:
                    extents = Vector3.one * Math.Abs(collider.radius);
                    break;
                case EColliderShape.Capsule:
                    // A capsule is conservatively enclosed by an oriented box.
                    var capsuleRadius = Math.Abs(collider.radius);
                    extents = new Vector3(
                        Math.Abs(collider.ext.x) + capsuleRadius,
                        Math.Abs(collider.ext.y) + capsuleRadius,
                        Math.Abs(collider.ext.z) + capsuleRadius);
                    break;
                default:
                    continue;
            }

            if (extents.sqrMagnitude < 0.0001f)
            {
                continue;
            }

            result.Add(new WorldBuildCollider(
                position + rotation * collider.pos,
                (worldRotation * Vector3.right).normalized,
                (worldRotation * Vector3.up).normalized,
                (worldRotation * Vector3.forward).normalized,
                extents + Vector3.one * 0.01f));
        }

        return result;
    }

    private static bool BuildColliderSetsOverlap(
        IReadOnlyList<WorldBuildCollider> left,
        IReadOnlyList<WorldBuildCollider> right)
    {
        for (var leftIndex = 0; leftIndex < left.Count; leftIndex++)
        {
            for (var rightIndex = 0; rightIndex < right.Count; rightIndex++)
            {
                if (OrientedBoxesOverlap(left[leftIndex], right[rightIndex]))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool OrientedBoxesOverlap(WorldBuildCollider left, WorldBuildCollider right)
    {
        const float epsilon = 0.00001f;
        var leftAxes = new[] { left.AxisX, left.AxisY, left.AxisZ };
        var rightAxes = new[] { right.AxisX, right.AxisY, right.AxisZ };
        var leftExtents = new[] { left.Extents.x, left.Extents.y, left.Extents.z };
        var rightExtents = new[] { right.Extents.x, right.Extents.y, right.Extents.z };
        var rotation = new float[3, 3];
        var absoluteRotation = new float[3, 3];
        for (var leftAxis = 0; leftAxis < 3; leftAxis++)
        {
            for (var rightAxis = 0; rightAxis < 3; rightAxis++)
            {
                rotation[leftAxis, rightAxis] = Vector3.Dot(leftAxes[leftAxis], rightAxes[rightAxis]);
                absoluteRotation[leftAxis, rightAxis] = Math.Abs(rotation[leftAxis, rightAxis]) + epsilon;
            }
        }

        var delta = right.Center - left.Center;
        var translation = new[]
        {
            Vector3.Dot(delta, leftAxes[0]),
            Vector3.Dot(delta, leftAxes[1]),
            Vector3.Dot(delta, leftAxes[2]),
        };

        for (var axis = 0; axis < 3; axis++)
        {
            var rightRadius = rightExtents[0] * absoluteRotation[axis, 0]
                + rightExtents[1] * absoluteRotation[axis, 1]
                + rightExtents[2] * absoluteRotation[axis, 2];
            if (Math.Abs(translation[axis]) > leftExtents[axis] + rightRadius)
            {
                return false;
            }
        }

        for (var axis = 0; axis < 3; axis++)
        {
            var leftRadius = leftExtents[0] * absoluteRotation[0, axis]
                + leftExtents[1] * absoluteRotation[1, axis]
                + leftExtents[2] * absoluteRotation[2, axis];
            var projected = Math.Abs(translation[0] * rotation[0, axis]
                + translation[1] * rotation[1, axis]
                + translation[2] * rotation[2, axis]);
            if (projected > leftRadius + rightExtents[axis])
            {
                return false;
            }
        }

        for (var leftAxis = 0; leftAxis < 3; leftAxis++)
        {
            var leftNext = (leftAxis + 1) % 3;
            var leftLast = (leftAxis + 2) % 3;
            for (var rightAxis = 0; rightAxis < 3; rightAxis++)
            {
                var rightNext = (rightAxis + 1) % 3;
                var rightLast = (rightAxis + 2) % 3;
                var leftRadius = leftExtents[leftNext] * absoluteRotation[leftLast, rightAxis]
                    + leftExtents[leftLast] * absoluteRotation[leftNext, rightAxis];
                var rightRadius = rightExtents[rightNext] * absoluteRotation[leftAxis, rightLast]
                    + rightExtents[rightLast] * absoluteRotation[leftAxis, rightNext];
                var projected = Math.Abs(
                    translation[leftLast] * rotation[leftNext, rightAxis]
                    - translation[leftNext] * rotation[leftLast, rightAxis]);
                if (projected > leftRadius + rightRadius)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool TryValidateInserterBuild(
        PlanetFactory factory,
        Player player,
        ItemProto item,
        BuildStepPlan candidate,
        out BuildStepPlan accepted,
        out string rejection)
    {
        accepted = candidate;
        rejection = string.Empty;
        if (!BuildUiIsIdle(player))
        {
            rejection = "The player's normal build UI owns preview state.";
            return false;
        }

        var tool = new SpherewrightInserterBuildTool();
        tool._Init(GameMain.data!);
        tool.SetFactoryReferences();
        try
        {
            if (!ReferenceEquals(tool.factory, factory))
            {
                rejection = "The isolated inserter validator is not bound to the local factory.";
                return false;
            }

            tool.handItem = item;
            tool.handPrefabDesc = item.prefabDesc;
            tool.startObjectId = candidate.InputObjectId;
            tool.castObjectId = candidate.OutputObjectId;
            tool.SnapshotPlayerInventory();
            var preview = CreatePreview(candidate, item);
            tool.buildPreviews.Add(preview);
            var valid = tool.CheckBuildConditions();
            if (!valid || preview.condition != EBuildCondition.Ok || preview.coverObjId != 0)
            {
                rejection = $"DSP inserter validation returned {preview.condition}.";
                return false;
            }

            accepted = BuildStepPlan.FromPreview(candidate, preview);
            return true;
        }
        finally
        {
            tool.buildPreviews.Clear();
            tool.ReleaseSnapshot();
            tool._Free();
        }
    }

    private static bool TryValidateBeltBuild(
        PlanetFactory factory,
        Player player,
        ItemProto item,
        IReadOnlyList<BuildStepPlan> candidates,
        out List<BuildStepPlan> accepted,
        out string rejection)
    {
        accepted = new List<BuildStepPlan>();
        rejection = string.Empty;
        if (!BuildUiIsIdle(player))
        {
            rejection = "The player's normal build UI owns preview state.";
            return false;
        }

        var tool = new SpherewrightPathBuildTool();
        tool._Init(GameMain.data!);
        tool.SetFactoryReferences();
        var previews = CreateLinkedPreviews(candidates, item);
        try
        {
            if (!ReferenceEquals(tool.factory, factory))
            {
                rejection = "The isolated path-build validator is not bound to the local factory.";
                return false;
            }

            tool.handItem = item;
            tool.handPrefabDesc = item.prefabDesc;
            tool.startObjectId = candidates[0].InputObjectId;
            tool.SnapshotPlayerInventory();
            tool.buildPreviews.AddRange(previews);
            var valid = tool.CheckBuildConditions();
            var rejected = previews.FirstOrDefault(preview => preview.condition != EBuildCondition.Ok || preview.coverObjId != 0);
            if (!valid || rejected is not null)
            {
                rejection = $"DSP belt-path validation returned {rejected?.condition.ToString() ?? "rejected"}.";
                return false;
            }

            for (var index = 0; index < previews.Count; index++)
            {
                accepted.Add(BuildStepPlan.FromPreview(candidates[index], previews[index]));
            }

            return true;
        }
        finally
        {
            tool.buildPreviews.Clear();
            tool.ReleaseSnapshot();
            tool._Free();
        }
    }

    private BridgeError? RevalidateStructuredBuildOnMainThread(NormalActionPlanPayload plan)
    {
        var playerResult = _reader.GetPlayerStateOnMainThread(
            plan.SessionId,
            new LocalPlanetRequest { PlanetId = plan.PlanetId });
        if (!playerResult.Success || playerResult.Value is null)
        {
            return playerResult.Error;
        }

        var item = LDB.items.Select(plan.BuildingItemId);
        var factory = GameMain.localPlanet?.factory;
        var player = GameMain.mainPlayer;
        if (item?.prefabDesc is null || factory is null || player?.package is null
            || !GameMain.history.ItemUnlocked(plan.BuildingItemId)
            || player.package.GetItemCount(plan.BuildingItemId) < plan.BuildSteps.Count
            || !string.Equals(CanonicalStateHash.PlayerAction(playerResult.Value), plan.PlayerStateHash, StringComparison.Ordinal))
        {
            return Stale("Building unlock, owned item count, player, or construction queue changed after prepare.");
        }

        if (plan.BuildResourceNodeId > 0)
        {
            var resource = _reader.InspectResourceNodeOnMainThread(
                plan.SessionId,
                new InspectResourceNodeRequest
                {
                    PlanetId = plan.PlanetId,
                    Kind = ResourceNodeKinds.Vein,
                    NodeId = plan.BuildResourceNodeId,
                });
            if (!resource.Success || resource.Value is null
                || !string.Equals(resource.Value.StateHash, plan.BuildResourceStateHash, StringComparison.Ordinal))
            {
                return Stale("The exact resource target changed after build preparation.");
            }
        }

        if (!RevalidateBuildEndpoint(plan, plan.SourceObjectId, plan.SourceFactoryStateHash)
            || !RevalidateBuildEndpoint(plan, plan.DestinationObjectId, plan.DestinationFactoryStateHash))
        {
            return Stale("A bound build endpoint identity or connection changed after preparation.");
        }

        bool valid;
        string rejection;
        if (plan.BuildKind == NormalBuildKinds.Belt)
        {
            valid = TryValidateBeltBuild(factory, player, item, plan.BuildSteps, out var accepted, out rejection)
                && BuildStepsEqual(plan.BuildSteps, accepted);
        }
        else if (plan.BuildKind == NormalBuildKinds.Inserter)
        {
            valid = TryValidateInserterBuild(factory, player, item, plan.BuildSteps[0], out var accepted, out rejection)
                && BuildStepsEqual(plan.BuildSteps, new[] { accepted });
        }
        else
        {
            valid = TryValidateClickBuild(
                    factory,
                    player,
                    item,
                    plan.BuildSteps[0],
                    plan.BuildResourceNodeId,
                    out var accepted,
                    out rejection)
                && BuildStepsEqual(plan.BuildSteps, new[] { accepted });
        }

        return valid ? null : Stale("DSP no longer accepts the exact prepared construction plan: " + rejection);
    }

    private bool RevalidateBuildEndpoint(NormalActionPlanPayload plan, int objectId, string endpointHash)
    {
        if (objectId <= 0)
        {
            return string.IsNullOrEmpty(endpointHash);
        }

        var snapshot = _reader.InspectFactoryEntityOnMainThread(
            plan.SessionId,
            new InspectFactoryEntityRequest { PlanetId = plan.PlanetId, ObjectId = objectId });
        return snapshot.Success && snapshot.Value is not null
            && string.Equals(BuildEndpointHash(snapshot.Value), endpointHash, StringComparison.Ordinal);
    }

    private static void CreatePreparedPrebuildsOnMainThread(ActionRecord action)
    {
        var factory = GameMain.localPlanet?.factory
            ?? throw new InvalidOperationException("The local factory is unavailable.");
        var player = GameMain.mainPlayer
            ?? throw new InvalidOperationException("The player is unavailable.");
        var item = LDB.items.Select(action.Plan.BuildingItemId)
            ?? throw new InvalidOperationException("The planned building prototype disappeared.");
        var baseline = player.package.GetItemCount(item.ID);
        if (baseline < action.Plan.BuildSteps.Count)
        {
            throw new InvalidOperationException("The planned building items are no longer in inventory.");
        }

        if (!BuildUiIsIdle(player))
        {
            throw new InvalidOperationException("The normal build UI acquired preview state during commit.");
        }

        List<BuildPreview> previews;
        Action create;
        Action cleanup;
        if (action.Plan.BuildKind == NormalBuildKinds.Belt)
        {
            var tool = new SpherewrightPathBuildTool();
            tool._Init(GameMain.data!);
            tool.SetFactoryReferences();
            tool.handItem = item;
            tool.handPrefabDesc = item.prefabDesc;
            tool.startObjectId = action.Plan.BuildSteps[0].InputObjectId;
            tool.SnapshotPlayerInventory();
            previews = CreateLinkedPreviews(action.Plan.BuildSteps, item);
            tool.buildPreviews.AddRange(previews);
            if (!tool.CheckBuildConditions() || !PreviewsExactlyMatch(action.Plan.BuildSteps, previews))
            {
                tool.buildPreviews.Clear();
                tool.ReleaseSnapshot();
                tool._Free();
                throw new InvalidOperationException("DSP rejected or changed the exact prepared belt path at commit.");
            }

            create = tool.CreatePrebuilds;
            cleanup = () =>
            {
                tool.buildPreviews.Clear();
                tool.ReleaseSnapshot();
                tool._Free();
            };
        }
        else if (action.Plan.BuildKind == NormalBuildKinds.Inserter)
        {
            var tool = new SpherewrightInserterBuildTool();
            tool._Init(GameMain.data!);
            tool.SetFactoryReferences();
            tool.handItem = item;
            tool.handPrefabDesc = item.prefabDesc;
            tool.startObjectId = action.Plan.SourceObjectId;
            tool.castObjectId = action.Plan.DestinationObjectId;
            tool.SnapshotPlayerInventory();
            previews = CreateLinkedPreviews(action.Plan.BuildSteps, item);
            tool.buildPreviews.AddRange(previews);
            if (!tool.CheckBuildConditions() || !PreviewsExactlyMatch(action.Plan.BuildSteps, previews))
            {
                tool.buildPreviews.Clear();
                tool.ReleaseSnapshot();
                tool._Free();
                throw new InvalidOperationException("DSP rejected or changed the exact prepared inserter at commit.");
            }

            create = tool.CreatePrebuilds;
            cleanup = () =>
            {
                tool.buildPreviews.Clear();
                tool.ReleaseSnapshot();
                tool._Free();
            };
        }
        else
        {
            var tool = new SpherewrightClickBuildTool();
            tool._Init(GameMain.data!);
            tool.SetFactoryReferences();
            tool.handItem = item;
            tool.handPrefabDesc = item.prefabDesc;
            tool.yaw = action.Plan.BuildSteps[0].Yaw;
            tool.SnapshotPlayerInventory();
            previews = CreateLinkedPreviews(action.Plan.BuildSteps, item);
            tool.buildPreviews.AddRange(previews);
            if (!tool.CheckBuildConditions() || !PreviewsExactlyMatch(action.Plan.BuildSteps, previews))
            {
                tool.buildPreviews.Clear();
                tool.ReleaseSnapshot();
                tool._Free();
                throw new InvalidOperationException("DSP rejected or changed the exact prepared building at commit.");
            }

            create = tool.CreatePrebuilds;
            cleanup = () =>
            {
                tool.buildPreviews.Clear();
                tool.ReleaseSnapshot();
                tool._Free();
            };
        }

        try
        {
            action.PreexistingBuildEntityIds.Clear();
            if (action.Plan.BuildKind == NormalBuildKinds.Inserter)
            {
                // DSP anchors every sorter that leaves a building at the same
                // source pose.  Capture older co-located sorters before the
                // prebuild exists so completion cannot attribute this action
                // to a different, already-built sorter.
                CaptureBuiltEntityIds(
                    factory,
                    item.ID,
                    previews[0].lpos,
                    action.PreexistingBuildEntityIds);
            }
            else if (action.Plan.BuildKind == NormalBuildKinds.Belt)
            {
                // A path that starts from an existing belt can create its
                // first new belt at exactly the source belt's pose. Capture
                // every older co-located belt before CreatePrebuilds so the
                // completion readback maps each step to the newly built
                // entity instead of inserting the source belt into the path.
                foreach (var preview in previews)
                {
                    CaptureBuiltEntityIds(
                        factory,
                        item.ID,
                        preview.lpos,
                        action.PreexistingBuildEntityIds);
                }
            }

            create();
            foreach (var preview in previews)
            {
                if (preview.objId >= 0)
                {
                    throw new InvalidOperationException("DSP did not return an ordinary prebuild object ID for every step.");
                }

                action.PrebuildIds.Add(-preview.objId);
                action.ExpectedBuildEntities.Add(new BuildExpectedEntity
                {
                    ItemId = item.ID,
                    Position = preview.lpos,
                    InputObjectId = preview.inputObjId,
                    OutputObjectId = preview.outputObjId,
                    InputStepIndex = action.Plan.BuildSteps[previews.IndexOf(preview)].InputStepIndex,
                    OutputStepIndex = action.Plan.BuildSteps[previews.IndexOf(preview)].OutputStepIndex,
                });
            }

            if (player.package.GetItemCount(item.ID) != baseline - previews.Count)
            {
                throw new InvalidOperationException("The accepted prebuild set did not consume exactly the planned owned items.");
            }
        }
        finally
        {
            cleanup();
        }
    }

    private void UpdatePreparedBuildOnMainThread(ActionRecord action)
    {
        var factory = GameMain.localPlanet?.factory;
        if (factory is null)
        {
            return;
        }

        var anyAlive = action.PrebuildIds.Any(prebuildId =>
            prebuildId > 0
            && prebuildId < factory.prebuildCursor
            && prebuildId < factory.prebuildPool.Length
            && factory.prebuildPool[prebuildId].id == prebuildId
            && !factory.prebuildPool[prebuildId].isDestroyed);
        if (anyAlive)
        {
            if (GameMain.gameTick > action.StartedAtGameTick + Math.Max(72000, action.Plan.EstimatedTicks * 20))
            {
                QuarantineBuildOutcome(action, "Ordinary prebuilds remained unfinished beyond the bounded game-tick window.");
            }

            return;
        }

        var resolved = new List<int>();
        foreach (var expected in action.ExpectedBuildEntities)
        {
            var entityId = FindBuiltEntityExcluding(
                factory,
                expected.ItemId,
                expected.Position,
                action.PreexistingBuildEntityIds,
                resolved);
            if (entityId <= 0)
            {
                QuarantineBuildOutcome(action, "An accepted prebuild disappeared without a provable matching built entity.");
                return;
            }

            resolved.Add(entityId);
        }

        if (!VerifyBuiltTopology(factory, action.Plan, resolved, out var rejection))
        {
            QuarantineBuildOutcome(action, "Built-entity topology readback failed: " + rejection);
            return;
        }

        action.TargetObjectIds = resolved;
        action.TargetObjectId = resolved.Count == 1 ? resolved[0] : (int?)null;
        Complete(action, $"Construction drones completed all {resolved.Count} ordinary prebuild(s), and entity/component/connection readback matched the plan.");
    }

    private void QuarantineBuildOutcome(ActionRecord action, string message)
    {
        action.State = NormalActionStates.OutcomeUnknown;
        action.Terminal = true;
        action.CompletedAtGameTick = GameMain.gameTick;
        action.Message = message;
        action.OriginalOutcomeMessage = message;
        action.AfterInventory = CaptureInventory(GameMain.mainPlayer);
        _sessions.QuarantineWritesOnMainThread(action.ActionId, message);
    }

    private static bool VerifyBuiltTopology(
        PlanetFactory factory,
        NormalActionPlanPayload plan,
        IReadOnlyList<int> entityIds,
        out string rejection)
    {
        rejection = string.Empty;
        if (plan.BuildKind == NormalBuildKinds.Resource)
        {
            ref var entity = ref factory.entityPool[entityIds[0]];
            if (entity.minerId <= 0 || entity.minerId >= factory.factorySystem.minerCursor)
            {
                rejection = "The resource building has no valid miner component.";
                return false;
            }

            ref var miner = ref factory.factorySystem.minerPool[entity.minerId];
            if (miner.veins is null || !miner.veins.Take(Math.Min(miner.veinCount, miner.veins.Length)).Contains(plan.BuildResourceNodeId))
            {
                rejection = "The built miner did not bind the exact prepared resource node.";
                return false;
            }
        }

        if (plan.BuildKind == NormalBuildKinds.Inserter)
        {
            ref var entity = ref factory.entityPool[entityIds[0]];
            if (entity.inserterId <= 0 || entity.inserterId >= factory.factorySystem.inserterCursor)
            {
                rejection = "The built sorter has no valid inserter component.";
                return false;
            }

            ref var inserter = ref factory.factorySystem.inserterPool[entity.inserterId];
            if (inserter.pickTarget != plan.SourceObjectId || inserter.insertTarget != plan.DestinationObjectId)
            {
                rejection = $"Sorter readback was {inserter.pickTarget}->{inserter.insertTarget}, not {plan.SourceObjectId}->{plan.DestinationObjectId}.";
                return false;
            }

            var step = plan.BuildSteps[0];
            if (!ObjectConnectionMatches(
                    factory,
                    plan.SourceObjectId,
                    step.InputFromSlot,
                    expectedIsOutput: true,
                    entityIds[0]))
            {
                rejection = $"Prepared source slot {step.InputFromSlot} does not point to the built sorter {entityIds[0]}.";
                return false;
            }

            if (!ObjectConnectionMatches(
                    factory,
                    plan.DestinationObjectId,
                    step.OutputToSlot,
                    expectedIsOutput: false,
                    entityIds[0]))
            {
                rejection = $"Prepared destination slot {step.OutputToSlot} does not point to the built sorter {entityIds[0]}.";
                return false;
            }
        }

        if (plan.BuildKind == NormalBuildKinds.Belt)
        {
            for (var index = 0; index < entityIds.Count; index++)
            {
                ref var entity = ref factory.entityPool[entityIds[index]];
                if (entity.beltId <= 0)
                {
                    rejection = $"Path entity {entityIds[index]} has no belt component.";
                    return false;
                }

                var expectedOutput = index + 1 < entityIds.Count
                    ? entityIds[index + 1]
                    : plan.DestinationObjectId;
                factory.ReadObjectConn(entityIds[index], 0, out var isOutput, out var otherObjectId, out _);
                if (!BeltConnectionProof.OutputMatches(expectedOutput, isOutput, otherObjectId))
                {
                    rejection = expectedOutput > 0
                        ? $"Belt segment {entityIds[index]} output is not connected to {expectedOutput}."
                        : $"Belt segment {entityIds[index]} unexpectedly has an output-side connection to object {otherObjectId}.";
                    return false;
                }
            }

            if (plan.SourceObjectId > 0)
            {
                factory.ReadObjectConn(entityIds[0], 1, out var isOutput, out var otherObjectId, out _);
                if (isOutput || otherObjectId != plan.SourceObjectId)
                {
                    rejection = "The first belt segment is not connected to the prepared source port.";
                    return false;
                }
            }
        }

        return true;
    }

    private static bool ObjectConnectionMatches(
        PlanetFactory factory,
        int objectId,
        int slot,
        bool expectedIsOutput,
        int expectedOtherObjectId)
    {
        if (objectId <= 0)
        {
            return false;
        }

        foreach (var candidateSlot in BuildConnectionSlots.SelectVerificationCandidates(slot, 16))
        {
            factory.ReadObjectConn(objectId, candidateSlot, out var isOutput, out var otherObjectId, out _);
            if (isOutput == expectedIsOutput && otherObjectId == expectedOtherObjectId)
            {
                return true;
            }
        }

        return false;
    }

    private GameCallResult<PreparedNormalAction> PrepareStorageTransferOnMainThread(
        string? requestedSessionId,
        PrepareTransferRequest request)
    {
        var common = ValidatePrepareCommon(requestedSessionId, request.PlanetId, request.StateHashVersion);
        if (common.Error is not null)
        {
            return GameCallResult<PreparedNormalAction>.Failed(common.Error);
        }

        if (request.Direction != TransferDirections.PlayerToStorage
            && request.Direction != TransferDirections.StorageToPlayer)
        {
            return InvalidPlan("Transfer direction must be player-to-storage or storage-to-player.");
        }

        if (request.ItemId <= 0 || request.Count <= 0 || request.Count > 10000)
        {
            return InvalidPlan("Transfer item and count must be positive; count is bounded to 10000 per action.");
        }

        var playerResult = _reader.GetPlayerStateOnMainThread(
            requestedSessionId,
            new LocalPlanetRequest { PlanetId = request.PlanetId });
        var storageResult = _reader.InspectFactoryEntityOnMainThread(
            requestedSessionId,
            new InspectFactoryEntityRequest { PlanetId = request.PlanetId, ObjectId = request.StorageEntityId });
        if (!playerResult.Success || playerResult.Value is null)
        {
            return GameCallResult<PreparedNormalAction>.Failed(playerResult.Error!);
        }

        if (!storageResult.Success || storageResult.Value is null)
        {
            return GameCallResult<PreparedNormalAction>.Failed(storageResult.Error!);
        }

        var storageSnapshot = storageResult.Value;
        if (!string.Equals(playerResult.Value.StateHash, request.ExpectedPlayerStateHash, StringComparison.Ordinal)
            || !string.Equals(storageSnapshot.StateHash, request.ExpectedStorageStateHash, StringComparison.Ordinal))
        {
            return StalePlan("Player inventory or exact storage contents changed after inspection.");
        }

        if (storageSnapshot.ObjectKind != FactoryObjectKinds.Entity
            || !string.Equals(storageSnapshot.ComponentKind, "storage", StringComparison.Ordinal))
        {
            return GameCallResult<PreparedNormalAction>.Failed(BridgeError.Create(
                BridgeErrorCodes.InvalidEntity,
                "The transfer target is not an exact built storage component.",
                false,
                "Inspect a built storage entity and use its object ID."));
        }

        var factory = GameMain.localPlanet?.factory;
        var player = GameMain.mainPlayer;
        if (factory is null || player?.package is null
            || !TryGetStorage(factory, request.StorageEntityId, out var storage))
        {
            return NotReadyPlan("The exact storage component is unavailable.");
        }

        var distance = Vector3.Distance(player.position, ToVector(storageSnapshot.Position));
        if (distance > player.mecha.buildArea)
        {
            return GameCallResult<PreparedNormalAction>.Failed(BridgeError.Create(
                BridgeErrorCodes.TargetOutOfRange,
                $"The storage is {distance:F2} metres away, outside the current normal interaction/build area.",
                true,
                "Move into range through spherewright_prepare_move, then inspect and prepare again."));
        }

        if (!CanTransferExactly(player.package, storage!, request.Direction, request.ItemId, request.Count, out var rejection))
        {
            return GameCallResult<PreparedNormalAction>.Failed(BridgeError.Create(
                rejection.IndexOf("source", StringComparison.OrdinalIgnoreCase) >= 0
                    ? BridgeErrorCodes.InventoryInsufficient
                    : BridgeErrorCodes.InventoryFull,
                rejection,
                true,
                "Adjust the count or free destination capacity, then inspect and prepare again."));
        }

        var playerActionHash = CanonicalStateHash.PlayerAction(playerResult.Value);
        var expectedHash = CanonicalStateHash.Combine(
            NormalActionKinds.Transfer,
            _sessions.SessionId,
            request.PlanetId,
            playerActionHash,
            storageSnapshot.StateHash,
            request.StorageEntityId,
            request.Direction,
            request.ItemId,
            request.Count);
        var payload = NormalActionPlanPayload.Transfer(
            _sessions.SessionId!,
            request.PlanetId,
            expectedHash,
            playerActionHash,
            storageSnapshot.StateHash,
            request.StorageEntityId,
            request.Direction,
            request.ItemId,
            request.Count);
        var prepared = AddPreparedPlan(
            payload,
            common.Session!,
            1,
            "The source decreases and destination increases by the exact requested count while their combined item count is conserved.");
        if (prepared.Success && prepared.Value is not null)
        {
            prepared.Value.SourceObjectId = request.Direction == TransferDirections.StorageToPlayer
                ? request.StorageEntityId
                : (int?)null;
            prepared.Value.DestinationObjectId = request.Direction == TransferDirections.PlayerToStorage
                ? request.StorageEntityId
                : (int?)null;
            prepared.Value.EstimatedDistance = distance;
            prepared.Value.ItemBudget.Add(new ActionItemBudget
            {
                ItemId = request.ItemId,
                Name = LDB.items.Select(request.ItemId)?.name ?? string.Empty,
                Count = request.Count,
                Direction = request.Direction,
            });
        }

        return prepared;
    }

    private BridgeError? RevalidateStorageTransferOnMainThread(NormalActionPlanPayload plan)
    {
        var playerResult = _reader.GetPlayerStateOnMainThread(
            plan.SessionId,
            new LocalPlanetRequest { PlanetId = plan.PlanetId });
        var storageResult = _reader.InspectFactoryEntityOnMainThread(
            plan.SessionId,
            new InspectFactoryEntityRequest { PlanetId = plan.PlanetId, ObjectId = plan.TransferStorageEntityId });
        if (!playerResult.Success || playerResult.Value is null)
        {
            return playerResult.Error;
        }

        if (!storageResult.Success || storageResult.Value is null)
        {
            return storageResult.Error;
        }

        if (!string.Equals(CanonicalStateHash.PlayerAction(playerResult.Value), plan.PlayerStateHash, StringComparison.Ordinal)
            || !string.Equals(storageResult.Value.StateHash, plan.TransferStorageStateHash, StringComparison.Ordinal))
        {
            return Stale("Player inventory or storage contents changed after transfer preparation.");
        }

        var factory = GameMain.localPlanet?.factory;
        var player = GameMain.mainPlayer;
        if (factory is null || player?.package is null
            || !TryGetStorage(factory, plan.TransferStorageEntityId, out var storage)
            || !CanTransferExactly(player.package, storage!, plan.TransferDirection, plan.TransferItemId, plan.Count, out _))
        {
            return Stale("Transfer source count, destination capacity, or exact storage identity changed.");
        }

        return null;
    }

    private static void ExecuteStorageTransferOnMainThread(ActionRecord action)
    {
        var factory = GameMain.localPlanet?.factory
            ?? throw new InvalidOperationException("The local factory is unavailable.");
        var player = GameMain.mainPlayer
            ?? throw new InvalidOperationException("The player is unavailable.");
        var plan = action.Plan;
        if (!TryGetStorage(factory, plan.TransferStorageEntityId, out var storage))
        {
            throw new InvalidOperationException("The exact storage component disappeared.");
        }

        var playerBefore = player.package.GetItemCount(plan.TransferItemId);
        var storageBefore = storage!.GetItemCount(plan.TransferItemId);
        var source = plan.TransferDirection == TransferDirections.PlayerToStorage ? player.package : storage;
        var destination = plan.TransferDirection == TransferDirections.PlayerToStorage ? storage : player.package;
        var removed = source.TakeItem(plan.TransferItemId, plan.Count, out var removedInc);
        if (removed != plan.Count)
        {
            throw new InvalidOperationException("The exact transfer source did not remove the prepared count.");
        }

        var added = destination.AddItemStacked(plan.TransferItemId, removed, removedInc, out var remainingInc);
        if (added != removed || remainingInc != 0)
        {
            throw new InvalidOperationException("The exact transfer destination did not accept the prepared count.");
        }

        if (ReferenceEquals(destination, player.package))
        {
            player.NotifyPackageAddItem(plan.TransferItemId, added, removedInc);
        }

        var playerAfter = player.package.GetItemCount(plan.TransferItemId);
        var storageAfter = storage.GetItemCount(plan.TransferItemId);
        var expectedPlayerDelta = plan.TransferDirection == TransferDirections.PlayerToStorage ? -plan.Count : plan.Count;
        if (playerAfter - playerBefore != expectedPlayerDelta
            || storageAfter - storageBefore != -expectedPlayerDelta
            || playerBefore + storageBefore != playerAfter + storageAfter)
        {
            throw new InvalidOperationException("Post-transfer readback did not prove exact bilateral conservation.");
        }

        action.TargetObjectId = plan.TransferStorageEntityId;
        action.TargetItemId = plan.TransferItemId;
        action.BeforeTargetAmount = storageBefore;
        action.AfterTargetAmount = storageAfter;
        action.Message = $"Normal storage transfer conserved item {plan.TransferItemId}: player {playerBefore}->{playerAfter}, storage {storageBefore}->{storageAfter}.";
        action.State = NormalActionStates.Completed;
        action.Terminal = true;
        action.Succeeded = true;
        action.CompletedAtGameTick = GameMain.gameTick;
        action.AfterInventory = CaptureInventory(player);
        action.AfterStateHash = CanonicalStateHash.Combine(
            NormalActionKinds.Transfer,
            playerAfter,
            storageAfter,
            plan.TransferItemId,
            plan.Count);
    }

    private GameCallResult<PreparedNormalAction> PrepareStructuredConfigurationOnMainThread(
        string? requestedSessionId,
        PrepareConfigureBuildingRequest request)
    {
        var common = ValidatePrepareCommon(requestedSessionId, request.PlanetId, request.StateHashVersion);
        if (common.Error is not null)
        {
            return GameCallResult<PreparedNormalAction>.Failed(common.Error);
        }

        if (request.Mode != BuildingConfigurationModes.Production
            && request.Mode != BuildingConfigurationModes.Research
            && request.Mode != BuildingConfigurationModes.SorterFilter
            && request.Mode != BuildingConfigurationModes.LogisticsStationStorage)
        {
            return InvalidPlan("Configuration mode must be production, research, sorter-filter, or logistics-station-storage.");
        }

        var stationLocalLogic = ELogisticStorage.None;
        var stationRemoteLogic = ELogisticStorage.None;
        if (request.Mode == BuildingConfigurationModes.LogisticsStationStorage
            && (!TryParseLogisticsStorageLogic(request.StationLocalLogic, out stationLocalLogic)
                || !TryParseLogisticsStorageLogic(request.StationRemoteLogic, out stationRemoteLogic)))
        {
            return InvalidPlan("Station local and remote logic must each be none, supply, or demand.");
        }

        var snapshotResult = _reader.InspectFactoryEntityOnMainThread(
            requestedSessionId,
            new InspectFactoryEntityRequest { PlanetId = request.PlanetId, ObjectId = request.EntityId });
        if (!snapshotResult.Success || snapshotResult.Value is null)
        {
            return GameCallResult<PreparedNormalAction>.Failed(snapshotResult.Error!);
        }

        var snapshot = snapshotResult.Value;
        if (!string.Equals(request.ExpectedFactoryStateHash, snapshot.StateHash, StringComparison.Ordinal))
        {
            return StalePlan("Building mode, recipe, progress, buffers, or identity changed after inspection.");
        }

        var stationMode = request.Mode == BuildingConfigurationModes.LogisticsStationStorage;
        if (snapshot.ObjectKind != FactoryObjectKinds.Entity
            || (!stationMode && (snapshot.Progress != 0
                || snapshot.IsWorking
                || snapshot.Buffers.Any(buffer => buffer.Count != 0))))
        {
            return GameCallResult<PreparedNormalAction>.Failed(BridgeError.Create(
                BridgeErrorCodes.ActionRejected,
                stationMode
                    ? "The logistics-station target is not a completed built entity."
                    : "Only a fully idle built device with empty input, output, and internal buffers can be configured.",
                true,
                stationMode
                    ? "Wait for normal construction to finish, then inspect the exact station entity again."
                    : "Wait for the exact device to become idle and empty, then inspect and prepare again."));
        }

        var factory = GameMain.localPlanet?.factory;
        if (factory is null)
        {
            return NotReadyPlan("The local factory is unavailable.");
        }

        RecipeProto? recipe = null;
        if (request.Mode == BuildingConfigurationModes.Production)
        {
            recipe = LDB.recipes.Select(request.RecipeId);
            if (recipe is null || !GameMain.history.RecipeUnlocked(request.RecipeId))
            {
                return GameCallResult<PreparedNormalAction>.Failed(BridgeError.Create(
                    BridgeErrorCodes.InvalidRecipe,
                    "The requested runtime recipe does not exist or is not unlocked.",
                    false,
                    "Complete the normal prerequisite technology and choose an unlocked recipe."));
            }

            if (!CanDeviceRunRecipe(factory, request.EntityId, recipe, out var reason))
            {
                return GameCallResult<PreparedNormalAction>.Failed(BridgeError.Create(
                    BridgeErrorCodes.RecipeNotSupportedByBuilding,
                    reason,
                    false,
                    "Choose a recipe whose runtime type matches the exact built device."));
            }
        }
        else if (request.Mode == BuildingConfigurationModes.Research
                 && !CanLabEnterResearchMode(factory, request.EntityId, request.TechId, out var reason))
        {
            return GameCallResult<PreparedNormalAction>.Failed(BridgeError.Create(
                BridgeErrorCodes.InvalidTechnology,
                reason,
                false,
                "Select an active matrix technology through the normal research queue, then configure an empty matrix lab."));
        }
        else if (request.Mode == BuildingConfigurationModes.SorterFilter
                 && !CanSetSorterFilter(factory, request.EntityId, request.FilterItemId, out var sorterReason))
        {
            return GameCallResult<PreparedNormalAction>.Failed(BridgeError.Create(
                BridgeErrorCodes.ActionRejected,
                sorterReason,
                true,
                "Wait until the exact sorter is idle and carrying no cargo, then inspect and prepare again with an unlocked filter item or zero to clear it."));
        }
        else if (request.Mode == BuildingConfigurationModes.LogisticsStationStorage)
        {
            if (snapshot.LogisticsStation is null
                || !string.Equals(
                    request.ExpectedStationConfigurationStateHash,
                    snapshot.LogisticsStation.ConfigurationStateHash,
                    StringComparison.Ordinal))
            {
                return StalePlan("The logistics-station identity, storage-slot configuration, route settings, or belt topology changed after inspection.");
            }

            if (!CanConfigureLogisticsStationStorage(
                    factory,
                    request.EntityId,
                    request.StationStorageIndex,
                    request.StationItemId,
                    request.StationMaximumCount,
                    stationLocalLogic,
                    stationRemoteLogic,
                    out var stationReason))
            {
                return GameCallResult<PreparedNormalAction>.Failed(BridgeError.Create(
                    BridgeErrorCodes.ActionRejected,
                    stationReason,
                    true,
                    "Choose an unlocked item and an empty or same-item slot with no outstanding orders; use 100-item limit steps within the station's current researched capacity."));
            }
        }

        var normalizedStationLocalLogic = ToContractLogisticsStorageLogic(stationLocalLogic);
        var normalizedStationRemoteLogic = ToContractLogisticsStorageLogic(stationRemoteLogic);

        var expectedHash = CanonicalStateHash.Combine(
            NormalActionKinds.ConfigureBuilding,
            _sessions.SessionId,
            request.PlanetId,
            snapshot.StateHash,
            request.EntityId,
            request.Mode,
            request.RecipeId,
            request.TechId,
            request.FilterItemId,
            request.ExpectedStationConfigurationStateHash,
            request.StationStorageIndex,
            request.StationItemId,
            request.StationMaximumCount,
            normalizedStationLocalLogic,
            normalizedStationRemoteLogic);
        var payload = NormalActionPlanPayload.Configure(
            _sessions.SessionId!,
            request.PlanetId,
            expectedHash,
            snapshot.StateHash,
            request.EntityId,
            request.RecipeId,
            request.Mode,
            request.TechId,
            request.FilterItemId,
            request.ExpectedStationConfigurationStateHash,
            request.StationStorageIndex,
            request.StationItemId,
            request.StationMaximumCount,
            normalizedStationLocalLogic,
            normalizedStationRemoteLogic);
        var prepared = AddPreparedPlan(
            payload,
            common.Session!,
            1,
            request.Mode == BuildingConfigurationModes.Research
                ? "The exact empty matrix lab reports research mode and the active technology after the UI/business setting path is called once."
                : request.Mode == BuildingConfigurationModes.SorterFilter
                    ? "The exact idle sorter reports the target item filter and matching entity sign after the current-version UI setting path is applied once."
                    : request.Mode == BuildingConfigurationModes.LogisticsStationStorage
                        ? "The exact station slot reports the selected unlocked item, 100-step limit, and local/remote logic after PlanetTransport.SetStationStorage is called once; item count and proliferator points remain unchanged by the call."
                    : "The exact idle device reports the target recipe after the current-version UI/business setting path is called once.");
        if (prepared.Success && prepared.Value is not null && recipe is not null)
        {
            AddRecipeBudget(prepared.Value, recipe, 1);
        }

        return prepared;
    }

    private BridgeError? RevalidateStructuredConfigurationOnMainThread(NormalActionPlanPayload plan)
    {
        var factory = GameMain.localPlanet?.factory;
        if (factory is null)
        {
            return Stale("The local factory disappeared after configuration preparation.");
        }

        if (plan.ConfigureMode == BuildingConfigurationModes.Research)
        {
            return CanLabEnterResearchMode(factory, plan.EntityId, plan.ConfigureTechId, out _)
                ? null
                : Stale("The exact lab or active matrix technology changed after preparation.");
        }

        if (plan.ConfigureMode == BuildingConfigurationModes.SorterFilter)
        {
            return CanSetSorterFilter(factory, plan.EntityId, plan.ConfigureFilterItemId, out _)
                ? null
                : Stale("The exact sorter identity, idle state, cargo state, or filter item changed after preparation.");
        }

        if (plan.ConfigureMode == BuildingConfigurationModes.LogisticsStationStorage)
        {
            var snapshotResult = _reader.InspectFactoryEntityOnMainThread(
                plan.SessionId,
                new InspectFactoryEntityRequest { PlanetId = plan.PlanetId, ObjectId = plan.EntityId });
            var stationSnapshot = snapshotResult.Value?.LogisticsStation;
            if (!snapshotResult.Success
                || stationSnapshot is null
                || !string.Equals(
                    stationSnapshot.ConfigurationStateHash,
                    plan.StationConfigurationStateHash,
                    StringComparison.Ordinal))
            {
                return Stale("The exact station identity or configuration changed after preparation.");
            }

            return TryParseLogisticsStorageLogic(plan.ConfigureStationLocalLogic, out var localLogic)
                   && TryParseLogisticsStorageLogic(plan.ConfigureStationRemoteLogic, out var remoteLogic)
                   && CanConfigureLogisticsStationStorage(
                       factory,
                       plan.EntityId,
                       plan.ConfigureStationStorageIndex,
                       plan.ConfigureStationItemId,
                       plan.ConfigureStationMaximumCount,
                       localLogic,
                       remoteLogic,
                       out _)
                ? null
                : Stale("The exact station slot, item unlock, capacity, current item, or outstanding orders changed after preparation.");
        }

        var recipe = LDB.recipes.Select(plan.ConfigureRecipeId);
        return recipe is not null
               && GameMain.history.RecipeUnlocked(plan.ConfigureRecipeId)
               && CanDeviceRunRecipe(factory, plan.EntityId, recipe, out _)
            ? null
            : Stale("The exact device no longer supports the prepared unlocked recipe.");
    }

    private static bool CanLabEnterResearchMode(
        PlanetFactory factory,
        int entityId,
        int techId,
        out string reason)
    {
        reason = "The exact entity is not a matrix lab or the requested matrix technology is not the current normal research target.";
        if (entityId <= 0 || entityId >= factory.entityCursor || entityId >= factory.entityPool.Length)
        {
            return false;
        }

        ref var entity = ref factory.entityPool[entityId];
        var item = entity.id == entityId ? LDB.items.Select(entity.protoId) : null;
        var tech = LDB.techs.Select(techId);
        return item?.prefabDesc?.isLab == true
               && entity.labId > 0
               && tech is not null
               && tech.IsLabTech
               && GameMain.history.currentTech == techId
               && !GameMain.history.TechUnlocked(techId);
    }

    private static bool TryParseLogisticsStorageLogic(string? value, out ELogisticStorage logic)
    {
        if (string.Equals(value, LogisticsStorageLogics.None, StringComparison.OrdinalIgnoreCase))
        {
            logic = ELogisticStorage.None;
            return true;
        }

        if (string.Equals(value, LogisticsStorageLogics.Supply, StringComparison.OrdinalIgnoreCase))
        {
            logic = ELogisticStorage.Supply;
            return true;
        }

        if (string.Equals(value, LogisticsStorageLogics.Demand, StringComparison.OrdinalIgnoreCase))
        {
            logic = ELogisticStorage.Demand;
            return true;
        }

        logic = ELogisticStorage.None;
        return false;
    }

    private static string ToContractLogisticsStorageLogic(ELogisticStorage logic) => logic switch
    {
        ELogisticStorage.Supply => LogisticsStorageLogics.Supply,
        ELogisticStorage.Demand => LogisticsStorageLogics.Demand,
        _ => LogisticsStorageLogics.None,
    };

    private static bool CanConfigureLogisticsStationStorage(
        PlanetFactory factory,
        int entityId,
        int storageIndex,
        int itemId,
        int maximumCount,
        ELogisticStorage localLogic,
        ELogisticStorage remoteLogic,
        out string reason)
    {
        reason = "The exact logistics-station storage slot is unavailable for this configuration.";
        if (entityId <= 0
            || entityId >= factory.entityCursor
            || entityId >= factory.entityPool.Length
            || itemId <= 0
            || maximumCount <= 0
            || maximumCount % 100 != 0)
        {
            return false;
        }

        ref var entity = ref factory.entityPool[entityId];
        if (entity.id != entityId || entity.stationId <= 0)
        {
            return false;
        }

        var station = factory.transport?.GetStationComponent(entity.stationId);
        if (station is null
            || station.id != entity.stationId
            || station.entityId != entityId
            || station.planetId != factory.planetId
            || station.isCollector
            || station.isVeinCollector
            || station.storage is null
            || storageIndex < 0
            || storageIndex >= station.storage.Length)
        {
            return false;
        }

        if (!station.isStellar && remoteLogic != ELogisticStorage.None)
        {
            reason = "A planetary logistics station requires remote logic none.";
            return false;
        }

        if (LDB.items.Select(itemId) is null || !GameMain.history.ItemUnlocked(itemId))
        {
            reason = "The requested station item does not exist or is not normally unlocked.";
            return false;
        }

        var model = LDB.models.Select(entity.modelIndex);
        var baseCapacity = model?.prefabDesc?.stationMaxItemCount ?? 0;
        var researchedExtraCapacity = station.isStellar
            ? GameMain.history.remoteStationExtraStorage
            : GameMain.history.localStationExtraStorage;
        var capacity = baseCapacity + researchedExtraCapacity;
        if (capacity <= 0 || maximumCount > capacity)
        {
            reason = $"The requested maximum {maximumCount} exceeds the station's current researched capacity {capacity}.";
            return false;
        }

        for (var index = 0; index < station.storage.Length; index++)
        {
            if (index != storageIndex && station.storage[index].itemId == itemId)
            {
                reason = "The requested item is already assigned to another slot in this station.";
                return false;
            }
        }

        var current = station.storage[storageIndex];
        if (current.itemId != 0 && current.itemId != itemId)
        {
            reason = "This action never replaces or clears an occupied station slot; choose an empty slot or keep the same item.";
            return false;
        }

        if (current.localOrder != 0 || current.remoteOrder != 0)
        {
            reason = "The station slot has outstanding logistics orders and must become idle before configuration.";
            return false;
        }

        if (current.itemId == 0 && (current.count != 0 || current.inc != 0))
        {
            reason = "The nominally empty station slot contains unexplained inventory state.";
            return false;
        }

        if (current.itemId == itemId
            && current.max == maximumCount
            && current.localLogic == localLogic
            && current.remoteLogic == remoteLogic)
        {
            reason = "The requested station storage configuration is already applied.";
            return false;
        }

        return true;
    }

    private static bool CanSetSorterFilter(
        PlanetFactory factory,
        int entityId,
        int filterItemId,
        out string reason)
    {
        reason = "The exact entity is not an idle empty sorter, or the requested filter item is unavailable.";
        if (entityId <= 0 || entityId >= factory.entityCursor || entityId >= factory.entityPool.Length
            || entityId >= factory.entitySignPool.Length
            || filterItemId < 0)
        {
            return false;
        }

        ref var entity = ref factory.entityPool[entityId];
        if (entity.id != entityId || entity.inserterId <= 0
            || entity.inserterId >= factory.factorySystem.inserterCursor
            || entity.inserterId >= factory.factorySystem.inserterPool.Length)
        {
            return false;
        }

        ref var inserter = ref factory.factorySystem.inserterPool[entity.inserterId];
        if (inserter.id != entity.inserterId || inserter.entityId != entityId
            || inserter.pickTarget == 0 || inserter.insertTarget == 0
            || inserter.stage != EInserterStage.Picking
            || inserter.time != 0 || inserter.itemId != 0 || inserter.itemCount != 0
            || inserter.stackCount != 0 || inserter.itemInc != 0)
        {
            return false;
        }

        if (filterItemId == 0)
        {
            return true;
        }

        return LDB.items.Select(filterItemId) is not null
               && GameMain.history.ItemUnlocked(filterItemId);
    }

    private static bool IsLabInResearchMode(int entityId, int techId)
    {
        var factory = GameMain.localPlanet?.factory;
        if (factory is null || entityId <= 0 || entityId >= factory.entityCursor)
        {
            return false;
        }

        ref var entity = ref factory.entityPool[entityId];
        if (entity.id != entityId || entity.labId <= 0 || entity.labId >= factory.factorySystem.labCursor)
        {
            return false;
        }

        ref var lab = ref factory.factorySystem.labPool[entity.labId];
        return lab.id == entity.labId && lab.researchMode && lab.techId == techId && lab.recipeId == 0;
    }

    private static bool IsSorterFilterApplied(int entityId, int filterItemId)
    {
        var factory = GameMain.localPlanet?.factory;
        if (factory is null || entityId <= 0 || entityId >= factory.entityCursor
            || entityId >= factory.entityPool.Length || entityId >= factory.entitySignPool.Length)
        {
            return false;
        }

        ref var entity = ref factory.entityPool[entityId];
        if (entity.id != entityId || entity.inserterId <= 0
            || entity.inserterId >= factory.factorySystem.inserterCursor
            || entity.inserterId >= factory.factorySystem.inserterPool.Length)
        {
            return false;
        }

        ref var inserter = ref factory.factorySystem.inserterPool[entity.inserterId];
        ref var sign = ref factory.entitySignPool[entityId];
        return inserter.id == entity.inserterId
               && inserter.entityId == entityId
               && inserter.filter == filterItemId
               && sign.iconId0 == (uint)filterItemId
               && sign.iconType == (filterItemId > 0 ? 1u : 0u);
    }

    private static bool IsLogisticsStationStorageConfigurationApplied(
        FactoryEntitySnapshot snapshot,
        NormalActionPlanPayload plan)
    {
        var slot = snapshot.LogisticsStation?.StorageSlots.FirstOrDefault(candidate =>
            candidate.Index == plan.ConfigureStationStorageIndex);
        return slot is not null
               && slot.ItemId == plan.ConfigureStationItemId
               && slot.MaximumCount == plan.ConfigureStationMaximumCount
               && string.Equals(slot.LocalLogic, plan.ConfigureStationLocalLogic, StringComparison.OrdinalIgnoreCase)
               && string.Equals(slot.RemoteLogic, plan.ConfigureStationRemoteLogic, StringComparison.OrdinalIgnoreCase);
    }

    private string? CaptureStructuredAfterStateHash(ActionRecord action)
    {
        if (action.ActionKind == NormalActionKinds.ConfigureBuilding)
        {
            var snapshot = _reader.InspectFactoryEntityOnMainThread(
                action.SessionId,
                new InspectFactoryEntityRequest { PlanetId = action.PlanetId, ObjectId = action.Plan.EntityId });
            return action.Plan.ConfigureMode == BuildingConfigurationModes.LogisticsStationStorage
                ? snapshot.Value?.LogisticsStation?.ConfigurationStateHash
                : snapshot.Value?.StateHash;
        }

        if (action.ActionKind == NormalActionKinds.Build && action.TargetObjectIds.Count > 0)
        {
            var hashes = new List<object?> { action.Plan.BuildKind, action.TargetObjectIds.Count };
            foreach (var objectId in action.TargetObjectIds)
            {
                var snapshot = _reader.InspectFactoryEntityOnMainThread(
                    action.SessionId,
                    new InspectFactoryEntityRequest { PlanetId = action.PlanetId, ObjectId = objectId });
                hashes.Add(snapshot.Value?.StateHash);
            }

            return CanonicalStateHash.Combine(NormalActionKinds.Build, hashes.ToArray());
        }

        return null;
    }

    private static bool CanTransferExactly(
        StorageComponent playerPackage,
        StorageComponent storage,
        string direction,
        int itemId,
        int count,
        out string rejection)
    {
        rejection = string.Empty;
        var source = direction == TransferDirections.PlayerToStorage ? playerPackage : storage;
        var destination = direction == TransferDirections.PlayerToStorage ? storage : playerPackage;
        using var sourceCopy = new StorageCopy(source);
        using var destinationCopy = new StorageCopy(destination);
        var removed = sourceCopy.Value.TakeItem(itemId, count, out var inc);
        if (removed != count)
        {
            rejection = $"The exact transfer source contains fewer than {count} of item {itemId}.";
            return false;
        }

        var added = destinationCopy.Value.AddItemStacked(itemId, count, inc, out var remainingInc);
        if (added != count || remainingInc != 0)
        {
            rejection = $"The exact transfer destination cannot accept {count} of item {itemId}.";
            return false;
        }

        return true;
    }

    private static bool TryGetStorage(PlanetFactory factory, int entityId, out StorageComponent? storage)
    {
        storage = null;
        if (entityId <= 0 || entityId >= factory.entityCursor || entityId >= factory.entityPool.Length)
        {
            return false;
        }

        ref var entity = ref factory.entityPool[entityId];
        if (entity.id != entityId || entity.storageId <= 0
            || entity.storageId >= factory.factoryStorage.storageCursor
            || entity.storageId >= factory.factoryStorage.storagePool.Length)
        {
            return false;
        }

        storage = factory.factoryStorage.storagePool[entity.storageId];
        return storage is not null && storage.id == entity.storageId && storage.entityId == entityId;
    }

    private static List<EndpointPoint> GetFreePortPoints(
        PlanetFactory factory,
        FactoryEntitySnapshot snapshot,
        bool requireOutput)
    {
        var result = new List<EndpointPoint>();
        if (snapshot.ObjectId <= 0 || snapshot.ObjectId >= factory.entityCursor)
        {
            return result;
        }

        ref var entity = ref factory.entityPool[snapshot.ObjectId];
        var item = LDB.items.Select(entity.protoId);
        if (item?.prefabDesc?.isBelt == true)
        {
            var slot = requireOutput ? 0 : 1;
            factory.ReadObjectConn(snapshot.ObjectId, slot, out _, out var otherObjectId, out _);
            if (otherObjectId == 0)
            {
                var rotation = Quaternion.AngleAxis(entity.tilt, entity.rot * Vector3.forward) * entity.rot;
                if (!requireOutput)
                {
                    rotation *= Quaternion.Euler(0f, 180f, 0f);
                }

                result.Add(new EndpointPoint(snapshot.ObjectId, slot, new Pose(entity.pos, rotation)));
            }

            return result;
        }

        var ports = item?.prefabDesc?.portPoses ?? Array.Empty<Pose>();
        for (var index = 0; index < ports.Length; index++)
        {
            factory.ReadObjectConn(snapshot.ObjectId, index, out _, out var otherObjectId, out _);
            if (otherObjectId != 0)
            {
                continue;
            }

            var pose = ports[index].GetTransformedBy(new Pose(entity.pos, entity.rot));
            result.Add(new EndpointPoint(snapshot.ObjectId, index, pose));
        }

        return result;
    }

    private static List<EndpointPoint> GetInserterEndpointPoints(
        PlanetFactory factory,
        FactoryEntitySnapshot snapshot)
    {
        var result = new List<EndpointPoint>();
        if (snapshot.ObjectId <= 0 || snapshot.ObjectId >= factory.entityCursor)
        {
            return result;
        }

        ref var entity = ref factory.entityPool[snapshot.ObjectId];
        var item = LDB.items.Select(entity.protoId);
        if (item?.prefabDesc is null)
        {
            return result;
        }

        if (item.prefabDesc.isBelt)
        {
            var baseRotation = Quaternion.AngleAxis(entity.tilt, entity.rot * Vector3.forward) * entity.rot;
            var beltSlots = new[]
            {
                Quaternion.identity,
                Quaternion.Euler(0f, 90f, 0f),
                Quaternion.Euler(0f, 180f, 0f),
                Quaternion.Euler(0f, -90f, 0f),
            };
            foreach (var slotRotation in beltSlots)
            {
                result.Add(new EndpointPoint(
                    snapshot.ObjectId,
                    -1,
                    new Pose(entity.pos, baseRotation * slotRotation)));
            }

            return result;
        }

        var slots = item.prefabDesc.slotPoses ?? Array.Empty<Pose>();
        var occupiedSlots = new List<int>();
        for (var index = 0; index < slots.Length; index++)
        {
            factory.ReadObjectConn(snapshot.ObjectId, index, out _, out var otherObjectId, out _);
            if (otherObjectId != 0)
            {
                occupiedSlots.Add(index);
            }
        }

        foreach (var index in BuildConnectionSlots.SelectAvailable(slots.Length, occupiedSlots))
        {
            var transformed = slots[index].GetTransformedBy(new Pose(entity.pos, entity.rot));
            result.Add(new EndpointPoint(snapshot.ObjectId, index, transformed));
        }

        return result;
    }

    private static string BuildEndpointHash(FactoryEntitySnapshot snapshot) =>
        CanonicalStateHash.FactoryEndpoint(snapshot);

    private static string BuildPlanFingerprint(BuildPreparation preparation)
    {
        var fields = new List<object?>
        {
            preparation.Kind,
            preparation.ResourceNodeId,
            preparation.SourceObjectId,
            preparation.DestinationObjectId,
            preparation.Steps.Count,
        };
        foreach (var step in preparation.Steps)
        {
            step.AppendFingerprint(fields);
        }

        return CanonicalStateHash.Combine("build-steps", fields.ToArray());
    }

    private static bool BuildStepsEqual(
        IReadOnlyList<BuildStepPlan> expected,
        IReadOnlyList<BuildStepPlan> actual)
    {
        if (expected.Count != actual.Count)
        {
            return false;
        }

        for (var index = 0; index < expected.Count; index++)
        {
            if (!expected[index].EquivalentTo(actual[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool PreviewsExactlyMatch(IReadOnlyList<BuildStepPlan> plan, IReadOnlyList<BuildPreview> previews)
    {
        if (plan.Count != previews.Count)
        {
            return false;
        }

        for (var index = 0; index < previews.Count; index++)
        {
            if (previews[index].condition != EBuildCondition.Ok || previews[index].coverObjId != 0
                || !plan[index].EquivalentTo(BuildStepPlan.FromPreview(plan[index], previews[index])))
            {
                return false;
            }
        }

        return true;
    }

    private static List<BuildPreview> CreateLinkedPreviews(IReadOnlyList<BuildStepPlan> steps, ItemProto item)
    {
        var previews = steps.Select(step => CreatePreview(step, item)).ToList();
        for (var index = 0; index < steps.Count; index++)
        {
            if (steps[index].InputStepIndex >= 0)
            {
                previews[index].input = previews[steps[index].InputStepIndex];
            }

            if (steps[index].OutputStepIndex >= 0)
            {
                previews[index].output = previews[steps[index].OutputStepIndex];
            }
        }

        return previews;
    }

    private static BuildPreview CreatePreview(BuildStepPlan step, ItemProto item)
    {
        var preview = new BuildPreview
        {
            item = item,
            desc = item.prefabDesc,
            lpos = step.Position,
            lpos2 = step.Position2,
            lrot = step.Rotation,
            lrot2 = step.Rotation2,
            tilt = step.Tilt,
            inputObjId = step.InputObjectId,
            outputObjId = step.OutputObjectId,
            inputFromSlot = step.InputFromSlot,
            inputToSlot = step.InputToSlot,
            outputFromSlot = step.OutputFromSlot,
            outputToSlot = step.OutputToSlot,
            inputOffset = step.InputOffset,
            outputOffset = step.OutputOffset,
            condition = EBuildCondition.Ok,
            isConnNode = step.IsConnectionNode,
            needModel = false,
        };
        if (step.Parameters.Count > 0)
        {
            preview.parameters = step.Parameters.ToArray();
            preview.paramCount = preview.parameters.Length;
        }

        return preview;
    }

    private static bool BuildUiIsIdle(Player player)
    {
        var build = player.controller?.actionBuild;
        return build is not null
               && !build.active
               && build.templatePreviews.Count == 0
               && build.clickTool.buildPreviews.Count == 0
               && build.pathTool.buildPreviews.Count == 0
               && build.inserterTool.buildPreviews.Count == 0;
    }

    private static int FindBuiltEntityExcluding(
        PlanetFactory factory,
        int itemId,
        Vector3 position,
        IReadOnlyCollection<int> preexistingEntityIds,
        IReadOnlyCollection<int> alreadySelectedEntityIds)
    {
        var candidates = new List<BuildEntityCandidate>();
        var limit = Math.Min(factory.entityCursor, factory.entityPool.Length);
        for (var entityId = 1; entityId < limit; entityId++)
        {
            ref var entity = ref factory.entityPool[entityId];
            if (entity.id != entityId || entity.protoId != itemId)
            {
                continue;
            }

            var distance = (entity.pos - position).sqrMagnitude;
            if (distance < 0.09f)
            {
                candidates.Add(new BuildEntityCandidate(entityId, distance));
            }
        }

        return BuildEntityAttribution.SelectNearestNewCandidate(
            candidates,
            preexistingEntityIds,
            alreadySelectedEntityIds,
            0.09f);
    }

    private static void CaptureBuiltEntityIds(
        PlanetFactory factory,
        int itemId,
        Vector3 position,
        ISet<int> destination)
    {
        var limit = Math.Min(factory.entityCursor, factory.entityPool.Length);
        for (var entityId = 1; entityId < limit; entityId++)
        {
            ref var entity = ref factory.entityPool[entityId];
            if (entity.id == entityId
                && entity.protoId == itemId
                && (entity.pos - position).sqrMagnitude < 0.09f)
            {
                destination.Add(entityId);
            }
        }
    }

    private static Vector3 ProjectTangent(Vector3 direction, Vector3 surfacePosition)
    {
        var normal = surfacePosition.normalized;
        return direction - normal * Vector3.Dot(direction, normal);
    }

    private static Vector3Snapshot Snapshot(Vector3 value) => new Vector3Snapshot
    {
        X = value.x,
        Y = value.y,
        Z = value.z,
    };

    private readonly struct WorldBuildCollider
    {
        public WorldBuildCollider(
            Vector3 center,
            Vector3 axisX,
            Vector3 axisY,
            Vector3 axisZ,
            Vector3 extents)
        {
            Center = center;
            AxisX = axisX;
            AxisY = axisY;
            AxisZ = axisZ;
            Extents = extents;
        }

        public Vector3 Center { get; }

        public Vector3 AxisX { get; }

        public Vector3 AxisY { get; }

        public Vector3 AxisZ { get; }

        public Vector3 Extents { get; }
    }

    private sealed class StorageCopy : IDisposable
    {
        public StorageCopy(StorageComponent source)
        {
            Value = new StorageComponent(source.size);
            Array.Copy(source.grids, Value.grids, Math.Min(source.size, source.grids.Length));
            Value.type = source.type;
            Value.bans = source.bans;
            Value.isPlayerInventory = source.isPlayerInventory;
        }

        public StorageComponent Value { get; }

        public void Dispose() => Value.Free();
    }

    private sealed class BuildPreparation
    {
        public bool Success { get; private set; }
        public string ErrorCode { get; private set; } = BridgeErrorCodes.BuildLocationInvalid;
        public string Rejection { get; private set; } = string.Empty;
        public string Kind { get; private set; } = string.Empty;
        public List<BuildStepPlan> Steps { get; } = new List<BuildStepPlan>();
        public int ResourceNodeId { get; set; }
        public string ResourceStateHash { get; set; } = string.Empty;
        public int SourceObjectId { get; set; }
        public string SourceEndpointHash { get; set; } = string.Empty;
        public int DestinationObjectId { get; set; }
        public string DestinationEndpointHash { get; set; } = string.Empty;

        public static BuildPreparation Succeeded(string kind, IEnumerable<BuildStepPlan> steps)
        {
            var result = new BuildPreparation { Success = true, Kind = kind };
            result.Steps.AddRange(steps);
            return result;
        }

        public static BuildPreparation Failed(string code, string rejection) => new BuildPreparation
        {
            ErrorCode = code,
            Rejection = rejection,
        };
    }

    private sealed class BuildStepPlan
    {
        public int ItemId { get; private set; }
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }
        public Vector3 Position2 { get; private set; }
        public Quaternion Rotation2 { get; private set; }
        public float Yaw { get; private set; }
        public float Tilt { get; private set; }
        public int InputStepIndex { get; set; } = -1;
        public int OutputStepIndex { get; set; } = -1;
        public int InputObjectId { get; set; }
        public int OutputObjectId { get; set; }
        public int InputFromSlot { get; set; }
        public int InputToSlot { get; set; }
        public int OutputFromSlot { get; set; }
        public int OutputToSlot { get; set; }
        public int InputOffset { get; set; }
        public int OutputOffset { get; set; }
        public bool IsConnectionNode { get; private set; }
        public List<int> Parameters { get; } = new List<int>();

        public static BuildStepPlan Core(int itemId, Vector3 position, Quaternion rotation, float yaw) => new BuildStepPlan
        {
            ItemId = itemId,
            Position = position,
            Position2 = position,
            Rotation = rotation,
            Rotation2 = rotation,
            Yaw = yaw,
        };

        public static BuildStepPlan Belt(int itemId, Vector3 position) => new BuildStepPlan
        {
            ItemId = itemId,
            Position = position,
            Position2 = position,
            Rotation = Maths.SphericalRotation(position, 0f),
            Rotation2 = Maths.SphericalRotation(position, 0f),
            IsConnectionNode = true,
            InputToSlot = 1,
            OutputFromSlot = 0,
            OutputToSlot = 1,
        };

        public static BuildStepPlan Inserter(
            int itemId,
            Pose source,
            Pose destination,
            int sourceObjectId,
            int sourceSlot,
            int destinationObjectId,
            int destinationSlot) => new BuildStepPlan
            {
                ItemId = itemId,
                Position = source.position,
                Rotation = source.rotation,
                Position2 = destination.position,
                Rotation2 = destination.rotation * Quaternion.Euler(0f, 180f, 0f),
                InputObjectId = sourceObjectId,
                InputFromSlot = sourceSlot,
                InputToSlot = 1,
                OutputObjectId = destinationObjectId,
                OutputFromSlot = 0,
                OutputToSlot = destinationSlot,
            };

        public static BuildStepPlan FromPreview(BuildStepPlan template, BuildPreview preview)
        {
            var result = new BuildStepPlan
            {
                ItemId = template.ItemId,
                Position = preview.lpos,
                Rotation = preview.lrot,
                Position2 = preview.lpos2,
                Rotation2 = preview.lrot2,
                Yaw = template.Yaw,
                Tilt = preview.tilt,
                InputStepIndex = template.InputStepIndex,
                OutputStepIndex = template.OutputStepIndex,
                InputObjectId = preview.inputObjId,
                OutputObjectId = preview.outputObjId,
                InputFromSlot = preview.inputFromSlot,
                InputToSlot = preview.inputToSlot,
                OutputFromSlot = preview.outputFromSlot,
                OutputToSlot = preview.outputToSlot,
                InputOffset = preview.inputOffset,
                OutputOffset = preview.outputOffset,
                IsConnectionNode = preview.isConnNode,
            };
            if (preview.parameters is not null && preview.paramCount > 0)
            {
                result.Parameters.AddRange(preview.parameters.Take(Math.Min(preview.paramCount, preview.parameters.Length)));
            }

            return result;
        }

        public bool EquivalentTo(BuildStepPlan other)
        {
            return ItemId == other.ItemId
                   && Vector3.Distance(Position, other.Position) <= 0.01f
                   && Vector3.Distance(Position2, other.Position2) <= 0.01f
                   && Quaternion.Angle(Rotation, other.Rotation) <= 0.1f
                   && Quaternion.Angle(Rotation2, other.Rotation2) <= 0.1f
                   && Math.Abs(Tilt - other.Tilt) <= 0.01f
                   && InputStepIndex == other.InputStepIndex
                   && OutputStepIndex == other.OutputStepIndex
                   && InputObjectId == other.InputObjectId
                   && OutputObjectId == other.OutputObjectId
                   && InputFromSlot == other.InputFromSlot
                   && InputToSlot == other.InputToSlot
                   && OutputFromSlot == other.OutputFromSlot
                   && OutputToSlot == other.OutputToSlot
                   && InputOffset == other.InputOffset
                   && OutputOffset == other.OutputOffset
                   && IsConnectionNode == other.IsConnectionNode
                   && Parameters.SequenceEqual(other.Parameters);
        }

        public void AppendFingerprint(ICollection<object?> fields)
        {
            fields.Add(ItemId);
            fields.Add(Position.x);
            fields.Add(Position.y);
            fields.Add(Position.z);
            fields.Add(Rotation.x);
            fields.Add(Rotation.y);
            fields.Add(Rotation.z);
            fields.Add(Rotation.w);
            fields.Add(Position2.x);
            fields.Add(Position2.y);
            fields.Add(Position2.z);
            fields.Add(Rotation2.x);
            fields.Add(Rotation2.y);
            fields.Add(Rotation2.z);
            fields.Add(Rotation2.w);
            fields.Add(Tilt);
            fields.Add(InputStepIndex);
            fields.Add(OutputStepIndex);
            fields.Add(InputObjectId);
            fields.Add(OutputObjectId);
            fields.Add(InputFromSlot);
            fields.Add(InputToSlot);
            fields.Add(OutputFromSlot);
            fields.Add(OutputToSlot);
            foreach (var parameter in Parameters)
            {
                fields.Add(parameter);
            }
        }
    }

    private sealed class BuildExpectedEntity
    {
        public int ItemId { get; set; }
        public Vector3 Position { get; set; }
        public int InputObjectId { get; set; }
        public int OutputObjectId { get; set; }
        public int InputStepIndex { get; set; }
        public int OutputStepIndex { get; set; }
    }

    private readonly struct EndpointPoint
    {
        public EndpointPoint(int objectId, int slot, Pose pose)
        {
            ObjectId = objectId;
            Slot = slot;
            Pose = pose;
        }

        public int ObjectId { get; }
        public int Slot { get; }
        public Pose Pose { get; }
    }

    private sealed partial class NormalActionPlanPayload
    {
        public static NormalActionPlanPayload StructuredBuild(
            string sessionId,
            int planetId,
            string expectedStateHash,
            string playerStateHash,
            int buildingItemId,
            BuildPreparation preparation)
        {
            var result = new NormalActionPlanPayload
            {
                ActionKind = NormalActionKinds.Build,
                SessionId = sessionId,
                PlanetId = planetId,
                ExpectedStateHash = expectedStateHash,
                PlayerStateHash = playerStateHash,
                BuildingItemId = buildingItemId,
                BuildKind = preparation.Kind,
                BuildResourceNodeId = preparation.ResourceNodeId,
                BuildResourceStateHash = preparation.ResourceStateHash,
                SourceObjectId = preparation.SourceObjectId,
                SourceFactoryStateHash = preparation.SourceEndpointHash,
                DestinationObjectId = preparation.DestinationObjectId,
                DestinationFactoryStateHash = preparation.DestinationEndpointHash,
                Count = preparation.Steps.Count,
                EstimatedTicks = Math.Max(3600, preparation.Steps.Count * 900L),
            };
            result.BuildSteps.AddRange(preparation.Steps);
            return result;
        }

        public static NormalActionPlanPayload Transfer(
            string sessionId,
            int planetId,
            string expectedStateHash,
            string playerStateHash,
            string storageStateHash,
            int storageEntityId,
            string direction,
            int itemId,
            int count) => new NormalActionPlanPayload
            {
                ActionKind = NormalActionKinds.Transfer,
                SessionId = sessionId,
                PlanetId = planetId,
                ExpectedStateHash = expectedStateHash,
                PlayerStateHash = playerStateHash,
                TransferStorageStateHash = storageStateHash,
                TransferStorageEntityId = storageEntityId,
                TransferDirection = direction,
                TransferItemId = itemId,
                Count = count,
                EstimatedTicks = 1,
            };
    }
}
