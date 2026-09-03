using System.Text.Json;
using Spherewright.Contracts.Actions;
using Spherewright.Contracts.Diagnostics;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Factory;
using Spherewright.Contracts.Journals;
using Spherewright.Contracts.Logistics;
using Spherewright.Contracts.Players;
using Spherewright.Contracts.Protocol;
using Spherewright.Contracts.Progression;
using Spherewright.Contracts.Sessions;
using Spherewright.Contracts.Versioning;
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
    public void FactoryEntity_ExposesDedicatedConfigurationHashContract()
    {
        var snapshot = new FactoryEntitySnapshot
        {
            StateHash = "sha256:full",
            ConfigurationStateHash = "sha256:configuration",
            ConfigurationStateHashVersion = 1,
            EndpointStateHash = "sha256:endpoint",
        };

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(snapshot, JsonOptions));

        Assert.Equal("sha256:configuration", json.RootElement.GetProperty("configurationStateHash").GetString());
        Assert.Equal(1, json.RootElement.GetProperty("configurationStateHashVersion").GetInt32());
    }

    [Fact]
    public void PlayerState_ExposesMechaResearchReservationWithoutSaveIdentity()
    {
        var snapshot = new PlayerStateSnapshot
        {
            AutoManageResearchItems = true,
            MechaResearchPower = 0d,
            MechaResearchItemBuffer = new List<MechaResearchItemSnapshot>
            {
                new MechaResearchItemSnapshot
                {
                    ItemId = 6001,
                    Name = "Electromagnetic Matrix",
                    PointCount = 903_600,
                    WholeItemCount = 251,
                    RemainderPoints = 0,
                },
            },
        };

        var text = JsonSerializer.Serialize(snapshot, JsonOptions);
        using var json = JsonDocument.Parse(text);
        var reserved = json.RootElement.GetProperty("mechaResearchItemBuffer")[0];

        Assert.True(json.RootElement.GetProperty("autoManageResearchItems").GetBoolean());
        Assert.Equal(0d, json.RootElement.GetProperty("mechaResearchPower").GetDouble());
        Assert.Equal(903_600, reserved.GetProperty("pointCount").GetInt32());
        Assert.Equal(251, reserved.GetProperty("wholeItemCount").GetInt32());
        Assert.Equal(0, reserved.GetProperty("remainderPoints").GetInt32());
        Assert.DoesNotContain("saveName", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BridgeStatus_UsesStableCamelCaseContract()
    {
        var status = new BridgeStatus
        {
            BridgeConnected = true,
            BridgeInstanceId = "instance",
            PluginVersion = SpherewrightProduct.CurrentVersion,
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
    public void OverseerWindowAndFinding_DoNotExposeProtectedSaveIdentity()
    {
        var window = new OverseerWindowSnapshot
        {
            State = OverseerWindowStates.Ready,
            StartGameTick = 100,
            EndGameTick = 700,
            ElapsedGameTicks = 600,
            ElapsedGameSeconds = 10,
            WallClockElapsedSeconds = 3_610,
            ExcludedNonGameSeconds = 3_600,
            CrossedSessionBoundary = true,
        };
        var finding = new OverseerFindingSnapshot
        {
            Kind = OverseerFindingKinds.MaterialShortage,
            Confidence = OverseerFindingConfidences.Confirmed,
            Severity = OverseerFindingSeverities.Stopped,
            PlanetId = 104,
            ObjectId = 774,
            ItemId = 1112,
            Summary = "Missing diamond",
        };

        var json = JsonSerializer.Serialize(new { window, finding }, JsonOptions);

        Assert.Contains("\"excludedNonGameSeconds\":3600", json, StringComparison.Ordinal);
        Assert.DoesNotContain("saveName", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("saveIdentity", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("protectedSaveKey", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("authToken", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("planToken", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("filePath", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OverseerProduction_DeclaresNativeWindowAndOmitsPrivateIdentity()
    {
        var snapshot = new OverseerProductionSnapshot
        {
            SessionId = "session",
            CapturedAtGameTick = 12_000,
            SnapshotId = "opaque-snapshot",
            TotalFactoryCount = 2,
            ReturnedFactoryCount = 1,
            RequestedItemIds = new List<int> { 6001, 6003 },
            RateSource = OverseerRateSources.NativeFactoryStatisticsLevel0,
            Window = new OverseerWindowSnapshot
            {
                State = OverseerWindowStates.Ready,
                StartGameTick = 11_401,
                EndGameTick = 12_000,
                ElapsedGameTicks = 600,
                ElapsedGameSeconds = 10,
            },
            Planets = new List<OverseerPlanetProductionSnapshot>
            {
                new OverseerPlanetProductionSnapshot
                {
                    FactoryIndex = 0,
                    PlanetId = 104,
                    PlanetName = "Owned planet",
                    Production = new List<ProductionRateSnapshot>
                    {
                        new ProductionRateSnapshot
                        {
                            PlanetId = 104,
                            ItemId = 6003,
                            ProducedCount = 1,
                            ActualProductionPerMinute = 6,
                            RateSource = OverseerRateSources.NativeFactoryStatisticsLevel0,
                            TheoreticalCoverage = OverseerTheoreticalCoverageStates.Unavailable,
                        },
                    },
                },
            },
        };

        var json = JsonSerializer.Serialize(snapshot, JsonOptions);
        using var parsed = JsonDocument.Parse(json);

        Assert.Equal(
            OverseerRateSources.NativeFactoryStatisticsLevel0,
            parsed.RootElement.GetProperty("rateSource").GetString());
        Assert.Equal(600, parsed.RootElement.GetProperty("window").GetProperty("elapsedGameTicks").GetInt64());
        Assert.Equal(
            OverseerTheoreticalCoverageStates.Unavailable,
            parsed.RootElement.GetProperty("planets")[0]
                .GetProperty("production")[0]
                .GetProperty("theoreticalCoverage")
                .GetString());
        Assert.DoesNotContain("saveName", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("protectedSaveKey", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("authToken", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("planToken", json, StringComparison.OrdinalIgnoreCase);
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
            DroneCapacity = 50,
            VesselCapacity = 10,
            DroneTripRangeRaw = 180d,
            VesselTripRangeRaw = 12d,
            WarpEnableDistanceRaw = 0.5d,
            StateHash = "sha256:live",
            ConfigurationStateHash = "sha256:config",
            FleetStateHash = "sha256:fleet",
        };

        var json = JsonSerializer.Serialize(station, JsonOptions);
        using var parsed = JsonDocument.Parse(json);

        Assert.Equal(180d, parsed.RootElement.GetProperty("droneTripRangeRaw").GetDouble());
        Assert.Equal(50_000, parsed.RootElement.GetProperty("requestedChargeEnergyPerTick").GetInt64());
        Assert.Equal(6_000_000, parsed.RootElement.GetProperty("maximumChargePowerWatts").GetInt64());
        Assert.Equal("sha256:config", parsed.RootElement.GetProperty("configurationStateHash").GetString());
        Assert.Equal(50, parsed.RootElement.GetProperty("droneCapacity").GetInt32());
        Assert.Equal(10, parsed.RootElement.GetProperty("vesselCapacity").GetInt32());
        Assert.Equal("sha256:fleet", parsed.RootElement.GetProperty("fleetStateHash").GetString());
        Assert.DoesNotContain("saveName", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("filePath", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LogisticsStationFleetTransfer_BindsDedicatedFleetHash()
    {
        var request = new PrepareLogisticsStationFleetTransferRequest
        {
            PlanetId = 104,
            StationEntityId = 920,
            Direction = LogisticsStationFleetTransferDirections.PlayerToStation,
            ItemId = LogisticsFleetItemIds.Drone,
            Count = 10,
            ExpectedPlayerStateHash = "sha256:player",
            ExpectedStationFleetStateHash = "sha256:fleet",
        };

        using var parsed = JsonDocument.Parse(JsonSerializer.Serialize(request, JsonOptions));
        Assert.Equal("player-to-station", parsed.RootElement.GetProperty("direction").GetString());
        Assert.Equal(5001, parsed.RootElement.GetProperty("itemId").GetInt32());
        Assert.Equal("sha256:fleet", parsed.RootElement.GetProperty("expectedStationFleetStateHash").GetString());
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

    [Fact]
    public void LogisticsStationBeltConfiguration_BindsOutputPortAndPublicStorageIndex()
    {
        var request = new PrepareConfigureBuildingRequest
        {
            PlanetId = 104,
            EntityId = 920,
            Mode = BuildingConfigurationModes.LogisticsStationBelt,
            StationBeltSlotIndex = 3,
            StationBeltStorageIndex = 0,
            ExpectedFactoryStateHash = "sha256:factory",
            ExpectedStationConfigurationStateHash = "sha256:station-config",
        };

        var json = JsonSerializer.Serialize(request, JsonOptions);
        using var parsed = JsonDocument.Parse(json);

        Assert.Equal("logistics-station-belt", parsed.RootElement.GetProperty("mode").GetString());
        Assert.Equal(3, parsed.RootElement.GetProperty("stationBeltSlotIndex").GetInt32());
        Assert.Equal(0, parsed.RootElement.GetProperty("stationBeltStorageIndex").GetInt32());
        Assert.Equal("sha256:station-config", parsed.RootElement.GetProperty("expectedStationConfigurationStateHash").GetString());
    }

    [Fact]
    public void Dismantle_BindsStableEndpointAndPlayerHashes()
    {
        var request = new PrepareDismantleRequest
        {
            PlanetId = 102,
            ObjectId = 17,
            ExpectedEndpointStateHash = "sha256:endpoint",
            ExpectedPlayerStateHash = "sha256:player",
        };
        var prepared = new PreparedNormalAction
        {
            ActionKind = NormalActionKinds.Dismantle,
            TargetObjectId = 17,
            PlannedResourceNodeIds = new List<int> { 245, 249, 252, 255, 256 },
        };

        using var requestJson = JsonDocument.Parse(JsonSerializer.Serialize(request, JsonOptions));
        using var preparedJson = JsonDocument.Parse(JsonSerializer.Serialize(prepared, JsonOptions));

        Assert.Equal("sha256:endpoint", requestJson.RootElement.GetProperty("expectedEndpointStateHash").GetString());
        Assert.Equal("sha256:player", requestJson.RootElement.GetProperty("expectedPlayerStateHash").GetString());
        Assert.Equal(17, preparedJson.RootElement.GetProperty("targetObjectId").GetInt32());
        Assert.Equal(5, preparedJson.RootElement.GetProperty("plannedResourceNodeIds").GetArrayLength());
    }
}
