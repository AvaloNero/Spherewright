using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

public static class FoundrySitePlanner
{
    public const int MaximumMachines = 32;
    public const float MaximumOffset = 64f;
    public const float Clearance = 0.5f;

    public static float? ColliderBoundingRadius(string shape, Vector3Snapshot center, Vector3Snapshot extent, float radius)
    {
        if (!Finite(center) || !Finite(extent) || !Finite(radius) || radius < 0f) return null;
        double bound;
        switch (shape)
        {
            case "Box":
                if (extent.X < 0f || extent.Y < 0f || extent.Z < 0f) return null;
                bound = Length(center) + Length(extent);
                break;
            case "Capsule":
                // DSP ext is an oriented half-segment, not box half-extents.
                bound = Length(center) + Length(extent) + radius;
                break;
            case "Sphere":
                bound = Length(center) + radius;
                break;
            default:
                return null;
        }
        var value = (float)bound;
        return Finite(value) && value > 0f ? value : null;
    }

    public static void ValidateRequest(FoundrySiteRequest request)
    {
        if (request is null || !Finite(request.Origin) || Length(request.Origin) < 1d
            || Length(request.Origin) > 10000d || !Finite(request.YawDegrees)
            || request.YawDegrees < 0f || request.YawDegrees >= 360f
            || request.Columns < 1 || request.Columns > 8
            || !Finite(request.ColumnSpacing) || request.ColumnSpacing < 4f || request.ColumnSpacing > 32f
            || !Finite(request.RowSpacing) || request.RowSpacing < 4f || request.RowSpacing > 32f)
            throw Reject("invalid_site", "An explicit finite surface origin, yaw in [0, 360), 1–8 columns and spacings in [4, 32] metres are required.");
    }

    // The adapter supplies DSP's spherical frame; no game geometry is guessed
    // here. Final grid snapping and native conditions are adapter-only work.
    public static FoundrySiteSnapshot CreateCandidates(FoundryPlanSnapshot plan, FoundrySiteRequest request,
        IReadOnlyList<BuildCatalogItem> buildings, Vector3Snapshot right, Vector3Snapshot forward)
    {
        ValidateRequest(request);
        if (plan is null || plan.Stages is null || plan.MachineCount < 1 || plan.MachineCount > MaximumMachines
            || plan.Stages.Count < 1 || plan.Stages.Count > MaximumMachines
            || plan.Stages.Any(s => s is null || s.MachineCount < 1 || s.MachineCount > MaximumMachines
                || string.IsNullOrWhiteSpace(s.StageId) || s.BuildingItemId <= 0 || s.RecipeId <= 0)
            || plan.Stages.Sum(s => s.MachineCount) != plan.MachineCount
            || plan.Stages.Select(s => s.StageId).Distinct(StringComparer.Ordinal).Count() != plan.Stages.Count
            || plan.PlanetId <= 0 || string.IsNullOrWhiteSpace(plan.SessionId) || string.IsNullOrWhiteSpace(plan.PlanHash))
            throw Reject("site_machine_limit", "Site previews require a valid material plan with at most 32 production machines.");
        if (buildings is null || buildings.Count > 512 || buildings.Any(b => b is null)
            || buildings.Select(b => b.ItemId).Distinct().Count() != buildings.Count)
            throw Reject("catalog_invalid", "The site building catalog must be bounded with unique identities.");
        var radius = Length(request.Origin);
        if (!Finite(right) || !Finite(forward) || Math.Abs(Length(right) - 1d) > 0.001d
            || Math.Abs(Length(forward) - 1d) > 0.001d || Math.Abs(Dot(right, forward)) > 0.001d
            || Math.Abs(Dot(right, request.Origin) / radius) > 0.001d
            || Math.Abs(Dot(forward, request.Origin) / radius) > 0.001d)
            throw Reject("invalid_site_frame", "The runtime frame must be orthonormal and tangent to the supplied surface origin.");

        var columns = Math.Min(request.Columns, plan.MachineCount);
        var rows = (plan.MachineCount + columns - 1) / columns;
        var site = new FoundrySiteSnapshot
        {
            SessionId = plan.SessionId, PlanetId = plan.PlanetId, CapturedAtGameTick = plan.CapturedAtGameTick,
            MaterialPlanHash = plan.PlanHash, Origin = Copy(request.Origin), YawDegrees = request.YawDegrees,
            Columns = columns, ColumnSpacing = request.ColumnSpacing, RowSpacing = request.RowSpacing,
        };
        foreach (var stage in plan.Stages)
        {
            var building = buildings.SingleOrDefault(b => b.ItemId == stage.BuildingItemId);
            if (building?.PlacementRadius is not float footprint || !Finite(footprint) || footprint <= 0f || footprint > 64f)
                throw Reject("unknown_footprint", "Every selected machine needs a finite current build-collider bounding radius.");
            for (var index = 0; index < stage.MachineCount; index++)
            {
                var ordinal = site.Machines.Count;
                var x = (ordinal % columns - (columns - 1) / 2d) * request.ColumnSpacing;
                var y = (ordinal / columns - (rows - 1) / 2d) * request.RowSpacing;
                if (Math.Sqrt(x * x + y * y) > MaximumOffset)
                    throw Reject("site_extent_limit", "Every machine must be within 64 tangent metres of the chosen origin.");
                var position = new Vector3Snapshot
                {
                    X = (float)(request.Origin.X + right.X * x + forward.X * y),
                    Y = (float)(request.Origin.Y + right.Y * x + forward.Y * y),
                    Z = (float)(request.Origin.Z + right.Z * x + forward.Z * y),
                };
                var scale = radius / Length(position);
                position.X = (float)(position.X * scale);
                position.Y = (float)(position.Y * scale);
                position.Z = (float)(position.Z * scale);
                site.Machines.Add(new FoundrySiteMachine
                {
                    PlacementId = stage.StageId + "/machine-" + (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    StageId = stage.StageId, MachineIndex = index + 1, BuildingItemId = stage.BuildingItemId,
                    RecipeId = stage.RecipeId, Position = position, YawDegrees = request.YawDegrees,
                    PlacementRadius = footprint,
                });
            }
        }
        return site;
    }

    // Must run AFTER native grid snapping: separated requested points may snap
    // onto one cell. Bounding spheres deliberately reject some tight layouts.
    public static void CheckPlannedClearance(FoundrySiteSnapshot site)
    {
        ValidateMachines(site);
        foreach (var machine in site.Machines) machine.OverlappingPlacementIds.Clear();
        for (var i = 0; i < site.Machines.Count; i++)
            for (var j = i + 1; j < site.Machines.Count; j++)
            {
                var a = site.Machines[i]; var b = site.Machines[j];
                var distance = Distance(a.Position, b.Position);
                if (distance <= a.PlacementRadius + b.PlacementRadius + Clearance)
                {
                    a.OverlappingPlacementIds.Add(b.PlacementId);
                    b.OverlappingPlacementIds.Add(a.PlacementId);
                }
            }
    }

    public static void CompleteAssessment(FoundrySiteSnapshot site, IReadOnlyDictionary<int, int> packageCounts,
        long revision, string playerActionStateHash)
    {
        ValidateMachines(site);
        if (packageCounts is null || packageCounts.Count > 12000 || packageCounts.Any(p => p.Key <= 0 || p.Value < 0)
            || revision < 0 || string.IsNullOrWhiteSpace(playerActionStateHash))
            throw Reject("invalid_site_evidence", "A fresh revision, player action hash and nonnegative package counts are required.");
        CheckPlannedClearance(site);
        site.Revision = revision;
        site.MachineInventory = site.Machines.GroupBy(m => m.BuildingItemId).OrderBy(g => g.Key)
            .Select(g => new FoundryInventoryBudget
            {
                ItemId = g.Key, RequiredCount = g.Count(),
                PackageCount = packageCounts.TryGetValue(g.Key, out var count) ? count : 0,
                MissingCount = Math.Max(0, g.Count() - (packageCounts.TryGetValue(g.Key, out var available) ? available : 0)),
            }).ToList();
        site.MachineInventorySufficient = site.MachineInventory.All(b => b.MissingCount == 0);
        site.MachinePreviewsClear = site.Machines.All(m => m.NativeCheckPerformed && m.NativeCheckPassed
            && m.NativeBuildCondition == "Ok" && m.OccupiedObjectId is null && m.OverlappingPlacementIds.Count == 0);
        site.Status = site.MachinePreviewsClear && site.MachineInventorySufficient ? "machine_previews_clear" : "blocked";
        var fields = new List<object?> { site.SchemaVersion, site.SessionId, site.PlanetId, site.Revision,
            site.MaterialPlanHash, playerActionStateHash, site.ClearanceBasis, site.Status,
            F(site.Origin.X), F(site.Origin.Y), F(site.Origin.Z), F(site.YawDegrees),
            site.Columns, F(site.ColumnSpacing), F(site.RowSpacing) };
        foreach (var m in site.Machines)
        {
            fields.Add(CanonicalStateHash.Combine("machine", m.PlacementId, m.StageId, m.MachineIndex,
                m.BuildingItemId, m.RecipeId, F(m.Position.X), F(m.Position.Y), F(m.Position.Z),
                F(m.YawDegrees), F(m.PlacementRadius), m.NativeCheckPerformed, m.NativeCheckPassed, m.NativeBuildCondition, m.OccupiedObjectId));
            foreach (var other in m.OverlappingPlacementIds.OrderBy(id => id, StringComparer.Ordinal))
                fields.Add(CanonicalStateHash.Combine("overlap", m.PlacementId, other));
        }
        foreach (var b in site.MachineInventory)
            fields.Add(CanonicalStateHash.Combine("package-budget", b.ItemId, b.RequiredCount, b.PackageCount, b.MissingCount));
        site.AssessmentHash = CanonicalStateHash.Combine("foundry-site-assessment-v1", fields.ToArray());
    }

    private static void ValidateMachines(FoundrySiteSnapshot site)
    {
        if (site is null || site.Machines is null || site.Machines.Count < 1 || site.Machines.Count > MaximumMachines
            || !Finite(site.Origin) || string.IsNullOrWhiteSpace(site.SessionId) || site.PlanetId <= 0
            || string.IsNullOrWhiteSpace(site.MaterialPlanHash)
            || site.Machines.Any(m => m is null || !Finite(m.Position) || Length(m.Position) < 1d
                || Length(m.Position) > 10000d || !Finite(m.YawDegrees) || m.YawDegrees < 0 || m.YawDegrees >= 360
                || m.BuildingItemId <= 0 || m.RecipeId <= 0 || m.MachineIndex < 1 || !Finite(m.PlacementRadius)
                || m.PlacementRadius <= 0 || m.PlacementRadius > 64f || m.OverlappingPlacementIds is null
                || string.IsNullOrWhiteSpace(m.PlacementId))
            || site.Machines.Select(m => m.PlacementId).Distinct(StringComparer.Ordinal).Count() != site.Machines.Count)
            throw Reject("invalid_site_evidence", "Machine evidence must have bounded, distinct placements with finite positions and radii.");
    }

    public static bool Finite(Vector3Snapshot? p) => p is not null && Finite(p.X) && Finite(p.Y) && Finite(p.Z);
    private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    private static double Dot(Vector3Snapshot a, Vector3Snapshot b) => (double)a.X * b.X + (double)a.Y * b.Y + (double)a.Z * b.Z;
    private static double Length(Vector3Snapshot p) => Math.Sqrt(Dot(p, p));
    private static double Distance(Vector3Snapshot a, Vector3Snapshot b) => Math.Sqrt(
        Math.Pow((double)a.X - b.X, 2) + Math.Pow((double)a.Y - b.Y, 2) + Math.Pow((double)a.Z - b.Z, 2));
    private static Vector3Snapshot Copy(Vector3Snapshot p) => new Vector3Snapshot { X = p.X, Y = p.Y, Z = p.Z };
    private static string F(float v) => v.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
    private static FoundryPlanningException Reject(string reason, string message) => new FoundryPlanningException(reason, message);
}
