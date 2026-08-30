namespace Spherewright.Contracts.Sessions;

public sealed class WriteBlocker
{
    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
