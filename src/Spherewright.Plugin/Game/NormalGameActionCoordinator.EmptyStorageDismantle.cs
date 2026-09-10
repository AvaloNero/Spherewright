using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;

namespace Spherewright.Plugin.Game;

internal sealed partial class NormalGameActionCoordinator
{
    private static EmptyStorageDismantleBoundary CaptureEmptyStorageDismantleBoundary(
        FactoryEntitySnapshot target, bool retainSurvivors)
    {
        var factory = GameMain.data?.localLoadedPlanetFactory;
        if (!EmptyStorageDismantlePolicy.SupportsSnapshot(target)
            || factory is null || !ReferenceEquals(factory, GameMain.localPlanet?.factory)
            || factory.factoryStorage is null || factory.factorySystem is null || factory.transport is null
            || factory.entityPool is null || factory.prebuildPool is null
            || factory.entityConnPool is null || factory.prebuildConnPool is null
            || factory.factoryStorage.storagePool is null || factory.transport.dispenserPool is null
            || factory.factorySystem.inserterPool is null
            || !EmptyStorageDismantlePolicy.BoundedPool(factory.entityCursor, factory.entityPool.Length)
            || !EmptyStorageDismantlePolicy.BoundedPool(factory.prebuildCursor, factory.prebuildPool.Length)
            || factory.entityConnPool.Length < factory.entityCursor * 16
            || factory.prebuildConnPool.Length < factory.prebuildCursor * 16
            || !EmptyStorageDismantlePolicy.BoundedPool(factory.factoryStorage.storageCursor, factory.factoryStorage.storagePool.Length)
            || !EmptyStorageDismantlePolicy.BoundedPool(factory.transport.dispenserCursor, factory.transport.dispenserPool.Length)
            || !EmptyStorageDismantlePolicy.BoundedPool(factory.factorySystem.inserterCursor, factory.factorySystem.inserterPool.Length)
            || !TryCaptureConfigurableStorage(target.ObjectId, 0, out var storage, out var contents, out _)
            || storage is null || !EmptyStorageDismantlePolicy.EmptyDefaultContents(contents)
            || !EmptyStorageDismantlePolicy.SingleLayer(storage.id, storage.previous, storage.next, storage.bottom, storage.top,
                storage.previousStorage is not null, storage.nextStorage is not null,
                ReferenceEquals(storage.bottomStorage, storage), ReferenceEquals(storage.topStorage, storage)))
            throw new InvalidOperationException("Empty storage recovery requires one default, completely empty, single-layer2101 and complete bounded native evidence.");

        var entity = factory.entityPool[target.ObjectId];
        if (entity.storageId != storage.id || storage.entityId != target.ObjectId || storage.isPlayerInventory
            || entity.beltId != 0 || entity.inserterId != 0 || entity.assemblerId != 0 || entity.minerId != 0
            || entity.labId != 0 || entity.tankId != 0 || entity.stationId != 0 || entity.dispenserId != 0
            || entity.splitterId != 0 || entity.fractionatorId != 0 || entity.ejectorId != 0 || entity.siloId != 0
            || entity.powerConId != 0 || entity.powerNodeId != 0 || entity.powerGenId != 0
            || entity.powerAccId != 0 || entity.powerExcId != 0 || entity.monitorId != 0
            || entity.speakerId != 0 || entity.spraycoaterId != 0 || entity.pilerId != 0
            || entity.turretId != 0 || entity.beaconId != 0 || entity.fieldGenId != 0 || entity.battleBaseId != 0
            || entity.constructionModuleId != 0 || entity.combatModuleId != 0
            || entity.extraInfoId != 0 || entity.markerId != 0)
            throw new InvalidOperationException("Empty storage recovery does not remove add-ons, other functional components or named/marked storage.");

        for (var id = 1; id < factory.entityCursor; id++)
        {
            var other = factory.entityPool[id];
            if (other.id == 0) continue;
            if (other.id != id || id != target.ObjectId && other.storageId == storage.id)
                throw new InvalidOperationException("Storage entity/component identity is ambiguous.");
            for (var slot = 0; slot < 16; slot++)
            {
                factory.ReadObjectConn(id, slot, out _, out var endpoint, out _);
                if (!EmptyStorageDismantlePolicy.ReferenceIsSafe(target.ObjectId, id, endpoint)
                    || id == target.ObjectId && factory.entityConnPool[id * 16 + slot] != 0)
                    throw new InvalidOperationException("Empty storage recovery rejects every outgoing or incoming entity connection, including one-sided references.");
            }
        }
        for (var id = 1; id < factory.prebuildCursor; id++)
        {
            if (factory.prebuildPool[id].id == 0) continue;
            if (factory.prebuildPool[id].id != id) throw new InvalidOperationException("Prebuild identity is malformed.");
            for (var slot = 0; slot < 16; slot++)
            {
                factory.ReadObjectConn(-id, slot, out _, out var endpoint, out _);
                if (!EmptyStorageDismantlePolicy.ReferenceIsSafe(target.ObjectId, -id, endpoint))
                    throw new InvalidOperationException("A prebuild references the empty storage; wait for normal completion, then reassess.");
            }
        }
        foreach (var inserter in factory.factorySystem.inserterPool.Take(factory.factorySystem.inserterCursor).Skip(1))
            if (inserter.id != 0 && (inserter.pickTarget == target.ObjectId || inserter.insertTarget == target.ObjectId))
                throw new InvalidOperationException("A cached sorter target still references the empty storage.");
        foreach (var other in factory.factoryStorage.storagePool.Take(factory.factoryStorage.storageCursor).Skip(1))
        {
            if (other is null || other.id == 0 || ReferenceEquals(other, storage)) continue;
            if (other.id == storage.id || other.previous == storage.id || other.next == storage.id
                || other.bottom == storage.id || other.top == storage.id
                || ReferenceEquals(other.previousStorage, storage) || ReferenceEquals(other.nextStorage, storage)
                || ReferenceEquals(other.bottomStorage, storage) || ReferenceEquals(other.topStorage, storage))
                throw new InvalidOperationException("Another native storage still references this layer.");
        }
        foreach (var dispenser in factory.transport.dispenserPool.Take(factory.transport.dispenserCursor).Skip(1))
            if (dispenser is not null && dispenser.id != 0
                && (dispenser.storageId == storage.id || ReferenceEquals(dispenser.storage, storage)))
                throw new InvalidOperationException("A native dispenser references the empty storage.");

        return new EmptyStorageDismantleBoundary
        {
            NativeHash = EmptyStorageDismantlePolicy.Fingerprint(target, contents!, StorageLinksFingerprint(storage)),
            StorageId = storage.id,
            Entities = retainSurvivors ? factory.entityPool.Take(factory.entityCursor).ToArray() : null,
            Prebuilds = retainSurvivors ? factory.prebuildPool.Take(factory.prebuildCursor).ToArray() : null,
            EntityConnections = retainSurvivors ? factory.entityConnPool.Take(factory.entityCursor * 16).ToArray() : null,
            PrebuildConnections = retainSurvivors ? factory.prebuildConnPool.Take(factory.prebuildCursor * 16).ToArray() : null,
        };
    }

    private static void VerifyEmptyStorageDismantleBoundary(int removedId, EmptyStorageDismantleBoundary before)
    {
        var factory = GameMain.data.localLoadedPlanetFactory;
        if (factory is null || before.Entities is null || before.Prebuilds is null
            || before.EntityConnections is null || before.PrebuildConnections is null
            || factory.entityCursor != before.Entities.Length || factory.prebuildCursor != before.Prebuilds.Length
            || factory.entityPool[removedId].id != 0 || factory.factoryStorage.storagePool[before.StorageId] is null
            || factory.factoryStorage.storagePool[before.StorageId].id != 0
            || factory.factoryStorage.storagePool[before.StorageId].entityId != 0)
            throw new InvalidOperationException("Empty storage removal did not prove the exact entity and native component disappeared.");
        for (var id = 1; id < before.Entities.Length; id++)
            if (id != removedId && !before.Entities[id].Equals(factory.entityPool[id]))
                throw new InvalidOperationException("Empty storage removal changed another native entity.");
        for (var id = 1; id < before.Prebuilds.Length; id++)
            if (!before.Prebuilds[id].Equals(factory.prebuildPool[id]))
                throw new InvalidOperationException("Empty storage removal changed a prebuild.");
        if (!before.EntityConnections.SequenceEqual(factory.entityConnPool.Take(before.EntityConnections.Length))
            || !before.PrebuildConnections.SequenceEqual(factory.prebuildConnPool.Take(before.PrebuildConnections.Length)))
            throw new InvalidOperationException("Empty storage removal changed surviving entity or prebuild connections.");
    }

    private sealed class EmptyStorageDismantleBoundary
    {
        public string NativeHash { get; set; } = string.Empty;
        public int StorageId { get; set; }
        public EntityData[]? Entities { get; set; }
        public PrebuildData[]? Prebuilds { get; set; }
        public int[]? EntityConnections { get; set; }
        public int[]? PrebuildConnections { get; set; }
    }
}
