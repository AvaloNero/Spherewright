using Spherewright.Contracts.Journals;

namespace Spherewright.Bridge.Core.Journals;

public sealed class GameplayFirstOccurrenceDetector
{
    private readonly HashSet<int> _manualItemIds;
    private readonly HashSet<int> _productionLineItemIds;
    private readonly HashSet<int> _researchIds;

    public GameplayFirstOccurrenceDetector(
        IEnumerable<int>? knownManualItemIds = null,
        IEnumerable<int>? knownProductionLineItemIds = null,
        IEnumerable<int>? knownResearchIds = null)
    {
        _manualItemIds = new HashSet<int>(knownManualItemIds ?? Enumerable.Empty<int>());
        _productionLineItemIds = new HashSet<int>(knownProductionLineItemIds ?? Enumerable.Empty<int>());
        _researchIds = new HashSet<int>(knownResearchIds ?? Enumerable.Empty<int>());
    }

    public IReadOnlyList<GameplayItemFirstOccurrence> ObserveManualCounts(
        IReadOnlyDictionary<int, long> cumulativeCounts)
    {
        return ObserveItemCounts(
            cumulativeCounts,
            _manualItemIds,
            GameplayJournalEventKinds.ManualItemFirst);
    }

    public IReadOnlyList<GameplayItemFirstOccurrence> ObserveProductionLineCounts(
        IReadOnlyDictionary<int, long> producedThisTick)
    {
        return ObserveItemCounts(
            producedThisTick,
            _productionLineItemIds,
            GameplayJournalEventKinds.ProductionLineItemFirst);
    }

    public bool TryObserveResearchSelection(int techId)
    {
        return techId > 0 && _researchIds.Add(techId);
    }

    private static IReadOnlyList<GameplayItemFirstOccurrence> ObserveItemCounts(
        IReadOnlyDictionary<int, long> counts,
        HashSet<int> knownIds,
        string kind)
    {
        var detected = new List<GameplayItemFirstOccurrence>();
        foreach (var pair in counts.OrderBy(pair => pair.Key))
        {
            if (pair.Key <= 0 || pair.Value <= 0 || !knownIds.Add(pair.Key))
            {
                continue;
            }

            detected.Add(new GameplayItemFirstOccurrence(pair.Key, pair.Value, kind));
        }

        return detected;
    }
}

public sealed class GameplayItemFirstOccurrence
{
    public GameplayItemFirstOccurrence(int itemId, long observedCount, string kind)
    {
        ItemId = itemId;
        ObservedCount = observedCount;
        Kind = kind;
    }

    public int ItemId { get; }

    public long ObservedCount { get; }

    public string Kind { get; }
}
