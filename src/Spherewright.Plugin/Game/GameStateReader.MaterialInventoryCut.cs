using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;

namespace Spherewright.Plugin.Game;

internal sealed partial class GameStateReader
{
    private MaterialInventoryCutSnapshot CaptureMaterialInventoryCut(PlanetFactory factory, IReadOnlyList<int> ids)
    {
        var cut = new MaterialInventoryCutSnapshot
        {
            SessionId = _sessions.SessionId!, PlanetId = factory.planetId,
            CapturedAtGameTick = GameMain.gameTick, RequestedObjectIds = ids.ToList(),
            ReasonCode = "material_inventory_selection_incomplete",
        };
        var budget = new MaterialInventoryCutPolicy();
        var paths = new Dictionary<int, MaterialCargoPathSnapshot>();
        foreach (var id in ids)
        {
            var entity = TryCaptureFactoryEntity(factory, id);
            if (entity is null)
            { cut.ReasonCode = "material_inventory_object_unavailable"; return cut; }
            // Do not silently label unsupported/unobserved buffers as empty stock.
            var buffersKnown = entity.ComponentKind switch
            {
                "belt" => true,
                "storage" => entity.StorageConfiguration is not null && StorageStockKnown(factory, id, entity),
                "tank" => entity.TankFluidCount.HasValue,
                "inserter" => entity.InserterStage is not null && InserterStockKnown(factory, id, entity),
                "assembler" => entity.RecipeId > 0 && AssemblerStockKnown(factory, id, entity),
                "miner" => entity.ProgressRequired > 0 && MinerStockKnown(factory, id, entity),
                _ => false,
            };
            if (!buffersKnown)
            { cut.ReasonCode = "material_inventory_buffers_unavailable_or_unsupported"; return cut; }
            if (entity.Buffers.Any(buffer => buffer.ItemId <= 0 || LDB.items.Select(buffer.ItemId) is null
                    || buffer.Count < 0 || buffer.Inc < 0 || buffer.CountUnit != "items" || buffer.UnitsPerItem != 1))
            { cut.ReasonCode = "material_inventory_buffer_identity_or_unit_unavailable"; return cut; }
            cut.Objects.Add(entity);
            if (entity.ComponentKind != "belt") continue;
            if (!TryCaptureMaterialCargoPath(factory, id, budget, paths, out var reason))
            { cut.ReasonCode = reason; return cut; }
            // The dictionary deduplicates paths, NOT separate belt-segment samples.
            cut.CargoPaths = paths.Values.OrderBy(path => path.PathId).ToList();
        }
        if (GameMain.gameTick != cut.CapturedAtGameTick)
        { cut.ReasonCode = "material_inventory_tick_changed"; return cut; }
        cut.State = "observed";
        cut.ReasonCode = null;
        return cut;
    }

    private static bool StorageStockKnown(PlanetFactory factory, int id, FactoryEntitySnapshot captured)
    {
        var storage = factory.factoryStorage.storagePool[factory.entityPool[id].storageId];
        if (storage?.grids is null || storage.size != captured.StorageConfiguration!.GridCount
            || storage.grids.Length < storage.size) return false;
        var observed = 0;
        for (var index = 0; index < storage.size; index++)
        {
            var grid = storage.grids[index];
            // The ordinary reader skips invalid/empty grids. A complete stock cut
            // must distinguish a verified empty grid from an omitted invalid one.
            if (grid.count < 0 || grid.inc < 0) return false;
            if (grid.count == 0) continue;
            if (grid.itemId <= 0 || LDB.items.Select(grid.itemId) is null || observed >= captured.Buffers.Count)
                return false;
            var buffer = captured.Buffers[observed++];
            if (buffer.Role != "storage" || buffer.ItemId != grid.itemId || buffer.Count != grid.count || buffer.Inc != grid.inc)
                return false;
        }
        return observed == captured.Buffers.Count;
    }

    private static bool InserterStockKnown(PlanetFactory factory, int id, FactoryEntitySnapshot captured)
    {
        // stackCount is NOT the number of held items. Check the same native field
        // used by CaptureInserter instead of making an empty-buffer assumption.
        var componentId = factory.entityPool[id].inserterId;
        ref var component = ref factory.factorySystem.inserterPool[componentId];
        return component.itemCount == 0 ? captured.Buffers.Count == 0
            : component.itemCount > 0 && captured.Buffers.Count == 1
                && captured.Buffers[0].ItemId == component.itemId && captured.Buffers[0].Count == component.itemCount;
    }

    private static bool MinerStockKnown(PlanetFactory factory, int id, FactoryEntitySnapshot captured)
    {
        var componentId = factory.entityPool[id].minerId;
        ref var component = ref factory.factorySystem.minerPool[componentId];
        return component.productCount == 0 ? captured.Buffers.All(buffer => buffer.Count == 0)
            : component.productCount > 0 && captured.Buffers.Count == 1
                && captured.Buffers[0].ItemId == component.productId && captured.Buffers[0].Count == component.productCount;
    }

    private static bool AssemblerStockKnown(PlanetFactory factory, int id, FactoryEntitySnapshot captured)
    {
        var componentId = factory.entityPool[id].assemblerId;
        ref var component = ref factory.factorySystem.assemblerPool[componentId];
        var recipe = component.recipeExecuteData;
        return recipe is not null && recipe.requires is not null && recipe.products is not null
            && component.served is not null && component.produced is not null && component.incServed is not null
            && recipe.requires.Length == component.served.Length && recipe.products.Length == component.produced.Length
            && component.incServed.Length == component.served.Length
            && captured.Buffers.Count == recipe.requires.Length + recipe.products.Length
            && captured.Buffers.All(buffer => buffer.Count >= 0 && buffer.ItemId > 0);
    }

    private static bool TryCaptureMaterialCargoPath(PlanetFactory factory, int entityId,
        MaterialInventoryCutPolicy budget, Dictionary<int, MaterialCargoPathSnapshot> captured,
        out string? reason)
    {
        reason = "material_cargo_path_identity_unavailable";
        var traffic = factory.cargoTraffic;
        var beltId = factory.entityPool[entityId].beltId;
        if (traffic?.beltPool is null || beltId <= 0 || beltId >= traffic.beltCursor
            || beltId >= traffic.beltPool.Length) return false;
        var belt = traffic.beltPool[beltId];
        if (belt.id != beltId || belt.entityId != entityId || traffic.pathPool is null
            || belt.segPathId <= 0 || belt.segPathId >= traffic.pathCursor
            || belt.segPathId >= traffic.pathPool.Length) return false;
        var path = traffic.GetCargoPath(belt.segPathId);
        if (path is null || path.id != belt.segPathId || path.buffer is null || path.belts is null
            || !ReferenceEquals(path.cargoContainer, factory.cargoContainer)) return false;
        // Before any full-path allocation; never widen the native adapter's bounds.
        if (!budget.TryReservePath(path.id, path.pathLength, path.belts.Count, out var seen))
        { reason = "material_cargo_path_budget_or_identity_rejected"; return false; }
        if (seen) return captured.ContainsKey(path.id) && captured[path.id].BeltObjectIds.Contains(entityId);

        lock (path.buffer)
        {
            // Reuse the already validated header-only Export probe, managed copies
            // and strict full-path decoder. No new or guessed DSP method.
            if (!NativeBeltPathCapture.TryRead(path, out var decoded, out reason)
                || decoded is null || !BeltUpgradePathPolicy.TryLocateAllCargo(decoded, out var references, out reason))
                return false;
            var members = new HashSet<int>();
            foreach (var memberId in decoded.BeltIds)
            {
                reason = "material_cargo_path_member_identity_unavailable";
                if (memberId <= 0 || memberId >= traffic.beltCursor || memberId >= traffic.beltPool.Length) return false;
                var member = traffic.beltPool[memberId];
                if (member.id != memberId || member.segPathId != decoded.Id || member.entityId <= 0
                    || member.entityId >= factory.entityCursor || member.entityId >= factory.entityPool.Length
                    || factory.entityPool[member.entityId].id != member.entityId
                    || factory.entityPool[member.entityId].beltId != memberId || !members.Add(member.entityId)) return false;
            }
            if (!members.Contains(entityId)) return false;
            var container = path.cargoContainer;
            reason = "material_cargo_pool_unavailable";
            if (container.cargoPool is null || container.cursor < 0 || container.cursor > container.cargoPool.Length) return false;
            var samples = new List<BeltCargoSample>();
            foreach (var reference in references)
            {
                reason = "material_cargo_readback_mismatch";
                if (reference.CargoId < 0 || reference.CargoId >= container.cursor
                    || reference.CargoId >= container.cargoPool.Length
                    || !path.GetCargoAtIndex(reference.ObservedPathCell, out var cargo, out var nativeId, out _)
                    || nativeId != reference.CargoId || cargo.item <= 0 || LDB.items.Select(cargo.item) is null) return false;
                samples.Add(new BeltCargoSample
                { CargoId = nativeId, ItemId = cargo.item, StackCount = cargo.stack, Inc = cargo.inc });
            }
            if (!BeltCargoObservationPolicy.TrySummarizePath(references, samples, out var items, out reason)) return false;
            if (!budget.TryReserveCargo(references))
            { reason = "material_cargo_duplicate_or_invalid_across_paths"; return false; }
            foreach (var item in items) item.Name = GetItemName(item.ItemId);
            captured.Add(decoded.Id, new MaterialCargoPathSnapshot
            {
                PathId = decoded.Id, PathLengthCells = decoded.Length, PathClosed = decoded.Closed,
                OutputPathId = decoded.OutputPathId, InputPathIds = decoded.InputPathIds.ToList(),
                CapturedAtGameTick = GameMain.gameTick, BeltObjectIds = members.OrderBy(id => id).ToList(),
                CargoStackCount = references.Count, ItemCount = items.Sum(item => item.Count), Items = items,
            });
            reason = null;
            return true;
        }
    }
}
