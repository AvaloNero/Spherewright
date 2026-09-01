using Spherewright.Bridge.Core.Safety;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class OwnedWorldResumeSourceSelectorTests
{
    private static readonly DateTimeOffset IssuedAt =
        new DateTimeOffset(2026, 9, 1, 4, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Select_HealthyRestartUsesOnlyExactPrimary()
    {
        var selected = OwnedWorldResumeSourceSelector.Select(
            quarantineRecovery: false,
            minimumGameTick: 500,
            IssuedAt,
            IssuedAt.AddSeconds(1),
            900,
            IssuedAt,
            500,
            TimeSpan.FromSeconds(2));

        Assert.Equal(OwnedWorldResumeSourceKind.OwnedPrimary, selected);
    }

    [Fact]
    public void Select_QuarantineRecoveryUsesOnlyLastExit()
    {
        var selected = OwnedWorldResumeSourceSelector.Select(
            quarantineRecovery: true,
            minimumGameTick: 500,
            IssuedAt,
            IssuedAt.AddSeconds(1),
            501,
            IssuedAt,
            900,
            TimeSpan.FromSeconds(2));

        Assert.Equal(OwnedWorldResumeSourceKind.LastExit, selected);
    }

    [Fact]
    public void Select_RejectsCandidateWhoseHeaderTickPredatesTicket()
    {
        var selected = OwnedWorldResumeSourceSelector.Select(
            quarantineRecovery: false,
            minimumGameTick: 500,
            IssuedAt,
            IssuedAt,
            900,
            IssuedAt,
            499,
            TimeSpan.FromSeconds(2));

        Assert.Equal(OwnedWorldResumeSourceKind.None, selected);
    }

    [Fact]
    public void Select_RejectsNegativeTolerance()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OwnedWorldResumeSourceSelector.Select(
                quarantineRecovery: false,
                minimumGameTick: 0,
                IssuedAt,
                IssuedAt,
                0,
                IssuedAt,
                0,
                TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public void Select_RejectsNegativeMinimumTick()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OwnedWorldResumeSourceSelector.Select(
                quarantineRecovery: false,
                minimumGameTick: -1,
                IssuedAt,
                IssuedAt,
                0,
                IssuedAt,
                0,
                TimeSpan.Zero));
    }
}
