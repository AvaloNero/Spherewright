using Spherewright.Contracts.Actions;

namespace Spherewright.Bridge.Core.Logistics;

public static class LogisticsStationFleetTransferPolicy
{
    public static bool TryValidate(
        bool isInterstellar,
        bool isCollector,
        bool isVeinCollector,
        string direction,
        int itemId,
        int count,
        int playerItemCount,
        int playerItemInc,
        int idleDroneCount,
        int workingDroneCount,
        int droneCapacity,
        int idleVesselCount,
        int workingVesselCount,
        int vesselCapacity,
        bool playerCanAcceptWithdrawal,
        out string rejection)
    {
        rejection = string.Empty;
        if (isCollector || isVeinCollector)
        {
            rejection = "Orbital and vein collectors do not expose an ordinary player fleet slot.";
            return false;
        }

        if (direction != LogisticsStationFleetTransferDirections.PlayerToStation
            && direction != LogisticsStationFleetTransferDirections.StationToPlayer)
        {
            rejection = "Fleet transfer direction must be player-to-station or station-to-player.";
            return false;
        }

        if (count <= 0 || count > 100)
        {
            rejection = "Fleet transfer count must be from 1 through 100.";
            return false;
        }

        var isDrone = itemId == LogisticsFleetItemIds.Drone;
        var isVessel = itemId == LogisticsFleetItemIds.Vessel;
        if (!isDrone && !isVessel)
        {
            rejection = "Only logistics drones (5001) or logistics vessels (5002) belong in station fleet slots.";
            return false;
        }

        if (isVessel && !isInterstellar)
        {
            rejection = "A planetary logistics station has no logistics-vessel slot.";
            return false;
        }

        var idle = isDrone ? idleDroneCount : idleVesselCount;
        var working = isDrone ? workingDroneCount : workingVesselCount;
        var capacity = isDrone ? droneCapacity : vesselCapacity;
        if (idle < 0 || working < 0 || capacity <= 0 || idle + working > capacity)
        {
            rejection = "The station fleet counters or current-version prefab capacity are inconsistent.";
            return false;
        }

        if (direction == LogisticsStationFleetTransferDirections.PlayerToStation)
        {
            if (playerItemCount < count)
            {
                rejection = "The player package contains fewer than the requested fleet items.";
                return false;
            }

            if (playerItemInc != 0)
            {
                rejection = "Proliferated fleet items are rejected because the normal station UI discards their proliferator points.";
                return false;
            }

            if (idle + working + count > capacity)
            {
                rejection = "The requested items exceed the station fleet capacity after working craft are included.";
                return false;
            }

            return true;
        }

        if (idle < count)
        {
            rejection = "Only idle station craft can be withdrawn, and fewer than the requested count are idle.";
            return false;
        }

        if (!playerCanAcceptWithdrawal)
        {
            rejection = "The player package cannot accept the exact requested withdrawal.";
            return false;
        }

        return true;
    }
}
