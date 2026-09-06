using Spherewright.Bridge.Core.Factory;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Actions;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Factory;
using Spherewright.Contracts.Sessions;

namespace Spherewright.Plugin.Game;

internal sealed partial class NormalGameActionCoordinator
{
    private GameCallResult<PreparedNormalAction> PrepareStorageConfigurationOnMainThread(
        string? sessionId, PrepareConfigureBuildingRequest request, SessionState session)
    {
        var snapshot = _reader.InspectFactoryEntityOnMainThread(sessionId,
            new InspectFactoryEntityRequest { PlanetId = request.PlanetId, ObjectId = request.EntityId });
        if (!snapshot.Success || snapshot.Value is null)
            return GameCallResult<PreparedNormalAction>.Failed(snapshot.Error!);
        if (snapshot.Value.StateHash != request.ExpectedFactoryStateHash)
            return StalePlan("The exact warehouse contents or configuration changed after inspection.");
        if (request.RecipeId != 0 || request.TechId != 0)
            return InvalidPlan("Storage capacity operations do not select recipes or technologies.");
        if (!TryCaptureConfigurableStorage(request.EntityId, request.FilterItemId,
                out var storage, out var before, out var sizes))
            return InvalidPlan("Storage capacity configuration requires one unstacked ordinary2101 warehouse with bounded valid native grids.");
        StorageUiState after;
        try
        {
            after = StorageConfigurationPolicy.Project(before!, request.StorageOperation,
                request.FilterItemId, request.StorageBannedGridCount, sizes!);
            if (!StorageFilterChangesUnlocked(before!, after))
                return InvalidPlan("Every newly assigned storage filter item must be unlocked.");
        }
        catch (ArgumentException ex) { return InvalidPlan(ex.Message); }
        var beforeHash = StorageConfigurationPolicy.Fingerprint(before!);
        var identityHash = StorageLinksFingerprint(storage!);
        var expectedHash = CanonicalStateHash.Combine(NormalActionKinds.ConfigureBuilding,
            sessionId, request.PlanetId, request.EntityId, snapshot.Value.StateHash, snapshot.Value.EndpointStateHash, identityHash, beforeHash,
            request.Mode, request.StorageOperation, request.FilterItemId, request.StorageBannedGridCount,
            StorageConfigurationPolicy.Fingerprint(after));
        var plan = NormalActionPlanPayload.StorageConfiguration(sessionId!, request.PlanetId, request.EntityId,
            snapshot.Value.StateHash, snapshot.Value.EndpointStateHash, identityHash, beforeHash, expectedHash, request.StorageOperation,
            request.FilterItemId, request.StorageBannedGridCount, after);
        var result = AddPreparedPlan(plan, session, 1,
            "One ordinary warehouse UI capacity/filter operation preserves every ordered item/count/inc and all topology. It does not move stock or guarantee throughput; bans count the final disabled automation grids.");
        if (result.Value is not null)
        {
            result.Value.TargetObjectId = request.EntityId;
            result.Value.PlannedStorageOperation = request.StorageOperation;
            result.Value.PlannedStorageConfiguration = StorageConfigurationPolicy.ToSnapshot(after);
        }
        return result;
    }

    private BridgeError? RevalidateStorageConfigurationOnMainThread(NormalActionPlanPayload plan)
    {
        var snapshot = _reader.InspectFactoryEntityOnMainThread(plan.SessionId,
            new InspectFactoryEntityRequest { PlanetId = plan.PlanetId, ObjectId = plan.EntityId });
        if (!snapshot.Success || snapshot.Value is null || snapshot.Value.StateHash != plan.FactoryStateHash
            || snapshot.Value.EndpointStateHash != plan.StorageEndpointStateHash
            || !TryCaptureConfigurableStorage(plan.EntityId, plan.ConfigureFilterItemId,
                out var storage, out var before, out var sizes)
            || StorageLinksFingerprint(storage!) != plan.StorageIdentityStateHash
            || StorageConfigurationPolicy.Fingerprint(before!) != plan.StorageNativeStateHash)
            return Stale("The exact warehouse, ordered grids, inventory, or automation configuration changed.");
        try
        {
            var after = StorageConfigurationPolicy.Project(before!, plan.StorageOperation,
                plan.ConfigureFilterItemId, plan.StorageBannedGridCount, sizes!);
            return StorageFilterChangesUnlocked(before!, after) && plan.StorageExpectedAfter is not null
                && StorageConfigurationPolicy.Fingerprint(after) == StorageConfigurationPolicy.Fingerprint(plan.StorageExpectedAfter)
                ? null : Stale("Storage filter unlocks or native stack sizes changed after preparation.");
        }
        catch (ArgumentException) { return Stale("The warehouse UI operation is no longer applicable."); }
    }

    private void ExecuteStorageConfigurationOnMainThread(ActionRecord action)
    {
        var plan = action.Plan;
        if (RevalidateStorageConfigurationOnMainThread(plan) is not null
            || !TryCaptureConfigurableStorage(plan.EntityId, plan.ConfigureFilterItemId,
                out var storage, out var before, out _) || plan.StorageExpectedAfter is null)
            throw new InvalidOperationException("Storage configuration lost its exact pre-write proof.");
        var factory = GameMain.localPlanet!.factory;
        var entityBefore = factory.entityPool[plan.EntityId];
        var packageBefore = CapturePlayerPackageState(GameMain.mainPlayer);
        var gridArrayBefore = storage!.grids;
        var linksBefore = StorageLinksFingerprint(storage);
        var endpoint = _reader.InspectFactoryEntityOnMainThread(plan.SessionId,
            new InspectFactoryEntityRequest { PlanetId = plan.PlanetId, ObjectId = plan.EntityId });
        if (!endpoint.Success || endpoint.Value is null)
            throw new InvalidOperationException("Storage endpoint evidence unavailable before configuration.");
        endpoint.Value.StorageConfiguration = StorageConfigurationPolicy.ToSnapshot(plan.StorageExpectedAfter);
        var expectedEndpoint = CanonicalStateHash.FactoryEndpoint(endpoint.Value);

        if (plan.StorageOperation == StorageConfigurationOperations.SetBans)
            storage.SetBans(plan.StorageBannedGridCount);
        else
        {
            if (plan.StorageOperation != StorageConfigurationOperations.ClearFilters)
                storage.type = EStorageType.Filtered;
            for (var i = 0; i < before!.Grids.Count; i++)
            {
                var grid = before.Grids[i];
                if (plan.StorageOperation == StorageConfigurationOperations.ClearFilters
                    || (plan.StorageOperation == StorageConfigurationOperations.LockOccupied && grid.ItemId > 0)
                    || (plan.StorageOperation == StorageConfigurationOperations.FilterEmptyOrMatching
                        && (grid.Count == 0 || grid.ItemId == plan.ConfigureFilterItemId)))
                    storage.SetFilter(i, plan.StorageExpectedAfter.Grids[i].Filter);
            }
            if (plan.StorageOperation == StorageConfigurationOperations.ClearFilters)
                storage.type = EStorageType.Default;
            // Same normal notification used by BuildingParameters storage paste.
            storage.NotifyStorageChange();
        }

        var readback = _reader.InspectFactoryEntityOnMainThread(plan.SessionId,
            new InspectFactoryEntityRequest { PlanetId = plan.PlanetId, ObjectId = plan.EntityId });
        if (!TryCaptureConfigurableStorage(plan.EntityId, plan.ConfigureFilterItemId,
                out var sameStorage, out var after, out _)
            || !ReferenceEquals(storage, sameStorage) || !ReferenceEquals(gridArrayBefore, storage.grids)
            || !entityBefore.Equals(factory.entityPool[plan.EntityId])
            || linksBefore != StorageLinksFingerprint(storage)
            || StorageConfigurationPolicy.Fingerprint(after!) != StorageConfigurationPolicy.Fingerprint(plan.StorageExpectedAfter)
            || CapturePlayerPackageState(GameMain.mainPlayer) != packageBefore
            || !readback.Success || readback.Value is null || readback.Value.EndpointStateHash != expectedEndpoint)
            throw new InvalidOperationException("Storage UI configuration could not prove exact ordered inventory, filters, identity, topology and player preservation.");
        action.TargetObjectId = plan.EntityId;
        Complete(action, "Native warehouse capacity/filter configuration applied once; ordered item/count/inc, identity, topology and player inventory were preserved. Delivery and throughput require separate observation.");
        action.AfterStateHash = CanonicalStateHash.Combine(BuildingConfigurationModes.StorageCapacity,
            readback.Value.StateHash, StorageConfigurationPolicy.Fingerprint(after!));
    }

    private static string StorageLinksFingerprint(StorageComponent storage) => CanonicalStateHash.Combine(
        "storage-links-v1", storage.id, storage.entityId, storage.previous, storage.next, storage.bottom, storage.top,
        storage.previousStorage?.id, storage.nextStorage?.id, storage.bottomStorage?.id, storage.topStorage?.id,
        storage.isPlayerInventory, storage.size);

    private static bool StorageFilterChangesUnlocked(StorageUiState before, StorageUiState after) =>
        after.Grids.Select((grid, i) => grid.Filter == 0 || grid.Filter == before.Grids[i].Filter
            || GameMain.history.ItemUnlocked(grid.Filter)).All(unlocked => unlocked);

    private static bool TryCaptureConfigurableStorage(int entityId, int extraFilter,
        out StorageComponent? storage, out StorageUiState? state, out IReadOnlyDictionary<int, int>? sizes)
    {
        storage = null; state = null; sizes = null;
        var factory = GameMain.localPlanet?.factory;
        if (factory is null || entityId <= 0 || entityId >= factory.entityCursor
            || entityId >= factory.entityPool.Length || factory.entityPool[entityId].id != entityId
            || factory.entityPool[entityId].protoId != 2101 || !TryGetStorage(factory, entityId, out storage)
            || storage is null || storage.isPlayerInventory || storage.previous != 0 || storage.next != 0
            || storage.previousStorage is not null || storage.nextStorage is not null
            || storage.size < 1 || storage.size > BlueprintStoragePolicy.MaximumGridCount
            || storage.grids is null || storage.grids.Length != storage.size
            || (storage.type != EStorageType.Default && storage.type != EStorageType.Filtered)) return false;
        var grids = storage.grids.Select(grid => new StorageUiGrid(grid.itemId, grid.count, grid.inc, grid.filter, grid.stackSize)).ToArray();
        var catalog = new Dictionary<int, int>();
        foreach (var itemId in grids.Select(grid => grid.ItemId).Concat(grids.Select(grid => grid.Filter)).Append(extraFilter).Distinct())
        {
            if (itemId == 0) continue;
            if (itemId < 0 || itemId >= 12000 || itemId >= StorageComponent.itemStackCount.Length
                || LDB.items.Select(itemId) is null || StorageComponent.itemStackCount[itemId] <= 0) return false;
            catalog[itemId] = StorageComponent.itemStackCount[itemId];
        }
        state = new StorageUiState(storage.type == EStorageType.Default ? "default" : "filtered", storage.bans, grids);
        try { StorageConfigurationPolicy.Validate(state, catalog); }
        catch (ArgumentException) { return false; }
        sizes = catalog;
        return true;
    }

    private sealed partial class NormalActionPlanPayload
    {
        public string StorageOperation { get; private set; } = string.Empty;
        public int StorageBannedGridCount { get; private set; } = -1;
        public string StorageNativeStateHash { get; private set; } = string.Empty;
        public string StorageIdentityStateHash { get; private set; } = string.Empty;
        public string StorageEndpointStateHash { get; private set; } = string.Empty;
        public StorageUiState? StorageExpectedAfter { get; private set; }

        public static NormalActionPlanPayload StorageConfiguration(string sessionId, int planetId, int entityId,
            string factoryHash, string endpointHash, string identityHash, string nativeHash, string expectedHash, string operation, int filter, int bans,
            StorageUiState after) => new NormalActionPlanPayload
        {
            ActionKind = NormalActionKinds.ConfigureBuilding, ConfigureMode = BuildingConfigurationModes.StorageCapacity,
            SessionId = sessionId, PlanetId = planetId, EntityId = entityId, FactoryStateHash = factoryHash,
            StorageEndpointStateHash = endpointHash, StorageIdentityStateHash = identityHash,
            StorageNativeStateHash = nativeHash, ExpectedStateHash = expectedHash, StorageOperation = operation,
            ConfigureFilterItemId = filter, StorageBannedGridCount = bans, StorageExpectedAfter = after,
        };
    }
}
