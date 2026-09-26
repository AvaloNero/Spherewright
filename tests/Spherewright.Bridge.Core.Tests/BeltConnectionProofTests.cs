using Spherewright.Bridge.Core.Safety;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class BeltConnectionProofTests
{
    [Theory]
    [InlineData(42, false, 42, true)]
    [InlineData(42, true, 42, false)]
    [InlineData(42, false, 41, false)]
    [InlineData(42, false, 0, false)]
    [InlineData(0, false, 0, true)]
    [InlineData(0, true, 0, true)]
    [InlineData(0, false, 99, false)]
    [InlineData(0, true, 99, false)]
    [InlineData(0, false, -99, false)]
    public void InputMatches_ProvesDirectedSourceOrEmptyInput(int expected, bool isOutput, int actual, bool matches)
    {
        Assert.Equal(matches, BeltConnectionProof.InputMatches(expected, isOutput, actual));
    }

    [Fact]
    public void OutputMatches_RequiresTheExpectedDirectedNeighbor()
    {
        Assert.True(BeltConnectionProof.OutputMatches(42, actualIsOutput: true, actualObjectId: 42));
        Assert.False(BeltConnectionProof.OutputMatches(42, actualIsOutput: false, actualObjectId: 42));
        Assert.False(BeltConnectionProof.OutputMatches(42, actualIsOutput: true, actualObjectId: 41));
    }

    [Fact]
    public void OutputMatches_RequiresAProvenFreeEndWhenNoNeighborIsExpected()
    {
        Assert.True(BeltConnectionProof.OutputMatches(0, actualIsOutput: false, actualObjectId: 0));
        Assert.True(BeltConnectionProof.OutputMatches(0, actualIsOutput: true, actualObjectId: 0));
        Assert.False(BeltConnectionProof.OutputMatches(0, actualIsOutput: true, actualObjectId: 99));
        Assert.False(BeltConnectionProof.OutputMatches(0, actualIsOutput: false, actualObjectId: 99));
    }
}
