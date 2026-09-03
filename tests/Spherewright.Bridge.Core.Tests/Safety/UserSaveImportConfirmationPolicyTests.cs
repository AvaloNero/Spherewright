using Spherewright.Bridge.Core.Safety;
using Xunit;

namespace Spherewright.Bridge.Core.Tests.Safety;

public sealed class UserSaveImportConfirmationPolicyTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    public void IsCommitDeclared_RejectsMissingConversationOrBoundaryDeclaration(
        bool userConfirmedInConversation,
        bool acknowledgeOriginalSaveRemainsUnchanged,
        bool acknowledgeJournalStartsAtImport)
    {
        Assert.False(UserSaveImportConfirmationPolicy.IsCommitDeclared(
            userConfirmedInConversation,
            acknowledgeOriginalSaveRemainsUnchanged,
            acknowledgeJournalStartsAtImport));
    }

    [Fact]
    public void IsCommitDeclared_AcceptsAllThreeDeclarations()
    {
        Assert.True(UserSaveImportConfirmationPolicy.IsCommitDeclared(true, true, true));
    }
}
