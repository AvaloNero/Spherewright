using Spherewright.Bridge.Core.Diagnostics;
using Spherewright.Bridge.Core.Logistics;
using Spherewright.Contracts.Diagnostics;
using Spherewright.Contracts.Errors;

namespace Spherewright.Plugin.Game;

internal sealed partial class GameStateReader
{
    private static BridgeError? TryCaptureOverseerDiagnosticLogisticsRoutes(
        IReadOnlyList<PlanetFactory> factories,
        ref long componentScanCount,
        ref long sourceReferenceScanCount,
        out OverseerDiagnosticLogisticsIndex? index)
    {
        index = new OverseerDiagnosticLogisticsIndex();
        foreach (var factory in factories)
        {
            var transport = factory.transport;
            var traffic = factory.cargoTraffic;
            if (transport?.stationPool is null
                || traffic?.beltPool is null
                || transport.stationCursor < 1
                || transport.stationCursor > transport.stationPool.Length
                || traffic.beltCursor < 1
                || traffic.beltCursor > traffic.beltPool.Length
                || factory.entityPool is null)
            {
                index = null;
                return NotReady("An owned factory's logistics topology is not ready for direct production diagnostics.");
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
                    || station.workShipCount < 0)
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
                        stores.Length + (long)slots.Length,
                        MaximumOverseerDirectDiagnosticSourceReferenceScanCount))
                {
                    index = null;
                    return OverseerScopeExceeded();
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
            }
        }

        return null;
    }

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
                requestedItemIds,
                logistics,
                capture,
                ref componentScanCount,
                ref sourceReferenceScanCount);
            if (error is not null) return error;

            error = TryCaptureLabDirectDiagnostics(
                factory,
                requestedItemIds,
                logistics,
                capture,
                ref componentScanCount,
                ref sourceReferenceScanCount);
            if (error is not null) return error;

            error = TryCaptureMinerDirectDiagnostics(
                factory,
                requestedItemIds,
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
        ISet<int> requestedItemIds,
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


            if (!assembler.recipeExecuteData.products.Any(requestedItemIds.Contains)) continue;

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
                out var demandBindings);
            if (routeError is not null) return routeError;

            var inputs = CreateDirectDiagnosticInputs(
                factory.planetId,
                assembler.recipeExecuteData,
                assembler.served,
                logistics,
                demandBindings!);
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
                if (!requestedItemIds.Contains(productId)) continue;
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
        ISet<int> requestedItemIds,
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


            if (!lab.recipeExecuteData.products.Any(requestedItemIds.Contains)) continue;

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
                out var demandBindings);
            if (routeError is not null) return routeError;

            var inputs = CreateDirectDiagnosticInputs(
                factory.planetId,
                lab.recipeExecuteData,
                lab.served,
                logistics,
                demandBindings!);
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
                if (!requestedItemIds.Contains(productId)) continue;
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
        ISet<int> requestedItemIds,
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

            if (productId > 0 && requestedItemIds.Contains(productId))
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
        IReadOnlyDictionary<int, OverseerDiagnosticLogisticsEndpoint> demandBindings)
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
                logistics.ApplyRouteEvidence(planetId, material, demand);
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
        out Dictionary<int, OverseerDiagnosticLogisticsEndpoint>? bindings)
    {
        bindings = new Dictionary<int, OverseerDiagnosticLogisticsEndpoint>();
        var inputItems = new HashSet<int>(inputItemIds);
        var inserterPool = factory.factorySystem.inserterPool;
        if (!TryConsumeBudget(
                ref topologyScanCount,
                factory.factorySystem.inserterCursor - 1L,
                MaximumOverseerDirectDiagnosticComponentScanCount))
        {
            bindings = null;
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
                bindings = null;
                return NotReady("A production unit's input sorter has inconsistent topology identity.");
            }

            ref var inserterEntity = ref factory.entityPool[inserter.entityId];
            if (inserterEntity.id != inserter.entityId || inserterEntity.inserterId != inserterId)
            {
                bindings = null;
                return NotReady("A production unit's input sorter does not match its entity identity.");
            }

            if (inserter.pickTarget <= 0) continue;
            var queue = new Queue<int>();
            var visited = new HashSet<int>();
            queue.Enqueue(inserter.pickTarget);
            while (queue.Count > 0)
            {
                var objectId = queue.Dequeue();
                if (!visited.Add(objectId)) continue;
                if (!TryConsumeBudget(
                        ref topologyScanCount,
                        1,
                        MaximumOverseerDirectDiagnosticComponentScanCount))
                {
                    bindings = null;
                    return OverseerScopeExceeded();
                }

                if (logistics.TryGetOutputBelt(factory.planetId, objectId, out var endpoint)
                    && inputItems.Contains(endpoint.ItemId)
                    && (inserter.filter <= 0 || inserter.filter == endpoint.ItemId))
                {
                    bindings[endpoint.ItemId] = endpoint;
                }

                if (objectId <= 0
                    || objectId >= factory.entityCursor
                    || objectId >= factory.entityPool.Length)
                {
                    continue;
                }

                ref var entity = ref factory.entityPool[objectId];
                if (entity.id != objectId || !IsDiagnosticCargoTransit(ref entity)) continue;
                for (var slot = 0; slot < 16; slot++)
                {
                    factory.ReadObjectConn(objectId, slot, out var isOutput, out var otherObjectId, out _);
                    if (!isOutput && otherObjectId > 0 && !visited.Contains(otherObjectId))
                    {
                        queue.Enqueue(otherObjectId);
                    }
                }
            }
        }

        return null;
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
                producer.WindowState = window.State;
                producer.WindowElapsedGameTicks = window.ElapsedGameTicks;
                producer.ActualProductionPerMinute = rate.ActualProductionPerMinute;
                var finding = ProductionFaultClassifier.ClassifyPrimary(producer);
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
    }

    private sealed class OverseerDiagnosticLogisticsIndex
    {
        private readonly Dictionary<string, OverseerDiagnosticLogisticsEndpoint> _outputBelts =
            new Dictionary<string, OverseerDiagnosticLogisticsEndpoint>(StringComparer.Ordinal);

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

        public void ApplyRouteEvidence(
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
            if (route is null) return;

            material.LogisticsExpected = true;
            material.LogisticsConfigured = route.Supply.Count > 0;
            material.LogisticsDemandPlanetId = demand.PlanetId;
            material.LogisticsDemandObjectId = demand.ObjectId;
            material.LogisticsOrderOutstanding = route.Remote
                ? demand.RemoteOrder != 0 || route.Supply.Any(endpoint => endpoint.RemoteOrder != 0)
                : demand.LocalOrder != 0 || route.Supply.Any(endpoint => endpoint.LocalOrder != 0);
            if (route.Supply.Count == 0) return;

            var primarySupply = route.Supply[0];
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
            material.LogisticsProgressStateKnown = false;
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
    }

    private sealed class OverseerDiagnosticRouteCandidate
    {
        public bool Remote { get; set; }

        public List<OverseerDiagnosticLogisticsEndpoint> Supply { get; set; } =
            new List<OverseerDiagnosticLogisticsEndpoint>();
    }
}
