using Spherewright.Contracts.Errors;

namespace Spherewright.Mcp.Tools;

public sealed class SpherewrightToolResult<T>
{
    public bool Success { get; set; }

    public T? Result { get; set; }

    public BridgeError? Error { get; set; }
}
