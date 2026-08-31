using System.Security.Cryptography;
using System.Text;
using Spherewright.Bridge.Core.Progression;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Bridge.Core.Snapshots;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Factory;
using Spherewright.Contracts.Players;
using Spherewright.Contracts.Power;
using Spherewright.Contracts.Progression;
using Spherewright.Contracts.Resources;
using Spherewright.Contracts.Sessions;

namespace Spherewright.Plugin.Game;

internal sealed class GameStateReader
{
    private const int DefaultLimit = 50;
    private const int MaximumLimit = 100;
    private readonly GameSessionTracker _sessions;
    private readonly SnapshotPageStore<ResourceNodeSnapshot> _resourceSnapshots =
        new SnapshotPageStore<ResourceNodeSnapshot>(TimeSpan.FromSeconds(60), 16);
    private readonly SnapshotPageStore<FactoryEntitySnapshot> _factorySnapshots =
        new SnapshotPageStore<FactoryEntitySnapshot>(TimeSpan.FromSeconds(60), 16);
    private readonly SnapshotPageStore<AssemblerSnapshot> _assemblerSnapshots =
        new SnapshotPageStore<AssemblerSnapshot>(TimeSpan.FromSeconds(60), 16);

    public GameStateReader(GameSessionTracker sessions)
    {
        _sessions = sessions;
    }

    public GameCallResult<SessionState> GetSessionStateOnMainThread()
    {
        return GameCallResult<SessionState>.Succeeded(_sessions.CaptureOnMainThread());
    }

    public GameCallResult<PlayerStateSnapshot> GetPlayerStateOnMainThread(
        string? requestedSessionId,
        LocalPlanetRequest request)
    {
        var accessError = ValidateOwnedPlanetOnMainThread(requestedSessionId, request.PlanetId, out var factory);
        if (accessError is not null)
        {
            return GameCallResult<PlayerStateSnapshot>.Failed(accessError);
        }

        var player = GameMain.mainPlayer;
        if (player?.package is null || player.mecha is null)
        {
            return GameCallResult<PlayerStateSnapshot>.Failed(NotReady(
                "The player inventory or mecha is not ready in the owned ordinary world."));
        }

        var result = new PlayerStateSnapshot
        {
            SessionId = _sessions.SessionId!,
            PlanetId = factory!.planetId,
            CapturedAtGameTick = GameMain.gameTick,
            Position = CaptureVector(player.position),
            MovementState = player.movementState.ToString(),
            IsAlive = player.isAlive,
            IsOnPlanet = player.planetId == factory.planetId,
            IsFlying = player.movementState == EMovementState.Fly,
            IsSailing = player.movementState >= EMovementState.Sail,
            Speed = player.speed,
            CoreEnergy = player.mecha.coreEnergy,
            CoreEnergyCapacity = player.mecha.coreEnergyCap,
            ReactorEnergy = player.mecha.reactorEnergy,
            ReactorItemId = player.mecha.reactorItemId,
            ReactorItemName = player.mecha.reactorItemId > 0 ? GetItemName(player.mecha.reactorItemId) : null,
            ReactorItemInc = player.mecha.reactorItemInc,
            AutoReplenishFuel = player.mecha.autoReplenishFuel,
            FuelStorageSlotCount = player.mecha.reactorStorage?.size ?? 0,
            BuildArea = player.mecha.buildArea,
            InventorySlotCount = player.package.size,
        };

        var inventory = new Dictionary<int, PlayerInventoryItem>();
        var packageGrids = player.package.grids ?? Array.Empty<StorageComponent.GRID>();
        var gridCount = Math.Min(player.package.size, packageGrids.Length);
        for (var index = 0; index < gridCount; index++)
        {
            var grid = packageGrids[index];
            if (grid.itemId <= 0 || grid.count <= 0)
            {
                continue;
            }

            result.InventoryOccupiedSlotCount++;
            if (!inventory.TryGetValue(grid.itemId, out var entry))
            {
                entry = new PlayerInventoryItem
                {
                    ItemId = grid.itemId,
                    Name = GetItemName(grid.itemId),
                };
                inventory.Add(grid.itemId, entry);
            }

            entry.Count += grid.count;
            entry.Inc += grid.inc;
            entry.SlotCount++;
        }

        result.Inventory = inventory.Values.OrderBy(item => item.ItemId).ToList();
        var fuelInventory = new Dictionary<int, PlayerInventoryItem>();
        var fuelStorage = player.mecha.reactorStorage;
        var fuelGrids = fuelStorage?.grids ?? Array.Empty<StorageComponent.GRID>();
        var fuelGridCount = Math.Min(fuelStorage?.size ?? 0, fuelGrids.Length);
        for (var index = 0; index < fuelGridCount; index++)
        {
            var grid = fuelGrids[index];
            if (grid.itemId <= 0 || grid.count <= 0)
            {
                continue;
            }

            result.FuelStorageOccupiedSlotCount++;
            if (!fuelInventory.TryGetValue(grid.itemId, out var entry))
            {
                entry = new PlayerInventoryItem
                {
                    ItemId = grid.itemId,
                    Name = GetItemName(grid.itemId),
                };
                fuelInventory.Add(grid.itemId, entry);
            }

            entry.Count += grid.count;
            entry.Inc += grid.inc;
            entry.SlotCount++;
        }

        result.FuelStorage = fuelInventory.Values.OrderBy(item => item.ItemId).ToList();
        if (player.inhandItemId > 0 && player.inhandItemCount > 0)
        {
            result.InHandItem = new PlayerInventoryItem
            {
                ItemId = player.inhandItemId,
                Name = GetItemName(player.inhandItemId),
                Count = player.inhandItemCount,
                Inc = player.inhandItemInc,
                SlotCount = 1,
            };
        }

        var forgeTasks = player.mecha.forge?.tasks;
        if (forgeTasks is not null)
        {
            for (var index = 0; index < forgeTasks.Count; index++)
            {
                var task = forgeTasks[index];
                if (task is null)
                {
                    continue;
                }

                var taskSnapshot = new HandcraftTaskSnapshot
                {
                    QueueIndex = index,
                    RecipeId = task.recipeId,
                    RecipeName = LDB.recipes.Select(task.recipeId)?.name ?? string.Empty,
                    RemainingCraftCount = task.count,
                    Progress = task.tick,
                    ProgressRequired = task.tickSpend,
                    ParentTaskIndex = task.parentTaskIndex,
                    IngredientsReserved = task.itemEnough,
                };
                AddPlayerItemAmounts(taskSnapshot.Inputs, task.itemIds, task.itemCounts, task.served);
                AddPlayerItemAmounts(taskSnapshot.Outputs, task.productIds, task.productCounts, task.produced);
                result.HandcraftQueue.Add(taskSnapshot);
            }
        }

        var construction = player.mecha.constructionModule;
        if (construction is not null)
        {
            result.ConstructionDrones = new ConstructionDroneSnapshot
            {
                Enabled = construction.droneEnabled,
                ConstructionEnabled = construction.droneConstructEnabled,
                Total = construction.droneCount,
                Alive = construction.droneAliveCount,
                Idle = construction.droneIdleCount,
                Working = Math.Max(0, construction.droneAliveCount - construction.droneIdleCount),
                PendingBuildTargets = construction.buildTargetTotalCount,
                PendingRepairTargets = construction.repairTargetTotalCount,
            };
        }

        // The public action fingerprint must remain stable while DSP performs
        // ordinary per-tick energy recharge. Exact energy is still returned in
        // the snapshot and captured separately by action readback, but it is
        // not a safe optimistic-concurrency field: including it makes every
        // prepare stale on the next Unity frame after the first energy use.
        result.StateHash = CanonicalStateHash.PlayerAction(result);
        result.StateHashVersion = CanonicalStateHash.Version;
        return GameCallResult<PlayerStateSnapshot>.Succeeded(result);
    }

    public GameCallResult<ProgressionStateSnapshot> GetProgressionStateOnMainThread(
        string? requestedSessionId,
        LocalPlanetRequest request)
    {
        var accessError = ValidateOwnedPlanetOnMainThread(requestedSessionId, request.PlanetId, out var factory);
        if (accessError is not null)
        {
            return GameCallResult<ProgressionStateSnapshot>.Failed(accessError);
        }

        var history = GameMain.history;
        if (history?.techStates is null || history.techQueue is null)
        {
            return GameCallResult<ProgressionStateSnapshot>.Failed(NotReady(
                "The technology state is not ready in the owned ordinary world."));
        }

        var queue = history.techQueue.Where(techId => techId > 0).ToList();
        var result = new ProgressionStateSnapshot
        {
            SessionId = _sessions.SessionId!,
            PlanetId = factory!.planetId,
            CapturedAtGameTick = GameMain.gameTick,
            CurrentTechId = history.currentTech,
            CurrentTechName = LDB.techs.Select(history.currentTech)?.name,
            TechQueue = queue,
        };

        foreach (var tech in LDB.techs.dataArray.OrderBy(proto => proto.ID))
        {
            if (tech is null || !history.techStates.TryGetValue(tech.ID, out var state))
            {
                continue;
            }

            var snapshot = new TechStateSnapshot
            {
                TechId = tech.ID,
                Name = tech.name ?? string.Empty,
                Unlocked = state.unlocked,
                CurrentLevel = state.curLevel,
                MaximumLevel = state.maxLevel,
                HashUploaded = state.hashUploaded,
                HashRequired = state.hashNeeded,
                UnlockTick = state.unlockTick,
                IsLabTech = tech.IsLabTech,
                IsQueued = queue.Contains(tech.ID),
                PrerequisiteTechIds = (tech.PreTechs ?? Array.Empty<int>())
                    .Concat(tech.PreTechsImplicit ?? Array.Empty<int>())
                    .Distinct()
                    .OrderBy(id => id)
                    .ToList(),
                UnlockRecipeIds = (tech.UnlockRecipes ?? Array.Empty<int>()).OrderBy(id => id).ToList(),
            };

            var techItems = tech.Items ?? Array.Empty<int>();
            var techItemPoints = tech.ItemPoints ?? Array.Empty<int>();
            var itemCount = Math.Min(techItems.Length, techItemPoints.Length);
            for (var index = 0; index < itemCount; index++)
            {
                var itemId = techItems[index];
                var requiredItemCount = state.hashNeeded * techItemPoints[index] / TechProto.kPointPerItem;
                var isMatrix = TechProto.matrixIds.Contains(itemId);
                snapshot.ItemRequirements.Add(new TechItemRequirement
                {
                    ItemId = itemId,
                    Name = GetItemName(itemId),
                    PointsPerHash = techItemPoints[index],
                    RequiredItemCount = requiredItemCount,
                    IsMatrix = isMatrix,
                });
                if (isMatrix)
                {
                    snapshot.MatrixRequirements.Add(new TechMatrixRequirement
                    {
                        ItemId = itemId,
                        Name = GetItemName(itemId),
                        PointsPerHash = techItemPoints[index],
                        RequiredItemCount = requiredItemCount,
                    });
                }
            }

            result.Technologies.Add(snapshot);
        }

        result.StateHash = CanonicalStateHash.Progression(result);
        result.StateHashVersion = CanonicalStateHash.Version;
        return GameCallResult<ProgressionStateSnapshot>.Succeeded(result);
    }

    public GameCallResult<RecipeCatalogSnapshot> GetRecipeCatalogOnMainThread(
        string? requestedSessionId,
        LocalPlanetRequest request)
    {
        var accessError = ValidateOwnedPlanetOnMainThread(requestedSessionId, request.PlanetId, out var factory);
        if (accessError is not null)
        {
            return GameCallResult<RecipeCatalogSnapshot>.Failed(accessError);
        }

        var history = GameMain.history;
        if (history is null)
        {
            return GameCallResult<RecipeCatalogSnapshot>.Failed(NotReady(
                "The runtime item and recipe catalog is not ready in the owned ordinary world."));
        }

        var result = new RecipeCatalogSnapshot
        {
            SessionId = _sessions.SessionId!,
            PlanetId = factory!.planetId,
            CapturedAtGameTick = GameMain.gameTick,
        };

        foreach (var item in LDB.items.dataArray.OrderBy(proto => proto.ID))
        {
            if (item is null)
            {
                continue;
            }

            result.Items.Add(new ItemCatalogEntry
            {
                ItemId = item.ID,
                Name = item.name ?? string.Empty,
                StackSize = item.StackSize,
                IsRaw = item.isRaw,
                CanBuild = item.CanBuild,
                Unlocked = history.ItemUnlocked(item.ID),
                HandcraftRecipeId = item.handcraft?.ID,
            });
        }

        foreach (var recipe in LDB.recipes.dataArray.OrderBy(proto => proto.ID))
        {
            if (recipe is null)
            {
                continue;
            }

            var entry = new RecipeCatalogEntry
            {
                RecipeId = recipe.ID,
                Name = recipe.name ?? string.Empty,
                RecipeType = recipe.Type.ToString(),
                Handcraft = recipe.Handcraft,
                Unlocked = history.RecipeUnlocked(recipe.ID),
                TimeSpend = recipe.TimeSpend,
                PrerequisiteTechId = recipe.preTech?.ID,
                PrerequisiteTechName = recipe.preTech?.name,
            };
            AddCatalogAmounts(entry.Inputs, recipe.Items, recipe.ItemCounts);
            AddCatalogAmounts(entry.Outputs, recipe.Results, recipe.ResultCounts);
            result.Recipes.Add(entry);
        }

        var redMatrixItemId = TechProto.matrixIds.Length > 1 ? TechProto.matrixIds[1] : 0;
        result.FirstRedMatrixDependencies = RuntimeDependencyGraphBuilder.Build(
            redMatrixItemId,
            GetItemName(redMatrixItemId),
            result.Recipes);
        return GameCallResult<RecipeCatalogSnapshot>.Succeeded(result);
    }

    public GameCallResult<ListResourceNodesResult> ListResourceNodesOnMainThread(
        string? requestedSessionId,
        ListResourceNodesRequest request)
    {
        var accessError = ValidateOwnedPlanetOnMainThread(requestedSessionId, request.PlanetId, out var factory);
        if (accessError is not null)
        {
            return GameCallResult<ListResourceNodesResult>.Failed(accessError);
        }

        var limitError = ValidateListLimit(request.Limit, "Resource-node");
        if (limitError is not null)
        {
            return GameCallResult<ListResourceNodesResult>.Failed(limitError);
        }

        var normalizedKind = NormalizeOptional(request.Kind);
        if (normalizedKind is not null
            && normalizedKind != ResourceNodeKinds.Vein
            && normalizedKind != ResourceNodeKinds.Vegetation)
        {
            return GameCallResult<ListResourceNodesResult>.Failed(InvalidRequest(
                "Resource kind must be vein, vegetation, or empty.",
                "Use a resource kind returned by this tool."));
        }

        var normalizedType = NormalizeOptional(request.ResourceType);
        var filterHash = ComputeFilterHash(
            $"kind={normalizedKind ?? "*"}|type={normalizedType ?? "*"}|product={request.ProductItemId?.ToString() ?? "*"}|limit={request.Limit}");
        SnapshotPage<ResourceNodeSnapshot>? page;
        if (!string.IsNullOrWhiteSpace(request.Cursor))
        {
            var status = _resourceSnapshots.TryGetPage(
                request.Cursor,
                _sessions.SessionId!,
                factory!.planetId,
                filterHash,
                request.Limit,
                out page);
            if (status != SnapshotCursorStatus.Success || page is null)
            {
                return GameCallResult<ListResourceNodesResult>.Failed(StaleCursor(
                    "The resource cursor is unknown, expired, or bound to a different session, planet, filter, or page size."));
            }
        }
        else
        {
            var nodes = CaptureResourceNodes(factory!, normalizedKind, normalizedType, request.ProductItemId);
            if (!_resourceSnapshots.TryCreate(
                    _sessions.SessionId!,
                    factory!.planetId,
                    filterHash,
                    nodes,
                    request.Limit,
                    out page)
                || page is null)
            {
                return GameCallResult<ListResourceNodesResult>.Failed(SnapshotCapacityExceeded("resource-node"));
            }
        }

        return GameCallResult<ListResourceNodesResult>.Succeeded(new ListResourceNodesResult
        {
            SessionId = _sessions.SessionId!,
            PlanetId = factory!.planetId,
            CapturedAtGameTick = page.Items.Count > 0 ? page.Items[0].CapturedAtGameTick : GameMain.gameTick,
            SnapshotId = page.SnapshotId,
            SnapshotExpiresAtUtc = page.ExpiresAtUtc,
            Nodes = page.Items.ToList(),
            NextCursor = page.NextCursor,
        });
    }

    public GameCallResult<ResourceNodeSnapshot> InspectResourceNodeOnMainThread(
        string? requestedSessionId,
        InspectResourceNodeRequest request)
    {
        var accessError = ValidateOwnedPlanetOnMainThread(requestedSessionId, request.PlanetId, out var factory);
        if (accessError is not null)
        {
            return GameCallResult<ResourceNodeSnapshot>.Failed(accessError);
        }

        if (request.NodeId <= 0)
        {
            return InvalidResource("The requested resource node ID must be positive.");
        }

        ResourceNodeSnapshot? snapshot;
        if (string.Equals(request.Kind, ResourceNodeKinds.Vein, StringComparison.OrdinalIgnoreCase))
        {
            snapshot = request.NodeId < factory!.veinCursor
                ? TryCaptureVein(factory, request.NodeId)
                : null;
        }
        else if (string.Equals(request.Kind, ResourceNodeKinds.Vegetation, StringComparison.OrdinalIgnoreCase))
        {
            snapshot = request.NodeId < factory!.vegeCursor
                ? TryCaptureVegetation(factory, request.NodeId)
                : null;
        }
        else
        {
            return GameCallResult<ResourceNodeSnapshot>.Failed(InvalidRequest(
                "Resource kind must be vein or vegetation.",
                "Use the kind and nodeId returned by spherewright_list_resource_nodes."));
        }

        return snapshot is null
            ? InvalidResource("The requested resource node no longer exists in the local factory.")
            : GameCallResult<ResourceNodeSnapshot>.Succeeded(snapshot);
    }

    public GameCallResult<ListFactoryEntitiesResult> ListFactoryEntitiesOnMainThread(
        string? requestedSessionId,
        ListFactoryEntitiesRequest request)
    {
        var accessError = ValidateOwnedPlanetOnMainThread(requestedSessionId, request.PlanetId, out var factory);
        if (accessError is not null)
        {
            return GameCallResult<ListFactoryEntitiesResult>.Failed(accessError);
        }

        var limitError = ValidateListLimit(request.Limit, "Factory-entity");
        if (limitError is not null)
        {
            return GameCallResult<ListFactoryEntitiesResult>.Failed(limitError);
        }

        var normalizedObjectKind = NormalizeOptional(request.ObjectKind);
        if (normalizedObjectKind is not null
            && normalizedObjectKind != FactoryObjectKinds.Entity
            && normalizedObjectKind != FactoryObjectKinds.Prebuild)
        {
            return GameCallResult<ListFactoryEntitiesResult>.Failed(InvalidRequest(
                "Factory object kind must be entity, prebuild, or empty.",
                "Use an object kind returned by this tool."));
        }

        var normalizedComponentKind = NormalizeOptional(request.ComponentKind);
        var filterHash = ComputeFilterHash(
            $"object={normalizedObjectKind ?? "*"}|component={normalizedComponentKind ?? "*"}|item={request.ItemId?.ToString() ?? "*"}|limit={request.Limit}");
        SnapshotPage<FactoryEntitySnapshot>? page;
        if (!string.IsNullOrWhiteSpace(request.Cursor))
        {
            var status = _factorySnapshots.TryGetPage(
                request.Cursor,
                _sessions.SessionId!,
                factory!.planetId,
                filterHash,
                request.Limit,
                out page);
            if (status != SnapshotCursorStatus.Success || page is null)
            {
                return GameCallResult<ListFactoryEntitiesResult>.Failed(StaleCursor(
                    "The factory cursor is unknown, expired, or bound to a different session, planet, filter, or page size."));
            }
        }
        else
        {
            var entities = CaptureFactoryEntities(
                factory!,
                normalizedObjectKind,
                normalizedComponentKind,
                request.ItemId);
            if (!_factorySnapshots.TryCreate(
                    _sessions.SessionId!,
                    factory!.planetId,
                    filterHash,
                    entities,
                    request.Limit,
                    out page)
                || page is null)
            {
                return GameCallResult<ListFactoryEntitiesResult>.Failed(SnapshotCapacityExceeded("factory-entity"));
            }
        }

        return GameCallResult<ListFactoryEntitiesResult>.Succeeded(new ListFactoryEntitiesResult
        {
            SessionId = _sessions.SessionId!,
            PlanetId = factory!.planetId,
            CapturedAtGameTick = page.Items.Count > 0 ? page.Items[0].CapturedAtGameTick : GameMain.gameTick,
            SnapshotId = page.SnapshotId,
            SnapshotExpiresAtUtc = page.ExpiresAtUtc,
            Entities = page.Items.ToList(),
            NextCursor = page.NextCursor,
        });
    }

    public GameCallResult<FactoryEntitySnapshot> InspectFactoryEntityOnMainThread(
        string? requestedSessionId,
        InspectFactoryEntityRequest request)
    {
        var accessError = ValidateOwnedPlanetOnMainThread(requestedSessionId, request.PlanetId, out var factory);
        if (accessError is not null)
        {
            return GameCallResult<FactoryEntitySnapshot>.Failed(accessError);
        }

        FactoryEntitySnapshot? snapshot = null;
        if (request.ObjectId > 0 && request.ObjectId < factory!.entityCursor)
        {
            snapshot = TryCaptureFactoryEntity(factory, request.ObjectId);
        }
        else if (request.ObjectId < 0 && -request.ObjectId < factory!.prebuildCursor)
        {
            snapshot = TryCapturePrebuild(factory, -request.ObjectId);
        }

        return snapshot is null
            ? InvalidFactoryEntity("The requested factory object no longer exists in the local factory.")
            : GameCallResult<FactoryEntitySnapshot>.Succeeded(snapshot);
    }

    public GameCallResult<PowerSummarySnapshot> GetPowerSummaryOnMainThread(
        string? requestedSessionId,
        LocalPlanetRequest request)
    {
        var accessError = ValidateOwnedPlanetOnMainThread(requestedSessionId, request.PlanetId, out var factory);
        if (accessError is not null)
        {
            return GameCallResult<PowerSummarySnapshot>.Failed(accessError);
        }

        var powerSystem = factory!.powerSystem;
        if (powerSystem?.netPool is null)
        {
            return GameCallResult<PowerSummarySnapshot>.Failed(NotReady(
                "The local planet power system is not ready."));
        }

        var result = new PowerSummarySnapshot
        {
            SessionId = _sessions.SessionId!,
            PlanetId = factory.planetId,
            CapturedAtGameTick = GameMain.gameTick,
        };
        var networkLimit = Math.Min(powerSystem.netCursor, powerSystem.netPool.Length);
        for (var networkId = 1; networkId < networkLimit; networkId++)
        {
            var network = powerSystem.netPool[networkId];
            if (network is null || network.id != networkId)
            {
                continue;
            }

            var snapshot = new PowerNetworkSnapshot
            {
                NetworkId = networkId,
                NodeCount = network.nodes?.Count ?? 0,
                ConsumerCount = network.consumers?.Count ?? 0,
                GeneratorCount = network.generators?.Count ?? 0,
                AccumulatorCount = network.accumulators?.Count ?? 0,
                ExchangerCount = network.exchangers?.Count ?? 0,
                EnergyRequired = network.energyRequired,
                EnergyServed = network.energyServed,
                EnergyCapacity = network.energyCapacity,
                EnergyGenerated = network.energyExport,
                EnergyStored = network.energyStored,
                ConsumerRatio = network.consumerRatio,
                GeneratorRatio = network.generaterRatio,
            };
            result.Networks.Add(snapshot);
            result.TotalEnergyRequired += snapshot.EnergyRequired;
            result.TotalEnergyServed += snapshot.EnergyServed;
            result.TotalEnergyCapacity += snapshot.EnergyCapacity;
            result.TotalEnergyGenerated += snapshot.EnergyGenerated;
        }

        return GameCallResult<PowerSummarySnapshot>.Succeeded(result);
    }

    public GameCallResult<ListAssemblersResult> ListAssemblersOnMainThread(
        string? requestedSessionId,
        ListAssemblersRequest request)
    {
        var accessError = ValidateOwnedSessionOnMainThread(requestedSessionId, out var factory);
        if (accessError is not null)
        {
            return GameCallResult<ListAssemblersResult>.Failed(accessError);
        }

        var limit = request.Limit == 0 ? DefaultLimit : request.Limit;
        if (limit < 1 || limit > MaximumLimit)
        {
            return GameCallResult<ListAssemblersResult>.Failed(BridgeError.Create(
                BridgeErrorCodes.InvalidRequest,
                $"Assembler list limit must be between 1 and {MaximumLimit}.",
                false,
                "Use a bounded limit and retry."));
        }

        var filterHash = ComputeFilterHash($"assemblers|limit={limit}");
        SnapshotPage<AssemblerSnapshot>? page;
        if (!string.IsNullOrWhiteSpace(request.Cursor))
        {
            var status = _assemblerSnapshots.TryGetPage(
                request.Cursor,
                _sessions.SessionId!,
                factory!.planetId,
                filterHash,
                limit,
                out page);
            if (status != SnapshotCursorStatus.Success || page is null)
            {
                return GameCallResult<ListAssemblersResult>.Failed(StaleCursor(
                    "The assembler cursor is unknown, expired, or bound to a different session, planet, or page size."));
            }
        }
        else
        {
            var assemblers = new List<AssemblerSnapshot>();
            var pool = factory!.factorySystem.assemblerPool;
            var cursor = Math.Min(factory.factorySystem.assemblerCursor, pool.Length);
            for (var componentId = 1; componentId < cursor; componentId++)
            {
                ref var assembler = ref pool[componentId];
                if (assembler.id != componentId)
                {
                    continue;
                }

                var snapshot = TryCaptureAssembler(factory, componentId, ref assembler);
                if (snapshot is not null)
                {
                    assemblers.Add(snapshot);
                }
            }

            if (!_assemblerSnapshots.TryCreate(
                    _sessions.SessionId!,
                    factory.planetId,
                    filterHash,
                    assemblers,
                    limit,
                    out page)
                || page is null)
            {
                return GameCallResult<ListAssemblersResult>.Failed(SnapshotCapacityExceeded("assembler"));
            }
        }

        return GameCallResult<ListAssemblersResult>.Succeeded(new ListAssemblersResult
        {
            Revision = _sessions.Revision,
            Assemblers = page.Items.ToList(),
            NextCursor = page.NextCursor,
        });
    }

    public GameCallResult<AssemblerSnapshot> InspectAssemblerOnMainThread(
        string? requestedSessionId,
        InspectAssemblerRequest request)
    {
        var accessError = ValidateOwnedSessionOnMainThread(requestedSessionId, out var factory);
        if (accessError is not null)
        {
            return GameCallResult<AssemblerSnapshot>.Failed(accessError);
        }

        if (request.EntityId <= 0 || request.EntityId >= factory!.entityCursor)
        {
            return InvalidAssembler("The requested entity does not exist in the current factory.");
        }

        ref var entity = ref factory.entityPool[request.EntityId];
        if (entity.id != request.EntityId || entity.assemblerId <= 0)
        {
            return InvalidAssembler("The requested entity is missing or is not an assembler.");
        }

        var componentId = entity.assemblerId;
        if (componentId >= factory.factorySystem.assemblerCursor)
        {
            return InvalidAssembler("The assembler component is no longer valid.");
        }

        ref var assembler = ref factory.factorySystem.assemblerPool[componentId];
        var snapshot = assembler.id == componentId && assembler.entityId == request.EntityId
            ? TryCaptureAssembler(factory, componentId, ref assembler)
            : null;
        return snapshot is null
            ? InvalidAssembler("The assembler component is no longer valid.")
            : GameCallResult<AssemblerSnapshot>.Succeeded(snapshot);
    }

    public GameCallResult<BuildCatalog> GetBuildCatalogOnMainThread(string? requestedSessionId)
    {
        var accessError = ValidateOwnedSessionOnMainThread(requestedSessionId, out var factory);
        if (accessError is not null)
        {
            return GameCallResult<BuildCatalog>.Failed(accessError);
        }

        var player = GameMain.mainPlayer;
        var history = GameMain.history;
        if (player is null || history is null || player.controller?.actionBuild is null)
        {
            return GameCallResult<BuildCatalog>.Failed(BridgeError.Create(
                BridgeErrorCodes.BridgeNotReady,
                "The player build system is not ready in the owned ordinary world.",
                true,
                "Wait until the player and local factory finish loading, then retry."));
        }

        var result = new BuildCatalog
        {
            PlanetId = factory!.planetId,
            Revision = _sessions.Revision,
            PlayerPosition = new Vector3Snapshot
            {
                X = player.position.x,
                Y = player.position.y,
                Z = player.position.z,
            },
            PlayerBuildArea = player.mecha.buildArea,
            SandboxToolsEnabled = GameMain.sandboxToolsEnabled,
        };

        foreach (var item in LDB.items.dataArray)
        {
            if (item is null || !item.CanBuild || item.prefabDesc is null)
            {
                continue;
            }

            var role = GetBasicLineRole(item.prefabDesc);
            if (role is null)
            {
                continue;
            }

            result.Buildings.Add(new BuildCatalogItem
            {
                ItemId = item.ID,
                Name = item.name ?? string.Empty,
                Role = role,
                ModelIndex = item.ModelIndex,
                Grade = item.Grade,
                BuildMode = item.BuildMode,
                Unlocked = history.ItemUnlocked(item.ID),
                Available = history.ItemUnlocked(item.ID),
                RecipeType = item.prefabDesc.assemblerRecipeType.ToString(),
                SlotCount = item.prefabDesc.slotPoses?.Length ?? 0,
                RoughRadius = item.prefabDesc.roughRadius,
                PowerConnectDistance = item.prefabDesc.powerConnectDistance,
                PowerCoverRadius = item.prefabDesc.powerCoverRadius,
            });
        }

        foreach (var recipe in LDB.recipes.dataArray)
        {
            if (recipe is null
                || recipe.Items is null
                || recipe.Results is null
                || recipe.ItemCounts is null
                || recipe.ResultCounts is null)
            {
                continue;
            }

            var recipeSnapshot = new BuildCatalogRecipe
            {
                RecipeId = recipe.ID,
                Name = recipe.name ?? string.Empty,
                RecipeType = recipe.Type.ToString(),
                Unlocked = history.RecipeUnlocked(recipe.ID),
                TimeSpend = recipe.TimeSpend,
            };
            for (var index = 0; index < Math.Min(recipe.Items.Length, recipe.ItemCounts.Length); index++)
            {
                recipeSnapshot.Inputs.Add(CreateIngredient(recipe.Items[index], recipe.ItemCounts[index]));
            }

            for (var index = 0; index < Math.Min(recipe.Results.Length, recipe.ResultCounts.Length); index++)
            {
                recipeSnapshot.Outputs.Add(CreateIngredient(recipe.Results[index], recipe.ResultCounts[index]));
            }

            result.Recipes.Add(recipeSnapshot);
        }

        result.Buildings = result.Buildings
            .OrderBy(item => item.Role, StringComparer.Ordinal)
            .ThenByDescending(item => item.Unlocked)
            .ThenBy(item => item.Grade)
            .ThenBy(item => item.ItemId)
            .ToList();
        result.Recipes = result.Recipes
            .OrderByDescending(recipe => recipe.Unlocked)
            .ThenByDescending(recipe => recipe.Inputs.Any(input => input.RawMaterial))
            .ThenBy(recipe => recipe.RecipeId)
            .ToList();
        result.RecommendedBasicLine = CreateRecommendation(result);

        return GameCallResult<BuildCatalog>.Succeeded(result);
    }

    private List<ResourceNodeSnapshot> CaptureResourceNodes(
        PlanetFactory factory,
        string? kind,
        string? resourceType,
        int? productItemId)
    {
        var result = new List<ResourceNodeSnapshot>();
        if (kind is null || kind == ResourceNodeKinds.Vein)
        {
            var limit = Math.Min(factory.veinCursor, factory.veinPool?.Length ?? 0);
            for (var nodeId = 1; nodeId < limit; nodeId++)
            {
                var snapshot = TryCaptureVein(factory, nodeId);
                if (snapshot is not null && ResourceMatches(snapshot, resourceType, productItemId))
                {
                    result.Add(snapshot);
                }
            }
        }

        if (kind is null || kind == ResourceNodeKinds.Vegetation)
        {
            var limit = Math.Min(factory.vegeCursor, factory.vegePool?.Length ?? 0);
            for (var nodeId = 1; nodeId < limit; nodeId++)
            {
                var snapshot = TryCaptureVegetation(factory, nodeId);
                if (snapshot is not null && ResourceMatches(snapshot, resourceType, productItemId))
                {
                    result.Add(snapshot);
                }
            }
        }

        return result
            .OrderBy(node => node.Kind, StringComparer.Ordinal)
            .ThenBy(node => node.NodeId)
            .ToList();
    }

    private ResourceNodeSnapshot? TryCaptureVein(PlanetFactory factory, int nodeId)
    {
        if (nodeId <= 0 || nodeId >= factory.veinCursor || nodeId >= factory.veinPool.Length)
        {
            return null;
        }

        ref var vein = ref factory.veinPool[nodeId];
        if (vein.id != nodeId || vein.type == EVeinType.None)
        {
            return null;
        }

        var player = GameMain.mainPlayer;
        var distance = player is null ? -1f : (vein.pos - player.position).magnitude;
        var veinProto = LDB.veins.Select((int)vein.type);
        var snapshot = new ResourceNodeSnapshot
        {
            SessionId = _sessions.SessionId!,
            PlanetId = factory.planetId,
            Kind = ResourceNodeKinds.Vein,
            NodeId = nodeId,
            ResourceType = vein.type.ToString(),
            ProtoId = (int)vein.type,
            Name = veinProto?.name ?? vein.type.ToString(),
            RemainingAmount = vein.amount,
            GroupIndex = vein.groupIndex,
            MinerCount = vein.minerCount,
            Position = CaptureVector(vein.pos),
            DistanceFromPlayer = distance,
            SameLocalPlanet = player?.planetId == factory.planetId,
            WithinPlayerBuildArea = player?.mecha is not null && distance <= player.mecha.buildArea,
            CapturedAtGameTick = GameMain.gameTick,
        };
        if (vein.productId > 0)
        {
            snapshot.Yields.Add(new ResourceYieldSnapshot
            {
                ItemId = vein.productId,
                Name = GetItemName(vein.productId),
                Count = 1,
                Chance = 1f,
            });
        }

        snapshot.StateHash = CanonicalStateHash.Resource(snapshot);
        snapshot.StateHashVersion = CanonicalStateHash.Version;
        return snapshot;
    }

    private ResourceNodeSnapshot? TryCaptureVegetation(PlanetFactory factory, int nodeId)
    {
        if (nodeId <= 0 || nodeId >= factory.vegeCursor || nodeId >= factory.vegePool.Length)
        {
            return null;
        }

        ref var vegetation = ref factory.vegePool[nodeId];
        if (vegetation.id != nodeId || vegetation.protoId <= 0)
        {
            return null;
        }

        var proto = LDB.veges.Select(vegetation.protoId);
        if (proto is null)
        {
            return null;
        }

        var player = GameMain.mainPlayer;
        var distance = player is null ? -1f : (vegetation.pos - player.position).magnitude;
        var snapshot = new ResourceNodeSnapshot
        {
            SessionId = _sessions.SessionId!,
            PlanetId = factory.planetId,
            Kind = ResourceNodeKinds.Vegetation,
            NodeId = nodeId,
            ResourceType = proto.Type.ToString(),
            ProtoId = vegetation.protoId,
            Name = proto.name ?? string.Empty,
            RemainingAmount = 1,
            Position = CaptureVector(vegetation.pos),
            DistanceFromPlayer = distance,
            SameLocalPlanet = player?.planetId == factory.planetId,
            WithinPlayerBuildArea = player?.mecha is not null && distance <= player.mecha.buildArea,
            CapturedAtGameTick = GameMain.gameTick,
        };
        var miningItems = proto.MiningItem ?? Array.Empty<int>();
        var miningCounts = proto.MiningCount ?? Array.Empty<int>();
        var miningChances = proto.MiningChance ?? Array.Empty<float>();
        var yieldCount = Math.Min(
            miningItems.Length,
            Math.Min(miningCounts.Length, miningChances.Length));
        for (var index = 0; index < yieldCount; index++)
        {
            snapshot.Yields.Add(new ResourceYieldSnapshot
            {
                ItemId = miningItems[index],
                Name = GetItemName(miningItems[index]),
                Count = miningCounts[index],
                Chance = miningChances[index],
            });
        }

        snapshot.StateHash = CanonicalStateHash.Resource(snapshot);
        snapshot.StateHashVersion = CanonicalStateHash.Version;
        return snapshot;
    }

    private List<FactoryEntitySnapshot> CaptureFactoryEntities(
        PlanetFactory factory,
        string? objectKind,
        string? componentKind,
        int? itemId)
    {
        var result = new List<FactoryEntitySnapshot>();
        if (objectKind is null || objectKind == FactoryObjectKinds.Entity)
        {
            var entityLimit = Math.Min(factory.entityCursor, factory.entityPool?.Length ?? 0);
            for (var entityId = 1; entityId < entityLimit; entityId++)
            {
                var snapshot = TryCaptureFactoryEntity(factory, entityId);
                if (snapshot is not null && FactoryEntityMatches(snapshot, componentKind, itemId))
                {
                    result.Add(snapshot);
                }
            }
        }

        if (objectKind is null || objectKind == FactoryObjectKinds.Prebuild)
        {
            var prebuildLimit = Math.Min(factory.prebuildCursor, factory.prebuildPool?.Length ?? 0);
            for (var prebuildId = 1; prebuildId < prebuildLimit; prebuildId++)
            {
                var snapshot = TryCapturePrebuild(factory, prebuildId);
                if (snapshot is not null && FactoryEntityMatches(snapshot, componentKind, itemId))
                {
                    result.Add(snapshot);
                }
            }
        }

        return result
            .OrderBy(snapshot => snapshot.ObjectKind, StringComparer.Ordinal)
            .ThenBy(snapshot => Math.Abs(snapshot.ObjectId))
            .ToList();
    }

    private FactoryEntitySnapshot? TryCaptureFactoryEntity(PlanetFactory factory, int entityId)
    {
        if (entityId <= 0 || entityId >= factory.entityCursor || entityId >= factory.entityPool.Length)
        {
            return null;
        }

        ref var entity = ref factory.entityPool[entityId];
        if (entity.id != entityId || entity.protoId <= 0)
        {
            return null;
        }

        var item = LDB.items.Select(entity.protoId);
        var snapshot = new FactoryEntitySnapshot
        {
            SessionId = _sessions.SessionId!,
            PlanetId = factory.planetId,
            ObjectId = entityId,
            ObjectKind = FactoryObjectKinds.Entity,
            ItemId = entity.protoId,
            Name = item?.name ?? string.Empty,
            ComponentKind = GetComponentKind(ref entity),
            Position = CaptureVector(entity.pos),
            Rotation = CaptureQuaternion(entity.rot),
            CapturedAtGameTick = GameMain.gameTick,
        };

        CaptureConnections(factory, entityId, snapshot.Connections);
        CapturePower(factory, ref entity, snapshot);
        CaptureAssembler(factory, ref entity, snapshot);
        CaptureLab(factory, ref entity, snapshot);
        CaptureMiner(factory, ref entity, snapshot);
        CaptureStorage(factory, ref entity, snapshot);
        CaptureTank(factory, ref entity, snapshot);
        CaptureInserter(factory, ref entity, snapshot);
        snapshot.StateHash = CanonicalStateHash.Factory(snapshot);
        snapshot.StateHashVersion = CanonicalStateHash.Version;
        snapshot.EndpointStateHash = CanonicalStateHash.FactoryEndpoint(snapshot);
        snapshot.EndpointStateHashVersion = CanonicalStateHash.Version;
        return snapshot;
    }

    private FactoryEntitySnapshot? TryCapturePrebuild(PlanetFactory factory, int prebuildId)
    {
        if (prebuildId <= 0 || prebuildId >= factory.prebuildCursor || prebuildId >= factory.prebuildPool.Length)
        {
            return null;
        }

        ref var prebuild = ref factory.prebuildPool[prebuildId];
        if (prebuild.id != prebuildId || prebuild.protoId <= 0 || prebuild.isDestroyed)
        {
            return null;
        }

        var item = LDB.items.Select(prebuild.protoId);
        var snapshot = new FactoryEntitySnapshot
        {
            SessionId = _sessions.SessionId!,
            PlanetId = factory.planetId,
            ObjectId = -prebuildId,
            ObjectKind = FactoryObjectKinds.Prebuild,
            ItemId = prebuild.protoId,
            Name = item?.name ?? string.Empty,
            ComponentKind = GetPrefabComponentKind(item?.prefabDesc),
            Position = CaptureVector(prebuild.pos),
            Rotation = CaptureQuaternion(prebuild.rot),
            RecipeId = prebuild.recipeId,
            RecipeName = prebuild.recipeId > 0 ? LDB.recipes.Select(prebuild.recipeId)?.name : null,
            RequiredBuildItemCount = prebuild.itemRequired,
            ConstructionProgress = prebuild.builderValue,
            CapturedAtGameTick = GameMain.gameTick,
        };
        CaptureConnections(factory, -prebuildId, snapshot.Connections);
        snapshot.StateHash = CanonicalStateHash.Factory(snapshot);
        snapshot.StateHashVersion = CanonicalStateHash.Version;
        snapshot.EndpointStateHash = CanonicalStateHash.FactoryEndpoint(snapshot);
        snapshot.EndpointStateHashVersion = CanonicalStateHash.Version;
        return snapshot;
    }

    private static void CaptureConnections(
        PlanetFactory factory,
        int objectId,
        ICollection<FactoryConnectionSnapshot> result)
    {
        for (var slot = 0; slot < 16; slot++)
        {
            factory.ReadObjectConn(objectId, slot, out var isOutput, out var otherObjectId, out var otherSlot);
            if (otherObjectId == 0)
            {
                continue;
            }

            result.Add(new FactoryConnectionSnapshot
            {
                Slot = slot,
                IsOutput = isOutput,
                OtherObjectId = otherObjectId,
                OtherSlot = otherSlot,
            });
        }
    }

    private static void CapturePower(
        PlanetFactory factory,
        ref EntityData entity,
        FactoryEntitySnapshot snapshot)
    {
        var consumerId = entity.powerConId;
        if (consumerId > 0
            && consumerId < factory.powerSystem.consumerCursor
            && consumerId < factory.powerSystem.consumerPool.Length)
        {
            ref var consumer = ref factory.powerSystem.consumerPool[consumerId];
            if (consumer.id == consumerId && consumer.entityId == entity.id)
            {
                snapshot.PowerNetworkId = consumer.networkId;
                snapshot.PowerDemandPerTick = consumer.requiredEnergy;
                snapshot.PowerServeRatio = GetPowerServeRatio(factory.powerSystem, consumer.networkId);
                return;
            }
        }

        var generatorId = entity.powerGenId;
        if (generatorId > 0
            && generatorId < factory.powerSystem.genCursor
            && generatorId < factory.powerSystem.genPool.Length)
        {
            ref var generator = ref factory.powerSystem.genPool[generatorId];
            if (generator.id == generatorId && generator.entityId == entity.id)
            {
                snapshot.PowerNetworkId = generator.networkId;
                snapshot.PowerServeRatio = GetPowerServeRatio(factory.powerSystem, generator.networkId);
                snapshot.Buffers.Add(new FactoryBufferSnapshot
                {
                    Role = "power-generation-current-tick",
                    ItemId = generator.curFuelId,
                    Name = GetItemName(generator.curFuelId),
                    Count = generator.generateCurrentTick > int.MaxValue
                        ? int.MaxValue
                        : (int)Math.Max(0, generator.generateCurrentTick),
                });
            }
        }
    }

    private static double? GetPowerServeRatio(PowerSystem powerSystem, int networkId)
    {
        if (networkId <= 0 || networkId >= powerSystem.netCursor || networkId >= powerSystem.netPool.Length)
        {
            return null;
        }

        var network = powerSystem.netPool[networkId];
        return network is not null && network.id == networkId ? network.consumerRatio : null;
    }

    private static void CaptureAssembler(
        PlanetFactory factory,
        ref EntityData entity,
        FactoryEntitySnapshot snapshot)
    {
        var assemblerId = entity.assemblerId;
        if (assemblerId <= 0
            || assemblerId >= factory.factorySystem.assemblerCursor
            || assemblerId >= factory.factorySystem.assemblerPool.Length)
        {
            return;
        }

        ref var assembler = ref factory.factorySystem.assemblerPool[assemblerId];
        if (assembler.id != assemblerId || assembler.entityId != entity.id)
        {
            return;
        }

        snapshot.RecipeId = assembler.recipeId;
        snapshot.RecipeName = assembler.recipeId > 0 ? LDB.recipes.Select(assembler.recipeId)?.name : null;
        snapshot.IsWorking = assembler.replicating;
        snapshot.Progress = assembler.time;
        snapshot.ProgressRequired = assembler.recipeExecuteData?.timeSpend ?? 0;
        if (assembler.recipeExecuteData is not null)
        {
            AddFactoryBuffers(snapshot.Buffers, "input", assembler.recipeExecuteData.requires, assembler.served, assembler.incServed);
            AddFactoryBuffers(snapshot.Buffers, "output", assembler.recipeExecuteData.products, assembler.produced, null);
        }
    }

    private static void CaptureLab(
        PlanetFactory factory,
        ref EntityData entity,
        FactoryEntitySnapshot snapshot)
    {
        var labId = entity.labId;
        if (labId <= 0
            || labId >= factory.factorySystem.labCursor
            || labId >= factory.factorySystem.labPool.Length)
        {
            return;
        }

        ref var lab = ref factory.factorySystem.labPool[labId];
        if (lab.id != labId || lab.entityId != entity.id)
        {
            return;
        }

        snapshot.RecipeId = lab.recipeId;
        snapshot.RecipeName = lab.recipeId > 0 ? LDB.recipes.Select(lab.recipeId)?.name : null;
        snapshot.IsWorking = lab.replicating;
        snapshot.Progress = lab.researchMode ? lab.hashBytes : lab.time;
        snapshot.ProgressRequired = lab.researchMode
            ? (GameMain.history?.techStates.TryGetValue(lab.techId, out var techState) == true
                ? (int)Math.Min(int.MaxValue, techState.hashNeeded)
                : 0)
            : lab.recipeExecuteData?.timeSpend ?? 0;
        if (lab.researchMode)
        {
            AddFactoryBuffers(snapshot.Buffers, "research-matrix", LabComponent.matrixIds, lab.matrixServed, lab.matrixIncServed);
        }
        else if (lab.recipeExecuteData is not null)
        {
            AddFactoryBuffers(snapshot.Buffers, "input", lab.recipeExecuteData.requires, lab.served, lab.incServed);
            AddFactoryBuffers(snapshot.Buffers, "output", lab.recipeExecuteData.products, lab.produced, null);
        }
    }

    private static void CaptureMiner(
        PlanetFactory factory,
        ref EntityData entity,
        FactoryEntitySnapshot snapshot)
    {
        var minerId = entity.minerId;
        if (minerId <= 0
            || minerId >= factory.factorySystem.minerCursor
            || minerId >= factory.factorySystem.minerPool.Length)
        {
            return;
        }

        ref var miner = ref factory.factorySystem.minerPool[minerId];
        if (miner.id != minerId || miner.entityId != entity.id)
        {
            return;
        }

        snapshot.IsWorking = miner.workstate != EWorkState.Idle;
        snapshot.Progress = miner.time;
        snapshot.ProgressRequired = miner.period;
        snapshot.InsertTargetObjectId = miner.insertTarget == 0 ? null : miner.insertTarget;
        var minerVeins = miner.veins ?? Array.Empty<int>();
        var veinCount = Math.Min(miner.veinCount, minerVeins.Length);
        for (var index = 0; index < veinCount; index++)
        {
            if (minerVeins[index] > 0)
            {
                snapshot.ResourceNodeIds.Add(minerVeins[index]);
            }
        }

        if (miner.productId > 0 || miner.productCount > 0)
        {
            snapshot.Buffers.Add(new FactoryBufferSnapshot
            {
                Role = "mined-output",
                ItemId = miner.productId,
                Name = GetItemName(miner.productId),
                Count = miner.productCount,
            });
        }
    }

    private static void CaptureStorage(
        PlanetFactory factory,
        ref EntityData entity,
        FactoryEntitySnapshot snapshot)
    {
        var storageId = entity.storageId;
        if (storageId <= 0
            || storageId >= factory.factoryStorage.storageCursor
            || storageId >= factory.factoryStorage.storagePool.Length)
        {
            return;
        }

        var storage = factory.factoryStorage.storagePool[storageId];
        if (storage is null || storage.id != storageId || storage.entityId != entity.id)
        {
            return;
        }

        var storageGrids = storage.grids ?? Array.Empty<StorageComponent.GRID>();
        var gridCount = Math.Min(storage.size, storageGrids.Length);
        for (var index = 0; index < gridCount; index++)
        {
            var grid = storageGrids[index];
            if (grid.itemId <= 0 || grid.count <= 0)
            {
                continue;
            }

            snapshot.Buffers.Add(new FactoryBufferSnapshot
            {
                Role = "storage",
                ItemId = grid.itemId,
                Name = GetItemName(grid.itemId),
                Count = grid.count,
                Inc = grid.inc,
            });
        }
    }

    private static void CaptureTank(
        PlanetFactory factory,
        ref EntityData entity,
        FactoryEntitySnapshot snapshot)
    {
        var tankId = entity.tankId;
        if (tankId <= 0
            || tankId >= factory.factoryStorage.tankCursor
            || tankId >= factory.factoryStorage.tankPool.Length)
        {
            return;
        }

        ref var tank = ref factory.factoryStorage.tankPool[tankId];
        if (tank.id != tankId || tank.entityId != entity.id
            || tank.fluidId <= 0 || tank.fluidCount <= 0)
        {
            return;
        }

        snapshot.Buffers.Add(new FactoryBufferSnapshot
        {
            Role = "tank-fluid",
            ItemId = tank.fluidId,
            Name = GetItemName(tank.fluidId),
            Count = tank.fluidCount,
            Inc = tank.fluidInc,
        });
    }

    private static void CaptureInserter(
        PlanetFactory factory,
        ref EntityData entity,
        FactoryEntitySnapshot snapshot)
    {
        var inserterId = entity.inserterId;
        if (inserterId <= 0
            || inserterId >= factory.factorySystem.inserterCursor
            || inserterId >= factory.factorySystem.inserterPool.Length)
        {
            return;
        }

        ref var inserter = ref factory.factorySystem.inserterPool[inserterId];
        if (inserter.id != inserterId || inserter.entityId != entity.id)
        {
            return;
        }

        snapshot.PickTargetObjectId = inserter.pickTarget == 0 ? null : inserter.pickTarget;
        snapshot.InsertTargetObjectId = inserter.insertTarget == 0 ? null : inserter.insertTarget;
        snapshot.FilterItemId = inserter.filter == 0 ? null : inserter.filter;
        snapshot.FilterItemName = inserter.filter > 0 ? GetItemName(inserter.filter) : null;
        snapshot.InserterStage = inserter.stage.ToString();
        snapshot.InserterStackCount = inserter.stackCount;
        snapshot.IsWorking = inserter.pickTarget != 0
            && inserter.insertTarget != 0
            && (inserter.itemCount > 0 || inserter.time > 0);
        snapshot.Progress = inserter.time;
        snapshot.ProgressRequired = inserter.stt;
        if (inserter.itemId > 0 && inserter.itemCount > 0)
        {
            snapshot.Buffers.Add(new FactoryBufferSnapshot
            {
                Role = "inserter-held",
                ItemId = inserter.itemId,
                Name = GetItemName(inserter.itemId),
                Count = inserter.itemCount,
                Inc = inserter.itemInc,
            });
        }
    }

    private static void AddFactoryBuffers(
        ICollection<FactoryBufferSnapshot> target,
        string role,
        int[]? itemIds,
        int[]? counts,
        int[]? incs)
    {
        var length = Math.Min(itemIds?.Length ?? 0, counts?.Length ?? 0);
        for (var index = 0; index < length; index++)
        {
            target.Add(new FactoryBufferSnapshot
            {
                Role = role,
                ItemId = itemIds![index],
                Name = GetItemName(itemIds[index]),
                Count = counts![index],
                Inc = index < (incs?.Length ?? 0) ? incs![index] : 0,
            });
        }
    }

    private static string GetComponentKind(ref EntityData entity)
    {
        if (entity.minerId > 0) return "miner";
        if (entity.assemblerId > 0) return "assembler";
        if (entity.labId > 0) return "lab";
        if (entity.inserterId > 0) return "inserter";
        if (entity.beltId > 0) return "belt";
        if (entity.storageId > 0) return "storage";
        if (entity.tankId > 0) return "tank";
        if (entity.powerGenId > 0) return "power-generator";
        if (entity.powerNodeId > 0) return "power-node";
        if (entity.stationId > 0) return "station";
        if (entity.splitterId > 0) return "splitter";
        if (entity.fractionatorId > 0) return "fractionator";
        if (entity.spraycoaterId > 0) return "spray-coater";
        if (entity.pilerId > 0) return "piler";
        return "other";
    }

    private static string GetPrefabComponentKind(PrefabDesc? descriptor)
    {
        if (descriptor is null) return "other";
        if (descriptor.veinMiner || descriptor.oilMiner || descriptor.minerType != EMinerType.None) return "miner";
        if (descriptor.isLab) return "lab";
        if (descriptor.isAssembler) return "assembler";
        if (descriptor.isInserter) return "inserter";
        if (descriptor.isBelt) return "belt";
        if (descriptor.isStorage) return "storage";
        if (descriptor.isPowerGen) return "power-generator";
        if (descriptor.isPowerNode) return "power-node";
        if (descriptor.isStation) return "station";
        if (descriptor.isSplitter) return "splitter";
        if (descriptor.isFractionator) return "fractionator";
        return "other";
    }

    private BridgeError? ValidateOwnedPlanetOnMainThread(
        string? requestedSessionId,
        int requestedPlanetId,
        out PlanetFactory? factory)
    {
        var error = ValidateOwnedSessionOnMainThread(requestedSessionId, out factory);
        if (error is not null)
        {
            return error;
        }

        if (requestedPlanetId <= 0 || factory!.planetId != requestedPlanetId)
        {
            factory = null;
            return BridgeError.Create(
                BridgeErrorCodes.StaleState,
                "The requested planet does not match the currently loaded local planet.",
                true,
                "Call get_session_state and retry with its current localPlanetId.");
        }

        return null;
    }

    private BridgeError? ValidateOwnedSessionOnMainThread(string? requestedSessionId, out PlanetFactory? factory)
    {
        factory = null;
        var state = _sessions.CaptureOnMainThread();
        if (!state.GameLoaded)
        {
            return BridgeError.Create(
                BridgeErrorCodes.GameNotLoaded,
                "No game session is currently loaded.",
                true,
                "Create and load a Spherewright-owned ordinary world, then retry.");
        }

        if (!state.OwnedBySpherewright)
        {
            return BridgeError.Create(
                BridgeErrorCodes.SessionNotOwned,
                "The loaded game session was not created by this Spherewright Plugin process, so its contents are restricted.",
                false,
                "Return to the main menu and create a Spherewright-owned ordinary world.");
        }

        if (string.IsNullOrWhiteSpace(requestedSessionId)
            || !string.Equals(requestedSessionId, state.SessionId, StringComparison.Ordinal))
        {
            return BridgeError.Create(
                BridgeErrorCodes.StaleSession,
                "The supplied session ID does not match the active owned session.",
                true,
                "Call get_session_state and retry with its sessionId.");
        }

        factory = GameMain.data?.localLoadedPlanetFactory;
        if (factory is null)
        {
            return BridgeError.Create(
                BridgeErrorCodes.NoLocalPlanet,
                "The owned session does not currently have a loaded local factory.",
                true,
                "Wait for the local planet factory to load and retry.");
        }

        return null;
    }

    private AssemblerSnapshot? TryCaptureAssembler(
        PlanetFactory factory,
        int componentId,
        ref AssemblerComponent assembler)
    {
        var entityId = assembler.entityId;
        if (entityId <= 0 || entityId >= factory.entityCursor)
        {
            return null;
        }

        ref var entity = ref factory.entityPool[entityId];
        if (entity.id != entityId || entity.assemblerId != componentId)
        {
            return null;
        }

        var item = LDB.items.Select(entity.protoId);
        var recipe = assembler.recipeId > 0 ? LDB.recipes.Select(assembler.recipeId) : null;
        return new AssemblerSnapshot
        {
            PlanetId = factory.planetId,
            EntityId = entityId,
            ComponentId = componentId,
            BuildingItemId = entity.protoId,
            BuildingName = item?.name ?? string.Empty,
            RecipeId = assembler.recipeId,
            RecipeName = recipe?.name,
            TimeSpent = assembler.time,
            TimeRequired = assembler.recipeExecuteData?.timeSpend ?? 0,
            IsWorking = assembler.replicating,
            Position = new Vector3Snapshot
            {
                X = entity.pos.x,
                Y = entity.pos.y,
                Z = entity.pos.z,
            },
            Revision = _sessions.Revision,
        };
    }

    private static void AddPlayerItemAmounts(
        ICollection<PlayerItemAmount> target,
        int[]? itemIds,
        int[]? counts,
        int[]? bufferedCounts)
    {
        var length = Math.Min(itemIds?.Length ?? 0, counts?.Length ?? 0);
        for (var index = 0; index < length; index++)
        {
            target.Add(new PlayerItemAmount
            {
                ItemId = itemIds![index],
                Name = GetItemName(itemIds[index]),
                Count = counts![index],
                BufferedCount = index < (bufferedCounts?.Length ?? 0) ? bufferedCounts![index] : 0,
            });
        }
    }

    private static void AddCatalogAmounts(
        ICollection<CatalogItemAmount> target,
        int[]? itemIds,
        int[]? counts)
    {
        var length = Math.Min(itemIds?.Length ?? 0, counts?.Length ?? 0);
        for (var index = 0; index < length; index++)
        {
            target.Add(new CatalogItemAmount
            {
                ItemId = itemIds![index],
                Name = GetItemName(itemIds[index]),
                Count = counts![index],
            });
        }
    }

    private static bool ResourceMatches(
        ResourceNodeSnapshot snapshot,
        string? resourceType,
        int? productItemId)
    {
        return (resourceType is null
                || string.Equals(snapshot.ResourceType, resourceType, StringComparison.OrdinalIgnoreCase))
            && (!productItemId.HasValue
                || snapshot.Yields.Any(item => item.ItemId == productItemId.Value));
    }

    private static bool FactoryEntityMatches(
        FactoryEntitySnapshot snapshot,
        string? componentKind,
        int? itemId)
    {
        return (componentKind is null
                || string.Equals(snapshot.ComponentKind, componentKind, StringComparison.OrdinalIgnoreCase))
            && (!itemId.HasValue || snapshot.ItemId == itemId.Value);
    }

    private static Vector3Snapshot CaptureVector(UnityEngine.Vector3 value)
    {
        return new Vector3Snapshot
        {
            X = value.x,
            Y = value.y,
            Z = value.z,
        };
    }

    private static QuaternionSnapshot CaptureQuaternion(UnityEngine.Quaternion value)
    {
        return new QuaternionSnapshot
        {
            X = value.x,
            Y = value.y,
            Z = value.z,
            W = value.w,
        };
    }

    private static string GetItemName(int itemId)
    {
        return itemId > 0 ? LDB.items.Select(itemId)?.name ?? string.Empty : string.Empty;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value!.Trim().ToLowerInvariant();
    }

    private static string ComputeFilterHash(string canonicalFilter)
    {
        using (var sha256 = SHA256.Create())
        {
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(canonicalFilter));
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (var value in bytes)
            {
                builder.Append(value.ToString("x2"));
            }

            return "sha256:" + builder;
        }
    }

    private static BridgeError? ValidateListLimit(int limit, string label)
    {
        return limit >= 1 && limit <= MaximumLimit
            ? null
            : BridgeError.Create(
                BridgeErrorCodes.InvalidRequest,
                $"{label} list limit must be between 1 and {MaximumLimit}.",
                false,
                "Use a bounded limit and retry.");
    }

    private static BridgeError InvalidRequest(string message, string recovery)
    {
        return BridgeError.Create(BridgeErrorCodes.InvalidRequest, message, false, recovery);
    }

    private static BridgeError NotReady(string message)
    {
        return BridgeError.Create(
            BridgeErrorCodes.BridgeNotReady,
            message,
            true,
            "Wait until the owned ordinary world finishes loading, then retry.");
    }

    private static BridgeError StaleCursor(string message)
    {
        return BridgeError.Create(
            BridgeErrorCodes.StaleCursor,
            message,
            true,
            "Discard the cursor and start a new listing with the current session, planet, and filters.");
    }

    private static BridgeError SnapshotCapacityExceeded(string label)
    {
        return BridgeError.Create(
            BridgeErrorCodes.ServerBusy,
            $"The bounded {label} snapshot capacity is temporarily full.",
            true,
            "Wait for an existing snapshot to expire, then start a new listing.");
    }

    private static GameCallResult<ResourceNodeSnapshot> InvalidResource(string message)
    {
        return GameCallResult<ResourceNodeSnapshot>.Failed(BridgeError.Create(
            BridgeErrorCodes.InvalidEntity,
            message,
            false,
            "Refresh the resource-node list and use a current kind and node ID."));
    }

    private static GameCallResult<FactoryEntitySnapshot> InvalidFactoryEntity(string message)
    {
        return GameCallResult<FactoryEntitySnapshot>.Failed(BridgeError.Create(
            BridgeErrorCodes.InvalidEntity,
            message,
            false,
            "Refresh the factory-entity list and use a current object ID."));
    }

    private static string? GetBasicLineRole(PrefabDesc descriptor)
    {
        if (descriptor.oilMiner || descriptor.minerType == EMinerType.Oil)
        {
            return "oil-extractor";
        }

        if (descriptor.veinMiner || descriptor.minerType == EMinerType.Vein)
        {
            return "vein-miner";
        }

        if (descriptor.isBelt)
        {
            return "belt";
        }

        if (descriptor.isStorage && !descriptor.isTank && !descriptor.isStation && !descriptor.isBattleBase)
        {
            return "storage";
        }

        if (descriptor.isTank)
        {
            return "tank";
        }

        if (descriptor.isAssembler && descriptor.assemblerRecipeType == ERecipeType.Smelt)
        {
            return "smelter";
        }

        if (descriptor.isAssembler && descriptor.assemblerRecipeType == ERecipeType.Refine)
        {
            return "refinery";
        }

        if (descriptor.isAssembler)
        {
            return "assembler";
        }

        if (descriptor.isLab)
        {
            return "matrix-lab";
        }

        if (descriptor.isInserter)
        {
            return "inserter";
        }

        if (descriptor.isPowerGen && descriptor.windForcedPower && descriptor.isPowerNode)
        {
            return "wind-power";
        }

        if (descriptor.isPowerGen)
        {
            return "power-generator";
        }

        if (descriptor.isPowerNode)
        {
            return "power-node";
        }

        return null;
    }

    private static BuildCatalogIngredient CreateIngredient(int itemId, int count)
    {
        var item = LDB.items.Select(itemId);
        return new BuildCatalogIngredient
        {
            ItemId = itemId,
            Name = item?.name ?? string.Empty,
            Count = count,
            RawMaterial = item?.isRaw ?? false,
        };
    }

    private static BasicLineRecommendation? CreateRecommendation(BuildCatalog catalog)
    {
        var storage = catalog.Buildings.FirstOrDefault(item => item.Available && item.Role == "storage");
        var smelter = catalog.Buildings.FirstOrDefault(item => item.Available && item.Role == "smelter");
        var inserter = catalog.Buildings.FirstOrDefault(item => item.Available && item.Role == "inserter");
        var power = catalog.Buildings.FirstOrDefault(item => item.Available && item.Role == "wind-power");
        var recipe = catalog.Recipes.FirstOrDefault(item => item.Unlocked && item.Inputs.Count > 0 && item.Inputs[0].RawMaterial)
            ?? catalog.Recipes.FirstOrDefault(item => item.Unlocked && item.Inputs.Count > 0 && item.Outputs.Count > 0);
        if (storage is null || smelter is null || inserter is null || power is null || recipe is null)
        {
            return null;
        }

        return new BasicLineRecommendation
        {
            StorageItemId = storage.ItemId,
            AssemblerItemId = smelter.ItemId,
            InserterItemId = inserter.ItemId,
            PowerGeneratorItemId = power.ItemId,
            RecipeId = recipe.RecipeId,
            InputItemId = recipe.Inputs[0].ItemId,
            OutputItemId = recipe.Outputs[0].ItemId,
        };
    }

    private static GameCallResult<AssemblerSnapshot> InvalidAssembler(string message)
    {
        return GameCallResult<AssemblerSnapshot>.Failed(BridgeError.Create(
            BridgeErrorCodes.InvalidEntity,
            message,
            false,
            "Refresh the assembler list and use a current assembler entity ID."));
    }
}
