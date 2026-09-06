namespace Spherewright.Plugin.Game;

internal sealed class SpherewrightPathBuildTool : BuildTool_Path
{
    private NativePathCommandScope? _commandScope;

    public bool CheckFullPathConditions()
    {
        _commandScope ??= new NativePathCommandScope(this);
        _commandScope.AssertBound();
        bool valid;
        using (var ui = new NativeBuildPreviewUiScope()) valid = CheckBuildConditions();
        _commandScope.AssertPlayerCommandUnchanged();
        return valid;
    }

    public void CreatePrebuildsWithDetachedCommand()
    {
        if (_commandScope is null) throw new InvalidOperationException("The native path was not fully checked.");
        _commandScope.AssertBound();
        CreatePrebuilds();
        _commandScope.AssertPlayerCommandUnchanged();
    }

    public bool SnapshotPlayerInventory()
    {
        if (tmpPackage is null)
        {
            tmpPackage = new StorageComponent(player.package.size);
        }

        if (tmpPackage.size != player.package.size)
        {
            tmpPackage.SetSize(player.package.size);
        }

        Array.Copy(player.package.grids, tmpPackage.grids, tmpPackage.size);
        tmpInhandId = player.inhandItemId;
        tmpInhandCount = player.inhandItemCount;
        return true;
    }

    public void ReleaseSnapshot()
    {
        try { tmpPackage?.Free(); }
        finally
        {
            tmpPackage = null;
            var scope = _commandScope;
            _commandScope = null;
            scope?.Dispose();
        }
    }
}
