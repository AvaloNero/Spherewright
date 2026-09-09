using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class BeltSourceRotationPolicyTests
{
    [Fact]
    public void CompletedBlueprintBeltUsesExactDerivedOrientationNotAPrebuildPose()
    {
        // Captured 2001 completion quaternion; geometry derivation remains a Plugin/live check.
        var actual = new QuaternionSnapshot { X=.219716623f, Y=.399377346f, Z=-.8845228f, W=.0992058441f };
        var derived = new QuaternionSnapshot { X=actual.X, Y=actual.Y, Z=actual.Z, W=actual.W };
        Assert.True(BeltSourceRotationPolicy.CanReadSegment(82,10,10,0,10));
        Assert.True(BeltSourceRotationPolicy.MatchesNativeRotation(actual,derived));
        Assert.False(BeltSourceRotationPolicy.MatchesNativeRotation(actual,new QuaternionSnapshot { W=1 }));
        derived.X += .000001f;
        Assert.False(BeltSourceRotationPolicy.MatchesNativeRotation(actual,derived));
        Assert.False(BeltSourceRotationPolicy.CanReadSegment(82,10,10,0,11));
    }

    [Theory]
    [InlineData(1,100,128,75,25,true)]
    [InlineData(1,8192,16384,8191,1,true)]
    [InlineData(0,100,128,0,1,false)]
    [InlineData(1,0,128,0,1,false)]
    [InlineData(1,8193,16384,0,1,false)]
    [InlineData(1,100,99,0,1,false)]
    [InlineData(1,100,128,-1,1,false)]
    [InlineData(1,100,128,0,0,false)]
    [InlineData(1,100,128,75,26,false)]
    [InlineData(1,100,128,int.MaxValue,int.MaxValue,false)]
    public void NativeReadIsBoundedToTwoValidSegmentEndpoints(int id,int length,int capacity,int start,int count,bool allowed) =>
        Assert.Equal(allowed,BeltSourceRotationPolicy.CanReadSegment(id,length,capacity,start,count));

    [Fact]
    public void OnlyCurrentExactNativeOrientationOrItsQuaternionSignTwinIsAllowed()
    {
        var q = new QuaternionSnapshot { W=1 };
        Assert.True(BeltSourceRotationPolicy.MatchesNativeRotation(q,new QuaternionSnapshot { W=1 }));
        Assert.True(BeltSourceRotationPolicy.MatchesNativeRotation(q,new QuaternionSnapshot { W=-1 }));
        Assert.False(BeltSourceRotationPolicy.MatchesNativeRotation(q,new QuaternionSnapshot { W=1, X=.000001f }));
    }
    [Fact]
    public void MissingMalformedOrZeroDerivationCannotExplainARotationChange()
    {
        var q = new QuaternionSnapshot { W=1 };
        Assert.False(BeltSourceRotationPolicy.MatchesNativeRotation(q,null));
        Assert.False(BeltSourceRotationPolicy.MatchesNativeRotation(q,new QuaternionSnapshot { W=float.NaN }));
        Assert.False(BeltSourceRotationPolicy.MatchesNativeRotation(q,new QuaternionSnapshot { W=float.PositiveInfinity }));
        Assert.False(BeltSourceRotationPolicy.MatchesNativeRotation(q,new QuaternionSnapshot()));
        Assert.False(BeltSourceRotationPolicy.MatchesNativeRotation(q,new QuaternionSnapshot { W=2 }));
    }
}
