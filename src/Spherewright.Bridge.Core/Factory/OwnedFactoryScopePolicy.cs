namespace Spherewright.Bridge.Core.Factory;

/// <summary>Bounds detailed reads to verified factories in the current owned star.</summary>
public static class OwnedFactoryScopePolicy
{
    public static OwnedFactoryReadStatus EvaluateCurrentStarRead(
        int requestedPlanetId,
        int targetPlanetId,
        bool targetIsInCurrentStar,
        bool factoryLoaded,
        int targetFactoryIndex,
        int factoryCount,
        int factoryPoolLength,
        int resolvedFactoryIndex,
        int planetFactoryIndex,
        int factoryPlanetId,
        bool factoryReferencesTargetPlanet)
    {
        if (requestedPlanetId <= 0 || targetPlanetId <= 0 || requestedPlanetId != targetPlanetId)
            return OwnedFactoryReadStatus.InvalidPlanetIdentity;
        if (!targetIsInCurrentStar)
            return OwnedFactoryReadStatus.OutsideCurrentStar;
        if (!factoryLoaded)
            return OwnedFactoryReadStatus.FactoryNotLoaded;
        if (factoryCount < 1 || factoryCount > factoryPoolLength
            || targetFactoryIndex < 0 || targetFactoryIndex >= factoryCount)
            return OwnedFactoryReadStatus.FactoryIndexUnavailable;
        if (resolvedFactoryIndex != targetFactoryIndex
            || planetFactoryIndex != targetFactoryIndex
            || factoryPlanetId != targetPlanetId
            || !factoryReferencesTargetPlanet)
            return OwnedFactoryReadStatus.FactoryIdentityMismatch;

        return OwnedFactoryReadStatus.Allowed;
    }

    public static bool IsCurrentLocalActionTarget(int requestedPlanetId, int localPlanetId) =>
        requestedPlanetId > 0 && requestedPlanetId == localPlanetId;
}

public enum OwnedFactoryReadStatus
{
    Allowed,
    InvalidPlanetIdentity,
    OutsideCurrentStar,
    FactoryNotLoaded,
    FactoryIndexUnavailable,
    FactoryIdentityMismatch,
}
