namespace Spherewright.Contracts.Sessions;

public static class OwnedSaveStates
{
    public const string None = "none";
    public const string WaitingForWorld = "waiting_for_world";
    public const string WaitingToSave = "waiting_to_save";
    public const string Saved = "saved";
    public const string SaveFailed = "save_failed";
}
