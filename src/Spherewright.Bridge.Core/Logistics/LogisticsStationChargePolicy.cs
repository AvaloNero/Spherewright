namespace Spherewright.Bridge.Core.Logistics;

public static class LogisticsStationChargePolicy
{
    public const long EnergyPerTickStep = 50_000;

    public const long PowerWattsStep = EnergyPerTickStep * 60;

    public static bool TryNormalizeUiPower(
        long prefabWorkEnergyPerTick,
        long requestedPowerWatts,
        out long requestedEnergyPerTick,
        out long minimumEnergyPerTick,
        out long maximumEnergyPerTick)
    {
        requestedEnergyPerTick = 0;
        minimumEnergyPerTick = 0;
        maximumEnergyPerTick = 0;
        if (prefabWorkEnergyPerTick <= 0
            || prefabWorkEnergyPerTick > long.MaxValue / 300
            || requestedPowerWatts <= 0
            || requestedPowerWatts % PowerWattsStep != 0)
        {
            return false;
        }

        // UIStationWindow assigns integer slider bounds using these exact
        // divisions, and its change handler writes round(50,000 * value).
        minimumEnergyPerTick = prefabWorkEnergyPerTick / 2 / EnergyPerTickStep * EnergyPerTickStep;
        maximumEnergyPerTick = prefabWorkEnergyPerTick * 5 / EnergyPerTickStep * EnergyPerTickStep;
        requestedEnergyPerTick = requestedPowerWatts / 60;
        return minimumEnergyPerTick > 0
               && maximumEnergyPerTick >= minimumEnergyPerTick
               && requestedEnergyPerTick >= minimumEnergyPerTick
               && requestedEnergyPerTick <= maximumEnergyPerTick;
    }
}
