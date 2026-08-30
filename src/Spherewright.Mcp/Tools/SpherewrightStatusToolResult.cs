using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Sessions;

namespace Spherewright.Mcp.Tools;

public sealed class SpherewrightStatusToolResult
{
    public bool Success { get; set; }

    public BridgeStatus? Status { get; set; }

    public BridgeError? Error { get; set; }
}

