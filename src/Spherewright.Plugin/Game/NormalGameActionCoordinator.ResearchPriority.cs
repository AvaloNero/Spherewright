using Spherewright.Bridge.Core.Progression;
using Spherewright.Contracts.Actions;
using Spherewright.Contracts.Sessions;

namespace Spherewright.Plugin.Game;

internal sealed partial class NormalGameActionCoordinator
{
    private static int[]? TryPrepareResearchPriority(int techId)
    {
        var history = GameMain.history;
        var tech = LDB.techs.Select(techId);
        if (history is null || tech is null || history.TechUnlocked(techId)) return null;
        // Exact prerequisite semantics from native AlterCurrentTech, including implicit
        // prerequisites and the maximum-level requirement. Enqueued alone is insufficient.
        var ready = (tech.PreTechs ?? Array.Empty<int>()).All(id => history.TechUnlocked(id, tech.PreTechsMax))
            && (tech.PreTechsImplicit ?? Array.Empty<int>()).All(id => history.TechUnlocked(id, tech.PreTechsMax));
        try { return ResearchQueuePolicy.Prioritize(history.techQueue, history.currentTech, techId, ready); }
        catch (ArgumentException) { return null; }
    }

    private void ExecuteResearchPriorityOnMainThread(ActionRecord action)
    {
        var history = GameMain.history;
        var before = _reader.GetProgressionStateOnMainThread(action.SessionId,
            new LocalPlanetRequest { PlanetId = action.PlanetId }).Value
            ?? throw new InvalidOperationException("Research priority pre-execution readback unavailable.");
        var order = TryPrepareResearchPriority(action.Plan.TechId);
        if (order is null || !order.SequenceEqual(action.Plan.ResearchQueueOrder!))
            throw new InvalidOperationException("Prepared native research priority is no longer valid.");
        var rawBefore = history.techQueue.ToArray();
        var expectedAfter = order.Select(i => rawBefore[i]).ToArray();
        var progressBefore = ResearchQueuePolicy.ProgressHash(before.Technologies);
        // The business path used by UIResearchQueue when a valid drag ends. No input,
        // direct array writes, dequeue, cancellation, completion or item injection.
        history.SortTechQueue(order);
        var after = _reader.GetProgressionStateOnMainThread(action.SessionId,
            new LocalPlanetRequest { PlanetId = action.PlanetId }).Value
            ?? throw new InvalidOperationException("Research priority readback unavailable; do not replay.");
        var inventoryAfter = CaptureInventory(GameMain.mainPlayer);
        if (!history.techQueue.SequenceEqual(expectedAfter) || history.currentTech != action.Plan.TechId
            || progressBefore != ResearchQueuePolicy.ProgressHash(after.Technologies)
            || action.BeforeInventory.Keys.Concat(inventoryAfter.Keys).Distinct()
                .Any(id => GetCount(action.BeforeInventory, id) != GetCount(inventoryAfter, id)))
            throw new InvalidOperationException("Native research priority queue, progress or item preservation could not be proved.");
        action.ResearchQueueReadback = new ResearchQueueReadback
        {
            CapturedAtGameTick = GameMain.gameTick, BeforeQueue = before.TechQueue.ToList(), AfterQueue = after.TechQueue.ToList(),
            PreviousCurrentTechId = before.CurrentTechId, CurrentTechId = after.CurrentTechId,
            ResearchProgressPreserved = true, InventoryPreserved = true,
        };
        Complete(action, "Native research priority applied: exact reordered queue verified; all research progress and inventory preserved. Research still requires normal materials and time.");
    }
}
