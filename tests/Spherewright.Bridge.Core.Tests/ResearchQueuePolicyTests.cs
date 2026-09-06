using Spherewright.Bridge.Core.Progression;
using Spherewright.Contracts.Progression;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class ResearchQueuePolicyTests
{
    [Fact]
    public void PriorityPreservesEveryOtherTechnologyAndEmptySlotWithoutChangingInput()
    {
        var queue = new[] { 3402, 1202, 2701, 1608, 0, 0, 0, 0 };
        var original = queue.ToArray();
        var order = ResearchQueuePolicy.Prioritize(queue, 3402, 2701, true);
        Assert.Equal(new[] { 2, 0, 1, 3, 4, 5, 6, 7 }, order);
        Assert.Equal(new[] { 2701, 3402, 1202, 1608, 0, 0, 0, 0 }, order.Select(i => queue[i]));
        Assert.Equal(original, queue);
        Assert.Equal(Enumerable.Range(0, queue.Length), order.OrderBy(i => i));
    }

    [Theory]
    [InlineData("paused")] [InlineData("missing")] [InlineData("head")] [InlineData("prerequisite")]
    [InlineData("duplicate")] [InlineData("hole")] [InlineData("negative")] [InlineData("oversized")]
    [InlineData("wrong_head")]
    public void UnsafeOrMeaninglessPriorityIsRejectedBeforeNativeCall(string error)
    {
        var queue = new[] { 3402, 1202, 2701, 0 };
        var current = 3402; var target = 2701; var ready = true;
        if (error == "paused") current = 0;
        if (error == "missing") target = 2702;
        if (error == "head") target = 3402;
        if (error == "prerequisite") ready = false;
        if (error == "duplicate") queue[1] = 2701;
        if (error == "hole") queue[1] = 0;
        if (error == "negative") queue[3] = -1;
        if (error == "oversized") queue = queue.Concat(new int[61]).ToArray();
        if (error == "wrong_head") current = 1202;
        Assert.Throws<ArgumentException>(() => ResearchQueuePolicy.Prioritize(queue, current, target, ready));
    }

    [Theory]
    [InlineData("hash")] [InlineData("required")] [InlineData("level")] [InlineData("unlock")]
    [InlineData("unlock_tick")]
    public void PriorityProofCannotHideResearchProgressOrUnlockChanges(string mutation)
    {
        var tech = new TechStateSnapshot { TechId = 2701, HashRequired = 12000, HashUploaded = 30, CurrentLevel = 1, MaximumLevel = 1 };
        var before = ResearchQueuePolicy.ProgressHash(new[] { tech });
        if (mutation == "hash") tech.HashUploaded++;
        if (mutation == "required") tech.HashRequired++;
        if (mutation == "level") tech.CurrentLevel++;
        if (mutation == "unlock") tech.Unlocked = true;
        if (mutation == "unlock_tick") tech.UnlockTick++;
        Assert.NotEqual(before, ResearchQueuePolicy.ProgressHash(new[] { tech }));
    }

    [Fact]
    public void QueuedFlagOrCatalogOrderIsNotResearchProgress()
    {
        var a = new TechStateSnapshot { TechId = 1, HashUploaded = 1 };
        var b = new TechStateSnapshot { TechId = 2, HashUploaded = 2 };
        var before = ResearchQueuePolicy.ProgressHash(new[] { a, b });
        a.IsQueued = true;
        Assert.Equal(before, ResearchQueuePolicy.ProgressHash(new[] { b, a }));
    }
}
