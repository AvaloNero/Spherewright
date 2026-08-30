namespace Spherewright.Plugin.Game;

internal sealed class SpherewrightPathBuildTool : BuildTool_Path
{
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
        tmpPackage?.Free();
        tmpPackage = null;
    }
}
