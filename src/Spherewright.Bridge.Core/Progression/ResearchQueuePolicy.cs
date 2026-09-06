using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Progression;

namespace Spherewright.Bridge.Core.Progression;

public static class ResearchQueuePolicy
{
    // The native SortTechQueue argument is a permutation of EVERY array index,
    // including trailing empty slots. Never write the queue or research fields directly.
    public static int[] Prioritize(IReadOnlyList<int> queue, int currentTech, int targetTech, bool prerequisitesUnlocked)
    {
        if (queue is null || queue.Count < 2 || queue.Count > 64 || targetTech <= 0
            || currentTech <= 0 || currentTech != queue[0] || !prerequisitesUnlocked
            || queue.Any(id => id < 0))
            throw new ArgumentException("An active bounded queue and completed native prerequisites are required.");
        var nonempty = queue.Where(id => id > 0).ToArray();
        if (nonempty.Distinct().Count() != nonempty.Length
            || !queue.Take(nonempty.Length).SequenceEqual(nonempty))
            throw new ArgumentException("Ambiguous repeated levels or holes in the native queue are unsupported.");
        var index = Array.IndexOf(nonempty, targetTech);
        if (index <= 0) throw new ArgumentException("Choose an already queued technology behind the active head.");
        return new[] { index }.Concat(Enumerable.Range(0, queue.Count).Where(i => i != index)).ToArray();
    }

    public static string ProgressHash(IEnumerable<TechStateSnapshot> technologies) => CanonicalStateHash.Combine(
        "research-progress-preservation-v1", technologies.OrderBy(t => t.TechId).Select(t => (object)CanonicalStateHash.Combine(
            "technology", t.TechId, t.HashUploaded, t.HashRequired, t.CurrentLevel, t.MaximumLevel, t.Unlocked, t.UnlockTick)).ToArray());
}
