using Spherewright.Contracts.Errors;

namespace Spherewright.Bridge.Core.Factory;

/// <summary>Describes a native rejection; never approves a path or replaces its validation.</summary>
public static class BeltBuildRejectionPolicy
{
    public const string InventoryRecovery = "Do not retry with unchanged inventory. Obtain the missing belts through normal "
        + "handcraft or transfer, then fresh-read and revalidate the complete path with the same explicit endpoint bindings. "
        + "NotEnoughItem does not prove placement: later native range, terrain, collision and connection checks may not have run. "
        + "Moving closer, dropping endpoints or reusing an old token cannot resolve a material shortage.";

    public static BridgeError? DescribeInventoryShortage(
        IReadOnlyList<string>? nativeConditions, bool sourceCoverMatches, bool unexpectedNewObjectCover)
    {
        if (!sourceCoverMatches || unexpectedNewObjectCover || nativeConditions is null
            || nativeConditions.Count < 2 || nativeConditions.Count > BeltBuildOccupancyPolicy.MaximumPathPoints)
            return null;

        var missing = false;
        foreach (var condition in nativeConditions)
        {
            if (string.Equals(condition, "NotEnoughItem", StringComparison.Ordinal)) missing = true;
            else if (!string.Equals(condition, "Ok", StringComparison.Ordinal)) return null;
        }

        return missing ? BridgeError.Create(BridgeErrorCodes.InventoryInsufficient,
            "DSP belt-path validation returned NotEnoughItem; the complete path is not placement-approved.",
            true, InventoryRecovery) : null;
    }
}
