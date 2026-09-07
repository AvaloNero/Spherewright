using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

/// <summary>Only a current bounded native-renderer derivation can justify a belt rotation change.</summary>
public static class BeltSourceRotationPolicy
{
    public const string PreservationMode = "whole_path_native_rotation_v1";

    public static bool CanReadSegment(int pathId, int pathLength, int positionCapacity, int start, int count) =>
        pathId > 0 && pathLength > 0 && pathLength <= 8192 && positionCapacity >= pathLength
        && start >= 0 && count > 0 && (long)start + count <= pathLength;

    // No permissive angle tolerance: these are results of the same current native
    // Quaternion.LookRotation call on the same two current path points. q/-q are
    // physically identical; neither NaN nor an unnormalised zero value is proof.
    public static bool MatchesNativeRotation(QuaternionSnapshot? actual, QuaternionSnapshot? derived) =>
        Valid(actual) && Valid(derived)
        && ((actual!.X == derived!.X && actual.Y == derived.Y && actual.Z == derived.Z && actual.W == derived.W)
            || (actual!.X == -derived!.X && actual.Y == -derived.Y && actual.Z == -derived.Z && actual.W == -derived.W));

    private static bool Valid(QuaternionSnapshot? q)
    {
        if (q is null || !Finite(q.X) || !Finite(q.Y) || !Finite(q.Z) || !Finite(q.W)) return false;
        var lengthSquared = (double)q.X*q.X + (double)q.Y*q.Y + (double)q.Z*q.Z + (double)q.W*q.W;
        return lengthSquared > .999 && lengthSquared < 1.001;
    }
    private static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
}
