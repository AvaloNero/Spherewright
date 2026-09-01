using Spherewright.Bridge.Core.Safety;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class BeltConnectionProofTests
{
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
