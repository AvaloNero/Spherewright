using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Actions;
using Spherewright.Contracts.Celestial;
using Spherewright.Contracts.Diagnostics;
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
    public void AttachedJournalGuidanceDoesNotInferNeverMadeFromMissingFirst()
    {
        var method = typeof(SpherewrightTools).GetMethod(nameof(SpherewrightTools.GetGameplayJournalAsync))!;
        var description = ((System.ComponentModel.DescriptionAttribute)Attribute.GetCustomAttribute(method,
            typeof(System.ComponentModel.DescriptionAttribute))!).Description;
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        foreach (var text in new[] { description, guide })
        {
            Assert.Contains("attached_existing_save", text);
            Assert.Contains("historicalCoverageComplete=false", text);
            Assert.Contains("not proof that the item was never made", text);
            Assert.Contains("durableThroughSequence", text);
            Assert.Contains("Never replay a completed craft", text);
        }
        Assert.Contains("complete old entry prefix", guide);
        Assert.Contains("no pending persistence and no persistence error", guide);
        Assert.Contains("not a mandatory +1", guide);
        Assert.Contains("backfill pre-tracking history", guide);
    }

    [Fact]
    public void TechCompletionRewardsAreDiscoverableAsMetadataNotDeliveryOrProduction()
    {
        var method = typeof(SpherewrightTools).GetMethod(nameof(SpherewrightTools.GetProgressionStateAsync))!;
        var description = ((System.ComponentModel.DescriptionAttribute)Attribute.GetCustomAttribute(method,
            typeof(System.ComponentModel.DescriptionAttribute))!).Description;
        var resource = AgentPlaybookResources.GetOpeningMovementPlaybook();
        Assert.Equal(AgentPlaybookResources.OpeningMovementUri, resource.Uri);
        foreach (var text in new[] { description, resource.Text })
        {
            Assert.Contains("completionItemRewards", text);
            Assert.Contains("not a delivery receipt", text);
            Assert.Contains("null/missing means unknown", text);
            Assert.Contains("empty list means observed no rewards", text);
            Assert.Contains("unlockTick", text);
            Assert.Contains("Never replay a completed action", text);
        }
        Assert.Contains("not handcraft or production", resource.Text);
        Assert.Contains("capacity overflow", resource.Text);
        Assert.Contains("multiple possible sources", resource.Text);
        Assert.Contains("at most32 entries", resource.Text);
        Assert.Contains("must not create or backfill a first-production Journal event", resource.Text);
    }

    [Fact]
    public void StalledMoveGuidanceIncludesSmallAndCoincidentFactoryObjects()
    {
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        var method = typeof(SpherewrightTools).GetMethod(nameof(SpherewrightTools.CommitMoveAsync))!;
        var description = ((System.ComponentModel.DescriptionAttribute)Attribute.GetCustomAttribute(method,
            typeof(System.ComponentModel.DescriptionAttribute))!).Description;
        Assert.Contains("including belts and sorters/inserters", guide);
        Assert.Contains("including belts and sorters/inserters", description);
        Assert.Contains("Retain every distinct object ID even when positions coincide", guide);
        Assert.Contains("current catalog/inspection for item identity", guide);
        Assert.Contains("center distance alone is not walking clearance", description);
        Assert.Contains("not horizontal walking footprints", guide);
    }

    [Fact]
    public void StalledMoveGuidanceKeepsObservationAndRecoveryBounds()
    {
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("incomplete or expired pagination cannot rule out obstacles", guide);
        Assert.Contains("stay outside the observed obstacle cluster", guide);
        Assert.Contains("crossing the same obstruction is not a new route", guide);
        Assert.Contains("Do not dismantle a working line", guide);
        Assert.Contains("do not submit the same target again", guide);
        Assert.Contains("four targets", guide);
        Assert.Contains("each direction **once**", guide);
        Assert.Contains("All recovery attempts use the existing", guide);
        Assert.Contains("movementState=Walk", guide);
    }

    [Fact]
    public void OpeningPlaybookDistinguishesUnfilteredSorterReadbackFromRequestZero()
    {
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("componentKind=inserter", guide);
        Assert.Contains("filterItemId=null", guide);
        Assert.Contains("filterItemId=0", guide);
        Assert.Contains("request and response representations differ", guide);
        Assert.Contains("missing property, failed inspection or non-inserter is still unknown", guide);
        Assert.Contains("preserve any accepted action and never replay it", guide);
        Assert.Contains("does not require repeating build-path or placement previews", guide);
        Assert.Contains("`targetObjectId` is optional", guide);
        Assert.Contains("not a request-hash echo", guide);
        Assert.Contains("Keep explicitly documented mode-specific checks", guide);
    }

    [Fact]
    public void BeltShortageRecoveryIsDiscoverableWithoutClaimingPlacementApproval()
    {
        var method = typeof(SpherewrightTools).GetMethod(nameof(SpherewrightTools.PrepareBuildAsync))!;
        var description = ((System.ComponentModel.DescriptionAttribute)Attribute.GetCustomAttribute(method,
            typeof(System.ComponentModel.DescriptionAttribute))!).Description;
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        foreach (var text in new[] { description, guide })
        {
            Assert.Contains("INVENTORY_INSUFFICIENT", text);
            Assert.Contains("do not retry with unchanged inventory", text);
            Assert.Contains("normal handcraft or transfer", text);
            Assert.Contains("revalidate the complete path", text);
            Assert.Contains("same explicit endpoint bindings", text);
            Assert.Contains("does not prove placement", text);
        }
        Assert.Contains("Older Plugin versions", guide);
        Assert.Contains("Mixed, unknown, occupancy and cover failures", guide);
    }

    [Fact]
    public async Task BeltShortageErrorReachesMcpWithoutAPlanOrConstructionToken()
    {
        var error = Spherewright.Bridge.Core.Factory.BeltBuildRejectionPolicy.DescribeInventoryShortage(
            new[] { "Ok", "NotEnoughItem" }, true, false)!;
        var bridge = new FakeBridgeClient(SuccessResult()) { BuildPrepareError = error };
        var result = await SpherewrightTools.PrepareBuildAsync(bridge, "session", 104, 2001, "player");
        Assert.True(result.IsError);
        var content = result.StructuredContent!.Value;
        Assert.Equal(JsonValueKind.Null, content.GetProperty("result").ValueKind);
        Assert.Equal(error.Code, content.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(error.Recovery, content.GetProperty("error").GetProperty("recovery").GetString());
        Assert.DoesNotContain("planToken", content.GetRawText());
    }

    [Fact]
    public async Task MainMenuResumeIsPreparedBeforeAnyOwnedWorldIsLoaded()
    {
        var bridge = new FakeBridgeClient(SuccessResult())
        {
            SessionSnapshot = new SessionState
            {
                BridgeConnected = true, GameLoaded = false, RestartResumeAvailable = true, RestartResumeToken = "ticket",
            },
        };
        var state = await SpherewrightTools.GetSessionStateAsync(bridge, CancellationToken.None);
        Assert.False(state.StructuredContent!.Value.GetProperty("result").GetProperty("gameLoaded").GetBoolean());
        Assert.True(state.StructuredContent!.Value.GetProperty("result").GetProperty("restartResumeAvailable").GetBoolean());
        var prepare = await SpherewrightTools.PrepareOwnedWorldResumeAsync(bridge, "ticket", CancellationToken.None);
        Assert.False(prepare.IsError);
        Assert.Equal("ticket", bridge.LastResumePrepareRequest!.ResumeToken);
    }

    [Fact]
    public void MainMenuGuideSeparatesPreResumeReadinessFromPostAdoptionProof()
    {
        foreach (var name in new[] { nameof(SpherewrightTools.GetSessionStateAsync), nameof(SpherewrightTools.PrepareOwnedWorldResumeAsync) })
        {
            var method = typeof(SpherewrightTools).GetMethod(name)!;
            var description = ((System.ComponentModel.DescriptionAttribute)Attribute.GetCustomAttribute(method,
                typeof(System.ComponentModel.DescriptionAttribute))!).Description;
            Assert.Contains("gameLoaded=false", description);
            Assert.Contains("restartResumeAvailable", description);
        }
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("do not wait for `gameLoaded=true` before prepare", guide);
        Assert.Contains("native preload/menu/no-loader", guide);
        var commit = typeof(SpherewrightTools).GetMethod(nameof(SpherewrightTools.CommitOwnedWorldResumeAsync))!;
        var commitDescription = ((System.ComponentModel.DescriptionAttribute)Attribute.GetCustomAttribute(commit,
            typeof(System.ComponentModel.DescriptionAttribute))!).Description;
        Assert.Contains("Healthy planned restarts load only the exact ticket-bound primary", commitDescription);
        Assert.Contains("Quarantine recovery alone", commitDescription);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(BeltPathModes.NativeGrid, true)]
    [InlineData(BeltPathModes.NativeGeodesic, false)]
    public async Task GeodesicRequestRequiresAnExactPluginEchoBeforeExposingTheToken(string? echo, bool blocked)
    {
        var bridge = new FakeBridgeClient(SuccessResult()) { BuildBeltRoutingMode = echo };
        var result = await SpherewrightTools.PrepareBuildAsync(bridge, "session", 104, 2001, "player",
            preferredPositionX: 0, preferredPositionY: 200.2f, preferredPositionZ: 0,
            pathEndX: 10, pathEndY: 199.95f, pathEndZ: 0, beltPathMode: BeltPathModes.NativeGeodesic);
        Assert.Equal(BeltPathModes.NativeGeodesic, bridge.LastBuildRequest!.BeltPathMode);
        Assert.Equal(blocked, result.IsError);
        var content = result.StructuredContent!.Value;
        if (blocked)
        {
            Assert.Equal(BridgeErrorCodes.BridgeNotReady, content.GetProperty("error").GetProperty("code").GetString());
            Assert.Equal(JsonValueKind.Null, content.GetProperty("result").ValueKind);
        }
        else Assert.Equal(echo, content.GetProperty("result").GetProperty("plannedBeltPath").GetProperty("routingMode").GetString());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(754)]
    public async Task GeodesicDoesNotNormalizeAnExistingEndpointIntoFreeSpace(int source)
    {
        var bridge = new FakeBridgeClient(SuccessResult());
        var result = await SpherewrightTools.PrepareBuildAsync(bridge, "session", 104, 2001, "player",
            preferredPositionX: 0, preferredPositionY: 200.2f, preferredPositionZ: 0,
            pathEndX: 10, pathEndY: 199.95f, pathEndZ: 0, sourceObjectId: source, beltPathMode: BeltPathModes.NativeGeodesic);
        Assert.True(result.IsError);
        Assert.Null(bridge.LastBuildRequest);
        Assert.Equal(BridgeErrorCodes.InvalidRequest, result.StructuredContent!.Value.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public void NativeGeodesicSubsetIsDiscoverableInSchemaAndEmbeddedGuide()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBridgeClient>(new FakeBridgeClient(SuccessResult()));
        services.AddMcpServer().WithToolsFromAssembly(typeof(SpherewrightTools).Assembly);
        using var provider = services.BuildServiceProvider();
        var tool = Assert.Single(provider.GetServices<McpServerTool>(),
            value => value.ProtocolTool.Name == "spherewright_prepare_build").ProtocolTool;
        Assert.Contains("native_geodesic", tool.InputSchema.GetProperty("properties").GetProperty("beltPathMode").GetProperty("description").GetString());
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("beltPathMode=native_geodesic", guide);
        Assert.Contains("plannedBeltPath.routingMode", guide);
        Assert.Contains("both sorter attachments", guide);
    }

    [Fact]
    public void BeltRoutingGuidanceRequiresWholeConnectionGeometryBeforeRemainingConstruction()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBridgeClient>(new FakeBridgeClient(SuccessResult()));
        services.AddMcpServer().WithToolsFromAssembly(typeof(SpherewrightTools).Assembly);
        using var provider = services.BuildServiceProvider();
        var tool = Assert.Single(provider.GetServices<McpServerTool>(),
            value => value.ProtocolTool.Name == "spherewright_prepare_build").ProtocolTool;
        var description = tool.InputSchema.GetProperty("properties").GetProperty("beltPathMode")
            .GetProperty("description").GetString()!;
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        foreach (var text in new[] { description, guide })
        {
            Assert.Contains("source, bridge and consumer", text);
            Assert.Contains("straight end segments", text);
            Assert.Contains("prepare the critical sorter before constructing the remaining route", text);
        }
        Assert.Contains("5m maximum straight distance", guide);
        Assert.Contains("3.2 maximum local grid segments", guide);
        Assert.Contains("Predicted cardinal directions are not native approval", guide);
        Assert.Contains("fallback excludes thermal generators and two-belt pairs", guide);
        Assert.Contains("do not replay the unchanged pair", guide);
    }

    [Fact]
    public void FullNativePathStageDoesNotDependOnThePlayersUiCommand()
    {
        var method = typeof(SpherewrightTools).GetMethod(nameof(SpherewrightTools.PrepareBuildAsync))!;
        var description = (System.ComponentModel.DescriptionAttribute)Attribute.GetCustomAttribute(method,
            typeof(System.ComponentModel.DescriptionAttribute))!;
        Assert.Contains("tool-owned stage1 command container", description.Description);
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("full native path checks", guide);
        Assert.Contains("never edits the player's command", guide);
        Assert.Contains("does not enable existing-belt cover reuse", guide);
    }

    [Fact]
    public void NewBeltOccupancyBoundaryIsDiscoverableWithoutPromisingUnboundedReuse()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBridgeClient>(new FakeBridgeClient(SuccessResult()));
        services.AddMcpServer().WithToolsFromAssembly(typeof(SpherewrightTools).Assembly);
        using var provider = services.BuildServiceProvider();
        var tool = Assert.Single(provider.GetServices<McpServerTool>(),
            value => value.ProtocolTool.Name == "spherewright_prepare_build").ProtocolTool;
        Assert.Contains("including both ends", tool.Description);
        Assert.Contains("0.25 m", tool.Description);
        Assert.Contains("source-only non-removing cover reuse", tool.Description);
        Assert.Contains("plannedBeltPath", tool.Description);
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("belt_path_existing_overlap", guide);
        Assert.Contains("Do not omit endpoint IDs", guide);
        Assert.Contains("native cover reuse", guide);
        Assert.Contains("does not automatically repair", guide);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("anchor_only_stage0")]
    public async Task LegacyBeltValidationCannotExposeAConstructionCapability(string? mode)
    {
        var bridge = new FakeBridgeClient(SuccessResult()) { OmitBuildBeltEcho = mode is null, BuildBeltMode = mode ?? string.Empty };
        var result = await SpherewrightTools.PrepareBuildAsync(bridge, "session", 104, 2001, "player");
        Assert.True(result.IsError);
        var content = result.StructuredContent!.Value;
        Assert.Equal(BridgeErrorCodes.BridgeNotReady, content.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(JsonValueKind.Null, content.GetProperty("result").ValueKind);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("whole_path_native_rotation_v1", false)]
    public async Task SourceCoverRequiresTheNativeCompletionRotationProof(string? mode, bool blocked)
    {
        var bridge = new FakeBridgeClient(SuccessResult()) { BuildBeltSourceCover = true, BuildBeltPreservationMode = mode };
        var result = await SpherewrightTools.PrepareBuildAsync(bridge, "session", 104, 2001, "player", sourceObjectId: 754, expectedSourceStateHash: "source");
        Assert.Equal(blocked, result.IsError);
        if (blocked) Assert.Equal(JsonValueKind.Null, result.StructuredContent!.Value.GetProperty("result").ValueKind);
        else Assert.Equal(mode, result.StructuredContent!.Value.GetProperty("result").GetProperty("plannedBeltPath").GetProperty("sourcePreservationMode").GetString());
    }

    [Fact]
    public async Task CurrentBeltPlanEchoIsReturnedWithOnlyTheNewObjectBudget()
    {
        var bridge = new FakeBridgeClient(SuccessResult());
        var result = await SpherewrightTools.PrepareBuildAsync(bridge, "session", 104, 2001, "player");
        Assert.False(result.IsError);
        var echo = result.StructuredContent!.Value.GetProperty("result").GetProperty("plannedBeltPath");
        Assert.Equal("full_path_stage1", echo.GetProperty("nativeValidationMode").GetString());
        Assert.Equal(2, echo.GetProperty("newObjectCount").GetInt32());
    }

    [Fact]
    public void AttachmentFailureStagesAreDiscoverableAndDoNotEncourageBlindPairRetries()
    {
        var method = typeof(SpherewrightTools).GetMethod(nameof(SpherewrightTools.PrepareBuildAsync))!;
        var description = (System.ComponentModel.DescriptionAttribute)Attribute.GetCustomAttribute(method,
            typeof(System.ComponentModel.DescriptionAttribute))!;
        Assert.Contains("geometry_unavailable is not an angle verdict", description.Description);
        Assert.Contains("bestFacingDegrees", description.Description);
        Assert.Contains("Do not repeat an unchanged pair", description.Description);
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("candidate_validation_rejected", guide);
        Assert.Contains("not a native placement-check count", guide);
        Assert.Contains("bestFacingDegrees=unknown", guide);
    }

    [Fact]
    public void ExactSlotFailuresExplainActualNativeChecksWithoutTreatingLastAngleAsOverallCause()
    {
        var method = typeof(SpherewrightTools).GetMethod(nameof(SpherewrightTools.PrepareBuildAsync))!;
        var description = (System.ComponentModel.DescriptionAttribute)Attribute.GetCustomAttribute(method,
            typeof(System.ComponentModel.DescriptionAttribute))!;
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        foreach (var text in new[] { description.Description, guide })
        {
            Assert.Contains("nativeChecks", text);
            Assert.Contains("lastNativeRejection", text);
            Assert.Contains("lastPreNativeRejection", text);
        }
        Assert.Contains("actual calls to DSP's placement validator", guide);
        Assert.Contains("Zero checks means the native placement result is unknown", guide);
        Assert.Contains("Do not replay the unchanged pair", guide);
        Assert.Contains("Older Plugins may report only the last exact-slot error", guide);
    }

    [Fact]
    public void EmptyStorageRecoveryIsDiscoverableWithoutAdvertisingGeneralDeletionOrAutomaticRebuild()
    {
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        foreach (var name in new[] { nameof(SpherewrightTools.PrepareDismantleAsync), nameof(SpherewrightTools.CommitDismantleAsync) })
        {
            var method = typeof(SpherewrightTools).GetMethod(name)!;
            var description = (System.ComponentModel.DescriptionAttribute)Attribute.GetCustomAttribute(method,
                typeof(System.ComponentModel.DescriptionAttribute))!;
            foreach (var text in new[] { description.Description, guide })
            {
                Assert.Contains("empty default2101 storage", text);
                Assert.Contains("prebuild", text);
                Assert.Contains("cached", text);
                Assert.Contains("no automatic rebuild", text);
            }
        }
        Assert.Contains("all30 native grids", guide);
        Assert.Contains("TooClose", guide);
        Assert.Contains("minimum grid span", guide);
    }

    [Fact]
    public void BoundedSingleBeltAttachmentIsDiscoverableWithoutWeakeningNativePlacementRules()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBridgeClient>(new FakeBridgeClient(SuccessResult()));
        services.AddMcpServer().WithToolsFromAssembly(typeof(SpherewrightTools).Assembly);
        using var provider = services.BuildServiceProvider();
        var tool = Assert.Single(provider.GetServices<McpServerTool>(),
            value => value.ProtocolTool.Name == "spherewright_prepare_build").ProtocolTool;
        Assert.Contains("native_single_belt_segment", tool.Description);
        Assert.Contains("plannedInserterAttachment", tool.Description);
        Assert.Contains("without retargeting", tool.Description);
        Assert.Contains("collision and materials remain mandatory", tool.Description);
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("at most64 native path intervals", guide);
        Assert.Contains("geometry requires fresh prepare", guide);
        Assert.Contains("does not prove its two sorter attachments", guide);
        Assert.Contains("Closed paths", guide);
    }

    [Fact]
    public void ExactObjectReadParameterAndPlaybookDiscloseDirectBridgeSchema()
    {
        var parameter = typeof(SpherewrightTools).GetMethod(nameof(SpherewrightTools.InspectFactoryEntityAsync))!
            .GetParameters().Single(value => value.Name == "objectId");
        var description = (System.ComponentModel.DescriptionAttribute)Attribute.GetCustomAttribute(parameter,
            typeof(System.ComponentModel.DescriptionAttribute))!;
        Assert.Contains("objectId, not entityId", description.Description);
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("objectId, not entityId", guide);
        Assert.Contains("first100", guide);
    }

    [Fact]
    public void SorterEndpointGeometryIsDiscoverableAsReadOnlyNotPlacementAuthority()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBridgeClient>(new FakeBridgeClient(SuccessResult()));
        services.AddMcpServer().WithToolsFromAssembly(typeof(SpherewrightTools).Assembly);
        using var provider = services.BuildServiceProvider();
        var tool = provider.GetServices<McpServerTool>().Single(value => value.ProtocolTool.Name == "spherewright_inspect_factory_entity").ProtocolTool;
        Assert.True(tool.Annotations!.ReadOnlyHint);
        Assert.Contains("sorterEndpoints", tool.Description);
        Assert.Contains("not a placement approval", tool.Description);
        Assert.Contains("unknown physical occupancy", tool.Description);
        Assert.Contains("not building centers", tool.Description);
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("sorterEndpoints", guide);
        Assert.Contains("one water source does not make a shared belt pure", guide);
    }

    [Fact]
    public void ObservationToolsAndPlaybookDiscloseComponentAndCargoCoverage()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBridgeClient>(new FakeBridgeClient(SuccessResult()));
        services.AddMcpServer().WithToolsFromAssembly(typeof(SpherewrightTools).Assembly);
        using var provider = services.BuildServiceProvider();
        var tools = provider.GetServices<McpServerTool>().Select(t => t.ProtocolTool).ToArray();
        var assemblers = tools.Single(t => t.Name == "spherewright_list_assemblers");
        var entities = tools.Single(t => t.Name == "spherewright_list_factory_entities");
        var inspect = tools.Single(t => t.Name == "spherewright_inspect_factory_entity");
        Assert.Contains("labs, not assemblers", assemblers.Description);
        Assert.Contains("componentKind=lab", entities.Description);
        Assert.Contains("same filtered snapshot", entities.Description);
        foreach (var tool in new[] { entities, inspect })
        {
            Assert.True(tool.Annotations!.ReadOnlyHint);
            Assert.Contains("Belt cargo is not observed", tool.Description);
        }
        Assert.Contains("unfinished trace", inspect.Description);
        Assert.Contains("separate detail-only beltCargo", inspect.Description);
        Assert.Contains("unavailable/null is unknown", inspect.Description);
        Assert.Contains("do not sum", inspect.Description);
        Assert.Contains("upgrade-preservation proof", inspect.Description);
        Assert.Contains("List snapshots omit beltCargo", entities.Description);
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("labs, not assemblers", guide);
        Assert.Contains("Belt cargo is not observed", guide);
        Assert.Contains("cargo unknown, not an empty belt", guide);
        Assert.Contains("never manufacture demand by clearing storage", guide);
        Assert.Contains("state=observed", guide);
        Assert.Contains("do not sum", guide);
        Assert.Contains("whole-path upgrade cargo-preservation", guide);
    }

    [Fact]
    public void RearPickupEvidenceIsDiscoverableButNotQueueOrRepairAuthority()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBridgeClient>(new FakeBridgeClient(SuccessResult()));
        services.AddMcpServer().WithToolsFromAssembly(typeof(SpherewrightTools).Assembly);
        using var provider = services.BuildServiceProvider();
        var inspect = provider.GetServices<McpServerTool>().Single(t => t.ProtocolTool.Name == "spherewright_inspect_factory_entity").ProtocolTool;
        Assert.True(inspect.Annotations!.ReadOnlyHint);
        Assert.Contains("beltCargo.rearPickup", inspect.Description);
        Assert.Contains("no_aligned_packet does not mean an empty belt", inspect.Description);
        Assert.Contains("not queue order", inspect.Description);
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("beltCargo.rearPickup", guide);
        Assert.Contains("aligned native pickup packet", guide);
        Assert.Contains("cannot skip", guide);
        Assert.Contains("last received slot, not an input filter", guide);
        Assert.Contains("never clear stock to manufacture throughput", guide);
    }

    [Fact]
    public void SettlingGuidanceDoesNotReplayMovesOrCountRevisionsAsActions()
    {
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("bounded read-only settling check", guide);
        Assert.Contains("not an extra or repeated Move", guide);
        Assert.Contains("revision is not an accepted-action counter", guide);
    }

    [Fact]
    public void PlayerReadDisclosesDroneReadinessIsNotBuildCompletion()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBridgeClient>(new FakeBridgeClient(SuccessResult()));
        services.AddMcpServer().WithToolsFromAssembly(typeof(SpherewrightTools).Assembly);
        using var provider = services.BuildServiceProvider();
        var read = provider.GetServices<McpServerTool>()
            .Single(t => t.ProtocolTool.Name == "spherewright_get_player_state").ProtocolTool;
        Assert.True(read.Annotations!.ReadOnlyHint);
        Assert.Contains("all alive non-idle drones, not unfinished buildings", read.Description);
        Assert.Contains("must not be replayed", read.Description);
        Assert.Contains("bounded read-only readiness checks", read.Description);
    }

    [Fact]
    public void PackagedBuildReadinessGuidancePreservesCompletedPartsAndFreshIdentity()
    {
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("successful build terminal and idle construction drones are different facts", guide);
        Assert.Contains("at most20 reads separated by500ms", guide);
        Assert.Contains("Continue only the unsubmitted part with fresh hashes", guide);
        Assert.Contains("never replay the completed route or whole module", guide);
        Assert.Contains("Native entity IDs can be reused after removal", guide);
        Assert.Contains("revalidate item, pose and endpoints", guide);
    }

    [Fact]
    public void FlightGuidanceRequiresNativeShoreCompletionAndBoundedStableArrival()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBridgeClient>(new FakeBridgeClient(SuccessResult()));
        services.AddMcpServer().WithToolsFromAssembly(typeof(SpherewrightTools).Assembly);
        using var provider = services.BuildServiceProvider();
        var flight = provider.GetServices<McpServerTool>()
            .Single(t => t.ProtocolTool.Name == "spherewright_commit_interplanetary_flight").ProtocolTool;
        Assert.Contains("transient Walk tick does not complete landing", flight.Description);
        Assert.Contains("unfinished exact shore order", flight.Description);
        Assert.Contains("600 consecutive grounded low-speed ticks", flight.Description);
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("must keep its progress/energy checks until it reaches its target", guide);
        Assert.Contains("Do not issue a competing Move", guide);
        Assert.Contains("cannot be relabeled successful", guide);
        Assert.Contains("Cold startup may retire even a failed-flight checkpoint", guide);
        Assert.Contains("Never edit a ticket or revive a retired token", guide);
        Assert.Contains("a new flight with a new checkpoint", guide);
        Assert.Contains("NO_LOCAL_PLANET", guide);
        Assert.Contains("keep polling the same actionId", guide);
    }

    [Fact]
    public void FactoryReadGuidanceDoesNotBudgetGenerationAsFuelStock()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBridgeClient>(new FakeBridgeClient(SuccessResult()));
        services.AddMcpServer().WithToolsFromAssembly(typeof(SpherewrightTools).Assembly);
        using var provider = services.BuildServiceProvider();
        var read = provider.GetServices<McpServerTool>()
            .Single(t => t.ProtocolTool.Name == "spherewright_list_factory_entities").ProtocolTool;
        Assert.Contains("joules_per_tick, never fuel inventory", read.Description);
        Assert.Contains("unitsPerItem=0 means no item conversion", read.Description);
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("Exclude this role even when an older Plugin incorrectly reports", guide);
        Assert.Contains("never divide by zero", guide);
        Assert.Contains("full-width power summary", guide);
    }

    [Fact]
    public async Task GovernorExposesReadOnlyDeclaredTargetAndExplicitSource()
    {
        var client = new FakeBridgeClient(SuccessResult());
        var source = new BlueprintSelectedEntity { ObjectId = 715, ExpectedRecipeId = 60, ExpectedEndpointStateHash = "fresh" };
        var layout = new FoundryBlueprintRequest { BlueprintCode = "explicit-layout" };
        var result = await SpherewrightTools.GetGovernorPlanAsync(client, "session", 104, 1112, 60, new[] { source }, .1m, 36000,
            validationBaselineProposalHash: "retained-pre-execution-proposal", parallelExpansionBlueprint: layout);
        Assert.False(result.IsError); Assert.Equal("session", client.LastSessionId);
        Assert.Same(source, Assert.Single(client.LastGovernorRequest!.SourceEntities));
        Assert.Equal(36000, client.LastGovernorRequest.ValidationGameTicks);
        Assert.Equal(600, client.LastGovernorRequest.MeasurementGameTicks);
        Assert.Equal(.1m, client.LastGovernorRequest.ToleranceFraction);
        Assert.Equal(60, client.LastGovernorRequest.TargetRatePerMinute);
        Assert.Equal("retained-pre-execution-proposal", client.LastGovernorRequest.ValidationBaselineProposalHash);
        Assert.Same(layout, client.LastGovernorRequest.ParallelExpansionBlueprint);
        var services = new ServiceCollection(); services.AddSingleton<IBridgeClient>(client);
        services.AddMcpServer().WithToolsFromAssembly(typeof(SpherewrightTools).Assembly);
        using var provider = services.BuildServiceProvider();
        var tool = provider.GetServices<McpServerTool>().Single(t => t.ProtocolTool.Name == "spherewright_get_governor_plan").ProtocolTool;
        Assert.True(tool.Annotations!.ReadOnlyHint);
        Assert.Contains("baseline", tool.Description);
        Assert.Contains("consumption", tool.Description);
        Assert.Contains("sourceEntities", tool.InputSchema.ToString());
        Assert.Contains("validationBaselineProposalHash", tool.InputSchema.ToString());
        Assert.Contains("parallelExpansionBlueprint", tool.InputSchema.ToString());
        Assert.Contains("measurementGameTicks", tool.InputSchema.ToString());
        Assert.Contains("parallelExpansionBlueprint", AgentPlaybookResources.GetOpeningMovementPlaybook().Text);
        Assert.Contains("target minus measured baseline", AgentPlaybookResources.GetOpeningMovementPlaybook().Text);
        Assert.Contains("costScope", AgentPlaybookResources.GetOpeningMovementPlaybook().Text);
        Assert.Contains("validationBaselineProposalHash", AgentPlaybookResources.GetOpeningMovementPlaybook().Text);
        Assert.Contains("declarationDurable", tool.Description);
        Assert.Contains("covering protected planned resume", tool.Description);
        Assert.Contains("declarationDurable", tool.InputSchema.ToString());
        Assert.Contains("Restart always resets continuous observation to zero", AgentPlaybookResources.GetOpeningMovementPlaybook().Text);
        Assert.Contains("not permission to relock after expansion", AgentPlaybookResources.GetOpeningMovementPlaybook().Text);
    }

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
                "spherewright_commit_blueprint_build",
                "spherewright_commit_build",
                "spherewright_commit_cancel_blueprint",
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
                "spherewright_commit_upgrade",
                "spherewright_export_blueprint",
                "spherewright_get_action_result",
                "spherewright_get_blueprint_builds",
                "spherewright_get_build_catalog",
                "spherewright_get_foundry_plan",
                "spherewright_get_gameplay_journal",
                "spherewright_get_governor_plan",
                "spherewright_get_local_star_system",
                "spherewright_get_overseer_diagnostic_bundle",
                "spherewright_get_overseer_production",
                "spherewright_get_overseer_summary",
                "spherewright_get_player_state",
                "spherewright_get_power_summary",
                "spherewright_get_progression_state",
                "spherewright_get_recipe_catalog",
                "spherewright_get_session_state",
                "spherewright_get_status",
                "spherewright_inspect_assembler",
                "spherewright_inspect_blueprint",
                "spherewright_inspect_factory_entity",
                "spherewright_inspect_resource_node",
                "spherewright_list_assemblers",
                "spherewright_list_factory_entities",
                "spherewright_list_resource_nodes",
                "spherewright_prepare_blueprint_build",
                "spherewright_prepare_build",
                "spherewright_prepare_cancel_blueprint",
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
                "spherewright_prepare_upgrade",
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
    public async Task FoundryPlan_MapsExplicitRecipeAndSupplyChoicesWithoutCommit()
    {
        var client = new FakeBridgeClient(SuccessResult());
        var result = await SpherewrightTools.GetFoundryPlanAsync(client, "session", 104, 1203, 20.5m,
            new[] { 1101 }, new[] { new FoundryRecipeChoice { ItemId = 1203, RecipeId = 16, BuildingItemId = 2303 } });
        Assert.False(result.IsError);
        Assert.Equal("session", client.LastSessionId);
        var request = Assert.IsType<GetFoundryPlanRequest>(client.LastFoundryRequest);
        Assert.Equal(20.5m, request.TargetRatePerMinute);
        Assert.Equal(1101, Assert.Single(request.ExternalSupplyItemIds));
        Assert.Equal(2303, Assert.Single(request.RecipeChoices).BuildingItemId);
        Assert.Null(request.Site);
    }

    [Fact]
    public async Task FoundryBlueprintLayoutAndIntentAreDiscoverableWithoutAnotherExecutor()
    {
        var client = new FakeBridgeClient(SuccessResult());
        var blueprint = new FoundryBlueprintRequest
        {
            BlueprintCode = "explicit data only", Site = new() { ExpectedPlayerStateHash = "player", Position = new() { Y = 200 } },
            BoundaryPorts = new() { new() { ItemId = 1001, Direction = "input", ObjectIndex = 0, Slot = 1 } },
        };
        await SpherewrightTools.GetFoundryPlanAsync(client, "session", 104, 1203, 10, blueprint: blueprint);
        Assert.Same(blueprint, client.LastFoundryRequest!.Blueprint);
        var intent = new FoundryConstructionIntent { TargetItemId = 1203, TargetRatePerMinute = 10, BoundaryPorts = blueprint.BoundaryPorts };
        await SpherewrightTools.PrepareBlueprintBuildAsync(client, "session", 104, "native-assessment", "player",
            blueprintCode: blueprint.BlueprintCode, site: blueprint.Site,
            foundryIntent: intent, expectedFoundryPlanHash: "construction-hash");
        Assert.Same(intent, client.LastBlueprintBuildRequest!.FoundryIntent);
        Assert.Equal("construction-hash", client.LastBlueprintBuildRequest.ExpectedFoundryPlanHash);
        Assert.Equal("native-assessment", client.LastBlueprintBuildRequest.ExpectedStateHash);
        var services = new ServiceCollection();
        services.AddMcpServer().WithToolsFromAssembly(typeof(SpherewrightTools).Assembly);
        using var provider = services.BuildServiceProvider();
        var tools = provider.GetServices<McpServerTool>().Select(t => t.ProtocolTool).ToArray();
        var read = tools.Single(t => t.Name == "spherewright_get_foundry_plan");
        Assert.True(read.Annotations!.ReadOnlyHint);
        Assert.True(read.InputSchema.GetProperty("properties").TryGetProperty("blueprint", out _));
        Assert.Contains("transportBudget", read.Description, StringComparison.Ordinal);
        Assert.Contains("transportCapacityVerified stays false", read.Description, StringComparison.Ordinal);
        var prepare = tools.Single(t => t.Name == "spherewright_prepare_blueprint_build");
        Assert.True(prepare.InputSchema.GetProperty("properties").TryGetProperty("foundryIntent", out _));
        Assert.True(prepare.InputSchema.GetProperty("properties").TryGetProperty("expectedFoundryPlanHash", out _));
        Assert.Contains("original intent remains durable", prepare.Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpgradeMapsFreshIdentityRecipeAndPlayerEvidence()
    {
        var client = new FakeBridgeClient(SuccessResult());
        var result = await SpherewrightTools.PrepareUpgradeAsync(client, "session", 104, 2255, 2304, 16,
            "endpoint-hash", "player-hash");
        Assert.False(result.IsError);
        var request = Assert.IsType<PrepareUpgradeRequest>(client.LastUpgradeRequest);
        Assert.Equal(0, request.ExpectedFilterItemId);
        Assert.Equal(104, request.PlanetId);
        Assert.Equal(2255, request.ObjectId);
        Assert.Equal(2304, request.TargetItemId);
        Assert.Equal(16, request.ExpectedRecipeId);
        Assert.Equal("endpoint-hash", request.ExpectedEndpointStateHash);
        Assert.Equal("player-hash", request.ExpectedPlayerStateHash);
        Assert.Equal(1, request.StateHashVersion);
    }

    [Fact]
    public async Task SorterUpgradeMapsExplicitInspectedFilter()
    {
        var client = new FakeBridgeClient(SuccessResult());
        var result = await SpherewrightTools.PrepareUpgradeAsync(client, "session", 104, 749, 2012, 0,
            "endpoint-hash", "player-hash", expectedFilterItemId: 1101);
        Assert.False(result.IsError);
        var request = Assert.IsType<PrepareUpgradeRequest>(client.LastUpgradeRequest);
        Assert.Equal(2012, request.TargetItemId);
        Assert.Equal(1101, request.ExpectedFilterItemId);
        Assert.Equal(0, request.ExpectedRecipeId);
    }

    [Fact]
    public async Task FoundryPlan_MapsOptionalBoundedSiteAndRemainsDiscoverableReadOnly()
    {
        var client = new FakeBridgeClient(SuccessResult());
        var result = await SpherewrightTools.GetFoundryPlanAsync(client, "session", 104, 1203, 30,
            site: new FoundrySiteRequest { Origin = new Vector3Snapshot { Y = 200 }, YawDegrees = 90, Columns = 2 });
        Assert.False(result.IsError);
        var request = Assert.IsType<GetFoundryPlanRequest>(client.LastFoundryRequest);
        Assert.Equal(200, request.Site!.Origin.Y); Assert.Equal(90, request.Site.YawDegrees); Assert.Equal(2, request.Site.Columns);
        Assert.Equal(12, request.Site.RowSpacing);
        var services = new ServiceCollection();
        services.AddMcpServer().WithToolsFromAssembly(typeof(SpherewrightTools).Assembly);
        using var provider = services.BuildServiceProvider();
        var tool = provider.GetServices<McpServerTool>().Single(t => t.ProtocolTool.Name == "spherewright_get_foundry_plan").ProtocolTool;
        Assert.True(tool.Annotations!.ReadOnlyHint); Assert.False(tool.Annotations.DestructiveHint);
        Assert.True(tool.InputSchema.GetProperty("properties").TryGetProperty("site", out _));
        Assert.Contains("native grid snapping", tool.Description, StringComparison.Ordinal);
        Assert.Contains("not permission to build", tool.Description, StringComparison.Ordinal);
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
        Assert.Contains("A commit acceptance or host timeout is not completion", contents.Text, StringComparison.Ordinal);
        Assert.Contains("Retain `actionId` and its terminal result before formatting", contents.Text, StringComparison.Ordinal);
        Assert.Contains("spherewright_get_foundry_plan", contents.Text, StringComparison.Ordinal);
        Assert.Contains("spherewright_get_governor_plan", contents.Text, StringComparison.Ordinal);
        Assert.Contains("prioritizeQueued=true", contents.Text, StringComparison.Ordinal);
        Assert.Contains("researchQueueReadback", contents.Text, StringComparison.Ordinal);
        Assert.Contains("get_blueprint_builds", contents.Text, StringComparison.Ordinal);
        Assert.Contains("never replay the whole blueprint", contents.Text, StringComparison.Ordinal);
        Assert.Contains("material draft is not an approved site", contents.Text, StringComparison.Ordinal);
        Assert.Contains("machine_previews_clear", contents.Text, StringComparison.Ordinal);
        Assert.Contains("up to 32 machines", contents.Text, StringComparison.Ordinal);
        Assert.Contains("Do not use `assessmentHash` as a state hash or commit token", contents.Text, StringComparison.Ordinal);
        Assert.Contains("prepare_harvest", contents.Text, StringComparison.Ordinal);
        Assert.Contains("Do not declare a production line complete", contents.Text, StringComparison.Ordinal);
        Assert.Contains("recovery_required", contents.Text, StringComparison.Ordinal);
        Assert.Contains("shore recovery", contents.Text, StringComparison.Ordinal);
        Assert.Contains("site.power", contents.Text, StringComparison.Ordinal);
        Assert.Contains("targetChainFindings", contents.Text, StringComparison.Ordinal);
        Assert.Contains("separate inventory observation interval", contents.Text, StringComparison.Ordinal);
        Assert.Contains("initialSorterFilterItemId", contents.Text, StringComparison.Ordinal);
        Assert.Contains("Later configuration cannot undo contamination", contents.Text, StringComparison.Ordinal);
        Assert.Contains("head-of-line blocking", contents.Text, StringComparison.Ordinal);
        Assert.Contains("reuse the already built objects", contents.Text, StringComparison.Ordinal);
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

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    public async Task BuildMapsInitialSorterFilterAndPreservesEndpointAndPlayerBindings(int filter)
    {
        var bridge = new FakeBridgeClient(SuccessResult());
        var result = await SpherewrightTools.PrepareBuildAsync(bridge, "owned-session", 104, 2011, "fresh-player",
            sourceObjectId: 10, expectedSourceStateHash: "fresh-source", destinationObjectId: 20,
            expectedDestinationStateHash: "fresh-destination", initialSorterFilterItemId: filter);
        Assert.False(result.IsError);
        var request = Assert.IsType<PrepareBuildRequest>(bridge.LastBuildRequest);
        Assert.Equal(filter, request.InitialSorterFilterItemId);
        Assert.Equal("fresh-player", request.ExpectedPlayerStateHash);
        Assert.Equal("fresh-source", request.ExpectedSourceStateHash);
        Assert.Equal("fresh-destination", request.ExpectedDestinationStateHash);
        Assert.Equal(10, request.SourceObjectId);
        Assert.Equal(20, request.DestinationObjectId);
    }

    [Fact]
    public void InitialSorterFilterIsDiscoverableWithContaminationWarning()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBridgeClient>(new FakeBridgeClient(SuccessResult()));
        services.AddMcpServer().WithToolsFromAssembly(typeof(SpherewrightTools).Assembly);
        using var provider = services.BuildServiceProvider();
        var tool = Assert.Single(provider.GetServices<McpServerTool>(),
            value => value.ProtocolTool.Name == "spherewright_prepare_build").ProtocolTool;
        Assert.True(tool.InputSchema.GetProperty("properties").TryGetProperty("initialSorterFilterItemId", out _));
        Assert.Contains("before the first pickup", tool.Description);
        Assert.Contains("later configuration cannot undo contamination", tool.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(1115)]
    public async Task OldOrMismatchedPluginCannotExposeAnUnfilteredConstructionToken(int? echo)
    {
        var bridge = new FakeBridgeClient(SuccessResult()) { OmitBuildFilterEcho = !echo.HasValue, BuildFilterEcho = echo };
        var result = await SpherewrightTools.PrepareBuildAsync(bridge, "session", 104, 2011, "player",
            initialSorterFilterItemId: 1000);
        Assert.True(result.IsError);
        var content = result.StructuredContent!.Value;
        Assert.False(content.GetProperty("success").GetBoolean());
        Assert.Equal(BridgeErrorCodes.BridgeNotReady, content.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(JsonValueKind.Null, content.GetProperty("result").ValueKind);
    }

    [Fact]
    public async Task LegacyUnfilteredBuildDoesNotRequireTheNewResponseField()
    {
        var bridge = new FakeBridgeClient(SuccessResult()) { OmitBuildFilterEcho = true };
        var result = await SpherewrightTools.PrepareBuildAsync(bridge, "session", 104, 2011, "player");
        Assert.False(result.IsError);
        Assert.Equal(0, bridge.LastBuildRequest!.InitialSorterFilterItemId);
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
    public async Task OverseerProductionTool_MapsBoundedItemsPageAndCursor()
    {
        var bridge = new FakeBridgeClient(SuccessResult());

        var result = await SpherewrightTools.GetOverseerProductionAsync(
            bridge,
            "session-overseer",
            new[] { 6003, 6001 },
            4,
            "cursor-overseer",
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("session-overseer", bridge.LastSessionId);
        Assert.Equal(new[] { 6003, 6001 }, bridge.LastOverseerProductionRequest?.ItemIds);
        Assert.Equal(4, bridge.LastOverseerProductionRequest?.Limit);
        Assert.Equal("cursor-overseer", bridge.LastOverseerProductionRequest?.Cursor);
    }

    [Fact]
    public async Task OverseerSummaryTool_MapsPlanetPageAndCursor()
    {
        var bridge = new FakeBridgeClient(SuccessResult());

        var result = await SpherewrightTools.GetOverseerSummaryAsync(
            bridge,
            "session-overseer-summary",
            3,
            "cursor-summary",
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("session-overseer-summary", bridge.LastSessionId);
        Assert.Equal(3, bridge.LastOverseerSummaryRequest?.Limit);
        Assert.Equal("cursor-summary", bridge.LastOverseerSummaryRequest?.Cursor);
    }

    [Fact]
    public async Task OverseerDiagnosticBundleTool_MapsBoundedItemsPageAndCursor()
    {
        var bridge = new FakeBridgeClient(SuccessResult());

        var result = await SpherewrightTools.GetOverseerDiagnosticBundleAsync(
            bridge,
            "session-overseer-bundle",
            new[] { 6003, 1106 },
            2,
            "cursor-bundle",
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("session-overseer-bundle", bridge.LastSessionId);
        Assert.Equal(new[] { 6003, 1106 }, bridge.LastOverseerDiagnosticBundleRequest?.ItemIds);
        Assert.Equal(2, bridge.LastOverseerDiagnosticBundleRequest?.Limit);
        Assert.Equal("cursor-bundle", bridge.LastOverseerDiagnosticBundleRequest?.Cursor);
    }

    [Fact]
    public async Task OverseerBundlePreservesStockedLogisticsBoundaryEvidence()
    {
        var bridge = new FakeBridgeClient(SuccessResult())
        {
            DiagnosticBundleSnapshot = new OverseerDiagnosticBundleSnapshot
            {
                Planets = new List<OverseerDiagnosticBundlePlanetSnapshot>
                {
                    new()
                    {
                        PlanetId = 104,
                        Production = new List<ProductionRateSnapshot>
                        {
                            new()
                            {
                                ItemId = 1106,
                                Findings = new List<OverseerFindingSnapshot>
                                {
                                    new()
                                    {
                                        Kind = OverseerFindingKinds.MaterialShortage,
                                        Evidence = new List<OverseerEvidenceSnapshot>
                                        {
                                            new() { Metric = "source_inventory", NumericValue = 200 },
                                            new() { Metric = "source_inventory_scope", TextValue = "configured_route_supply_total" },
                                            new() { Metric = "logistics_dispatch_state", TextValue = "unproven" },
                                            new() { Metric = "upstream_trace_stop_reason", TextValue = "stocked_logistics_boundary" },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            },
        };

        var result = await SpherewrightTools.GetOverseerDiagnosticBundleAsync(bridge, "session-boundary", new[] { 1106 });

        Assert.False(result.IsError);
        var finding = result.StructuredContent!.Value.GetProperty("result").GetProperty("planets")[0]
            .GetProperty("production")[0].GetProperty("findings")[0];
        Assert.Equal(OverseerFindingKinds.MaterialShortage, finding.GetProperty("kind").GetString());
        var evidence = finding.GetProperty("evidence");
        Assert.Equal(200, evidence[0].GetProperty("numericValue").GetDouble());
        Assert.Equal("configured_route_supply_total", evidence[1].GetProperty("textValue").GetString());
        Assert.Equal("unproven", evidence[2].GetProperty("textValue").GetString());
        Assert.Equal("stocked_logistics_boundary", evidence[3].GetProperty("textValue").GetString());
    }

    [Fact]
    public void PackagedPlaybookDistinguishesStockFromDispatchAndCausalProof()
    {
        var playbook = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("stocked_logistics_boundary", playbook, StringComparison.Ordinal);
        Assert.Contains("configured_route_supply_total", playbook, StringComparison.Ordinal);
        Assert.Contains("logistics_dispatch_state=unproven", playbook, StringComparison.Ordinal);
        Assert.Contains("not available/unreserved stock", playbook, StringComparison.Ordinal);
        Assert.Contains("before changing logistics or expanding a mine", playbook, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PrepareSelectResearch_MapsDedicatedSelectionHash(bool prioritize)
    {
        var bridge = new FakeBridgeClient(SuccessResult());

        var result = await SpherewrightTools.PrepareSelectResearchAsync(
            bridge,
            "session-research",
            104,
            1604,
            "sha256:selection",
            1,
            prioritize,
            CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Equal("session-research", bridge.LastSessionId);
        Assert.Equal(1604, bridge.LastSelectResearchRequest?.TechId);
        Assert.Equal("sha256:selection", bridge.LastSelectResearchRequest?.ExpectedSelectionStateHash);
        Assert.Equal(prioritize, bridge.LastSelectResearchRequest?.PrioritizeQueued);
    }

    [Fact]
    public async Task WarehouseOperationMapsNativeDisabledGridCountAndFreshHash()
    {
        var bridge = new FakeBridgeClient(SuccessResult());
        var result = await SpherewrightTools.PrepareConfigureBuildingAsync(bridge, "session-storage", 104, 761, 0,
            "fresh-storage", mode: BuildingConfigurationModes.StorageCapacity,
            storageOperation: StorageConfigurationOperations.SetBans, storageBannedGridCount: 30);
        Assert.False(result.IsError);
        Assert.Equal(BuildingConfigurationModes.StorageCapacity, bridge.LastConfigureRequest!.Mode);
        Assert.Equal("fresh-storage", bridge.LastConfigureRequest.ExpectedFactoryStateHash);
        Assert.Equal(StorageConfigurationOperations.SetBans, bridge.LastConfigureRequest.StorageOperation);
        Assert.Equal(30, bridge.LastConfigureRequest.StorageBannedGridCount);
    }

    [Fact]
    public async Task WarehouseWithoutConfirmedEchoNeverExposesToken()
    {
        var bridge = new FakeBridgeClient(SuccessResult()) { OmitStorageEcho = true };
        var result = await SpherewrightTools.PrepareConfigureBuildingAsync(bridge, "session-storage", 104, 761, 0,
            "fresh-storage", mode: BuildingConfigurationModes.StorageCapacity,
            storageOperation: StorageConfigurationOperations.FilterEmptyOrMatching, filterItemId: 1000);
        Assert.True(result.IsError);
        var content = result.StructuredContent!.Value;
        Assert.Equal(BridgeErrorCodes.BridgeNotReady, content.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(JsonValueKind.Null, content.GetProperty("result").ValueKind);
        Assert.Equal(-1, bridge.LastConfigureRequest!.StorageBannedGridCount);
        Assert.Equal(1000, bridge.LastConfigureRequest.FilterItemId);
    }

    [Fact]
    public void WarehouseDescriptionDisclosesNativeCapacityMeaningAndNonGridBufferOffsets()
    {
        var method = typeof(SpherewrightTools).GetMethod(nameof(SpherewrightTools.PrepareConfigureBuildingAsync))!;
        var description = ((System.ComponentModel.DescriptionAttribute)method.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false).Single()).Description;
        Assert.Contains("DISABLED final grids", description); Assert.Contains("NOT grid indices", description);
        Assert.Contains("do not clear occupied grids", description); Assert.Contains("lock-occupied", description);
    }

    [Theory]
    [InlineData(3600, false)] [InlineData(600, true)]
    public async Task GovernorMinuteWindowRequiresMatchingInstalledPlugin(int echoed, bool rejected)
    {
        var client = new FakeBridgeClient(SuccessResult()) { GovernorResponseMeasurementGameTicks = echoed };
        var result = await SpherewrightTools.GetGovernorPlanAsync(client, "session", 104, 1109, 76,
            new[] { new BlueprintSelectedEntity { ObjectId = 113, ExpectedEndpointStateHash = "fresh", ExpectedRecipeId = 17 } },
            measurementGameTicks: 3600);
        Assert.Equal(3600, client.LastGovernorRequest!.MeasurementGameTicks);
        Assert.Equal(rejected, result.IsError);
    }

    [Theory]
    [InlineData(0)] [InlineData(36000)]
    public async Task GovernorRejectsUnsupportedMeasurementPeriodBeforeCallingBridge(int period)
    {
        var client = new FakeBridgeClient(SuccessResult());
        var result = await SpherewrightTools.GetGovernorPlanAsync(client, "session", 104, 1109, 76,
            new[] { new BlueprintSelectedEntity { ObjectId = 113, ExpectedEndpointStateHash = "fresh" } },
            measurementGameTicks: period);
        Assert.True(result.IsError); Assert.Null(client.LastGovernorRequest);
    }

    [Fact]
    public void WarehouseReservationRaceRecoveryIsDiscoverableInMcpAndPackagedPlaybook()
    {
        var method = typeof(SpherewrightTools).GetMethod(nameof(SpherewrightTools.PrepareConfigureBuildingAsync))!;
        var description = ((System.ComponentModel.DescriptionAttribute)method.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false).Single()).Description;
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        foreach (var phrase in new[] { "filter0 does not prove an empty grid", "lock-occupied does not reserve empty grids", "storage_configuration_unchanged" })
        {
            Assert.Contains(phrase, description);
            Assert.Contains(phrase, guide);
        }
        Assert.Contains("TakeItem searches forward", guide);
        Assert.Contains("original bans to restore", guide);
        Assert.Contains("not a global inventory freeze", guide);
        Assert.Contains("plan only the unfinished steps", guide);
        Assert.Contains("Never repeat stock removal", guide);
        Assert.Contains("never repeatedly drain stock or relax the full stateHash", description);
    }

    [Fact]
    public void ConfigureHashParameterExplicitlySeparatesSorterAndWarehouseDomains()
    {
        var method = typeof(SpherewrightTools).GetMethod(nameof(SpherewrightTools.PrepareConfigureBuildingAsync))!;
        var parameter = method.GetParameters().Single(p => p.Name == "expectedFactoryStateHash");
        var description = ((System.ComponentModel.DescriptionAttribute)parameter.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false).Single()).Description;
        Assert.Contains("configurationStateHash, NOT stateHash", description);
        Assert.Contains("storage-capacity use the full stateHash", description);
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("sorter-filter uses root `configurationStateHash`; storage-capacity uses full `stateHash`", guide);
        Assert.Contains("inspect the actual payload/hash source", guide, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WarehouseInstantReadbackSurvivesMcpAndWarnsAgainstCrossTickEquality()
    {
        var bridge = new FakeBridgeClient(SuccessResult())
        {
            ActionResult = new ActionResultSnapshot
            {
                ActionId = "storage-config", Terminal = true, Succeeded = true,
                StorageConfigurationReadback = new StorageConfigurationReadback
                {
                    EntityId = 761, CapturedAtGameTick = 100, Operation = StorageConfigurationOperations.SetBans,
                    BuffersBefore = new List<FactoryBufferSnapshot> { new() { ItemId = 1120, Count = 1 } },
                    BuffersAfter = new List<FactoryBufferSnapshot> { new() { ItemId = 1120, Count = 1 } },
                },
            },
        };
        var result = await SpherewrightTools.GetActionResultAsync(bridge, "storage-config", CancellationToken.None);
        var proof = result.StructuredContent!.Value.GetProperty("result").GetProperty("storageConfigurationReadback");
        Assert.Equal(100, proof.GetProperty("capturedAtGameTick").GetInt64());
        Assert.Equal(1, proof.GetProperty("buffersAfter")[0].GetProperty("count").GetInt32());
        var method = typeof(SpherewrightTools).GetMethod(nameof(SpherewrightTools.CommitConfigureBuildingAsync))!;
        var description = ((System.ComponentModel.DescriptionAttribute)method.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false).Single()).Description;
        Assert.Contains("storageConfigurationReadback", description);
        Assert.Contains("do not require cross-tick equality", description);
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("synchronous native configuration boundary", guide);
        Assert.Contains("do not require cross-tick stock equality", guide);
    }

    [Fact]
    public void SorterFilterDescriptionDisclosesRetainedCargoAndUnchangedDestination()
    {
        var method = typeof(SpherewrightTools).GetMethod(nameof(SpherewrightTools.PrepareConfigureBuildingAsync))!;
        var description = ((System.ComponentModel.DescriptionAttribute)method.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false).Single()).Description;
        Assert.Contains("configurationStateHash", description);
        Assert.Contains("Inserting", description);
        Assert.Contains("SAME destination", description);
        Assert.Contains("cannot clear a jam", description);
    }

    [Theory]
    [InlineData(600, 580, 0, 20)]
    [InlineData(0, 20, 20, 0)]
    public async Task TransferTerminalPreservesSameTickBilateralEvidence(
        int storageBefore, int storageAfter, int playerBefore, int playerAfter)
    {
        var bridge = new FakeBridgeClient(SuccessResult())
        {
            ActionResult = new ActionResultSnapshot
            {
                ActionId = "transfer-proof", ActionKind = NormalActionKinds.Transfer,
                State = NormalActionStates.Completed, Terminal = true, Succeeded = true,
                StartedAtGameTick = 100, CompletedAtGameTick = 100,
                TargetObjectId = 753, TargetItemId = 1000, RequestedCount = 20,
                BeforeTargetAmount = storageBefore, AfterTargetAmount = storageAfter,
                ItemDeltas = new List<ActionItemDelta>
                {
                    new() { ItemId = 1000, BeforeCount = playerBefore, AfterCount = playerAfter, Delta = playerAfter - playerBefore },
                },
            },
        };
        var result = await SpherewrightTools.GetActionResultAsync(bridge, "transfer-proof", CancellationToken.None);
        var proof = result.StructuredContent!.Value.GetProperty("result");
        Assert.True(proof.GetProperty("terminal").GetBoolean());
        Assert.True(proof.GetProperty("succeeded").GetBoolean());
        Assert.Equal(100, proof.GetProperty("completedAtGameTick").GetInt64());
        Assert.Equal(storageBefore, proof.GetProperty("beforeTargetAmount").GetInt32());
        Assert.Equal(storageAfter, proof.GetProperty("afterTargetAmount").GetInt32());
        var player = proof.GetProperty("itemDeltas")[0];
        Assert.Equal(playerBefore, player.GetProperty("beforeCount").GetInt32());
        Assert.Equal(playerAfter, player.GetProperty("afterCount").GetInt32());
        Assert.Equal(0, storageAfter - storageBefore + player.GetProperty("delta").GetInt32());
    }

    [Fact]
    public void TransferGuideSeparatesTerminalConservationFromLaterLogistics()
    {
        var method = typeof(SpherewrightTools).GetMethod(nameof(SpherewrightTools.CommitTransferAsync))!;
        var description = ((System.ComponentModel.DescriptionAttribute)Attribute.GetCustomAttribute(method,
            typeof(System.ComponentModel.DescriptionAttribute))!).Description;
        foreach (var field in new[] { "beforeTargetAmount", "afterTargetAmount", "itemDeltas", "completedAtGameTick" })
            Assert.Contains(field, description);
        Assert.Contains("do not require cross-tick equality", description);
        Assert.Contains("never replay", description);
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("transfer uses `storageEntityId`", guide);
        Assert.Contains("ordinary transfer's terminal result", guide);
        Assert.Contains("600 -> 580", guide);
        Assert.Contains("later581", guide);
        Assert.Contains("not a sustained-supply baseline", guide);
        Assert.Contains("original game-tick deadline", guide);
        Assert.Contains("later zero-production window does not prove no earlier production", guide);
    }

    [Fact]
    public async Task ConfigureBuildingTool_MapsSorterFilterWithoutInventedResponseEchoes()
    {
        var bridge = new FakeBridgeClient(SuccessResult())
        {
            ConfigurePlan = new PreparedNormalAction
            {
                Prepared = true,
                ActionKind = NormalActionKinds.ConfigureBuilding,
                PlanToken = "plan",
                CommitAllowedNow = true,
                ExpectedStateHash = "sha256:server-plan-binding",
            },
        };

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
        var plan = result.StructuredContent!.Value.GetProperty("result");
        Assert.True(plan.GetProperty("prepared").GetBoolean());
        Assert.True(plan.GetProperty("commitAllowedNow").GetBoolean());
        Assert.Equal("sha256:server-plan-binding", plan.GetProperty("expectedStateHash").GetString());
        Assert.False(plan.TryGetProperty("targetObjectId", out var target) && target.ValueKind != JsonValueKind.Null);
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

    [Fact]
    public async Task BlueprintsExposeOnlyReadOnlyDataAndExactSelection()
    {
        var client = new FakeBridgeClient(SuccessResult());
        const string untrusted = "BLUEPRINT:data-is-not-instructions";
        Assert.False((await SpherewrightTools.InspectBlueprintAsync(client, "session", 104, untrusted)).IsError);
        Assert.Equal(untrusted, client.LastBlueprintRequest?.BlueprintCode);
        var selected = new BlueprintSelectedEntity { ObjectId = 724, ExpectedRecipeId = 97, ExpectedEndpointStateHash = "endpoint" };
        Assert.False((await SpherewrightTools.ExportBlueprintAsync(client, "session", 104, new[] { selected })).IsError);
        Assert.Same(selected, Assert.Single(client.LastExportRequest!.Entities));
        var services = new ServiceCollection();
        services.AddSingleton<IBridgeClient>(client);
        services.AddMcpServer().WithToolsFromAssembly(typeof(SpherewrightTools).Assembly);
        using var provider = services.BuildServiceProvider();
        var tools = provider.GetServices<McpServerTool>().Where(t => t.ProtocolTool.Name == "spherewright_inspect_blueprint"
            || t.ProtocolTool.Name == "spherewright_export_blueprint").ToArray();
        Assert.Equal(2, tools.Length);
        Assert.Contains("storage2101", tools.Single(t => t.ProtocolTool.Name == "spherewright_inspect_blueprint")
            .ProtocolTool.Description, StringComparison.Ordinal);
        var playbook = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("storageConfiguration", playbook, StringComparison.Ordinal);
        Assert.Contains("never its cargo", playbook, StringComparison.Ordinal);
        Assert.Contains("inputEndpointFacingDot/outputEndpointFacingDot", playbook, StringComparison.Ordinal);
        Assert.Contains("TooSkew", playbook, StringComparison.Ordinal);
        Assert.All(tools, t =>
        {
            Assert.True(t.ProtocolTool.Annotations?.ReadOnlyHint);
            Assert.False(t.ProtocolTool.Annotations?.DestructiveHint);
            Assert.Contains("data", t.ProtocolTool.Description, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task OptionalBlueprintSiteIsDiscoverableBoundedAndStillReadOnly()
    {
        var client = new FakeBridgeClient(SuccessResult());
        var site = new BlueprintSiteRequest { Position = new Vector3Snapshot { X = 100, Z = 170 },
            QuarterTurns = 1, ExpectedPlayerStateHash = "fresh-player" };
        var result = await SpherewrightTools.InspectBlueprintAsync(client, "session", 104, "explicit-code", site);
        Assert.False(result.IsError);
        Assert.Same(site, client.LastBlueprintRequest!.Site);
        var services = new ServiceCollection();
        services.AddSingleton<IBridgeClient>(client);
        services.AddMcpServer().WithToolsFromAssembly(typeof(SpherewrightTools).Assembly);
        using var provider = services.BuildServiceProvider();
        var tool = provider.GetServices<McpServerTool>().Single(t => t.ProtocolTool.Name == "spherewright_inspect_blueprint").ProtocolTool;
        Assert.True(tool.Annotations!.ReadOnlyHint);
        Assert.Contains("site", tool.InputSchema.ToString());
        Assert.Contains("at most32 NEW objects", tool.Description);
        Assert.Contains("executable=false", tool.Description);
        Assert.Contains("No covering", tool.Description);
    }

    [Fact]
    public async Task BlueprintExecutionAndCancellationExposeFreshFiniteAuthority()
    {
        var client = new FakeBridgeClient(SuccessResult());
        Assert.False((await SpherewrightTools.GetBlueprintBuildsAsync(client, "session", 104, "build")).IsError);
        Assert.Equal("build", client.LastBlueprintReadRequest!.BuildId);
        Assert.False((await SpherewrightTools.PrepareBlueprintBuildAsync(client, "session", 104, "progress", "player",
            resumeBuildId: "build", maximumObjectsToSubmit: 1)).IsError);
        Assert.Equal("build", client.LastBlueprintBuildRequest!.ResumeBuildId);
        Assert.Equal("progress", client.LastBlueprintBuildRequest.ExpectedStateHash);
        Assert.Equal(1, client.LastBlueprintBuildRequest.MaximumObjectsToSubmit);
        Assert.Null(client.LastBlueprintBuildRequest.BlueprintCode);
        Assert.Null(client.LastBlueprintBuildRequest.Site);
        Assert.Null(client.LastBlueprintBuildRequest.FoundryIntent);
        Assert.Null(client.LastBlueprintBuildRequest.ExpectedFoundryPlanHash);
        Assert.False((await SpherewrightTools.CommitBlueprintBuildAsync(client, "session", 104, "token", "key")).IsError);
        Assert.False((await SpherewrightTools.PrepareCancelBlueprintAsync(client, "session", 104, "build", "fresh")).IsError);
        Assert.Equal("fresh", client.LastCancelBlueprintRequest!.ExpectedStateHash);
        Assert.False((await SpherewrightTools.CommitCancelBlueprintAsync(client, "session", 104, "token", "key")).IsError);
        var services = new ServiceCollection(); services.AddSingleton<IBridgeClient>(client);
        services.AddMcpServer().WithToolsFromAssembly(typeof(SpherewrightTools).Assembly);
        using var provider = services.BuildServiceProvider();
        var tools = provider.GetServices<McpServerTool>().ToDictionary(t => t.ProtocolTool.Name, t => t.ProtocolTool);
        Assert.Contains("resumeBuildId", tools["spherewright_prepare_blueprint_build"].InputSchema.ToString());
        Assert.Contains("one dependency-ready native prebuild", tools["spherewright_commit_blueprint_build"].Description);
        Assert.Contains("NOT replay", tools["spherewright_commit_blueprint_build"].Description);
        Assert.Contains("no automatic demolition", tools["spherewright_prepare_cancel_blueprint"].Description, StringComparison.OrdinalIgnoreCase);
        Assert.True(tools["spherewright_get_blueprint_builds"].Annotations!.ReadOnlyHint);
        Assert.False(tools["spherewright_commit_blueprint_build"].Annotations!.ReadOnlyHint);
    }

    [Fact]
    public async Task MoveSurfaceEvidenceIsPreservedWithoutChangingRequestOrCommitAuthority()
    {
        var bridge=new FakeBridgeClient(SuccessResult()) { MovePlan=new PreparedNormalAction
        { Prepared=true,CommitAllowedNow=true,PlanToken="move-plan",SurfacePreview=new MovementSurfacePreview
        { State="partial",ShoreRisk="detected",UnknownSampleCount=1,ShoreRiskSampleCount=1,CapturedAtGameTick=123 } } };
        var result=await SpherewrightTools.PrepareMoveAsync(bridge,"session",104,200,0,1,"fresh-player");
        var plan=result.StructuredContent!.Value.GetProperty("result");
        Assert.Equal("detected",plan.GetProperty("surfacePreview").GetProperty("shoreRisk").GetString());
        Assert.Equal("move-plan",plan.GetProperty("planToken").GetString());
        Assert.True(plan.GetProperty("commitAllowedNow").GetBoolean());
        Assert.Equal("fresh-player",bridge.LastMoveRequest!.ExpectedPlayerStateHash);
        Assert.Equal(200,bridge.LastMoveRequest.Target.X);
        Assert.Equal(1,bridge.LastMoveRequest.StateHashVersion);
    }

    [Fact]
    public void MoveSurfacePreviewIsDiscoverableAndNeverDescribedAsPathfinding()
    {
        var services=new ServiceCollection();services.AddSingleton<IBridgeClient>(new FakeBridgeClient(SuccessResult()));
        services.AddMcpServer().WithToolsFromAssembly(typeof(SpherewrightTools).Assembly);
        using var provider=services.BuildServiceProvider();
        var description=provider.GetServices<McpServerTool>().Single(t=>t.ProtocolTool.Name=="spherewright_prepare_move").ProtocolTool.Description!;
        Assert.Contains("surfacePreview",description);
        Assert.Contains("at most66",description);
        Assert.Contains("not route clearance",description);
        Assert.Contains("does not change existing hash/commit admission",description);
        var guide=AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("prepare_move.surfacePreview",guide);
        Assert.Contains("absent evidence is unknown, never dry ground",guide);
        Assert.Contains("drop the recovery intention",guide);
    }

    [Fact]
    public void PackagedPowerGuidanceSeparatesNativeSpacingAndDynamicCapacity()
    {
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("wind-to-wind separation is at least 10.5m", guide);
        Assert.Contains("Check all planned pairs", guide);
        Assert.Contains("does not authorize moving or rebuilding successful objects", guide);
        Assert.Contains("current-tick available generation, not immutable installed topology", guide);
        Assert.Contains("Retain and explain changes", guide);
        Assert.Contains("ignore shortages", guide);
    }

    [Fact]
    public void FuelPowerRatingsAreDiscoverableWithoutClaimingLiveSupplyOrNewWriteAuthority()
    {
        var method = typeof(SpherewrightTools).GetMethod(nameof(SpherewrightTools.GetBuildCatalogAsync))!;
        var description = ((System.ComponentModel.DescriptionAttribute)Attribute.GetCustomAttribute(method,
            typeof(System.ComponentModel.DescriptionAttribute))!).Description;
        Assert.Contains("fuelPowerProfile", description);
        Assert.Contains("Null/missing is unknown, not zero", description);
        Assert.Contains("do not grant Foundry planned-generation credit", description);
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("fuelEnergyUsePerTick * 3600 / fuelHeatValueJoules", guide);
        Assert.Contains("not live generation, fuel inventory or sustainable supply", guide);
        Assert.Contains("do not credit unbuilt/unfuelled generators", guide);
        Assert.Contains("never substitute a remembered generator rating", guide);
    }

    private sealed class FakeBridgeClient : IBridgeClient
    {
        public PrepareBlueprintBuildRequest? LastBlueprintBuildRequest { get; private set; }
        public PrepareCancelBlueprintRequest? LastCancelBlueprintRequest { get; private set; }
        public BlueprintBuildRequest? LastBlueprintReadRequest { get; private set; }
        public Task<BridgeCallResult<BlueprintBuildList>> GetBlueprintBuildsAsync(string sessionId, BlueprintBuildRequest request, CancellationToken cancellationToken)
        {
            LastBlueprintReadRequest = request;
            return Task.FromResult(BridgeCallResult<BlueprintBuildList>.Succeeded(new BlueprintBuildList()));
        }
        public Task<BridgeCallResult<PreparedNormalAction>> PrepareBlueprintBuildAsync(string sessionId, PrepareBlueprintBuildRequest request, CancellationToken cancellationToken)
        { LastBlueprintBuildRequest = request; return Prepared(sessionId, NormalActionKinds.BlueprintBuild); }
        public Task<BridgeCallResult<NormalActionCommitResult>> CommitBlueprintBuildAsync(string sessionId, CommitNormalActionRequest request, CancellationToken cancellationToken) =>
            Committed(sessionId, request, NormalActionKinds.BlueprintBuild);
        public Task<BridgeCallResult<PreparedNormalAction>> PrepareCancelBlueprintAsync(string sessionId, PrepareCancelBlueprintRequest request, CancellationToken cancellationToken)
        { LastCancelBlueprintRequest = request; return Prepared(sessionId, NormalActionKinds.CancelBlueprintBuild); }
        public Task<BridgeCallResult<NormalActionCommitResult>> CommitCancelBlueprintAsync(string sessionId, CommitNormalActionRequest request, CancellationToken cancellationToken) =>
            Committed(sessionId, request, NormalActionKinds.CancelBlueprintBuild);
        public InspectBlueprintRequest? LastBlueprintRequest { get; private set; }
        public ExportBlueprintRequest? LastExportRequest { get; private set; }
        public Task<BridgeCallResult<BlueprintInspection>> InspectBlueprintAsync(string sessionId,
            InspectBlueprintRequest request, CancellationToken cancellationToken)
        {
            LastBlueprintRequest = request;
            return Task.FromResult(BridgeCallResult<BlueprintInspection>.Succeeded(new BlueprintInspection { SessionId = sessionId }));
        }
        public Task<BridgeCallResult<BlueprintInspection>> ExportBlueprintAsync(string sessionId,
            ExportBlueprintRequest request, CancellationToken cancellationToken)
        {
            LastExportRequest = request;
            return Task.FromResult(BridgeCallResult<BlueprintInspection>.Succeeded(new BlueprintInspection { SessionId = sessionId }));
        }
        public PrepareUpgradeRequest? LastUpgradeRequest { get; private set; }
        public Task<BridgeCallResult<PreparedNormalAction>> PrepareUpgradeAsync(string sessionId,
            PrepareUpgradeRequest request, CancellationToken cancellationToken)
        {
            LastUpgradeRequest = request;
            return Prepared(sessionId, NormalActionKinds.Upgrade);
        }

        public Task<BridgeCallResult<NormalActionCommitResult>> CommitUpgradeAsync(string sessionId,
            CommitNormalActionRequest request, CancellationToken cancellationToken) =>
            Committed(sessionId, request, NormalActionKinds.Upgrade);
        private readonly BridgeCallResult<BridgeStatus> _result;

        public FakeBridgeClient(BridgeCallResult<BridgeStatus> result)
        {
            _result = result;
        }

        public BridgeCallResult<ListAssemblersResult>? ListResult { get; set; }

        public ActionResultSnapshot? ActionResult { get; set; }
        public PreparedNormalAction? MovePlan { get; set; }
        public PrepareMoveRequest? LastMoveRequest { get; private set; }
        public SessionState? SessionSnapshot { get; set; }
        public PrepareOwnedWorldResumeRequest? LastResumePrepareRequest { get; private set; }

        public string? LastSessionId { get; private set; }

        public ListAssemblersRequest? LastListRequest { get; private set; }

        public ListResourceNodesRequest? LastResourceListRequest { get; private set; }

        public PrepareConfigureBuildingRequest? LastConfigureRequest { get; private set; }
        public PreparedNormalAction? ConfigurePlan { get; set; }
        public bool OmitStorageEcho { get; set; }

        public PrepareBuildRequest? LastBuildRequest { get; private set; }
        public BridgeError? BuildPrepareError { get; set; }
        public bool OmitBuildFilterEcho { get; set; }
        public bool OmitBuildBeltEcho { get; set; }
        public string BuildBeltMode { get; set; } = "full_path_stage1";
        public bool BuildBeltSourceCover { get; set; }
        public string? BuildBeltPreservationMode { get; set; }
        public string? BuildBeltRoutingMode { get; set; }
        public int? BuildFilterEcho { get; set; }

        public PrepareDismantleRequest? LastDismantleRequest { get; private set; }

        public PrepareLogisticsStationFleetTransferRequest? LastFleetTransferRequest { get; private set; }

        public PrepareQuarantineReconciliationRequest? LastReconciliationRequest { get; private set; }

        public PrepareInterplanetaryFlightRequest? LastFlightRequest { get; private set; }

        public PrepareSelectResearchRequest? LastSelectResearchRequest { get; private set; }

        public PrepareFlightCheckpointReloadRequest? LastFlightCheckpointReloadRequest { get; private set; }

        public PrepareUserSaveImportRequest? LastImportPrepareRequest { get; private set; }

        public CommitUserSaveImportRequest? LastImportCommitRequest { get; private set; }

        public GetOverseerProductionRequest? LastOverseerProductionRequest { get; private set; }

        public GetOverseerSummaryRequest? LastOverseerSummaryRequest { get; private set; }

        public GetOverseerDiagnosticBundleRequest? LastOverseerDiagnosticBundleRequest { get; private set; }

        public OverseerDiagnosticBundleSnapshot? DiagnosticBundleSnapshot { get; set; }

        public Task<BridgeCallResult<BridgeStatus>> GetBridgeStatusAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_result);
        }

        public Task<BridgeCallResult<SessionState>> GetSessionStateAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(BridgeCallResult<SessionState>.Succeeded(SessionSnapshot ?? new SessionState
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
            CancellationToken cancellationToken)
        {
            LastMoveRequest=request;
            return MovePlan is null ? Prepared(sessionId,NormalActionKinds.Move)
                : Task.FromResult(BridgeCallResult<PreparedNormalAction>.Succeeded(MovePlan));
        }

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
            CancellationToken cancellationToken)
        {
            LastBuildRequest = request;
            LastSessionId = sessionId;
            if (BuildPrepareError is not null)
                return Task.FromResult(BridgeCallResult<PreparedNormalAction>.Failed(BuildPrepareError));
            var prepared = new PreparedNormalAction
            {
                Prepared = true, ActionKind = NormalActionKinds.Build, PlanToken = "plan",
                PlannedSorterFilterItemId = OmitBuildFilterEcho ? null : BuildFilterEcho ?? request.InitialSorterFilterItemId,
            };
            if (request.BuildingItemId >= 2001 && request.BuildingItemId <= 2003)
            {
                prepared.BuildKind = "belt";
                prepared.SourceObjectId = request.SourceObjectId;
                prepared.DestinationObjectId = request.DestinationObjectId;
                prepared.PlannedPath.AddRange(new[] { new Vector3Snapshot { Y = 200 }, new Vector3Snapshot { X = 1, Y = 200 } });
                prepared.ItemBudget.Add(new ActionItemBudget { ItemId = request.BuildingItemId, Count = 2, Direction = "construction-consumption" });
                if (!OmitBuildBeltEcho)
                    prepared.PlannedBeltPath = new BeltPathPlanSnapshot { NativeValidationMode = BuildBeltMode,
                        SourceBindingMode = BuildBeltSourceCover ? "non_removing_belt_cover" : request.SourceObjectId.HasValue ? "native_device_port" : "none",
                        ReusedSourceObjectId = BuildBeltSourceCover ? request.SourceObjectId : null,
                        SourcePreservationMode = BuildBeltPreservationMode, NewObjectCount = 2, RoutingMode = BuildBeltRoutingMode };
            }
            return Task.FromResult(BridgeCallResult<PreparedNormalAction>.Succeeded(prepared));
        }

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

        public async Task<BridgeCallResult<PreparedNormalAction>> PrepareConfigureBuildingAsync(
            string sessionId,
            PrepareConfigureBuildingRequest request,
            CancellationToken cancellationToken)
        {
            LastConfigureRequest = request;
            var result = await Prepared(sessionId, NormalActionKinds.ConfigureBuilding);
            if (ConfigurePlan is not null)
                result = BridgeCallResult<PreparedNormalAction>.Succeeded(ConfigurePlan);
            if (request.Mode == BuildingConfigurationModes.StorageCapacity && !OmitStorageEcho && result.Value is not null)
            {
                result.Value.PlannedStorageOperation = request.StorageOperation;
                result.Value.PlannedStorageConfiguration = new StorageConfigurationSnapshot
                    { GridCount = 30, BannedGridCount = request.StorageBannedGridCount, Mode = "default", GridFilterItemIds = Enumerable.Repeat(0, 30).ToList() };
            }
            return result;
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

        public Task<BridgeCallResult<FoundryPlanSnapshot>> GetFoundryPlanAsync(
            string sessionId, GetFoundryPlanRequest request, CancellationToken cancellationToken)
        {
            LastSessionId = sessionId;
            LastFoundryRequest = request;
            return Task.FromResult(BridgeCallResult<FoundryPlanSnapshot>.Succeeded(new FoundryPlanSnapshot
            {
                SessionId = sessionId, PlanetId = request.PlanetId,
                TargetItemId = request.TargetItemId, TargetRatePerMinute = request.TargetRatePerMinute,
            }));
        }

        public GetFoundryPlanRequest? LastFoundryRequest { get; private set; }
        public GetGovernorPlanRequest? LastGovernorRequest { get; private set; }
        public int? GovernorResponseMeasurementGameTicks { get; set; }
        public Task<BridgeCallResult<GovernorPlanSnapshot>> GetGovernorPlanAsync(string sessionId, GetGovernorPlanRequest request, CancellationToken cancellationToken)
        {
            LastSessionId = sessionId; LastGovernorRequest = request;
            return Task.FromResult(BridgeCallResult<GovernorPlanSnapshot>.Succeeded(new GovernorPlanSnapshot
            { SessionId = sessionId, PlanetId = request.PlanetId, TargetItemId = request.TargetItemId, TargetRatePerMinute = request.TargetRatePerMinute,
                MeasurementGameTicks = GovernorResponseMeasurementGameTicks ?? request.MeasurementGameTicks }));
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

        public Task<BridgeCallResult<OverseerProductionSnapshot>> GetOverseerProductionAsync(
            string sessionId,
            GetOverseerProductionRequest request,
            CancellationToken cancellationToken)
        {
            LastSessionId = sessionId;
            LastOverseerProductionRequest = request;
            return Task.FromResult(BridgeCallResult<OverseerProductionSnapshot>.Succeeded(
                new OverseerProductionSnapshot
                {
                    SessionId = sessionId,
                    RequestedItemIds = request.ItemIds.ToList(),
                }));
        }

        public Task<BridgeCallResult<OverseerSummarySnapshot>> GetOverseerSummaryAsync(
            string sessionId,
            GetOverseerSummaryRequest request,
            CancellationToken cancellationToken)
        {
            LastSessionId = sessionId;
            LastOverseerSummaryRequest = request;
            return Task.FromResult(BridgeCallResult<OverseerSummarySnapshot>.Succeeded(
                new OverseerSummarySnapshot
                {
                    SessionId = sessionId,
                }));
        }

        public Task<BridgeCallResult<OverseerDiagnosticBundleSnapshot>> GetOverseerDiagnosticBundleAsync(
            string sessionId,
            GetOverseerDiagnosticBundleRequest request,
            CancellationToken cancellationToken)
        {
            LastSessionId = sessionId;
            LastOverseerDiagnosticBundleRequest = request;
            return Task.FromResult(BridgeCallResult<OverseerDiagnosticBundleSnapshot>.Succeeded(
                DiagnosticBundleSnapshot ?? new OverseerDiagnosticBundleSnapshot
                {
                    SessionId = sessionId,
                    RequestedItemIds = request.ItemIds.ToList(),
                }));
        }

        public Task<BridgeCallResult<PreparedOwnedWorldResumePlan>> PrepareOwnedWorldResumeAsync(
            PrepareOwnedWorldResumeRequest request,
            CancellationToken cancellationToken)
        {
            LastResumePrepareRequest = request;
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
