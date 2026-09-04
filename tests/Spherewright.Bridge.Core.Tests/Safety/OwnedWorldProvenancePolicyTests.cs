using Spherewright.Bridge.Core.Safety;
using Xunit;

namespace Spherewright.Bridge.Core.Tests.Safety;

public sealed class OwnedWorldProvenancePolicyTests
{
    [Fact]
    public void ProtectedResume_AcceptsExactLegacyM0TicketIdentity()
    {
        const string legacySaveName = "Spherewright_M0_20260801_010203_0123456789abcdef0123456789abcdef";

        Assert.True(OwnedWorldProvenancePolicy.MatchesProtectedSaveIdentity(
            legacySaveName,
            legacySaveName));
    }

    [Theory]
    [InlineData("Spherewright_New_20260904_123456_0123456789abcdef0123456789abcdef")]
    [InlineData("Spherewright_Imported_20260904_123456_0123456789abcdef0123456789abcdef")]
    public void ManualSpherewrightLikeName_DoesNotMatchWithoutProtectedTicketIdentity(
        string manuallyLoadedName)
    {
        Assert.False(OwnedWorldProvenancePolicy.MatchesProtectedSaveIdentity(
            ticketOwnedSaveName: null,
            manuallyLoadedName));
        Assert.False(OwnedWorldProvenancePolicy.MatchesProtectedSaveIdentity(
            "Spherewright_New_different",
            manuallyLoadedName));
    }

    [Fact]
    public void ReloadedOriginalPlayerSave_DoesNotMatchImportedCopyTicket()
    {
        Assert.False(OwnedWorldProvenancePolicy.MatchesProtectedSaveIdentity(
            "Spherewright_Imported_20260904_123456_0123456789abcdef0123456789abcdef",
            "PlayersOriginalSave"));
    }
}
