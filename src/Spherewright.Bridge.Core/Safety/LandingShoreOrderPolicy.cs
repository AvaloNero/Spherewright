namespace Spherewright.Bridge.Core.Safety;

/// <summary>
/// Gate for beginning ordinary stable-Walk verification, not a landing-success decision.
/// Native AbortOrder dequeues without setting targetReached, so transient Walk must not
/// cancel an unfinished shore movement or hide a foreign/cleared order.
/// </summary>
public static class LandingShoreOrderPolicy
{
    public static bool MayVerifyStableWalk(bool hasTrackedOrder, bool hasCurrentOrder,
        bool currentOrderIsExact, bool trackedTargetReached)
    {
        if (currentOrderIsExact && (!hasTrackedOrder || !hasCurrentOrder)) return false;
        if (!hasTrackedOrder) return !hasCurrentOrder && !trackedTargetReached;
        if (hasCurrentOrder && !currentOrderIsExact) return false;
        return trackedTargetReached;
    }
}
