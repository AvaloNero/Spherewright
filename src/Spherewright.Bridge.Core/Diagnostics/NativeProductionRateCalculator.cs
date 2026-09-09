using Spherewright.Contracts.Diagnostics;

namespace Spherewright.Bridge.Core.Diagnostics;

public sealed class NativeProductionRateAnalysis
{
    public OverseerWindowSnapshot Window { get; set; } = new OverseerWindowSnapshot();

    public double ActualProductionPerMinute { get; set; }

    public double ActualConsumptionPerMinute { get; set; }
}

public static class NativeProductionRateCalculator
{
    public const int GameTicksPerSecond = 60;
    public const int NativeWindowGameTicks = 600;

    public static bool IsSupportedWindow(int gameTicks) => gameTicks == 600 || gameTicks == 3600;

    public static int SampleStepGameTicks(int gameTicks) => gameTicks == 600 ? 1 : gameTicks == 3600 ? 6
        : throw new ArgumentOutOfRangeException(nameof(gameTicks));

    public static NativeProductionRateAnalysis Calculate(
        long capturedAtGameTick,
        long producedCount,
        long consumedCount,
        int measurementGameTicks = NativeWindowGameTicks)
    {
        if (capturedAtGameTick < 0 || producedCount < 0 || consumedCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capturedAtGameTick),
                "The game tick and native production counters must be non-negative.");
        }

        var step = SampleStepGameTicks(measurementGameTicks);
        var end = capturedAtGameTick - capturedAtGameTick % step;
        var observedGameTicks = end >= measurementGameTicks - 1
            ? measurementGameTicks
            : end + 1;
        var elapsedGameSeconds = observedGameTicks / (double)GameTicksPerSecond;
        var windowReady = observedGameTicks == measurementGameTicks;
        return new NativeProductionRateAnalysis
        {
            Window = new OverseerWindowSnapshot
            {
                State = windowReady ? OverseerWindowStates.Ready : OverseerWindowStates.WarmingUp,
                ResetReason = windowReady ? null : OverseerWindowResetReasons.NativeWindowNotFull,
                StartGameTick = Math.Max(0, end - measurementGameTicks + 1),
                EndGameTick = end,
                ElapsedGameTicks = observedGameTicks,
                ElapsedGameSeconds = elapsedGameSeconds,
                WallClockElapsedSeconds = 0d,
                ExcludedNonGameSeconds = 0d,
                CrossedSessionBoundary = false,
            },
            ActualProductionPerMinute = producedCount * 60d / elapsedGameSeconds,
            ActualConsumptionPerMinute = consumedCount * 60d / elapsedGameSeconds,
        };
    }
}
