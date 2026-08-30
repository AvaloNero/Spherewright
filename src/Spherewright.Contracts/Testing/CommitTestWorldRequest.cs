namespace Spherewright.Contracts.Testing;

public sealed class CommitTestWorldRequest
{
    public string PlanToken { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = string.Empty;
}
