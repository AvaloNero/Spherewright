using System.Text.Json;
using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class FactoryObjectReadPolicyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(int.MinValue)]
    public void InvalidObjectIdIsRequestErrorNotDisappearanceEvidence(int id)
    {
        var error = FactoryObjectReadPolicy.ValidateObjectId(id);
        Assert.NotNull(error);
        Assert.Equal(BridgeErrorCodes.InvalidRequest, error.Code);
        Assert.False(error.Retryable);
        Assert.Contains("objectId, not entityId", error.Message);
        Assert.Contains("does not prove", error.Recovery);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(761)]
    [InlineData(int.MaxValue)]
    [InlineData(-1)]
    [InlineData(-761)]
    [InlineData(-int.MaxValue)]
    public void NonzeroSignedIdsProceedToExistingOwnedNativeIdentityChecks(int id) =>
        Assert.Null(FactoryObjectReadPolicy.ValidateObjectId(id));

    [Fact]
    public void MisnamedReadFieldCannotAliasADifferentActionSchema()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var wrong = JsonSerializer.Deserialize<InspectFactoryEntityRequest>("{\"planetId\":104,\"entityId\":761}", options)!;
        var right = JsonSerializer.Deserialize<InspectFactoryEntityRequest>("{\"planetId\":104,\"objectId\":761}", options)!;
        Assert.Equal(0, wrong.ObjectId);
        Assert.Equal(BridgeErrorCodes.InvalidRequest, FactoryObjectReadPolicy.ValidateObjectId(wrong.ObjectId)!.Code);
        Assert.Equal(761, right.ObjectId);
        Assert.Null(FactoryObjectReadPolicy.ValidateObjectId(right.ObjectId));
    }
}
