using Spherewright.Contracts.Diagnostics;

namespace Spherewright.Bridge.Core.Diagnostics;

public sealed class LogisticsProgressSample
{
    // These two keys remain inside current-user-protected persistence and are
    // never copied into a public Overseer response.
    public string ProtectedSaveKey { get; set; } = string.Empty;

    public string RouteKey { get; set; } = string.Empty;

    public string SessionId { get; set; } = string.Empty;

    public long GameTick { get; set; }

    public DateTimeOffset CapturedAtUtc { get; set; }

    public bool OrderOutstanding { get; set; }

    public long OutstandingOrderMagnitude { get; set; }

    public bool ConsumerInputMissing { get; set; }

    public long DemandInventoryCount { get; set; }

    public long SourceInventoryCount { get; set; }

    public int CarrierFleetCount { get; set; }

    public int ActiveRouteCarrierCount { get; set; }

    public string CarrierProgressFingerprint { get; set; } = string.Empty;
}

public sealed class LogisticsProgressWindowState
{
    public string ProtectedSaveKey { get; set; } = string.Empty;

    public string RouteKey { get; set; } = string.Empty;

    public LogisticsProgressSample LastSample { get; set; } = new LogisticsProgressSample();

    public long StagnantSinceGameTick { get; set; }

    public DateTimeOffset StagnantSinceCapturedAtUtc { get; set; }

    public string StagnantSinceSessionId { get; set; } = string.Empty;
}

public sealed class LogisticsProgressWindowAnalysis
{
    public OverseerWindowSnapshot Window { get; set; } = new OverseerWindowSnapshot();

    public bool ProgressObserved { get; set; }

    public bool ProgressStateKnown { get; set; }

    public LogisticsProgressWindowState NextState { get; set; } = new LogisticsProgressWindowState();
}

public static class LogisticsProgressWindowAnalyzer
{
    public const long DefaultMinimumObservationTicks = 600;
    public const long DefaultMaximumAdjacentSampleGapTicks = 3600;

    public static LogisticsProgressWindowAnalysis Analyze(
        LogisticsProgressWindowState? previous,
        LogisticsProgressSample current,
        long minimumObservationTicks = DefaultMinimumObservationTicks,
        long maximumAdjacentSampleGapTicks = DefaultMaximumAdjacentSampleGapTicks)
    {
        ValidateSample(current, nameof(current));
        if (minimumObservationTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumObservationTicks));
        }

        if (maximumAdjacentSampleGapTicks < minimumObservationTicks)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAdjacentSampleGapTicks));
        }

        if (previous is null)
        {
            return Reset(
                current,
                OverseerWindowStates.WarmingUp,
                OverseerWindowResetReasons.InitialSample);
        }

        ValidateState(previous, nameof(previous));
        var last = previous.LastSample;
        if (!string.Equals(previous.ProtectedSaveKey, current.ProtectedSaveKey, StringComparison.Ordinal))
        {
            return Reset(
                current,
                OverseerWindowStates.Discontinuous,
                OverseerWindowResetReasons.OwnedSaveChanged);
        }

        if (!string.Equals(previous.RouteKey, current.RouteKey, StringComparison.Ordinal))
        {
            return Reset(
                current,
                OverseerWindowStates.Discontinuous,
                OverseerWindowResetReasons.LogisticsRouteChanged);
        }

        if (current.GameTick < last.GameTick)
        {
            return Reset(
                current,
                OverseerWindowStates.Discontinuous,
                OverseerWindowResetReasons.GameTickRegressed);
        }

        var adjacentTicks = current.GameTick - last.GameTick;
        if (adjacentTicks == 0 && !ObservableStateEquals(last, current))
        {
            return Reset(
                current,
                OverseerWindowStates.Discontinuous,
                OverseerWindowResetReasons.LogisticsStateAdvancedWithoutGameTime);
        }

        if (adjacentTicks > maximumAdjacentSampleGapTicks)
        {
            return Reset(
                current,
                OverseerWindowStates.Discontinuous,
                OverseerWindowResetReasons.LogisticsSampleGapExceeded);
        }

        if (!IsQualifying(current))
        {
            return Reset(
                current,
                OverseerWindowStates.WarmingUp,
                OverseerWindowResetReasons.LogisticsObservationNotQualifying);
        }

        if (!IsQualifying(last))
        {
            return Reset(
                current,
                OverseerWindowStates.WarmingUp,
                OverseerWindowResetReasons.InitialSample);
        }

        var progressObserved = current.DemandInventoryCount > last.DemandInventoryCount
            || current.OutstandingOrderMagnitude < last.OutstandingOrderMagnitude
            || current.ActiveRouteCarrierCount != last.ActiveRouteCarrierCount
            || !string.Equals(
                current.CarrierProgressFingerprint,
                last.CarrierProgressFingerprint,
                StringComparison.Ordinal);
        if (progressObserved)
        {
            var result = Reset(current, OverseerWindowStates.Ready, resetReason: null);
            result.ProgressObserved = true;
            result.ProgressStateKnown = true;
            result.Window.StartGameTick = last.GameTick;
            PopulateElapsedWindow(result.Window, last.CapturedAtUtc, adjacentTicks, current.CapturedAtUtc);
            result.Window.CrossedSessionBoundary = !string.Equals(
                last.SessionId,
                current.SessionId,
                StringComparison.Ordinal);
            return result;
        }

        var elapsedTicks = current.GameTick - previous.StagnantSinceGameTick;
        var state = elapsedTicks >= minimumObservationTicks
            ? OverseerWindowStates.Ready
            : OverseerWindowStates.WarmingUp;
        var analysis = new LogisticsProgressWindowAnalysis
        {
            ProgressObserved = false,
            ProgressStateKnown = string.Equals(state, OverseerWindowStates.Ready, StringComparison.Ordinal),
            Window = new OverseerWindowSnapshot
            {
                State = state,
                ResetReason = string.Equals(state, OverseerWindowStates.Ready, StringComparison.Ordinal)
                    ? null
                    : OverseerWindowResetReasons.LogisticsWindowNotFull,
                StartGameTick = previous.StagnantSinceGameTick,
                EndGameTick = current.GameTick,
                CrossedSessionBoundary = !string.Equals(
                    previous.StagnantSinceSessionId,
                    current.SessionId,
                    StringComparison.Ordinal),
            },
            NextState = new LogisticsProgressWindowState
            {
                ProtectedSaveKey = current.ProtectedSaveKey,
                RouteKey = current.RouteKey,
                LastSample = Clone(current),
                StagnantSinceGameTick = previous.StagnantSinceGameTick,
                StagnantSinceCapturedAtUtc = previous.StagnantSinceCapturedAtUtc,
                StagnantSinceSessionId = previous.StagnantSinceSessionId,
            },
        };
        PopulateElapsedWindow(
            analysis.Window,
            previous.StagnantSinceCapturedAtUtc,
            elapsedTicks,
            current.CapturedAtUtc);
        return analysis;
    }

    private static LogisticsProgressWindowAnalysis Reset(
        LogisticsProgressSample current,
        string state,
        string? resetReason)
    {
        return new LogisticsProgressWindowAnalysis
        {
            Window = new OverseerWindowSnapshot
            {
                State = state,
                ResetReason = resetReason,
                StartGameTick = current.GameTick,
                EndGameTick = current.GameTick,
            },
            NextState = new LogisticsProgressWindowState
            {
                ProtectedSaveKey = current.ProtectedSaveKey,
                RouteKey = current.RouteKey,
                LastSample = Clone(current),
                StagnantSinceGameTick = current.GameTick,
                StagnantSinceCapturedAtUtc = current.CapturedAtUtc,
                StagnantSinceSessionId = current.SessionId,
            },
        };
    }

    private static void PopulateElapsedWindow(
        OverseerWindowSnapshot window,
        DateTimeOffset startedAtUtc,
        long elapsedGameTicks,
        DateTimeOffset currentAtUtc)
    {
        var elapsedGameSeconds = elapsedGameTicks / (double)OverseerCounterWindowAnalyzer.GameTicksPerSecond;
        var wallClockElapsedSeconds = Math.Max(0d, (currentAtUtc - startedAtUtc).TotalSeconds);
        window.ElapsedGameTicks = elapsedGameTicks;
        window.ElapsedGameSeconds = elapsedGameSeconds;
        window.WallClockElapsedSeconds = wallClockElapsedSeconds;
        window.ExcludedNonGameSeconds = Math.Max(0d, wallClockElapsedSeconds - elapsedGameSeconds);
    }

    private static bool IsQualifying(LogisticsProgressSample sample) =>
        sample.ConsumerInputMissing
        && sample.OrderOutstanding
        && sample.OutstandingOrderMagnitude > 0
        && sample.SourceInventoryCount > 0
        && sample.CarrierFleetCount > 0;

    private static bool ObservableStateEquals(
        LogisticsProgressSample left,
        LogisticsProgressSample right) =>
        left.OrderOutstanding == right.OrderOutstanding
        && left.OutstandingOrderMagnitude == right.OutstandingOrderMagnitude
        && left.ConsumerInputMissing == right.ConsumerInputMissing
        && left.DemandInventoryCount == right.DemandInventoryCount
        && left.SourceInventoryCount == right.SourceInventoryCount
        && left.CarrierFleetCount == right.CarrierFleetCount
        && left.ActiveRouteCarrierCount == right.ActiveRouteCarrierCount
        && string.Equals(
            left.CarrierProgressFingerprint,
            right.CarrierProgressFingerprint,
            StringComparison.Ordinal);

    private static void ValidateState(LogisticsProgressWindowState state, string parameterName)
    {
        if (state.LastSample is null)
        {
            throw new ArgumentException("A logistics progress window requires its last sample.", parameterName);
        }

        ValidateSample(state.LastSample, parameterName);
        if (string.IsNullOrWhiteSpace(state.ProtectedSaveKey)
            || string.IsNullOrWhiteSpace(state.RouteKey)
            || string.IsNullOrWhiteSpace(state.StagnantSinceSessionId)
            || !string.Equals(state.ProtectedSaveKey, state.LastSample.ProtectedSaveKey, StringComparison.Ordinal)
            || !string.Equals(state.RouteKey, state.LastSample.RouteKey, StringComparison.Ordinal)
            || state.StagnantSinceGameTick < 0
            || state.StagnantSinceGameTick > state.LastSample.GameTick)
        {
            throw new ArgumentException("A logistics progress window has an invalid identity or stagnant baseline.", parameterName);
        }
    }

    private static void ValidateSample(LogisticsProgressSample sample, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(sample.ProtectedSaveKey)
            || string.IsNullOrWhiteSpace(sample.RouteKey)
            || string.IsNullOrWhiteSpace(sample.SessionId)
            || string.IsNullOrWhiteSpace(sample.CarrierProgressFingerprint)
            || sample.GameTick < 0
            || sample.OutstandingOrderMagnitude < 0
            || sample.DemandInventoryCount < 0
            || sample.SourceInventoryCount < 0
            || sample.CarrierFleetCount < 0
            || sample.ActiveRouteCarrierCount < 0
            || sample.ActiveRouteCarrierCount > sample.CarrierFleetCount
            || sample.OrderOutstanding != (sample.OutstandingOrderMagnitude > 0))
        {
            throw new ArgumentException("A logistics progress sample has an invalid identity, count, order, or carrier state.", parameterName);
        }
    }

    private static LogisticsProgressSample Clone(LogisticsProgressSample sample)
    {
        return new LogisticsProgressSample
        {
            ProtectedSaveKey = sample.ProtectedSaveKey,
            RouteKey = sample.RouteKey,
            SessionId = sample.SessionId,
            GameTick = sample.GameTick,
            CapturedAtUtc = sample.CapturedAtUtc,
            OrderOutstanding = sample.OrderOutstanding,
            OutstandingOrderMagnitude = sample.OutstandingOrderMagnitude,
            ConsumerInputMissing = sample.ConsumerInputMissing,
            DemandInventoryCount = sample.DemandInventoryCount,
            SourceInventoryCount = sample.SourceInventoryCount,
            CarrierFleetCount = sample.CarrierFleetCount,
            ActiveRouteCarrierCount = sample.ActiveRouteCarrierCount,
            CarrierProgressFingerprint = sample.CarrierProgressFingerprint,
        };
    }
}
