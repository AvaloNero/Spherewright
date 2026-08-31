using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Spherewright.Contracts.Actions;
using Spherewright.Contracts.Factory;
using Spherewright.Contracts.Resources;
using Spherewright.Contracts.Sessions;
using Spherewright.Contracts.Testing;
using Spherewright.Mcp.BridgeClient;

namespace Spherewright.Mcp.Tools;

[McpServerToolType]
public static class SpherewrightTools
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [McpServerTool(
        Name = "spherewright_get_status",
        Title = "Get Spherewright status",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Returns the authenticated local Spherewright bridge, plugin, protocol, game-version, and write-health status. This Gate A tool never reads save contents or changes the game.")]
    public static async Task<CallToolResult> GetStatusAsync(
        [Description("Injected authenticated bridge client.")] IBridgeClient bridgeClient,
        [Description("Cancellation token supplied by the MCP host.")] CancellationToken cancellationToken)
    {
        var result = await bridgeClient.GetBridgeStatusAsync(cancellationToken).ConfigureAwait(false);
        var payload = new SpherewrightStatusToolResult
        {
            Success = result.Success,
            Status = result.Value,
            Error = result.Error,
        };
        var text = result.Success
            ? "Spherewright bridge is connected."
            : $"{result.Error!.Code}: {result.Error.Message} Recovery: {result.Error.Recovery}";

        return new CallToolResult
        {
            IsError = !result.Success,
            StructuredContent = JsonSerializer.SerializeToElement(payload, JsonOptions),
            Content = new List<ContentBlock>
            {
                new TextContentBlock { Text = text },
            },
        };
    }

    [McpServerTool(
        Name = "spherewright_get_session_state",
        Title = "Get Spherewright session state",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Returns a privacy-gated game-session snapshot. Save, planet, and factory metadata are returned only for a dedicated world created by the current Spherewright Plugin process.")]
    public static async Task<CallToolResult> GetSessionStateAsync(
        [Description("Injected authenticated bridge client.")] IBridgeClient bridgeClient,
        [Description("Cancellation token supplied by the MCP host.")] CancellationToken cancellationToken)
    {
        var result = await bridgeClient.GetSessionStateAsync(cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Spherewright session state is available.");
    }

    [McpServerTool(
        Name = "spherewright_get_player_state",
        Title = "Get player state in the owned ordinary world",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Returns a main-thread snapshot of position, movement, mecha energy, inventory, handcraft queue, and construction drones. It refuses unowned sessions.")]
    public static async Task<CallToolResult> GetPlayerStateAsync(
        [Description("Injected authenticated bridge client.")] IBridgeClient bridgeClient,
        [Description("Current session ID returned by spherewright_get_session_state.")] string sessionId,
        [Description("Current local planet ID returned by spherewright_get_session_state.")] int planetId,
        [Description("Cancellation token supplied by the MCP host.")] CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.GetPlayerStateAsync(
            sessionId,
            new LocalPlanetRequest { PlanetId = planetId },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Player snapshot captured from the owned ordinary world.");
    }

    [McpServerTool(
        Name = "spherewright_get_progression_state",
        Title = "Get technology progression in the owned ordinary world",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Returns the current technology, research queue, hash progress, matrix requirements, prerequisites, and unlocked technology states from the current runtime.")]
    public static async Task<CallToolResult> GetProgressionStateAsync(
        [Description("Injected authenticated bridge client.")] IBridgeClient bridgeClient,
        [Description("Current session ID returned by spherewright_get_session_state.")] string sessionId,
        [Description("Current local planet ID returned by spherewright_get_session_state.")] int planetId,
        [Description("Cancellation token supplied by the MCP host.")] CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.GetProgressionStateAsync(
            sessionId,
            new LocalPlanetRequest { PlanetId = planetId },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Technology progression captured from the owned ordinary world.");
    }

    [McpServerTool(
        Name = "spherewright_get_recipe_catalog",
        Title = "Get runtime items, recipes, and first-red-matrix dependencies",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Returns current LDB item and recipe identities, unlock state, inputs and outputs, plus a deterministic runtime dependency graph rooted at the first red matrix.")]
    public static async Task<CallToolResult> GetRecipeCatalogAsync(
        [Description("Injected authenticated bridge client.")] IBridgeClient bridgeClient,
        [Description("Current session ID returned by spherewright_get_session_state.")] string sessionId,
        [Description("Current local planet ID returned by spherewright_get_session_state.")] int planetId,
        [Description("Cancellation token supplied by the MCP host.")] CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.GetRecipeCatalogAsync(
            sessionId,
            new LocalPlanetRequest { PlanetId = planetId },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Runtime recipe catalog and red-matrix dependency graph captured.");
    }

    [McpServerTool(
        Name = "spherewright_list_resource_nodes",
        Title = "List resource nodes in the owned ordinary world",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Lists an immutable bounded snapshot of local veins and hand-harvestable vegetation. The opaque cursor is bound to session, planet, filters, page size, and expiry.")]
    public static async Task<CallToolResult> ListResourceNodesAsync(
        [Description("Injected authenticated bridge client.")] IBridgeClient bridgeClient,
        [Description("Current session ID returned by spherewright_get_session_state.")] string sessionId,
        [Description("Current local planet ID returned by spherewright_get_session_state.")] int planetId,
        [Description("Optional kind: vein or vegetation.")] string kind = "",
        [Description("Optional runtime resource type such as Iron, Copper, Oil, Tree, or Stone.")] string resourceType = "",
        [Description("Optional yielded item ID; use zero for no item filter.")] int productItemId = 0,
        [Description("Page size from 1 through 100.")] int limit = 50,
        [Description("Opaque continuation cursor, or empty to create a new snapshot.")] string cursor = "",
        [Description("Cancellation token supplied by the MCP host.")] CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.ListResourceNodesAsync(
            sessionId,
            new ListResourceNodesRequest
            {
                PlanetId = planetId,
                Kind = string.IsNullOrWhiteSpace(kind) ? null : kind,
                ResourceType = string.IsNullOrWhiteSpace(resourceType) ? null : resourceType,
                ProductItemId = productItemId > 0 ? productItemId : null,
                Limit = limit,
                Cursor = string.IsNullOrWhiteSpace(cursor) ? null : cursor,
            },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Resource-node page captured from one immutable owned-world snapshot.");
    }

    [McpServerTool(
        Name = "spherewright_inspect_resource_node",
        Title = "Inspect a live resource node",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Re-reads one live vein or vegetation object on Unity's main thread; it does not rely on a prior list snapshot.")]
    public static async Task<CallToolResult> InspectResourceNodeAsync(
        [Description("Injected authenticated bridge client.")] IBridgeClient bridgeClient,
        [Description("Current session ID returned by spherewright_get_session_state.")] string sessionId,
        [Description("Current local planet ID returned by spherewright_get_session_state.")] int planetId,
        [Description("Resource kind returned by spherewright_list_resource_nodes.")] string kind,
        [Description("Node ID returned by spherewright_list_resource_nodes.")] int nodeId,
        [Description("Cancellation token supplied by the MCP host.")] CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.InspectResourceNodeAsync(
            sessionId,
            new InspectResourceNodeRequest { PlanetId = planetId, Kind = kind, NodeId = nodeId },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Live resource node captured from the owned ordinary world.");
    }

    [McpServerTool(
        Name = "spherewright_list_factory_entities",
        Title = "List built entities and prebuilds",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Lists an immutable bounded snapshot of built factory entities and legal prebuilds, including component identity, position, recipe, buffers, connections, and power state where applicable.")]
    public static async Task<CallToolResult> ListFactoryEntitiesAsync(
        [Description("Injected authenticated bridge client.")] IBridgeClient bridgeClient,
        [Description("Current session ID returned by spherewright_get_session_state.")] string sessionId,
        [Description("Current local planet ID returned by spherewright_get_session_state.")] int planetId,
        [Description("Optional object kind: entity or prebuild.")] string objectKind = "",
        [Description("Optional component kind such as miner, assembler, lab, inserter, belt, storage, or power-generator.")] string componentKind = "",
        [Description("Optional exact building item ID; use zero for no item filter.")] int itemId = 0,
        [Description("Page size from 1 through 100.")] int limit = 50,
        [Description("Opaque continuation cursor, or empty to create a new snapshot.")] string cursor = "",
        [Description("Cancellation token supplied by the MCP host.")] CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.ListFactoryEntitiesAsync(
            sessionId,
            new ListFactoryEntitiesRequest
            {
                PlanetId = planetId,
                ObjectKind = string.IsNullOrWhiteSpace(objectKind) ? null : objectKind,
                ComponentKind = string.IsNullOrWhiteSpace(componentKind) ? null : componentKind,
                ItemId = itemId > 0 ? itemId : null,
                Limit = limit,
                Cursor = string.IsNullOrWhiteSpace(cursor) ? null : cursor,
            },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Factory-object page captured from one immutable owned-world snapshot.");
    }

    [McpServerTool(
        Name = "spherewright_inspect_factory_entity",
        Title = "Inspect a live built entity or prebuild",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Re-reads one built entity (positive objectId) or prebuild (negative objectId) on Unity's main thread.")]
    public static async Task<CallToolResult> InspectFactoryEntityAsync(
        [Description("Injected authenticated bridge client.")] IBridgeClient bridgeClient,
        [Description("Current session ID returned by spherewright_get_session_state.")] string sessionId,
        [Description("Current local planet ID returned by spherewright_get_session_state.")] int planetId,
        [Description("Positive entity ID or negative prebuild ID returned by spherewright_list_factory_entities.")] int objectId,
        [Description("Cancellation token supplied by the MCP host.")] CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.InspectFactoryEntityAsync(
            sessionId,
            new InspectFactoryEntityRequest { PlanetId = planetId, ObjectId = objectId },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Live factory object captured from the owned ordinary world.");
    }

    [McpServerTool(
        Name = "spherewright_get_power_summary",
        Title = "Get local planet power networks",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Returns all active local power networks with generation, capacity, demand, served energy, storage, and service ratios from the current runtime.")]
    public static async Task<CallToolResult> GetPowerSummaryAsync(
        [Description("Injected authenticated bridge client.")] IBridgeClient bridgeClient,
        [Description("Current session ID returned by spherewright_get_session_state.")] string sessionId,
        [Description("Current local planet ID returned by spherewright_get_session_state.")] int planetId,
        [Description("Cancellation token supplied by the MCP host.")] CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.GetPowerSummaryAsync(
            sessionId,
            new LocalPlanetRequest { PlanetId = planetId },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Power-network summary captured from the owned ordinary world.");
    }

    [McpServerTool(
        Name = "spherewright_prepare_configure_building",
        Title = "Prepare an idle device configuration",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false)]
    [Description("Re-reads one exact idle built device with empty buffers and prepares either a production recipe, matrix-research mode, or sorter item filter without changing it.")]
    public static async Task<CallToolResult> PrepareConfigureBuildingAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        int entityId,
        int recipeId,
        string expectedFactoryStateHash,
        string mode = BuildingConfigurationModes.Production,
        int techId = 0,
        int filterItemId = 0,
        int stateHashVersion = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.PrepareConfigureBuildingAsync(
            sessionId,
            new PrepareConfigureBuildingRequest
            {
                PlanetId = planetId,
                EntityId = entityId,
                RecipeId = recipeId,
                Mode = mode,
                TechId = techId,
                FilterItemId = filterItemId,
                ExpectedFactoryStateHash = expectedFactoryStateHash,
                StateHashVersion = stateHashVersion,
            },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Idle empty-device configuration plan prepared; device state is unchanged.");
    }

    [McpServerTool(
        Name = "spherewright_commit_configure_building",
        Title = "Commit an idle device configuration",
        ReadOnly = false,
        Destructive = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Applies the prepared recipe, matrix-research mode, or sorter filter once through the current-version UI/business path, then rereads the exact device.")]
    public static async Task<CallToolResult> CommitConfigureBuildingAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        string planToken,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var request = CreateCommitRequest(sessionId, planetId, planToken, idempotencyKey);
        var result = await bridgeClient.CommitConfigureBuildingAsync(sessionId, request, cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Device configuration completed with live readback.");
    }

    [McpServerTool(
        Name = "spherewright_prepare_build",
        Title = "Prepare one normal owned-item building",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false)]
    [Description("Uses DSP's click-build validator to find one clear near-player site for an unlocked building already in inventory. Bound source and destination objects use endpointStateHash so ordinary production progress cannot make their unchanged physical topology stale. Prepare creates no prebuild and consumes nothing.")]
    public static async Task<CallToolResult> PrepareBuildAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        int buildingItemId,
        string expectedPlayerStateHash,
        float preferredDistance = 12f,
        float? preferredPositionX = null,
        float? preferredPositionY = null,
        float? preferredPositionZ = null,
        float? preferredYaw = null,
        int resourceNodeId = 0,
        string expectedResourceStateHash = "",
        int sourceObjectId = 0,
        string expectedSourceStateHash = "",
        int destinationObjectId = 0,
        string expectedDestinationStateHash = "",
        float? pathEndX = null,
        float? pathEndY = null,
        float? pathEndZ = null,
        float pathLength = 6f,
        int stateHashVersion = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.PrepareBuildAsync(
            sessionId,
            new PrepareBuildRequest
            {
                PlanetId = planetId,
                BuildingItemId = buildingItemId,
                PreferredDistance = preferredDistance,
                PreferredPosition = CreateOptionalVector(preferredPositionX, preferredPositionY, preferredPositionZ),
                PreferredYaw = preferredYaw,
                ResourceNodeId = resourceNodeId > 0 ? resourceNodeId : (int?)null,
                ExpectedResourceStateHash = string.IsNullOrWhiteSpace(expectedResourceStateHash) ? null : expectedResourceStateHash,
                SourceObjectId = sourceObjectId > 0 ? sourceObjectId : (int?)null,
                ExpectedSourceStateHash = string.IsNullOrWhiteSpace(expectedSourceStateHash) ? null : expectedSourceStateHash,
                DestinationObjectId = destinationObjectId > 0 ? destinationObjectId : (int?)null,
                ExpectedDestinationStateHash = string.IsNullOrWhiteSpace(expectedDestinationStateHash) ? null : expectedDestinationStateHash,
                PathEnd = CreateOptionalVector(pathEndX, pathEndY, pathEndZ),
                PathLength = pathLength,
                ExpectedPlayerStateHash = expectedPlayerStateHash,
                StateHashVersion = stateHashVersion,
            },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "One owned-item construction plan prepared through DSP's build validator; no prebuild exists yet.");
    }

    [McpServerTool(
        Name = "spherewright_commit_build",
        Title = "Create and wait on one normal prebuild",
        ReadOnly = false,
        Destructive = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Consumes one owned building item through BuildTool_Click.CreatePrebuilds and returns a pollable action. Spherewright never calls BuildFinally; normal construction drones must finish it.")]
    public static async Task<CallToolResult> CommitBuildAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        string planToken,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var request = CreateCommitRequest(sessionId, planetId, planToken, idempotencyKey);
        var result = await bridgeClient.CommitBuildAsync(sessionId, request, cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Normal prebuild accepted; poll its actionId while construction drones work.");
    }

    [McpServerTool(
        Name = "spherewright_prepare_transfer",
        Title = "Prepare an exact player-storage transfer",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false)]
    [Description("Re-reads the player and one exact storage, verifies source count, destination capacity, range, and bilateral conservation without moving any item.")]
    public static async Task<CallToolResult> PrepareTransferAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        string direction,
        int storageEntityId,
        int itemId,
        int count,
        string expectedPlayerStateHash,
        string expectedStorageStateHash,
        int stateHashVersion = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.PrepareTransferAsync(
            sessionId,
            new PrepareTransferRequest
            {
                PlanetId = planetId,
                Direction = direction,
                StorageEntityId = storageEntityId,
                ItemId = itemId,
                Count = count,
                ExpectedPlayerStateHash = expectedPlayerStateHash,
                ExpectedStorageStateHash = expectedStorageStateHash,
                StateHashVersion = stateHashVersion,
            },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Exact player-storage transfer plan prepared; both containers are unchanged.");
    }

    [McpServerTool(
        Name = "spherewright_commit_transfer",
        Title = "Commit an exact player-storage transfer",
        ReadOnly = false,
        Destructive = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Moves the prepared exact count through StorageComponent's normal UI business operations and proves equal-and-opposite container deltas.")]
    public static async Task<CallToolResult> CommitTransferAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        string planToken,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var request = CreateCommitRequest(sessionId, planetId, planToken, idempotencyKey);
        var result = await bridgeClient.CommitTransferAsync(sessionId, request, cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Player-storage transfer completed with exact bilateral conservation readback.");
    }

    [McpServerTool(
        Name = "spherewright_prepare_refuel",
        Title = "Prepare a normal mecha refuel transfer",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false)]
    [Description("Re-reads player inventory and the mecha fuel chamber, then binds the exact native one-stack transfer count and destination grid without moving fuel.")]
    public static async Task<CallToolResult> PrepareRefuelAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        int itemId,
        int count,
        string expectedPlayerStateHash,
        int stateHashVersion = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.PrepareRefuelAsync(
            sessionId,
            new PrepareRefuelRequest
            {
                PlanetId = planetId,
                ItemId = itemId,
                Count = count,
                ExpectedPlayerStateHash = expectedPlayerStateHash,
                StateHashVersion = stateHashVersion,
            },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Native mecha-refuel plan prepared; inventory and fuel chamber are unchanged.");
    }

    [McpServerTool(
        Name = "spherewright_commit_refuel",
        Title = "Commit a normal mecha refuel transfer",
        ReadOnly = false,
        Destructive = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Uses Mecha.AutoReplenishFuel for the prepared stack and proves exact equal-and-opposite player/fuel-chamber item deltas; it never injects energy or items.")]
    public static async Task<CallToolResult> CommitRefuelAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        string planToken,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var request = CreateCommitRequest(sessionId, planetId, planToken, idempotencyKey);
        var result = await bridgeClient.CommitRefuelAsync(sessionId, request, cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Mecha refuel completed through DSP's native transfer with conservation readback.");
    }

    [McpServerTool(
        Name = "spherewright_prepare_save",
        Title = "Prepare a save of the owned world",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false)]
    [Description("Binds the current revision and exact high-entropy save identity of the active Spherewright-created session without writing a save.")]
    public static async Task<CallToolResult> PrepareSaveAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        long expectedRevision,
        int stateHashVersion = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.PrepareSaveAsync(
            sessionId,
            new PrepareSaveRequest
            {
                PlanetId = planetId,
                ExpectedRevision = expectedRevision,
                StateHashVersion = stateHashVersion,
            },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Owned-world save plan prepared; no save file was written.");
    }

    [McpServerTool(
        Name = "spherewright_commit_save",
        Title = "Save the owned world normally",
        ReadOnly = false,
        Destructive = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Calls DSP's normal save API only for the exact current Spherewright-owned save name and records the confirmed game tick; it never enumerates or opens another save.")]
    public static async Task<CallToolResult> CommitSaveAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        string planToken,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var request = CreateCommitRequest(sessionId, planetId, planToken, idempotencyKey);
        var result = await bridgeClient.CommitSaveAsync(sessionId, request, cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "DSP confirmed a normal save of the exact active Spherewright-owned world.");
    }

    [McpServerTool(
        Name = "spherewright_prepare_quarantine_reconciliation",
        Title = "Prove the exact quarantined action outcome",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false)]
    [Description("Re-reads only the exact outcome-unknown build named by writeQuarantineActionId and prepares a short-lived proof when its retained item cost, built entities, components, and directed topology are all unambiguous. This never clears quarantine or changes the game.")]
    public static async Task<CallToolResult> PrepareQuarantineReconciliationAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        string actionId,
        long expectedRevision,
        int stateHashVersion = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.PrepareQuarantineReconciliationAsync(
            sessionId,
            new PrepareQuarantineReconciliationRequest
            {
                PlanetId = planetId,
                ActionId = actionId,
                ExpectedRevision = expectedRevision,
                StateHashVersion = stateHashVersion,
            },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Exact quarantine reconciliation proof prepared; quarantine remains active until its matching commit.");
    }

    [McpServerTool(
        Name = "spherewright_commit_quarantine_reconciliation",
        Title = "Commit an exact quarantine reconciliation",
        ReadOnly = false,
        Destructive = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Revalidates the prepared proof and clears write quarantine only when the same retained outcome-unknown action, reason, item cost, entities, components, and topology still match. It is not an unconditional administrative clear.")]
    public static async Task<CallToolResult> CommitQuarantineReconciliationAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        string planToken,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var request = CreateCommitRequest(sessionId, planetId, planToken, idempotencyKey);
        var result = await bridgeClient.CommitQuarantineReconciliationAsync(sessionId, request, cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "The exact prior action was proved and its matching write quarantine was cleared.");
    }

    [McpServerTool(
        Name = "spherewright_prepare_move",
        Title = "Prepare normal surface movement",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false)]
    [Description("Re-reads the owned-world player state and prepares a short-lived normal ground-movement order. It never moves or teleports the player during prepare.")]
    public static async Task<CallToolResult> PrepareMoveAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        float targetX,
        float targetY,
        float targetZ,
        string expectedPlayerStateHash,
        float arrivalTolerance = 1.5f,
        int stateHashVersion = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.PrepareMoveAsync(
            sessionId,
            new PrepareMoveRequest
            {
                PlanetId = planetId,
                Target = new Vector3Snapshot { X = targetX, Y = targetY, Z = targetZ },
                ArrivalTolerance = arrivalTolerance,
                ExpectedPlayerStateHash = expectedPlayerStateHash,
                StateHashVersion = stateHashVersion,
            },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Normal surface-movement plan prepared; player state is unchanged.");
    }

    [McpServerTool(
        Name = "spherewright_commit_move",
        Title = "Commit prepared normal surface movement",
        ReadOnly = false,
        Destructive = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Starts the prepared movement through DSP's Player.Order path and returns a pollable action. It never writes player position.")]
    public static async Task<CallToolResult> CommitMoveAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        string planToken,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var request = CreateCommitRequest(sessionId, planetId, planToken, idempotencyKey);
        var result = await bridgeClient.CommitMoveAsync(sessionId, request, cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Normal movement order accepted; poll its actionId for game-tick completion.");
    }

    [McpServerTool(
        Name = "spherewright_prepare_harvest",
        Title = "Prepare normal manual harvesting",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false)]
    [Description("Binds an inspected vein or vegetation node plus current player state and budgets its runtime manual-mining yield without changing the world.")]
    public static async Task<CallToolResult> PrepareHarvestAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        string resourceKind,
        [Description("Resource node ID returned by spherewright_list_resource_nodes or spherewright_inspect_resource_node; factory object IDs are a separate namespace and are invalid here.")] int nodeId,
        int requestedYieldCount,
        string expectedResourceStateHash,
        string expectedPlayerStateHash,
        int stateHashVersion = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.PrepareHarvestAsync(
            sessionId,
            new PrepareHarvestRequest
            {
                PlanetId = planetId,
                ResourceKind = resourceKind,
                NodeId = nodeId,
                RequestedYieldCount = requestedYieldCount,
                ExpectedResourceStateHash = expectedResourceStateHash,
                ExpectedPlayerStateHash = expectedPlayerStateHash,
                StateHashVersion = stateHashVersion,
            },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Normal manual-harvest plan prepared; resource and inventory are unchanged.");
    }

    [McpServerTool(
        Name = "spherewright_commit_harvest",
        Title = "Commit prepared normal manual harvesting",
        ReadOnly = false,
        Destructive = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Starts DSP's normal player mining order. Walking, mining time, energy use, node depletion, and item delivery remain game-driven.")]
    public static async Task<CallToolResult> CommitHarvestAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        string planToken,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var request = CreateCommitRequest(sessionId, planetId, planToken, idempotencyKey);
        var result = await bridgeClient.CommitHarvestAsync(sessionId, request, cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Normal manual-harvest order accepted; poll its actionId for conservation readback.");
    }

    [McpServerTool(
        Name = "spherewright_prepare_handcraft",
        Title = "Prepare a normal replicator task",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false)]
    [Description("Validates an unlocked handcraft recipe against the live player inventory through MechaForge's test path and returns a no-side-effect plan.")]
    public static async Task<CallToolResult> PrepareHandcraftAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        int recipeId,
        int count,
        string expectedPlayerStateHash,
        int stateHashVersion = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.PrepareHandcraftAsync(
            sessionId,
            new PrepareHandcraftRequest
            {
                PlanetId = planetId,
                RecipeId = recipeId,
                Count = count,
                ExpectedPlayerStateHash = expectedPlayerStateHash,
                StateHashVersion = stateHashVersion,
            },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Normal replicator plan prepared; no material or queue state changed.");
    }

    [McpServerTool(
        Name = "spherewright_commit_handcraft",
        Title = "Commit a prepared normal replicator task",
        ReadOnly = false,
        Destructive = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Queues the prepared recipe through MechaForge.AddTask so normal ingredients, energy, time, and output delivery apply.")]
    public static async Task<CallToolResult> CommitHandcraftAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        string planToken,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var request = CreateCommitRequest(sessionId, planetId, planToken, idempotencyKey);
        var result = await bridgeClient.CommitHandcraftAsync(sessionId, request, cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Normal replicator task accepted; poll its actionId for product readback.");
    }

    [McpServerTool(
        Name = "spherewright_prepare_select_research",
        Title = "Prepare normal technology selection",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false)]
    [Description("Validates a runtime technology, prerequisites, level, and queue through DSP's CanEnqueueTech path without changing research state.")]
    public static async Task<CallToolResult> PrepareSelectResearchAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        int techId,
        string expectedProgressionStateHash,
        int stateHashVersion = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.PrepareSelectResearchAsync(
            sessionId,
            new PrepareSelectResearchRequest
            {
                PlanetId = planetId,
                TechId = techId,
                ExpectedProgressionStateHash = expectedProgressionStateHash,
                StateHashVersion = stateHashVersion,
            },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Normal technology-selection plan prepared; research state is unchanged.");
    }

    [McpServerTool(
        Name = "spherewright_commit_select_research",
        Title = "Commit prepared normal technology selection",
        ReadOnly = false,
        Destructive = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Queues the prepared technology through GameHistoryData.EnqueueTech; it does not add hashes, matrices, or unlock flags.")]
    public static async Task<CallToolResult> CommitSelectResearchAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        string planToken,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var request = CreateCommitRequest(sessionId, planetId, planToken, idempotencyKey);
        var result = await bridgeClient.CommitSelectResearchAsync(sessionId, request, cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Normal technology queue accepted the selection.");
    }

    [McpServerTool(
        Name = "spherewright_get_action_result",
        Title = "Get a retained Spherewright action result",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Returns the current or terminal state of an action accepted by this Plugin process. It does not repeat the action.")]
    public static async Task<CallToolResult> GetActionResultAsync(
        [Description("Injected authenticated bridge client.")] IBridgeClient bridgeClient,
        [Description("Action ID returned by a Spherewright commit.")] string actionId,
        [Description("Cancellation token supplied by the MCP host.")] CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.GetActionResultAsync(
            new GetActionResultRequest { ActionId = actionId },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Retained action state captured without repeating the action.");
    }

    [McpServerTool(
        Name = "spherewright_list_assemblers",
        Title = "List assemblers in the owned ordinary world",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Lists a bounded page of assembler snapshots from the active Spherewright-owned ordinary world. It refuses unowned game sessions.")]
    public static async Task<CallToolResult> ListAssemblersAsync(
        [Description("Injected authenticated bridge client.")] IBridgeClient bridgeClient,
        [Description("Current session ID returned by spherewright_get_session_state.")] string sessionId,
        [Description("Maximum assemblers to return, from 1 through 100.")] int limit = 50,
        [Description("Opaque continuation cursor from a previous result, or an empty string to start.")] string cursor = "",
        [Description("Cancellation token supplied by the MCP host.")] CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.ListAssemblersAsync(
            sessionId,
            new ListAssemblersRequest { Limit = limit, Cursor = string.IsNullOrEmpty(cursor) ? null : cursor },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Assembler page captured from the owned ordinary world.");
    }

    [McpServerTool(
        Name = "spherewright_inspect_assembler",
        Title = "Inspect an assembler in the owned ordinary world",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Returns one validated assembler snapshot from the active Spherewright-owned ordinary world. It refuses stale entities and unowned game sessions.")]
    public static async Task<CallToolResult> InspectAssemblerAsync(
        [Description("Injected authenticated bridge client.")] IBridgeClient bridgeClient,
        [Description("Current session ID returned by spherewright_get_session_state.")] string sessionId,
        [Description("Current assembler entity ID returned by spherewright_list_assemblers.")] int entityId,
        [Description("Cancellation token supplied by the MCP host.")] CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.InspectAssemblerAsync(
            sessionId,
            new InspectAssemblerRequest { EntityId = entityId },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Assembler snapshot captured from the owned ordinary world.");
    }

    [McpServerTool(
        Name = "spherewright_get_build_catalog",
        Title = "Get the current unlocked ordinary build catalog",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Returns the currently implemented unlocked building and smelting-recipe candidates from DSP's runtime prototypes. It refuses unowned sessions and never changes game state.")]
    public static async Task<CallToolResult> GetBuildCatalogAsync(
        [Description("Injected authenticated bridge client.")] IBridgeClient bridgeClient,
        [Description("Current session ID returned by spherewright_get_session_state.")] string sessionId,
        [Description("Cancellation token supplied by the MCP host.")] CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.GetBuildCatalogAsync(sessionId, cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Live unlocked build catalog captured from the owned ordinary world.");
    }

    [McpServerTool(
        Name = "spherewright_prepare_new_game",
        Title = "Prepare a standard peaceful new game",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false)]
    [Description("Creates a short-lived, single-use plan for a fresh peaceful 1x world with sandbox mode disabled. This prepare step does not start a game or write a save.")]
    public static async Task<CallToolResult> PrepareTestWorldAsync(
        [Description("Injected authenticated bridge client.")] IBridgeClient bridgeClient,
        [Description("Galaxy seed from 0 through 99999999.")] int galaxySeed = 13572468,
        [Description("Galaxy star count from 20 through 80.")] int starCount = 32,
        [Description("Cancellation token supplied by the MCP host.")] CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.PrepareTestWorldAsync(
            new PrepareTestWorldRequest { GalaxySeed = galaxySeed, StarCount = starCount },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Standard peaceful new-world plan prepared; no game state changed.");
    }

    [McpServerTool(
        Name = "spherewright_commit_new_game",
        Title = "Create the prepared standard peaceful world",
        ReadOnly = false,
        Destructive = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Consumes one unexpired plan to start a fresh peaceful 1x non-sandbox world through DSP's official new-game flow. Requires writes enabled and a UUID idempotency key.")]
    public static async Task<CallToolResult> CommitTestWorldAsync(
        [Description("Injected authenticated bridge client.")] IBridgeClient bridgeClient,
        [Description("Single-use plan token returned by spherewright_prepare_new_game.")] string planToken,
        [Description("UUID reused for retries of this exact commit.")] string idempotencyKey,
        [Description("Cancellation token supplied by the MCP host.")] CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.CommitTestWorldAsync(
            new CommitTestWorldRequest { PlanToken = planToken, IdempotencyKey = idempotencyKey },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Standard peaceful Spherewright world creation accepted.");
    }

    [McpServerTool(
        Name = "spherewright_prepare_resume_owned_game",
        Title = "Prepare one-time resume of the exact owned world",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false)]
    [Description("Validates a protected one-time ticket and only the freshness metadata of DSP's fixed LastExit slot. It never enumerates saves, accepts a save name, or loads anything during prepare.")]
    public static async Task<CallToolResult> PrepareOwnedWorldResumeAsync(
        IBridgeClient bridgeClient,
        string resumeToken,
        CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.PrepareOwnedWorldResumeAsync(
            new PrepareOwnedWorldResumeRequest { ResumeToken = resumeToken },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "One-time exact owned-world resume plan prepared; no save was loaded.");
    }

    [McpServerTool(
        Name = "spherewright_commit_resume_owned_game",
        Title = "Resume the exact normally closed owned world",
        ReadOnly = false,
        Destructive = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Loads only DSP's fixed LastExit slot through DSPGame.StartGame and adopts it only when the one-time ticket's embedded high-entropy owned name, minimum tick, planet, peaceful/non-sandbox state, and 1x resources all match.")]
    public static async Task<CallToolResult> CommitOwnedWorldResumeAsync(
        IBridgeClient bridgeClient,
        string planToken,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.CommitOwnedWorldResumeAsync(
            new CommitOwnedWorldResumeRequest
            {
                PlanToken = planToken,
                IdempotencyKey = idempotencyKey,
            },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "DSP accepted the exact fixed LastExit resume; poll actionId for provenance validation and high-entropy resave.");
    }

    private static CommitNormalActionRequest CreateCommitRequest(
        string sessionId,
        int planetId,
        string planToken,
        string idempotencyKey)
    {
        return new CommitNormalActionRequest
        {
            SessionId = sessionId,
            PlanetId = planetId,
            PlanToken = planToken,
            IdempotencyKey = idempotencyKey,
        };
    }

    private static Vector3Snapshot? CreateOptionalVector(float? x, float? y, float? z)
    {
        if (!x.HasValue && !y.HasValue && !z.HasValue)
        {
            return null;
        }

        if (!x.HasValue || !y.HasValue || !z.HasValue)
        {
            throw new ArgumentException("All three vector coordinates must be supplied together.");
        }

        return new Vector3Snapshot { X = x.Value, Y = y.Value, Z = z.Value };
    }

    private static CallToolResult ToToolResult<T>(BridgeCallResult<T> result, string successText)
    {
        var payload = new SpherewrightToolResult<T>
        {
            Success = result.Success,
            Result = result.Value,
            Error = result.Error,
        };
        var text = result.Success
            ? successText
            : $"{result.Error!.Code}: {result.Error.Message} Recovery: {result.Error.Recovery}";

        return new CallToolResult
        {
            IsError = !result.Success,
            StructuredContent = JsonSerializer.SerializeToElement(payload, JsonOptions),
            Content = new List<ContentBlock>
            {
                new TextContentBlock { Text = text },
            },
        };
    }
}
