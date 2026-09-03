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

    public static NativeProductionRateAnalysis Calculate(
        long capturedAtGameTick,
        long producedCount,
        long consumedCount)
    {
        if (capturedAtGameTick < 0 || producedCount < 0 || consumedCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capturedAtGameTick),
                "The game tick and native production counters must be non-negative.");
        }

        var observedGameTicks = capturedAtGameTick >= NativeWindowGameTicks - 1
            ? NativeWindowGameTicks
            : capturedAtGameTick + 1;
        var elapsedGameSeconds = observedGameTicks / (double)GameTicksPerSecond;
        var windowReady = observedGameTicks == NativeWindowGameTicks;
        return new NativeProductionRateAnalysis
        {
            Window = new OverseerWindowSnapshot
            {
                State = windowReady ? OverseerWindowStates.Ready : OverseerWindowStates.WarmingUp,
                ResetReason = windowReady ? null : OverseerWindowResetReasons.NativeWindowNotFull,
                StartGameTick = Math.Max(0, capturedAtGameTick - NativeWindowGameTicks + 1),
                EndGameTick = capturedAtGameTick,
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
