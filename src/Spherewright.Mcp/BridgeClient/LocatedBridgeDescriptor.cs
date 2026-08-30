using Spherewright.Contracts.Protocol;

namespace Spherewright.Mcp.BridgeClient;

internal sealed class LocatedBridgeDescriptor
{
    public LocatedBridgeDescriptor(string path, BridgeRuntimeDescriptor descriptor)
    {
        Path = path;
        Descriptor = descriptor;
    }

    public string Path { get; }

    public BridgeRuntimeDescriptor Descriptor { get; }
}

