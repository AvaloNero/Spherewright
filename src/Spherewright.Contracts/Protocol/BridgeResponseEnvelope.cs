using Spherewright.Contracts.Errors;

namespace Spherewright.Contracts.Protocol;

public sealed class BridgeResponseEnvelope<TResult>
{
    public int ProtocolVersion { get; set; } = ProtocolConstants.CurrentVersion;

    public string MessageType { get; set; } = BridgeMessageTypes.Response;

    public string RequestId { get; set; } = string.Empty;

    public string? SessionId { get; set; }

    public bool Success { get; set; }

    public TResult? Result { get; set; }

    public BridgeError? Error { get; set; }
}

