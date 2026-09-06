using Spherewright.Bridge.Core.Factory;

namespace Spherewright.Plugin.Game;

internal sealed partial class GameStateReader
{
    private static List<int> GetSupportedUpgradeTargets(ItemProto source)
    {
        if (!BuildingUpgradePolicy.SupportsItem(source.ID) || !source.canUpgrade || source.Upgrades is null)
            return new List<int>();
        return source.Upgrades.Select(id => LDB.items.Select(id))
            .Where(target => target is not null && target.Upgrades is not null
                && BuildingUpgradePolicy.SupportsPair(source.ID, target.ID) && target.Grade > source.Grade
                && source.Upgrades.SequenceEqual(target.Upgrades) && target.IsUpgradeOf(source)
                && source.GetGradeItem(target.Grade)?.ID == target.ID && GameMain.history.ItemUnlocked(target.ID))
            .Select(target => target!.ID).Distinct().OrderBy(id => id).ToList();
    }
}
