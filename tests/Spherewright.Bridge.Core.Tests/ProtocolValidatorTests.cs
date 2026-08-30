using Spherewright.Bridge.Core.Routing;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Protocol;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class ProtocolValidatorTests
{
    [Fact]
    public void CurrentProtocolAndUuid_AreAccepted()
    {
        var error = ProtocolValidator.ValidateHeader(new BridgeEnvelopeHeader
        {
            ProtocolVersion = ProtocolConstants.CurrentVersion,
            MessageType = BridgeMessageTypes.Request,
            RequestId = Guid.NewGuid().ToString("D"),
        }, BridgeMessageTypes.Request);

        Assert.Null(error);
    }

    [Fact]
    public void IncompatibleProtocol_IsRejected()
    {
        var error = ProtocolValidator.ValidateHeader(new BridgeEnvelopeHeader
        {
            ProtocolVersion = 999,
            MessageType = BridgeMessageTypes.Request,
            RequestId = Guid.NewGuid().ToString("D"),
        }, BridgeMessageTypes.Request);

        Assert.NotNull(error);
        Assert.Equal(BridgeErrorCodes.InvalidRequest, error.Code);
    }
}

