namespace Spherewright.Contracts.Factory;

public sealed class PrepareBasicProductionLineRequest
{
    public long ExpectedRevision { get; set; }

    public int InputItemCount { get; set; } = 20;
}
