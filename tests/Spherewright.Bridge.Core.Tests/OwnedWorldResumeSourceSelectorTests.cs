using Spherewright.Bridge.Core.Safety;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class OwnedWorldResumeSourceSelectorTests
{
    private static readonly DateTimeOffset IssuedAt =
        new DateTimeOffset(2026, 9, 1, 4, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Select_PrefersFreshLastExit()
    {
        var selected = OwnedWorldResumeSourceSelector.Select(
            IssuedAt,
            IssuedAt.AddSeconds(1),
            IssuedAt,
            TimeSpan.FromSeconds(2));

        Assert.Equal(OwnedWorldResumeSourceKind.LastExit, selected);
    }

    [Fact]
    public void Select_FallsBackToFreshExactOwnedPrimary()
    {
        var selected = OwnedWorldResumeSourceSelector.Select(
            IssuedAt,
            IssuedAt.AddSeconds(-10),
            IssuedAt.AddSeconds(-1),
            TimeSpan.FromSeconds(2));

        Assert.Equal(OwnedWorldResumeSourceKind.OwnedPrimary, selected);
    }

    [Fact]
    public void Select_RejectsWhenBothSourcesPredateTicketWindow()
    {
        var selected = OwnedWorldResumeSourceSelector.Select(
            IssuedAt,
            IssuedAt.AddSeconds(-3),
            IssuedAt.AddSeconds(-3),
            TimeSpan.FromSeconds(2));

        Assert.Equal(OwnedWorldResumeSourceKind.None, selected);
    }

    [Fact]
    public void Select_RejectsNegativeTolerance()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OwnedWorldResumeSourceSelector.Select(
                IssuedAt,
                IssuedAt,
                IssuedAt,
                TimeSpan.FromSeconds(-1)));
    }
}
