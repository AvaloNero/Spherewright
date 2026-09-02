using System.Text.Json;
using Spherewright.Contracts.Actions;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Journals;
using Spherewright.Contracts.Logistics;
using Spherewright.Contracts.Protocol;
using Spherewright.Contracts.Progression;
using Spherewright.Contracts.Sessions;
using Xunit;

namespace Spherewright.Contracts.Tests;

public sealed class ProtocolContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public void ProgressionSelection_UsesDedicatedStableHashContract()
    {
        var snapshot = new ProgressionStateSnapshot
        {
            StateHash = "sha256:full",
            SelectionStateHash = "sha256:selection",
            SelectionStateHashVersion = 1,
        };
        var request = new PrepareSelectResearchRequest
        {
            PlanetId = 104,
            TechId = 1604,
            ExpectedSelectionStateHash = "sha256:selection",
        };

        using var snapshotJson = JsonDocument.Parse(JsonSerializer.Serialize(snapshot, JsonOptions));
        using var requestJson = JsonDocument.Parse(JsonSerializer.Serialize(request, JsonOptions));

        Assert.Equal("sha256:selection", snapshotJson.RootElement.GetProperty("selectionStateHash").GetString());
        Assert.Equal(1, snapshotJson.RootElement.GetProperty("selectionStateHashVersion").GetInt32());
        Assert.Equal("sha256:selection", requestJson.RootElement.GetProperty("expectedSelectionStateHash").GetString());
    }

    [Fact]
    public void BridgeStatus_UsesStableCamelCaseContract()
    {
        var status = new BridgeStatus
        {
            BridgeConnected = true,
            BridgeInstanceId = "instance",
            PluginVersion = "0.1.0",
            ProtocolVersion = ProtocolConstants.CurrentVersion,
            GameVersion = "0.10.34.28529",
            GameLoaded = false,
            WritesConfigured = false,
            WriteHealth = WriteHealthStates.Healthy,
        };

        var json = JsonSerializer.Serialize(status, JsonOptions);

        Assert.Contains("\"bridgeConnected\":true", json, StringComparison.Ordinal);
        Assert.Contains("\"protocolVersion\":1", json, StringComparison.Ordinal);
        Assert.DoesNotContain("authToken", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pipeName", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ErrorCodes_HaveStableWireValues()
    {
        Assert.Equal("BRIDGE_NOT_READY", BridgeErrorCodes.BridgeNotReady);
        Assert.Equal("AUTH_FAILED", BridgeErrorCodes.AuthFailed);
        Assert.Equal("STALE_REVISION", BridgeErrorCodes.StaleRevision);
        Assert.Equal("ACTION_OUTCOME_UNKNOWN", BridgeErrorCodes.ActionOutcomeUnknown);
        Assert.Equal("SANDBOX_MODE_ACTIVE", BridgeErrorCodes.SandboxModeActive);
        Assert.Equal("confirmed_disabled", SandboxModeStates.ConfirmedDisabled);
    }

    [Fact]
    public void RequestEnvelope_DefaultsToCurrentProtocol()
    {
        var request = new BridgeRequestEnvelope<EmptyPayload>();

        Assert.Equal(ProtocolConstants.CurrentVersion, request.ProtocolVersion);
        Assert.Equal(BridgeMessageTypes.Request, request.MessageType);
    }

    [Fact]
    public void GameplayJournal_ExposesTimesWithoutRawSaveIdentity()
    {
        var journal = new GameplayJournalSnapshot
        {
            SessionId = "session",
            JournalId = "opaque-hash",
            CreatedAtActualTime = "2026-09-01T00:00:00+08:00",
            TrackingStartedAtGameTick = 120,
            TrackingStartedAtGameTime = "000d 00:00:02",
            DurableThroughSequence = 0,
            PersistencePending = true,
            PersistenceError = "IOException",
            Entries = new List<GameplayJournalEntry>
            {
                new GameplayJournalEntry
                {
                    Sequence = 1,
                    Kind = GameplayJournalEventKinds.ManualItemFirst,
                    ItemId = 1101,
                    ActualTime = "2026-09-01T00:00:01+08:00",
                    GameTick = 180,
                    GameTime = "000d 00:00:03",
                },
            },
        };

        var json = JsonSerializer.Serialize(journal, JsonOptions);
        using var parsed = JsonDocument.Parse(json);
        var entry = parsed.RootElement.GetProperty("entries")[0];

        Assert.Equal("2026-09-01T00:00:01+08:00", entry.GetProperty("actualTime").GetString());
        Assert.Equal(180, entry.GetProperty("gameTick").GetInt64());
        Assert.Equal(0, parsed.RootElement.GetProperty("durableThroughSequence").GetInt64());
        Assert.True(parsed.RootElement.GetProperty("persistencePending").GetBoolean());
        Assert.Equal("IOException", parsed.RootElement.GetProperty("persistenceError").GetString());
        Assert.DoesNotContain("saveName", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("filePath", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LogisticsStation_UsesExplicitRawSettingsAndStableHashes()
    {
        var station = new LogisticsStationSnapshot
        {
            SessionId = "session",
            PlanetId = 104,
            EntityId = 920,
            StationId = 2,
            GalacticStationId = 7,
            BuildingItemId = 2104,
            RequestedChargeEnergyPerTick = 50_000,
            RequestedChargePowerWatts = 3_000_000,
            MaximumChargeEnergyPerTick = 100_000,
            MaximumChargePowerWatts = 6_000_000,
            DroneTripRangeRaw = 180d,
            VesselTripRangeRaw = 12d,
            WarpEnableDistanceRaw = 0.5d,
            StateHash = "sha256:live",
            ConfigurationStateHash = "sha256:config",
        };

        var json = JsonSerializer.Serialize(station, JsonOptions);
        using var parsed = JsonDocument.Parse(json);

        Assert.Equal(180d, parsed.RootElement.GetProperty("droneTripRangeRaw").GetDouble());
        Assert.Equal(50_000, parsed.RootElement.GetProperty("requestedChargeEnergyPerTick").GetInt64());
        Assert.Equal(6_000_000, parsed.RootElement.GetProperty("maximumChargePowerWatts").GetInt64());
        Assert.Equal("sha256:config", parsed.RootElement.GetProperty("configurationStateHash").GetString());
        Assert.DoesNotContain("saveName", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("filePath", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LogisticsStationConfiguration_BindsSeparateConfigurationHashAndSlotIntent()
    {
        var request = new PrepareConfigureBuildingRequest
        {
            PlanetId = 104,
            EntityId = 920,
            Mode = BuildingConfigurationModes.LogisticsStationStorage,
            StationStorageIndex = 1,
            StationItemId = 1106,
            StationMaximumCount = 5_000,
            StationLocalLogic = LogisticsStorageLogics.Demand,
            StationRemoteLogic = LogisticsStorageLogics.Supply,
            ExpectedFactoryStateHash = "sha256:factory",
            ExpectedStationConfigurationStateHash = "sha256:station-config",
        };

        var json = JsonSerializer.Serialize(request, JsonOptions);
        using var parsed = JsonDocument.Parse(json);

        Assert.Equal("logistics-station-storage", parsed.RootElement.GetProperty("mode").GetString());
        Assert.Equal(1, parsed.RootElement.GetProperty("stationStorageIndex").GetInt32());
        Assert.Equal("demand", parsed.RootElement.GetProperty("stationLocalLogic").GetString());
        Assert.Equal("sha256:station-config", parsed.RootElement.GetProperty("expectedStationConfigurationStateHash").GetString());
    }

    [Fact]
    public void LogisticsStationChargeConfiguration_UsesExplicitPowerAndConfigurationHash()
    {
        var request = new PrepareConfigureBuildingRequest
        {
            PlanetId = 104,
            EntityId = 920,
            Mode = BuildingConfigurationModes.LogisticsStationCharge,
            StationMaximumChargePowerWatts = 12_000_000,
            ExpectedFactoryStateHash = "sha256:factory",
            ExpectedStationConfigurationStateHash = "sha256:station-config",
        };

        var json = JsonSerializer.Serialize(request, JsonOptions);
        using var parsed = JsonDocument.Parse(json);

        Assert.Equal("logistics-station-charge", parsed.RootElement.GetProperty("mode").GetString());
        Assert.Equal(12_000_000, parsed.RootElement.GetProperty("stationMaximumChargePowerWatts").GetInt64());
        Assert.Equal("sha256:station-config", parsed.RootElement.GetProperty("expectedStationConfigurationStateHash").GetString());
    }
}
