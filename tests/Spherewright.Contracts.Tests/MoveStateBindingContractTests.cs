using System.Text.Json;
using Spherewright.Contracts.Actions;
using Xunit;

namespace Spherewright.Contracts.Tests;

public sealed class MoveStateBindingContractTests
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public void LegacyMoveRemainsStrictAndClaimsNoDriftBinding()
    {
        Assert.False(JsonSerializer.Deserialize<PrepareMoveRequest>("{}", Options)!.AllowPassiveDrift);
        Assert.Null(JsonSerializer.Deserialize<PreparedNormalAction>("{}", Options)!.MoveStateBinding);
    }

    [Fact]
    public void ExplicitBindingLimitsOriginAndDeadlineRoundTrip()
    {
        var expiry = DateTimeOffset.UtcNow;
        var plan = new PreparedNormalAction { MoveStateBinding = new()
        {
            InspectionStateHash = "server-inspected", InspectionCapturedAtGameTick = 42,
            OriginPosition = new() { X = 1, Y = 2, Z = 3 },
            MaximumDisplacementMetres = 0.05, MaximumSpeedMetresPerSecond = 0.15,
            MaximumAgeSeconds = 2, ExpiresAtUtc = expiry,
        } };
        var round = JsonSerializer.Deserialize<PreparedNormalAction>(JsonSerializer.Serialize(plan, Options), Options)!.MoveStateBinding!;
        Assert.Equal("bounded_passive_drift", round.Mode);
        Assert.Equal("server-inspected", round.InspectionStateHash);
        Assert.Equal(42, round.InspectionCapturedAtGameTick);
        Assert.Equal(3, round.OriginPosition.Z);
        Assert.Equal(0.05, round.MaximumDisplacementMetres);
        Assert.Equal(0.15, round.MaximumSpeedMetresPerSecond);
        Assert.Equal(2, round.MaximumAgeSeconds);
        Assert.Equal(expiry, round.ExpiresAtUtc);
    }
}
