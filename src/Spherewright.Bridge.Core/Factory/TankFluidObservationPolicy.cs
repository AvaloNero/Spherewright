namespace Spherewright.Bridge.Core.Factory;

/// <summary>Validates copied native tank identity and quantity, never inventing an item for an empty tank.</summary>
public static class TankFluidObservationPolicy
{
    // The adapter must check pool/cursor bounds before copying these scalars.
    public static int? ObserveCount(int expectedEntityId, int expectedTankId,
        int tankId, int tankEntityId, int fluidItemId, int fluidCount)
    {
        if (expectedEntityId <= 0 || expectedTankId <= 0
            || tankId != expectedTankId || tankEntityId != expectedEntityId
            || fluidItemId < 0 || fluidCount < 0 || (fluidCount > 0 && fluidItemId == 0))
            return null;

        return fluidCount;
    }
}
