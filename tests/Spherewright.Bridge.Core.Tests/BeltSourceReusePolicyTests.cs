using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Factory;
using Spherewright.Contracts.Actions;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class BeltSourceReusePolicyTests
{
    [Theory]
    [InlineData(754,2001,2001,0,0,0,false,0,1,true)]
    [InlineData(754,2002,2002,0,0,0,false,0,0,true)]
    [InlineData(754,2003,2003,0,0,0,false,0,1,true)]
    [InlineData(0,2001,2001,0,0,0,false,0,1,false)]
    [InlineData(-1,2001,2001,0,0,0,false,0,1,false)]
    [InlineData(65537,2001,2001,0,0,0,false,0,1,false)]
    [InlineData(754,2001,2002,0,0,0,false,0,1,false)]
    [InlineData(754,2101,2101,0,0,0,false,0,1,false)]
    [InlineData(754,2001,2001,1,0,0,false,0,1,false)]
    [InlineData(754,2001,2001,0,755,0,false,0,1,false)]
    [InlineData(754,2001,2001,0,0,1,false,0,1,false)]
    [InlineData(754,2001,2001,0,0,0,true,0,1,false)]
    [InlineData(754,2001,2001,0,0,0,false,755,1,false)]
    [InlineData(754,2001,2001,0,0,0,false,0,2,false)]
    public void OnlyFlatSameGradeOpenSourceWithFreeOutput(int id,int oldItem,int newItem,int slot,int destination,
        float tilt,bool closed,int output,int inputs,bool expected) => Assert.Equal(expected,
        BeltSourceReusePolicy.Supports(id,oldItem,newItem,slot,destination,tilt,closed,output,inputs));

    [Fact]
    public void PartitionRetainsEveryNewPointButNotTheZeroCostSource()
    {
        var points = new[] { P(0), P(1), P(2), P(3) };
        Assert.True(BeltSourceReusePolicy.TrySeparateNewPoints(points, P(0), out var newPoints));
        Assert.Equal(3, newPoints.Count);
        Assert.Equal(new[] {1f,2f,3f}, newPoints.Select(p=>p.X));
        points[1].X=99;
        Assert.Equal(1f,newPoints[0].X);
    }
    [Fact]
    public void PartitionIsNotAnOccupancyExemption()
    {
        Assert.True(BeltSourceReusePolicy.TrySeparateNewPoints(new[]{P(0),P(1),P(2)},P(0),out var newPoints));
        Assert.False(BeltBuildOccupancyPolicy.TryValidateNewPath(newPoints,new[]{new BeltBuildObstacle(99,P(1))},out var failure));
        Assert.Equal("belt_path_existing_overlap",failure!.Reason);
    }

    [Fact]
    public void AReusedSourceMustExistExactlyOnceAtItsBoundCentre()
    {
        Assert.True(BeltSourceReusePolicy.HasUniqueSourceCentre(754,P(0),new[]{new BeltBuildObstacle(754,P(0)),new BeltBuildObstacle(755,P(1))}));
        Assert.False(BeltSourceReusePolicy.HasUniqueSourceCentre(754,P(0),new[]{new BeltBuildObstacle(755,P(0))}));
        Assert.False(BeltSourceReusePolicy.HasUniqueSourceCentre(754,P(0),new[]{new BeltBuildObstacle(754,P(1))}));
        Assert.False(BeltSourceReusePolicy.HasUniqueSourceCentre(754,P(0),new[]{new BeltBuildObstacle(754,P(0)),new BeltBuildObstacle(754,P(0))}));
    }
    [Theory]
    [InlineData(755)] [InlineData(-1)]
    public void OldColocatedBeltsAndPrebuildsAreNotLegitimizedByCoverReuse(int other)
    {
        Assert.False(BeltSourceReusePolicy.HasUniqueSourceCentre(754,P(0),new[]{new BeltBuildObstacle(754,P(0)),new BeltBuildObstacle(other,P(.25f))}));
        Assert.True(BeltSourceReusePolicy.HasUniqueSourceCentre(754,P(0),new[]{new BeltBuildObstacle(754,P(0)),new BeltBuildObstacle(other,P(.251f))}));
    }
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(256)] [InlineData(257)]
    public void RejectsInsufficientOrSaturatedPath(int count) => Assert.False(
        BeltSourceReusePolicy.TrySeparateNewPoints(Enumerable.Range(0,count).Select(_=>P(0)).ToArray(),P(0),out _));
    [Fact]
    public void RejectsMismatchedInvalidOrRaisedSourcePath()
    {
        Assert.False(BeltSourceReusePolicy.TrySeparateNewPoints(new[]{P(1),P(2),P(3)},P(0),out _));
        Assert.False(BeltSourceReusePolicy.TrySeparateNewPoints(new[]{P(0),P(float.NaN),P(3)},P(0),out _));
        Assert.False(BeltSourceReusePolicy.TrySeparateNewPoints(new[]{P(0),new Vector3Snapshot{Y=202},P(3)},P(0),out _));
    }
    [Theory]
    [InlineData(754,754,false,true)] [InlineData(754,754,true,false)] [InlineData(754,755,false,false)]
    [InlineData(0,0,false,false)] [InlineData(-1,-1,false,false)]
    public void CoverCannotTurnIntoRemovalOrAnotherIdentity(int expected,int actual,bool remove,bool valid) =>
        Assert.Equal(valid,BeltSourceReusePolicy.IsNonRemovingCover(expected,actual,remove));
    [Theory]
    [InlineData(100)] [InlineData(-100)]
    public void OnlyExactNewPrebuildOrBuiltOutputMayChange(int id)
    {
        var before=Connections(); var after=Connections();
        after[0]=new FactoryConnectionSnapshot{Slot=0,IsOutput=true,OtherObjectId=id,OtherSlot=1};
        Assert.True(BeltSourceReusePolicy.ProvesOnlyOutputChanged(before,after,id));
        after[4].OtherObjectId=99;
        Assert.False(BeltSourceReusePolicy.ProvesOnlyOutputChanged(before,after,id));
    }
    [Fact]
    public void RejectsMissingDuplicateOverwrittenOrWrongDirectionSlots()
    {
        var before=Connections(); var after=Connections();
        after[0]=new FactoryConnectionSnapshot{Slot=0,OtherObjectId=100,OtherSlot=1};
        Assert.False(BeltSourceReusePolicy.ProvesOnlyOutputChanged(before,after,100));
        after[0].IsOutput=true; after[0].OtherSlot=2;
        Assert.False(BeltSourceReusePolicy.ProvesOnlyOutputChanged(before,after,100));
        after[0].OtherSlot=1; before[0].OtherObjectId=101;
        Assert.False(BeltSourceReusePolicy.ProvesOnlyOutputChanged(before,after,100));
        before[0].OtherObjectId=0; after[15].Slot=14;
        Assert.False(BeltSourceReusePolicy.ProvesOnlyOutputChanged(before,after,100));
        Assert.False(BeltSourceReusePolicy.ProvesOnlyOutputChanged(Connections().Take(15).ToArray(),Connections(),100));
    }
    [Theory]
    [InlineData("a","a",true)] [InlineData("a","b",false)] [InlineData("","",false)] [InlineData(null,null,false)]
    public void MissingOrChangedEvidenceCannotProveConservation(string? a,string? b,bool valid) =>
        Assert.Equal(valid,BeltSourceReusePolicy.SameEvidence(a,b));

    private static Vector3Snapshot P(float x) => new(){X=x,Y=200};

    [Fact]
    public void ConfirmedCoverEchoCountsOnlyNewItemsAndRequiresTheRequestedSource()
    {
        var plan=Plan();
        Assert.True(BeltSourceReusePolicy.ConfirmsPlanEcho(plan,2001,754,0));
        Assert.False(BeltSourceReusePolicy.ConfirmsPlanEcho(plan,2001,755,0));
        Assert.False(BeltSourceReusePolicy.ConfirmsPlanEcho(plan,2002,754,0));
        Assert.False(BeltSourceReusePolicy.ConfirmsPlanEcho(plan,2001,754,755));
    }
    [Theory]
    [InlineData("missing")] [InlineData("mode")] [InlineData("source_mode")] [InlineData("source_id")]
    [InlineData("count")] [InlineData("cost")] [InlineData("path")] [InlineData("null_budget")] [InlineData("preservation")]
    public void MissingOrInconsistentEchoCannotAuthorizeOldDuplicateAnchorPlans(string mutation)
    {
        var plan=Plan();
        switch(mutation)
        {
            case "missing": plan.PlannedBeltPath=null; break;
            case "mode": plan.PlannedBeltPath!.NativeValidationMode="anchor_only_stage0"; break;
            case "source_mode": plan.PlannedBeltPath!.SourceBindingMode="remove_and_replace"; break;
            case "source_id": plan.PlannedBeltPath!.ReusedSourceObjectId=755; break;
            case "preservation": plan.PlannedBeltPath!.SourcePreservationMode=null; break;
            case "count": plan.PlannedBeltPath!.NewObjectCount=3; break;
            case "cost": plan.ItemBudget[0].Count=3; break;
            case "path": plan.PlannedPath=null!; break;
            case "null_budget": plan.ItemBudget=null!; break;
        }
        Assert.False(BeltSourceReusePolicy.ConfirmsPlanEcho(plan,2001,754,0));
    }
    [Fact]
    public void FreeAndNativeDevicePortsDoNotPretendToReuseABelt()
    {
        var p=Plan();p.PlannedBeltPath!.ReusedSourceObjectId=null;p.PlannedBeltPath.SourceBindingMode="native_device_port";
        Assert.True(BeltSourceReusePolicy.ConfirmsPlanEcho(p,2001,754,0));
        p.SourceObjectId=null;p.PlannedBeltPath.SourceBindingMode="none";
        Assert.True(BeltSourceReusePolicy.ConfirmsPlanEcho(p,2001,0,0));
        p.PlannedBeltPath.ReusedSourceObjectId=754;
        Assert.False(BeltSourceReusePolicy.ConfirmsPlanEcho(p,2001,0,0));
    }
    private static PreparedNormalAction Plan() => new()
    {
        Prepared=true,BuildKind="belt",SourceObjectId=754,PlannedPath=new(){P(1),P(2)},
        ItemBudget=new(){new ActionItemBudget{ItemId=2001,Count=2,Direction="construction-consumption"}},
        PlannedBeltPath=new(){NativeValidationMode="full_path_stage1",SourceBindingMode="non_removing_belt_cover",ReusedSourceObjectId=754,NewObjectCount=2,SourcePreservationMode=BeltSourceRotationPolicy.PreservationMode}
    };
    private static List<FactoryConnectionSnapshot> Connections() => Enumerable.Range(0,16)
        .Select(i=>new FactoryConnectionSnapshot{Slot=i}).ToList();
}
