using System.Text.Json;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Contracts.Tests;

public sealed class TankFluidContractTests
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"componentKind\":\"tank\",\"buffers\":[]}")]
    [InlineData("{\"tankFluidCount\":null,\"buffers\":[]}")]
    public void LegacyOrUnavailableTankIsNotZero(string json) =>
        Assert.Null(JsonSerializer.Deserialize<FactoryEntitySnapshot>(json, Options)!.TankFluidCount);

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(9512)]
    public void OptionalExactQuantityRoundTrips(int? count)
    {
        var source = new FactoryEntitySnapshot { TankFluidCount = count };
        var round = JsonSerializer.Deserialize<FactoryEntitySnapshot>(JsonSerializer.Serialize(source, Options), Options)!;
        Assert.Equal(count, round.TankFluidCount);
        Assert.Empty(round.Buffers);
    }
}
