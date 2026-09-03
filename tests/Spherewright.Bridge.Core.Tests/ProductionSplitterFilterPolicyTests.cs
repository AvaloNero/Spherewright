using Spherewright.Bridge.Core.Diagnostics;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class ProductionSplitterFilterPolicyTests
{
    [Theory]
    [InlineData(0, true, 1112, true)]
    [InlineData(0, false, 1112, true)]
    [InlineData(1112, true, 1112, true)]
    [InlineData(1112, true, 1109, false)]
    [InlineData(1112, false, 1112, false)]
    [InlineData(1112, false, 1109, true)]
    public void AllowsItem_MatchesNativePriorityOutputSemantics(
        int filterItemId,
        bool isPriorityOutput,
        int itemId,
        bool expected)
    {
        Assert.Equal(
            expected,
            ProductionSplitterFilterPolicy.AllowsItem(filterItemId, isPriorityOutput, itemId));
    }

    [Fact]
    public void AllowsItem_RejectsInvalidIdentityInputs()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ProductionSplitterFilterPolicy.AllowsItem(-1, true, 1112));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ProductionSplitterFilterPolicy.AllowsItem(0, true, 0));
    }
}
