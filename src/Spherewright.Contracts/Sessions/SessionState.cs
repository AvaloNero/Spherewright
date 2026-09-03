namespace Spherewright.Contracts.Sessions;

public sealed class SessionState
{
    public bool BridgeConnected { get; set; }

    public bool GameLoaded { get; set; }

    public bool OwnedBySpherewright { get; set; }

    public bool AccessRestricted { get; set; }

    public string GameVersion { get; set; } = string.Empty;

    public string? SessionId { get; set; }

    public string? SaveName { get; set; }

    public long? GameTick { get; set; }

    public long Revision { get; set; }

    public int? LocalPlanetId { get; set; }

    public string? LocalPlanetName { get; set; }

    public string PeacefulMode { get; set; } = PeacefulModeStates.Unknown;

    public string SandboxMode { get; set; } = SandboxModeStates.Unknown;

    public float? ResourceMultiplier { get; set; }

    public bool WritesAllowed { get; set; }

    public string WriteHealth { get; set; } = WriteHealthStates.Healthy;

    public string? WriteQuarantineActionId { get; set; }

    public List<WriteBlocker> WriteBlockers { get; set; } = new List<WriteBlocker>();

    public string OwnedSaveState { get; set; } = OwnedSaveStates.None;

    public string? OwnedSaveError { get; set; }

    public long? LastOwnedSaveGameTick { get; set; }

    public bool RestartResumeAvailable { get; set; }

    public string? RestartResumeToken { get; set; }

    public bool FlightCheckpointAvailable { get; set; }

    public string? FlightCheckpointId { get; set; }

    public string? FlightCheckpointReloadToken { get; set; }

    public int? FlightCheckpointOriginPlanetId { get; set; }

    public int? FlightCheckpointDestinationPlanetId { get; set; }

    public long? FlightCheckpointGameTick { get; set; }

    public bool CurrentSessionLoadedFromFlightCheckpoint { get; set; }

    public bool UserSaveImportConfigured { get; set; }

    public List<string> Capabilities { get; set; } = new List<string>();
}
