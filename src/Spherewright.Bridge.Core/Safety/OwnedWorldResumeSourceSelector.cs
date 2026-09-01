namespace Spherewright.Bridge.Core.Safety;

public enum OwnedWorldResumeSourceKind
{
    None = 0,
    LastExit = 1,
    OwnedPrimary = 2,
}

public static class OwnedWorldResumeSourceSelector
{
    public static OwnedWorldResumeSourceKind Select(
        bool quarantineRecovery,
        long minimumGameTick,
        DateTimeOffset ticketIssuedAtUtc,
        DateTimeOffset? lastExitWrittenAtUtc,
        long? lastExitGameTick,
        DateTimeOffset? ownedPrimaryWrittenAtUtc,
        long? ownedPrimaryGameTick,
        TimeSpan timestampTolerance)
    {
        if (minimumGameTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumGameTick));
        }

        if (timestampTolerance < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timestampTolerance));
        }

        var minimumWrittenAt = ticketIssuedAtUtc - timestampTolerance;
        if (quarantineRecovery)
        {
            return lastExitWrittenAtUtc >= minimumWrittenAt
                   && lastExitGameTick >= minimumGameTick
                ? OwnedWorldResumeSourceKind.LastExit
                : OwnedWorldResumeSourceKind.None;
        }

        return ownedPrimaryWrittenAtUtc >= minimumWrittenAt
               && ownedPrimaryGameTick >= minimumGameTick
            ? OwnedWorldResumeSourceKind.OwnedPrimary
            : OwnedWorldResumeSourceKind.None;
    }
}
