using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Spherewright.Contracts.Actions;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Factory;
using Spherewright.Contracts.Players;
using Spherewright.Contracts.Power;
using Spherewright.Contracts.Progression;
using Spherewright.Contracts.Protocol;
using Spherewright.Contracts.Resources;
using Spherewright.Contracts.Sessions;
using Spherewright.Contracts.Testing;
using Spherewright.Mcp.BridgeClient;
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
                "spherewright_commit_handcraft",
                "spherewright_commit_harvest",
                "spherewright_commit_move",
                "spherewright_commit_new_game",
                "spherewright_commit_quarantine_reconciliation",
                "spherewright_commit_refuel",
                "spherewright_commit_resume_owned_game",
                "spherewright_commit_save",
                "spherewright_commit_select_research",
                "spherewright_commit_transfer",
                "spherewright_get_action_result",
                "spherewright_get_build_catalog",
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
                "spherewright_prepare_handcraft",
                "spherewright_prepare_harvest",
                "spherewright_prepare_move",
                "spherewright_prepare_new_game",
                "spherewright_prepare_quarantine_reconciliation",
                "spherewright_prepare_refuel",
                "spherewright_prepare_resume_owned_game",
                "spherewright_prepare_save",
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
        Assert.False(structured.TryGetProperty("authToken", out _));
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
    public async Task ConfigureBuildingTool_MapsSorterFilterMode()
    {
        var bridge = new FakeBridgeClient(SuccessResult());

        var result = await SpherewrightTools.PrepareConfigureBuildingAsync(
            bridge,
            "session-filter",
            103,
            12,
            0,
            "sha256:factory",
            BuildingConfigurationModes.SorterFilter,
            0,
            1120,
            1,
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("session-filter", bridge.LastSessionId);
        Assert.Equal(BuildingConfigurationModes.SorterFilter, bridge.LastConfigureRequest?.Mode);
        Assert.Equal(1120, bridge.LastConfigureRequest?.FilterItemId);
        Assert.Equal("sha256:factory", bridge.LastConfigureRequest?.ExpectedFactoryStateHash);
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

    private static BridgeCallResult<BridgeStatus> SuccessResult()
    {
        return BridgeCallResult<BridgeStatus>.Succeeded(new BridgeStatus
        {
            BridgeConnected = true,
            BridgeInstanceId = "instance",
            PluginVersion = "0.1.0",
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

        public string? LastSessionId { get; private set; }

        public ListAssemblersRequest? LastListRequest { get; private set; }

        public ListResourceNodesRequest? LastResourceListRequest { get; private set; }

        public PrepareConfigureBuildingRequest? LastConfigureRequest { get; private set; }

        public PrepareQuarantineReconciliationRequest? LastReconciliationRequest { get; private set; }

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
            return Task.FromResult(BridgeCallResult<ActionResultSnapshot>.Succeeded(new ActionResultSnapshot
            {
                ActionId = request.ActionId,
                ActionKind = "new-game",
                State = "completed",
                Terminal = true,
                Succeeded = true,
            }));
        }

        public Task<BridgeCallResult<PreparedNormalAction>> PrepareMoveAsync(
            string sessionId,
            PrepareMoveRequest request,
            CancellationToken cancellationToken) => Prepared(sessionId, NormalActionKinds.Move);

        public Task<BridgeCallResult<NormalActionCommitResult>> CommitMoveAsync(
            string sessionId,
            CommitNormalActionRequest request,
            CancellationToken cancellationToken) => Committed(sessionId, request, NormalActionKinds.Move);

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
            CancellationToken cancellationToken) => Prepared(sessionId, NormalActionKinds.SelectResearch);

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
    }
}
