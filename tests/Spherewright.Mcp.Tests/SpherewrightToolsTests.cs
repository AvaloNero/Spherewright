using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Actions;
using Spherewright.Contracts.Celestial;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Factory;
using Spherewright.Contracts.Journals;
using Spherewright.Contracts.Players;
using Spherewright.Contracts.Power;
using Spherewright.Contracts.Progression;
using Spherewright.Contracts.Protocol;
using Spherewright.Contracts.Resources;
using Spherewright.Contracts.Sessions;
using Spherewright.Contracts.Testing;
using Spherewright.Mcp.BridgeClient;
using Spherewright.Mcp.Resources;
using Spherewright.Mcp.Tools;
using Xunit;

namespace Spherewright.Mcp.Tests;

public sealed class SpherewrightToolsTests
{
    [Fact]
    public void AssemblyRegistration_ExposesOnlySafeCurrentGateTools()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBridgeClient>(new FakeBridgeClient(SuccessResult()));
        services.AddMcpServer().WithToolsFromAssembly(typeof(SpherewrightTools).Assembly);
        using var provider = services.BuildServiceProvider();

        var tools = provider.GetServices<McpServerTool>().ToArray();

        Assert.Equal(
            new[]
            {
                "spherewright_commit_build",
                "spherewright_commit_configure_building",
                "spherewright_commit_dismantle",
                "spherewright_commit_handcraft",
                "spherewright_commit_harvest",
                "spherewright_commit_interplanetary_flight",
                "spherewright_commit_logistics_station_fleet_transfer",
                "spherewright_commit_move",
                "spherewright_commit_new_game",
                "spherewright_commit_quarantine_reconciliation",
                "spherewright_commit_refuel",
                "spherewright_commit_reload_flight_checkpoint",
                "spherewright_commit_resume_owned_game",
                "spherewright_commit_save",
                "spherewright_commit_save_import",
                "spherewright_commit_select_research",
                "spherewright_commit_transfer",
                "spherewright_get_action_result",
                "spherewright_get_build_catalog",
                "spherewright_get_gameplay_journal",
                "spherewright_get_local_star_system",
                "spherewright_get_player_state",
                "spherewright_get_power_summary",
                "spherewright_get_progression_state",
                "spherewright_get_recipe_catalog",
                "spherewright_get_session_state",
                "spherewright_get_status",
                "spherewright_inspect_assembler",
                "spherewright_inspect_factory_entity",
                "spherewright_inspect_resource_node",
                "spherewright_list_assemblers",
                "spherewright_list_factory_entities",
                "spherewright_list_resource_nodes",
                "spherewright_prepare_build",
                "spherewright_prepare_configure_building",
                "spherewright_prepare_dismantle",
                "spherewright_prepare_handcraft",
                "spherewright_prepare_harvest",
                "spherewright_prepare_interplanetary_flight",
                "spherewright_prepare_logistics_station_fleet_transfer",
                "spherewright_prepare_move",
                "spherewright_prepare_new_game",
                "spherewright_prepare_quarantine_reconciliation",
                "spherewright_prepare_refuel",
                "spherewright_prepare_reload_flight_checkpoint",
                "spherewright_prepare_resume_owned_game",
                "spherewright_prepare_save",
                "spherewright_prepare_save_import",
                "spherewright_prepare_select_research",
                "spherewright_prepare_transfer",
            },
            tools.Select(tool => tool.ProtocolTool.Name).OrderBy(name => name).ToArray());
        Assert.All(
            tools.Where(tool => tool.ProtocolTool.Name.StartsWith("spherewright_get_", StringComparison.Ordinal)
                || tool.ProtocolTool.Name.StartsWith("spherewright_list_", StringComparison.Ordinal)
                || tool.ProtocolTool.Name.StartsWith("spherewright_inspect_", StringComparison.Ordinal)),
            tool => Assert.True(tool.ProtocolTool.Annotations?.ReadOnlyHint));
        Assert.All(
            tools.Where(tool => tool.ProtocolTool.Name.StartsWith("spherewright_commit_", StringComparison.Ordinal)),
            tool => Assert.True(tool.ProtocolTool.Annotations?.DestructiveHint));
        Assert.DoesNotContain(tools, tool => tool.ProtocolTool.Name.Contains("basic_production_line", StringComparison.Ordinal));
    }

    [Fact]
    public void AssemblyRegistration_ExposesReadableOpeningMovementPlaybookResource()
    {
        var services = new ServiceCollection();
        services.AddMcpServer().WithResourcesFromAssembly(typeof(AgentPlaybookResources).Assembly);
        using var provider = services.BuildServiceProvider();

        var resource = Assert.Single(provider.GetServices<McpServerResource>());
        Assert.False(resource.IsTemplated);
        var descriptor = resource.ProtocolResource
            ?? throw new InvalidOperationException("Expected a direct MCP resource descriptor.");
        Assert.Equal(AgentPlaybookResources.OpeningMovementUri, descriptor.Uri);
        Assert.Equal("text/markdown", descriptor.MimeType);

        var contents = AgentPlaybookResources.GetOpeningMovementPlaybook();
        Assert.Equal(AgentPlaybookResources.OpeningMovementUri, contents.Uri);
        Assert.Contains("do not submit the same target again", contents.Text, StringComparison.Ordinal);
        Assert.Contains("about **5 m**", contents.Text, StringComparison.Ordinal);
        Assert.Contains("four targets", contents.Text, StringComparison.Ordinal);
        Assert.Contains("each direction **once**", contents.Text, StringComparison.Ordinal);
        Assert.Contains("movementState=Walk", contents.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void NewWorldCommit_AdvertisesDiscoverableOpeningPlaybook()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBridgeClient>(new FakeBridgeClient(SuccessResult()));
        services.AddMcpServer()
            .WithToolsFromAssembly(typeof(SpherewrightTools).Assembly)
            .WithResourcesFromAssembly(typeof(AgentPlaybookResources).Assembly);
        using var provider = services.BuildServiceProvider();

        var commitNewGame = Assert.Single(
            provider.GetServices<McpServerTool>(),
            tool => tool.ProtocolTool.Name == "spherewright_commit_new_game");
        var playbook = Assert.Single(provider.GetServices<McpServerResource>());
        var playbookDescriptor = playbook.ProtocolResource
            ?? throw new InvalidOperationException("Expected a direct MCP resource descriptor.");

        Assert.Contains(AgentPlaybookResources.OpeningMovementUri, commitNewGame.ProtocolTool.Description, StringComparison.Ordinal);
        Assert.Equal(AgentPlaybookResources.OpeningMovementUri, playbookDescriptor.Uri);
    }

    [Fact]
    public async Task StatusTool_ReturnsStructuredSuccess()
    {
        var result = await SpherewrightTools.GetStatusAsync(
            new FakeBridgeClient(SuccessResult()),
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.True(result.StructuredContent.HasValue);
        var structured = result.StructuredContent.Value;
        Assert.True(structured.GetProperty("success").GetBoolean());
        Assert.True(structured.GetProperty("status").GetProperty("bridgeConnected").GetBoolean());
        Assert.Equal(
            AgentPlaybookResources.OpeningMovementUri,
            structured.GetProperty("agentPlaybookResourceUri").GetString());
        Assert.Contains("before the first gameplay action", structured.GetProperty("recommendedFirstStep").GetString(), StringComparison.Ordinal);
        Assert.False(structured.TryGetProperty("authToken", out _));
    }

    [Fact]
    public async Task ActionResultTool_ReturnsStructuredMovementRecovery()
    {
        var bridge = new FakeBridgeClient(SuccessResult())
        {
            ActionResult = new ActionResultSnapshot
            {
                ActionId = "move-stall",
                ActionKind = NormalActionKinds.Move,
                State = NormalActionStates.ActionFailed,
                Terminal = true,
                Succeeded = false,
                Stalled = true,
                FailureKind = MovementFailureKinds.PositionStalled,
                StalledGameTicks = 180,
                RemainingDistance = 12.5,
                DoNotRetrySameTarget = true,
                RecommendedRecovery = MovementFailureRecoveryAdvisor.RecoverySummary,
                RecommendedShortMoveDistanceMeters = 5,
                OrthogonalProbeDistanceMeters = 4,
                MaximumOrthogonalProbeAttempts = 4,
            },
        };

        var result = await SpherewrightTools.GetActionResultAsync(
            bridge,
            "move-stall",
            CancellationToken.None);

        var action = result.StructuredContent!.Value.GetProperty("result");
        Assert.Equal("position_stalled", action.GetProperty("failureKind").GetString());
        Assert.Equal(180, action.GetProperty("stalledGameTicks").GetInt64());
        Assert.Equal(12.5, action.GetProperty("remainingDistance").GetDouble());
        Assert.True(action.GetProperty("doNotRetrySameTarget").GetBoolean());
        Assert.Equal(5, action.GetProperty("recommendedShortMoveDistanceMeters").GetDouble());
        Assert.Equal(4, action.GetProperty("orthogonalProbeDistanceMeters").GetDouble());
        Assert.Equal(4, action.GetProperty("maximumOrthogonalProbeAttempts").GetInt32());
    }

    [Fact]
    public async Task StatusTool_ReturnsStructuredBridgeNotReadyError()
    {
        var error = BridgeError.Create(
            BridgeErrorCodes.BridgeNotReady,
            "Bridge unavailable.",
            true,
            "Start DSP and retry.");

        var result = await SpherewrightTools.GetStatusAsync(
            new FakeBridgeClient(BridgeCallResult<BridgeStatus>.Failed(error)),
            CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(
            BridgeErrorCodes.BridgeNotReady,
            result.StructuredContent!.Value.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task ListAssemblersTool_MapsSessionLimitAndCursor()
    {
        var bridge = new FakeBridgeClient(SuccessResult())
        {
            ListResult = BridgeCallResult<ListAssemblersResult>.Succeeded(new ListAssemblersResult
            {
                Revision = 9,
            }),
        };

        var result = await SpherewrightTools.ListAssemblersAsync(
            bridge,
            "session-9",
            17,
            "cursor-9",
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("session-9", bridge.LastSessionId);
        Assert.Equal(17, bridge.LastListRequest?.Limit);
        Assert.Equal("cursor-9", bridge.LastListRequest?.Cursor);
        Assert.Equal(9, result.StructuredContent!.Value.GetProperty("result").GetProperty("revision").GetInt64());
    }

    [Fact]
    public async Task GameplayJournalTool_MapsOwnedSession()
    {
        var bridge = new FakeBridgeClient(SuccessResult());

        var result = await SpherewrightTools.GetGameplayJournalAsync(
            bridge,
            "session-journal",
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("session-journal", bridge.LastSessionId);
        Assert.Equal(
            "journal",
            result.StructuredContent!.Value.GetProperty("result").GetProperty("journalId").GetString());
    }

    [Fact]
    public async Task PrepareSelectResearch_MapsDedicatedSelectionHash()
    {
        var bridge = new FakeBridgeClient(SuccessResult());

        var result = await SpherewrightTools.PrepareSelectResearchAsync(
            bridge,
            "session-research",
            104,
            1604,
            "sha256:selection",
            1,
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("session-research", bridge.LastSessionId);
        Assert.Equal(1604, bridge.LastSelectResearchRequest?.TechId);
        Assert.Equal("sha256:selection", bridge.LastSelectResearchRequest?.ExpectedSelectionStateHash);
    }

    [Fact]
    public async Task ConfigureBuildingTool_MapsSorterFilterMode()
    {
        var bridge = new FakeBridgeClient(SuccessResult());

        var result = await SpherewrightTools.PrepareConfigureBuildingAsync(
            bridgeClient: bridge,
            sessionId: "session-filter",
            planetId: 103,
            entityId: 12,
            recipeId: 0,
            expectedFactoryStateHash: "sha256:factory",
            mode: BuildingConfigurationModes.SorterFilter,
            filterItemId: 1120,
            stateHashVersion: 1,
            cancellationToken: CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("session-filter", bridge.LastSessionId);
        Assert.Equal(BuildingConfigurationModes.SorterFilter, bridge.LastConfigureRequest?.Mode);
        Assert.Equal(1120, bridge.LastConfigureRequest?.FilterItemId);
        Assert.Equal("sha256:factory", bridge.LastConfigureRequest?.ExpectedFactoryStateHash);
    }

    [Fact]
    public async Task ConfigureBuildingTool_MapsLogisticsStationStorageMode()
    {
        var bridge = new FakeBridgeClient(SuccessResult());

        var result = await SpherewrightTools.PrepareConfigureBuildingAsync(
            bridgeClient: bridge,
            sessionId: "session-station",
            planetId: 104,
            entityId: 920,
            recipeId: 0,
            expectedFactoryStateHash: "sha256:factory",
            mode: BuildingConfigurationModes.LogisticsStationStorage,
            stationStorageIndex: 2,
            stationItemId: 1106,
            stationMaximumCount: 5_000,
            stationLocalLogic: LogisticsStorageLogics.Demand,
            stationRemoteLogic: LogisticsStorageLogics.Supply,
            expectedStationConfigurationStateHash: "sha256:station-config",
            stateHashVersion: 1,
            cancellationToken: CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("session-station", bridge.LastSessionId);
        Assert.Equal(BuildingConfigurationModes.LogisticsStationStorage, bridge.LastConfigureRequest?.Mode);
        Assert.Equal(2, bridge.LastConfigureRequest?.StationStorageIndex);
        Assert.Equal(1106, bridge.LastConfigureRequest?.StationItemId);
        Assert.Equal(5_000, bridge.LastConfigureRequest?.StationMaximumCount);
        Assert.Equal(LogisticsStorageLogics.Demand, bridge.LastConfigureRequest?.StationLocalLogic);
        Assert.Equal(LogisticsStorageLogics.Supply, bridge.LastConfigureRequest?.StationRemoteLogic);
        Assert.Equal("sha256:station-config", bridge.LastConfigureRequest?.ExpectedStationConfigurationStateHash);
    }

    [Fact]
    public async Task ConfigureBuildingTool_MapsLogisticsStationChargeMode()
    {
        var bridge = new FakeBridgeClient(SuccessResult());

        var result = await SpherewrightTools.PrepareConfigureBuildingAsync(
            bridgeClient: bridge,
            sessionId: "session-station-charge",
            planetId: 104,
            entityId: 920,
            recipeId: 0,
            expectedFactoryStateHash: "sha256:factory",
            mode: BuildingConfigurationModes.LogisticsStationCharge,
            stationMaximumChargePowerWatts: 12_000_000,
            expectedStationConfigurationStateHash: "sha256:station-config",
            stateHashVersion: 1,
            cancellationToken: CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("session-station-charge", bridge.LastSessionId);
        Assert.Equal(BuildingConfigurationModes.LogisticsStationCharge, bridge.LastConfigureRequest?.Mode);
        Assert.Equal(12_000_000, bridge.LastConfigureRequest?.StationMaximumChargePowerWatts);
        Assert.Equal("sha256:station-config", bridge.LastConfigureRequest?.ExpectedStationConfigurationStateHash);
    }

    [Fact]
    public async Task ConfigureBuildingTool_MapsLogisticsStationBeltMode()
    {
        var bridge = new FakeBridgeClient(SuccessResult());

        var result = await SpherewrightTools.PrepareConfigureBuildingAsync(
            bridgeClient: bridge,
            sessionId: "session-station-belt",
            planetId: 104,
            entityId: 920,
            recipeId: 0,
            expectedFactoryStateHash: "sha256:factory",
            mode: BuildingConfigurationModes.LogisticsStationBelt,
            stationBeltSlotIndex: 3,
            stationBeltStorageIndex: 0,
            expectedStationConfigurationStateHash: "sha256:station-config",
            stateHashVersion: 1,
            cancellationToken: CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("session-station-belt", bridge.LastSessionId);
        Assert.Equal(BuildingConfigurationModes.LogisticsStationBelt, bridge.LastConfigureRequest?.Mode);
        Assert.Equal(3, bridge.LastConfigureRequest?.StationBeltSlotIndex);
        Assert.Equal(0, bridge.LastConfigureRequest?.StationBeltStorageIndex);
        Assert.Equal("sha256:station-config", bridge.LastConfigureRequest?.ExpectedStationConfigurationStateHash);
    }

    [Fact]
    public async Task DismantleTool_MapsStableEndpointAndPlayerHashes()
    {
        var bridge = new FakeBridgeClient(SuccessResult());

        var result = await SpherewrightTools.PrepareDismantleAsync(
            bridge,
            "session-dismantle",
            102,
            17,
            "sha256:endpoint",
            "sha256:player",
            1,
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("session-dismantle", bridge.LastSessionId);
        Assert.Equal(102, bridge.LastDismantleRequest?.PlanetId);
        Assert.Equal(17, bridge.LastDismantleRequest?.ObjectId);
        Assert.Equal("sha256:endpoint", bridge.LastDismantleRequest?.ExpectedEndpointStateHash);
        Assert.Equal("sha256:player", bridge.LastDismantleRequest?.ExpectedPlayerStateHash);
    }

    [Fact]
    public async Task PrepareStationFleetTransfer_MapsExactFleetHashAndDirection()
    {
        var bridge = new FakeBridgeClient(SuccessResult());

        var result = await SpherewrightTools.PrepareLogisticsStationFleetTransferAsync(
            bridge,
            "session-fleet",
            104,
            920,
            LogisticsStationFleetTransferDirections.PlayerToStation,
            LogisticsFleetItemIds.Drone,
            10,
            "sha256:player",
            "sha256:fleet",
            1,
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("session-fleet", bridge.LastSessionId);
        Assert.Equal(920, bridge.LastFleetTransferRequest?.StationEntityId);
        Assert.Equal(LogisticsStationFleetTransferDirections.PlayerToStation, bridge.LastFleetTransferRequest?.Direction);
        Assert.Equal(LogisticsFleetItemIds.Drone, bridge.LastFleetTransferRequest?.ItemId);
        Assert.Equal("sha256:fleet", bridge.LastFleetTransferRequest?.ExpectedStationFleetStateHash);
    }

    [Fact]
    public async Task PrepareQuarantineReconciliation_MapsExactActionAndRevision()
    {
        var bridge = new FakeBridgeClient(SuccessResult());

        var result = await SpherewrightTools.PrepareQuarantineReconciliationAsync(
            bridge,
            "session-quarantine",
            104,
            "action-quarantine",
            445,
            1,
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("session-quarantine", bridge.LastSessionId);
        Assert.Equal("action-quarantine", bridge.LastReconciliationRequest?.ActionId);
        Assert.Equal(445, bridge.LastReconciliationRequest?.ExpectedRevision);
    }

    [Fact]
    public async Task PrepareInterplanetaryFlight_MapsBoundDestinationAndProofs()
    {
        var bridge = new FakeBridgeClient(SuccessResult());

        var result = await SpherewrightTools.PrepareInterplanetaryFlightAsync(
            bridge,
            "session-flight",
            104,
            103,
            "sha256:player",
            "sha256:star",
            0.97d,
            1,
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("session-flight", bridge.LastSessionId);
        Assert.Equal(104, bridge.LastFlightRequest?.PlanetId);
        Assert.Equal(103, bridge.LastFlightRequest?.DestinationPlanetId);
        Assert.Equal("sha256:player", bridge.LastFlightRequest?.ExpectedPlayerStateHash);
        Assert.Equal("sha256:star", bridge.LastFlightRequest?.ExpectedStarSystemStateHash);
        Assert.Equal(0.97d, bridge.LastFlightRequest?.MinimumCoreEnergyRatio);
    }

    [Fact]
    public async Task PrepareFlightCheckpointReload_MapsOnlyReusableToken()
    {
        var bridge = new FakeBridgeClient(SuccessResult());

        var result = await SpherewrightTools.PrepareFlightCheckpointReloadAsync(
            bridge,
            "checkpoint-token",
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("checkpoint-token", bridge.LastFlightCheckpointReloadRequest?.ReloadToken);
    }

    [Fact]
    public async Task SaveImportTools_MapExplicitConversationConfirmation()
    {
        var bridge = new FakeBridgeClient(SuccessResult());

        var prepared = await SpherewrightTools.PrepareUserSaveImportAsync(
            bridge,
            "session-import",
            7,
            CancellationToken.None);
        var committed = await SpherewrightTools.CommitUserSaveImportAsync(
            bridge,
            "session-import",
            "import-plan",
            "f1078b10-c48b-430f-b0e0-4de18438762c",
            true,
            true,
            true,
            CancellationToken.None);

        Assert.False(prepared.IsError);
        Assert.True(prepared.StructuredContent!.Value
            .GetProperty("result")
            .GetProperty("userConfirmationRequired")
            .GetBoolean());
        Assert.False(committed.IsError);
        Assert.Equal("session-import", bridge.LastSessionId);
        Assert.Equal(7, bridge.LastImportPrepareRequest?.ExpectedRevision);
        Assert.Equal("import-plan", bridge.LastImportCommitRequest?.PlanToken);
        Assert.True(bridge.LastImportCommitRequest?.UserConfirmedInConversation);
        Assert.True(bridge.LastImportCommitRequest?.AcknowledgeOriginalSaveRemainsUnchanged);
        Assert.True(bridge.LastImportCommitRequest?.AcknowledgeJournalStartsAtImport);
    }

    private static BridgeCallResult<BridgeStatus> SuccessResult()
    {
        return BridgeCallResult<BridgeStatus>.Succeeded(new BridgeStatus
        {
            BridgeConnected = true,
            BridgeInstanceId = "instance",
            PluginVersion = Spherewright.Contracts.Versioning.SpherewrightProduct.CurrentVersion,
            ProtocolVersion = ProtocolConstants.CurrentVersion,
            GameVersion = "0.10.34.28529",
            GameLoaded = false,
            WritesConfigured = false,
            WriteHealth = WriteHealthStates.Healthy,
        });
    }

    private sealed class FakeBridgeClient : IBridgeClient
    {
        private readonly BridgeCallResult<BridgeStatus> _result;

        public FakeBridgeClient(BridgeCallResult<BridgeStatus> result)
        {
            _result = result;
        }

        public BridgeCallResult<ListAssemblersResult>? ListResult { get; set; }

        public ActionResultSnapshot? ActionResult { get; set; }

        public string? LastSessionId { get; private set; }

        public ListAssemblersRequest? LastListRequest { get; private set; }

        public ListResourceNodesRequest? LastResourceListRequest { get; private set; }

        public PrepareConfigureBuildingRequest? LastConfigureRequest { get; private set; }

        public PrepareDismantleRequest? LastDismantleRequest { get; private set; }

        public PrepareLogisticsStationFleetTransferRequest? LastFleetTransferRequest { get; private set; }

        public PrepareQuarantineReconciliationRequest? LastReconciliationRequest { get; private set; }

        public PrepareInterplanetaryFlightRequest? LastFlightRequest { get; private set; }

        public PrepareSelectResearchRequest? LastSelectResearchRequest { get; private set; }

        public PrepareFlightCheckpointReloadRequest? LastFlightCheckpointReloadRequest { get; private set; }

        public PrepareUserSaveImportRequest? LastImportPrepareRequest { get; private set; }

        public CommitUserSaveImportRequest? LastImportCommitRequest { get; private set; }
        public Task<BridgeCallResult<BridgeStatus>> GetBridgeStatusAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_result);
        }

        public Task<BridgeCallResult<SessionState>> GetSessionStateAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(BridgeCallResult<SessionState>.Succeeded(new SessionState
            {
                BridgeConnected = true,
                GameVersion = "0.10.34.28529",
            }));
        }

        public Task<BridgeCallResult<PlayerStateSnapshot>> GetPlayerStateAsync(
            string sessionId,
            LocalPlanetRequest request,
            CancellationToken cancellationToken)
        {
            LastSessionId = sessionId;
            return Task.FromResult(BridgeCallResult<PlayerStateSnapshot>.Succeeded(new PlayerStateSnapshot
            {
                SessionId = sessionId,
                PlanetId = request.PlanetId,
            }));
        }

        public Task<BridgeCallResult<ProgressionStateSnapshot>> GetProgressionStateAsync(
            string sessionId,
            LocalPlanetRequest request,
            CancellationToken cancellationToken)
        {
            LastSessionId = sessionId;
            return Task.FromResult(BridgeCallResult<ProgressionStateSnapshot>.Succeeded(new ProgressionStateSnapshot
            {
                SessionId = sessionId,
                PlanetId = request.PlanetId,
            }));
        }

        public Task<BridgeCallResult<GameplayJournalSnapshot>> GetGameplayJournalAsync(
            string sessionId,
            CancellationToken cancellationToken)
        {
            LastSessionId = sessionId;
            return Task.FromResult(BridgeCallResult<GameplayJournalSnapshot>.Succeeded(new GameplayJournalSnapshot
            {
                SessionId = sessionId,
                JournalId = "journal",
            }));
        }

        public Task<BridgeCallResult<LocalStarSystemSnapshot>> GetLocalStarSystemAsync(
            string sessionId,
            LocalPlanetRequest request,
            CancellationToken cancellationToken)
        {
            LastSessionId = sessionId;
            return Task.FromResult(BridgeCallResult<LocalStarSystemSnapshot>.Succeeded(new LocalStarSystemSnapshot
            {
                SessionId = sessionId,
                LocalPlanetId = request.PlanetId,
            }));
        }

        public Task<BridgeCallResult<RecipeCatalogSnapshot>> GetRecipeCatalogAsync(
            string sessionId,
            LocalPlanetRequest request,
            CancellationToken cancellationToken)
        {
            LastSessionId = sessionId;
            return Task.FromResult(BridgeCallResult<RecipeCatalogSnapshot>.Succeeded(new RecipeCatalogSnapshot
            {
                SessionId = sessionId,
                PlanetId = request.PlanetId,
            }));
        }

        public Task<BridgeCallResult<ListResourceNodesResult>> ListResourceNodesAsync(
            string sessionId,
            ListResourceNodesRequest request,
            CancellationToken cancellationToken)
        {
            LastSessionId = sessionId;
            LastResourceListRequest = request;
            return Task.FromResult(BridgeCallResult<ListResourceNodesResult>.Succeeded(new ListResourceNodesResult
            {
                SessionId = sessionId,
                PlanetId = request.PlanetId,
            }));
        }

        public Task<BridgeCallResult<ResourceNodeSnapshot>> InspectResourceNodeAsync(
            string sessionId,
            InspectResourceNodeRequest request,
            CancellationToken cancellationToken)
        {
            LastSessionId = sessionId;
            return Task.FromResult(BridgeCallResult<ResourceNodeSnapshot>.Succeeded(new ResourceNodeSnapshot
            {
                SessionId = sessionId,
                PlanetId = request.PlanetId,
                Kind = request.Kind,
                NodeId = request.NodeId,
            }));
        }

        public Task<BridgeCallResult<ListFactoryEntitiesResult>> ListFactoryEntitiesAsync(
            string sessionId,
            ListFactoryEntitiesRequest request,
            CancellationToken cancellationToken)
        {
            LastSessionId = sessionId;
            return Task.FromResult(BridgeCallResult<ListFactoryEntitiesResult>.Succeeded(new ListFactoryEntitiesResult
            {
                SessionId = sessionId,
                PlanetId = request.PlanetId,
            }));
        }

        public Task<BridgeCallResult<FactoryEntitySnapshot>> InspectFactoryEntityAsync(
            string sessionId,
            InspectFactoryEntityRequest request,
            CancellationToken cancellationToken)
        {
            LastSessionId = sessionId;
            return Task.FromResult(BridgeCallResult<FactoryEntitySnapshot>.Succeeded(new FactoryEntitySnapshot
            {
                SessionId = sessionId,
                PlanetId = request.PlanetId,
                ObjectId = request.ObjectId,
            }));
        }

        public Task<BridgeCallResult<PowerSummarySnapshot>> GetPowerSummaryAsync(
            string sessionId,
            LocalPlanetRequest request,
            CancellationToken cancellationToken)
        {
            LastSessionId = sessionId;
            return Task.FromResult(BridgeCallResult<PowerSummarySnapshot>.Succeeded(new PowerSummarySnapshot
            {
                SessionId = sessionId,
                PlanetId = request.PlanetId,
            }));
        }

        public Task<BridgeCallResult<ActionResultSnapshot>> GetActionResultAsync(
            GetActionResultRequest request,
            CancellationToken cancellationToken)
        {
            var result = ActionResult ?? new ActionResultSnapshot
            {
                ActionId = request.ActionId,
                ActionKind = "new-game",
                State = "completed",
                Terminal = true,
                Succeeded = true,
            };
            return Task.FromResult(BridgeCallResult<ActionResultSnapshot>.Succeeded(result));
        }

        public Task<BridgeCallResult<PreparedNormalAction>> PrepareMoveAsync(
            string sessionId,
            PrepareMoveRequest request,
            CancellationToken cancellationToken) => Prepared(sessionId, NormalActionKinds.Move);

        public Task<BridgeCallResult<NormalActionCommitResult>> CommitMoveAsync(
            string sessionId,
            CommitNormalActionRequest request,
            CancellationToken cancellationToken) => Committed(sessionId, request, NormalActionKinds.Move);

        public Task<BridgeCallResult<PreparedNormalAction>> PrepareInterplanetaryFlightAsync(
            string sessionId,
            PrepareInterplanetaryFlightRequest request,
            CancellationToken cancellationToken)
        {
            LastSessionId = sessionId;
            LastFlightRequest = request;
            return Prepared(sessionId, NormalActionKinds.InterplanetaryFlight);
        }

        public Task<BridgeCallResult<NormalActionCommitResult>> CommitInterplanetaryFlightAsync(
            string sessionId,
            CommitNormalActionRequest request,
            CancellationToken cancellationToken) => Committed(sessionId, request, NormalActionKinds.InterplanetaryFlight);

        public Task<BridgeCallResult<PreparedNormalAction>> PrepareHarvestAsync(
            string sessionId,
            PrepareHarvestRequest request,
            CancellationToken cancellationToken) => Prepared(sessionId, NormalActionKinds.Harvest);

        public Task<BridgeCallResult<NormalActionCommitResult>> CommitHarvestAsync(
            string sessionId,
            CommitNormalActionRequest request,
            CancellationToken cancellationToken) => Committed(sessionId, request, NormalActionKinds.Harvest);

        public Task<BridgeCallResult<PreparedNormalAction>> PrepareHandcraftAsync(
            string sessionId,
            PrepareHandcraftRequest request,
            CancellationToken cancellationToken) => Prepared(sessionId, NormalActionKinds.Handcraft);

        public Task<BridgeCallResult<NormalActionCommitResult>> CommitHandcraftAsync(
            string sessionId,
            CommitNormalActionRequest request,
            CancellationToken cancellationToken) => Committed(sessionId, request, NormalActionKinds.Handcraft);

        public Task<BridgeCallResult<PreparedNormalAction>> PrepareSelectResearchAsync(
            string sessionId,
            PrepareSelectResearchRequest request,
            CancellationToken cancellationToken)
        {
            LastSelectResearchRequest = request;
            return Prepared(sessionId, NormalActionKinds.SelectResearch);
        }

        public Task<BridgeCallResult<NormalActionCommitResult>> CommitSelectResearchAsync(
            string sessionId,
            CommitNormalActionRequest request,
            CancellationToken cancellationToken) => Committed(sessionId, request, NormalActionKinds.SelectResearch);

        public Task<BridgeCallResult<PreparedNormalAction>> PrepareBuildAsync(
            string sessionId,
            PrepareBuildRequest request,
            CancellationToken cancellationToken) => Prepared(sessionId, NormalActionKinds.Build);

        public Task<BridgeCallResult<NormalActionCommitResult>> CommitBuildAsync(
            string sessionId,
            CommitNormalActionRequest request,
            CancellationToken cancellationToken) => Committed(sessionId, request, NormalActionKinds.Build);

        public Task<BridgeCallResult<PreparedNormalAction>> PrepareDismantleAsync(
            string sessionId,
            PrepareDismantleRequest request,
            CancellationToken cancellationToken)
        {
            LastSessionId = sessionId;
            LastDismantleRequest = request;
            return Prepared(sessionId, NormalActionKinds.Dismantle);
        }

        public Task<BridgeCallResult<NormalActionCommitResult>> CommitDismantleAsync(
            string sessionId,
            CommitNormalActionRequest request,
            CancellationToken cancellationToken) => Committed(sessionId, request, NormalActionKinds.Dismantle);

        public Task<BridgeCallResult<PreparedNormalAction>> PrepareConfigureBuildingAsync(
            string sessionId,
            PrepareConfigureBuildingRequest request,
            CancellationToken cancellationToken)
        {
            LastConfigureRequest = request;
            return Prepared(sessionId, NormalActionKinds.ConfigureBuilding);
        }

        public Task<BridgeCallResult<NormalActionCommitResult>> CommitConfigureBuildingAsync(
            string sessionId,
            CommitNormalActionRequest request,
            CancellationToken cancellationToken) => Committed(sessionId, request, NormalActionKinds.ConfigureBuilding);

        public Task<BridgeCallResult<PreparedNormalAction>> PrepareTransferAsync(
            string sessionId,
            PrepareTransferRequest request,
            CancellationToken cancellationToken) => Prepared(sessionId, NormalActionKinds.Transfer);

        public Task<BridgeCallResult<NormalActionCommitResult>> CommitTransferAsync(
            string sessionId,
            CommitNormalActionRequest request,
            CancellationToken cancellationToken) => Committed(sessionId, request, NormalActionKinds.Transfer);

        public Task<BridgeCallResult<PreparedNormalAction>> PrepareLogisticsStationFleetTransferAsync(
            string sessionId,
            PrepareLogisticsStationFleetTransferRequest request,
            CancellationToken cancellationToken)
        {
            LastFleetTransferRequest = request;
            return Prepared(sessionId, NormalActionKinds.LogisticsStationFleetTransfer);
        }

        public Task<BridgeCallResult<NormalActionCommitResult>> CommitLogisticsStationFleetTransferAsync(
            string sessionId,
            CommitNormalActionRequest request,
            CancellationToken cancellationToken) =>
            Committed(sessionId, request, NormalActionKinds.LogisticsStationFleetTransfer);

        public Task<BridgeCallResult<PreparedNormalAction>> PrepareRefuelAsync(
            string sessionId,
            PrepareRefuelRequest request,
            CancellationToken cancellationToken) => Prepared(sessionId, NormalActionKinds.Refuel);

        public Task<BridgeCallResult<NormalActionCommitResult>> CommitRefuelAsync(
            string sessionId,
            CommitNormalActionRequest request,
            CancellationToken cancellationToken) => Committed(sessionId, request, NormalActionKinds.Refuel);

        public Task<BridgeCallResult<PreparedNormalAction>> PrepareSaveAsync(
            string sessionId,
            PrepareSaveRequest request,
            CancellationToken cancellationToken) => Prepared(sessionId, NormalActionKinds.Save);

        public Task<BridgeCallResult<NormalActionCommitResult>> CommitSaveAsync(
            string sessionId,
            CommitNormalActionRequest request,
            CancellationToken cancellationToken) => Committed(sessionId, request, NormalActionKinds.Save);

        public Task<BridgeCallResult<PreparedNormalAction>> PrepareQuarantineReconciliationAsync(
            string sessionId,
            PrepareQuarantineReconciliationRequest request,
            CancellationToken cancellationToken)
        {
            LastReconciliationRequest = request;
            return Prepared(sessionId, NormalActionKinds.ReconcileQuarantine);
        }

        public Task<BridgeCallResult<NormalActionCommitResult>> CommitQuarantineReconciliationAsync(
            string sessionId,
            CommitNormalActionRequest request,
            CancellationToken cancellationToken) => Committed(sessionId, request, NormalActionKinds.ReconcileQuarantine);

        private Task<BridgeCallResult<PreparedNormalAction>> Prepared(string sessionId, string actionKind)
        {
            LastSessionId = sessionId;
            return Task.FromResult(BridgeCallResult<PreparedNormalAction>.Succeeded(new PreparedNormalAction
            {
                Prepared = true,
                ActionKind = actionKind,
                PlanToken = "plan",
            }));
        }

        private Task<BridgeCallResult<NormalActionCommitResult>> Committed(
            string sessionId,
            CommitNormalActionRequest request,
            string actionKind)
        {
            LastSessionId = sessionId;
            return Task.FromResult(BridgeCallResult<NormalActionCommitResult>.Succeeded(new NormalActionCommitResult
            {
                ActionId = "action",
                ActionKind = actionKind,
                IdempotencyKey = request.IdempotencyKey,
                Accepted = true,
            }));
        }

        public Task<BridgeCallResult<ListAssemblersResult>> ListAssemblersAsync(
            string sessionId,
            ListAssemblersRequest request,
            CancellationToken cancellationToken)
        {
            LastSessionId = sessionId;
            LastListRequest = request;
            return Task.FromResult(ListResult ?? BridgeCallResult<ListAssemblersResult>.Succeeded(new ListAssemblersResult()));
        }

        public Task<BridgeCallResult<AssemblerSnapshot>> InspectAssemblerAsync(
            string sessionId,
            InspectAssemblerRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(BridgeCallResult<AssemblerSnapshot>.Succeeded(new AssemblerSnapshot
            {
                EntityId = request.EntityId,
            }));
        }

        public Task<BridgeCallResult<BuildCatalog>> GetBuildCatalogAsync(
            string sessionId,
            CancellationToken cancellationToken)
        {
            LastSessionId = sessionId;
            return Task.FromResult(BridgeCallResult<BuildCatalog>.Succeeded(new BuildCatalog
            {
                PlanetId = 1001,
            }));
        }

        public Task<BridgeCallResult<PreparedTestWorldPlan>> PrepareTestWorldAsync(
            PrepareTestWorldRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(BridgeCallResult<PreparedTestWorldPlan>.Succeeded(new PreparedTestWorldPlan
            {
                PlanToken = "plan",
                GalaxySeed = request.GalaxySeed,
                StarCount = request.StarCount,
            }));
        }

        public Task<BridgeCallResult<TestWorldCreationResult>> CommitTestWorldAsync(
            CommitTestWorldRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(BridgeCallResult<TestWorldCreationResult>.Succeeded(new TestWorldCreationResult
            {
                ActionId = "action",
                Accepted = true,
            }));
        }

        public Task<BridgeCallResult<PreparedUserSaveImportPlan>> PrepareUserSaveImportAsync(
            string sessionId,
            PrepareUserSaveImportRequest request,
            CancellationToken cancellationToken)
        {
            LastSessionId = sessionId;
            LastImportPrepareRequest = request;
            return Task.FromResult(BridgeCallResult<PreparedUserSaveImportPlan>.Succeeded(
                new PreparedUserSaveImportPlan
                {
                    Prepared = true,
                    PlanToken = "import-plan",
                    OriginalSavePreserved = true,
                    HistoricalCoverageComplete = false,
                    UserConfirmationRequired = true,
                    ConfirmationPrompt = "Confirm this import in the conversation.",
                }));
        }

        public Task<BridgeCallResult<UserSaveImportResult>> CommitUserSaveImportAsync(
            string sessionId,
            CommitUserSaveImportRequest request,
            CancellationToken cancellationToken)
        {
            LastSessionId = sessionId;
            LastImportCommitRequest = request;
            return Task.FromResult(BridgeCallResult<UserSaveImportResult>.Succeeded(
                new UserSaveImportResult
                {
                    ActionId = "import-action",
                    Accepted = true,
                    State = NormalActionStates.Completed,
                    SessionId = sessionId,
                    OriginalSavePreserved = true,
                }));
        }
        public Task<BridgeCallResult<PreparedOwnedWorldResumePlan>> PrepareOwnedWorldResumeAsync(
            PrepareOwnedWorldResumeRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(BridgeCallResult<PreparedOwnedWorldResumePlan>.Succeeded(new PreparedOwnedWorldResumePlan
            {
                Prepared = true,
                PlanToken = "resume-plan",
            }));
        }

        public Task<BridgeCallResult<OwnedWorldResumeResult>> CommitOwnedWorldResumeAsync(
            CommitOwnedWorldResumeRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(BridgeCallResult<OwnedWorldResumeResult>.Succeeded(new OwnedWorldResumeResult
            {
                ActionId = "resume-action",
                Accepted = true,
                State = NormalActionStates.WaitingForGame,
            }));
        }

        public Task<BridgeCallResult<PreparedFlightCheckpointReloadPlan>> PrepareFlightCheckpointReloadAsync(
            PrepareFlightCheckpointReloadRequest request,
            CancellationToken cancellationToken)
        {
            LastFlightCheckpointReloadRequest = request;
            return Task.FromResult(BridgeCallResult<PreparedFlightCheckpointReloadPlan>.Succeeded(
                new PreparedFlightCheckpointReloadPlan
                {
                    Prepared = true,
                    PlanToken = "checkpoint-plan",
                    CheckpointId = "checkpoint-id",
                }));
        }

        public Task<BridgeCallResult<FlightCheckpointReloadResult>> CommitFlightCheckpointReloadAsync(
            CommitFlightCheckpointReloadRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(BridgeCallResult<FlightCheckpointReloadResult>.Succeeded(
                new FlightCheckpointReloadResult
                {
                    ActionId = "checkpoint-action",
                    CheckpointId = "checkpoint-id",
                    Accepted = true,
                    State = NormalActionStates.WaitingForGame,
                }));
        }
    }
}
