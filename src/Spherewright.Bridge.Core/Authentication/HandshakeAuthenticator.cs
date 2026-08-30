using System.Text;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Protocol;

namespace Spherewright.Bridge.Core.Authentication;

public sealed class HandshakeAuthenticator
{
    private readonly string _bridgeInstanceId;
    private readonly byte[] _authTokenBytes;

    public HandshakeAuthenticator(string bridgeInstanceId, string authToken)
    {
        if (string.IsNullOrWhiteSpace(bridgeInstanceId))
        {
            throw new ArgumentException("Bridge instance ID is required.", nameof(bridgeInstanceId));
        }

        if (string.IsNullOrWhiteSpace(authToken))
        {
            throw new ArgumentException("Authentication token is required.", nameof(authToken));
        }

        _bridgeInstanceId = bridgeInstanceId;
        _authTokenBytes = Encoding.UTF8.GetBytes(authToken);
    }

    public BridgeError? Authenticate(HandshakeRequest? request)
    {
        if (request is null
            || !string.Equals(request.BridgeInstanceId, _bridgeInstanceId, StringComparison.Ordinal)
            || !FixedTimeEquals(_authTokenBytes, Encoding.UTF8.GetBytes(request.AuthToken ?? string.Empty)))
        {
            return BridgeError.Create(
                BridgeErrorCodes.AuthFailed,
                "Bridge authentication failed.",
                false,
                "Rediscover the active Spherewright bridge descriptor and reconnect.");
        }

        if (string.IsNullOrWhiteSpace(request.ClientName) || string.IsNullOrWhiteSpace(request.ClientVersion))
        {
            return BridgeError.Create(
                BridgeErrorCodes.InvalidRequest,
                "Handshake client name and version are required.",
                false,
                "Send a complete handshake request.");
        }

        return null;
    }

    private static bool FixedTimeEquals(byte[] expected, byte[] actual)
    {
        var difference = expected.Length ^ actual.Length;
        var length = Math.Max(expected.Length, actual.Length);
        for (var index = 0; index < length; index++)
        {
            var left = index < expected.Length ? expected[index] : (byte)0;
            var right = index < actual.Length ? actual[index] : (byte)0;
            difference |= left ^ right;
        }

        return difference == 0;
    }
}

