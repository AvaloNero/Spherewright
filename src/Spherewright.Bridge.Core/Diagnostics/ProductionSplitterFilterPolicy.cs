namespace Spherewright.Bridge.Core.Diagnostics;

public static class ProductionSplitterFilterPolicy
{
    public static bool AllowsItem(
        int filterItemId,
        bool isPriorityOutput,
        int itemId)
    {
        if (filterItemId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(filterItemId));
        }

        if (itemId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemId));
        }

        if (filterItemId == 0)
        {
            return true;
        }

        return isPriorityOutput
            ? itemId == filterItemId
            : itemId != filterItemId;
    }
}
