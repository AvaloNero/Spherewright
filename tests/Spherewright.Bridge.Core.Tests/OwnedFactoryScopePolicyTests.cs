using Spherewright.Bridge.Core.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class OwnedFactoryScopePolicyTests
{
    [Theory]
    [InlineData(104)]
    [InlineData(102)]
    public void CurrentLocalOrLoadedSameStarFactoryCanBeRead(int targetPlanetId)
    {
        var status = Evaluate(
            requestedPlanetId: targetPlanetId,
            targetPlanetId: targetPlanetId,
            factoryPlanetId: targetPlanetId,
            targetIsInCurrentStar: true,
            factoryLoaded: true);

        Assert.Equal(OwnedFactoryReadStatus.Allowed, status);
    }

    [Fact]
    public void OtherStarIsRejected()
    {
        Assert.Equal(OwnedFactoryReadStatus.OutsideCurrentStar,
            Evaluate(targetIsInCurrentStar: false));
    }

    [Fact]
    public void PositiveRequestedPlanetIdMustMatchTargetIdentity()
    {
        Assert.Equal(OwnedFactoryReadStatus.InvalidPlanetIdentity,
            Evaluate(requestedPlanetId: 102, targetPlanetId: 103));
    }

    [Fact]
    public void UnloadedFactoryIsUnavailableRatherThanAnEmptyRead()
    {
        Assert.Equal(OwnedFactoryReadStatus.FactoryNotLoaded,
            Evaluate(factoryLoaded: false));
    }

    [Theory]
    [InlineData(-1, 12, 12)]
    [InlineData(12, 12, 12)]
    [InlineData(7, 13, 12)]
    public void NegativeOrOutOfRangeFactoryPoolIndicesAreUnavailable(
        int targetFactoryIndex,
        int factoryCount,
        int factoryPoolLength)
    {
        Assert.Equal(OwnedFactoryReadStatus.FactoryIndexUnavailable,
            Evaluate(
                targetFactoryIndex: targetFactoryIndex,
                factoryCount: factoryCount,
                factoryPoolLength: factoryPoolLength));
    }

    [Theory]
    [InlineData(8, 7, 7, 7, 102, true)]
    [InlineData(7, 8, 7, 7, 102, true)]
    [InlineData(7, 7, 8, 7, 102, true)]
    [InlineData(7, 7, 7, 103, 102, true)]
    [InlineData(7, 7, 7, 7, 102, false)]
    public void InconsistentFactoryPoolIdentityIsRejected(
        int resolvedFactoryIndex,
        int targetFactoryIndex,
        int planetFactoryIndex,
        int factoryPlanetId,
        int targetPlanetId,
        bool factoryReferencesTargetPlanet)
    {
        Assert.Equal(OwnedFactoryReadStatus.FactoryIdentityMismatch,
            Evaluate(
                requestedPlanetId: targetPlanetId,
                targetPlanetId: targetPlanetId,
                resolvedFactoryIndex: resolvedFactoryIndex,
                targetFactoryIndex: targetFactoryIndex,
                planetFactoryIndex: planetFactoryIndex,
                factoryPlanetId: factoryPlanetId,
                factoryReferencesTargetPlanet: factoryReferencesTargetPlanet));
    }

    [Theory]
    [InlineData(0, 104, false)]
    [InlineData(102, 104, false)]
    [InlineData(104, 104, true)]
    public void PrepareScopeRemainsCurrentLocalPlanetOnly(int requestedPlanetId, int localPlanetId, bool expected) =>
        Assert.Equal(expected, OwnedFactoryScopePolicy.IsCurrentLocalActionTarget(requestedPlanetId, localPlanetId));

    private static OwnedFactoryReadStatus Evaluate(
        int requestedPlanetId = 102,
        int targetPlanetId = 102,
        bool targetIsInCurrentStar = true,
        bool factoryLoaded = true,
        int targetFactoryIndex = 7,
        int factoryCount = 12,
        int factoryPoolLength = 12,
        int resolvedFactoryIndex = 7,
        int planetFactoryIndex = 7,
        int factoryPlanetId = 102,
        bool factoryReferencesTargetPlanet = true) =>
        OwnedFactoryScopePolicy.EvaluateCurrentStarRead(
            requestedPlanetId,
            targetPlanetId,
            targetIsInCurrentStar,
            factoryLoaded,
            targetFactoryIndex,
            factoryCount,
            factoryPoolLength,
            resolvedFactoryIndex,
            planetFactoryIndex,
            factoryPlanetId,
            factoryReferencesTargetPlanet);
}
