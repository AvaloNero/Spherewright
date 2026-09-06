namespace Spherewright.Bridge.Core.Safety;

// Shared by the catalog read and the existing native refuel preparation.
// Heat is an item energy value, not a promise about burn rate or movement range.
public static class OrdinaryMechaFuelPolicy
{
    public static bool IsAccepted(long heatValueJoules, int fuelType, bool nativeItemIsFuel) =>
        heatValueJoules > 0 && fuelType > 0 && nativeItemIsFuel;
}
