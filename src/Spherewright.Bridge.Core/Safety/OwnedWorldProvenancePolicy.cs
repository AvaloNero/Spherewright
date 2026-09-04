namespace Spherewright.Bridge.Core.Safety;

public static class OwnedWorldProvenancePolicy
{
    public static bool MatchesProtectedSaveIdentity(
        string? ticketOwnedSaveName,
        string? loadedSaveName) =>
        !string.IsNullOrWhiteSpace(ticketOwnedSaveName)
        && string.Equals(ticketOwnedSaveName, loadedSaveName, StringComparison.Ordinal);
}
