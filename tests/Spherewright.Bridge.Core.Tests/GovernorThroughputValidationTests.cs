using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Diagnostics;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class GovernorThroughputValidationTests
{
    [Fact]
    public void TargetNeedsAll36000CoveredGameTicksNotWallTimeOrOneFastSample()
    {
        var run = Started();
        for (var tick = 3000; tick < 38400; tick += 600)
            Assert.False(run.Observe(Current(tick), true).ThroughputTargetObserved);
        var result = run.Observe(Current(38400), true);
        Assert.Equal(36000, result.ObservedContiguousGameTicks);
        Assert.True(result.ThroughputTargetObserved); Assert.True(result.DoubleThroughputTargetObserved);
        Assert.False(result.Durable); Assert.Equal(60, result.ObservationCount);
        Assert.Equal(30, result.BaselineProductionPerMinute); Assert.Equal(2, result.TargetMultiplier);
        Assert.Equal(2400, result.LockedAtGameTick);
        Assert.Contains("sampled", result.HealthEvidenceBasis);
        Assert.Contains(result.RemainingChecks, c => c.Contains("not complete balance"));
    }

    [Fact]
    public void OverlappingWindowsCoverOnlyTheirUnionAndDuplicateReadsAddNothing()
    {
        var run = Started();
        run.Observe(Current(3000), true);
        Assert.Equal(900, run.Observe(Current(3300), true).ObservedContiguousGameTicks);
        var duplicate = run.Observe(Current(3300), true);
        Assert.Equal(900, duplicate.ObservedContiguousGameTicks); Assert.Equal(2, duplicate.ObservationCount);
    }

    [Fact]
    public void AOneTickUnobservedGapStartsANewWindowWithoutBridgingHistory()
    {
        var run = Started(); run.Observe(Current(3000), true);
        var result = run.Observe(Current(3601), true);
        Assert.Equal(600, result.ObservedContiguousGameTicks);
        Assert.Equal("unobserved_game_tick_gap", result.ResetReason);
    }

    [Theory]
    [InlineData(0)] [InlineData(53)] [InlineData(67)]
    public void ZeroOrOutOfToleranceOutputResetsRatherThanDeclaringBalance(int rate)
    {
        var run = Started(); run.Observe(Current(3000), true);
        var result = run.Observe(Current(3600, rate), true);
        Assert.Equal(0, result.ObservedContiguousGameTicks);
        Assert.Equal("measured_rate_outside_declared_tolerance", result.ResetReason);
        Assert.False(result.ThroughputTargetObserved);
    }

    [Fact]
    public void LastResetReasonIsHistoricalWhileFreshHealthyWindowsCanReachTheTarget()
    {
        var run = Started();
        run.Observe(Current(3000), true);
        var failed = run.Observe(Current(3600, 53), true);
        Assert.Equal(0, failed.ObservedContiguousGameTicks);
        Assert.Equal("measured_rate_outside_declared_tolerance", failed.ResetReason);

        // A healthy window still overlapping the rejected scope is not a second failure.
        var waiting = run.Observe(Current(3900), true);
        Assert.Equal("waiting_for_target", waiting.State);
        Assert.Equal(0, waiting.ObservedContiguousGameTicks);
        Assert.Equal(failed.ResetReason, waiting.ResetReason);

        var observing = run.Observe(Current(4200), true);
        Assert.Equal("observing", observing.State);
        Assert.Equal(600, observing.ObservedContiguousGameTicks);
        Assert.Equal(failed.ResetReason, observing.ResetReason);
        for (var tick = 4800; tick <= 39600; tick += 600)
            run.Observe(Current(tick), true);
        var passed = run.Snapshot();
        Assert.Equal(36000, passed.ObservedContiguousGameTicks);
        Assert.True(passed.DoubleThroughputTargetObserved);
        Assert.Equal(failed.ResetReason, passed.ResetReason);
        Assert.Equal(3601, passed.StartGameTick);
        Assert.Equal(30, passed.BaselineProductionPerMinute);
        Assert.Equal(60, passed.TargetRatePerMinute);
    }

    [Theory]
    [InlineData("session")] [InlineData("target")] [InlineData("item")]
    [InlineData("planet")] [InlineData("tolerance")] [InlineData("duration")] [InlineData("recipe_scale")]
    public void ACallerCannotRelaxOrRetargetTheOriginalDeclaration(string changed)
    {
        var run = Started(); var current = Current(3000);
        switch (changed)
        {
            case "session": current.SessionId = "new-session"; break;
            case "target": current.TargetRatePerMinute = 55; break;
            case "item": current.TargetItemId++; break;
            case "planet": current.PlanetId++; break;
            case "tolerance": current.ToleranceFraction = .2m; break;
            case "duration": current.ValidationGameTicks = 36001; break;
            case "recipe_scale": current.FullTargetScale.PlanHash = "changed"; break;
        }
        var error = Assert.Throws<FoundryPlanningException>(() => run.Observe(current, true));
        Assert.Equal("governor_validation_declaration_mismatch", error.Reason);
        Assert.Equal(0, run.Snapshot().ObservedContiguousGameTicks);
    }

    [Theory]
    [InlineData("write")] [InlineData("source")] [InlineData("age")] [InlineData("already_expanded")]
    public void BaselineMustBeExplicitlyLockedBeforeExpansionOrWrites(string changed)
    {
        var run = new GovernorThroughputValidation(Declaration());
        var current = Declaration();
        switch (changed)
        {
            case "write": current.Revision++; break;
            case "source": current.SourceStateHash = "expanded"; break;
            case "age": current.CapturedAtGameTick += 3601; break;
            case "already_expanded": current.Supply[0].ActualProductionPerMinute = 60; break;
        }
        Assert.Equal("governor_validation_start_stale",
            Assert.Throws<FoundryPlanningException>(() => run.Begin(current, true)).Reason);
        Assert.Null(run.Snapshot().LockedAtGameTick);
    }

    [Fact]
    public void SourceChangesAfterChosenExpansionRequireEntirelyPostChangeWindows()
    {
        var run = Started(); run.Observe(Current(3000), true);
        var expanded = Current(3300); expanded.SourceStateHash = "expanded";
        Assert.Equal(0, run.Observe(expanded, true).ObservedContiguousGameTicks);
        expanded = Current(3899); expanded.SourceStateHash = "expanded";
        Assert.Equal(600, run.Observe(expanded, true).ObservedContiguousGameTicks);
        Assert.Equal(30, run.Snapshot().BaselineProductionPerMinute);
    }

    [Theory]
    [InlineData("power")] [InlineData("truncated")] [InlineData("attribution")]
    [InlineData("finding")] [InlineData("reciprocal")] [InlineData("writes")]
    public void SampledHealthOrAttributionFailureClearsTheThroughputStreak(string changed)
    {
        var run = Started(); run.Observe(Current(3000), true); var current = Current(3600);
        switch (changed)
        {
            case "power": current.Power.MinimumConsumerRatio = .9; break;
            case "truncated": current.FindingsTruncated = true; break;
            case "attribution": current.SelectionContainsAllTargetProducers = false; break;
            case "finding": current.TargetChainFindings.Add(new OverseerFindingSnapshot()); break;
            case "reciprocal": current.Blockers.Add("selected_reciprocal_connection_unproven:715:0"); break;
        }
        var result = run.Observe(current, changed != "writes");
        Assert.Equal(0, result.ObservedContiguousGameTicks);
        Assert.Equal("sampled_health_or_attribution_unproven", result.ResetReason);
    }

    [Theory]
    [InlineData("not_ready")] [InlineData("wrong_duration")] [InlineData("cross_session")] [InlineData("old_window")]
    public void InvalidOrStaleNativeWindowsCannotContribute(string changed)
    {
        var run = Started(); var current = Current(3000);
        switch (changed)
        {
            case "not_ready": current.CurrentWindow.State = "warming_up"; break;
            case "wrong_duration": current.CurrentWindow.ElapsedGameTicks = 599; break;
            case "cross_session": current.CurrentWindow.CrossedSessionBoundary = true; break;
            case "old_window": current.CapturedAtGameTick += 3; break;
        }
        Assert.Equal("native_window_unavailable", run.Observe(current, true).ResetReason);
    }

    [Theory]
    [InlineData("zero")] [InlineData("unstable")] [InlineData("few_samples")] [InlineData("unattributed")]
    public void InvalidPreExecutionBaselinesAreNeverAccepted(string changed)
    {
        var baseline = Declaration();
        switch (changed)
        {
            case "zero": baseline.Baseline.ProductionPerMinute = 0; break;
            case "unstable": baseline.Baseline.State = "unstable"; break;
            case "few_samples": baseline.Baseline.IndependentWindowCount = 2; break;
            case "unattributed": baseline.SelectionContainsAllTargetProducers = false; break;
        }
        Assert.Throws<FoundryPlanningException>(() => new GovernorThroughputValidation(baseline));
    }

    [Fact]
    public void ImmutableScalarsSurviveExternalDtoEditsAndTickRegressionLosesStreak()
    {
        var baseline = Declaration(); var run = new GovernorThroughputValidation(baseline);
        run.Begin(baseline, true); baseline.Baseline.ProductionPerMinute = 1; baseline.TargetRatePerMinute = 2;
        run.Observe(Current(3000), true);
        var regressed = run.Observe(Current(2999), true);
        Assert.Equal(30, regressed.BaselineProductionPerMinute); Assert.Equal(60, regressed.TargetRatePerMinute);
        Assert.Equal(0, regressed.ObservedContiguousGameTicks); Assert.Equal("game_tick_regressed", regressed.ResetReason);
    }

    [Fact]
    public void AOnePointFiveTargetIsNotReportedAsTheDoubleThroughputGate()
    {
        var baseline = Declaration(); baseline.TargetRatePerMinute = 45;
        var run = new GovernorThroughputValidation(baseline); run.Begin(baseline, true);
        for (var tick = 3000; tick <= 38400; tick += 600)
        {
            var current = Current(tick, 45); current.TargetRatePerMinute = 45; run.Observe(current, true);
        }
        Assert.True(run.Snapshot().ThroughputTargetObserved); Assert.False(run.Snapshot().DoubleThroughputTargetObserved);
    }

    [Fact]
    public void ReadingAReadyProposalDoesNotImplicitlyStartTheExperiment()
    {
        var run = new GovernorThroughputValidation(Declaration());
        Assert.Throws<FoundryPlanningException>(() => run.Observe(Current(3000), true));
    }

    [Fact]
    public void UnattributedGlobalWarningsRemainSeparateFromAHealthyMeasuredTarget()
    {
        var run = Started(); var current = Current(3000);
        current.Findings.Add(new OverseerFindingSnapshot { PlanetId = 104, ObjectId = 14, Kind = "vein_exhausted" });
        current.UnattributedPlanetFindingCount = 1;
        Assert.Equal(600, run.Observe(current, true).ObservedContiguousGameTicks);
        Assert.Single(current.Findings); Assert.False(current.Balanced);
    }

    [Fact]
    public void AnUnlockedCandidateCannotBecomeAPersistedDeclaration()
    {
        var run = new GovernorThroughputValidation(Declaration());
        Assert.Equal("governor_validation_not_started", Assert.Throws<FoundryPlanningException>(
            () => run.CreateCheckpoint("owned-hash", "game-version")).Reason);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void FailedDurableWriteCannotPublishALockOrBypassFreshPreExecutionChecks(bool throws)
    {
        var run = new GovernorThroughputValidation(Declaration());
        bool Persist(GovernorThroughputValidation value)
        {
            Assert.Equal(2400, value.CreateCheckpoint("owned-hash", "game-version").LockedAtGameTick);
            if (throws) throw new IOException("Injected write failure");
            return false;
        }
        if (throws) Assert.Throws<IOException>(() => run.TryBeginDurably(Declaration(), true, Persist));
        else Assert.False(run.TryBeginDurably(Declaration(), true, Persist));
        Assert.Null(run.Snapshot().LockedAtGameTick); Assert.Equal("declaration_persistence_failed", run.Snapshot().ResetReason);
        Assert.Throws<FoundryPlanningException>(() => run.Observe(Current(3000), true));
        var afterWrite = Declaration(); afterWrite.Revision++;
        Assert.Equal("governor_validation_start_stale", Assert.Throws<FoundryPlanningException>(
            () => run.TryBeginDurably(afterWrite, true, _ => true)).Reason);
    }

    [Fact]
    public void DurablePublicationRequiresTheSuccessfulCallbackAndDoesNotReplayIt()
    {
        var run = new GovernorThroughputValidation(Declaration()); var writes = 0;
        Assert.True(run.TryBeginDurably(Declaration(), true, value =>
        {
            value.CreateCheckpoint("owned-hash", "game-version").Validate(); writes++; return true;
        }));
        Assert.Equal(2400, run.Snapshot().LockedAtGameTick);
        Assert.Throws<FoundryPlanningException>(() => run.TryBeginDurably(Declaration(), true, _ => { writes++; return true; }));
        Assert.Equal(1, writes);
    }

    [Fact]
    public void ProtectedResumeRetainsOriginalDeclarationButLosesAllPreviousObservationCredit()
    {
        var run = Started();
        var checkpoint = run.CreateCheckpoint("owned-hash", "game-version");
        for (var tick = 3000; tick <= 38400; tick += 600) run.Observe(Current(tick), true);
        Assert.True(run.Snapshot().DoubleThroughputTargetObserved);
        // Round-trip the private DTO, not a client request or a claimed historical rate.
        checkpoint = System.Text.Json.JsonSerializer.Deserialize<GovernorValidationCheckpoint>(
            System.Text.Json.JsonSerializer.Serialize(checkpoint))!;
        var restored = GovernorThroughputValidation.Restore(checkpoint, "owned-hash", "game-version", "resumed", 40000, 40030);
        var state = restored.Snapshot();
        Assert.Equal("pre-execution-proposal", state.BaselineProposalHash);
        Assert.Equal(2400, state.DeclaredAtGameTick); Assert.Equal(2400, state.LockedAtGameTick);
        Assert.Equal(30, state.BaselineProductionPerMinute); Assert.Equal(60, state.TargetRatePerMinute);
        Assert.Equal(.1m, state.ToleranceFraction); Assert.Equal(36000, state.RequiredGameTicks);
        Assert.Equal(0, state.ObservedContiguousGameTicks); Assert.Equal(0, state.ObservationCount);
        Assert.Null(state.StartGameTick); Assert.Null(state.EndGameTick); Assert.False(state.Durable);
        Assert.False(state.DoubleThroughputTargetObserved); Assert.Equal("protected_resume_observation_reset", state.ResetReason);

        GovernorPlanSnapshot Resumed(long tick)
        {
            var current = Current(tick); current.SessionId = "resumed"; current.Revision = 1;
            return current;
        }
        Assert.Equal(0, restored.Observe(Resumed(40030), true).ObservedContiguousGameTicks);
        for (var tick = 40629; tick < 76029; tick += 600)
            Assert.False(restored.Observe(Resumed(tick), true).DoubleThroughputTargetObserved);
        Assert.True(restored.Observe(Resumed(76029), true).DoubleThroughputTargetObserved);
        Assert.Equal(36000, restored.Snapshot().ObservedContiguousGameTicks);
    }

    [Theory]
    [InlineData("owner")] [InlineData("version")] [InlineData("missing_proof")]
    [InlineData("old_save")] [InlineData("future_proof")] [InlineData("same_session")]
    [InlineData("missing_session")]
    public void RestoreRequiresAConfirmedCoveringPlannedResumeNotTheCurrentTick(string changed)
    {
        var checkpoint = Started().CreateCheckpoint("owned-hash", "game-version");
        var owner = "owned-hash"; var version = "game-version"; var session = "resumed";
        long? savedTick = 3000; long currentTick = 50000;
        switch (changed)
        {
            case "owner": owner = "another-owned-world"; break;
            case "version": version = "different-version"; break;
            case "missing_proof": savedTick = null; break;
            case "old_save": savedTick = 2399; break; // Many later ticks cannot repair a pre-lock load.
            case "future_proof": savedTick = 50001; break;
            case "same_session": session = "owned-session"; break;
            case "missing_session": session = ""; break;
        }
        Assert.Equal("governor_validation_restore_unproven", Assert.Throws<FoundryPlanningException>(() =>
            GovernorThroughputValidation.Restore(checkpoint, owner, version, session, savedTick, currentTick)).Reason);
    }

    [Theory]
    [InlineData("version")] [InlineData("owner")] [InlineData("session")] [InlineData("hash")]
    [InlineData("source")] [InlineData("scale")] [InlineData("planet")] [InlineData("item")]
    [InlineData("declared_tick")] [InlineData("revision")] [InlineData("lock_tick")]
    [InlineData("baseline")] [InlineData("target")] [InlineData("tolerance")] [InlineData("duration")]
    public void PersistedScalarsAreIntegrityChecked(string changed)
    {
        var saved = Started().CreateCheckpoint("owned-hash", "game-version");
        switch (changed)
        {
            case "version": saved.Version++; break;
            case "owner": saved.OwnedIdentityHash += "changed"; break;
            case "session": saved.SourceSessionId += "changed"; break;
            case "hash": saved.BaselineProposalHash += "changed"; break;
            case "source": saved.SourceStateHash += "changed"; break;
            case "scale": saved.ScalePlanHash += "changed"; break;
            case "planet": saved.PlanetId++; break;
            case "item": saved.TargetItemId++; break;
            case "declared_tick": saved.DeclaredAtGameTick--; break;
            case "revision": saved.DeclarationRevision++; break;
            case "lock_tick": saved.LockedAtGameTick++; break;
            case "baseline": saved.BaselineRatePerMinute++; break;
            case "target": saved.TargetRatePerMinute++; break;
            case "tolerance": saved.ToleranceFraction += .01m; break;
            case "duration": saved.RequiredGameTicks++; break;
        }
        Assert.Equal("governor_validation_checkpoint_invalid", Assert.Throws<FoundryPlanningException>(saved.Validate).Reason);
    }

    [Theory]
    [InlineData("oversized")] [InlineData("missing_source")] [InlineData("lock_before_declaration")]
    [InlineData("late_lock")] [InlineData("zero_baseline")] [InlineData("zero_target")]
    [InlineData("relaxed_tolerance")] [InlineData("short_duration")] [InlineData("negative_revision")]
    public void IntegrityHashDoesNotReplaceStructuralValidation(string changed)
    {
        var saved = Started().CreateCheckpoint("owned-hash", "game-version");
        switch (changed)
        {
            case "oversized": saved.SourceSessionId = new string('x', 257); break;
            case "missing_source": saved.SourceStateHash = ""; break;
            case "lock_before_declaration": saved.LockedAtGameTick = 2399; break;
            case "late_lock": saved.LockedAtGameTick = 6001; break;
            case "zero_baseline": saved.BaselineRatePerMinute = 0; break;
            case "zero_target": saved.TargetRatePerMinute = 0; break;
            case "relaxed_tolerance": saved.ToleranceFraction = .51m; break;
            case "short_duration": saved.RequiredGameTicks = 35999; break;
            case "negative_revision": saved.DeclarationRevision = -1; break;
        }
        saved.IntegrityHash = saved.CalculateIntegrityHash();
        Assert.Throws<FoundryPlanningException>(saved.Validate);
    }

    [Fact]
    public void ARestoredDeclarationStillRejectsRetargetingAndRequiresFreshExpandedSourceWindows()
    {
        var saved = Started().CreateCheckpoint("owned-hash", "game-version");
        var restored = GovernorThroughputValidation.Restore(saved, "owned-hash", "game-version", "resumed", 3000, 3030);
        var current = Current(3630); current.SessionId = "resumed"; current.SourceStateHash = "expanded";
        Assert.Equal(0, restored.Observe(current, true).ObservedContiguousGameTicks);
        current = Current(4230); current.SessionId = "resumed"; current.SourceStateHash = "expanded";
        Assert.Equal(600, restored.Observe(current, true).ObservedContiguousGameTicks);
        current.TargetRatePerMinute = 59;
        Assert.Equal("governor_validation_declaration_mismatch", Assert.Throws<FoundryPlanningException>(
            () => restored.Observe(current, true)).Reason);
        Assert.Equal(30, restored.Snapshot().BaselineProductionPerMinute);
        saved.BaselineRatePerMinute = 1; // The restored validator copied its original immutable scalars.
        Assert.Equal(30, restored.Snapshot().BaselineProductionPerMinute);
    }

    [Fact]
    public void ArchiveIsBoundedIdempotentAndNeverEvictsOrReplacesAnOriginalDeclaration()
    {
        var archive = new GovernorDeclarationArchive { IdentityHash = "owned-hash", GameVersion = "game-version" };
        var first = Started().CreateCheckpoint("owned-hash", "game-version");
        archive.AddLockedDeclaration(first); archive.AddLockedDeclaration(first);
        Assert.Single(archive.Declarations);
        for (var index = 1; index < GovernorDeclarationArchive.MaximumDeclarations; index++)
        {
            var saved = Started().CreateCheckpoint("owned-hash", "game-version");
            saved.BaselineProposalHash += index; saved.IntegrityHash = saved.CalculateIntegrityHash();
            archive.AddLockedDeclaration(saved);
        }
        var ninth = Started().CreateCheckpoint("owned-hash", "game-version");
        ninth.BaselineProposalHash += "ninth"; ninth.IntegrityHash = ninth.CalculateIntegrityHash();
        Assert.Equal("governor_validation_limit", Assert.Throws<FoundryPlanningException>(() => archive.AddLockedDeclaration(ninth)).Reason);
        var replacement = Started().CreateCheckpoint("owned-hash", "game-version");
        replacement.BaselineRatePerMinute = 1; replacement.IntegrityHash = replacement.CalculateIntegrityHash();
        Assert.Throws<FoundryPlanningException>(() => archive.AddLockedDeclaration(replacement));
        Assert.Equal(8, archive.Declarations.Count); Assert.Same(first, archive.Declarations[0]);
        Assert.Equal(30, first.BaselineRatePerMinute);
    }

    [Theory]
    [InlineData("version")] [InlineData("owner")] [InlineData("game_version")]
    [InlineData("null_entries")] [InlineData("null_entry")] [InlineData("duplicate")]
    [InlineData("wrong_entry_owner")] [InlineData("wrong_entry_version")]
    public void InvalidPrivateArchiveCannotBeAttached(string changed)
    {
        var archive = new GovernorDeclarationArchive { IdentityHash = "owned-hash", GameVersion = "game-version" };
        var saved = Started().CreateCheckpoint("owned-hash", "game-version"); archive.AddLockedDeclaration(saved);
        switch (changed)
        {
            case "version": archive.Version = 2; break;
            case "owner": archive.IdentityHash = "other-world"; break;
            case "game_version": archive.GameVersion = "different-version"; break;
            case "null_entries": archive.Declarations = null!; break;
            case "null_entry": archive.Declarations.Add(null!); break;
            case "duplicate": archive.Declarations.Add(saved); break;
            case "wrong_entry_owner": saved.OwnedIdentityHash = "other-world"; break;
            case "wrong_entry_version": saved.GameVersion = "different-version"; break;
        }
        saved.IntegrityHash = saved.CalculateIntegrityHash();
        Assert.Throws<FoundryPlanningException>(() => archive.Validate("owned-hash", "game-version"));
    }

    private static GovernorThroughputValidation Started()
    {
        var declaration = Declaration(); var run = new GovernorThroughputValidation(declaration);
        run.Begin(declaration, true); return run;
    }
    private static GovernorPlanSnapshot Declaration() => Current(2400, 30);
    private static GovernorPlanSnapshot Current(long tick, decimal rate = 60) => new()
    {
        SessionId = "owned-session", PlanetId = 104, TargetItemId = 1112, Revision = 1,
        CapturedAtGameTick = tick, ProposalHash = "pre-execution-proposal", SourceStateHash = "source",
        TargetRatePerMinute = 60, ToleranceFraction = .1m, ValidationGameTicks = 36000,
        SelectionContainsAllTargetProducers = true,
        Baseline = new GovernorBaselineSnapshot { State = "ready", IndependentWindowCount = 3,
            StartGameTick = 601, EndGameTick = 2400, ProductionPerMinute = 30, MinimumPerMinute = 30, MaximumPerMinute = 30 },
        CurrentWindow = new OverseerWindowSnapshot { State = "ready", StartGameTick = tick - 599, EndGameTick = tick, ElapsedGameTicks = 600 },
        FullTargetScale = new FoundryPlanSnapshot { PlanHash = "scale" },
        Power = new OverseerPowerSummarySnapshot { MinimumConsumerRatio = 1 },
        Supply = new List<GovernorSupplyBalance> { new() { ItemId = 1112, ActualProductionPerMinute = rate } },
    };
}
