using System.Text.Json;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Protocol;
using Spherewright.Contracts.Sessions;
using Xunit;

namespace Spherewright.Contracts.Tests;

public sealed class ProtocolContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public void BridgeStatus_UsesStableCamelCaseContract()
    {
        var status = new BridgeStatus
        {
            BridgeConnected = true,
            BridgeInstanceId = "instance",
            PluginVersion = "0.1.0",
            ProtocolVersion = ProtocolConstants.CurrentVersion,
            GameVersion = "0.10.34.28529",
            GameLoaded = false,
            WritesConfigured = false,
            WriteHealth = WriteHealthStates.Healthy,
        };

        var json = JsonSerializer.Serialize(status, JsonOptions);

        Assert.Contains("\"bridgeConnected\":true", json, StringComparison.Ordinal);
        Assert.Contains("\"protocolVersion\":1", json, StringComparison.Ordinal);
        Assert.DoesNotContain("authToken", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pipeName", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ErrorCodes_HaveStableWireValues()
    {
        Assert.Equal("BRIDGE_NOT_READY", BridgeErrorCodes.BridgeNotReady);
        Assert.Equal("AUTH_FAILED", BridgeErrorCodes.AuthFailed);
        Assert.Equal("STALE_REVISION", BridgeErrorCodes.StaleRevision);
        Assert.Equal("ACTION_OUTCOME_UNKNOWN", BridgeErrorCodes.ActionOutcomeUnknown);
        Assert.Equal("SANDBOX_MODE_ACTIVE", BridgeErrorCodes.SandboxModeActive);
        Assert.Equal("confirmed_disabled", SandboxModeStates.ConfirmedDisabled);
    }

    [Fact]
    public void RequestEnvelope_DefaultsToCurrentProtocol()
    {
        var request = new BridgeRequestEnvelope<EmptyPayload>();

        Assert.Equal(ProtocolConstants.CurrentVersion, request.ProtocolVersion);
        Assert.Equal(BridgeMessageTypes.Request, request.MessageType);
    }
}
