using Spherewright.Bridge.Core.Diagnostics;
using Spherewright.Contracts.Diagnostics;
using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

// Finite passive observation, not gameplay execution or an autonomous expansion loop.
// The constructor receives an actual server-retained PRE-execution proposal, never
// a caller's claimed rate/history. Scalar copies prevent later DTO mutation.
public sealed class GovernorThroughputValidation
{
    private readonly string _sessionId;
    private readonly int _planetId;
    private readonly int _targetItemId;
    private readonly string _baselineHash;
    private readonly long _declaredTick;
    private readonly long _declarationRevision;
    private readonly decimal _baselineRate;
    private readonly decimal _targetRate;
    private readonly decimal _tolerance;
    private readonly int _requiredTicks;
    private readonly int _measurementGameTicks;
    private readonly string _scaleHash;
    private string _sourceHash;
    private long _scopeStart;
    private long _lastCapture = -1;
    private long? _start;
    private long? _end;
    private decimal? _minimum;
    private decimal? _maximum;
    private long _count;
    private string? _resetReason;
    private long? _lockedAt;

    public GovernorThroughputValidation(GovernorPlanSnapshot declaration)
    {
        if (declaration is null || string.IsNullOrWhiteSpace(declaration.SessionId)
            || string.IsNullOrWhiteSpace(declaration.ProposalHash) || string.IsNullOrWhiteSpace(declaration.SourceStateHash)
            || declaration.PlanetId <= 0 || declaration.TargetItemId <= 0 || declaration.CapturedAtGameTick < 0 || declaration.Revision < 0
            || declaration.Baseline.State != "ready" || declaration.Baseline.IndependentWindowCount != 3
            || declaration.Baseline.ProductionPerMinute is not decimal baseline || baseline <= 0
            || !declaration.Baseline.EndGameTick.HasValue || declaration.Baseline.EndGameTick > declaration.CapturedAtGameTick
            || declaration.TargetRatePerMinute <= baseline || declaration.TargetRatePerMinute > 1000000
            || declaration.ToleranceFraction <= 0 || declaration.ToleranceFraction > .5m
            || declaration.ValidationGameTicks < 36000 || declaration.ValidationGameTicks > 216000
            || !NativeProductionRateCalculator.IsSupportedWindow(declaration.MeasurementGameTicks)
            || !declaration.SelectionContainsAllTargetProducers || declaration.Blockers.Count != 0
            || !HealthyObservation(declaration, true) || string.IsNullOrWhiteSpace(declaration.FullTargetScale.PlanHash))
            throw new FoundryPlanningException("governor_validation_baseline_invalid",
                "Use a retained attributable, healthy, ready nonzero pre-execution proposal with a higher declared target.");
        _sessionId = declaration.SessionId; _planetId = declaration.PlanetId; _targetItemId = declaration.TargetItemId;
        _baselineHash = declaration.ProposalHash; _declaredTick = declaration.CapturedAtGameTick;
        _declarationRevision = declaration.Revision;
        _baselineRate = baseline; _targetRate = declaration.TargetRatePerMinute;
        _tolerance = declaration.ToleranceFraction; _requiredTicks = declaration.ValidationGameTicks;
        _measurementGameTicks = declaration.MeasurementGameTicks;
        _scaleHash = declaration.FullTargetScale.PlanHash; _sourceHash = declaration.SourceStateHash;
        _scopeStart = _declaredTick;
    }

    public GovernorValidationCheckpoint CreateCheckpoint(string ownedIdentityHash, string gameVersion)
    {
        if (!_lockedAt.HasValue)
            throw new FoundryPlanningException("governor_validation_not_started", "Only an actually locked server declaration can be persisted.");
        var checkpoint = new GovernorValidationCheckpoint
        {
            Version = 3, MeasurementGameTicks = _measurementGameTicks,
            OwnedIdentityHash = ownedIdentityHash, GameVersion = gameVersion, SourceSessionId = _sessionId,
            BaselineProposalHash = _baselineHash, SourceStateHash = _sourceHash, ScalePlanHash = _scaleHash,
            PlanetId = _planetId, TargetItemId = _targetItemId,
            DeclaredAtGameTick = _declaredTick, DeclarationRevision = _declarationRevision,
            LockedAtGameTick = _lockedAt.Value, BaselineRatePerMinute = _baselineRate,
            TargetRatePerMinute = _targetRate, ToleranceFraction = _tolerance, RequiredGameTicks = _requiredTicks,
        };
        checkpoint.IntegrityHash = checkpoint.CalculateIntegrityHash();
        checkpoint.Validate();
        return checkpoint;
    }

    // A current tick/auto-resave cannot prove the loaded save included the declaration.
    // The Plugin supplies the consumed ticket's watermark only after a healthy planned
    // resume and its Journal continuity check, never from a client request.
    public static GovernorThroughputValidation Restore(GovernorValidationCheckpoint checkpoint,
        string ownedIdentityHash, string gameVersion, string sessionId,
        long? confirmedPlannedResumeMinimumGameTick, long currentGameTick)
    {
        if (checkpoint is null)
            throw new FoundryPlanningException("governor_validation_checkpoint_invalid", "No server declaration is available.");
        checkpoint.Validate();
        if (checkpoint.OwnedIdentityHash != ownedIdentityHash || checkpoint.GameVersion != gameVersion
            || string.IsNullOrWhiteSpace(sessionId) || sessionId.Length > 256 || sessionId == checkpoint.SourceSessionId
            || !confirmedPlannedResumeMinimumGameTick.HasValue
            || confirmedPlannedResumeMinimumGameTick.Value < checkpoint.LockedAtGameTick
            || currentGameTick < confirmedPlannedResumeMinimumGameTick.Value)
            throw new FoundryPlanningException("governor_validation_restore_unproven",
                "Require the same protected identity/version and a confirmed planned-resume save covering the original lock; current time alone is insufficient.");
        return new GovernorThroughputValidation(checkpoint, sessionId, currentGameTick);
    }

    private GovernorThroughputValidation(GovernorValidationCheckpoint checkpoint, string sessionId, long currentGameTick)
    {
        _sessionId = sessionId; _planetId = checkpoint.PlanetId; _targetItemId = checkpoint.TargetItemId;
        _baselineHash = checkpoint.BaselineProposalHash; _declaredTick = checkpoint.DeclaredAtGameTick;
        _declarationRevision = checkpoint.DeclarationRevision; _baselineRate = checkpoint.BaselineRatePerMinute;
        _targetRate = checkpoint.TargetRatePerMinute; _tolerance = checkpoint.ToleranceFraction;
        _requiredTicks = checkpoint.RequiredGameTicks; _scaleHash = checkpoint.ScalePlanHash;
        _measurementGameTicks = checkpoint.MeasurementGameTicks;
        _sourceHash = checkpoint.SourceStateHash; _lockedAt = checkpoint.LockedAtGameTick;
        _lastCapture = currentGameTick;
        Reset("protected_resume_observation_reset", currentGameTick);
    }

    public void Begin(GovernorPlanSnapshot current, bool writesHealthy)
    {
        if (_lockedAt.HasValue) return;
        var rates = current.Supply.Where(s => s.ItemId == _targetItemId).ToArray();
        if (current.Revision != _declarationRevision || current.SourceStateHash != _sourceHash
            || current.CapturedAtGameTick < _declaredTick || current.CapturedAtGameTick - _declaredTick > 3600
            || current.Baseline.State != "ready" || !HealthyObservation(current, writesHealthy)
            || rates.Length != 1 || Math.Abs(rates[0].ActualProductionPerMinute - _baselineRate) > _baselineRate * _tolerance)
            throw new FoundryPlanningException("governor_validation_start_stale",
                "Lock the ready baseline before any accepted write/source change and within3600 game ticks; do not declare a baseline after expansion.");
        ValidateDeclaration(current);
        _lockedAt = current.CapturedAtGameTick;
        _scopeStart = _lockedAt.Value;
    }

    public bool TryBeginDurably(GovernorPlanSnapshot current, bool writesHealthy,
        Func<GovernorThroughputValidation, bool> persistDeclaration)
    {
        if (persistDeclaration is null) throw new ArgumentNullException(nameof(persistDeclaration));
        if (_lockedAt.HasValue)
            throw new FoundryPlanningException("governor_validation_already_started", "An existing lock cannot be silently replaced or repersisted.");
        Begin(current, writesHealthy);
        var persisted = false;
        try
        {
            persisted = persistDeclaration(this);
            return persisted;
        }
        finally
        {
            if (!persisted)
            {
                _lockedAt = null;
                Reset("declaration_persistence_failed", _declaredTick);
            }
        }
    }

    public GovernorThroughputValidationSnapshot Observe(GovernorPlanSnapshot current, bool writesHealthy)
    {
        // Never permit a different session, scale, tolerance or target to relax the declaration.
        ValidateDeclaration(current);
        if (!_lockedAt.HasValue) throw new FoundryPlanningException("governor_validation_not_started", "Lock this declaration before expanding the source.");
        var window = current.CurrentWindow;
        var sampleStep = NativeProductionRateCalculator.SampleStepGameTicks(_measurementGameTicks);
        if (current.CapturedAtGameTick < _declaredTick || current.CapturedAtGameTick < _lastCapture)
        { Reset("game_tick_regressed", Math.Max(_declaredTick, _lastCapture)); return Snapshot(); }
        if (current.SourceStateHash != _sourceHash)
        {
            _sourceHash = current.SourceStateHash;
            Reset("source_configuration_changed", current.CapturedAtGameTick);
        }
        _lastCapture = current.CapturedAtGameTick;
        if (!HealthyObservation(current, writesHealthy))
        { Reset("sampled_health_or_attribution_unproven", current.CapturedAtGameTick); return Snapshot(); }
        if (window.State != OverseerWindowStates.Ready || window.CrossedSessionBoundary
            || window.ElapsedGameTicks != _measurementGameTicks || !window.StartGameTick.HasValue
            || window.EndGameTick - window.StartGameTick.Value != _measurementGameTicks - 1
            || window.EndGameTick % sampleStep != 0
            || window.EndGameTick > current.CapturedAtGameTick
            || current.CapturedAtGameTick - window.EndGameTick > Math.Max(1, sampleStep - 1))
        { Reset("native_window_unavailable", current.CapturedAtGameTick); return Snapshot(); }
        if (window.StartGameTick.Value < _scopeStart) return Snapshot();
        if (_end.HasValue && window.EndGameTick <= _end.Value) return Snapshot();
        var rates = current.Supply.Where(s => s.ItemId == _targetItemId).ToArray();
        if (rates.Length != 1 || rates[0].ActualProductionPerMinute <= 0
            || Math.Abs(rates[0].ActualProductionPerMinute - _targetRate) > _targetRate * _tolerance)
        { Reset("measured_rate_outside_declared_tolerance", window.EndGameTick); return Snapshot(); }
        if (_end.HasValue && window.StartGameTick.Value > _end.Value + 1)
            Reset("unobserved_game_tick_gap", window.StartGameTick.Value);
        _start ??= window.StartGameTick.Value;
        _end = window.EndGameTick;
        var rate = rates[0].ActualProductionPerMinute;
        _minimum = _minimum.HasValue ? Math.Min(_minimum.Value, rate) : rate;
        _maximum = _maximum.HasValue ? Math.Max(_maximum.Value, rate) : rate;
        _count = checked(_count + 1);
        return Snapshot();
    }

    public GovernorThroughputValidationSnapshot Snapshot()
    {
        var observed = _start.HasValue && _end.HasValue ? _end.Value - _start.Value + 1 : 0;
        var passed = observed >= _requiredTicks;
        return new GovernorThroughputValidationSnapshot
        {
            State = passed ? "throughput_target_observed" : observed > 0 ? "observing" : "waiting_for_target",
            BaselineProposalHash = _baselineHash, DeclaredAtGameTick = _declaredTick,
            LockedAtGameTick = _lockedAt,
            BaselineProductionPerMinute = _baselineRate, TargetRatePerMinute = _targetRate,
            TargetMultiplier = _targetRate / _baselineRate, ToleranceFraction = _tolerance,
            RequiredGameTicks = _requiredTicks, ObservedContiguousGameTicks = observed,
            MeasurementGameTicks = _measurementGameTicks,
            MeasurementBasis = "overlapping_native_" + _measurementGameTicks + "_tick_windows_no_unobserved_tick_gaps",
            StartGameTick = _start, EndGameTick = _end,
            MinimumWindowRatePerMinute = _minimum, MaximumWindowRatePerMinute = _maximum,
            ObservationCount = _count, ResetReason = _resetReason, ThroughputTargetObserved = passed,
            DoubleThroughputTargetObserved = passed && _targetRate == 2 * _baselineRate && _tolerance <= .1m,
        };
    }

    private void Reset(string reason, long scopeStart)
    {
        _start = null; _end = null; _minimum = null; _maximum = null; _count = 0;
        _resetReason = reason; _scopeStart = scopeStart;
    }

    private void ValidateDeclaration(GovernorPlanSnapshot current)
    {
        if (current.SessionId != _sessionId || current.PlanetId != _planetId || current.TargetItemId != _targetItemId
            || current.TargetRatePerMinute != _targetRate || current.ToleranceFraction != _tolerance
            || current.MeasurementGameTicks != _measurementGameTicks
            || current.ValidationGameTicks != _requiredTicks || current.FullTargetScale.PlanHash != _scaleHash)
            throw new FoundryPlanningException("governor_validation_declaration_mismatch",
                "The pre-execution session, item, recipe scale, target, tolerance and duration are immutable.");
    }

    private static bool HealthyObservation(GovernorPlanSnapshot plan, bool writesHealthy) => writesHealthy
        && plan.SelectionContainsAllTargetProducers && !plan.FindingsTruncated && plan.TargetChainFindings.Count == 0
        && plan.Power.MinimumConsumerRatio >= .999 && plan.Power.MinimumConsumerRatio <= 1
        && plan.Blockers.All(b => b == "stable_nonzero_three_independent_windows_required"
            || b == "target_not_above_observed_reference");
}
