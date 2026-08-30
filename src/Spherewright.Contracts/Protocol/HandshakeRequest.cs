namespace Spherewright.Contracts.Protocol;

public sealed class HandshakeRequest
{
    public string BridgeInstanceId { get; set; } = string.Empty;

    public string AuthToken { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;

    public string ClientVersion { get; set; } = string.Empty;
}

