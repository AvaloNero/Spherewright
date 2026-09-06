using Spherewright.Bridge.Core.Factory;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class SorterEndpointObservationPolicyTests
{
    [Theory]
    [InlineData(1, 32, 16, true)]
    [InlineData(1, 28, 12, true)]
    [InlineData(1, 27, 12, false)]
    [InlineData(0, 32, 12, false)]
    [InlineData(-1, 32, 12, false)]
    [InlineData(int.MaxValue, int.MaxValue, 1, false)]
    [InlineData(1, 32, 17, false)]
    [InlineData(1, 32, 0, false)]
    public void NativeConnectionStrideIsBoundedBeforeReading(int id, int poolLength, int slots, bool expected) =>
        Assert.Equal(expected, SorterEndpointObservationPolicy.CanReadConnections(id, poolLength, slots));

    [Fact]
    public void FixedSlotsReportEmptyOccupiedAndPrebuildWithoutMovingOrMutatingInputs()
    {
        var input = Slots(3);
        input[1].Occupied = true; input[1].OtherObjectId = 763; input[1].OtherSlot = 0;
        input[2].Occupied = true; input[2].OtherObjectId = -5; input[2].OtherSlot = 1;
        var result = SorterEndpointObservationPolicy.Create(false, 123, input);
        Assert.Equal("observed", result.State); Assert.Equal("native_slots", result.Kind);
        Assert.Null(result.ReasonCode); Assert.Equal(123, result.CapturedAtGameTick);
        Assert.False(result.Endpoints[0].Occupied); Assert.Equal(0, result.Endpoints[0].OtherObjectId);
        Assert.True(result.Endpoints[2].Occupied); Assert.Equal(-5, result.Endpoints[2].OtherObjectId);
        input[0].Position.X = 99; input[0].Outward.Z = -1; input.Clear();
        Assert.Equal(0, result.Endpoints[0].Position.X); Assert.Equal(1, result.Endpoints[0].Outward.Z);
        Assert.Equal(3, result.Endpoints.Count);
    }

    [Fact]
    public void FourVirtualBeltPosesDoNotClaimFreePhysicalSlots()
    {
        var result = SorterEndpointObservationPolicy.Create(true, 3, Belt());
        Assert.Equal("observed", result.State); Assert.Equal("belt_virtual", result.Kind);
        Assert.All(result.Endpoints, point =>
        {
            Assert.Equal(-1, point.Slot); Assert.Null(point.Occupied);
            Assert.Null(point.OtherObjectId); Assert.Null(point.OtherSlot);
        });
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
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    public void InvalidEvidenceNeverLeaksPartialPoses(int variant)
    {
        var input = Slots(2);
        switch (variant)
        {
            case 0: input[1].Position.X = float.NaN; break;
            case 1: input[1].Outward.Y = float.PositiveInfinity; break;
            case 2: input[1].Position.Z = 10001; break;
            case 3: input[1].Outward.Z = 0; break;
            case 4: input[1].Outward.Z = 2; break;
            case 5: input[1].Index = 0; break;
            case 6: input[1].Slot = 0; break;
            case 7: input[1].Occupied = null; break;
            case 8: input[1].OtherObjectId = 42; break;
            case 9: input[1].OtherSlot = 0; break;
            case 10: input[1].Occupied = true; input[1].OtherObjectId = 42; input[1].OtherSlot = 16; break;
            case 11: input[1].Position = null!; break;
            case 12: input[1] = null!; break;
        }
        Unknown(SorterEndpointObservationPolicy.Create(false, 1, input));
    }

    [Fact]
    public void CountsTicksAndVirtualOccupancyAreFailClosed()
    {
        Unknown(SorterEndpointObservationPolicy.Create(false, 1, null));
        Unknown(SorterEndpointObservationPolicy.Create(false, 1, Slots(0)));
        Unknown(SorterEndpointObservationPolicy.Create(false, 1, Slots(17)));
        Unknown(SorterEndpointObservationPolicy.Create(false, -1, Slots(1)));
        Unknown(SorterEndpointObservationPolicy.Create(true, 1, Slots(3)));
        var points = Belt(); points[3].Occupied = false;
        Unknown(SorterEndpointObservationPolicy.Create(true, 1, points));
        Assert.Equal("observed", SorterEndpointObservationPolicy.Create(false, 0, Slots(16)).State);
    }

    [Fact]
    public void OptionalGeometryDoesNotChangeExistingActionConfigurationOrEndpointHashes()
    {
        var entity = new FactoryEntitySnapshot { SessionId = "owned", PlanetId = 104, ObjectId = 760, ItemId = 2309 };
        var hashes = new[] { CanonicalStateHash.Factory(entity), CanonicalStateHash.FactoryConfiguration(entity), CanonicalStateHash.FactoryEndpoint(entity) };
        entity.SorterEndpoints = SorterEndpointObservationPolicy.Create(false, 1, Slots(3));
        Assert.Equal(hashes, new[] { CanonicalStateHash.Factory(entity), CanonicalStateHash.FactoryConfiguration(entity), CanonicalStateHash.FactoryEndpoint(entity) });
    }

    private static void Unknown(SorterEndpointObservation result)
    {
        Assert.Equal("unavailable", result.State); Assert.NotNull(result.ReasonCode); Assert.Empty(result.Endpoints);
    }
    private static List<SorterEndpointSnapshot> Slots(int count) => Enumerable.Range(0, count).Select(i => new SorterEndpointSnapshot
    {
        Index = i, Slot = i, Position = new Vector3Snapshot { X = i }, Outward = new Vector3Snapshot { Z = 1 },
        Occupied = false, OtherObjectId = 0,
    }).ToList();
    private static List<SorterEndpointSnapshot> Belt() => Enumerable.Range(0, 4).Select(i => new SorterEndpointSnapshot
    {
        Index = i, Slot = -1, Outward = new Vector3Snapshot { X = i % 2 == 0 ? (i == 0 ? 1 : -1) : 0, Z = i % 2 == 1 ? (i == 1 ? 1 : -1) : 0 },
    }).ToList();
}
