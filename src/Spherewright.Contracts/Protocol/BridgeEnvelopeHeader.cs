namespace Spherewright.Contracts.Protocol;

public sealed class BridgeEnvelopeHeader
{
    public int ProtocolVersion { get; set; }

    public string MessageType { get; set; } = string.Empty;

    public string RequestId { get; set; } = string.Empty;

    public string? SessionId { get; set; }

    public string? Method { get; set; }
}

