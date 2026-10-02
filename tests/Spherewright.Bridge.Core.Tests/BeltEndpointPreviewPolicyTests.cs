using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Actions;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class BeltEndpointPreviewPolicyTests
{
    [Theory]
    [InlineData(BeltPathModes.NativeGrid)]
    [InlineData(BeltPathModes.NativeElevatedGrid)]
    public void AcceptsOneOrTwoExactBindingsAndAnExplicitBeltOrientation(string mode)
    {
        var both = Request(mode);
        both.BeltEndpointPreview!.Source!.ExistingSlot = 4;
        both.BeltEndpointPreview.Source.ExistingBeltQuarterTurns = 0;
        both.BeltEndpointPreview.Source.PlannedBeltQuarterTurns = 2;
        both.BeltEndpointPreview.Destination = Binding(43, -1, 3, 1);
        Assert.Null(BeltEndpointPreviewPolicy.ValidateRequest(both));

        var sourceOnly = Request(mode);
        sourceOnly.BeltEndpointPreview!.Destination = null;
        Assert.Null(BeltEndpointPreviewPolicy.ValidateRequest(sourceOnly));

        var destinationOnly = Request(mode);
        destinationOnly.BeltEndpointPreview!.Source = null;
        destinationOnly.BeltEndpointPreview.Destination = Binding(43, 7, 0, 3);
        Assert.Null(BeltEndpointPreviewPolicy.ValidateRequest(destinationOnly));
    }

    [Theory]
    [InlineData("wrong_building")]
    [InlineData("geodesic")]
    [InlineData("unknown_mode")]
    [InlineData("missing_start")]
    [InlineData("missing_end")]
    [InlineData("span_over_30m")]
    [InlineData("empty_player_hash")]
    [InlineData("whitespace_player_hash")]
    [InlineData("oversized_player_hash")]
    [InlineData("legacy_source")]
    [InlineData("legacy_destination")]
    [InlineData("resource")]
    [InlineData("resource_hash")]
    [InlineData("source_hash")]
    [InlineData("destination_hash")]
    [InlineData("initial_filter")]
    public void RejectsMixedOrUnsupportedBuildAndFutureInputShapes(string fault)
    {
        var request = Request();
        switch (fault)
        {
            case "wrong_building": request.BuildingItemId = 2002; break;
            case "geodesic": request.BeltPathMode = BeltPathModes.NativeGeodesic; break;
            case "unknown_mode": request.BeltPathMode = "automatic"; break;
            case "missing_start": request.PreferredPosition = null; break;
            case "missing_end": request.PathEnd = null; break;
            case "span_over_30m": request.PathEnd = SurfacePoint(40, 2); break;
            case "empty_player_hash": request.ExpectedPlayerStateHash = string.Empty; break;
            case "whitespace_player_hash": request.ExpectedPlayerStateHash = " "; break;
            case "oversized_player_hash": request.ExpectedPlayerStateHash = new string('p', 257); break;
            case "legacy_source": request.SourceObjectId = 82; break;
            case "legacy_destination": request.DestinationObjectId = 83; break;
            case "resource": request.ResourceNodeId = 9; break;
            case "resource_hash": request.ExpectedResourceStateHash = "resource"; break;
            case "source_hash": request.ExpectedSourceStateHash = "source"; break;
            case "destination_hash": request.ExpectedDestinationStateHash = "destination"; break;
            case "initial_filter": request.InitialSorterFilterItemId = 1120; break;
        }

        Assert.NotNull(BeltEndpointPreviewPolicy.ValidateRequest(request));
    }

    [Theory]
    [InlineData("no_binding")]
    [InlineData("sorter")]
    [InlineData("zero_filter")]
    [InlineData("oversized_filter")]
    [InlineData("zero_id")]
    [InlineData("oversized_id")]
    [InlineData("empty_hash")]
    [InlineData("whitespace_hash")]
    [InlineData("oversized_hash")]
    [InlineData("slot_low")]
    [InlineData("slot_high")]
    [InlineData("existing_turn_low")]
    [InlineData("existing_turn_high")]
    [InlineData("planned_turn_low")]
    [InlineData("planned_turn_high")]
    [InlineData("device_turn")]
    [InlineData("same_endpoint")]
    public void RejectsMalformedBindingsBeforeNativeQualification(string fault)
    {
        var request = Request();
        var preview = request.BeltEndpointPreview!;
        switch (fault)
        {
            case "no_binding": preview.Source = null; preview.Destination = null; break;
            case "sorter": preview.SorterItemId = 2013; break;
            case "zero_filter": preview.FilterItemId = 0; break;
            case "oversized_filter": preview.FilterItemId = short.MaxValue + 1; break;
            case "zero_id": preview.Source!.ExistingObjectId = 0; break;
            case "oversized_id": preview.Source!.ExistingObjectId = 131073; break;
            case "empty_hash": preview.Source!.ExpectedEndpointStateHash = string.Empty; break;
            case "whitespace_hash": preview.Source!.ExpectedEndpointStateHash = "  "; break;
            case "oversized_hash": preview.Source!.ExpectedEndpointStateHash = new string('h', 257); break;
            case "slot_low": preview.Source!.ExistingSlot = -2; break;
            case "slot_high": preview.Source!.ExistingSlot = 16; break;
            case "existing_turn_low": preview.Source!.ExistingBeltQuarterTurns = -1; break;
            case "existing_turn_high": preview.Source!.ExistingBeltQuarterTurns = 4; break;
            case "planned_turn_low": preview.Source!.PlannedBeltQuarterTurns = -1; break;
            case "planned_turn_high": preview.Source!.PlannedBeltQuarterTurns = 4; break;
            case "device_turn": preview.Source!.ExistingBeltQuarterTurns = 1; break;
            case "same_endpoint": preview.Destination = Binding(preview.Source!.ExistingObjectId, 2, 0, 1); break;
        }

        Assert.NotNull(BeltEndpointPreviewPolicy.ValidateRequest(request));
    }

    [Theory]
    [InlineData(BeltPathModes.NativeGrid)]
    [InlineData(BeltPathModes.NativeElevatedGrid)]
    public void ConfirmsExactTokenlessReadOnlyPreviewAndNewOnlyBudget(string mode)
    {
        var request = Request(mode);
        var plan = Plan(request.BeltEndpointPreview!, mode);

        Assert.True(BeltEndpointPreviewPolicy.ConfirmsReadOnlyEcho(request, plan, "owned-session"));
        Assert.False(plan.Prepared);
        Assert.False(plan.CommitAllowedNow);
        Assert.Empty(plan.PlanToken);
        Assert.Equal(4, plan.PlannedPath.Count);
        Assert.Equal(2, plan.ItemBudget.Count);
        Assert.Equal(4, plan.ItemBudget.Single(row => row.ItemId == 2001).Count);
        Assert.Equal(2, plan.ItemBudget.Single(row => row.ItemId == 2011).Count);
        Assert.All(plan.ItemBudget, row => Assert.Equal("preview-construction-consumption", row.Direction));
        Assert.False(plan.BeltEndpointPreview!.Executable);
    }

    [Theory]
    [InlineData("prepared")]
    [InlineData("commit_allowed")]
    [InlineData("token")]
    [InlineData("action_kind")]
    [InlineData("build_kind")]
    [InlineData("source_object")]
    [InlineData("destination_object")]
    [InlineData("phase")]
    [InlineData("planet")]
    [InlineData("session")]
    [InlineData("null_session")]
    [InlineData("wrong_expected_session")]
    [InlineData("empty_expected_session")]
    [InlineData("whitespace_expected_session")]
    [InlineData("player_hash")]
    [InlineData("negative_revision")]
    [InlineData("negative_tick")]
    [InlineData("assessment_hash")]
    [InlineData("path_echo")]
    [InlineData("routing_mode")]
    [InlineData("null_routing_mode")]
    [InlineData("source_preservation_mode")]
    [InlineData("destination_preservation_mode")]
    [InlineData("validation_mode")]
    [InlineData("source_mode")]
    [InlineData("destination_mode")]
    [InlineData("source_hash")]
    [InlineData("destination_hash")]
    [InlineData("source_slot")]
    [InlineData("destination_slot")]
    [InlineData("filter")]
    [InlineData("sorter")]
    [InlineData("source_orientation")]
    [InlineData("destination_orientation")]
    [InlineData("planned_index")]
    [InlineData("source_position")]
    [InlineData("destination_position")]
    [InlineData("zero_existing_item")]
    [InlineData("negative_recipe")]
    [InlineData("belt_budget")]
    [InlineData("sorter_budget")]
    [InlineData("budget_direction")]
    [InlineData("extra_budget")]
    [InlineData("missing_attachment")]
    [InlineData("extra_attachment")]
    [InlineData("null_attachments")]
    [InlineData("null_attachment_row")]
    [InlineData("null_blockers")]
    [InlineData("null_budget")]
    [InlineData("null_path")]
    [InlineData("negative_without_blocker")]
    [InlineData("native_without_check")]
    [InlineData("native_blocker")]
    [InlineData("native_condition")]
    [InlineData("native_span")]
    public void RejectsTokenizedOrMismatchedReadOnlyEcho(string fault)
    {
        var request = Request();
        var plan = Plan(request.BeltEndpointPreview!);
        var echo = plan.BeltEndpointPreview!;
        var path = plan.PlannedBeltPath!;
        var source = echo.Attachments.Single(row => row.Role == "source");
        var destination = echo.Attachments.Single(row => row.Role == "destination");

        switch (fault)
        {
            case "prepared": plan.Prepared = true; break;
            case "commit_allowed": plan.CommitAllowedNow = true; break;
            case "token": plan.PlanToken = "normal-build-token"; break;
            case "action_kind": plan.ActionKind = "transfer"; break;
            case "build_kind": plan.BuildKind = "belt"; break;
            case "source_object": plan.SourceObjectId = 70; break;
            case "destination_object": plan.DestinationObjectId = 71; break;
            case "phase": echo.Phase = "normal_build"; break;
            case "planet": echo.PlanetId++; break;
            case "session": echo.SessionId = ""; break;
            case "null_session": echo.SessionId = null!; break;
            case "wrong_expected_session": echo.SessionId = "another-session"; break;
            case "player_hash": plan.ExpectedStateHash = "other-player-state-hash"; break;
            case "negative_revision": echo.Revision = -1; break;
            case "negative_tick": echo.CapturedAtGameTick = -1; break;
            case "assessment_hash": echo.AssessmentHash = ""; break;
            case "path_echo": plan.PlannedBeltPath = null; break;
            case "routing_mode": path.RoutingMode = BeltPathModes.NativeGeodesic; break;
            case "null_routing_mode": path.RoutingMode = null; break;
            case "source_preservation_mode": path.SourcePreservationMode = "unexpected-source-preservation"; break;
            case "destination_preservation_mode": path.DestinationPreservationMode = "unexpected-destination-preservation"; break;
            case "validation_mode": path.NativeValidationMode = "anchor_only_stage0"; break;
            case "source_mode": path.SourceBindingMode = "native_device_port"; break;
            case "destination_mode": path.DestinationBindingMode = "non_removing_belt_cover"; break;
            case "source_hash": source.EndpointStateHash = "stale-source"; break;
            case "destination_hash": destination.EndpointStateHash = "stale-destination"; break;
            case "source_slot": source.Attachment.SourceSlot++; break;
            case "destination_slot": destination.Attachment.DestinationSlot++; break;
            case "filter": source.FilterItemId++; break;
            case "sorter": destination.SorterItemId++; break;
            case "source_orientation": source.PlannedBeltQuarterTurns++; break;
            case "destination_orientation": destination.ExistingBeltQuarterTurns++; break;
            case "planned_index": destination.PlannedBeltIndex--; break;
            case "source_position": source.Attachment.DestinationPosition = Point(50, 180, 50); break;
            case "destination_position": destination.Attachment.SourcePosition = Point(50, 180, 50); break;
            case "zero_existing_item": source.ExistingItemId = 0; break;
            case "negative_recipe": destination.ObservedRecipeId = -1; break;
            case "belt_budget": plan.ItemBudget.Single(row => row.ItemId == 2001).Count++; break;
            case "sorter_budget": plan.ItemBudget.Single(row => row.ItemId == 2011).Count++; break;
            case "budget_direction": plan.ItemBudget[0].Direction = "construction-consumption"; break;
            case "extra_budget": plan.ItemBudget.Add(new ActionItemBudget { ItemId = 2101, Count = 1, Direction = "preview-construction-consumption" }); break;
            case "missing_attachment": echo.Attachments.Remove(source); break;
            case "extra_attachment": echo.Attachments.Add(source); break;
            case "null_attachments": echo.Attachments = null!; break;
            case "null_attachment_row": echo.Attachments.Add(null!); break;
            case "null_blockers": echo.Blockers = null!; break;
            case "null_budget": plan.ItemBudget = null!; break;
            case "null_path": plan.PlannedPath = null!; break;
            case "negative_without_blocker": echo.NativeCheckPassed = false; echo.Blockers.Clear(); break;
            case "native_without_check": echo.NativeCheckPerformed = false; break;
            case "native_blocker": echo.Blockers.Add("overlap"); break;
            case "native_condition": destination.NativeCondition = "Collision"; break;
            case "native_span": destination.NativeSpan = 4; break;
        }

        var expectedSessionId = fault switch
        {
            "empty_expected_session" => string.Empty,
            "whitespace_expected_session" => " ",
            _ => "owned-session",
        };
        Assert.False(BeltEndpointPreviewPolicy.ConfirmsReadOnlyEcho(request, plan, expectedSessionId));
    }

    [Fact]
    public void AFailedNativeCheckCanOnlyEchoAnExplicitReadOnlyFailure()
    {
        var request = Request();
        var plan = Plan(request.BeltEndpointPreview!, nativePassed: false);
        plan.BeltEndpointPreview!.Blockers.Add("native_collision");
        foreach (var row in plan.BeltEndpointPreview.Attachments)
        {
            row.NativeCondition = "Collision";
            row.NativeSpan = 0;
        }

        Assert.True(BeltEndpointPreviewPolicy.ConfirmsReadOnlyEcho(request, plan, "owned-session"));
        Assert.False(plan.Prepared);
        Assert.False(plan.CommitAllowedNow);
        Assert.Empty(plan.PlanToken);
        Assert.False(plan.BeltEndpointPreview.NativeCheckPassed);
        Assert.False(plan.BeltEndpointPreview.Executable);
    }

    [Fact]
    public void AFailedNativeCheckRequiresAtLeastOneNonEmptyBlocker()
    {
        var request = Request();
        var plan = Plan(request.BeltEndpointPreview!, nativePassed: false);
        plan.BeltEndpointPreview!.Blockers.Clear();

        Assert.False(BeltEndpointPreviewPolicy.ConfirmsReadOnlyEcho(request, plan, "owned-session"));

        plan.BeltEndpointPreview.Blockers.Add(" ");
        Assert.False(BeltEndpointPreviewPolicy.ConfirmsReadOnlyEcho(request, plan, "owned-session"));
    }

    [Fact]
    public void PrerequisiteBlockerIsAValidTokenlessAssessmentButNotANativeResult()
    {
        var request = Request();
        var plan = Plan(request.BeltEndpointPreview!, nativePassed: false);
        var preview = plan.BeltEndpointPreview!;
        preview.NativeCheckPerformed = false;
        preview.Blockers.Add("prerequisite_not_met");
        foreach (var attachment in preview.Attachments)
        {
            attachment.NativeCondition = "not_checked";
            attachment.NativeSpan = 0;
        }

        Assert.True(BeltEndpointPreviewPolicy.ConfirmsReadOnlyEcho(request, plan, "owned-session"));
        Assert.False(preview.NativeCheckPerformed);
        Assert.False(preview.NativeCheckPassed);
        Assert.All(preview.Attachments, attachment =>
        {
            Assert.Equal("not_checked", attachment.NativeCondition);
            Assert.Equal(0, attachment.NativeSpan);
        });
        Assert.False(plan.Prepared);
        Assert.False(plan.CommitAllowedNow);
        Assert.Equal(string.Empty, plan.PlanToken);
    }

    [Theory]
    [InlineData("planned_endpoint_sorter_overlap:sorter:1:sorter:0")]
    [InlineData("planned_endpoint_unlinked_belt_overlap:sorter:0:belt:1")]
    [InlineData("planned_endpoint_existing_overlap:sorter:0:object:517")]
    [InlineData("planned_endpoint_existing_overlap:sorter:0:object:-17")]
    [InlineData("planned_endpoint_prototype_unavailable:object:517:proto:2001")]
    public void PreciseOccupancyFailureRemainsNonNativeAndNeverExecutable(string blocker)
    {
        var request = Request();
        var plan = Plan(request.BeltEndpointPreview!, nativePassed: false);
        var preview = plan.BeltEndpointPreview!;
        preview.NativeCheckPerformed = false;
        preview.Blockers.Add("planned_endpoint_collision_or_prototype_unavailable");
        preview.Blockers.Add(blocker);
        foreach (var attachment in preview.Attachments)
        {
            attachment.NativeCondition = "not_checked";
            attachment.NativeSpan = 0;
        }

        Assert.True(BeltEndpointPreviewPolicy.ConfirmsReadOnlyEcho(request, plan, "owned-session"));
        Assert.False(plan.Prepared);
        Assert.False(plan.CommitAllowedNow);
        Assert.Empty(plan.PlanToken);
        Assert.False(preview.Executable);

        preview.NativeCheckPassed = true;
        Assert.False(BeltEndpointPreviewPolicy.ConfirmsReadOnlyEcho(request, plan, "owned-session"));
        preview.NativeCheckPassed = false;
        plan.PlanToken = "unexpected-write-capability";
        Assert.False(BeltEndpointPreviewPolicy.ConfirmsReadOnlyEcho(request, plan, "owned-session"));
    }

    [Theory]
    [InlineData("oversized_detail")]
    [InlineData("too_many_details")]
    public void OccupancyFailureDetailsRetainTheExistingResponseBounds(string fault)
    {
        var request = Request();
        var plan = Plan(request.BeltEndpointPreview!, nativePassed: false);
        var preview = plan.BeltEndpointPreview!;
        preview.NativeCheckPerformed = false;
        foreach (var attachment in preview.Attachments)
        {
            attachment.NativeCondition = "not_checked";
            attachment.NativeSpan = 0;
        }
        if (fault == "oversized_detail") preview.Blockers.Add(new string('x', 257));
        else preview.Blockers.AddRange(Enumerable.Repeat("planned_endpoint_existing_overlap:sorter:0:object:517", 17));

        Assert.False(BeltEndpointPreviewPolicy.ConfirmsReadOnlyEcho(request, plan, "owned-session"));
    }

    [Theory]
    [InlineData("device_in_virtual_belt_slot")]
    [InlineData("belt_in_device_slot")]
    public void RejectsEndpointItemTypeThatDoesNotMatchVirtualSlot(string fault)
    {
        var request = Request();
        var preview = request.BeltEndpointPreview!;
        preview.Destination = fault == "device_in_virtual_belt_slot"
            ? Binding(43, -1, 0, 1)
            : Binding(43, 7, 0, 1);
        var plan = Plan(preview);
        var destinationEcho = plan.BeltEndpointPreview!.Attachments.Single(a => a.Role == "destination");
        destinationEcho.ExistingItemId = fault == "device_in_virtual_belt_slot" ? 2101 : 2001;

        Assert.False(BeltEndpointPreviewPolicy.ConfirmsReadOnlyEcho(request, plan, "owned-session"));
    }

    private static PrepareBuildRequest Request(string mode = BeltPathModes.NativeGrid) => new()
    {
        PlanetId = 104,
        BuildingItemId = 2001,
        ExpectedPlayerStateHash = "player-state-hash",
        PreferredPosition = SurfacePoint(1, 2),
        PathEnd = SurfacePoint(11, 2),
        BeltPathMode = mode,
        BeltStartAltitudeLevel = mode == BeltPathModes.NativeElevatedGrid ? 0 : null,
        BeltEndAltitudeLevel = mode == BeltPathModes.NativeElevatedGrid ? 1 : null,
        BeltEndpointPreview = new BeltEndpointPreviewRequest
        {
            SorterItemId = 2011,
            FilterItemId = 1109,
            Source = Binding(41, 3, 0, 2),
            Destination = Binding(43, -1, 0, 1),
        },
    };

    private static PlannedBeltEndpointBinding Binding(int id, int slot, int existingTurns, int plannedTurns) => new()
    {
        ExistingObjectId = id,
        ExpectedEndpointStateHash = $"endpoint-{id}",
        ExistingSlot = slot,
        ExistingBeltQuarterTurns = existingTurns,
        PlannedBeltQuarterTurns = plannedTurns,
    };

    private static PreparedNormalAction Plan(BeltEndpointPreviewRequest request,
        string mode = BeltPathModes.NativeGrid, bool nativePassed = true)
    {
        const int beltCount = 4;
        var attachments = new List<PlannedBeltEndpointAttachment>();
        Add(request.Source, "source", 0);
        Add(request.Destination, "destination", beltCount - 1);
        var preview = new BeltEndpointPreviewSnapshot
        {
            Phase = BeltEndpointPreviewPolicy.Phase,
            SessionId = "owned-session",
            PlanetId = 104,
            Revision = 17,
            CapturedAtGameTick = 1234,
            NativeCheckPerformed = true,
            NativeCheckPassed = nativePassed,
            AssessmentHash = "preview-assessment-hash",
            Attachments = attachments,
        };
        var plan = new PreparedNormalAction
        {
            Prepared = false,
            ActionKind = NormalActionKinds.Build,
            PlanToken = string.Empty,
            ExpectedStateHash = "player-state-hash",
            BuildKind = "belt_endpoint_preview",
            BeltEndpointPreview = preview,
            PlannedBeltPath = new BeltPathPlanSnapshot
            {
                NativeValidationMode = "full_path_stage1",
                SourceBindingMode = "none",
                DestinationBindingMode = "none",
                NewObjectCount = beltCount,
                RoutingMode = mode,
                StartAltitudeLevel = mode == BeltPathModes.NativeElevatedGrid ? 0 : null,
                EndAltitudeLevel = mode == BeltPathModes.NativeElevatedGrid ? 1 : null,
            },
        };
        plan.PlannedPath.AddRange(Enumerable.Range(0, beltCount).Select(PathPoint));
        plan.ItemBudget.Add(new ActionItemBudget
        {
            ItemId = 2001, Count = beltCount, Direction = "preview-construction-consumption",
        });
        plan.ItemBudget.Add(new ActionItemBudget
        {
            ItemId = request.SorterItemId,
            Count = attachments.Count,
            Direction = "preview-construction-consumption",
        });
        return plan;

        void Add(PlannedBeltEndpointBinding? binding, string role, int beltIndex)
        {
            if (binding is null) return;
            var source = role == "source";
            attachments.Add(new PlannedBeltEndpointAttachment
            {
                Role = role,
                ExistingObjectId = binding.ExistingObjectId,
                EndpointStateHash = binding.ExpectedEndpointStateHash,
                ExistingItemId = binding.ExistingSlot == -1 ? 2001 : 2101,
                ObservedRecipeId = 58,
                PlannedBeltIndex = beltIndex,
                SorterItemId = request.SorterItemId,
                FilterItemId = request.FilterItemId,
                ExistingBeltQuarterTurns = binding.ExistingBeltQuarterTurns,
                PlannedBeltQuarterTurns = binding.PlannedBeltQuarterTurns,
                NativeCondition = nativePassed ? "Ok" : "Collision",
                NativeSpan = nativePassed ? 2 : 0,
                Attachment = new InserterAttachmentPlanSnapshot
                {
                    Mode = "read_only_planned_belt_exact_slots",
                    SourceSlot = source ? binding.ExistingSlot : -1,
                    DestinationSlot = source ? -1 : binding.ExistingSlot,
                    SourcePosition = source ? SurfacePoint(2, 3) : PathPoint(beltIndex),
                    DestinationPosition = source ? PathPoint(beltIndex) : SurfacePoint(3, 4),
                },
            });
        }
    }

    private static Vector3Snapshot Point(float x, float y, float z) => new() { X = x, Y = y, Z = z };

    private static Vector3Snapshot PathPoint(int index) => index switch
    {
        0 => SurfacePoint(1, 2),
        3 => SurfacePoint(11, 2),
        _ => SurfacePoint(1 + index * 3, 2 + index),
    };

    private static Vector3Snapshot SurfacePoint(float x, float z) =>
        Point(x, (float)Math.Sqrt(200 * 200 - x * x - z * z), z);
}
