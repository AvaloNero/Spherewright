using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

// Normal production/cargo movement is an observation, NOT a source configuration change.
public static class GovernorSourceBinding
{
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
