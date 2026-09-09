using Spherewright.Bridge.Core.Diagnostics;
using Spherewright.Bridge.Core.Factory;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Factory;
using Spherewright.Contracts.Sessions;

namespace Spherewright.Plugin.Game;

internal sealed partial class GameStateReader
{
    private string? _governorSession;
    private readonly Dictionary<string, GovernorMeasurementSeries> _governorSeries = new Dictionary<string, GovernorMeasurementSeries>();
    private readonly Dictionary<string, GovernorThroughputValidation> _governorValidationCandidates = new Dictionary<string, GovernorThroughputValidation>();
    private readonly Dictionary<string, GovernorThroughputValidation> _governorValidations = new Dictionary<string, GovernorThroughputValidation>();

    public GameCallResult<GovernorPlanSnapshot> GetGovernorPlanOnMainThread(string? sessionId, GetGovernorPlanRequest request)
    {
        var access = ValidateOwnedPlanetOnMainThread(sessionId, request.PlanetId, out var factory);
        if (access is not null) return GameCallResult<GovernorPlanSnapshot>.Failed(access);
        try
        {
            GovernorPlanCompiler.ValidateRequest(request);
            if (_governorSession != sessionId)
            {
                _governorSession = sessionId; _governorSeries.Clear();
                _governorValidationCandidates.Clear(); _governorValidations.Clear();
            }
            var source = new List<FactoryEntitySnapshot>();
            foreach (var selected in request.SourceEntities)
            {
                var entity = TryCaptureFactoryEntity(factory!, selected.ObjectId);
                if (entity is null || entity.EndpointStateHash != selected.ExpectedEndpointStateHash || entity.RecipeId != selected.ExpectedRecipeId)
                    return GameCallResult<GovernorPlanSnapshot>.Failed(BridgeError.Create(BridgeErrorCodes.StaleState,
                        "A selected Governor source/endpoint/recipe changed.", false, "Fresh inspect the explicit source selection before recalculating."));
                source.Add(entity);
            }
            var recipes = GetRecipeCatalogOnMainThread(sessionId, new LocalPlanetRequest { PlanetId = request.PlanetId });
            if (!recipes.Success) return GameCallResult<GovernorPlanSnapshot>.Failed(recipes.Error!);
            var buildings = GetBuildCatalogOnMainThread(sessionId);
            if (!buildings.Success) return GameCallResult<GovernorPlanSnapshot>.Failed(buildings.Error!);
            var scale = FoundryPlanCompiler.Compile(new GetFoundryPlanRequest { PlanetId = request.PlanetId,
                TargetItemId = request.TargetItemId, TargetRatePerMinute = request.TargetRatePerMinute,
                ExternalSupplyItemIds = request.ExternalSupplyItemIds, RecipeChoices = request.RecipeChoices }, recipes.Value!, buildings.Value!.Buildings);
            var itemIds = scale.Stages.SelectMany(s => s.Inputs).Select(i => i.ItemId).Append(request.TargetItemId).Distinct().OrderBy(i => i).ToArray();
            if (itemIds.Length > MaximumOverseerItemCount) throw new FoundryPlanningException("governor_item_limit", "Selected scale exceeds64 observed inputs.");
            // Reuse the existing same-tick captured bundle, without paging or mixing factories.
            var error = TryCaptureOverseerDiagnosticBundle(GameMain.data, itemIds, out var entries);
            if (error is not null) return GameCallResult<GovernorPlanSnapshot>.Failed(error);
            var measured = entries.Single(e => e.Planet.PlanetId == request.PlanetId).Planet;
            // Diagnostic findings stay on the existing fresh600-tick path. Only
            // Governor's explicitly declared production window changes; no new
            // general Overseer surface or native sampling/execution loop.
            if (request.MeasurementGameTicks == 3600)
            {
                var counterError = TryApplyGovernorMinuteCounters(factory!, measured);
                if (counterError is not null) return GameCallResult<GovernorPlanSnapshot>.Failed(counterError);
            }
            var sourceHash = GovernorSourceBinding.Create(source);
            var key = CanonicalStateHash.Combine("governor-series-v1", sessionId, request.PlanetId, request.TargetItemId,
                CanonicalStateHash.Combine("selected-ids", source.Select(e => e.ObjectId).OrderBy(id => id).Cast<object>().ToArray()));
            if (!_governorSeries.TryGetValue(key, out var series))
            {
                if (_governorSeries.Count >= 8) throw new FoundryPlanningException("governor_series_limit", "At most8 source-bound observation series per session; no history is silently evicted.");
                series = new GovernorMeasurementSeries(); _governorSeries.Add(key, series);
            }
            var window = NativeProductionRateCalculator.Calculate(GameMain.gameTick, 0, 0, request.MeasurementGameTicks).Window;
            var stocks = itemIds.ToDictionary(id => id, id => source.SelectMany(e => e.Buffers)
                .Where(b => b.ItemId == id && FactoryBufferSemantics.IsItemCount(b)).Sum(b => (long)b.Count));
            var actual = measured.Production.Single(p => p.ItemId == request.TargetItemId).ActualProductionPerMinute;
            if (double.IsNaN(actual) || double.IsInfinity(actual) || actual < 0 || actual > 1000000000)
                throw new FoundryPlanningException("governor_invalid_rate", "Native target measurement is not bounded.");
            series.Observe(GovernorSourceBinding.CreateMeasurementBinding(key, sourceHash, itemIds, request.MeasurementGameTicks),
                window, (decimal)actual, stocks, request.MeasurementGameTicks, GameMain.gameTick);
            BlueprintInspection? copy = null;
            if (source.All(e => BoundedBlueprintReader.SupportsItem(e.ItemId)))
            {
                var exported = ExportBlueprintOnMainThread(sessionId, new ExportBlueprintRequest { PlanetId = request.PlanetId, Entities = request.SourceEntities });
                if (exported.Success) copy = exported.Value;
            }
            var result = GovernorPlanCompiler.Compile(request, recipes.Value!, buildings.Value.Buildings, source,
                measured, window, series, sourceHash, _sessions.CaptureOnMainThread().Revision, copy);
            foreach (var entity in source)
            {
                if (entity.ComponentKind == "inserter" && !BuildingUpgradePolicy.HasCompleteInserterConnections(entity))
                    result.Blockers.Add("selected_sorter_missing_required_factory_edge:" + entity.ObjectId);
                foreach (var edge in entity.Connections)
                {
                    factory!.ReadObjectConn(edge.OtherObjectId, edge.OtherSlot, out var output, out var back, out var slot);
                    if (output == edge.IsOutput || back != entity.ObjectId || slot != edge.Slot)
                        result.Blockers.Add("selected_reciprocal_connection_unproven:" + entity.ObjectId + ":" + edge.Slot);
                }
            }
            if (request.ParallelExpansionBlueprint is not null)
            {
                var parallelRequest = GovernorPlanCompiler.CreateParallelConstructionRequest(request, result);
                if (parallelRequest is null)
                    result.RemainingChecks.Add("The explicit parallel layout is not assessed until a stable nonzero baseline leaves a positive target-rate increment. Do not use the current starved rate instead.");
                else
                {
                    var parallel = GetFoundryPlanOnMainThread(sessionId, parallelRequest);
                    if (!parallel.Success || parallel.Value is null) return GameCallResult<GovernorPlanSnapshot>.Failed(parallel.Error!);
                    GovernorPlanCompiler.AttachParallelConstruction(request, result, parallel.Value, recipes.Value!, buildings.Value.Buildings);
                }
            }
            result.ProposalHash = GovernorPlanCompiler.Fingerprint(result); // Includes final live reciprocal blockers.
            var healthyWrites = _sessions.CaptureOnMainThread().WriteHealth == WriteHealthStates.Healthy;
            if (request.ValidationBaselineProposalHash is string baselineHash)
            {
                if (!_governorValidations.TryGetValue(baselineHash, out var validation))
                {
                    if (_governorValidations.Count >= 8)
                        throw new FoundryPlanningException("governor_validation_limit", "At most8 finite validation declarations per session; no declarations are silently evicted.");
                    validation = _governorValidationCandidates.Values.SingleOrDefault(v => v.Snapshot().BaselineProposalHash == baselineHash);
                    if (validation is not null)
                    {
                        if (!validation.TryBeginDurably(result, healthyWrites, _governorDeclarationStore.TryPut))
                        {
                            // The failed lock has been cleared. Also discard this candidate;
                            // the caller must fresh-read rather than rely on an unpersisted hash.
                            foreach (var candidateKey in _governorValidationCandidates.Where(p => ReferenceEquals(p.Value, validation)).Select(p => p.Key).ToArray())
                                _governorValidationCandidates.Remove(candidateKey);
                            throw new FoundryPlanningException("governor_validation_persistence_unavailable",
                                "The declaration was not durably locked; do not expand or retry an old candidate.");
                        }
                    }
                    else validation = _governorDeclarationStore.ReadForProtectedResume(baselineHash, result.CapturedAtGameTick);
                    if (validation is null)
                        throw new FoundryPlanningException("governor_validation_baseline_unavailable",
                            "No retained candidate or protected saved declaration matches this hash; caller-supplied history is never accepted.");
                    _governorValidations.Add(baselineHash, validation);
                }
                result.ThroughputValidation = validation.Observe(result, healthyWrites);
                result.ThroughputValidation.DeclarationDurable = true; // Samples themselves remain non-durable.
                result.ValidationBaselineAvailable = true;
            }
            else if (healthyWrites && result.Baseline.State == "ready" && result.Blockers.Count == 0
                && result.TargetChainFindings.Count == 0 && !result.FindingsTruncated)
            {
                // At most one latest candidate for each of the existing8 bounded source series.
                // A locked declaration is held separately and is never replaced by later data.
                _governorValidationCandidates[key] = new GovernorThroughputValidation(result);
                result.ValidationBaselineAvailable = true;
            }
            return GameCallResult<GovernorPlanSnapshot>.Succeeded(result);
        }
        catch (FoundryPlanningException exception)
        {
            return GameCallResult<GovernorPlanSnapshot>.Failed(BridgeError.Create(BridgeErrorCodes.InvalidRequest,
                "Governor " + exception.Reason + ": " + exception.Message, false,
                "Fresh-read a bounded explicit source selection, runtime catalogs and nonzero Overseer windows; proposals never grant write authority."));
        }
    }

    private static BridgeError? TryApplyGovernorMinuteCounters(PlanetFactory factory,
        Spherewright.Contracts.Diagnostics.OverseerDiagnosticBundlePlanetSnapshot measured)
    {
        var stats = GameMain.data.statistics?.production?.factoryStatPool;
        if (stats is null || factory.index < 0 || factory.index >= stats.Length
            || stats[factory.index] is not { } stat || stat.productIndices is null || stat.productPool is null
            || stat.productCursor < 1 || stat.productCursor > stat.productPool.Length
            || measured.Production.Count > MaximumOverseerItemCount)
            return NotReady("Governor native minute statistics are unavailable.");
        foreach (var row in measured.Production)
        {
            if (row.ItemId <= 0 || row.ItemId >= stat.productIndices.Length)
                return NotReady("Governor native minute item index is invalid.");
            var index = stat.productIndices[row.ItemId];
            if (index < 0 || index >= stat.productCursor)
                return NotReady("Governor native minute product index is inconsistent.");
            long produced = 0, consumed = 0;
            if (index > 0)
            {
                var product = stat.productPool[index];
                if (product is null) return NotReady("Governor native minute product is unavailable.");
                try
                {
                    (produced, consumed) = NativeMinuteProductionCounters.Read(row.ItemId, product.itemId,
                        product.count, product.cursor, product.total);
                }
                catch (ArgumentException)
                { return NotReady("Governor native minute product identity, rings or totals are inconsistent."); }
            }
            var rate = NativeProductionRateCalculator.Calculate(measured.CapturedAtGameTick, produced, consumed, 3600);
            row.ProducedCount = produced; row.ConsumedCount = consumed;
            row.ActualProductionPerMinute = rate.ActualProductionPerMinute;
            row.ActualConsumptionPerMinute = rate.ActualConsumptionPerMinute;
            row.RateSource = "native_factory_statistics_level_1";
            row.Utilization = row.TheoreticalProductionPerMinute.HasValue
                ? OverseerTheoreticalProductionCalculator.CalculateUtilization(rate.Window.State,
                    rate.ActualProductionPerMinute, row.TheoreticalProductionPerMinute.Value) : null;
        }
        return null;
    }
}
