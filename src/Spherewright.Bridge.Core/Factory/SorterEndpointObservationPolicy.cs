using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

public static class SorterEndpointObservationPolicy
{
    public const int MaximumNativeSlots = 16;

    public static bool CanReadConnections(int entityId, int poolLength, int slotCount) => entityId > 0
        && slotCount > 0 && slotCount <= MaximumNativeSlots
        && (long)entityId * MaximumNativeSlots + slotCount <= poolLength;

    public static SorterEndpointObservation Create(bool belt, long gameTick,
        IReadOnlyList<SorterEndpointSnapshot>? endpoints)
    {
        var result = new SorterEndpointObservation
        {
            Kind = belt ? "belt_virtual" : "native_slots", CapturedAtGameTick = gameTick,
            ReasonCode = "sorter_endpoint_evidence_invalid",
        };
        if (gameTick < 0 || endpoints is null || endpoints.Count < 1
            || endpoints.Count > MaximumNativeSlots || (belt && endpoints.Count != 4)) return result;
        for (var i = 0; i < endpoints.Count; i++)
        {
            var point = endpoints[i];
            if (point is null || point.Index != i || !Finite(point.Position) || !Finite(point.Outward)) return result;
            var direction = point.Outward;
            var lengthSquared = (double)direction.X * direction.X + (double)direction.Y * direction.Y
                + (double)direction.Z * direction.Z;
            if (Math.Abs(lengthSquared - 1d) > 0.01d) return result;
            if (belt)
            {
                if (point.Slot != -1 || point.Occupied.HasValue || point.OtherObjectId.HasValue || point.OtherSlot.HasValue)
                    return result;
            }
            else if (point.Slot != i || !point.OtherObjectId.HasValue || !point.Occupied.HasValue
                || point.OtherObjectId == int.MinValue || point.Occupied.Value != (point.OtherObjectId.Value != 0)
                || (point.Occupied.Value && (!point.OtherSlot.HasValue || point.OtherSlot < 0 || point.OtherSlot > 15))
                || (!point.Occupied.Value && point.OtherSlot.HasValue)) return result;
        }
        // Never return partial poses or retain caller-owned mutable vectors/lists.
        result.Endpoints = endpoints.Select(point => new SorterEndpointSnapshot
        {
            Index = point.Index, Slot = point.Slot, Position = Copy(point.Position), Outward = Copy(point.Outward),
            Occupied = point.Occupied, OtherObjectId = point.OtherObjectId, OtherSlot = point.OtherSlot,
        }).ToList();
        result.State = "observed";
        result.ReasonCode = null;
        return result;
    }

    private static Vector3Snapshot Copy(Vector3Snapshot source) => new Vector3Snapshot { X = source.X, Y = source.Y, Z = source.Z };
    private static bool Finite(Vector3Snapshot? vector) => vector is not null
        && new[] { vector.X, vector.Y, vector.Z }.All(value => !float.IsNaN(value) && !float.IsInfinity(value) && Math.Abs(value) <= 10000f);
}
