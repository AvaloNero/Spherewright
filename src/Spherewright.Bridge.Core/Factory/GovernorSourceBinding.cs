using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

// Normal production/cargo movement is an observation, NOT a source configuration change.
public static class GovernorSourceBinding
{
    // A proposed rate/machine budget is not an observed world change. Keep the
    // actual source and observed item scope bound; full proposal/scale hashes
    // still protect every declaration and executable construction plan.
    public static string CreateMeasurementBinding(string seriesKey, string sourceHash, IReadOnlyList<int> observedItemIds,
        int measurementGameTicks = 600)
    {
        if (!Diagnostics.NativeProductionRateCalculator.IsSupportedWindow(measurementGameTicks)
            || string.IsNullOrWhiteSpace(seriesKey) || string.IsNullOrWhiteSpace(sourceHash)
            || observedItemIds is null || observedItemIds.Count < 1 || observedItemIds.Count > 64
            || observedItemIds.Any(id => id <= 0) || observedItemIds.Distinct().Count() != observedItemIds.Count)
            throw new FoundryPlanningException("governor_measurement_scope_invalid", "Require a source-bound series and1..64 distinct observed item identities.");
        return CanonicalStateHash.Combine("governor-measurement-binding-v3", seriesKey, sourceHash, measurementGameTicks,
            CanonicalStateHash.Combine("observed-items", observedItemIds.OrderBy(id => id).Cast<object>().ToArray()));
    }

    public static string Create(IReadOnlyList<FactoryEntitySnapshot> source)
    {
        if (source is null || source.Count < 1 || source.Count > 32 || source.Any(e => e is null || e.ObjectId <= 0)
            || source.Select(e => e.ObjectId).Distinct().Count() != source.Count
            || source.Any(e => e.SessionId != source[0].SessionId || e.PlanetId != source[0].PlanetId))
            throw new FoundryPlanningException("governor_source_invalid", "Require1..32 distinct current entities in one owned local capture.");
        return CanonicalStateHash.Combine("governor-source-v2", source.OrderBy(e => e.ObjectId).Select(e => (object)
            CanonicalStateHash.Combine("static-source", CanonicalStateHash.FactoryEndpoint(e), e.RecipeId,
                e.ForceAccelerationMode, e.FilterItemId.GetValueOrDefault(), e.PickTargetObjectId,
                e.InsertTargetObjectId, e.PowerNetworkId)).ToArray());
    }
}
