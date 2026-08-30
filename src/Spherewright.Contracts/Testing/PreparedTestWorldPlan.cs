namespace Spherewright.Contracts.Testing;

public sealed class PreparedTestWorldPlan
{
    public string PlanToken { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public string SaveName { get; set; } = string.Empty;

    public int GalaxySeed { get; set; }

    public int StarCount { get; set; }

    public float ResourceMultiplier { get; set; }

    public bool PeacefulMode { get; set; }

    public bool SandboxMode { get; set; }

    public bool CommitAllowed { get; set; }

    public List<string> Warnings { get; set; } = new List<string>();
}
