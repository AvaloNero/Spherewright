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
        DateTimeOffset ticketIssuedAtUtc,
        DateTimeOffset? lastExitWrittenAtUtc,
        DateTimeOffset? ownedPrimaryWrittenAtUtc,
        TimeSpan timestampTolerance)
    {
        if (timestampTolerance < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timestampTolerance));
        }

        var minimumWrittenAt = ticketIssuedAtUtc - timestampTolerance;
        if (lastExitWrittenAtUtc >= minimumWrittenAt)
        {
            return OwnedWorldResumeSourceKind.LastExit;
        }

        if (ownedPrimaryWrittenAtUtc >= minimumWrittenAt)
        {
            return OwnedWorldResumeSourceKind.OwnedPrimary;
        }

        return OwnedWorldResumeSourceKind.None;
    }
}
