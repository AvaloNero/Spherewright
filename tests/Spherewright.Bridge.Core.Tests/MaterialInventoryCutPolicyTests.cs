using Spherewright.Bridge.Core.Factory;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class MaterialInventoryCutPolicyTests
{
    [Fact]
    public void OmittedEmptyAndBoundedExplicitSelectionAreAccepted()
    {
        Assert.Null(MaterialInventoryCutPolicy.ValidateSelection(null));
        Assert.Null(MaterialInventoryCutPolicy.ValidateSelection(Array.Empty<int>()));
        Assert.Null(MaterialInventoryCutPolicy.ValidateSelection(Enumerable.Range(1, 256).ToArray()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void InvalidObjectIdsReject(int id) =>
        Assert.NotNull(MaterialInventoryCutPolicy.ValidateSelection(new[] { id }));

    [Fact]
    public void DuplicatesAndOversizeSelectionReject()
    {
        Assert.NotNull(MaterialInventoryCutPolicy.ValidateSelection(new[] { 1, 1 }));
        Assert.NotNull(MaterialInventoryCutPolicy.ValidateSelection(Enumerable.Range(1, 257).ToArray()));
    }

    [Fact]
    public void RepeatedPathUsesNoAdditionalBudgetButCannotChangeIdentity()
    {
        var budget = new MaterialInventoryCutPolicy();
        Assert.True(budget.TryReservePath(1, 8192, 512, out var seen));
        Assert.False(seen);
        Assert.True(budget.TryReservePath(1, 8192, 512, out seen));
        Assert.True(seen);
        Assert.False(budget.TryReservePath(1, 8191, 512, out _));
        Assert.False(budget.TryReservePath(1, 8192, 511, out _));
    }

    [Fact]
    public void TotalCellsAndBeltsCannotExceedBudgets()
    {
        var cells = new MaterialInventoryCutPolicy();
        for (var id = 1; id <= 4; id++) Assert.True(cells.TryReservePath(id, 8192, 1, out _));
        Assert.False(cells.TryReservePath(5, 1, 1, out _));
        var belts = new MaterialInventoryCutPolicy();
        for (var id = 1; id <= 8; id++) Assert.True(belts.TryReservePath(id, 10, 512, out _));
        Assert.False(belts.TryReservePath(9, 10, 1, out _));
    }

    [Fact]
    public void NativePerPathAndDistinctPathBoundsRemainStrict()
    {
        var budget = new MaterialInventoryCutPolicy();
        Assert.False(budget.TryReservePath(0, 1, 1, out _));
        Assert.False(budget.TryReservePath(1, 0, 1, out _));
        Assert.False(budget.TryReservePath(1, 8193, 1, out _));
        Assert.False(budget.TryReservePath(1, 10, 513, out _));
        for (var id = 1; id <= 64; id++) Assert.True(budget.TryReservePath(id, 10, 1, out _));
        Assert.False(budget.TryReservePath(65, 10, 1, out _));
        Assert.True(budget.TryReservePath(1, 10, 1, out var seen));
        Assert.True(seen);
    }

    [Fact]
    public void WholePathSupportsMoreThan64StacksWithoutWideningSegmentReader()
    {
        var references = Enumerable.Range(0, 65).Select(id => new BeltCargoReference(id, id * 10 + 5)).ToArray();
        var samples = Enumerable.Range(0, 65).Select(id => new BeltCargoSample
        { CargoId = id, ItemId = 1206, StackCount = 4, Inc = 0 }).ToArray();
        Assert.False(BeltCargoObservationPolicy.TrySummarize(references, samples, out _, out _));
        Assert.True(BeltCargoObservationPolicy.TrySummarizePath(references, samples, out var totals, out _));
        Assert.Equal(260, Assert.Single(totals).Count);
        samples[64].CargoId = 0;
        Assert.False(BeltCargoObservationPolicy.TrySummarizePath(references, samples, out totals, out _));
        Assert.Empty(totals);
    }

    [Fact]
    public void MissingWholePathReadbackNeverBecomesObservedEmpty()
    {
        Assert.False(BeltCargoObservationPolicy.TrySummarizePath(null, null, out var totals, out _));
        Assert.Empty(totals);
        Assert.True(BeltCargoObservationPolicy.TrySummarizePath(Array.Empty<BeltCargoReference>(),
            Array.Empty<BeltCargoSample>(), out totals, out _));
        Assert.Empty(totals);
    }

    [Theory]
    [InlineData(820)]
    [InlineData(821)]
    public void WholePathCargoLimitIsBounded(int count)
    {
        var references = Enumerable.Range(0, count).Select(id => new BeltCargoReference(id, id * 10 + 5)).ToArray();
        var samples = Enumerable.Range(0, count).Select(id => new BeltCargoSample
        { CargoId = id, ItemId = 1206, StackCount = 1, Inc = 0 }).ToArray();
        Assert.Equal(count == 820, BeltCargoObservationPolicy.TrySummarizePath(references, samples, out _, out _));
    }

    [Fact]
    public void OptionalObservationDoesNotChangeExistingActionBindingHashes()
    {
        var entity = new FactoryEntitySnapshot { ObjectId = 1, ItemId = 2001,
            ComponentKind = "belt", ObjectKind = FactoryObjectKinds.Entity, SessionId = "fixture", PlanetId = 104 };
        var before = new[] { CanonicalStateHash.Factory(entity), CanonicalStateHash.FactoryEndpoint(entity),
            CanonicalStateHash.FactoryConfiguration(entity) };
        entity.MaterialInventoryCut = new MaterialInventoryCutSnapshot { State = "observed", CapturedAtGameTick = 100,
            CargoPaths = new List<MaterialCargoPathSnapshot> { new() { PathId = 2, ItemCount = 30 } } };
        Assert.Equal(before, new[] { CanonicalStateHash.Factory(entity), CanonicalStateHash.FactoryEndpoint(entity),
            CanonicalStateHash.FactoryConfiguration(entity) });
    }
}
