namespace Spherewright.Contracts.Factory;

public sealed class ListAssemblersResult
{
    public List<AssemblerSnapshot> Assemblers { get; set; } = new List<AssemblerSnapshot>();

    public string? NextCursor { get; set; }

    public long Revision { get; set; }
}
