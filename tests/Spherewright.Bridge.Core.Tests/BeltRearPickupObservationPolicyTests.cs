using Spherewright.Bridge.Core.Factory;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class BeltRearPickupObservationPolicyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(99999999)]
    public void AlignedRearReportsOnlyItsNativePacketIncludingZeroBasedId(int id)
    {
        var path = new byte[100]; Put(path, 70, 19); Put(path, 90, id);
        var samples = new[] { Sample(19, 1004, 2, 6), Sample(id, 1003, 4, 12) };
        var result = Capture(path, 70, 30, samples);
        Assert.Equal("observed", result.State); Assert.Null(result.ReasonCode);
        Assert.Equal(1003, result.ItemId); Assert.Equal(4, result.Count); Assert.Equal(12, result.Inc);
        Assert.Equal(94, result.MarkerPathCell); Assert.Equal(42, result.CapturedAtGameTick);
        samples[1].StackCount = 99;
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void LastSingleCellIncludesCompleteRearInExistingNineCellGuard()
    {
        var path = new byte[100]; Put(path, 90, 0);
        var result = Capture(path, 99, 1, new[] { Sample(0) });
        Assert.Equal("observed", result.State); Assert.Equal(1003, result.ItemId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NoAlignedPacketIsNotAnEmptySegmentOrZeroFlow(bool withUnalignedCargo)
    {
        var path = new byte[100];
        if (withUnalignedCargo) Put(path, 80, 0);
        var result = Capture(path, 70, 30, withUnalignedCargo ? new[] { Sample(0) } : Array.Empty<BeltCargoSample>());
        Assert.Equal("no_aligned_packet", result.State);
        Assert.Equal("no_packet_aligned_for_native_rear_pickup", result.ReasonCode);
        Assert.Null(result.ItemId); Assert.Null(result.Count); Assert.Null(result.Inc);
    }

    [Theory]
    [InlineData(false, 40, 20, "selected_segment_is_not_path_rear")]
    [InlineData(true, 80, 20, "closed_path_has_no_open_rear")]
    public void OtherSegmentsAndClosedPathsNeverClaimARearPacket(bool closed, int start, int length, string reason)
    {
        var result = Capture(new byte[100], start, length, Array.Empty<BeltCargoSample>(), closed);
        Assert.Equal("not_applicable", result.State); Assert.Equal(reason, result.ReasonCode);
        Assert.Null(result.ItemId); Assert.Null(result.MarkerPathCell);
    }

    [Theory]
    [InlineData(0, 0, 1)]
    [InlineData(100, -1, 1)]
    [InlineData(100, 0, 0)]
    [InlineData(1000, 0, 513)]
    [InlineData(int.MaxValue, int.MaxValue - 1, int.MaxValue)]
    public void InvalidBoundsFailClosedBeforeCopy(int length, int start, int count)
    {
        var result = BeltRearPickupObservationPolicy.Capture(Array.Empty<byte>(), 0, length, start, count, false, null, 1);
        Assert.Equal("unavailable", result.State); Assert.Null(result.ItemId);
        Assert.Equal("belt_segment_outside_observation_bound", result.ReasonCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(9)]
    [InlineData(11)]
    [InlineData(531)]
    public void MissingOrWrongWindowIsNotObservedEmpty(int length)
    {
        var result = BeltRearPickupObservationPolicy.Capture(length < 0 ? null : new byte[length], 90, 100, 99, 1, false, null, 42);
        Assert.Equal("unavailable", result.State); Assert.Equal("cargo_window_size_invalid", result.ReasonCode);
        Assert.Null(result.ItemId);
    }

    [Fact]
    public void HugePathStillUsesOnlyTenCopiedBytesAtTail()
    {
        var window = new byte[10]; Put(window, 0, 0);
        var result = BeltRearPickupObservationPolicy.Capture(window, int.MaxValue - 10, int.MaxValue,
            int.MaxValue - 1, 1, false, new[] { Sample(0) }, 42);
        Assert.Equal("observed", result.State); Assert.Equal(int.MaxValue - 6, result.MarkerPathCell);
    }

    [Fact]
    public void TinyPathCannotProveNativeTenByteRear()
    {
        var result = Capture(new byte[9], 0, 9, Array.Empty<BeltCargoSample>());
        Assert.Equal("unavailable", result.State); Assert.Equal("rear_packet_window_unavailable", result.ReasonCode);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(4, 0)]
    [InlineData(5, 0)]
    [InlineData(6, 101)]
    [InlineData(9, 0)]
    public void CorruptRearCannotBecomeNoAlignedPacketOrPartialEvidence(int offset, byte value)
    {
        var path = new byte[100]; Put(path, 90, 0); path[90 + offset] = value;
        var result = Capture(path, 90, 10, new[] { Sample(0) });
        Assert.Equal("unavailable", result.State); Assert.NotNull(result.ReasonCode);
        Assert.Null(result.ItemId); Assert.Null(result.Count);
    }

    [Fact]
    public void MissingMismatchedDuplicateOrInvalidNativeSamplesCannotClaimAPacket()
    {
        var path = new byte[100]; Put(path, 90, 0);
        foreach (var samples in new IReadOnlyList<BeltCargoSample>?[]
        { null, Array.Empty<BeltCargoSample>(), new[] { Sample(1) }, new[] { Sample(0), Sample(0) }, new[] { Sample(0, 1003, 0) } })
        {
            var result = Capture(path, 90, 10, samples);
            Assert.Equal("unavailable", result.State); Assert.Null(result.ItemId);
        }
    }

    [Fact]
    public void AmbiguousIdElsewhereInSelectedSegmentIsNotAcceptedAsTheRear()
    {
        var path = new byte[100]; Put(path, 70, 0); Put(path, 90, 0);
        var result = Capture(path, 70, 30, new[] { Sample(0) });
        Assert.Equal("unavailable", result.State); Assert.Equal("ambiguous_cargo_reference", result.ReasonCode);
    }

    [Fact]
    public void ReadOnlyCapturePreservesBytesAndExistingActionHashes()
    {
        var path = new byte[100]; Put(path, 90, 0); var before = path.ToArray();
        var entity = new FactoryEntitySnapshot { ComponentKind = "belt", ObjectId = 10, ItemId = 2001 };
        var state = CanonicalStateHash.Factory(entity); var endpoint = CanonicalStateHash.FactoryEndpoint(entity);
        var config = CanonicalStateHash.FactoryConfiguration(entity);
        entity.BeltCargo = new() { RearPickup = Capture(path, 90, 10, new[] { Sample(0) }) };
        Assert.Equal(before, path); Assert.Equal(state, CanonicalStateHash.Factory(entity));
        Assert.Equal(endpoint, CanonicalStateHash.FactoryEndpoint(entity));
        Assert.Equal(config, CanonicalStateHash.FactoryConfiguration(entity));
    }

    private static BeltRearPickupSnapshot Capture(byte[] path, int start, int length, IReadOnlyList<BeltCargoSample>? samples, bool closed = false)
    {
        Assert.True(BeltCargoObservationPolicy.TryGetWindow(path.Length, start, length, out var windowStart, out var windowLength, out _));
        return BeltRearPickupObservationPolicy.Capture(path.Skip(windowStart).Take(windowLength).ToArray(),
            windowStart, path.Length, start, length, closed, samples, 42);
    }
    private static BeltCargoSample Sample(int id, int item = 1003, int count = 1, int inc = 0) =>
        new() { CargoId = id, ItemId = item, StackCount = count, Inc = inc };
    private static void Put(byte[] path, int start, int id)
    {
        for (var i = 0; i < 5; i++) path[start + i] = (byte)(246 + i);
        for (var i = 5; i < 9; i++, id /= 100) path[start + i] = (byte)(id % 100 + 1);
        path[start + 9] = 255;
    }
}
