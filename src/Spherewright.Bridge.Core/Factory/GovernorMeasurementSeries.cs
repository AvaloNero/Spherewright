using Spherewright.Bridge.Core.Diagnostics;
using Spherewright.Contracts.Diagnostics;
using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

// Bounded observation metadata, not an execution loop. Windows are actual copied
// Overseer measurements; callers cannot supply a claimed measured baseline.
public sealed class GovernorMeasurementSeries
{
    private string _binding = string.Empty;
    private readonly List<(long Start, long End, decimal Rate)> _independent = new List<(long, long, decimal)>();
    private long _lastTick = -1;
    private long _scopeStartTick;
    private int _measurementGameTicks = 600;
    private Dictionary<int, long> _stocks = new Dictionary<int, long>();

    public Dictionary<int, long>? PreviousStocks { get; private set; }
    public long? PreviousStockTick { get; private set; }
    public int MeasurementGameTicks => _measurementGameTicks;

    public void Observe(string binding, OverseerWindowSnapshot window, decimal rate, IReadOnlyDictionary<int, long> stocks,
        int measurementGameTicks = 600, long? capturedAtGameTick = null)
    {
        var step = NativeProductionRateCalculator.SampleStepGameTicks(measurementGameTicks);
        var capture = capturedAtGameTick ?? window.EndGameTick;
        if (string.IsNullOrEmpty(binding) || rate < 0 || stocks.Any(i => i.Key <= 0 || i.Value < 0))
            throw new ArgumentException("A bound non-negative runtime measurement is required.");
        if (_binding != binding || capture < _lastTick || window.CrossedSessionBoundary || _measurementGameTicks != measurementGameTicks)
        {
            _binding = binding; _independent.Clear(); _lastTick = -1; _stocks.Clear();
            _scopeStartTick = capture; _measurementGameTicks = measurementGameTicks;
            PreviousStocks = null; PreviousStockTick = null;
        }
        if (capture > _lastTick)
        {
            PreviousStocks = _lastTick < 0 ? null : new Dictionary<int, long>(_stocks);
            PreviousStockTick = _lastTick < 0 ? (long?)null : _lastTick;
            _stocks = stocks.ToDictionary(i => i.Key, i => i.Value); _lastTick = capture;
        }
        if (window.State != OverseerWindowStates.Ready || !window.StartGameTick.HasValue
            || window.ElapsedGameTicks != measurementGameTicks || window.EndGameTick - window.StartGameTick != measurementGameTicks - 1
            || window.EndGameTick % step != 0 || capture < window.EndGameTick || capture - window.EndGameTick >= step)
        { _independent.Clear(); return; }
        if (window.StartGameTick < _scopeStartTick) return; // Exclude native history predating this binding/session.
        if (_independent.Count > 0)
        {
            var last = _independent[_independent.Count - 1];
            if (window.StartGameTick <= last.End) return; // Overlap is not an independent baseline.
            if (window.StartGameTick - last.End > measurementGameTicks + 1) _independent.Clear();
        }
        _independent.Add((window.StartGameTick.Value, window.EndGameTick, rate));
        if (_independent.Count > 3) _independent.RemoveAt(0);
    }

    public GovernorBaselineSnapshot Baseline(decimal tolerance)
    {
        if (tolerance <= 0 || tolerance > .5m) throw new ArgumentOutOfRangeException(nameof(tolerance));
        var result = new GovernorBaselineSnapshot { IndependentWindowCount = _independent.Count };
        if (_independent.Count == 0) return result;
        result.StartGameTick = _independent[0].Start; result.EndGameTick = _independent[_independent.Count - 1].End;
        result.ProductionPerMinute = _independent.Average(x => x.Rate);
        result.MinimumPerMinute = _independent.Min(x => x.Rate); result.MaximumPerMinute = _independent.Max(x => x.Rate);
        result.State = _independent.Count < 3 ? "warming_up" : result.MinimumPerMinute <= 0 ? "zero_baseline"
            : _independent.All(x => Math.Abs(x.Rate - result.ProductionPerMinute.Value) <= result.ProductionPerMinute * tolerance)
                ? "ready" : "unstable";
        return result;
    }
}
