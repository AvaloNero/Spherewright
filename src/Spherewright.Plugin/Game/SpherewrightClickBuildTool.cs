namespace Spherewright.Plugin.Game;

internal sealed class SpherewrightClickBuildTool : BuildTool_Click
{
    public bool SnapshotPlayerInventory(int additionalItemId = 0, int additionalItemCount = 0)
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
        if (additionalItemId <= 0 || additionalItemCount <= 0)
        {
            return true;
        }

        var added = tmpPackage.AddItemStacked(additionalItemId, additionalItemCount, 0, out var remainingInc);
        return added == additionalItemCount && remainingInc == 0;
    }

    public void ReleaseSnapshot()
    {
        tmpPackage?.Free();
        tmpPackage = null;
    }
}
