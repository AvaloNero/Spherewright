using System.Text.Json;
using Spherewright.Contracts.Actions;
using Xunit;

namespace Spherewright.Contracts.Tests;

public sealed class BeltPathPlanContractTests
{
    [Fact]
    public void OptionalEchoIsAbsentInLegacyResultsButExplicitForCurrentCoverPlans()
    {
        var options=new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase};
        Assert.Null(JsonSerializer.Deserialize<PreparedNormalAction>("{}",options)!.PlannedBeltPath);
        var original=new PreparedNormalAction{PlannedBeltPath=new BeltPathPlanSnapshot
            {NativeValidationMode="full_path_stage1",SourceBindingMode="non_removing_belt_cover",ReusedSourceObjectId=754,NewObjectCount=3}};
        var json=JsonSerializer.Serialize(original,options);
        var result=JsonSerializer.Deserialize<PreparedNormalAction>(json,options)!.PlannedBeltPath!;
        Assert.Equal("full_path_stage1",result.NativeValidationMode);
        Assert.Equal("non_removing_belt_cover",result.SourceBindingMode);
        Assert.Equal(754,result.ReusedSourceObjectId);
        Assert.Equal(3,result.NewObjectCount);
    }
}
