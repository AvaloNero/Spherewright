using Spherewright.Bridge.Core.Journals;
using Spherewright.Contracts.Journals;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class GameplayFirstOccurrenceDetectorTests
{
    [Fact]
    public void ManualAndProductionLineFirsts_AreIndependent()
    {
        var detector = new GameplayFirstOccurrenceDetector();

        var manual = detector.ObserveManualCounts(new Dictionary<int, long> { [1101] = 1 });
        var production = detector.ObserveProductionLineCounts(new Dictionary<int, long> { [1101] = 2 });
        var repeatedManual = detector.ObserveManualCounts(new Dictionary<int, long> { [1101] = 3 });

        Assert.Single(manual);
        Assert.Equal(GameplayJournalEventKinds.ManualItemFirst, manual[0].Kind);
        Assert.Single(production);
        Assert.Equal(GameplayJournalEventKinds.ProductionLineItemFirst, production[0].Kind);
        Assert.Empty(repeatedManual);
    }

    [Fact]
    public void HistoricalSeeds_AreNeverMisreportedAsNewFirsts()
    {
        var detector = new GameplayFirstOccurrenceDetector(
            knownManualItemIds: new[] { 1101 },
            knownProductionLineItemIds: new[] { 1102 },
            knownResearchIds: new[] { 1001 });

        Assert.Empty(detector.ObserveManualCounts(new Dictionary<int, long> { [1101] = 20 }));
        Assert.Empty(detector.ObserveProductionLineCounts(new Dictionary<int, long> { [1102] = 5 }));
        Assert.False(detector.TryObserveResearchSelection(1001));
        Assert.True(detector.TryObserveResearchSelection(2001));
        Assert.False(detector.TryObserveResearchSelection(2001));
    }

    [Fact]
    public void NonPositiveCounts_DoNotCreateEvents()
    {
        var detector = new GameplayFirstOccurrenceDetector();

        var detected = detector.ObserveProductionLineCounts(new Dictionary<int, long>
        {
            [0] = 10,
            [1101] = 0,
            [1102] = -1,
        });

        Assert.Empty(detected);
    }
}
