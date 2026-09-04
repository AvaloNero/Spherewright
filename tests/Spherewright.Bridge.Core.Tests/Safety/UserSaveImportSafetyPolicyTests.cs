using Spherewright.Bridge.Core.Safety;
using Xunit;

namespace Spherewright.Bridge.Core.Tests.Safety;

public sealed class UserSaveImportSafetyPolicyTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void IsEnabled_RequiresWritesAndUserSaveImport(
        bool allowWrites,
        bool allowUserSaveImport)
    {
        Assert.False(UserSaveImportSafetyPolicy.IsEnabled(allowWrites, allowUserSaveImport));
    }

    [Fact]
    public void IsEnabled_AcceptsBothOptIns()
    {
        Assert.True(UserSaveImportSafetyPolicy.IsEnabled(
            allowWrites: true,
            allowUserSaveImport: true));
    }

    [Fact]
    public void MatchesPreparedCandidate_RequiresExactSessionRevisionAndGameData()
    {
        var preparedData = new object();

        Assert.True(UserSaveImportSafetyPolicy.MatchesPreparedCandidate(
            "session-a",
            "session-a",
            7,
            7,
            preparedData,
            preparedData));
        Assert.False(UserSaveImportSafetyPolicy.MatchesPreparedCandidate(
            "session-a",
            "session-b",
            7,
            7,
            preparedData,
            preparedData));
        Assert.False(UserSaveImportSafetyPolicy.MatchesPreparedCandidate(
            "session-a",
            "session-a",
            7,
            8,
            preparedData,
            preparedData));
        Assert.False(UserSaveImportSafetyPolicy.MatchesPreparedCandidate(
            "session-a",
            "session-a",
            7,
            7,
            preparedData,
            new object()));
    }

    [Theory]
    [InlineData(false, 900L, 900L)]
    [InlineData(true, 900L, null)]
    [InlineData(true, 900L, 899L)]
    [InlineData(true, 900L, 901L)]
    public void HasVerifiedCopyHeader_RejectsUnprovedCopy(
        bool saveReturnedTrue,
        long expectedGameTick,
        long? headerGameTick)
    {
        Assert.False(UserSaveImportSafetyPolicy.HasVerifiedCopyHeader(
            saveReturnedTrue,
            expectedGameTick,
            headerGameTick));
    }

    [Fact]
    public void HasVerifiedCopyHeader_AcceptsExactTickAfterSuccessfulSave()
    {
        Assert.True(UserSaveImportSafetyPolicy.HasVerifiedCopyHeader(
            saveReturnedTrue: true,
            expectedGameTick: 900,
            headerGameTick: 900));
    }
}
