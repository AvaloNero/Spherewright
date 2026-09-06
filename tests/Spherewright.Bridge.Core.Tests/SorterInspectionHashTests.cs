using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Errors;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class SorterInspectionHashTests
{
    [Fact]
    public void CorrectConfigurationHashPassesEvenIfLiveFullHashDiffers() =>
        Assert.Null(SorterFilterPolicy.ValidateInspectionHash("configuration", "full", "configuration"));

    [Fact]
    public void HashEqualityDoesNotCauseFalseWrongDomainRejection() =>
        Assert.Null(SorterFilterPolicy.ValidateInspectionHash("same", "same", "same"));

    [Fact]
    public void ExactFullHashIsNonRetryableRequestErrorNotFakeStateChurn()
    {
        var error = SorterFilterPolicy.ValidateInspectionHash("full", "full", "configuration")!;
        Assert.Equal(BridgeErrorCodes.InvalidRequest, error.Code); Assert.False(error.Retryable);
        Assert.Contains("root configurationStateHash", error.Message);
        Assert.Contains("storage-capacity still uses full stateHash", error.Recovery);
    }

    [Fact]
    public void RealStalenessRetainsFreshReadSemanticsAndDoesNotGuessItsCause()
    {
        var error = SorterFilterPolicy.ValidateInspectionHash("old", "new-full", "new-configuration")!;
        Assert.Equal(BridgeErrorCodes.StaleState, error.Code); Assert.True(error.Retryable);
        Assert.Contains("wrong hash domain", error.Message);
        Assert.Contains("not full stateHash", error.Recovery);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void MissingRequestHashFailsClosed(string? supplied)
    {
        var error = SorterFilterPolicy.ValidateInspectionHash(supplied, "full", "configuration")!;
        Assert.Equal(BridgeErrorCodes.InvalidRequest, error.Code); Assert.False(error.Retryable);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void MissingNativeConfigurationEvidenceCannotPass(string? configuration)
    {
        var error = SorterFilterPolicy.ValidateInspectionHash(configuration, "full", configuration)!;
        Assert.Equal(BridgeErrorCodes.BridgeNotReady, error.Code); Assert.False(error.Retryable);
    }

    [Fact]
    public void HashComparisonIsOrdinalNotCaseFolded() =>
        Assert.Equal(BridgeErrorCodes.StaleState,
            SorterFilterPolicy.ValidateInspectionHash("CONFIGURATION", "full", "configuration")!.Code);
}
