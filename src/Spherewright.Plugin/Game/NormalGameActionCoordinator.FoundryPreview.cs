using UnityEngine;

namespace Spherewright.Plugin.Game;

internal sealed partial class NormalGameActionCoordinator
{
    internal static FoundryNativePreviewEvidence InspectFoundryMachine(
        PlanetFactory factory, Player player, ItemProto item, Vector3 position, float yaw)
    {
        var evidence = new FoundryNativePreviewEvidence();
        // The CURRENT structured build path includes the occupied-object
        // collider guard that an isolated native preview alone can miss.
        var candidate = BuildStepPlan.Core(item.ID, position, Maths.SphericalRotation(position, yaw), yaw);
        evidence.Passed = TryValidateClickBuild(factory, player, item, candidate, 0,
            out var accepted, out var rejection, evidence);
        if (evidence.Passed && (Vector3.Distance(accepted.Position, candidate.Position) > 0.001f
            || Quaternion.Angle(accepted.Rotation, candidate.Rotation) > 0.01f))
        {
            evidence.Passed = false;
            rejection = "The native preview adjusted the candidate pose; this machine grid requires a new explicit assessment.";
        }
        evidence.Rejection = string.IsNullOrEmpty(rejection) ? null : rejection;
        return evidence;
    }
}

internal sealed class FoundryNativePreviewEvidence
{
    internal bool NativeCheckPerformed { get; set; }
    internal bool Passed { get; set; }
    internal string? Condition { get; set; }
    internal string? Rejection { get; set; }
    internal int? OccupiedObjectId { get; set; }
}
