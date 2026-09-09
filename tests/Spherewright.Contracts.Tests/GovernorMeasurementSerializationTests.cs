using System.Text.Json;
using Spherewright.Contracts.Factory;
using Xunit;

namespace Spherewright.Contracts.Tests;

public sealed class GovernorMeasurementSerializationTests
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public void MissingLegacyFieldsStillMeanSixHundredTicks()
    {
        Assert.Equal(600, JsonSerializer.Deserialize<GetGovernorPlanRequest>("{}", Options)!.MeasurementGameTicks);
        Assert.Equal(600, JsonSerializer.Deserialize<GovernorPlanSnapshot>("{}", Options)!.MeasurementGameTicks);
        Assert.Equal(600, JsonSerializer.Deserialize<GovernorThroughputValidationSnapshot>("{}", Options)!.MeasurementGameTicks);
    }

    [Fact]
    public void ExplicitMinuteRequestAndDeclarationHaveIndependentValidationDuration()
    {
        var request = JsonSerializer.Deserialize<GetGovernorPlanRequest>("{\"measurementGameTicks\":3600}", Options)!;
        Assert.Equal(3600, request.MeasurementGameTicks); Assert.Equal(36000, request.ValidationGameTicks);
        var plan = new GovernorPlanSnapshot { MeasurementGameTicks = 3600,
            ThroughputValidation = new GovernorThroughputValidationSnapshot { MeasurementGameTicks = 3600, RequiredGameTicks = 36000 } };
        var decoded = JsonSerializer.Deserialize<GovernorPlanSnapshot>(JsonSerializer.Serialize(plan, Options), Options)!;
        Assert.Equal(3600, decoded.MeasurementGameTicks); Assert.Equal(3600, decoded.ThroughputValidation!.MeasurementGameTicks);
        Assert.Equal(36000, decoded.ThroughputValidation.RequiredGameTicks);
    }
}
