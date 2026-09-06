using System.Reflection;
using Spherewright.Bridge.Core.Factory;
using UnityEngine;

namespace Spherewright.Plugin.Game;

// BuildTool.controller has a native private setter. Rebind ONLY our unregistered
// tool; never replace player.controller, GameData, or the real player's command.
// PlayerController is a MonoBehaviour: do not use new/MemberwiseClone/uninitialized
// allocation. The owned temporary host is inactive before AddComponent, disabled,
// never initialized as a player and destroyed synchronously before this frame ends.
internal sealed class NativePathCommandScope : IDisposable
{
    private static readonly MethodInfo ControllerSetter = typeof(BuildTool).GetProperty("controller")?.GetSetMethod(true)
        ?? throw new MissingMethodException("Native build-controller binding is unsupported.");
    private readonly BuildTool _tool;
    private readonly PlayerController _real;
    private readonly CommandState _original;
    private readonly GameObject _host;
    private readonly PlayerController _detached;
    private bool _disposed;

    internal NativePathCommandScope(BuildTool tool)
    {
        _tool = tool;
        _real = tool.controller;
        if (!ReferenceEquals(tool.player, GameMain.mainPlayer)
            || !ReferenceEquals(_real, GameMain.mainPlayer.controller) || tool.active)
            throw new InvalidOperationException("Only an inactive local player's private build tool can use detached path validation.");
        _original = _real.cmd;
        _host = new GameObject("Spherewright.BoundedPathPreview");
        try
        {
            _host.hideFlags = HideFlags.HideAndDontSave;
            _host.SetActive(false);
            _detached = _host.AddComponent<PlayerController>();
            _detached.enabled = false;
            // Independent value, not a copy of raycast/input/player references.
            _detached.cmd = new CommandState { stage = BeltPathNativeStagePolicy.FullPathStage };
            ControllerSetter.Invoke(tool, new object[] { _detached });
            AssertBound();
        }
        catch
        {
            try { ControllerSetter.Invoke(tool, new object[] { _real }); }
            finally { UnityEngine.Object.DestroyImmediate(_host); }
            throw;
        }
    }

    internal void AssertBound()
    {
        if (_disposed || !ReferenceEquals(_tool.controller, _detached)
            || !BeltPathNativeStagePolicy.CanCheck(!ReferenceEquals(_detached, _real),
                _host.activeInHierarchy, _detached.enabled, _detached.cmd.stage))
            throw new InvalidOperationException("Full native path checking requires an inactive detached stage1 context.");
        AssertPlayerCommandUnchanged();
    }

    internal void AssertPlayerCommandUnchanged()
    {
        if (!ReferenceEquals(GameMain.mainPlayer.controller, _real) || !_real.cmd.Equals(_original))
            throw new InvalidOperationException("Native path work unexpectedly changed the player's command; do not retry construction.");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            // This assertion never restores/overwrites a player's unexpected change.
            AssertPlayerCommandUnchanged();
        }
        finally
        {
            try { ControllerSetter.Invoke(_tool, new object[] { _real }); }
            finally { UnityEngine.Object.DestroyImmediate(_host); }
        }
    }
}
