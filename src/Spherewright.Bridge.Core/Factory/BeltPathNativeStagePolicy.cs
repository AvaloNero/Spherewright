namespace Spherewright.Bridge.Core.Factory;

/// <summary>The full native path check is stage1, never the stage0 anchor preview.
/// Its command container must be inactive and detached from the real player.</summary>
public static class BeltPathNativeStagePolicy
{
    public const int FullPathStage = 1;

    public static bool CanCheck(bool detached, bool activeInHierarchy, bool componentEnabled, int stage) =>
        detached && !activeInHierarchy && !componentEnabled && stage == FullPathStage;
}
