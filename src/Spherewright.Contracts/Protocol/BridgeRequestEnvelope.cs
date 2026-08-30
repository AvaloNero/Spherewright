namespace Spherewright.Contracts.Protocol;

public sealed class BridgeRequestEnvelope<TPayload>
{
    public int ProtocolVersion { get; set; } = ProtocolConstants.CurrentVersion;

    public string MessageType { get; set; } = BridgeMessageTypes.Request;

    public string RequestId { get; set; } = string.Empty;

    public string? SessionId { get; set; }

    public string? Method { get; set; }

    public TPayload Payload { get; set; } = default!;
}

