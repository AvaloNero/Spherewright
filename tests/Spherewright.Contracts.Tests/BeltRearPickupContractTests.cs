using System.Text.Json;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Contracts.Tests;

public sealed class BeltRearPickupContractTests
{
    [Fact]
    public void LegacyCargoDoesNotClaimRearEvidence() => Assert.Null(JsonSerializer.Deserialize<BeltCargoSnapshot>("{}")!.RearPickup);

    [Theory]
    [InlineData("unavailable")]
    [InlineData("not_applicable")]
    [InlineData("no_aligned_packet")]
    [InlineData("observed")]
    public void AdditiveRearEvidenceRoundTripsWithoutTurningUnknownIntoZero(string state)
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var cargo = new BeltCargoSnapshot { RearPickup = new() { State = state, CapturedAtGameTick = 42,
            ItemId = state == "observed" ? 1003 : null, Count = state == "observed" ? 4 : null } };
        var round = JsonSerializer.Deserialize<BeltCargoSnapshot>(JsonSerializer.Serialize(cargo, options), options)!;
        Assert.Equal(state, round.RearPickup!.State); Assert.Equal(cargo.RearPickup.Count, round.RearPickup.Count);
        Assert.Equal("open_path_rear_pickup_aligned_packet", round.RearPickup.Coverage);
        Assert.Equal(42, round.RearPickup.CapturedAtGameTick);
    }
}
