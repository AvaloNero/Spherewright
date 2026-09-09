using System.Text.Json;
using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class BlueprintBuildStateTests
{
    [Theory]
    [InlineData("native")] [InlineData("material")] [InlineData("technology")] [InlineData("occupied")]
    public void CannotCreateExecutionFromBlockedPreview(string missing)
    {
        var site = Site();
        if (missing == "native") site.NativeCheckPassed = false;
        if (missing == "material") site.InventorySufficient = false;
        if (missing == "technology") site.TechnologySatisfied = false;
        if (missing == "occupied") site.Blockers.Add("occupied");
        Assert.Throws<InvalidOperationException>(() => BlueprintBuildState.Create(site));
    }

    [Fact]
    public void PlanMustBeBoundedAndHashDetectsModifiedConfiguration()
    {
        var state = Create();
        state.Site.Objects[0].RecipeId++;
        Assert.Throws<InvalidDataException>(state.Validate);
        var site = Site();
        site.Objects = Enumerable.Range(0, 33).Select(i => new BlueprintSiteObject { Index = i, ItemId = 2001 }).ToList();
        Assert.Throws<InvalidDataException>(() => BlueprintBuildState.Create(site));
    }

    [Fact]
    public void StepWaitsForNativeCompletionBeforeSubmittingDependentObject()
    {
        var state = Create(); Begin(state);
        Assert.Equal(0, state.NextReadyIndex());
        state.BeforeSubmit(0, 101, 2, "before");
        Assert.Null(state.NextReadyIndex());
        state.ConfirmSubmission(0, 8, 1, "after");
        Assert.Null(state.NextReadyIndex());
        state.ConfirmCompletion(0, 100, 110);
        Assert.Equal(1, state.NextReadyIndex());
        state.Validate();
    }

    [Theory]
    [InlineData("connection")] [InlineData("budget")] [InlineData("object")] [InlineData("progress")]
    [InlineData("invalid_endpoint")] [InlineData("oversized_parameters")]
    public void MalformedNestedDurableDataRejectsAsDataErrorBeforeFingerprinting(string corruption)
    {
        var state = Create();
        if (corruption == "connection") state.Site.Connections.Add(null!);
        if (corruption == "budget") state.Site.ConstructionItems.Add(null!);
        if (corruption == "object") state.Site.Objects[0] = null!;
        if (corruption == "progress") state.Objects[0] = null!;
        if (corruption == "invalid_endpoint") state.Site.Connections.Add(new BlueprintPlanConnection { FromIndex = 999, ToIndex = 0 });
        if (corruption == "oversized_parameters") state.Site.Objects[0].Parameters = new int[129];
        Assert.Throws<InvalidDataException>(state.Validate);
    }

    [Fact]
    public void NoWholeReplayOrDuplicateDebitAfterPartialSuccessAndRestart()
    {
        var state = Create(); Begin(state, 1); CompleteFirst(state);
        state.Stop("paused");
        var serialized = JsonSerializer.Serialize(state);
        Assert.DoesNotContain("planToken", serialized, StringComparison.OrdinalIgnoreCase);
        var restored = JsonSerializer.Deserialize<BlueprintBuildState>(serialized)!;
        restored.Validate();
        Assert.Null(restored.NextReadyIndex());
        restored.Begin("new-session", Guid.NewGuid().ToString(), 2, 111);
        Assert.Equal(1, restored.NextReadyIndex());
        Assert.Throws<InvalidOperationException>(() => restored.BeforeSubmit(0, 111, 1, "again"));
        Assert.Equal(100, restored.Objects[0].EntityId);
        Assert.Equal(2, restored.Objects[0].InventoryBefore);
        Assert.Equal(1, restored.Objects[0].InventoryAfter);
    }

    [Fact]
    public void CancellationRetainsPendingObjectAndDoesNotClaimRollback()
    {
        var state = Create(); Begin(state);
        state.BeforeSubmit(0, 101, 2, "before"); state.ConfirmSubmission(0, 8, 1, "after");
        state.Stop("cancelled");
        Assert.Null(state.NextReadyIndex());
        Assert.Equal(BlueprintObjectStates.PendingConstruction, state.Objects[0].State);
        Assert.Equal(8, state.Objects[0].PrebuildId);
        Assert.Equal(BlueprintObjectStates.NotSubmitted, state.Objects[1].State);
        state.ConfirmCompletion(0, 100, 110); // Normal drones are not cancelled.
        Assert.Equal("cancelled", state.Phase);
        Assert.Throws<InvalidOperationException>(() => state.Stop("completed"));
    }

    [Theory]
    [InlineData("submitting")] [InlineData("outcome_unknown")]
    public void CrashGapOrUnknownResultCannotBeRetried(string phase)
    {
        var state = Create(); Begin(state);
        state.BeforeSubmit(0, 101, 2, "before");
        if (phase == "outcome_unknown") state.Stop("outcome_unknown", 0, "native_exception");
        var restored = JsonSerializer.Deserialize<BlueprintBuildState>(JsonSerializer.Serialize(state))!;
        Assert.Throws<InvalidOperationException>(() => restored.Begin("new", Guid.NewGuid().ToString(), 2, 111));
        Assert.Null(restored.NextReadyIndex());
    }

    [Fact]
    public void SaveOlderThanDurableEvidenceCannotResume()
    {
        var state = Create(); Begin(state); CompleteFirst(state); state.Stop("paused");
        Assert.Throws<InvalidOperationException>(() => state.Begin("new", Guid.NewGuid().ToString(), 2, 109));
    }

    [Theory]
    [InlineData(0, 1)] [InlineData(8, 2)] [InlineData(8, 0)]
    public void SubmissionRequiresPrebuildIdentityAndExactDebit(int id, int after)
    {
        var state = Create(); Begin(state); state.BeforeSubmit(0, 101, 2, "before");
        Assert.Throws<InvalidOperationException>(() => state.ConfirmSubmission(0, id, after, "after"));
        Assert.Equal(BlueprintObjectStates.Submitting, state.Objects[0].State);
    }

    [Theory]
    [InlineData(0)] [InlineData(33)]
    public void RejectsUnboundedSubmissionAuthority(int limit) => Assert.Throws<InvalidOperationException>(() => Begin(Create(), limit));

    [Fact]
    public void ProgressHashBindsSessionRevisionGenerationAndEvidence()
    {
        var state = Create(); Begin(state);
        var hash = state.ProgressHash("session", 1);
        Assert.NotEqual(hash, state.ProgressHash("other", 1));
        Assert.NotEqual(hash, state.ProgressHash("session", 2));
        state.BeforeSubmit(0, 101, 2, "before");
        Assert.NotEqual(hash, state.ProgressHash("session", 1));
        hash = state.ProgressHash("session", 1);
        state.ConfirmSubmission(0, 8, 1, "after");
        Assert.NotEqual(hash, state.ProgressHash("session", 1));
    }

    [Fact]
    public void RepeatedCompletionCannotOverwriteFirstDurableEvidence()
    {
        var state = Create(); Begin(state); CompleteFirst(state);
        Assert.Throws<InvalidOperationException>(() => state.ConfirmCompletion(0, 100, 112));
        Assert.Equal(110, state.Objects[0].CompletedAtGameTick);
    }

    [Fact]
    public void BlockedUnsubmittedStepOnlyResumesWithNewAuthority()
    {
        var state = Create(); Begin(state); CompleteFirst(state);
        state.Stop("blocked", 1, "occupied");
        Assert.Null(state.NextReadyIndex());
        state.Begin("fresh", Guid.NewGuid().ToString(), 2, 111);
        Assert.Equal(1, state.NextReadyIndex());
        Assert.Equal(BlueprintObjectStates.Completed, state.Objects[0].State);
    }

    [Theory]
    [InlineData("unknown_phase")] [InlineData("negative_generation")] [InlineData("lost_receipt_state")]
    [InlineData("future_submission")] [InlineData("early_completion")] [InlineData("missing_write_ahead")]
    [InlineData("false_completed")]
    public void CorruptDurableProgressCannotBecomeResumeAuthority(string corruption)
    {
        var state = Create(); Begin(state); CompleteFirst(state);
        switch (corruption)
        {
            case "unknown_phase": state.Phase = "anything"; break;
            case "negative_generation": state.Generation = -1; break;
            case "lost_receipt_state": state.Objects[0].State = BlueprintObjectStates.NotSubmitted; break;
            case "future_submission": state.Objects[0].SubmittedAtGameTick = 111; break;
            case "early_completion": state.Objects[0].CompletedAtGameTick = 100; break;
            case "missing_write_ahead": state.Objects[1].State = BlueprintObjectStates.Submitting; break;
            case "false_completed": state.Phase = "completed"; break;
        }
        Assert.Throws<InvalidDataException>(state.Validate);
    }

    [Fact]
    public void CyclicApprovedShapeCannotBeExecutedEvenWithRecomputedHash()
    {
        var site = Site(); site.Objects[0].Dependencies.Add(1);
        Assert.Throws<InvalidDataException>(() => BlueprintBuildState.Create(site));
    }

    [Fact]
    public void ExistingRunningActionCannotBeReplacedInSameSession()
    {
        var state = Create(); Begin(state);
        Assert.Throws<InvalidOperationException>(() => Begin(state));
        state.Begin("restarted-session", Guid.NewGuid().ToString(), 2, 100);
        Assert.Equal("restarted-session", state.ActiveSessionId);
    }

    [Fact]
    public void UnknownPhaseCannotResumeEvenWithoutAnObjectReceipt()
    {
        var state = Create(); Begin(state); state.Stop("outcome_unknown");
        Assert.Throws<InvalidOperationException>(() => state.Begin("fresh", Guid.NewGuid().ToString(), 2, 100));
    }

    [Fact]
    public void EmptyInventoryProofIsNotAReceipt()
    {
        var state = Create(); Begin(state);
        Assert.Throws<InvalidOperationException>(() => state.BeforeSubmit(0, 101, 2, ""));
        state.BeforeSubmit(0, 101, 2, "before");
        Assert.Throws<InvalidOperationException>(() => state.ConfirmSubmission(0, 8, 1, ""));
    }

    private static BlueprintBuildState Create() => BlueprintBuildState.Create(Site());

    [Fact]
    public void ProvedPendingCompletionKeepsUnknownActionStoppedUntilExplicitCancellationAndFreshAuthority()
    {
        var state = Create(); Begin(state);
        state.BeforeSubmit(0,101,2,"before"); state.ConfirmSubmission(0,8,1,"after");
        state.Stop("outcome_unknown"); // Completion check failed, not an unknown native debit.
        var restored = JsonSerializer.Deserialize<BlueprintBuildState>(JsonSerializer.Serialize(state))!;
        restored.ConfirmCompletion(0,100,110); // Adapter must prove unique actual geometry/material/edges first.
        Assert.Equal("outcome_unknown",restored.Phase);
        Assert.Null(restored.NextReadyIndex());
        Assert.Throws<InvalidOperationException>(() => restored.Begin("new",Guid.NewGuid().ToString(),1,111));
        restored.Stop("cancelled"); // Existing explicit stop never refunds or resubmits the completed object.
        restored.Begin("new",Guid.NewGuid().ToString(),1,111);
        Assert.Equal(1,restored.NextReadyIndex());
        Assert.Equal(100,restored.Objects[0].EntityId);
        Assert.Equal(2,restored.Objects[0].InventoryBefore); Assert.Equal(1,restored.Objects[0].InventoryAfter);
        Assert.Throws<InvalidOperationException>(() => restored.BeforeSubmit(0,112,1,"replay"));
        restored.Validate();
    }

    private static void Begin(BlueprintBuildState state, int limit = 2) => state.Begin("session", Guid.NewGuid().ToString(), limit, 100);
    private static void CompleteFirst(BlueprintBuildState state)
    { state.BeforeSubmit(0, 101, 2, "before"); state.ConfirmSubmission(0, 8, 1, "after"); state.ConfirmCompletion(0, 100, 110); }
    private static BlueprintSiteSnapshot Site() => new()
    {
        SessionId = "session", PlanetId = 104, CapturedAtGameTick = 100, BlueprintHash = "blueprint",
        NativeCheckPerformed = true, NativeCheckPassed = true, InventorySufficient = true, TechnologySatisfied = true,
        Objects = new List<BlueprintSiteObject> { new() { Index = 0, ItemId = 2001 },
            new() { Index = 1, ItemId = 2001, Dependencies = new List<int> { 0 } } },
        Connections = new List<BlueprintPlanConnection> { new() { FromIndex = 1, ToIndex = 0, FromSlot = 0, ToSlot = 1 } },
    };
}
