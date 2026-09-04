namespace Spherewright.Bridge.Core.Safety;

public static class UserSaveImportSafetyPolicy
{
    public static bool IsEnabled(bool allowWrites, bool allowUserSaveImport) =>
        allowWrites && allowUserSaveImport;

    public static bool MatchesPreparedCandidate(
        string? expectedSessionId,
        string? currentSessionId,
        long expectedRevision,
        long currentRevision,
        object? expectedGameData,
        object? currentGameData) =>
        !string.IsNullOrWhiteSpace(expectedSessionId)
        && string.Equals(expectedSessionId, currentSessionId, StringComparison.Ordinal)
        && expectedRevision == currentRevision
        && expectedGameData is not null
        && ReferenceEquals(expectedGameData, currentGameData);

    public static bool HasVerifiedCopyHeader(
        bool saveReturnedTrue,
        long expectedGameTick,
        long? headerGameTick) =>
        saveReturnedTrue
        && expectedGameTick >= 0
        && headerGameTick == expectedGameTick;
}
