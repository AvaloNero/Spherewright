namespace Spherewright.Bridge.Core.Factory;

// Bounded private per-owned document. It is not an alternative planner or a client DTO.
public sealed class GovernorDeclarationArchive
{
    public const int MaximumDeclarations = 8;
    public int Version { get; set; } = 1;
    public string IdentityHash { get; set; } = string.Empty;
    public string GameVersion { get; set; } = string.Empty;
    public List<GovernorValidationCheckpoint> Declarations { get; set; } = new List<GovernorValidationCheckpoint>();

    public void Validate(string expectedIdentityHash, string expectedGameVersion)
    {
        if (Version != 1 || string.IsNullOrWhiteSpace(expectedIdentityHash) || string.IsNullOrWhiteSpace(expectedGameVersion)
            || IdentityHash != expectedIdentityHash || GameVersion != expectedGameVersion || Declarations is null
            || Declarations.Count > MaximumDeclarations || Declarations.Any(d => d is null)
            || Declarations.Select(d => d.BaselineProposalHash).Distinct().Count() != Declarations.Count)
            throw Invalid();
        foreach (var saved in Declarations)
        {
            saved.Validate();
            if (saved.OwnedIdentityHash != IdentityHash || saved.GameVersion != GameVersion) throw Invalid();
        }
    }

    public void AddLockedDeclaration(GovernorValidationCheckpoint saved)
    {
        Validate(saved.OwnedIdentityHash, saved.GameVersion);
        saved.Validate();
        var existing = Declarations.SingleOrDefault(d => d.BaselineProposalHash == saved.BaselineProposalHash);
        if (existing is not null)
        {
            if (existing.IntegrityHash != saved.IntegrityHash) throw Invalid();
            return;
        }
        if (Declarations.Count >= MaximumDeclarations)
            throw new FoundryPlanningException("governor_validation_limit", "Eight durable declarations per owned identity; no silent eviction.");
        Declarations.Add(saved);
    }

    private static FoundryPlanningException Invalid() => new FoundryPlanningException(
        "governor_validation_archive_invalid", "The bounded private declaration archive has an invalid or conflicting identity.");
}
