using System.Globalization;
using System.Text.Json;
using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class FoundrySitePlannerTests
{
    [Fact]
    public void BoxColliderBoundIncludesItsLocalCenterAndFullThreeDimensionalExtent()
    {
        Assert.Equal(6f, FoundrySitePlanner.ColliderBoundingRadius("Box",
            new Vector3Snapshot { Y = 1 }, new Vector3Snapshot { X = 3, Y = 4 }, 0));
    }

    [Fact]
    public void SphereAndRotatedCapsuleBoundsMustIncludeTheSeparateNativeRadius()
    {
        Assert.Equal(5f, FoundrySitePlanner.ColliderBoundingRadius("Sphere",
            new Vector3Snapshot { Y = 2 }, new Vector3Snapshot(), 3));
        Assert.Equal(9f, FoundrySitePlanner.ColliderBoundingRadius("Capsule",
            new Vector3Snapshot { Y = 2 }, new Vector3Snapshot { X = -3, Z = 4 }, 2));
    }

    [Theory]
    [InlineData("unknown", 1, 1)]
    [InlineData("Box", -1, 0)]
    [InlineData("Capsule", 1, -1)]
    [InlineData("Sphere", 1, float.NaN)]
    public void InvalidColliderShapesAndDimensionsDoNotBecomeInventedBounds(string shape, float extent, float radius)
    {
        Assert.Null(FoundrySitePlanner.ColliderBoundingRadius(shape, new Vector3Snapshot(), new Vector3Snapshot { X = extent }, radius));
    }

    [Fact]
    public void CandidatesAreBoundedDependencyOrderedAndRemainOnTheSphere()
    {
        var site = Candidates();
        Assert.Equal(new[] { "item-2/machine-1", "item-2/machine-2", "item-3/machine-1" }, site.Machines.Select(m => m.PlacementId));
        Assert.All(site.Machines, m => Assert.InRange(Length(m.Position), 199.999, 200.001));
        Assert.Equal("material", site.MaterialPlanHash);
        Assert.Equal("not_checked", site.Status);
        Assert.False(site.MachinePreviewsClear);
        Assert.All(site.Machines, m => Assert.False(m.NativeCheckPerformed));
        Assert.Equal(3, site.Columns);
    }

    [Fact]
    public void CompilationDoesNotMutateOrRetainTheInputCatalogOrOrigin()
    {
        var (plan, request, buildings) = Inputs();
        var before = JsonSerializer.Serialize(new { plan, request, buildings });
        var site = Create(plan, request, buildings);
        Assert.Equal(before, JsonSerializer.Serialize(new { plan, request, buildings }));
        request.Origin.Y = 999; buildings[0].PlacementRadius = 99; plan.Stages[0].StageId = "changed";
        Assert.Equal(200, site.Origin.Y);
        Assert.Equal(2, site.Machines[0].PlacementRadius);
        Assert.Equal("item-2", site.Machines[0].StageId);
    }

    [Theory]
    [InlineData(0, 12, 12, 0)]
    [InlineData(9, 12, 12, 0)]
    [InlineData(4, 3, 12, 0)]
    [InlineData(4, 33, 12, 0)]
    [InlineData(4, 12, 3, 0)]
    [InlineData(4, 12, 33, 0)]
    [InlineData(4, 12, 12, -1)]
    [InlineData(4, 12, 12, 360)]
    public void InvalidLayoutParametersAreRejected(int columns, float columnSpacing, float rowSpacing, float yaw)
    {
        var (plan, request, buildings) = Inputs();
        request.Columns = columns; request.ColumnSpacing = columnSpacing; request.RowSpacing = rowSpacing; request.YawDegrees = yaw;
        Assert.Equal("invalid_site", Assert.Throws<FoundryPlanningException>(() => Create(plan, request, buildings)).Reason);
    }

    [Fact]
    public void MissingZeroAndNonfiniteOriginsOrSpacingsFailClosed()
    {
        foreach (var origin in new[] { null, new Vector3Snapshot(), new Vector3Snapshot { Y = float.NaN }, new Vector3Snapshot { Y = float.PositiveInfinity } })
        {
            var (plan, request, buildings) = Inputs(); request.Origin = origin!;
            Assert.Equal("invalid_site", Assert.Throws<FoundryPlanningException>(() => Create(plan, request, buildings)).Reason);
        }
        var (p, r, b) = Inputs(); r.ColumnSpacing = float.PositiveInfinity;
        Assert.Equal("invalid_site", Assert.Throws<FoundryPlanningException>(() => Create(p, r, b)).Reason);
    }

    [Fact]
    public void SiteBoundIsStricterThanMaterialBoundAndCannotCreateAnUnboundedPreviewSweep()
    {
        var (plan, request, buildings) = Inputs();
        plan.Stages[0].MachineCount = 32; plan.MachineCount = 33;
        Assert.Equal("site_machine_limit", Assert.Throws<FoundryPlanningException>(() => Create(plan, request, buildings)).Reason);
        plan.Stages[0].MachineCount = 31; plan.MachineCount = 32; request.Columns = 1;
        Assert.Equal("site_extent_limit", Assert.Throws<FoundryPlanningException>(() => Create(plan, request, buildings)).Reason);
        request.Columns = 8;
        Assert.Equal(32, Create(plan, request, buildings).Machines.Count);
    }

    [Fact]
    public void UnknownFootprintsAndNontangentFramesAreNotGuessed()
    {
        var (plan, request, buildings) = Inputs(); buildings[0].PlacementRadius = null;
        Assert.Equal("unknown_footprint", Assert.Throws<FoundryPlanningException>(() => Create(plan, request, buildings)).Reason);
        buildings[0].PlacementRadius = 2;
        Assert.Equal("invalid_site_frame", Assert.Throws<FoundryPlanningException>(() => FoundrySitePlanner.CreateCandidates(
            plan, request, buildings, new Vector3Snapshot { X = 1 }, new Vector3Snapshot { Y = 1 })).Reason);
    }

    [Fact]
    public void SnappedMachineCollisionsAreSymmetricAndRecalculatedInsteadOfUsingRawLayout()
    {
        var site = Candidates();
        FoundrySitePlanner.CheckPlannedClearance(site);
        Assert.All(site.Machines, m => Assert.Empty(m.OverlappingPlacementIds));
        var original = site.Machines[1].Position;
        site.Machines[1].Position = site.Machines[0].Position;
        FoundrySitePlanner.CheckPlannedClearance(site);
        Assert.Equal(site.Machines[1].PlacementId, Assert.Single(site.Machines[0].OverlappingPlacementIds));
        Assert.Equal(site.Machines[0].PlacementId, Assert.Single(site.Machines[1].OverlappingPlacementIds));
        site.Machines[1].Position = original;
        FoundrySitePlanner.CheckPlannedClearance(site);
        Assert.All(site.Machines, m => Assert.Empty(m.OverlappingPlacementIds));
    }

    [Fact]
    public void IndividualNativeOkCannotHideAnInsufficientWholePlanInventory()
    {
        var site = ClearNative();
        FoundrySitePlanner.CompleteAssessment(site, new Dictionary<int, int> { [10] = 1, [20] = 1 }, 7, "player");
        Assert.True(site.MachinePreviewsClear);
        Assert.False(site.MachineInventorySufficient);
        Assert.Equal("blocked", site.Status);
        var budget = site.MachineInventory.Single(b => b.ItemId == 10);
        Assert.Equal(2, budget.RequiredCount); Assert.Equal(1, budget.PackageCount); Assert.Equal(1, budget.MissingCount);
    }

    [Fact]
    public void NativeClearPlusFullInventoryIsOnlyAPreviewNotAnExecutablePlan()
    {
        var site = ClearNative(); Complete(site);
        Assert.Equal("machine_previews_clear", site.Status);
        Assert.True(site.MachinePreviewsClear); Assert.True(site.MachineInventorySufficient);
        var plan = new FoundryPlanSnapshot { Site = site, Phase = "site_preview" };
        Assert.False(plan.Executable);
        Assert.DoesNotContain("planToken", JsonSerializer.Serialize(plan), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(false, false, null)]
    [InlineData(true, false, "Collide")]
    [InlineData(true, false, "Ok")]
    [InlineData(true, true, "NotEnoughItem")]
    public void UncheckedOrRejectedNativeConditionNeverBecomesClear(bool performed, bool passed, string? condition)
    {
        var site = ClearNative(); var machine = site.Machines[0];
        machine.NativeCheckPerformed = performed; machine.NativeCheckPassed = passed; machine.NativeBuildCondition = condition;
        Complete(site);
        Assert.False(site.MachinePreviewsClear); Assert.Equal("blocked", site.Status);
    }

    [Fact]
    public void PostSnapOverlapStillBlocksAnAllNativeOkAssessment()
    {
        var site = ClearNative(); site.Machines[1].Position = site.Machines[0].Position;
        Complete(site);
        Assert.False(site.MachinePreviewsClear); Assert.Equal("blocked", site.Status);
    }

    [Fact]
    public void ExistingObjectGuardCannotBeOverriddenByANativeOkFlag()
    {
        var site = ClearNative(); Complete(site); var first = site.AssessmentHash;
        site.Machines[0].OccupiedObjectId = -42; Complete(site);
        Assert.False(site.MachinePreviewsClear); Assert.Equal("blocked", site.Status);
        Assert.NotEqual(first, site.AssessmentHash);
    }

    [Fact]
    public void AssessmentHashIsCaptureTickIndependentButBoundToSessionRevisionPlayerAndMaterial()
    {
        var site = ClearNative(); Complete(site); var first = site.AssessmentHash;
        site.CapturedAtGameTick++; Complete(site); Assert.Equal(first, site.AssessmentHash);
        site.SessionId = "resumed"; Complete(site); Assert.NotEqual(first, site.AssessmentHash);
        site.SessionId = "owned-session";
        Complete(site, revision: 8); Assert.NotEqual(first, site.AssessmentHash);
        Complete(site, playerHash: "manual-operation"); Assert.NotEqual(first, site.AssessmentHash);
        site.MaterialPlanHash = "new-material"; Complete(site); Assert.NotEqual(first, site.AssessmentHash);
    }

    [Fact]
    public void PoseFootprintConditionAndBudgetEachChangeAssessmentHash()
    {
        var baseline = ClearNative(); Complete(baseline);
        foreach (var mutate in new Action<FoundrySiteSnapshot>[]
        {
            s => s.Machines[0].Position.X += 1,
            s => s.Machines[0].YawDegrees = 90,
            s => s.Machines[0].PlacementRadius += 1,
            s => s.Machines[0].NativeBuildCondition = "Collide",
        })
        {
            var site = ClearNative(); mutate(site); Complete(site);
            Assert.NotEqual(baseline.AssessmentHash, site.AssessmentHash);
        }
        FoundrySitePlanner.CompleteAssessment(baseline, new Dictionary<int, int> { [10] = 3, [20] = 1 }, 7, "player");
        var other = ClearNative(); Complete(other); Assert.NotEqual(other.AssessmentHash, baseline.AssessmentHash);
    }

    [Fact]
    public void InvalidSnappedEvidenceCannotProduceAnyAssessmentHash()
    {
        var site = ClearNative(); site.Machines[0].Position.X = float.NaN;
        Assert.Equal("invalid_site_evidence", Assert.Throws<FoundryPlanningException>(() => Complete(site)).Reason);
        Assert.Empty(site.AssessmentHash);
    }

    [Fact]
    public void CultureAndInputBuildingOrderDoNotChangeTheAssessment()
    {
        var first = ClearNative(); Complete(first);
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            var (plan, request, buildings) = Inputs(); buildings.Reverse();
            var next = Create(plan, request, buildings); SetClear(next); Complete(next);
            Assert.Equal(first.AssessmentHash, next.AssessmentHash);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    private static void Complete(FoundrySiteSnapshot site, long revision = 7, string playerHash = "player") =>
        FoundrySitePlanner.CompleteAssessment(site, new Dictionary<int, int> { [10] = 2, [20] = 1 }, revision, playerHash);
    private static FoundrySiteSnapshot ClearNative() { var site = Candidates(); SetClear(site); return site; }
    private static void SetClear(FoundrySiteSnapshot site)
    {
        foreach (var m in site.Machines) { m.NativeCheckPerformed = true; m.NativeCheckPassed = true; m.NativeBuildCondition = "Ok"; }
    }
    private static FoundrySiteSnapshot Candidates() { var (p, r, b) = Inputs(); return Create(p, r, b); }
    private static FoundrySiteSnapshot Create(FoundryPlanSnapshot p, FoundrySiteRequest r, List<BuildCatalogItem> b) =>
        FoundrySitePlanner.CreateCandidates(p, r, b, new Vector3Snapshot { X = 1 }, new Vector3Snapshot { Z = 1 });
    private static double Length(Vector3Snapshot p) => Math.Sqrt((double)p.X * p.X + (double)p.Y * p.Y + (double)p.Z * p.Z);
    private static (FoundryPlanSnapshot, FoundrySiteRequest, List<BuildCatalogItem>) Inputs() => (
        new FoundryPlanSnapshot
        {
            SessionId = "owned-session", PlanetId = 104, CapturedAtGameTick = 100, PlanHash = "material", MachineCount = 3,
            Stages = new List<FoundryStage>
            {
                new FoundryStage { StageId = "item-2", BuildingItemId = 10, RecipeId = 1, MachineCount = 2 },
                new FoundryStage { StageId = "item-3", BuildingItemId = 20, RecipeId = 2, MachineCount = 1 },
            },
        },
        new FoundrySiteRequest { Origin = new Vector3Snapshot { Y = 200 } },
        new List<BuildCatalogItem>
        {
            new BuildCatalogItem { ItemId = 10, PlacementRadius = 2 },
            new BuildCatalogItem { ItemId = 20, PlacementRadius = 3 },
        });
}
