using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Protocol;

namespace Spherewright.Bridge.Core.Routing;

public static class ProtocolValidator
{
    public static BridgeError? ValidateHeader(BridgeEnvelopeHeader? header, string expectedMessageType)
    {
        if (header is null)
        {
            return Invalid("The bridge envelope is missing.");
        }

        if (header.ProtocolVersion != ProtocolConstants.CurrentVersion)
        {
            return BridgeError.Create(
                BridgeErrorCodes.InvalidRequest,
                $"Protocol version {header.ProtocolVersion} is not supported.",
                false,
                $"Use protocol version {ProtocolConstants.CurrentVersion}.");
        }

        if (!string.Equals(header.MessageType, expectedMessageType, StringComparison.Ordinal))
        {
            return Invalid($"Expected message type '{expectedMessageType}'.");
        }

        if (!Guid.TryParse(header.RequestId, out _))
        {
            return Invalid("requestId must be a UUID.");
        }

        return null;
    }

    private static BridgeError Invalid(string message)
    {
        return BridgeError.Create(
            BridgeErrorCodes.InvalidRequest,
            message,
            false,
            "Create a new request that matches the Spherewright bridge schema.");
    }
}

