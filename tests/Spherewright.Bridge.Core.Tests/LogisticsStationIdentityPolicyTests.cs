using Spherewright.Bridge.Core.Logistics;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class LogisticsStationIdentityPolicyTests
{
    [Theory]
    [InlineData(false, 0, 104, true)]
    [InlineData(false, 104, 104, true)]
    [InlineData(false, 102, 104, false)]
    [InlineData(true, 104, 104, true)]
    [InlineData(true, 0, 104, false)]
    [InlineData(true, 102, 104, false)]
    [InlineData(false, 0, 0, false)]
    public void MatchesLocalPlanet_AcceptsOnlyTheNativeLocalStationSentinelOrExactPlanet(
        bool isInterstellar,
        int stationPlanetId,
        int factoryPlanetId,
        bool expected)
    {
        Assert.Equal(
            expected,
            LogisticsStationIdentityPolicy.MatchesLocalPlanet(
                isInterstellar,
                stationPlanetId,
                factoryPlanetId));
    }
}
