namespace Spherewright.Bridge.Core.Logistics;

public static class LogisticsStationIdentityPolicy
{
    public static bool MatchesLocalPlanet(
        bool isInterstellar,
        int stationPlanetId,
        int factoryPlanetId)
    {
        if (factoryPlanetId <= 0)
        {
            return false;
        }

        if (isInterstellar)
        {
            return stationPlanetId == factoryPlanetId;
        }

        return stationPlanetId == 0 || stationPlanetId == factoryPlanetId;
    }
}
