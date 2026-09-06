using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;

namespace Spherewright.Plugin.Game;

internal sealed partial class GameStateReader
{
    // Detail-only: never scan cargo for every entity in a retained pagination snapshot.
    private static BeltCargoSnapshot CaptureBeltCargo(PlanetFactory factory, int entityId)
    {
        var result = new BeltCargoSnapshot { CapturedAtGameTick = GameMain.gameTick };
        result.ReasonCode = "belt_identity_unavailable";
        if (entityId <= 0 || factory.entityPool is null || entityId >= factory.entityCursor
            || entityId >= factory.entityPool.Length || factory.entityPool[entityId].id != entityId)
            return result;
        var traffic = factory.cargoTraffic;
        var beltId = factory.entityPool[entityId].beltId;
        if (traffic?.beltPool is null || beltId <= 0 || beltId >= traffic.beltCursor
            || beltId >= traffic.beltPool.Length) return result;
        var belt = traffic.beltPool[beltId];
        if (belt.id != beltId || belt.entityId != entityId) return result;

        result.ReasonCode = "cargo_path_unavailable";
        // The native GetCargoPath guard uses &&, so check BOTH bounds before calling it.
        if (traffic.pathPool is null || belt.segPathId <= 0 || belt.segPathId >= traffic.pathCursor
            || belt.segPathId >= traffic.pathPool.Length) return result;
        var path = traffic.GetCargoPath(belt.segPathId);
        if (path is null || path.id != belt.segPathId || path.buffer is null
            || path.cargoContainer is null || !ReferenceEquals(path.cargoContainer, factory.cargoContainer))
            return result;
        result.PathId = path.id;
        result.PathLengthCells = path.pathLength;
        result.SegmentStartCell = belt.segIndex;
        result.SegmentLengthCells = belt.segLength;
        result.PathClosed = path.closed;
        if (!BeltCargoObservationPolicy.TryGetWindow(path.pathLength, belt.segIndex, belt.segLength,
                out var start, out var length, out var reason))
        {
            result.ReasonCode = reason;
            return result;
        }
        var buffer = path.buffer;
        if (path.pathLength > buffer.Length) return result;

        // All DSP reads stay in this one main-thread call. The native reader uses this
        // same buffer lock; it is reentrant. Only a <=530-byte copy crosses into Core.
        lock (buffer)
        {
            var window = new byte[length];
            Array.Copy(buffer, start, window, 0, length);
            if (!BeltCargoObservationPolicy.TryLocateCargo(window, start, path.pathLength,
                    belt.segIndex, belt.segLength, path.closed, out var references, out reason))
            {
                result.ReasonCode = reason;
                return result;
            }
            var container = path.cargoContainer;
            result.ReasonCode = "cargo_pool_unavailable";
            if (container.cargoPool is null || container.cursor < 0
                || container.cursor > container.cargoPool.Length) return result;
            var samples = new List<BeltCargoSample>();
            foreach (var reference in references)
            {
                // Cargo IDs are zero-based. Validate before the native indexed read.
                if (reference.CargoId < 0 || reference.CargoId >= container.cursor
                    || reference.CargoId >= container.cargoPool.Length) return result;
                result.ReasonCode = "native_cargo_readback_mismatch";
                if (!path.GetCargoAtIndex(reference.ObservedPathCell, out var cargo,
                        out var nativeId, out _) || nativeId != reference.CargoId
                    || cargo.item <= 0 || LDB.items.Select(cargo.item) is null) return result;
                samples.Add(new BeltCargoSample
                {
                    CargoId = nativeId, ItemId = cargo.item, StackCount = cargo.stack, Inc = cargo.inc,
                });
            }
            if (!BeltCargoObservationPolicy.TrySummarize(references, samples, out var items, out reason))
            {
                result.ReasonCode = reason;
                return result;
            }
            foreach (var item in items) item.Name = GetItemName(item.ItemId);
            result.Items = items;
            result.CargoStackCount = references.Count;
            result.ItemCount = items.Sum(item => item.Count);
            result.State = "observed";
            result.ReasonCode = null;
            return result;
        }
    }
}
