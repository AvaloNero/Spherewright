using Spherewright.Bridge.Core.Authentication;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Protocol;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class HandshakeAuthenticatorTests
{
    [Fact]
    public void CorrectInstanceAndToken_AreAccepted()
    {
        var authenticator = new HandshakeAuthenticator("bridge", "secret");

        var error = authenticator.Authenticate(new HandshakeRequest
        {
            BridgeInstanceId = "bridge",
            AuthToken = "secret",
            ClientName = "tests",
            ClientVersion = "1.0.0",
        });

        Assert.Null(error);
    }

    [Theory]
    [InlineData("other", "secret")]
    [InlineData("bridge", "wrong")]
    [InlineData("bridge", "secret-longer")]
    public void InvalidCredentials_AreRejectedWithoutEchoingSecrets(string instanceId, string token)
    {
        var authenticator = new HandshakeAuthenticator("bridge", "secret");

        var error = authenticator.Authenticate(new HandshakeRequest
        {
            BridgeInstanceId = instanceId,
            AuthToken = token,
            ClientName = "tests",
            ClientVersion = "1.0.0",
        });

        Assert.NotNull(error);
        Assert.Equal(BridgeErrorCodes.AuthFailed, error.Code);
        Assert.DoesNotContain("secret", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("wrong", error.Message, StringComparison.OrdinalIgnoreCase);
    }
}

