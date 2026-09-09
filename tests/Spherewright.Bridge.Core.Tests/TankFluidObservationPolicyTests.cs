using Spherewright.Bridge.Core.Factory;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class TankFluidObservationPolicyTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1120, 0)]
    [InlineData(1120, 9512)]
    [InlineData(1114, 1)]
    [InlineData(1120, int.MaxValue)]
    public void ValidIdentityRetainsExactQuantityIncludingZero(int item, int count) =>
        Assert.Equal(count, TankFluidObservationPolicy.ObserveCount(165, 2, 2, 165, item, count));

    [Theory]
    [InlineData(0, 2, 2, 0, 0, 0)]
    [InlineData(-1, 2, 2, -1, 0, 0)]
    [InlineData(165, 0, 0, 165, 0, 0)]
    [InlineData(165, -1, -1, 165, 0, 0)]
    [InlineData(165, 2, 0, 165, 0, 0)]
    [InlineData(165, 2, 3, 165, 0, 0)]
    [InlineData(165, 2, 2, 0, 0, 0)]
    [InlineData(165, 2, 2, 166, 0, 0)]
    [InlineData(165, 2, 2, 165, -1, 0)]
    [InlineData(165, 2, 2, 165, 1120, -1)]
    [InlineData(165, 2, 2, 165, 0, 1)]
    public void InvalidIdentityOrQuantityIsUnknownNotEmpty(int entity, int expectedTank,
        int actualTank, int owner, int item, int count) =>
        Assert.Null(TankFluidObservationPolicy.ObserveCount(entity, expectedTank, actualTank, owner, item, count));

    [Fact]
    public void ObservationDoesNotChangeExistingActionHashesOrInventBuffers()
    {
        var entity = new FactoryEntitySnapshot { ObjectId = 165, ItemId = 2106, ComponentKind = "tank" };
        var live = CanonicalStateHash.Factory(entity);
        var configuration = CanonicalStateHash.FactoryConfiguration(entity);
        var endpoint = CanonicalStateHash.FactoryEndpoint(entity);
        entity.TankFluidCount = 0;
        Assert.Empty(entity.Buffers);
        Assert.Equal(live, CanonicalStateHash.Factory(entity));
        Assert.Equal(configuration, CanonicalStateHash.FactoryConfiguration(entity));
        Assert.Equal(endpoint, CanonicalStateHash.FactoryEndpoint(entity));
    }
}
