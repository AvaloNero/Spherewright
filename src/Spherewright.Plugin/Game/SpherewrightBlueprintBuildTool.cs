using System.Reflection;
using UnityEngine;

namespace Spherewright.Plugin.Game;

// Tool-owned data only. Do not open/register this as the player's active UI tool.
// The native UI initializer allocates a visible anchor/GPUI buffers and global event hooks;
// native placement maths and condition checks do not need those rendering resources.
internal sealed class SpherewrightBlueprintBuildTool : BuildTool_BlueprintPaste, IDisposable
{
    private static readonly FieldInfo ReformGridIds = typeof(BuildTool_BlueprintPaste)
        .GetField("reformGridIds", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException("Native blueprint tool layout is unsupported.");

    protected override void _OnInit()
    {
        dotsSnapped = new Vector3[1];
        dotsCursor = 1;
        bpGratBoxArr = new BPGratBox[8];
        bpGratBoxConditionArr = new IntVector4[8];
        // This is the exact tool-local initialization made by native _OnOpen. It is NOT
        // a game/world field, and only accumulates rejected terrain cells during checking.
        ReformGridIds.SetValue(this, new HashSet<int>());
        useReforms = false;
        autoBuryBase = false;
    }

    protected override void _OnFree()
    {
        foreach (var preview in bpPool ?? Array.Empty<BuildPreview>()) preview?.Free();
        bpPool = null;
        bpCursor = 0;
        dotsSnapped = null;
        bpGratBoxArr = null;
        bpGratBoxConditionArr = null;
        blueprint = null;
    }

    internal void Translate(BlueprintData data, Vector3 requestedPosition, int quarterTurns)
    {
        blueprint = data;
        yaw = quarterTurns * 90f;
        castGroundPosSnapped = planet.aux.Snap(requestedPosition, true);
        dotsSnapped[0] = BlueprintUtils.RecalculateCursorPos(castGroundPosSnapped, yaw, data, 0, segment);
        gratBoxCursor = BlueprintUtils.GenerateAreaGratBoxByBPData(data, bpGratBoxArr,
            bpGratBoxConditionArr, dotsSnapped, 1, yaw, segment);
        bpCursor = BlueprintUtils.InitBuildPreviewByBPData(data, ref bpPool, 1);
        // false never enters native platform/reform preparation. Native translation sets
        // every pose and its tropic condition, including sorter slot endpoint adjustment.
        BlueprintUtils.RefreshBuildPreview(planet, data, bpPool, ref previewReform, false,
            dotsSnapped, 1, bpGratBoxConditionArr, yaw, false, segment);
    }

    internal bool CheckNewObjects()
    {
        using (var ui = new NativeBuildPreviewUiScope())
            return CheckBuildConditionsPrestage() && CheckBuildConditions();
    }

    public void Dispose() => _Free();
}
