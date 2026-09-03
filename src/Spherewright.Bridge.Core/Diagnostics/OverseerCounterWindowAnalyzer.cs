using Spherewright.Contracts.Diagnostics;

namespace Spherewright.Bridge.Core.Diagnostics;

public sealed class OverseerCounterSample
{
    // This key is derived from the protected owned-save identity and is never
    // copied into a public diagnostic snapshot.
    public string ProtectedSaveKey { get; set; } = string.Empty;

    public string SessionId { get; set; } = string.Empty;

    public long GameTick { get; set; }

    public DateTimeOffset CapturedAtUtc { get; set; }

    public long ProducedTotal { get; set; }

    public long ConsumedTotal { get; set; }
}

public sealed class OverseerCounterWindowAnalysis
{
    public OverseerWindowSnapshot Window { get; set; } = new OverseerWindowSnapshot();

    public long ProducedDelta { get; set; }

    public long ConsumedDelta { get; set; }

    public double ActualProductionPerMinute { get; set; }

    public double ActualConsumptionPerMinute { get; set; }

    public double? TheoreticalProductionPerMinute { get; set; }

    public double? Utilization { get; set; }
}

public static class OverseerCounterWindowAnalyzer
{
    public const int GameTicksPerSecond = 60;
    public const long DefaultMaximumAdjacentSampleGapTicks = 600;

    public static OverseerCounterWindowAnalysis Analyze(
        OverseerCounterSample? previous,
        OverseerCounterSample current,
        double? theoreticalProductionPerMinute = null,
        long maximumAdjacentSampleGapTicks = DefaultMaximumAdjacentSampleGapTicks)
    {
        ValidateSample(current, nameof(current));
        if (previous is not null)
        {
            ValidateSample(previous, nameof(previous));
        }

        if (maximumAdjacentSampleGapTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAdjacentSampleGapTicks));
        }

        if (theoreticalProductionPerMinute.HasValue)
        {
            ValidateNonNegativeFinite(theoreticalProductionPerMinute.Value, nameof(theoreticalProductionPerMinute));
        }

        var analysis = new OverseerCounterWindowAnalysis
        {
            TheoreticalProductionPerMinute = theoreticalProductionPerMinute,
            Window = new OverseerWindowSnapshot
            {
                EndGameTick = current.GameTick,
                State = OverseerWindowStates.WarmingUp,
                ResetReason = OverseerWindowResetReasons.InitialSample,
            },
        };
        if (previous is null)
        {
            return analysis;
        }

        analysis.Window.StartGameTick = previous.GameTick;
        analysis.Window.CrossedSessionBoundary = !string.Equals(
            previous.SessionId,
            current.SessionId,
            StringComparison.Ordinal);
        var wallClockElapsedSeconds = Math.Max(
            0d,
            (current.CapturedAtUtc - previous.CapturedAtUtc).TotalSeconds);
        analysis.Window.WallClockElapsedSeconds = wallClockElapsedSeconds;

        if (!string.Equals(previous.ProtectedSaveKey, current.ProtectedSaveKey, StringComparison.Ordinal))
        {
            return Discontinuous(analysis, OverseerWindowResetReasons.OwnedSaveChanged);
        }

        if (current.GameTick < previous.GameTick)
        {
            return Discontinuous(analysis, OverseerWindowResetReasons.GameTickRegressed);
        }

        if (current.ProducedTotal < previous.ProducedTotal
            || current.ConsumedTotal < previous.ConsumedTotal)
        {
            return Discontinuous(analysis, OverseerWindowResetReasons.CounterRegressed);
        }

        var elapsedGameTicks = current.GameTick - previous.GameTick;
        if (elapsedGameTicks == 0)
        {
            return current.ProducedTotal == previous.ProducedTotal
                && current.ConsumedTotal == previous.ConsumedTotal
                    ? Warm(analysis, OverseerWindowResetReasons.SameGameTick)
                    : Discontinuous(analysis, OverseerWindowResetReasons.CounterAdvancedWithoutGameTime);
        }

        if (elapsedGameTicks > maximumAdjacentSampleGapTicks)
        {
            return Discontinuous(analysis, OverseerWindowResetReasons.SampleGapExceeded);
        }

        var elapsedGameSeconds = elapsedGameTicks / (double)GameTicksPerSecond;
        analysis.Window.State = OverseerWindowStates.Ready;
        analysis.Window.ResetReason = null;
        analysis.Window.ElapsedGameTicks = elapsedGameTicks;
        analysis.Window.ElapsedGameSeconds = elapsedGameSeconds;
        analysis.Window.ExcludedNonGameSeconds = Math.Max(0d, wallClockElapsedSeconds - elapsedGameSeconds);
        analysis.ProducedDelta = current.ProducedTotal - previous.ProducedTotal;
        analysis.ConsumedDelta = current.ConsumedTotal - previous.ConsumedTotal;
        analysis.ActualProductionPerMinute = analysis.ProducedDelta * 60d / elapsedGameSeconds;
        analysis.ActualConsumptionPerMinute = analysis.ConsumedDelta * 60d / elapsedGameSeconds;
        if (theoreticalProductionPerMinute > 0d)
        {
            analysis.Utilization = analysis.ActualProductionPerMinute / theoreticalProductionPerMinute.Value;
        }

        return analysis;
    }

    private static OverseerCounterWindowAnalysis Warm(
        OverseerCounterWindowAnalysis analysis,
        string resetReason)
    {
        analysis.Window.State = OverseerWindowStates.WarmingUp;
        analysis.Window.ResetReason = resetReason;
        return analysis;
    }

    private static OverseerCounterWindowAnalysis Discontinuous(
        OverseerCounterWindowAnalysis analysis,
        string resetReason)
    {
        analysis.Window.State = OverseerWindowStates.Discontinuous;
        analysis.Window.ResetReason = resetReason;
        return analysis;
    }

    private static void ValidateSample(OverseerCounterSample sample, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(sample.ProtectedSaveKey)
            || string.IsNullOrWhiteSpace(sample.SessionId)
            || sample.GameTick < 0
            || sample.ProducedTotal < 0
            || sample.ConsumedTotal < 0)
        {
            throw new ArgumentException("A counter sample must have a protected save key, session, non-negative tick, and non-negative counters.", parameterName);
        }
    }

    private static void ValidateNonNegativeFinite(double value, string parameterName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
