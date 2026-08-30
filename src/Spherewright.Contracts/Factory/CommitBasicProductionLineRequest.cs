namespace Spherewright.Contracts.Factory;

public sealed class CommitBasicProductionLineRequest
{
    public string PlanToken { get; set; } = string.Empty;

    public string IdempotencyKey { get; set; } = string.Empty;
}
