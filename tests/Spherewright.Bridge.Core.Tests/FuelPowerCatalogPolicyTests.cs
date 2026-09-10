using Spherewright.Bridge.Core.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class FuelPowerCatalogPolicyTests
{
    [Theory]
    [InlineData(2204, 1)]
    [InlineData(2211, 2)]
    public void OrdinaryFuelGeneratorCopiesNativeScalarsWithoutInventedRatings(int item, int mask)
    {
        // Deliberately synthetic values, not a claim about current DSP ratings.
        var profile = FuelPowerCatalogPolicy.Capture(item, true, 12345, 15432, mask)!;
        Assert.NotNull(profile);
        Assert.Equal(12345, profile.GenerationEnergyPerTick);
        Assert.Equal(15432, profile.FuelEnergyUsePerTick);
        Assert.Equal(mask, profile.FuelTypeMask);
    }

    [Theory]
    [InlineData(2203, true, 100, 120, 1)]
    [InlineData(2210, true, 100, 120, 2)]
    [InlineData(2204, false, 100, 120, 1)]
    [InlineData(2211, true, 0, 120, 2)]
    [InlineData(2211, true, -1, 120, 2)]
    [InlineData(2211, true, 100, 0, 2)]
    [InlineData(2211, true, 100, -1, 2)]
    [InlineData(2211, true, 1000000000001L, 120, 2)]
    [InlineData(2211, true, 100, 1000000000001L, 2)]
    [InlineData(2211, true, 100, 120, 0)]
    [InlineData(2211, true, 100, 120, -1)]
    [InlineData(2211, true, 100, 120, 4)]
    public void UnsupportedOrInvalidDataIsUnavailableNotZero(int item, bool nativeFuel,
        long generation, long fuelUse, int mask) =>
        Assert.Null(FuelPowerCatalogPolicy.Capture(item, nativeFuel, generation, fuelUse, mask));

    [Fact]
    public void UpperBoundIsExactAndEachCaptureIsIndependent()
    {
        var first = FuelPowerCatalogPolicy.Capture(2211, true, 1000000000000L, 1000000000000L, 2)!;
        var second = FuelPowerCatalogPolicy.Capture(2211, true, 1000000000000L, 1000000000000L, 2)!;
        first.GenerationEnergyPerTick = 1;
        Assert.Equal(1000000000000L, second.GenerationEnergyPerTick);
        Assert.Equal(1000000000000L, second.FuelEnergyUsePerTick);
    }
}
