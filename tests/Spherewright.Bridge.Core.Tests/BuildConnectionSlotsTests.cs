using Spherewright.Bridge.Core.Safety;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class BuildConnectionSlotsTests
{
    [Fact]
    public void SelectAvailable_ExcludesEveryOccupiedMachineSlot()
    {
        var available = BuildConnectionSlots.SelectAvailable(4, new[] { 0, 2 });

        Assert.Equal(new[] { 1, 3 }, available);
    }

    [Fact]
    public void SelectAvailable_IgnoresDuplicateAndOutOfRangeObservations()
    {
        var available = BuildConnectionSlots.SelectAvailable(3, new[] { -1, 1, 1, 3 });

        Assert.Equal(new[] { 0, 2 }, available);
    }

    [Fact]
    public void SelectAvailable_RejectsNegativeSlotCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BuildConnectionSlots.SelectAvailable(-1, Array.Empty<int>()));
    }

    [Fact]
    public void SelectVerificationCandidates_PreservesConcreteMachineSlot()
    {
        var candidates = BuildConnectionSlots.SelectVerificationCandidates(8, 16);

        Assert.Equal(new[] { 8 }, candidates);
    }

    [Fact]
    public void SelectVerificationCandidates_ScansAllSlotsForVirtualBeltAttachment()
    {
        var candidates = BuildConnectionSlots.SelectVerificationCandidates(-1, 4);

        Assert.Equal(new[] { 0, 1, 2, 3 }, candidates);
    }

    [Fact]
    public void SelectVerificationCandidates_RejectsOutOfRangeConcreteSlot()
    {
        var candidates = BuildConnectionSlots.SelectVerificationCandidates(4, 4);

        Assert.Empty(candidates);
    }
}
