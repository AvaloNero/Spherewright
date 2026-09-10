using Spherewright.Contracts.Progression;

namespace Spherewright.Bridge.Core.Progression;

public static class TechCompletionRewardCatalog
{
    public const int MaximumRewardEntries = 32;

    // Call on the game thread with native arrays and item lookup; only copied DTOs leave it.
    // Fail the whole field closed instead of silently truncating malformed/unsupported metadata.
    public static List<TechCompletionItemReward>? Capture(
        IReadOnlyList<int>? itemIds,
        IReadOnlyList<int>? counts,
        Func<int, string?> resolveItemName)
    {
        if (resolveItemName is null) throw new ArgumentNullException(nameof(resolveItemName));
        if (itemIds is null || counts is null || itemIds.Count != counts.Count
            || itemIds.Count > MaximumRewardEntries)
            return null;

        var rewards = new List<TechCompletionItemReward>(itemIds.Count);
        for (var index = 0; index < itemIds.Count; index++)
        {
            var itemId = itemIds[index];
            var count = counts[index];
            if (itemId <= 0 || count <= 0) return null;
            var name = resolveItemName(itemId);
            if (string.IsNullOrWhiteSpace(name)) return null;
            // Preserve native order and repeated entries; they are separate native grant calls.
            rewards.Add(new TechCompletionItemReward { ItemId = itemId, Name = name!, Count = count });
        }

        return rewards;
    }
}
