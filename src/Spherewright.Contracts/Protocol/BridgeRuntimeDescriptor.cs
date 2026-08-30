namespace Spherewright.Contracts.Protocol;

public sealed class BridgeRuntimeDescriptor
{
    public int ProcessId { get; set; }

    public string BridgeInstanceId { get; set; } = string.Empty;

    public string PipeName { get; set; } = string.Empty;

    public string AuthToken { get; set; } = string.Empty;

    public int ProtocolVersion { get; set; } = ProtocolConstants.CurrentVersion;

    public string PluginVersion { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }
}

