using System.Reflection;

namespace Spherewright.Plugin.Game;

// Native condition checking updates the build tooltip/cursor even when no UI tool was
// opened. Restore this cosmetic preview state; never touch movement input or game data.
internal sealed class NativeBuildPreviewUiScope : IDisposable
{
    private static readonly FieldInfo CursorIndex = typeof(UICursor).GetField("cursorIndex", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new MissingFieldException("Native build cursor layout is unsupported.");
    private readonly int _cursor = (int)CursorIndex.GetValue(null);
    private readonly int _state = GameMain.mainPlayer.controller.actionBuild.model.cursorState;
    private readonly string _text = GameMain.mainPlayer.controller.actionBuild.model.cursorText;
    private readonly int _gridLength = UIRoot.instance.uiGame.inserterBuildTip.gridLen;

    public void Dispose()
    {
        UICursor.SetCursor((ECursor)_cursor);
        GameMain.mainPlayer.controller.actionBuild.model.cursorState = _state;
        GameMain.mainPlayer.controller.actionBuild.model.cursorText = _text;
        UIRoot.instance.uiGame.inserterBuildTip.gridLen = _gridLength;
    }
}
