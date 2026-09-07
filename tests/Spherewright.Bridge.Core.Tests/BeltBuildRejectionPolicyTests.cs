using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Errors;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class BeltBuildRejectionPolicyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PureNativeShortageRemainsRejectedWithMaterialSpecificRecovery(bool hasRetainedSource)
    {
        var conditions = new List<string> { "Ok", "NotEnoughItem", "NotEnoughItem" };
        if (hasRetainedSource) conditions.Add("Ok");
        var error = BeltBuildRejectionPolicy.DescribeInventoryShortage(conditions, true, false);
        Assert.NotNull(error);
        Assert.Equal(BridgeErrorCodes.InventoryInsufficient, error.Code);
        Assert.True(error.Retryable);
        Assert.Contains("not placement-approved", error.Message);
        Assert.Contains("Do not retry with unchanged inventory", error.Recovery);
        Assert.Contains("normal handcraft or transfer", error.Recovery);
        Assert.Contains("revalidate the complete path", error.Recovery);
        Assert.Contains("same explicit endpoint bindings", error.Recovery);
        Assert.Contains("checks may not have run", error.Recovery);
        Assert.DoesNotContain("Do not retry this site", error.Recovery);
    }

    [Theory]
    [InlineData("Collision")]
    [InlineData("OutOfReach")]
    [InlineData("Failure")]
    [InlineData("NotEnoughItem ")]
    [InlineData("notenoughitem")]
    [InlineData("DSP returned NotEnoughItem")]
    [InlineData(null)]
    public void MixedOrUnknownConditionsDoNotBecomeAMaterialOnlyDiagnosis(string? other)
    {
        Assert.Null(BeltBuildRejectionPolicy.DescribeInventoryShortage(
            new[] { "NotEnoughItem", other! }, true, false));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void CoverFailuresAreNotMaskedByShortage(bool coverMatches, bool unexpectedCover)
    {
        Assert.Null(BeltBuildRejectionPolicy.DescribeInventoryShortage(
            new[] { "Ok", "NotEnoughItem" }, coverMatches, unexpectedCover));
    }

    [Fact]
    public void MissingEmptyOrOversizedEvidenceCannotDescribeAPureShortage()
    {
        Assert.Null(BeltBuildRejectionPolicy.DescribeInventoryShortage(null, true, false));
        Assert.Null(BeltBuildRejectionPolicy.DescribeInventoryShortage(Array.Empty<string>(), true, false));
        Assert.Null(BeltBuildRejectionPolicy.DescribeInventoryShortage(new[] { "NotEnoughItem" }, true, false));
        Assert.Null(BeltBuildRejectionPolicy.DescribeInventoryShortage(
            Enumerable.Repeat("NotEnoughItem", BeltBuildOccupancyPolicy.MaximumPathPoints + 1).ToArray(), true, false));
    }

    [Fact]
    public void AllOkEvidenceDoesNotCreateARejectionOrApproveAPath()
    {
        Assert.Null(BeltBuildRejectionPolicy.DescribeInventoryShortage(new[] { "Ok", "Ok" }, true, false));
    }
}
