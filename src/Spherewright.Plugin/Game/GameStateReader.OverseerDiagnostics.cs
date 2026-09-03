using System.Security.Cryptography;
using System.Text;
using Spherewright.Bridge.Core.Diagnostics;
using Spherewright.Bridge.Core.Logistics;
using Spherewright.Contracts.Diagnostics;
using Spherewright.Contracts.Errors;

namespace Spherewright.Plugin.Game;

internal sealed partial class GameStateReader
{
    private static BridgeError? TryCaptureOverseerDiagnosticLogisticsRoutes(
        IReadOnlyList<PlanetFactory> factories,
        OverseerLogisticsProgressStore progressStore,
        ref long componentScanCount,
        ref long sourceReferenceScanCount,
        out OverseerDiagnosticLogisticsIndex? index)
    {
        index = new OverseerDiagnosticLogisticsIndex(
            progressStore,
            GameMain.gameTick,
            DateTimeOffset.UtcNow);
        foreach (var factory in factories)
        {
            var transport = factory.transport;
            var traffic = factory.cargoTraffic;
            var factorySystem = factory.factorySystem;
            if (transport?.stationPool is null
                || traffic?.beltPool is null
                || factorySystem?.inserterPool is null
                || transport.stationCursor < 1
                || transport.stationCursor > transport.stationPool.Length
                || traffic.beltCursor < 1
                || traffic.beltCursor > traffic.beltPool.Length
                || factorySystem.inserterCursor < 1
                || factorySystem.inserterCursor > factorySystem.inserterPool.Length
                || factory.entityPool is null
                || factory.entityCursor < 1
                || factory.entityCursor > factory.entityPool.Length)
            {
                index = null;
                return NotReady("An owned factory's logistics topology is not ready for production diagnostics.");
            }

            if (!TryConsumeBudget(
                    ref componentScanCount,
                    transport.stationCursor - 1L,
                    MaximumOverseerDirectDiagnosticComponentScanCount))
            {
                index = null;
                return OverseerScopeExceeded();
            }

            for (var stationId = 1; stationId < transport.stationCursor; stationId++)
            {
                var station = transport.stationPool[stationId];
                if (station is null || station.id == 0) continue;
                if (station.id != stationId
                    || station.entityId <= 0
                    || station.entityId >= factory.entityCursor
                    || station.entityId >= factory.entityPool.Length)
                {
                    index = null;
                    return NotReady("An active logistics station does not match its station or entity identity.");
                }

                ref var entity = ref factory.entityPool[station.entityId];
                if (entity.id != station.entityId
                    || entity.stationId != stationId
                    || !LogisticsStationIdentityPolicy.MatchesLocalPlanet(
                        station.isStellar,
                        station.planetId,
                        factory.planetId)
                    || station.idleDroneCount < 0
                    || station.workDroneCount < 0
                    || station.idleShipCount < 0
                    || station.workShipCount < 0
                    || station.workDroneDatas is null
                    || station.workDroneOrders is null
                    || station.workShipDatas is null
                    || station.workShipOrders is null
                    || station.workDroneCount > station.workDroneDatas.Length
                    || station.workDroneCount > station.workDroneOrders.Length
                    || station.workShipCount > station.workShipDatas.Length
                    || station.workShipCount > station.workShipOrders.Length)
                {
                    index = null;
                    return NotReady("An active logistics station has inconsistent identity or fleet counters.");
                }

                var stores = station.storage;
                var slots = station.slots;
                if (stores is null || slots is null)
                {
                    index = null;
                    return NotReady("An active logistics station has no storage or belt-slot array.");
                }

                if (stores.Length > MaximumOverseerStationStorageSlots
                    || !TryConsumeBudget(
                        ref sourceReferenceScanCount,
                        stores.Length
                        + (long)slots.Length
                        + station.workDroneCount
                        + station.workShipCount,
                        MaximumOverseerDirectDiagnosticSourceReferenceScanCount))
                {
                    index = null;
                    return OverseerScopeExceeded();
                }

                var stationTransportError = TryCaptureOverseerDiagnosticStationTransport(
                    factory.planetId,
                    station,
                    out var stationTransport);
                if (stationTransportError is not null)
                {
                    index = null;
                    return stationTransportError;
                }

                var endpoints = new OverseerDiagnosticLogisticsEndpoint[stores.Length];
                for (var storageIndex = 0; storageIndex < stores.Length; storageIndex++)
                {
                    var store = stores[storageIndex];
                    if (store.itemId < 0
                        || store.count < 0
                        || !IsKnownLogisticsMode(store.localLogic)
                        || !IsKnownLogisticsMode(store.remoteLogic)
                        || (store.itemId == 0
                            && (store.count != 0
                                || store.localOrder != 0
                                || store.remoteOrder != 0
                                || store.localLogic != ELogisticStorage.None
                                || store.remoteLogic != ELogisticStorage.None)))
                    {
                        index = null;
                        return NotReady("A logistics station storage slot has inconsistent diagnostic state.");
                    }

                    endpoints[storageIndex] = new OverseerDiagnosticLogisticsEndpoint
                    {
                        PlanetId = factory.planetId,
                        ObjectId = station.entityId,
                        ItemId = store.itemId,
                        Count = store.count,
                        LocalOrder = store.localOrder,
                        RemoteOrder = store.remoteOrder,
                        LocalLogic = store.localLogic,
                        RemoteLogic = store.remoteLogic,
                        LocalCarrierCount = checked(station.idleDroneCount + station.workDroneCount),
                        RemoteCarrierCount = checked(station.idleShipCount + station.workShipCount),
                        StorageIndex = storageIndex,
                        Station = stationTransport!,
                    };
                    if (store.itemId > 0)
                    {
                        index.Endpoints.Add(endpoints[storageIndex]);
                    }
                }

                foreach (var slot in slots)
                {
                    if (slot.dir != IODir.Output || slot.beltId == 0 || slot.storageIdx == 0) continue;
                    var storageIndex = slot.storageIdx - 1;
                    if (slot.beltId < 0
                        || slot.beltId >= traffic.beltCursor
                        || slot.beltId >= traffic.beltPool.Length
                        || storageIndex < 0
                        || storageIndex >= endpoints.Length)
                    {
                        index = null;
                        return NotReady("A logistics output slot references an invalid belt or storage identity.");
                    }

                    ref var belt = ref traffic.beltPool[slot.beltId];
                    var endpoint = endpoints[storageIndex];
                    if (belt.id != slot.beltId
                        || belt.entityId <= 0
                        || belt.entityId >= factory.entityCursor
                        || belt.entityId >= factory.entityPool.Length
                        || endpoint.ItemId <= 0)
                    {
                        index = null;
                        return NotReady("A configured logistics output slot has no valid belt or item identity.");
                    }

                    ref var beltEntity = ref factory.entityPool[belt.entityId];
                    if (beltEntity.id != belt.entityId || beltEntity.beltId != slot.beltId)
                    {
                        index = null;
                        return NotReady("A logistics output belt does not match its entity identity.");
                    }

                    if (!index.TryAddOutputBelt(factory.planetId, belt.entityId, endpoint))
                    {
                        index = null;
                        return NotReady("A logistics output belt is ambiguously bound to multiple station slots.");
                    }
                }

                // Native station input ports draw from the station-wide needs[]
                // array. storageIdx records the last successful pick; it is not
                // a fixed selector for an input belt.
                var supplyEndpointGroups = endpoints
                    .Where(endpoint => endpoint.ItemId > 0
                        && (endpoint.LocalLogic == ELogisticStorage.Supply
                            || endpoint.RemoteLogic == ELogisticStorage.Supply))
                    .GroupBy(endpoint => endpoint.ItemId)
                    .ToList();
                if (supplyEndpointGroups.Count == 0) continue;

                var inputBeltEntities = new HashSet<int>();
                foreach (var slot in slots)
                {
                    if (slot.dir != IODir.Input || slot.beltId == 0) continue;
                    if (slot.beltId < 0
                        || slot.beltId >= traffic.beltCursor
                        || slot.beltId >= traffic.beltPool.Length)
                    {
                        index = null;
                        return NotReady("A logistics input slot references an invalid belt identity.");
                    }

                    ref var belt = ref traffic.beltPool[slot.beltId];
                    if (belt.id != slot.beltId
                        || belt.entityId <= 0
                        || belt.entityId >= factory.entityCursor
                        || belt.entityId >= factory.entityPool.Length)
                    {
                        index = null;
                        return NotReady("A configured logistics input slot has no valid belt identity.");
                    }

                    ref var beltEntity = ref factory.entityPool[belt.entityId];
                    if (beltEntity.id != belt.entityId
                        || beltEntity.beltId != slot.beltId
                        || !inputBeltEntities.Add(belt.entityId))
                    {
                        index = null;
                        return NotReady("A logistics input belt does not match one unique entity identity.");
                    }

                    foreach (var group in supplyEndpointGroups)
                    {
                        var traceError = TryTraceDiagnosticCargoUpstream(
                            factory,
                            belt.entityId,
                            station.entityId,
                            -1,
                            group.Key,
                            null,
                            ref componentScanCount,
                            out var upstreamObjects,
                            out _);
                        if (traceError is not null)
                        {
                            index = null;
                            return traceError;
                        }

                        foreach (var endpoint in group)
                        {
                            endpoint.UpstreamCandidateObjectIds.UnionWith(upstreamObjects!);
                        }
                    }
                }
            }
        }

        return null;
    }

    private static BridgeError? TryCaptureOverseerDiagnosticStationTransport(
        int planetId,
        StationComponent station,
        out OverseerDiagnosticStationTransportSnapshot? snapshot)
    {
        snapshot = new OverseerDiagnosticStationTransportSnapshot
        {
            PlanetId = planetId,
            StationId = station.id,
            GlobalStationId = station.gid,
        };

        for (var index = 0; index < station.workDroneCount; index++)
        {
            var carrier = station.workDroneDatas[index];
            var order = station.workDroneOrders[index];
            if (carrier.itemId < 0
                || carrier.itemCount < 0
                || carrier.inc < 0
                || float.IsNaN(carrier.direction)
                || float.IsInfinity(carrier.direction)
                || float.IsNaN(carrier.maxt)
                || float.IsInfinity(carrier.maxt)
                || float.IsNaN(carrier.t)
                || float.IsInfinity(carrier.t)
                || order.itemId < 0)
            {
                snapshot = null;
                return NotReady("An active logistics drone has invalid item, order, or progress state.");
            }

            if (carrier.itemId == 0 || carrier.endId <= 0)
            {
                continue;
            }

            if ((carrier.direction != -1f && carrier.direction != 1f)
                || carrier.maxt <= 0f
                || (order.itemId > 0
                    && (order.itemId != carrier.itemId
                        || (order.otherStationId > 0 && order.otherStationId != carrier.endId))))
            {
                snapshot = null;
                return NotReady("An active logistics drone does not match its route or native movement state.");
            }

            snapshot.LocalCarriers.Add(new OverseerDiagnosticLocalCarrierSnapshot
            {
                TargetStationId = carrier.endId,
                ItemId = carrier.itemId,
                StateToken = string.Join(
                    ":",
                    planetId,
                    station.id,
                    index,
                    carrier.endId,
                    carrier.gene,
                    FloatBits(carrier.direction),
                    FloatBits(carrier.maxt),
                    FloatBits(carrier.t),
                    carrier.itemId,
                    carrier.itemCount,
                    carrier.inc,
                    order.itemId,
                    order.thisIndex,
                    order.otherIndex,
                    order.thisOrdered,
                    order.otherOrdered),
            });
        }

        for (var index = 0; index < station.workShipCount; index++)
        {
            var carrier = station.workShipDatas[index];
            var order = station.workShipOrders[index];
            if (carrier.itemId < 0
                || carrier.itemCount < 0
                || carrier.inc < 0
                || carrier.stage < -2
                || carrier.stage > 2
                || carrier.shipIndex < 0
                || carrier.shipIndex >= station.workShipDatas.Length
                || float.IsNaN(carrier.t)
                || float.IsInfinity(carrier.t)
                || float.IsNaN(carrier.uSpeed)
                || float.IsInfinity(carrier.uSpeed)
                || float.IsNaN(carrier.warpState)
                || float.IsInfinity(carrier.warpState)
                || double.IsNaN(carrier.uPos.x)
                || double.IsInfinity(carrier.uPos.x)
                || double.IsNaN(carrier.uPos.y)
                || double.IsInfinity(carrier.uPos.y)
                || double.IsNaN(carrier.uPos.z)
                || double.IsInfinity(carrier.uPos.z)
                || order.itemId < 0)
            {
                snapshot = null;
                return NotReady("An active logistics vessel has invalid item, order, or progress state.");
            }

            if (carrier.itemId == 0 || carrier.otherGId <= 0)
            {
                continue;
            }

            if ((carrier.direction != -1 && carrier.direction != 1)
                || (order.itemId > 0
                    && (order.itemId != carrier.itemId
                        || (order.otherStationGId > 0 && order.otherStationGId != carrier.otherGId))))
            {
                snapshot = null;
                return NotReady("An active logistics vessel does not match its route or native movement state.");
            }

            snapshot.RemoteCarriers.Add(new OverseerDiagnosticRemoteCarrierSnapshot
            {
                TargetGlobalStationId = carrier.otherGId,
                ItemId = carrier.itemId,
                StateToken = string.Join(
                    ":",
                    planetId,
                    station.gid,
                    carrier.shipIndex,
                    carrier.otherGId,
                    carrier.gene,
                    carrier.stage,
                    carrier.direction,
                    FloatBits(carrier.t),
                    FloatBits(carrier.uSpeed),
                    FloatBits(carrier.warpState),
                    DoubleBits(carrier.uPos.x),
                    DoubleBits(carrier.uPos.y),
                    DoubleBits(carrier.uPos.z),
                    carrier.itemId,
                    carrier.itemCount,
                    carrier.inc,
                    carrier.warperCnt,
                    order.itemId,
                    order.thisIndex,
                    order.otherIndex,
                    order.thisOrdered,
                    order.otherOrdered),
            });
        }

        return null;
    }

    private static int FloatBits(float value) =>
        BitConverter.ToInt32(BitConverter.GetBytes(value), 0);

    private static long DoubleBits(double value) => BitConverter.DoubleToInt64Bits(value);

    private static BridgeError? TryCaptureOverseerDirectDiagnostics(
        PlanetFactory factory,
        ISet<int> requestedItemIds,
        OverseerWindowSnapshot window,
        OverseerDiagnosticLogisticsIndex logistics,
        ref long componentScanCount,
        ref long sourceReferenceScanCount,
        out OverseerDirectDiagnosticCapture? capture)
    {
        capture = new OverseerDirectDiagnosticCapture();
        var factorySystem = factory.factorySystem;
        var powerSystem = factory.powerSystem;
        var transport = factory.transport;
        if (factorySystem?.assemblerPool is null
            || factorySystem.labPool is null
            || factorySystem.minerPool is null
            || factorySystem.inserterPool is null
            || factorySystem.fractionatorPool is null
            || powerSystem?.consumerPool is null
            || powerSystem.genPool is null
            || transport?.stationPool is null
            || factory.entityPool is null
            || factory.veinPool is null
            || !IsValidPoolCursor(factorySystem.assemblerCursor, factorySystem.assemblerPool.Length)
            || !IsValidPoolCursor(factorySystem.labCursor, factorySystem.labPool.Length)
            || !IsValidPoolCursor(factorySystem.minerCursor, factorySystem.minerPool.Length)
            || !IsValidPoolCursor(factorySystem.inserterCursor, factorySystem.inserterPool.Length)
            || !IsValidPoolCursor(factorySystem.fractionatorCursor, factorySystem.fractionatorPool.Length))
        {
            capture = null;
            return NotReady("An owned factory's direct-diagnostic component pools are not ready.");
        }

        var additionalComponents =
            factorySystem.assemblerCursor - 1L
            + factorySystem.labCursor - 1L
            + factorySystem.minerCursor - 1L
            + factorySystem.fractionatorCursor - 1L
            + powerSystem.genCursor - 1L
            + transport.stationCursor - 1L;
        if (!TryConsumeBudget(
                ref componentScanCount,
                additionalComponents,
                MaximumOverseerDirectDiagnosticComponentScanCount))
        {
            capture = null;
            return OverseerScopeExceeded();
        }

        try
        {
            var error = TryCaptureAssemblerDirectDiagnostics(
                factory,
                logistics,
                capture,
                ref componentScanCount,
                ref sourceReferenceScanCount);
            if (error is not null) return error;

            error = TryCaptureLabDirectDiagnostics(
                factory,
                logistics,
                capture,
                ref componentScanCount,
                ref sourceReferenceScanCount);
            if (error is not null) return error;

            error = TryCaptureMinerDirectDiagnostics(
                factory,
                window,
                capture,
                ref sourceReferenceScanCount);
            if (error is not null) return error;

            CaptureUnsupportedDirectDiagnosticProducers(factory, requestedItemIds, capture);
            return null;
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is ArithmeticException
            || exception is OverflowException)
        {
            capture = null;
            return NotReady("An owned factory's direct-diagnostic inputs are invalid or exceed safe numeric bounds.");
        }
    }

    private static BridgeError? TryCaptureAssemblerDirectDiagnostics(
        PlanetFactory factory,
        OverseerDiagnosticLogisticsIndex logistics,
        OverseerDirectDiagnosticCapture capture,
        ref long topologyScanCount,
        ref long sourceReferenceScanCount)
    {
        var factorySystem = factory.factorySystem;
        for (var assemblerId = 1; assemblerId < factorySystem.assemblerCursor; assemblerId++)
        {
            ref var assembler = ref factorySystem.assemblerPool[assemblerId];
            if (assembler.id == 0 || assembler.recipeId <= 0) continue;
            if (assembler.id != assemblerId
                || assembler.speed <= 0
                || assembler.recipeExecuteData is null
                || LDB.recipes.Select(assembler.recipeId) is null)
            {
                return NotReady("An active configured assembler has invalid direct-diagnostic identity or recipe state.");
            }
            var powerError = TryCaptureDirectDiagnosticPower(
                factory,
                TheoreticalProducerKind.Assembler,
                assemblerId,
                assembler.entityId,
                assembler.pcId,
                out var entity,
                out var networkId,
                out var powerServeRatio);
            if (powerError is not null) return powerError;

            var recipeError = TryValidateDirectDiagnosticRecipe(
                assembler.recipeExecuteData,
                assembler.served,
                assembler.produced,
                ref sourceReferenceScanCount);
            if (recipeError is not null) return recipeError;

            var routeError = TryFindDirectDiagnosticDemandBindings(
                factory,
                assembler.entityId,
                assembler.recipeExecuteData.requires,
                logistics,
                ref topologyScanCount,
                out var demandBindings,
                out var upstreamCandidates);
            if (routeError is not null) return routeError;

            var inputs = CreateDirectDiagnosticInputs(
                factory.planetId,
                assembler.recipeExecuteData,
                assembler.served,
                logistics,
                demandBindings!,
                upstreamCandidates!,
                capture);
            var outputs = new List<ProductionOutputState>(assembler.recipeExecuteData.products.Length);
            for (var index = 0; index < assembler.recipeExecuteData.products.Length; index++)
            {
                outputs.Add(new ProductionOutputState
                {
                    ItemId = assembler.recipeExecuteData.products[index],
                    ItemName = GetItemName(assembler.recipeExecuteData.products[index]),
                    BufferedCount = assembler.produced[index],
                    BufferCapacity = ProductionOutputBufferCapacityCalculator.CalculateAssemblerCapacity(
                        assembler.recipeType == ERecipeType.Smelt,
                        assembler.recipeType == ERecipeType.Assemble,
                        assembler.recipeExecuteData.productCounts[index]),
                });
            }

            foreach (var productId in assembler.recipeExecuteData.products.Distinct())
            {
                capture.AddProducer(productId, new ProductionFaultInput
                {
                    PlanetId = factory.planetId,
                    ObjectId = assembler.entityId,
                    TargetItemId = productId,
                    TargetItemName = GetItemName(productId),
                    ProductionUnitKind = "assembler",
                    ProductionUnitName = $"{GetItemName(entity.protoId)} {assembler.entityId}",
                    ExpectedCycleGameTicks = ProductionOutputBufferCapacityCalculator.CalculateCycleGameTicks(
                        assembler.recipeExecuteData.timeSpend,
                        assembler.speed),
                    ActualProductionStateKnown = false,
                    IsConfigured = true,
                    IsWorking = assembler.replicating,
                    PowerNetworkId = networkId,
                    PowerServeRatio = powerServeRatio,
                    Inputs = inputs,
                    Outputs = outputs,
                });
            }
        }

        return null;
    }

    private static BridgeError? TryCaptureLabDirectDiagnostics(
        PlanetFactory factory,
        OverseerDiagnosticLogisticsIndex logistics,
        OverseerDirectDiagnosticCapture capture,
        ref long topologyScanCount,
        ref long sourceReferenceScanCount)
    {
        var factorySystem = factory.factorySystem;
        for (var labId = 1; labId < factorySystem.labCursor; labId++)
        {
            ref var lab = ref factorySystem.labPool[labId];
            if (lab.id == 0 || !lab.matrixMode) continue;
            if (lab.id != labId
                || lab.researchMode
                || lab.recipeId <= 0
                || lab.speed <= 0
                || lab.speedOverride <= 0
                || lab.recipeExecuteData is null
                || LDB.recipes.Select(lab.recipeId) is null)
            {
                return NotReady("An active matrix lab has invalid direct-diagnostic identity or recipe state.");
            }
            var powerError = TryCaptureDirectDiagnosticPower(
                factory,
                TheoreticalProducerKind.Lab,
                labId,
                lab.entityId,
                lab.pcId,
                out var entity,
                out var networkId,
                out var powerServeRatio);
            if (powerError is not null) return powerError;

            var recipeError = TryValidateDirectDiagnosticRecipe(
                lab.recipeExecuteData,
                lab.served,
                lab.produced,
                ref sourceReferenceScanCount);
            if (recipeError is not null) return recipeError;

            var routeError = TryFindDirectDiagnosticDemandBindings(
                factory,
                lab.entityId,
                lab.recipeExecuteData.requires,
                logistics,
                ref topologyScanCount,
                out var demandBindings,
                out var upstreamCandidates);
            if (routeError is not null) return routeError;

            var inputs = CreateDirectDiagnosticInputs(
                factory.planetId,
                lab.recipeExecuteData,
                lab.served,
                logistics,
                demandBindings!,
                upstreamCandidates!,
                capture);
            var outputs = new List<ProductionOutputState>(lab.recipeExecuteData.products.Length);
            for (var index = 0; index < lab.recipeExecuteData.products.Length; index++)
            {
                outputs.Add(new ProductionOutputState
                {
                    ItemId = lab.recipeExecuteData.products[index],
                    ItemName = GetItemName(lab.recipeExecuteData.products[index]),
                    BufferedCount = lab.produced[index],
                    BufferCapacity = ProductionOutputBufferCapacityCalculator.CalculateMatrixLabCapacity(lab.speedOverride),
                });
            }

            foreach (var productId in lab.recipeExecuteData.products.Distinct())
            {
                capture.AddProducer(productId, new ProductionFaultInput
                {
                    PlanetId = factory.planetId,
                    ObjectId = lab.entityId,
                    TargetItemId = productId,
                    TargetItemName = GetItemName(productId),
                    ProductionUnitKind = "matrix_lab",
                    ProductionUnitName = $"{GetItemName(entity.protoId)} {lab.entityId}",
                    ExpectedCycleGameTicks = ProductionOutputBufferCapacityCalculator.CalculateCycleGameTicks(
                        lab.recipeExecuteData.timeSpend,
                        lab.speed),
                    ActualProductionStateKnown = false,
                    IsConfigured = true,
                    IsWorking = lab.replicating,
                    PowerNetworkId = networkId,
                    PowerServeRatio = powerServeRatio,
                    Inputs = inputs,
                    Outputs = outputs,
                });
            }
        }

        return null;
    }

    private static BridgeError? TryCaptureMinerDirectDiagnostics(
        PlanetFactory factory,
        OverseerWindowSnapshot window,
        OverseerDirectDiagnosticCapture capture,
        ref long sourceReferenceScanCount)
    {
        var factorySystem = factory.factorySystem;
        for (var minerId = 1; minerId < factorySystem.minerCursor; minerId++)
        {
            ref var miner = ref factorySystem.minerPool[minerId];
            if (miner.id == 0) continue;
            if (miner.id != minerId
                || miner.type == EMinerType.None
                || miner.period <= 0
                || miner.speed <= 0
                || miner.productCount < 0)
            {
                return NotReady("An active resource extractor has invalid direct-diagnostic identity or counters.");
            }

            var powerError = TryCaptureDirectDiagnosticPower(
                factory,
                TheoreticalProducerKind.Miner,
                minerId,
                miner.entityId,
                miner.pcId,
                out var entity,
                out var networkId,
                out var powerServeRatio);
            if (powerError is not null) return powerError;

            var productId = 0;
            var resourceStateKnown = false;
            long remainingResourceAmount = 0;
            double sourceMultiplier = 1d;
            switch (miner.type)
            {
                case EMinerType.Vein:
                    resourceStateKnown = true;
                    if (miner.veinCount < 0
                        || (miner.veinCount > 0 && (miner.veins is null || miner.veinCount > miner.veins.Length)))
                    {
                        return NotReady("An active vein miner has an invalid direct-diagnostic source array.");
                    }

                    if (!TryConsumeBudget(
                            ref sourceReferenceScanCount,
                            miner.veinCount,
                            MaximumOverseerDirectDiagnosticSourceReferenceScanCount))
                    {
                        return OverseerScopeExceeded();
                    }

                    productId = miner.productId;
                    sourceMultiplier = miner.veinCount;
                    for (var index = 0; index < miner.veinCount; index++)
                    {
                        var veinError = TryGetTheoreticalVein(factory, miner.veins![index], out var vein);
                        if (veinError is not null) return veinError;
                        if (productId == 0) productId = vein.productId;
                        if (vein.productId != productId || vein.amount < 0)
                        {
                            return NotReady("A vein miner's source nodes do not share one valid product identity.");
                        }

                        remainingResourceAmount = checked(remainingResourceAmount + vein.amount);
                    }
                    break;

                case EMinerType.Oil:
                    if (miner.veins is null || miner.veins.Length == 0)
                    {
                        return NotReady("An active oil extractor has no direct-diagnostic source identity.");
                    }

                    if (!TryConsumeBudget(
                            ref sourceReferenceScanCount,
                            1,
                            MaximumOverseerDirectDiagnosticSourceReferenceScanCount))
                    {
                        return OverseerScopeExceeded();
                    }

                    var oilError = TryGetTheoreticalVein(factory, miner.veins[0], out var oilVein);
                    if (oilError is not null) return oilError;
                    productId = oilVein.productId;
                    remainingResourceAmount = oilVein.amount;
                    sourceMultiplier = oilVein.amount * (double)VeinData.oilSpeedMultiplier;
                    break;

                case EMinerType.Water:
                    productId = factory.planet.waterItemId;
                    break;

                default:
                    return NotReady("An active resource extractor uses an unsupported runtime miner type.");
            }

            if (productId < 0 || (productId > 0 && LDB.items.Select(productId) is null))
            {
                return NotReady("An active resource extractor has an invalid runtime product identity.");
            }

            var faultInput = new ProductionFaultInput
            {
                PlanetId = factory.planetId,
                ObjectId = miner.entityId,
                TargetItemId = productId,
                TargetItemName = productId > 0 ? GetItemName(productId) : string.Empty,
                ProductionUnitKind = "resource_extractor",
                ProductionUnitName = $"{GetItemName(entity.protoId)} {miner.entityId}",
                WindowState = window.State,
                WindowElapsedGameTicks = window.ElapsedGameTicks,
                ExpectedCycleGameTicks = CalculateMinerCycleGameTicks(
                    miner.period,
                    miner.speed,
                    GameMain.history?.miningSpeedScale ?? 0f,
                    sourceMultiplier),
                ActualProductionStateKnown = false,
                IsConfigured = true,
                IsWorking = miner.workstate != EWorkState.Idle,
                PowerNetworkId = networkId,
                PowerServeRatio = powerServeRatio,
                IsResourceExtractor = true,
                ResourceStateKnown = resourceStateKnown,
                RemainingResourceAmount = remainingResourceAmount,
                Outputs = productId > 0
                    ? new[]
                    {
                        new ProductionOutputState
                        {
                            ItemId = productId,
                            ItemName = GetItemName(productId),
                            BufferedCount = miner.productCount,
                            BufferCapacity = ProductionOutputBufferCapacityCalculator.MinerOutputThreshold,
                        },
                    }
                    : Array.Empty<ProductionOutputState>(),
            };

            if (productId > 0)
            {
                capture.AddProducer(productId, faultInput);
            }
            else if (productId == 0 && resourceStateKnown && remainingResourceAmount == 0)
            {
                faultInput.ActualProductionPerMinute = 0d;
                var finding = ProductionFaultClassifier.ClassifyPrimary(faultInput);
                if (finding is not null) capture.InfrastructureFindings.Add(finding);
            }
        }

        return null;
    }

    private static BridgeError? TryCaptureDirectDiagnosticPower(
        PlanetFactory factory,
        TheoreticalProducerKind producerKind,
        int componentId,
        int entityId,
        int consumerId,
        out EntityData entity,
        out int? networkId,
        out double? powerServeRatio)
    {
        entity = default;
        networkId = null;
        powerServeRatio = null;
        var connectionError = TryGetTheoreticalConsumerConnection(
            factory,
            producerKind,
            componentId,
            entityId,
            consumerId,
            out var connected);
        if (connectionError is not null) return connectionError;

        entity = factory.entityPool[entityId];
        ref var consumer = ref factory.powerSystem.consumerPool[consumerId];
        networkId = consumer.networkId;
        if (!connected) return null;
        var ratio = GetPowerServeRatio(factory.powerSystem, consumer.networkId);
        if (!ratio.HasValue
            || double.IsNaN(ratio.Value)
            || double.IsInfinity(ratio.Value)
            || ratio.Value < 0d)
        {
            return NotReady("A production unit's power network has no valid serve ratio.");
        }

        powerServeRatio = ratio.Value;
        return null;
    }

    private static BridgeError? TryValidateDirectDiagnosticRecipe(
        RecipeExecuteData recipe,
        int[]? served,
        int[]? produced,
        ref long sourceReferenceScanCount)
    {
        if (recipe.requires is null
            || recipe.requireCounts is null
            || recipe.products is null
            || recipe.productCounts is null
            || served is null
            || produced is null
            || recipe.timeSpend <= 0
            || recipe.requires.Length != recipe.requireCounts.Length
            || recipe.products.Length != recipe.productCounts.Length
            || served.Length != recipe.requires.Length
            || produced.Length != recipe.products.Length
            || recipe.products.Length == 0)
        {
            return NotReady("A configured production unit has inconsistent direct-diagnostic recipe arrays.");
        }

        if (!TryConsumeBudget(
                ref sourceReferenceScanCount,
                recipe.requires.Length + (long)recipe.products.Length,
                MaximumOverseerDirectDiagnosticSourceReferenceScanCount))
        {
            return OverseerScopeExceeded();
        }

        for (var index = 0; index < recipe.requires.Length; index++)
        {
            if (recipe.requires[index] <= 0
                || recipe.requireCounts[index] <= 0
                || served[index] < 0
                || LDB.items.Select(recipe.requires[index]) is null)
            {
                return NotReady("A configured production unit has an invalid direct-diagnostic input.");
            }
        }

        for (var index = 0; index < recipe.products.Length; index++)
        {
            if (recipe.products[index] <= 0
                || recipe.productCounts[index] <= 0
                || produced[index] < 0
                || LDB.items.Select(recipe.products[index]) is null)
            {
                return NotReady("A configured production unit has an invalid direct-diagnostic output.");
            }
        }

        return null;
    }

    private static IReadOnlyList<ProductionMaterialInput> CreateDirectDiagnosticInputs(
        int planetId,
        RecipeExecuteData recipe,
        IReadOnlyList<int> served,
        OverseerDiagnosticLogisticsIndex logistics,
        IReadOnlyDictionary<int, OverseerDiagnosticLogisticsEndpoint> demandBindings,
        IReadOnlyDictionary<int, HashSet<int>> upstreamCandidates,
        OverseerDirectDiagnosticCapture capture)
    {
        var result = new List<ProductionMaterialInput>(recipe.requires.Length);
        for (var index = 0; index < recipe.requires.Length; index++)
        {
            var itemId = recipe.requires[index];
            var material = new ProductionMaterialInput
            {
                ItemId = itemId,
                ItemName = GetItemName(itemId),
                AvailableCount = served[index],
                RequiredPerCycle = recipe.requireCounts[index],
            };
            if (demandBindings.TryGetValue(itemId, out var demand))
            {
                var supply = logistics.ApplyRouteEvidence(planetId, material, demand);
                if (supply is not null && supply.UpstreamCandidateObjectIds.Count > 0)
                {
                    capture.RegisterUpstreamCandidates(
                        supply.PlanetId,
                        material,
                        supply.UpstreamCandidateObjectIds);
                }
            }

            else if (upstreamCandidates.TryGetValue(itemId, out var candidates))
            {
                capture.RegisterUpstreamCandidates(planetId, material, candidates);
            }

            result.Add(material);
        }

        return result;
    }

    private static BridgeError? TryFindDirectDiagnosticDemandBindings(
        PlanetFactory factory,
        int productionEntityId,
        IReadOnlyList<int> inputItemIds,
        OverseerDiagnosticLogisticsIndex logistics,
        ref long topologyScanCount,
        out Dictionary<int, OverseerDiagnosticLogisticsEndpoint>? demandBindings,
        out Dictionary<int, HashSet<int>>? upstreamCandidates)
    {
        demandBindings = new Dictionary<int, OverseerDiagnosticLogisticsEndpoint>();
        upstreamCandidates = new Dictionary<int, HashSet<int>>();
        var inputItems = new HashSet<int>(inputItemIds);
        var inserterPool = factory.factorySystem.inserterPool;
        if (!TryConsumeBudget(
                ref topologyScanCount,
                factory.factorySystem.inserterCursor - 1L,
                MaximumOverseerDirectDiagnosticComponentScanCount))
        {
            demandBindings = null;
            upstreamCandidates = null;
            return OverseerScopeExceeded();
        }

        for (var inserterId = 1; inserterId < factory.factorySystem.inserterCursor; inserterId++)
        {
            ref var inserter = ref inserterPool[inserterId];
            if (inserter.id == 0 || inserter.insertTarget != productionEntityId) continue;
            if (inserter.id != inserterId
                || inserter.entityId <= 0
                || inserter.entityId >= factory.entityCursor
                || inserter.entityId >= factory.entityPool.Length)
            {
                demandBindings = null;
                upstreamCandidates = null;
                return NotReady("A production unit's input sorter has inconsistent topology identity.");
            }

            ref var inserterEntity = ref factory.entityPool[inserter.entityId];
            if (inserterEntity.id != inserter.entityId || inserterEntity.inserterId != inserterId)
            {
                demandBindings = null;
                upstreamCandidates = null;
                return NotReady("A production unit's input sorter does not match its entity identity.");
            }

            var pickTarget = inserter.pickTarget;
            if (pickTarget <= 0) continue;
            var inserterFilter = inserter.filter;
            if (inserterFilter < 0)
            {
                demandBindings = null;
                upstreamCandidates = null;
                return NotReady("A production unit's input sorter has an invalid item filter.");
            }

            factory.ReadObjectConn(
                inserter.entityId,
                1,
                out var pickConnectionIsOutput,
                out var pickConnectionObjectId,
                out var pickTargetSlot);
            if (pickConnectionIsOutput
                || pickConnectionObjectId != pickTarget
                || pickTargetSlot < 0
                || pickTargetSlot >= 16)
            {
                demandBindings = null;
                upstreamCandidates = null;
                return NotReady("A production unit's input sorter has inconsistent pick-side topology.");
            }

            var candidateItems = (inserterFilter > 0
                    ? inputItems.Where(itemId => itemId == inserterFilter)
                    : inputItems)
                .ToArray();
            foreach (var itemId in candidateItems)
            {
                var traceError = TryTraceDiagnosticCargoUpstream(
                    factory,
                    pickTarget,
                    inserter.entityId,
                    pickTargetSlot,
                    itemId,
                    logistics,
                    ref topologyScanCount,
                    out var visitedObjects,
                    out var demandBinding);
                if (traceError is not null)
                {
                    demandBindings = null;
                    upstreamCandidates = null;
                    return traceError;
                }

                if (demandBinding is not null)
                {
                    demandBindings[itemId] = demandBinding;
                }

                if (!upstreamCandidates.TryGetValue(itemId, out var candidates))
                {
                    candidates = new HashSet<int>();
                    upstreamCandidates.Add(itemId, candidates);
                }

                candidates.UnionWith(visitedObjects!);
            }
        }

        return null;
    }

    private static BridgeError? TryTraceDiagnosticCargoUpstream(
        PlanetFactory factory,
        int startingObjectId,
        int downstreamObjectId,
        int downstreamSlot,
        int itemId,
        OverseerDiagnosticLogisticsIndex? logistics,
        ref long topologyScanCount,
        out HashSet<int>? visitedObjects,
        out OverseerDiagnosticLogisticsEndpoint? demandBinding)
    {
        visitedObjects = new HashSet<int>();
        demandBinding = null;
        var queue = new Queue<OverseerDiagnosticTransitNode>();
        var visitedStates = new HashSet<long>();
        queue.Enqueue(new OverseerDiagnosticTransitNode(
            startingObjectId,
            downstreamObjectId,
            downstreamSlot));
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            var objectId = node.ObjectId;
            if (!visitedStates.Add(node.StateKey)) continue;
            visitedObjects.Add(objectId);
            if (!TryConsumeBudget(
                    ref topologyScanCount,
                    1,
                    MaximumOverseerDirectDiagnosticComponentScanCount))
            {
                visitedObjects = null;
                demandBinding = null;
                return OverseerScopeExceeded();
            }

            if (logistics is not null
                && logistics.TryGetOutputBelt(factory.planetId, objectId, out var endpoint)
                && endpoint.ItemId == itemId)
            {
                demandBinding = endpoint;
            }

            if (objectId <= 0
                || objectId >= factory.entityCursor
                || objectId >= factory.entityPool.Length)
            {
                continue;
            }

            ref var entity = ref factory.entityPool[objectId];
            if (entity.id != objectId || !IsDiagnosticCargoTransit(ref entity)) continue;
            if (entity.inserterId > 0)
            {
                var inserterPool = factory.factorySystem.inserterPool;
                if (entity.inserterId >= factory.factorySystem.inserterCursor
                    || entity.inserterId >= inserterPool.Length)
                {
                    visitedObjects = null;
                    demandBinding = null;
                    return NotReady("A diagnostic cargo path references an invalid sorter component.");
                }

                ref var transitInserter = ref inserterPool[entity.inserterId];
                if (transitInserter.id != entity.inserterId
                    || transitInserter.entityId != objectId)
                {
                    visitedObjects = null;
                    demandBinding = null;
                    return NotReady("A diagnostic cargo path sorter does not match its entity identity.");
                }

                if (transitInserter.filter < 0)
                {
                    visitedObjects = null;
                    demandBinding = null;
                    return NotReady("A diagnostic cargo path sorter has an invalid item filter.");
                }

                if (transitInserter.filter > 0 && transitInserter.filter != itemId)
                {
                    continue;
                }
            }

            if (entity.splitterId > 0)
            {
                var splitterError = TryValidateDiagnosticSplitterOutput(
                    factory,
                    ref entity,
                    node,
                    itemId,
                    out var itemCanReachOutput);
                if (splitterError is not null)
                {
                    visitedObjects = null;
                    demandBinding = null;
                    return splitterError;
                }

                if (!itemCanReachOutput) continue;
            }

            for (var slot = 0; slot < 16; slot++)
            {
                factory.ReadObjectConn(
                    objectId,
                    slot,
                    out var isOutput,
                    out var otherObjectId,
                    out var otherSlot);
                if (!isOutput && otherObjectId > 0)
                {
                    if (otherSlot < 0 || otherSlot >= 16)
                    {
                        visitedObjects = null;
                        demandBinding = null;
                        return NotReady("A diagnostic cargo path has an invalid upstream connection slot.");
                    }

                    queue.Enqueue(new OverseerDiagnosticTransitNode(
                        otherObjectId,
                        objectId,
                        otherSlot));
                }
            }
        }

        return null;
    }

    private static BridgeError? TryValidateDiagnosticSplitterOutput(
        PlanetFactory factory,
        ref EntityData entity,
        OverseerDiagnosticTransitNode node,
        int itemId,
        out bool itemCanReachOutput)
    {
        itemCanReachOutput = false;
        var traffic = factory.cargoTraffic;
        var splitterId = entity.splitterId;
        if (traffic?.splitterPool is null
            || splitterId <= 0
            || splitterId >= traffic.splitterCursor
            || splitterId >= traffic.splitterPool.Length
            || node.DownstreamSlot < 0
            || node.DownstreamSlot > 3
            || node.DownstreamObjectId <= 0
            || node.DownstreamObjectId >= factory.entityCursor
            || node.DownstreamObjectId >= factory.entityPool.Length)
        {
            return NotReady("A diagnostic cargo path references an invalid splitter output identity.");
        }

        ref var splitter = ref traffic.splitterPool[splitterId];
        ref var downstreamEntity = ref factory.entityPool[node.DownstreamObjectId];
        var slotBeltId = splitter.GetSlotBelt(node.DownstreamSlot);
        if (splitter.id != splitterId
            || splitter.entityId != entity.id
            || downstreamEntity.id != node.DownstreamObjectId
            || downstreamEntity.beltId <= 0
            || slotBeltId != downstreamEntity.beltId
            || splitter.outFilter < 0)
        {
            return NotReady("A diagnostic cargo path splitter does not match its entity, belt, or filter identity.");
        }

        var outputMatchCount = 0;
        var isPriorityOutput = false;
        if (splitter.output0 == slotBeltId)
        {
            outputMatchCount++;
            isPriorityOutput = true;
        }

        if (splitter.output1 == slotBeltId) outputMatchCount++;
        if (splitter.output2 == slotBeltId) outputMatchCount++;
        if (splitter.output3 == slotBeltId) outputMatchCount++;
        if (outputMatchCount != 1)
        {
            return NotReady("A diagnostic cargo path does not enter its splitter through one exact output belt.");
        }

        itemCanReachOutput = ProductionSplitterFilterPolicy.AllowsItem(
            splitter.outFilter,
            isPriorityOutput,
            itemId);
        return null;
    }

    private readonly struct OverseerDiagnosticTransitNode
    {
        public OverseerDiagnosticTransitNode(
            int objectId,
            int downstreamObjectId,
            int downstreamSlot)
        {
            ObjectId = objectId;
            DownstreamObjectId = downstreamObjectId;
            DownstreamSlot = downstreamSlot;
        }

        public int ObjectId { get; }

        public int DownstreamObjectId { get; }

        public int DownstreamSlot { get; }

        public long StateKey => ((long)(uint)ObjectId << 5) | (uint)(DownstreamSlot + 1);
    }

    private static bool IsDiagnosticCargoTransit(ref EntityData entity) =>
        entity.beltId > 0
        || entity.splitterId > 0
        || entity.pilerId > 0
        || entity.spraycoaterId > 0
        || entity.inserterId > 0
        || entity.storageId > 0
        || entity.tankId > 0;

    private static long CalculateMinerCycleGameTicks(
        int period,
        int speed,
        float miningSpeedScale,
        double sourceMultiplier)
    {
        var effectiveSpeed = speed * (double)miningSpeedScale * sourceMultiplier;
        if (period <= 0
            || speed <= 0
            || float.IsNaN(miningSpeedScale)
            || float.IsInfinity(miningSpeedScale)
            || miningSpeedScale < 0f
            || double.IsNaN(sourceMultiplier)
            || double.IsInfinity(sourceMultiplier)
            || sourceMultiplier < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceMultiplier));
        }

        return effectiveSpeed > 0d
            ? Math.Max(1L, checked((long)Math.Ceiling(period / effectiveSpeed)))
            : 1L;
    }

    private static void CaptureUnsupportedDirectDiagnosticProducers(
        PlanetFactory factory,
        ISet<int> requestedItemIds,
        OverseerDirectDiagnosticCapture capture)
    {
        var factorySystem = factory.factorySystem;
        for (var fractionatorId = 1; fractionatorId < factorySystem.fractionatorCursor; fractionatorId++)
        {
            ref var fractionator = ref factorySystem.fractionatorPool[fractionatorId];
            if (fractionator.id != fractionatorId || fractionator.productId <= 0) continue;
            if (requestedItemIds.Contains(fractionator.productId))
            {
                capture.AddUnsupportedProducer(fractionator.productId);
            }
        }

        var powerSystem = factory.powerSystem;
        for (var generatorId = 1; generatorId < powerSystem.genCursor; generatorId++)
        {
            ref var generator = ref powerSystem.genPool[generatorId];
            if (generator.id != generatorId || !generator.gamma || generator.productId <= 0) continue;
            if (requestedItemIds.Contains(generator.productId))
            {
                capture.AddUnsupportedProducer(generator.productId);
            }
        }

        var transport = factory.transport;
        for (var stationId = 1; stationId < transport.stationCursor; stationId++)
        {
            var station = transport.stationPool[stationId];
            if (station is null || station.id != stationId || !station.isCollector) continue;
            foreach (var productId in station.collectionIds ?? Array.Empty<int>())
            {
                if (requestedItemIds.Contains(productId)) capture.AddUnsupportedProducer(productId);
            }
        }
    }

    private static void ApplyOverseerDirectDiagnostics(
        OverseerDirectDiagnosticCapture capture,
        IReadOnlyDictionary<int, OverseerDirectDiagnosticCapture> capturesByPlanet,
        OverseerWindowSnapshot window,
        ProductionRateSnapshot rate)
    {
        var producers = capture.GetProducers(rate.ItemId);
        var unsupportedCount = capture.GetUnsupportedProducerCount(rate.ItemId);
        var findings = new List<OverseerFindingSnapshot>();
        if (rate.ActualProductionPerMinute <= 0d)
        {
            foreach (var producer in producers)
            {
                var root = CloneDiagnosticInput(
                    producer,
                    window,
                    rate.ActualProductionPerMinute,
                    actualProductionStateKnown: true);
                var finding = ProductionRootCauseTracer.TracePrimary(
                    root,
                    reference => capturesByPlanet.TryGetValue(reference.PlanetId, out var producerCapture)
                        ? producerCapture.ResolveProducer(reference, window)
                        : null);
                if (finding is not null) findings.Add(finding);
            }
        }

        findings = findings
            .OrderBy(finding => finding.ObjectId)
            .ThenBy(finding => finding.Kind, StringComparer.Ordinal)
            .ToList();
        rate.DirectDiagnosticCoverage = unsupportedCount == 0
            ? OverseerDirectDiagnosticCoverageStates.Complete
            : OverseerDirectDiagnosticCoverageStates.Partial;
        rate.DirectDiagnosedProducerCount = producers.Count;
        rate.DirectProducerCount = checked(producers.Count + unsupportedCount);
        rate.FindingCount = findings.Count;
        rate.FindingsTruncated = findings.Count > MaximumOverseerDirectFindingsPerItem;
        rate.Findings = findings.Take(MaximumOverseerDirectFindingsPerItem).ToList();
    }

    private sealed class OverseerDirectDiagnosticCapture
    {
        private readonly Dictionary<int, List<ProductionFaultInput>> _producers =
            new Dictionary<int, List<ProductionFaultInput>>();
        private readonly Dictionary<int, int> _unsupportedProducerCounts =
            new Dictionary<int, int>();
        private readonly List<OverseerUpstreamCandidateBinding> _upstreamCandidateBindings =
            new List<OverseerUpstreamCandidateBinding>();

        public List<OverseerFindingSnapshot> InfrastructureFindings { get; } =
            new List<OverseerFindingSnapshot>();

        public void AddProducer(int itemId, ProductionFaultInput input)
        {
            if (!_producers.TryGetValue(itemId, out var producers))
            {
                producers = new List<ProductionFaultInput>();
                _producers.Add(itemId, producers);
            }

            producers.Add(input);
        }

        public IReadOnlyList<ProductionFaultInput> GetProducers(int itemId) =>
            _producers.TryGetValue(itemId, out var producers)
                ? producers
                : Array.Empty<ProductionFaultInput>();

        public void AddUnsupportedProducer(int itemId)
        {
            _unsupportedProducerCounts.TryGetValue(itemId, out var count);
            _unsupportedProducerCounts[itemId] = checked(count + 1);
        }

        public int GetUnsupportedProducerCount(int itemId) =>
            _unsupportedProducerCounts.TryGetValue(itemId, out var count) ? count : 0;

        public void RegisterUpstreamCandidates(
            int planetId,
            ProductionMaterialInput material,
            IEnumerable<int> candidateObjectIds)
        {
            _upstreamCandidateBindings.Add(new OverseerUpstreamCandidateBinding
            {
                PlanetId = planetId,
                Material = material,
                CandidateObjectIds = new HashSet<int>(candidateObjectIds),
            });
        }

        public void BindUpstreamProducers(
            Func<int, int, IReadOnlyList<ProductionFaultInput>> resolveProducers)
        {
            foreach (var group in _upstreamCandidateBindings.GroupBy(binding => binding.Material))
            {
                var material = group.Key;
                material.UpstreamProducers = group
                    .SelectMany(binding => resolveProducers(binding.PlanetId, material.ItemId)
                        .Where(producer => producer.PlanetId == binding.PlanetId
                            && binding.CandidateObjectIds.Contains(producer.ObjectId)))
                    .GroupBy(producer => $"{producer.PlanetId}:{producer.ObjectId}:{producer.TargetItemId}")
                    .Select(producers => producers.First())
                    .OrderBy(producer => producer.PlanetId)
                    .ThenBy(producer => producer.ObjectId)
                    .Select(producer => new ProductionUpstreamReference
                    {
                        PlanetId = producer.PlanetId,
                        ObjectId = producer.ObjectId,
                        ItemId = producer.TargetItemId,
                    })
                    .ToList();
            }
        }

        public ProductionFaultInput? ResolveProducer(
            ProductionUpstreamReference reference,
            OverseerWindowSnapshot window)
        {
            if (!_producers.TryGetValue(reference.ItemId, out var producers)) return null;
            var producer = producers.FirstOrDefault(candidate =>
                candidate.PlanetId == reference.PlanetId
                && candidate.ObjectId == reference.ObjectId);
            return producer is null
                ? null
                : CloneDiagnosticInput(
                    producer,
                    window,
                    actualProductionPerMinute: 0d,
                    actualProductionStateKnown: false);
        }
    }

    private static ProductionFaultInput CloneDiagnosticInput(
        ProductionFaultInput source,
        OverseerWindowSnapshot window,
        double actualProductionPerMinute,
        bool actualProductionStateKnown)
    {
        return new ProductionFaultInput
        {
            PlanetId = source.PlanetId,
            ObjectId = source.ObjectId,
            TargetItemId = source.TargetItemId,
            TargetItemName = source.TargetItemName,
            ProductionUnitKind = source.ProductionUnitKind,
            ProductionUnitName = source.ProductionUnitName,
            WindowState = window.State,
            WindowElapsedGameTicks = window.ElapsedGameTicks,
            ExpectedCycleGameTicks = source.ExpectedCycleGameTicks,
            ActualProductionPerMinute = actualProductionPerMinute,
            ActualProductionStateKnown = actualProductionStateKnown,
            IsConfigured = source.IsConfigured,
            IsWorking = source.IsWorking,
            PowerNetworkId = source.PowerNetworkId,
            PowerServeRatio = source.PowerServeRatio,
            IsResourceExtractor = source.IsResourceExtractor,
            ResourceStateKnown = source.ResourceStateKnown,
            RemainingResourceAmount = source.RemainingResourceAmount,
            Inputs = source.Inputs,
            Outputs = source.Outputs,
        };
    }

    private sealed class OverseerUpstreamCandidateBinding
    {
        public int PlanetId { get; set; }

        public ProductionMaterialInput Material { get; set; } = new ProductionMaterialInput();

        public HashSet<int> CandidateObjectIds { get; set; } = new HashSet<int>();
    }

    private sealed class OverseerDiagnosticLogisticsIndex
    {
        private readonly OverseerLogisticsProgressStore _progressStore;
        private readonly long _capturedAtGameTick;
        private readonly DateTimeOffset _capturedAtUtc;
        private readonly Dictionary<string, OverseerDiagnosticLogisticsEndpoint> _outputBelts =
            new Dictionary<string, OverseerDiagnosticLogisticsEndpoint>(StringComparer.Ordinal);
        private readonly Dictionary<string, OverseerDiagnosticLogisticsObservation> _progressObservations =
            new Dictionary<string, OverseerDiagnosticLogisticsObservation>(StringComparer.Ordinal);

        public OverseerDiagnosticLogisticsIndex(
            OverseerLogisticsProgressStore progressStore,
            long capturedAtGameTick,
            DateTimeOffset capturedAtUtc)
        {
            _progressStore = progressStore;
            _capturedAtGameTick = capturedAtGameTick;
            _capturedAtUtc = capturedAtUtc;
        }

        public List<OverseerDiagnosticLogisticsEndpoint> Endpoints { get; } =
            new List<OverseerDiagnosticLogisticsEndpoint>();

        public bool TryAddOutputBelt(
            int planetId,
            int beltEntityId,
            OverseerDiagnosticLogisticsEndpoint endpoint) =>
            TryAddOutputBelt(OutputBeltKey(planetId, beltEntityId), endpoint);

        public bool TryGetOutputBelt(
            int planetId,
            int beltEntityId,
            out OverseerDiagnosticLogisticsEndpoint endpoint) =>
            _outputBelts.TryGetValue(OutputBeltKey(planetId, beltEntityId), out endpoint!);

        public OverseerDiagnosticLogisticsEndpoint? ApplyRouteEvidence(
            int consumerPlanetId,
            ProductionMaterialInput material,
            OverseerDiagnosticLogisticsEndpoint demand)
        {
            var candidates = new List<OverseerDiagnosticRouteCandidate>();
            if (demand.LocalLogic == ELogisticStorage.Demand)
            {
                candidates.Add(new OverseerDiagnosticRouteCandidate
                {
                    Remote = false,
                    Supply = Endpoints
                        .Where(endpoint => endpoint.PlanetId == consumerPlanetId
                            && endpoint.ItemId == material.ItemId
                            && endpoint.LocalLogic == ELogisticStorage.Supply)
                        .OrderByDescending(endpoint => endpoint.Count)
                        .ThenBy(endpoint => endpoint.ObjectId)
                        .ToList(),
                });
            }

            if (demand.RemoteLogic == ELogisticStorage.Demand)
            {
                candidates.Add(new OverseerDiagnosticRouteCandidate
                {
                    Remote = true,
                    Supply = Endpoints
                        .Where(endpoint => endpoint.PlanetId != consumerPlanetId
                            && endpoint.ItemId == material.ItemId
                            && endpoint.RemoteLogic == ELogisticStorage.Supply)
                        .OrderByDescending(endpoint => endpoint.Count)
                        .ThenBy(endpoint => endpoint.PlanetId)
                        .ThenBy(endpoint => endpoint.ObjectId)
                        .ToList(),
                });
            }

            var route = candidates
                .OrderByDescending(candidate => candidate.Supply.Count > 0)
                .ThenByDescending(candidate => candidate.Supply.Sum(endpoint => (long)endpoint.Count))
                .ThenBy(candidate => candidate.Remote)
                .FirstOrDefault();
            if (route is null) return null;

            material.LogisticsExpected = true;
            material.LogisticsConfigured = route.Supply.Count > 0;
            material.LogisticsDemandPlanetId = demand.PlanetId;
            material.LogisticsDemandObjectId = demand.ObjectId;
            var demandOrder = route.Remote ? demand.RemoteOrder : demand.LocalOrder;
            material.LogisticsOrderOutstanding = demandOrder > 0;
            if (route.Supply.Count == 0) return null;

            // The public path exposes one supply endpoint. Recursive candidates
            // must come from that same endpoint, never another member of the
            // aggregate inventory/carrier set.
            var primarySupply = route.Supply
                .OrderByDescending(endpoint => endpoint.UpstreamCandidateObjectIds.Count > 0)
                .ThenByDescending(endpoint => endpoint.Count)
                .ThenBy(endpoint => endpoint.PlanetId)
                .ThenBy(endpoint => endpoint.ObjectId)
                .First();
            material.LogisticsSupplyPlanetId = primarySupply.PlanetId;
            material.LogisticsSupplyObjectId = primarySupply.ObjectId;
            material.SourceInventoryKnown = true;
            material.SourceInventoryCount = route.Supply.Sum(endpoint => (long)endpoint.Count);
            material.LogisticsCarrierStateKnown = true;
            material.LogisticsCarrierCount = route.Supply
                .Append(demand)
                .GroupBy(endpoint => $"{endpoint.PlanetId}:{endpoint.ObjectId}", StringComparer.Ordinal)
                .Select(group => group.First())
                .Sum(endpoint => route.Remote ? endpoint.RemoteCarrierCount : endpoint.LocalCarrierCount);
            var carrierStates = CaptureRouteCarrierStates(route.Remote, demand, route.Supply);
            material.LogisticsActiveRouteCarrierCount = carrierStates.Count;
            var routeKey = CreateRouteKey(route.Remote, demand, route.Supply);
            if (!_progressObservations.TryGetValue(routeKey, out var observation))
            {
                var sample = new LogisticsProgressSample
                {
                    RouteKey = routeKey,
                    GameTick = _capturedAtGameTick,
                    CapturedAtUtc = _capturedAtUtc,
                    OrderOutstanding = demandOrder > 0,
                    OutstandingOrderMagnitude = Math.Max(0L, (long)demandOrder),
                    ConsumerInputMissing = material.AvailableCount < material.RequiredPerCycle,
                    DemandInventoryCount = demand.Count,
                    SourceInventoryCount = material.SourceInventoryCount,
                    CarrierFleetCount = material.LogisticsCarrierCount,
                    ActiveRouteCarrierCount = carrierStates.Count,
                    CarrierProgressFingerprint = HashDiagnosticValue(
                        "spherewright-logistics-carriers-v1\n"
                        + string.Join("\n", carrierStates.OrderBy(value => value, StringComparer.Ordinal))),
                };
                observation = new OverseerDiagnosticLogisticsObservation
                {
                    Sample = sample,
                };
                _progressObservations.Add(routeKey, observation);
            }
            else if (material.AvailableCount < material.RequiredPerCycle)
            {
                observation.Sample.ConsumerInputMissing = true;
            }

            observation.Materials.Add(material);
            return primarySupply;
        }

        public void FinalizeProgressEvidence()
        {
            if (_progressObservations.Count == 0)
            {
                return;
            }

            var samples = _progressObservations.Values
                .Select(observation => observation.Sample)
                .OrderBy(sample => sample.RouteKey, StringComparer.Ordinal)
                .ToList();
            if (!_progressStore.TryObserveBatchOnMainThread(samples, out var analyses)
                || analyses is null)
            {
                return;
            }

            foreach (var pair in _progressObservations)
            {
                if (!analyses.TryGetValue(pair.Key, out var progress))
                {
                    continue;
                }

                foreach (var material in pair.Value.Materials)
                {
                    material.LogisticsProgressObserved = progress.ProgressObserved;
                    material.LogisticsProgressStateKnown = progress.ProgressStateKnown;
                    material.LogisticsProgressWindowElapsedGameTicks = progress.Window.ElapsedGameTicks;
                    material.LogisticsProgressCrossedSessionBoundary = progress.Window.CrossedSessionBoundary;
                }
            }
        }

        private static List<string> CaptureRouteCarrierStates(
            bool remote,
            OverseerDiagnosticLogisticsEndpoint demand,
            IReadOnlyList<OverseerDiagnosticLogisticsEndpoint> supplies)
        {
            var stations = supplies
                .Append(demand)
                .Select(endpoint => endpoint.Station)
                .GroupBy(
                    station => $"{station.PlanetId}:{station.StationId}",
                    StringComparer.Ordinal)
                .Select(group => group.First())
                .ToList();
            if (remote)
            {
                var supplyGlobalIds = new HashSet<int>(supplies.Select(endpoint => endpoint.Station.GlobalStationId));
                var result = new List<string>();
                foreach (var station in stations)
                {
                    var ownerIsDemand = station.GlobalStationId == demand.Station.GlobalStationId;
                    var ownerIsSupply = supplyGlobalIds.Contains(station.GlobalStationId);
                    foreach (var carrier in station.RemoteCarriers)
                    {
                        if (carrier.ItemId != demand.ItemId) continue;
                        if ((ownerIsDemand && supplyGlobalIds.Contains(carrier.TargetGlobalStationId))
                            || (ownerIsSupply
                                && carrier.TargetGlobalStationId == demand.Station.GlobalStationId))
                        {
                            result.Add(carrier.StateToken);
                        }
                    }
                }

                return result;
            }

            var supplyStationIds = new HashSet<int>(supplies.Select(endpoint => endpoint.Station.StationId));
            var local = new List<string>();
            foreach (var station in stations)
            {
                var ownerIsDemand = station.StationId == demand.Station.StationId;
                var ownerIsSupply = supplyStationIds.Contains(station.StationId);
                foreach (var carrier in station.LocalCarriers)
                {
                    if (carrier.ItemId != demand.ItemId) continue;
                    if ((ownerIsDemand && supplyStationIds.Contains(carrier.TargetStationId))
                        || (ownerIsSupply && carrier.TargetStationId == demand.Station.StationId))
                    {
                        local.Add(carrier.StateToken);
                    }
                }
            }

            return local;
        }

        private static string CreateRouteKey(
            bool remote,
            OverseerDiagnosticLogisticsEndpoint demand,
            IEnumerable<OverseerDiagnosticLogisticsEndpoint> supplies)
        {
            var supplyIdentity = supplies
                .Select(endpoint => $"{endpoint.PlanetId}:{endpoint.ObjectId}:{endpoint.StorageIndex}")
                .OrderBy(value => value, StringComparer.Ordinal);
            var canonical = string.Join(
                "|",
                remote ? "remote" : "local",
                demand.ItemId,
                demand.PlanetId,
                demand.ObjectId,
                demand.StorageIndex,
                string.Join(",", supplyIdentity));
            return HashDiagnosticValue("spherewright-logistics-route-v1\n" + canonical);
        }

        private static string HashDiagnosticValue(string value)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
                return "sha256:" + BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static string OutputBeltKey(int planetId, int beltEntityId) =>
            $"{planetId}:{beltEntityId}";

        private bool TryAddOutputBelt(string key, OverseerDiagnosticLogisticsEndpoint endpoint)
        {
            if (_outputBelts.ContainsKey(key)) return false;
            _outputBelts.Add(key, endpoint);
            return true;
        }
    }

    private sealed class OverseerDiagnosticLogisticsEndpoint
    {
        public int PlanetId { get; set; }

        public int ObjectId { get; set; }

        public int ItemId { get; set; }

        public int Count { get; set; }

        public int LocalOrder { get; set; }

        public int RemoteOrder { get; set; }

        public ELogisticStorage LocalLogic { get; set; }

        public ELogisticStorage RemoteLogic { get; set; }

        public int LocalCarrierCount { get; set; }

        public int RemoteCarrierCount { get; set; }

        public int StorageIndex { get; set; }

        public OverseerDiagnosticStationTransportSnapshot Station { get; set; } =
            new OverseerDiagnosticStationTransportSnapshot();

        public HashSet<int> UpstreamCandidateObjectIds { get; } = new HashSet<int>();
    }

    private sealed class OverseerDiagnosticRouteCandidate
    {
        public bool Remote { get; set; }

        public List<OverseerDiagnosticLogisticsEndpoint> Supply { get; set; } =
            new List<OverseerDiagnosticLogisticsEndpoint>();
    }

    private sealed class OverseerDiagnosticLogisticsObservation
    {
        public LogisticsProgressSample Sample { get; set; } = new LogisticsProgressSample();

        public List<ProductionMaterialInput> Materials { get; } =
            new List<ProductionMaterialInput>();
    }

    private sealed class OverseerDiagnosticStationTransportSnapshot
    {
        public int PlanetId { get; set; }

        public int StationId { get; set; }

        public int GlobalStationId { get; set; }

        public List<OverseerDiagnosticLocalCarrierSnapshot> LocalCarriers { get; } =
            new List<OverseerDiagnosticLocalCarrierSnapshot>();

        public List<OverseerDiagnosticRemoteCarrierSnapshot> RemoteCarriers { get; } =
            new List<OverseerDiagnosticRemoteCarrierSnapshot>();
    }

    private sealed class OverseerDiagnosticLocalCarrierSnapshot
    {
        public int TargetStationId { get; set; }

        public int ItemId { get; set; }

        public string StateToken { get; set; } = string.Empty;
    }

    private sealed class OverseerDiagnosticRemoteCarrierSnapshot
    {
        public int TargetGlobalStationId { get; set; }

        public int ItemId { get; set; }

        public string StateToken { get; set; } = string.Empty;
    }
}
