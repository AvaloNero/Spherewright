using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;
using UnityEngine;

namespace Spherewright.Plugin.Game;

internal sealed partial class NormalGameActionCoordinator
{
    // Read-only reproduction of current CargoTraffic.AlterBeltRenderer's final
    // entity/collider rotation calculation. Never writes pose, renderer or path.
    private static bool ProvesNativeBeltRotation(PlanetFactory factory, int entityId)
    {
        var entity = factory.entityPool[entityId]; // Caller already checked entity identity/pools.
        var traffic = factory.cargoTraffic;
        if (traffic?.beltPool is null || entity.beltId <= 0 || entity.beltId >= traffic.beltCursor
            || entity.beltId >= traffic.beltPool.Length) return false;
        var belt = traffic.beltPool[entity.beltId];
        if (belt.id != entity.beltId || belt.entityId != entityId || traffic.pathPool is null
            || belt.segPathId <= 0 || belt.segPathId >= traffic.pathCursor || belt.segPathId >= traffic.pathPool.Length) return false;
        var path = traffic.GetCargoPath(belt.segPathId);
        if (path is null || path.id != belt.segPathId || path.closed || path.pointPos is null
            || !BeltSourceRotationPolicy.CanReadSegment(path.id, path.pathLength, path.pointPos.Length, belt.segIndex, belt.segLength)) return false;
        var a = path.pointPos[belt.segIndex];
        var b = path.pointPos[belt.segIndex + belt.segLength - 1];
        if (!IsFinite(a.x) || !IsFinite(a.y) || !IsFinite(a.z) || !IsFinite(b.x) || !IsFinite(b.y) || !IsFinite(b.z)) return false;
        // Keep the exact float operation order used by the inspected native method.
        var centre = new Vector3((a.x + b.x) * .50016f, (a.y + b.y) * .50016f, (a.z + b.z) * .50016f);
        var forward = new Vector3(b.x - a.x, b.y - a.y, b.z - a.z);
        var length = Mathf.Sqrt(forward.x * forward.x + forward.y * forward.y + forward.z * forward.z);
        if (!IsFinite(length) || !IsFinite(centre.x) || !IsFinite(centre.y) || !IsFinite(centre.z) || centre.sqrMagnitude < 1f) return false;
        var derived = length > .6f ? Quaternion.LookRotation(forward, centre)
            : Quaternion.LookRotation(centre) * Quaternion.Euler(90f, 0f, 0f);
        var chunks = factory.planet.physics?.colChunks;
        var chunk = entity.colliderId >> 20;
        var index = entity.colliderId & 0xFFFFF;
        if (!factory.planet.factoryLoaded || entity.colliderId <= 0 || chunks is null || chunk < 0 || chunk >= chunks.Length
            || chunks[chunk]?.colliderPool is null || index <= 0 || index >= chunks[chunk].cursor || index >= chunks[chunk].colliderPool.Length) return false;
        var collider = chunks[chunk].colliderPool[index];
        if (collider.objId != entityId || collider.objType != EObjectType.Entity) return false;
        QuaternionSnapshot Copy(Quaternion q) => new QuaternionSnapshot { X=q.x, Y=q.y, Z=q.z, W=q.w };
        return BeltSourceRotationPolicy.MatchesNativeRotation(Copy(entity.rot), Copy(derived))
            && BeltSourceRotationPolicy.MatchesNativeRotation(Copy(collider.q), Copy(derived))
            && collider.pos.x == centre.x && collider.pos.y == centre.y && collider.pos.z == centre.z;
    }
}
