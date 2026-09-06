using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

// Current native BuildingParameters storage encoding: bans, mode, reserved8, filters100.
// Cargo is deliberately absent. Reject data the native decoder would ignore or clamp.
public static class BlueprintStoragePolicy
{
    public const int ParameterCount = 110;
    public const int MaximumGridCount = 100;

    public static bool IsSupportedShape(int[]? parameters)
    {
        if (parameters is null || parameters.Length != ParameterCount
            || parameters[0] < 0 || parameters[0] > MaximumGridCount
            || (parameters[1] != 0 && parameters[1] != 9)) return false;
        for (var i = 2; i < 10; i++) if (parameters[i] != 0) return false;
        for (var i = 10; i < ParameterCount; i++)
            if (parameters[i] < 0 || parameters[i] > short.MaxValue
                || (parameters[1] == 0 && parameters[i] != 0)) return false;
        return true;
    }

    public static bool FitsNativeStorage(int[]? parameters, int gridCount, Func<int, bool> supportsFilter)
    {
        if (!IsSupportedShape(parameters) || gridCount < 1 || gridCount > MaximumGridCount
            || parameters![0] > gridCount) return false;
        for (var i = 0; i < MaximumGridCount; i++)
        {
            var filter = parameters![10 + i];
            if (filter != 0 && (i >= gridCount || !supportsFilter(filter))) return false;
        }
        return true;
    }

    public static bool FiltersUnlocked(int[] parameters, Func<int, bool> isUnlocked) =>
        IsSupportedShape(parameters) && parameters.Skip(10).All(id => id == 0 || isUnlocked(id));

    public static bool MatchesConfiguration(int[] parameters, StorageConfigurationSnapshot? actual)
    {
        if (actual is null || !FitsNativeStorage(parameters, actual.GridCount, _ => true)
            || actual.BannedGridCount != parameters[0]
            || actual.Mode != (parameters[1] == 0 ? "default" : "filtered")
            || actual.GridFilterItemIds is null || actual.GridFilterItemIds.Count != actual.GridCount) return false;
        for (var i = 0; i < actual.GridCount; i++)
            if (actual.GridFilterItemIds[i] != parameters[10 + i]) return false;
        return true;
    }
}
