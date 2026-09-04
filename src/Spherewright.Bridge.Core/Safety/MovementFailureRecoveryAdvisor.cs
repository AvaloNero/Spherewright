using Spherewright.Contracts.Actions;

namespace Spherewright.Bridge.Core.Safety;

public sealed class MovementFailureRecoveryAdvice
{
    public string FailureKind { get; set; } = string.Empty;

    public long StalledGameTicks { get; set; }

    public double RemainingDistance { get; set; }

    public bool DoNotRetrySameTarget { get; set; }

    public string RecommendedRecovery { get; set; } = string.Empty;

    public double RecommendedShortMoveDistanceMeters { get; set; }

    public double OrthogonalProbeDistanceMeters { get; set; }

    public int MaximumOrthogonalProbeAttempts { get; set; }
}

public static class MovementFailureRecoveryAdvisor
{
    public const double SingleObstacleMoveDistanceMeters = 5d;
    public const double OrthogonalProbeDistanceMeters = 4d;
    public const int MaximumOrthogonalProbeAttempts = 4;

    public const string RecoverySummary =
        "Fresh-read the player and nearby geometry. If one obstacle is identifiable, prepare and commit one local-tangent target about 5 m away from it. Otherwise try at most four orthogonal local-tangent targets about 4 m away, each direction once. Poll every returned actionId to terminal; after success fresh-read Walk, low speed, and sufficient energy.";

    public static MovementFailureRecoveryAdvice ForStall(MovementProgressObservation observation)
    {
        var failureKind = observation.Status switch
        {
            MovementProgressStatus.PositionStalled => MovementFailureKinds.PositionStalled,
            MovementProgressStatus.RouteStalled => MovementFailureKinds.RouteStalled,
            _ => throw new ArgumentException("A progressing observation has no movement-failure recovery advice.", nameof(observation)),
        };

        return Create(
            failureKind,
            observation.StalledGameTicks,
            observation.RemainingDistance);
    }

    public static MovementFailureRecoveryAdvice ForBoundedTimeout(
        long elapsedGameTicks,
        double remainingDistance) =>
        Create(MovementFailureKinds.BoundedTimeout, elapsedGameTicks, remainingDistance);

    private static MovementFailureRecoveryAdvice Create(
        string failureKind,
        long stalledGameTicks,
        double remainingDistance) => new MovementFailureRecoveryAdvice
        {
            FailureKind = failureKind,
            StalledGameTicks = stalledGameTicks,
            RemainingDistance = remainingDistance,
            DoNotRetrySameTarget = true,
            RecommendedRecovery = RecoverySummary,
            RecommendedShortMoveDistanceMeters = SingleObstacleMoveDistanceMeters,
            OrthogonalProbeDistanceMeters = OrthogonalProbeDistanceMeters,
            MaximumOrthogonalProbeAttempts = MaximumOrthogonalProbeAttempts,
        };
}
