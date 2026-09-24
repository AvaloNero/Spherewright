using Spherewright.Contracts.Journals;

namespace Spherewright.Bridge.Core.Safety;

/// <summary>Researched, directional native migration; never a wildcard version bypass.</summary>
public static class OwnedWorldVersionCompatibilityPolicy
{
    public const string SourceVersion = "0.10.34.28529";
    public const string TargetVersion = "0.10.35.29057";

    public static bool IsSupportedMigration(string source, string target) =>
        source == SourceVersion && target == TargetVersion;

    public static bool AllowsReauthorization(string source, string target) =>
        !string.IsNullOrWhiteSpace(source) && (source == target || IsSupportedMigration(source, target));

    // GameVersion remains the journal's origin. Only an explicit durable transition
    // advances its effective version; an old document alone cannot authorize new runtime use.
    public static bool JournalMatches(string origin, IReadOnlyList<GameplayJournalVersionTransition>? transitions,
        long entryCount, string expected)
    {
        if (string.IsNullOrWhiteSpace(origin) || transitions is null || entryCount < 0) return false;
        if (transitions.Count == 0) return origin == expected;
        if (transitions.Count != 1) return false;
        var change = transitions[0];
        return change is not null && change.FromGameVersion == origin
            && IsSupportedMigration(change.FromGameVersion, change.ToGameVersion)
            && change.ToGameVersion == expected && change.AdoptedAtGameTick >= 0
            && change.DurableThroughSequence >= 0 && change.DurableThroughSequence <= entryCount
            && DateTimeOffset.TryParse(change.RecordedAtUtc, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind, out _);
    }
}
