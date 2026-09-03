namespace Spherewright.Bridge.Core.Safety;

public static class UserSaveImportConfirmationPolicy
{
    public static bool IsCommitDeclared(
        bool userConfirmedInConversation,
        bool acknowledgeOriginalSaveRemainsUnchanged,
        bool acknowledgeJournalStartsAtImport) =>
        userConfirmedInConversation
        && acknowledgeOriginalSaveRemainsUnchanged
        && acknowledgeJournalStartsAtImport;
}
