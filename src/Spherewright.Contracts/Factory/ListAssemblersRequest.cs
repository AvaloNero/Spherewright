namespace Spherewright.Contracts.Factory;

public sealed class ListAssemblersRequest
{
    public int Limit { get; set; } = 50;

    public string? Cursor { get; set; }
}
