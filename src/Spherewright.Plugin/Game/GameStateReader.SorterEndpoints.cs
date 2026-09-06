using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;
using UnityEngine;

namespace Spherewright.Plugin.Game;

internal sealed partial class GameStateReader
{
    // The same native poses used by GetInserterEndpointPoints, copied only for detail reads.
    private static SorterEndpointObservation CaptureSorterEndpoints(PlanetFactory factory, int entityId)
    {
        var unavailable = new SorterEndpointObservation
        {
            CapturedAtGameTick = GameMain.gameTick, ReasonCode = "sorter_endpoint_identity_unavailable",
        };
        if (factory.entityPool is null || entityId <= 0 || entityId >= factory.entityCursor
            || entityId >= factory.entityPool.Length || factory.entityPool[entityId].id != entityId) return unavailable;
        var entity = factory.entityPool[entityId];
        var prefab = LDB.items.Select(entity.protoId)?.prefabDesc;
        if (prefab is null) return unavailable;
        var points = new List<SorterEndpointSnapshot>();
        if (prefab.isBelt)
        {
            var basis = Quaternion.AngleAxis(entity.tilt, entity.rot * Vector3.forward) * entity.rot;
            var yaw = new[] { 0f, 90f, 180f, -90f };
            for (var i = 0; i < yaw.Length; i++)
                points.Add(new SorterEndpointSnapshot
                {
                    Index = i, Slot = -1, Position = CaptureVector(entity.pos),
                    Outward = CaptureVector(basis * Quaternion.Euler(0f, yaw[i], 0f) * Vector3.forward),
                });
        }
        else
        {
            var slots = prefab.slotPoses;
            unavailable.ReasonCode = "native_sorter_slots_unavailable_or_over_limit";
            if (slots is null || factory.entityConnPool is null
                || !SorterEndpointObservationPolicy.CanReadConnections(entityId, factory.entityConnPool.Length, slots.Length))
                return unavailable;
            var basis = new Pose(entity.pos, entity.rot);
            for (var i = 0; i < slots.Length; i++)
            {
                var pose = slots[i].GetTransformedBy(basis);
                factory.ReadObjectConn(entityId, i, out _, out var otherObjectId, out var otherSlot);
                points.Add(new SorterEndpointSnapshot
                {
                    Index = i, Slot = i, Position = CaptureVector(pose.position), Outward = CaptureVector(pose.rotation * Vector3.forward),
                    Occupied = otherObjectId != 0, OtherObjectId = otherObjectId, OtherSlot = otherObjectId == 0 ? (int?)null : otherSlot,
                });
            }
        }
        return SorterEndpointObservationPolicy.Create(prefab.isBelt, GameMain.gameTick, points);
    }
}
