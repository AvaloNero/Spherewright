using Spherewright.Bridge.Core.Factory;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class BeltCargoObservationPolicyTests
{
    [Theory]
    [InlineData(0, 0, 1)]
    [InlineData(-1, 0, 1)]
    [InlineData(100, -1, 1)]
    [InlineData(100, 0, 0)]
    [InlineData(100, 0, -1)]
    [InlineData(1000, 0, 513)]
    [InlineData(100, 100, 1)]
    [InlineData(100, 99, 2)]
    [InlineData(int.MaxValue, int.MaxValue, 1)]
    [InlineData(int.MaxValue, int.MaxValue - 1, int.MaxValue)]
    public void RejectsInvalidOrUnboundedSegments(int pathLength, int start, int length)
    {
        Assert.False(BeltCargoObservationPolicy.TryGetWindow(pathLength, start, length,
            out _, out _, out var reason));
        Assert.Equal("belt_segment_outside_observation_bound", reason);
    }

    [Theory]
    [InlineData(1, 0, 1, 0, 1)]
    [InlineData(1024, 100, 512, 91, 530)]
    [InlineData(int.MaxValue, int.MaxValue - 1, 1, int.MaxValue - 10, 10)]
    public void LocalWindowHasFixedBoundEvenOnHugeNativePath(int pathLength, int start, int length,
        int expectedStart, int expectedLength)
    {
        Assert.True(BeltCargoObservationPolicy.TryGetWindow(pathLength, start, length,
            out var windowStart, out var windowLength, out var reason));
        Assert.Null(reason);
        Assert.Equal(expectedStart, windowStart);
        Assert.Equal(expectedLength, windowLength);
        Assert.InRange(windowLength, 1, BeltCargoObservationPolicy.MaximumWindowCells);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ZeroBytesProveOnlyTheSelectedSegmentEmpty(bool closed)
    {
        Assert.True(Locate(new byte[100], 40, 20, out var references, out var reason, closed));
        Assert.Empty(references);
        Assert.Null(reason);
        Assert.True(BeltCargoObservationPolicy.TrySummarize(references, Array.Empty<BeltCargoSample>(),
            out var items, out reason));
        Assert.Empty(items);
        Assert.Null(reason);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    public void EveryCellOfATouchingStackResolvesToOneNativeReference(int offset)
    {
        var path = new byte[100];
        PutStack(path, 40, 1234567);
        Assert.True(Locate(path, 40 + offset, 1, out var references, out var reason));
        Assert.Null(reason);
        var reference = Assert.Single(references);
        Assert.Equal(1234567, reference.CargoId);
        Assert.Equal(40 + offset, reference.ObservedPathCell);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(99)]
    [InlineData(100)]
    [InlineData(9999)]
    [InlineData(99999999)]
    public void DecodesNativeBase100IncludingZeroBasedCargoId(int id)
    {
        var path = new byte[100];
        PutStack(path, 40, id);
        Assert.True(Locate(path, 40, 10, out var references, out _));
        Assert.Equal(id, Assert.Single(references).CargoId);
    }

    [Fact]
    public void TouchingStacksAreUniqueInsideOneSegmentButNotAdditiveAcrossNeighbours()
    {
        var path = new byte[100];
        PutStack(path, 40, 0);
        Assert.True(Locate(path, 40, 10, out var whole, out _));
        Assert.Single(whole);
        Assert.True(Locate(path, 40, 5, out var first, out _));
        Assert.True(Locate(path, 45, 5, out var second, out _));
        Assert.Equal(Assert.Single(first).CargoId, Assert.Single(second).CargoId);
        Assert.Equal("unique_stacks_touching_selected_belt_segment", new BeltCargoSnapshot().Coverage);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 249)]
    [InlineData(3, 248)]
    [InlineData(4, 251)]
    [InlineData(5, 0)]
    [InlineData(6, 101)]
    [InlineData(7, 245)]
    [InlineData(8, 255)]
    [InlineData(9, 0)]
    public void MalformedTouchingStackIsUnknownWithoutPartialCargo(int offset, byte value)
    {
        var path = new byte[100];
        PutStack(path, 20, 3);
        PutStack(path, 40, 4);
        path[40 + offset] = value;
        Assert.False(Locate(path, 20, 30, out var references, out var reason));
        Assert.Empty(references);
        Assert.NotNull(reason);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    public void CorruptZeroAtOnlySelectedCellIsUnknownNotEmpty(int offset)
    {
        var path = new byte[100];
        PutStack(path, 40, 5);
        path[40 + offset] = 0;
        Assert.False(Locate(path, 40 + offset, 1, out var references, out var reason));
        Assert.Empty(references);
        Assert.Equal("cargo_marker_invalid", reason);
    }

    [Fact]
    public void BoundedRandomValidSegmentsMatchTouchingStackSet()
    {
        var path = new byte[1500];
        var stackStarts = Enumerable.Range(0, 70).Select(i => 15 + 20 * i).ToArray();
        for (var id = 0; id < stackStarts.Length; id++) PutStack(path, stackStarts[id], id);
        var random = new Random(20260907);
        for (var trial = 0; trial < 1000; trial++)
        {
            var start = random.Next(path.Length);
            var length = random.Next(1, Math.Min(512, path.Length - start) + 1);
            Assert.True(Locate(path, start, length, out var references, out _));
            var expected = Enumerable.Range(0, stackStarts.Length)
                .Where(id => stackStarts[id] < start + length && stackStarts[id] + 10 > start);
            Assert.Equal(expected, references.Select(reference => reference.CargoId));
        }
    }

    [Theory]
    [InlineData(false, "cargo_marker_outside_window")]
    [InlineData(true, "closed_path_seam_not_observed")]
    public void BoundaryFragmentsAreNotSilentlyOmitted(bool closed, string expectedReason)
    {
        var path = new byte[20];
        path[0] = 249;
        Assert.False(Locate(path, 0, 1, out var references, out var reason, closed));
        Assert.Empty(references);
        Assert.Equal(expectedReason, reason);
    }

    [Fact]
    public void LastCellIncompleteHeadDoesNotReadPastWindow()
    {
        var path = new byte[20];
        path[19] = 246;
        Assert.False(Locate(path, 19, 1, out var references, out _));
        Assert.Empty(references);
    }

    [Fact]
    public void RepeatedCargoIdAtDifferentPositionsIsNotCountedTwiceOrDeclaredComplete()
    {
        var path = new byte[100];
        PutStack(path, 20, 0);
        PutStack(path, 40, 0);
        Assert.False(Locate(path, 20, 30, out var references, out var reason, closed: true));
        Assert.Empty(references);
        Assert.Equal("ambiguous_cargo_reference", reason);
    }

    [Fact]
    public void DoesNotExtrapolateLocalCoverageToOtherPartsOfPath()
    {
        var path = new byte[100];
        path[0] = 251; // Outside both the selected segment and its bounded guard.
        PutStack(path, 80, 9);
        Assert.True(Locate(path, 40, 1, out var references, out _));
        Assert.Empty(references);
    }

    [Fact]
    public void RejectsNullWrongOffsetOversizedOrShortWindow()
    {
        foreach (var window in new byte[]?[] { null, new byte[19], new byte[21], new byte[531] })
        {
            Assert.False(BeltCargoObservationPolicy.TryLocateCargo(window, 31, 100, 40, 2, false,
                out var refs, out var reason));
            Assert.Empty(refs);
            Assert.Equal("cargo_window_size_invalid", reason);
        }
        Assert.False(BeltCargoObservationPolicy.TryLocateCargo(new byte[20], 30, 100, 40, 2, false,
            out _, out _));
    }

    [Fact]
    public void SummarizesExactNativeStackCountsAndIncInStableItemOrder()
    {
        var refs = new[] { new BeltCargoReference(0, 4), new BeltCargoReference(1, 14), new BeltCargoReference(2, 24) };
        var samples = new[] { Sample(2, 1112, 4, 12), Sample(1, 1000, 1, 0), Sample(0, 1112, 2, 6) };
        Assert.True(BeltCargoObservationPolicy.TrySummarize(refs, samples, out var items, out var reason));
        Assert.Null(reason);
        Assert.Equal(new[] { 1000, 1112 }, items.Select(item => item.ItemId));
        Assert.Equal(1, items[0].Count);
        Assert.Equal(2, items[1].CargoStackCount);
        Assert.Equal(6, items[1].Count);
        Assert.Equal(18, items[1].Inc);
        samples[0].StackCount = 99;
        Assert.Equal(6, items[1].Count); // Public result owns its copied totals.
    }

    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(32768, 1, 0)]
    [InlineData(1112, 0, 0)]
    [InlineData(1112, -1, 0)]
    [InlineData(1112, 256, 0)]
    [InlineData(1112, 1, -1)]
    [InlineData(1112, 1, 256)]
    public void InvalidNativeCargoCannotBecomeObservedEmpty(int item, int count, int inc)
    {
        Assert.False(BeltCargoObservationPolicy.TrySummarize(new[] { new BeltCargoReference(0, 4) },
            new[] { Sample(0, item, count, inc) }, out var items, out var reason));
        Assert.Empty(items);
        Assert.Equal("cargo_readback_incomplete", reason);
    }

    [Fact]
    public void MissingDuplicateOrExtraReadbacksDoNotReturnPartialTotals()
    {
        var refs = new[] { new BeltCargoReference(0, 4), new BeltCargoReference(1, 14) };
        foreach (var samples in new[]
        {
            new[] { Sample(0, 1112, 1, 0) },
            new[] { Sample(0, 1112, 1, 0), Sample(0, 1112, 1, 0) },
            new[] { Sample(0, 1112, 1, 0), Sample(2, 1112, 1, 0) },
        })
        {
            Assert.False(BeltCargoObservationPolicy.TrySummarize(refs, samples, out var items, out _));
            Assert.Empty(items);
        }
    }

    [Fact]
    public void PublicUnavailableIsNotZeroAndDoesNotChangeAnyExistingActionHash()
    {
        var entity = new FactoryEntitySnapshot { ComponentKind = "belt", ObjectId = 10, ItemId = 2001 };
        Assert.Null(entity.BeltCargo);
        var state = CanonicalStateHash.Factory(entity);
        var config = CanonicalStateHash.FactoryConfiguration(entity);
        var endpoint = CanonicalStateHash.FactoryEndpoint(entity);
        entity.BeltCargo = new BeltCargoSnapshot { ReasonCode = "cargo_pool_unavailable" };
        Assert.Equal("unavailable", entity.BeltCargo.State);
        Assert.Null(entity.BeltCargo.ItemCount);
        Assert.Null(entity.BeltCargo.CargoStackCount);
        entity.BeltCargo.State = "observed";
        entity.BeltCargo.ItemCount = 4;
        entity.BeltCargo.Items.Add(new BeltCargoItemSnapshot { ItemId = 1112, Count = 4 });
        Assert.Equal(state, CanonicalStateHash.Factory(entity));
        Assert.Equal(config, CanonicalStateHash.FactoryConfiguration(entity));
        Assert.Equal(endpoint, CanonicalStateHash.FactoryEndpoint(entity));
    }

    [Fact]
    public void SummaryBoundsAndDuplicateReferenceGuardsAreFailClosed()
    {
        var refs = Enumerable.Range(0, 65).Select(id => new BeltCargoReference(id, id * 10)).ToArray();
        var samples = Enumerable.Range(0, 65).Select(id => Sample(id, 1112, 1, 0)).ToArray();
        Assert.False(BeltCargoObservationPolicy.TrySummarize(refs, samples, out _, out _));
        Assert.False(BeltCargoObservationPolicy.TrySummarize(null, samples, out _, out _));
        Assert.False(BeltCargoObservationPolicy.TrySummarize(refs, null, out _, out _));
        Assert.False(BeltCargoObservationPolicy.TrySummarize(new[] { refs[0], refs[0] }, samples.Take(2).ToArray(), out _, out _));
    }

    private static BeltCargoSample Sample(int id, int item, int count, int inc) =>
        new() { CargoId = id, ItemId = item, StackCount = count, Inc = inc };

    private static bool Locate(byte[] path, int start, int length, out List<BeltCargoReference> refs,
        out string? reason, bool closed = false)
    {
        Assert.True(BeltCargoObservationPolicy.TryGetWindow(path.Length, start, length,
            out var windowStart, out var windowLength, out _));
        return BeltCargoObservationPolicy.TryLocateCargo(path.Skip(windowStart).Take(windowLength).ToArray(),
            windowStart, path.Length, start, length, closed, out refs, out reason);
    }

    private static void PutStack(byte[] path, int start, int cargoId)
    {
        for (var i = 0; i < 5; i++) path[start + i] = (byte)(246 + i);
        for (var i = 5; i < 9; i++, cargoId /= 100) path[start + i] = (byte)(cargoId % 100 + 1);
        path[start + 9] = 255;
    }
}
