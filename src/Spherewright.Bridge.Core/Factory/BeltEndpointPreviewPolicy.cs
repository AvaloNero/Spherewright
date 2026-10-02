using Spherewright.Contracts.Actions;

namespace Spherewright.Bridge.Core.Factory;

/// <summary>One read-only NEW2001 span and at most two exact existing attachments. No executor, search or token.</summary>
public static class BeltEndpointPreviewPolicy
{
    public const string Phase = "read_only_belt_endpoint_preview";
    public const int MaximumBeltObjects = 64;

    public static string? ValidateRequest(PrepareBuildRequest request)
    {
        var preview = request.BeltEndpointPreview;
        if (preview is null) return null;
        if (request.BuildingItemId != 2001
            || (request.BeltPathMode != BeltPathModes.NativeGrid && request.BeltPathMode != BeltPathModes.NativeElevatedGrid)
            || request.PreferredPosition is null || request.PathEnd is null
            || request.SourceObjectId.HasValue || request.DestinationObjectId.HasValue
            || request.ResourceNodeId.HasValue || !string.IsNullOrEmpty(request.ExpectedResourceStateHash)
            || !string.IsNullOrEmpty(request.ExpectedSourceStateHash) || !string.IsNullOrEmpty(request.ExpectedDestinationStateHash)
            || request.InitialSorterFilterItemId != 0)
            return "belt_endpoint_preview_requires_explicit_free_new_2001_span";
        if (!Valid(request.PreferredPosition) || !Valid(request.PathEnd))
            return "belt_endpoint_preview_coordinates_invalid";
        if (string.IsNullOrWhiteSpace(request.ExpectedPlayerStateHash) || request.ExpectedPlayerStateHash.Length > 256)
            return "belt_endpoint_preview_requires_fresh_player_hash";
        if (SquaredDistance(request.PreferredPosition, request.PathEnd) > 30d * 30d)
            return "belt_endpoint_preview_span_limit";
        if (preview.SorterItemId is not (2011 or 2012) || preview.FilterItemId <= 0 || preview.FilterItemId > short.MaxValue)
            return "belt_endpoint_preview_sorter_or_filter_unsupported";
        if (preview.Source is null && preview.Destination is null)
            return "belt_endpoint_preview_requires_exact_existing_binding";
        foreach (var binding in new[] { preview.Source, preview.Destination })
        {
            if (binding is null) continue;
            if (binding.ExistingObjectId <= 0 || binding.ExistingObjectId > 131072
                || string.IsNullOrWhiteSpace(binding.ExpectedEndpointStateHash) || binding.ExpectedEndpointStateHash.Length > 256
                || binding.ExistingSlot < -1 || binding.ExistingSlot > 15
                || binding.ExistingBeltQuarterTurns < 0 || binding.ExistingBeltQuarterTurns > 3
                || binding.PlannedBeltQuarterTurns < 0 || binding.PlannedBeltQuarterTurns > 3
                || (binding.ExistingSlot != -1 && binding.ExistingBeltQuarterTurns != 0))
                return "belt_endpoint_preview_binding_invalid";
        }
        return preview.Source?.ExistingObjectId == preview.Destination?.ExistingObjectId
            ? "belt_endpoint_preview_self_route_unsupported" : null;
    }

    // A mixed-cohort Plugin can ignore an additive field and return a normal token.
    // That response MUST be withheld rather than downgraded to regular construction.
    public static bool ConfirmsReadOnlyEcho(PrepareBuildRequest request, PreparedNormalAction? result, string expectedSessionId)
    {
        if (request.BeltEndpointPreview is not { } expected || ValidateRequest(request) is not null
            || result is null || result.Prepared || result.CommitAllowedNow || !string.IsNullOrEmpty(result.PlanToken)
            || result.BuildKind != "belt_endpoint_preview" || result.ActionKind != "build"
            || result.ExpectedStateHash != request.ExpectedPlayerStateHash
            || result.SourceObjectId.HasValue || result.DestinationObjectId.HasValue
            || result.BeltEndpointPreview is not { } actual || actual.Phase != Phase || actual.Executable
            || actual.PlanetId != request.PlanetId || string.IsNullOrWhiteSpace(actual.SessionId)
            || actual.Revision < 0 || actual.CapturedAtGameTick < 0
            || string.IsNullOrWhiteSpace(expectedSessionId) || actual.SessionId != expectedSessionId
            || string.IsNullOrWhiteSpace(actual.AssessmentHash) || actual.AssessmentHash.Length > 256
            || actual.Attachments is null || actual.Blockers is null || result.ItemBudget is null
            || actual.Attachments.Any(a => a is null) || actual.Blockers.Count > 16
            || actual.Blockers.Any(b => string.IsNullOrWhiteSpace(b) || b.Length > 256)
            || result.PlannedPath is null || result.PlannedPath.Any(p => !Valid(p))
            || result.PlannedBeltPath is not { } path || path.NativeValidationMode != "full_path_stage1"
            || path.RoutingMode != request.BeltPathMode
            || path.NewObjectCount < 2 || path.NewObjectCount > MaximumBeltObjects
            || path.SourceBindingMode != "none" || path.DestinationBindingMode != "none"
            || path.ReusedSourceObjectId.HasValue || path.ReusedDestinationObjectId.HasValue
            || path.SourcePreservationMode is not null || path.DestinationPreservationMode is not null
            || result.PlannedPath.Count != path.NewObjectCount
            || !BeltPathRoutingPolicy.ConfirmsPlanEcho(request.BeltPathMode, path)
            || (request.BeltPathMode == BeltPathModes.NativeElevatedGrid
                && !BeltElevationPolicy.ConfirmsPlanEcho(request.BeltStartAltitudeLevel, request.BeltEndAltitudeLevel, path))) return false;
        var bindingCount = (expected.Source is null ? 0 : 1) + (expected.Destination is null ? 0 : 1);
        if (actual.Attachments.Count != bindingCount || result.ItemBudget.Count != 2
            || result.ItemBudget.Any(i => i is null || i.Direction != "preview-construction-consumption")
            || result.ItemBudget.Count(i => i.ItemId == 2001 && i.Count == path.NewObjectCount) != 1
            || result.ItemBudget.Count(i => i.ItemId == expected.SorterItemId && i.Count == bindingCount) != 1) return false;
        foreach (var role in new[] { "source", "destination" })
        {
            var binding = role == "source" ? expected.Source : expected.Destination;
            var rows = actual.Attachments.Where(a => a.Role == role).ToArray();
            if (binding is null) { if (rows.Length != 0) return false; continue; }
            if (rows.Length != 1) return false;
            var a = rows[0]; var source = role == "source";
            if (a.ExistingObjectId != binding.ExistingObjectId || a.EndpointStateHash != binding.ExpectedEndpointStateHash
                || a.ExistingItemId <= 0 || a.ObservedRecipeId < 0 || a.Attachment is null
                || (binding.ExistingSlot == -1) != (a.ExistingItemId >= 2001 && a.ExistingItemId <= 2003)
                || a.SorterItemId != expected.SorterItemId || a.FilterItemId != expected.FilterItemId
                || a.PlannedBeltIndex != (source ? 0 : path.NewObjectCount - 1)
                || a.ExistingBeltQuarterTurns != binding.ExistingBeltQuarterTurns
                || a.PlannedBeltQuarterTurns != binding.PlannedBeltQuarterTurns
                || a.Attachment.Mode != "read_only_planned_belt_exact_slots"
                || a.Attachment.SourceSlot != (source ? binding.ExistingSlot : -1)
                || a.Attachment.DestinationSlot != (source ? -1 : binding.ExistingSlot)
                || a.Attachment.InputOffset != 0 || a.Attachment.OutputOffset != 0
                || !Valid(a.Attachment.SourcePosition) || !Valid(a.Attachment.DestinationPosition)
                || !SamePoint(source ? a.Attachment.DestinationPosition : a.Attachment.SourcePosition,
                    result.PlannedPath[a.PlannedBeltIndex])) return false;
        }
        // A prerequisite blocker may prevent the native sorter check from running.
        // Its explicit flags describe that fact; this is not a known native rejection
        // or a positive. Here we validate a tokenless assessment, not placement approval.
        return !actual.NativeCheckPassed ? actual.Blockers.Count > 0 : (actual.NativeCheckPerformed && actual.Blockers.Count == 0
            && actual.Attachments.All(a => a.NativeCondition == "Ok" && a.NativeSpan >= 1 && a.NativeSpan <= 3));
    }

    private static bool Valid(Spherewright.Contracts.Factory.Vector3Snapshot? point) => point is not null
        && new[] { point.X, point.Y, point.Z }.All(f => !float.IsNaN(f) && !float.IsInfinity(f) && Math.Abs(f) <= 10000)
        && (double)point.X * point.X + (double)point.Y * point.Y + (double)point.Z * point.Z >= 1;

    private static bool SamePoint(Spherewright.Contracts.Factory.Vector3Snapshot a, Spherewright.Contracts.Factory.Vector3Snapshot b) =>
        Math.Abs(a.X - b.X) <= .001f && Math.Abs(a.Y - b.Y) <= .001f && Math.Abs(a.Z - b.Z) <= .001f;

    private static double SquaredDistance(Spherewright.Contracts.Factory.Vector3Snapshot a, Spherewright.Contracts.Factory.Vector3Snapshot b) =>
        ((double)a.X - b.X) * (a.X - b.X) + ((double)a.Y - b.Y) * (a.Y - b.Y) + ((double)a.Z - b.Z) * (a.Z - b.Z);
}
