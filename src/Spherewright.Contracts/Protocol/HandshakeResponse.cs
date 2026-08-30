namespace Spherewright.Contracts.Protocol;

public sealed class HandshakeResponse
{
    public bool Accepted { get; set; }

    public string BridgeInstanceId { get; set; } = string.Empty;

    public int ProtocolVersion { get; set; } = ProtocolConstants.CurrentVersion;

    public string PluginVersion { get; set; } = string.Empty;
}

