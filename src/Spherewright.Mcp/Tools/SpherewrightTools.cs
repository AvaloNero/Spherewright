using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Spherewright.Contracts.Actions;
using Spherewright.Contracts.Celestial;
using Spherewright.Contracts.Diagnostics;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Factory;
using Spherewright.Contracts.Journals;
using Spherewright.Contracts.Resources;
using Spherewright.Contracts.Sessions;
using Spherewright.Contracts.Testing;
using Spherewright.Mcp.BridgeClient;
using Spherewright.Mcp.Resources;

namespace Spherewright.Mcp.Tools;

[McpServerToolType]
public static partial class SpherewrightTools
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
    [Description("Returns the authenticated local Spherewright bridge, plugin, protocol, game-version, and write-health status. Read the advertised opening and core-operation Agent playbook before the first gameplay action, including after new-world creation. This tool never reads save contents or changes the game.")]
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
            AgentPlaybookResourceUri = AgentPlaybookResources.OpeningMovementUri,
            RecommendedFirstStep = "Read the opening and core-operation Agent playbook MCP resource before the first gameplay action.",
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
    [Description("Returns a privacy-gated game-session snapshot. Before the first gameplay action in a session, read MCP resource spherewright://agent/playbooks/opening-movement-v1. At the main menu, gameLoaded=false is expected before protected resume. restartResumeAvailable advertises a ticket, not final native menu readiness: fresh prepare_resume_owned_game checks that readiness. Do not wait for a loaded world before calling prepare. Save, planet, and factory metadata are returned only for an owned world.")]
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
    [Description("Returns a main-thread snapshot of position, movement, mecha energy, inventory, handcraft queue, construction drones, autoManageResearchItems and mechaResearchItemBuffer (research points, whole items and remainder points). constructionDrones.working counts all alive non-idle drones, not unfinished buildings; a successful build terminal must not be replayed because this count is still positive. Use bounded read-only readiness checks before the next operation. Compare research buffers with retained inventory and fresh progression when reconciling native research material returns; a backpack increase alone is not a new transfer or production event. It refuses unowned sessions.")]
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
    [Description("Returns current technology, research queue, hash progress, requirements, prerequisites and unlock states. completionItemRewards is bounded native completion metadata, not a delivery receipt: null/missing means unknown; an empty list means observed no rewards. Reconcile actual before/after inventory, capacity, unlockTick and action boundaries before attributing a gain. Never replay a completed action or count rewards as handcraft/production; read the Agent playbook for research inventory accounting.")]
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
        Name = "spherewright_get_gameplay_journal",
        Title = "Get the per-save first-event gameplay journal",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Returns the protected journal for the current owned save: first manual and production-line output per item are tracked independently, as are first technology and upgrade selections, with wall-clock and in-save game times. For attached_existing_save with historicalCoverageComplete=false, an absent first-event entry is not proof that the item was never made: historical seeds suppress later repeats without backfilling timestamps. Verify durableThroughSequence and persistence health; prove a craft from its terminal and materials, not an expected Journal increment. Never replay a completed craft because its first-event entry is absent.")]
    public static async Task<CallToolResult> GetGameplayJournalAsync(
        [Description("Injected authenticated bridge client.")] IBridgeClient bridgeClient,
        [Description("Current session ID returned by spherewright_get_session_state.")] string sessionId,
        [Description("Cancellation token supplied by the MCP host.")] CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.GetGameplayJournalAsync(sessionId, cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Per-save first-event gameplay journal captured.");
    }

    [McpServerTool(
        Name = "spherewright_get_local_star_system",
        Title = "Get planets in the current star system",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Returns the current star's planets, live distances, non-generating theme-based potential resources, and stable planet identity. It does not generate or inspect unvisited factories.")]
    public static async Task<CallToolResult> GetLocalStarSystemAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.GetLocalStarSystemAsync(
            sessionId,
            new LocalPlanetRequest { PlanetId = planetId },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Current-star planet catalog captured without generating unvisited factories.");
    }

    [McpServerTool(
        Name = "spherewright_get_recipe_catalog",
        Title = "Get runtime items, recipes, and first-red-matrix dependencies",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Returns current LDB item and recipe identities, unlock state, inputs and outputs, plus a deterministic runtime dependency graph rooted at the first red matrix. Item fuelHeatValueJoules/fuelType and nullable acceptedAsMechaFuel use the same current native eligibility as normal refuel preparation: fuels are not limited to names containing fuel rod. Null is unknown. Item heat is not burn-rate, movement-range or automatic-supply proof; acquire a real stack and use fresh prepare_refuel/commit_refuel with the native exact transfer count.")]
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
    [Description("Lists an immutable bounded snapshot of built factory entities and legal prebuilds, including component identity, position, recipe, buffers, connections, and power state where applicable. Use componentKind=station for detailed logistics-station state, and componentKind=lab for matrix production/research; assembler-only results do not include labs. Finish the same filtered snapshot's pages before claiming absence. Buffer units matter: power-generation-current-tick is joules_per_tick, never fuel inventory, even if an older Plugin labels it items; unitsPerItem=0 means no item conversion. Optional tankFluidCount reports identity-verified total fluid items including zero; null/missing is unknown. Empty tank buffers alone are not zero; the scalar and positive tank-fluid buffer are the same stock, not additive. Belt cargo is not observed in buffers; an empty belt buffers list does not prove an empty belt. List snapshots omit beltCargo: use inspect_factory_entity for that bounded local observation. Trace actual directed/reciprocal endpoints and observed device buffers/Overseer rates instead.")]
    public static async Task<CallToolResult> ListFactoryEntitiesAsync(
        [Description("Injected authenticated bridge client.")] IBridgeClient bridgeClient,
        [Description("Current session ID returned by spherewright_get_session_state.")] string sessionId,
        [Description("Current local planet ID returned by spherewright_get_session_state.")] int planetId,
        [Description("Optional object kind: entity or prebuild.")] string objectKind = "",
        [Description("Optional component kind such as miner, assembler, lab, inserter, belt, storage, station, or power-generator.")] string componentKind = "",
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
    [Description("Re-reads one built entity (positive objectId) or prebuild (negative objectId) on Unity's main thread. Completed station entities include detailed logisticsStation state and independent live/configuration hashes. Optional tankFluidCount reports identity-verified total fluid items including zero; null/missing is unknown. Empty tank buffers alone are not zero; the scalar and positive tank-fluid buffer are the same stock, not additive. Belt cargo is not observed in buffers: use the separate detail-only beltCargo object. state=observed reports unique stacks touching only this belt segment at one game tick; unavailable/null is unknown, never zero. The read is bounded to 512 segment cells, with explicit reasons for unobserved seams or invalid references. Adjacent observations can include the same stack: do not sum them, infer flow from a single sample, or use these counts as an upgrade-preservation proof. Optional beltCargo.rearPickup identifies only the aligned native pickup packet when this segment ends an open path. no_aligned_packet does not mean an empty belt; not_applicable/unavailable/null is no queue-head evidence. Aggregated items are not queue order. Compare fresh reciprocal station input and needs/capacity before inferring mixed-input blockage, never call a single snapshot proof of sustained deadlock or clear stock to manufacture throughput. Optional detail-only sorterEndpoints exposes at most16 native slot world positions/outward directions with occupancy, or4 belt virtual orientations. Virtual slot=-1 has unknown physical occupancy, not a free-slot promise. Plan from slot geometry, not building centers; an observed endpoint is not a placement approval. Missing/unavailable geometry is unknown. Lists omit this geometry; existing action/endpoint hashes do not bind either optional observation. Follow bounded directed/reciprocal connections to real consumers; an unfinished trace is not proof that no consumer exists.")]
    public static async Task<CallToolResult> InspectFactoryEntityAsync(
        [Description("Injected authenticated bridge client.")] IBridgeClient bridgeClient,
        [Description("Current session ID returned by spherewright_get_session_state.")] string sessionId,
        [Description("Current local planet ID returned by spherewright_get_session_state.")] int planetId,
        [Description("Required nonzero positive entity ID or negative prebuild ID returned by spherewright_list_factory_entities. This read's Bridge field is objectId, not entityId; a first page lacking an ID does not prove absence.")] int objectId,
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
        Name = "spherewright_get_overseer_production",
        Title = "Get multi-planet production windows",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Returns a cursor-bound page of all already-created owned factories for up to 64 exact item IDs. Actual rates come from DSP's save-persisted 600-game-tick automatic production and consumption window; this is not an interval ledger, so do not sum overlapping or gapped windows as full-interval conservation, and keep inventory intervals separate. A below-target conservative lower bound is inconclusive, not proof of insufficient actual inflow. Read the Agent playbook before declaring a supply failure. Theoretical output capacity is independently recomputed from identity-bound current runtime components. Supported assemblers, matrix labs, and resource extractors report bounded findings for material shortage, full output buffers, insufficient power, and exhausted veins; material shortages recursively follow item-admitting physical cargo paths and can cross an exact demand/supply station route into a producer behind the supply station's input belt. Physically proven logistics routes use a protected per-save game-tick window: active or warming shipments do not become shortages, while a truly missing consumer input with a positive demand reservation, available source inventory, a nonempty fleet, and 600 continuous ticks without carrier/order/delivery progress produces only a suspected logistics stall. Refilled input resets the stagnant baseline.")]
    public static async Task<CallToolResult> GetOverseerProductionAsync(
        [Description("Injected authenticated bridge client.")] IBridgeClient bridgeClient,
        [Description("Current session ID returned by spherewright_get_session_state.")] string sessionId,
        [Description("One to 64 unique positive runtime item IDs. Resend the identical IDs with a cursor.")] int[] itemIds,
        [Description("Number of planets per page, from 1 to 16; zero uses the default of 8.")] int limit = 0,
        [Description("Opaque nextCursor from the preceding page, or empty for a fresh snapshot.")] string cursor = "",
        [Description("Cancellation token supplied by the MCP host.")] CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.GetOverseerProductionAsync(
            sessionId,
            new GetOverseerProductionRequest
            {
                ItemIds = (itemIds ?? Array.Empty<int>()).ToList(),
                Limit = limit,
                Cursor = string.IsNullOrWhiteSpace(cursor) ? null : cursor,
            },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Save-persisted multi-planet production window captured from the owned ordinary world.");
    }

    [McpServerTool(
        Name = "spherewright_get_overseer_summary",
        Title = "Get multi-planet operations summary",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Returns one cursor-bound snapshot page containing per-planet power networks and logistics aggregates for every already-created owned factory, plus one global current-research summary. It does not create or load remote factories.")]
    public static async Task<CallToolResult> GetOverseerSummaryAsync(
        [Description("Injected authenticated bridge client.")] IBridgeClient bridgeClient,
        [Description("Current session ID returned by spherewright_get_session_state.")] string sessionId,
        [Description("Number of planets per page, from 1 to 16; zero uses the default of 8.")] int limit = 0,
        [Description("Opaque nextCursor from the preceding page, or empty for a fresh snapshot.")] string cursor = "",
        [Description("Cancellation token supplied by the MCP host.")] CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.GetOverseerSummaryAsync(
            sessionId,
            new GetOverseerSummaryRequest
            {
                Limit = limit,
                Cursor = string.IsNullOrWhiteSpace(cursor) ? null : cursor,
            },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Multi-planet power, logistics, and research summary captured from the owned ordinary world.");
    }

    [McpServerTool(
        Name = "spherewright_get_overseer_diagnostic_bundle",
        Title = "Get a same-tick Overseer diagnostic bundle",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Returns one cursor-bound, same-game-tick diagnostic bundle that joins bounded production rates and root-cause findings with per-planet power and logistics summaries plus global research. A stocked_logistics_boundary retains consumer shortage and aggregate source inventory without claiming dispatch failure or an upstream producer cause. The public allowlist schema excludes save identity, filesystem paths, auth credentials, and action plan credentials. It does not create or load remote factories.")]
    public static async Task<CallToolResult> GetOverseerDiagnosticBundleAsync(
        [Description("Injected authenticated bridge client.")] IBridgeClient bridgeClient,
        [Description("Current session ID returned by spherewright_get_session_state.")] string sessionId,
        [Description("One to 64 unique positive runtime item IDs. Resend the identical IDs with a cursor.")] int[] itemIds,
        [Description("Number of planets per page, from 1 to 16; zero uses the default of 8.")] int limit = 0,
        [Description("Opaque nextCursor from the preceding page, or empty for a fresh snapshot.")] string cursor = "",
        [Description("Cancellation token supplied by the MCP host.")] CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.GetOverseerDiagnosticBundleAsync(
            sessionId,
            new GetOverseerDiagnosticBundleRequest
            {
                ItemIds = (itemIds ?? Array.Empty<int>()).ToList(),
                Limit = limit,
                Cursor = string.IsNullOrWhiteSpace(cursor) ? null : cursor,
            },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Same-tick multi-planet Overseer diagnostic bundle captured from the owned ordinary world.");
    }

    [McpServerTool(
        Name = "spherewright_prepare_configure_building",
        Title = "Prepare a device configuration",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false)]
    [Description("Re-reads one exact built device for a production recipe, matrix-research mode, sorter filter, storage-capacity, station storage slot/output selector/charge setting without changing it. Storage-capacity requires an unstacked2101 warehouse and fresh full stateHash: storageOperation is set-bans (storageBannedGridCount is DISABLED final grids, 0 restores all), lock-occupied, filter-empty-or-matching (filterItemId), or clear-filters. Non-ban operations use storageBannedGridCount=-1. These native UI operations preserve all inventory and do not clear occupied grids or prove throughput; inspect plannedStorageConfiguration. Public buffers omit empty grids, so their offsets are NOT grid indices. filter0 does not prove an empty grid; lock-occupied does not reserve empty grids. Live delivery can occupy space between operations; storage_configuration_unchanged is not proof of a reservation. Read the agent playbook for bounded input guarding, normal transfer and capacity restoration; never repeatedly drain stock or relax the full stateHash. Sorter mode uses configurationStateHash including carried cargo: cargo-free sorters and ordinary2011/2012 unidirectional sorters in Inserting are supported. Existing cargo is preserved and still goes to the SAME destination; a new filter only governs future pickups and cannot clear a jam. Station modes bind their separate configuration hash and never clear, replace, or fill station inventory.")]
    public static async Task<CallToolResult> PrepareConfigureBuildingAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        int entityId,
        int recipeId,
        [Description("For mode=sorter-filter use the inspected root configurationStateHash, NOT stateHash, despite this parameter's name. For mode=storage-capacity use the full stateHash. Follow the documented hash domain for each other mode.")]
        string expectedFactoryStateHash,
        string mode = BuildingConfigurationModes.Production,
        int techId = 0,
        int filterItemId = 0,
        int stationStorageIndex = -1,
        int stationBeltSlotIndex = -1,
        int stationBeltStorageIndex = -1,
        int stationItemId = 0,
        int stationMaximumCount = 0,
        string stationLocalLogic = LogisticsStorageLogics.None,
        string stationRemoteLogic = LogisticsStorageLogics.None,
        long stationMaximumChargePowerWatts = 0,
        string expectedStationConfigurationStateHash = "",
        int stateHashVersion = 1,
        string storageOperation = "",
        int storageBannedGridCount = -1,
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
                StorageOperation = storageOperation,
                StorageBannedGridCount = storageBannedGridCount,
                StationStorageIndex = stationStorageIndex,
                StationBeltSlotIndex = stationBeltSlotIndex,
                StationBeltStorageIndex = stationBeltStorageIndex,
                StationItemId = stationItemId,
                StationMaximumCount = stationMaximumCount,
                StationLocalLogic = stationLocalLogic,
                StationRemoteLogic = stationRemoteLogic,
                StationMaximumChargePowerWatts = stationMaximumChargePowerWatts,
                ExpectedStationConfigurationStateHash = expectedStationConfigurationStateHash,
                ExpectedFactoryStateHash = expectedFactoryStateHash,
                StateHashVersion = stateHashVersion,
            },
            cancellationToken).ConfigureAwait(false);
        if (mode == BuildingConfigurationModes.StorageCapacity && result.Success
            && (result.Value?.PlannedStorageOperation != storageOperation
                || result.Value?.PlannedStorageConfiguration is null))
            result = BridgeCallResult<PreparedNormalAction>.Failed(BridgeError.Create(
                BridgeErrorCodes.BridgeNotReady,
                "The Plugin did not confirm the storage UI operation and expected configuration; no token is exposed.",
                false, "Install a matching Plugin/MCP cohort, then fresh-read and prepare again."));
        return ToToolResult(result, "Device configuration plan prepared; device and station inventory state are unchanged.");
    }

    [McpServerTool(
        Name = "spherewright_commit_configure_building",
        Title = "Commit a device configuration",
        ReadOnly = false,
        Destructive = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Applies the prepared recipe, matrix-research mode, cargo-preserving sorter filter, single-warehouse storage-capacity operation, station storage slot, station output-belt selector, or maximum charge setting once through the current-version UI/business path. Warehouse operations prove every ordered grid's item/count/inc, filters/bans, identity, topology and player inventory; no items are moved or deleted by this operation. Successful storageConfigurationReadback retains the same-tick native boundary before normal delivery resumes. Later fresh inventory may differ after unbanning releases held cargo: reconcile delivery, do not require cross-tick equality or replay the action. Sorter cargo, destination, timing and topology remain unchanged by the filter assignment. Station inventory is never directly written. Poll the returned action to terminal and fresh-read delivery separately.")]
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
[Description("Uses DSP's normal click/path/inserter validators for an unlocked building already in inventory. For ordinary2011/2012 sorters, set initialSorterFilterItemId to an unlocked item BEFORE connecting a mixed source: native construction installs the filter before the first pickup, with plannedSorterFilterItemId and final sign/filter readback. Zero means unfiltered; later configuration cannot undo contamination. Exact slots are tried first; one explicitly selected belt segment and an ordinary storage/assembler/lab may use native_single_belt_segment attachment, bounded to64 path intervals without retargeting neighboring belts. Inspect plannedInserterAttachment for both slots, positions and signed input/output offsets. Native facing/tilt, span, collision and materials remain mandatory; TooSkew is not permission to relax angles or move existing buildings. For exact-slot failures, nativeChecks counts actual DSP placement checks; read lastNativeRejection before the lastPreNativeRejection of a different slot pair. Neither a passing angle nor an unrelated TooSkew proves the overall placement result. On rejection read the attachment stage, candidateChecks and bestFacingDegrees: geometry_unavailable is not an angle verdict, and candidate checks do not imply native placement checks ran. Do not repeat an unchanged pair; move closer only for explicit OutOfReach. Bound endpoints use endpointStateHash; private segment geometry and offsets are rechecked before commit, while cargo flow alone is not geometry change. Full path checks use an inactive tool-owned stage1 command container, never the player's current anchor-only stage or a temporary edit to player commands. All NEW belt points, including both ends, require more than 0.25 m centre separation from existing built/prebuild belts and other new points; source-only non-removing cover reuse supports a same-grade flat built belt with a free output, one incoming belt at most, and a free destination. Existing destination cover/merging, tier replacement, tilted/raised sources and closed paths reject. Read plannedBeltPath: full_path_stage1, sourceBindingMode, reusedSourceObjectId and newObjectCount; itemBudget/plannedPath count only NEW objects, never the retained source. Source-cover plans also require sourcePreservationMode=whole_path_native_rotation_v1: native path geometry must prove any completion-time belt rotation change. MCP withholds tokens when this echo is absent or inconsistent. Do not omit endpoint IDs to bypass occupancy or treat topology alone as permission for co-located belts. Belt NotEnoughItem / INVENTORY_INSUFFICIENT requires normal handcraft or transfer, not another site or Move: do not retry with unchanged inventory. After restocking, fresh-read and revalidate the complete path with the same explicit endpoint bindings; shortage does not prove placement or later collision checks. Prepare creates no prebuild and consumes nothing.")]
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
        int initialSorterFilterItemId = 0,
        [Description("native_grid (default) or native_geodesic: one explicit free-ground2001/2002/2003 route, 1.5–30m after native snapping. Requires all start/end coordinates and no entity/resource binding; no cover, merging or raised path. Require plannedBeltPath.routingMode to match a non-default request before commit. Geodesic belts may rotate their four sorter directions: check source, bridge and consumer facing AND native span, not endpoint coordinates alone. For grid-aligned attachments prefer native_grid with straight end segments; fresh-read actual ports and prepare the critical sorter before constructing the remaining route. Never relax native checks or replay an unchanged rejected pair.")]
        string beltPathMode = BeltPathModes.NativeGrid,
        CancellationToken cancellationToken = default)
    {
        var request = new PrepareBuildRequest
            {
                PlanetId = planetId,
                BuildingItemId = buildingItemId,
                InitialSorterFilterItemId = initialSorterFilterItemId,
                PreferredDistance = preferredDistance,
                PreferredPosition = CreateOptionalVector(preferredPositionX, preferredPositionY, preferredPositionZ),
                PreferredYaw = preferredYaw,
                ResourceNodeId = resourceNodeId > 0 || (beltPathMode == BeltPathModes.NativeGeodesic && resourceNodeId != 0) ? resourceNodeId : (int?)null,
                ExpectedResourceStateHash = string.IsNullOrWhiteSpace(expectedResourceStateHash) ? null : expectedResourceStateHash,
                SourceObjectId = sourceObjectId > 0 || (beltPathMode == BeltPathModes.NativeGeodesic && sourceObjectId != 0) ? sourceObjectId : (int?)null,
                ExpectedSourceStateHash = string.IsNullOrWhiteSpace(expectedSourceStateHash) ? null : expectedSourceStateHash,
                DestinationObjectId = destinationObjectId > 0 || (beltPathMode == BeltPathModes.NativeGeodesic && destinationObjectId != 0) ? destinationObjectId : (int?)null,
                ExpectedDestinationStateHash = string.IsNullOrWhiteSpace(expectedDestinationStateHash) ? null : expectedDestinationStateHash,
                PathEnd = CreateOptionalVector(pathEndX, pathEndY, pathEndZ),
                PathLength = pathLength,
                BeltPathMode = beltPathMode,
                ExpectedPlayerStateHash = expectedPlayerStateHash,
                StateHashVersion = stateHashVersion,
            };
        var routingError = Spherewright.Bridge.Core.Factory.BeltPathRoutingPolicy.ValidateRequest(request,
            buildingItemId >= 2001 && buildingItemId <= 2003);
        if (routingError is not null)
            return ToToolResult(BridgeCallResult<PreparedNormalAction>.Failed(BridgeError.Create(
                BridgeErrorCodes.InvalidRequest, routingError, false,
                Spherewright.Bridge.Core.Factory.BeltPathRoutingPolicy.Recovery)), "No construction plan prepared.");
        var result = await bridgeClient.PrepareBuildAsync(sessionId, request, cancellationToken).ConfigureAwait(false);
        // Mixed-cohort installs must not silently turn a requested filter into a
        // legacy unfiltered plan. Do not expose that plan's commit capability.
        if (initialSorterFilterItemId > 0 && result.Success
            && result.Value?.PlannedSorterFilterItemId != initialSorterFilterItemId)
            result = BridgeCallResult<PreparedNormalAction>.Failed(BridgeError.Create(
                BridgeErrorCodes.BridgeNotReady,
                "The installed Plugin did not confirm the requested initial sorter filter; no construction token is exposed.",
                false, "Install matching Plugin/MCP files after a normal save and shutdown, then fresh-read and prepare again. Do not commit the unconfirmed plan."));
        if (buildingItemId >= 2001 && buildingItemId <= 2003 && result.Success
            && !Spherewright.Bridge.Core.Factory.BeltSourceReusePolicy.ConfirmsPlanEcho(result.Value, buildingItemId,
                Math.Max(0, sourceObjectId), Math.Max(0, destinationObjectId)))
            result = BridgeCallResult<PreparedNormalAction>.Failed(BridgeError.Create(
                BridgeErrorCodes.BridgeNotReady,
                "The installed Plugin did not confirm full native belt validation and the exact NEW-object budget; no construction token is exposed.",
                false, "Install matching Plugin/MCP files after a normal save and shutdown, then fresh-read and prepare again. Do not commit a legacy or unconfirmed belt plan."));
        if (buildingItemId >= 2001 && buildingItemId <= 2003 && result.Success
            && !Spherewright.Bridge.Core.Factory.BeltPathRoutingPolicy.ConfirmsPlanEcho(beltPathMode, result.Value?.PlannedBeltPath))
            result = BridgeCallResult<PreparedNormalAction>.Failed(BridgeError.Create(
                BridgeErrorCodes.BridgeNotReady,
                "The installed Plugin did not confirm the requested belt routing mode; no construction token is exposed.",
                false, "Install matching Plugin/MCP files after a normal save and shutdown, then fresh-prepare. Never commit a silently substituted grid route."));
        return ToToolResult(result, "One owned-item construction plan prepared through DSP's build validator; no prebuild exists yet.");
    }

    [McpServerTool(
        Name = "spherewright_commit_build",
        Title = "Create and wait on one normal prebuild",
        ReadOnly = false,
        Destructive = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Consumes the prepared owned-item budget through the corresponding native click/path/inserter CreatePrebuilds and returns a pollable action. A prepared initial sorter filter is checked on the prebuild and completed entity. Spherewright never calls BuildFinally; normal construction drones must finish every step. Poll actionId to terminal before continuing.")]
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
        Name = "spherewright_prepare_dismantle",
        Title = "Prepare one normal resource-miner or basic-sorter dismantle",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false)]
    [Description("Re-reads one exact completed resource miner or ordinary2011/2012 sorter and the settled local player; verifies endpoint identity, native build range, idle manual build UI and conservative package capacity. Sorters require native recoverable cargo, bounded inbound-reference checks and reciprocity of every PRESENT edge. Missing ends are allowed for normal dismantle/rebuild repair; mismatched present edges, advanced stacking, prebuild endpoints and the native instant-dismantle option are rejected. Prepare removes nothing and returns no item.")]
    public static async Task<CallToolResult> PrepareDismantleAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        int objectId,
        string expectedEndpointStateHash,
        string expectedPlayerStateHash,
        int stateHashVersion = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.PrepareDismantleAsync(
            sessionId,
            new PrepareDismantleRequest
            {
                PlanetId = planetId,
                ObjectId = objectId,
                ExpectedEndpointStateHash = expectedEndpointStateHash,
                ExpectedPlayerStateHash = expectedPlayerStateHash,
                StateHashVersion = stateHashVersion,
            },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Normal supported-entity dismantle prepared with no side effect.");
    }

    [McpServerTool(
        Name = "spherewright_commit_dismantle",
        Title = "Dismantle one exact resource miner or basic sorter normally",
        ReadOnly = false,
        Destructive = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Calls DSP's normal PlayerAction_Build.DoDismantleObject once for the approved miner or ordinary2011/2012 sorter. Poll actionId to terminal; proves exact disappearance and building/cargo recovery without unexplained inventory deltas. Sorter removal also verifies cargo inc and that surviving endpoint poses/configurations/other slots are unchanged. It neither writes connections directly nor automatically rebuilds. Missing outcome evidence quarantines writes; never repeat the dismantle under a new key.")]
    public static async Task<CallToolResult> CommitDismantleAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        string planToken,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var request = CreateCommitRequest(sessionId, planetId, planToken, idempotencyKey);
        var result = await bridgeClient.CommitDismantleAsync(sessionId, request, cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Normal resource-miner dismantle completed with disappearance and recovery readback.");
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
    [Description("Moves the prepared exact count through StorageComponent's normal UI business operations and proves equal-and-opposite container deltas. Poll the action to terminal. For a successful transfer, beforeTargetAmount/afterTargetAmount are the storage counts and itemDeltas are the player's changes at completedAtGameTick. Check these same-tick deltas, then inspect later logistics separately: an active storage may already have received or sent items, so do not require cross-tick equality. A later stock difference or reporting error does not undo the terminal result; never replay an accepted transfer. Missing proof or an unexplained flow still requires reconciliation, not assumed conservation.")]
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
        Name = "spherewright_prepare_logistics_station_fleet_transfer",
        Title = "Prepare an exact logistics-station fleet transfer",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false)]
    [Description("Re-reads the player package and one exact station fleet, then verifies item type, idle/working counts, prefab capacity, range, empty player hand, proliferator safety, and exact destination capacity without moving a craft.")]
    public static async Task<CallToolResult> PrepareLogisticsStationFleetTransferAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        int stationEntityId,
        string direction,
        int itemId,
        int count,
        string expectedPlayerStateHash,
        string expectedStationFleetStateHash,
        int stateHashVersion = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.PrepareLogisticsStationFleetTransferAsync(
            sessionId,
            new PrepareLogisticsStationFleetTransferRequest
            {
                PlanetId = planetId,
                StationEntityId = stationEntityId,
                Direction = direction,
                ItemId = itemId,
                Count = count,
                ExpectedPlayerStateHash = expectedPlayerStateHash,
                ExpectedStationFleetStateHash = expectedStationFleetStateHash,
                StateHashVersion = stateHashVersion,
            },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Exact player-station fleet transfer prepared; the player package and station remain unchanged.");
    }

    [McpServerTool(
        Name = "spherewright_commit_logistics_station_fleet_transfer",
        Title = "Commit an exact logistics-station fleet transfer",
        ReadOnly = false,
        Destructive = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Moves the prepared logistics drones or vessels between the player package and a station's idle fleet using the current UI counter semantics, then proves equal-and-opposite counts and preservation of working craft, storage, energy, and unrelated inventory.")]
    public static async Task<CallToolResult> CommitLogisticsStationFleetTransferAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        string planToken,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var request = CreateCommitRequest(sessionId, planetId, planToken, idempotencyKey);
        var result = await bridgeClient.CommitLogisticsStationFleetTransferAsync(
            sessionId,
            request,
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Player-station fleet transfer completed with exact conservation readback.");
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
    [Description("Re-reads the owned-world player state and prepares a short-lived normal ground-movement order. Additive surfacePreview samples at most32m of the requested shortest surface arc using at most66 native downward rays. shoreRisk=detected warns of a possible water/shore crossing; partial/unavailable/null is not dry-ground proof. not_detected is not route clearance or guaranteed Walk: small intervening features, obstacles and the actual controller path remain unchecked. Read the agent playbook before movement: a fully observed short crossing distinguishes water in transit from an explicitly above-water terminal suffix; an aggregate warning alone does not identify the landing. This advisory evidence does not change existing hash/commit admission and never chooses or executes another target. For ordinary walking, revise risky/unknown proposals from fresh evidence before commit; after terminal arrival always fresh-read Walk, low speed and energy. It never moves or teleports the player during prepare.")]
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
    [Description("Starts the prepared movement through DSP's Player.Order path and returns a pollable action. Poll its actionId to terminal. On position_stalled or route_stalled, follow the opening-movement playbook and never retry the same target. Inspect nearby objects including belts and sorters/inserters; center distance alone is not walking clearance. It never writes player position.")]
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
        Name = "spherewright_prepare_interplanetary_flight",
        Title = "Prepare a normal same-star planet flight",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false)]
    [Description("Binds a non-gas destination in the current star, Drive Engine level 2, a nearly full core, usable normal fuel, and stable player/star-system snapshots. Prepare never launches or changes movement.")]
    public static async Task<CallToolResult> PrepareInterplanetaryFlightAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        int destinationPlanetId,
        string expectedPlayerStateHash,
        string expectedStarSystemStateHash,
        double minimumCoreEnergyRatio = 0.95d,
        int stateHashVersion = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.PrepareInterplanetaryFlightAsync(
            sessionId,
            new PrepareInterplanetaryFlightRequest
            {
                PlanetId = planetId,
                DestinationPlanetId = destinationPlanetId,
                ExpectedPlayerStateHash = expectedPlayerStateHash,
                ExpectedStarSystemStateHash = expectedStarSystemStateHash,
                MinimumCoreEnergyRatio = minimumCoreEnergyRatio,
                StateHashVersion = stateHashVersion,
            },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Same-star flight plan prepared; the player remains grounded.");
    }

    [McpServerTool(
        Name = "spherewright_commit_interplanetary_flight",
        Title = "Launch and land a prepared same-star flight",
        ReadOnly = false,
        Destructive = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Uses DSP's native flight/sail transition and sail-energy functions to launch, steer, brake, and land on the bound planet. Poll to terminal: a transient Walk tick does not complete landing or cancel an unfinished exact shore order. Stable arrival still requires 600 consecutive grounded low-speed ticks. A terminal recovery_required requires its matching checkpoint flow, not a competing Move or relabeling success. It never teleports, grants fuel, or enables sandbox fast travel.")]
    public static async Task<CallToolResult> CommitInterplanetaryFlightAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        string planToken,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var request = CreateCommitRequest(sessionId, planetId, planToken, idempotencyKey);
        var result = await bridgeClient.CommitInterplanetaryFlightAsync(sessionId, request, cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Normal same-star flight accepted; poll its actionId through landing.");
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
    [Description("Validates a runtime technology, prerequisites, level, and queue without mutation. Default enqueues normally. Explicit prioritizeQueued=true only moves an already queued non-head technology with all native prerequisites completed to the front, preserving other queue order, progress and inventory. Fresh selection hash required; paused, duplicate-level or invalid queues reject. No research progress or unlocks are granted.")]
    public static async Task<CallToolResult> PrepareSelectResearchAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        int techId,
        string expectedSelectionStateHash,
        int stateHashVersion = 1,
        bool prioritizeQueued = false,
        CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.PrepareSelectResearchAsync(
            sessionId,
            new PrepareSelectResearchRequest
            {
                PlanetId = planetId,
                TechId = techId,
                ExpectedSelectionStateHash = expectedSelectionStateHash,
                PrioritizeQueued = prioritizeQueued,
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
    [Description("Applies the exact prepared native enqueue or explicit queued-tech priority change. Priority uses native SortTechQueue, preserves all other order, verifies research progress and inventory immediately, and returns researchQueueReadback. Poll to terminal then fresh progression. No hashes, matrices or unlock flags are added.")]
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
    [Description("Returns the current or terminal state of an action accepted by this Plugin process. It does not repeat the action. Retain the terminal result before formatting later observations. Transfer beforeTargetAmount/afterTargetAmount and itemDeltas describe the synchronous completedAtGameTick boundary, not a later warehouse inventory after normal logistics. A host reporting error does not authorize replaying an accepted action.")]
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
    [Description("Lists a bounded page of assembler snapshots from the active Spherewright-owned ordinary world. It refuses unowned game sessions. This is not a list of every production device: matrix recipes use labs, not assemblers. Use spherewright_list_factory_entities with componentKind=lab for matrix production/research. Choose component families from the runtime recipe catalog; no match in one family or partial page does not prove no producer/consumer exists.")]
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
    [Description("Returns building and recipe candidates with native unlock/availability flags from the owned world's runtime prototypes. Optional fuelPowerProfile reports ordinary thermal/fusion base generation and full-load fuel energy in joules per tick plus the native fuel type mask. Null/missing is unknown, not zero. These catalog ratings are not live generation, inventory or sustainable supply and do not grant Foundry planned-generation credit. It refuses unowned sessions and never changes game state.")]
    public static async Task<CallToolResult> GetBuildCatalogAsync(
        [Description("Injected authenticated bridge client.")] IBridgeClient bridgeClient,
        [Description("Current session ID returned by spherewright_get_session_state.")] string sessionId,
        [Description("Cancellation token supplied by the MCP host.")] CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.GetBuildCatalogAsync(sessionId, cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Live build catalog with native availability flags captured from the owned ordinary world.");
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
    [Description("Consumes one unexpired plan to start a fresh peaceful 1x non-sandbox world through DSP's official new-game flow. Requires writes enabled and a UUID idempotency key. After creation, read MCP resource spherewright://agent/playbooks/opening-movement-v1 before the first gameplay action.")]
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
        Name = "spherewright_prepare_save_import",
        Title = "Prepare confirmation-gated import of the loaded save",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false)]
    [Description("Creates a no-game-side-effect plan bound to the exact currently loaded unowned session and revision. After this tool succeeds, stop and explicitly ask the user the returned confirmationPrompt—even if import was requested earlier. Do not call spherewright_commit_save_import until a subsequent user message clearly confirms. The original save remains unchanged; journal history starts at import.")]
    public static async Task<CallToolResult> PrepareUserSaveImportAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        long expectedRevision,
        CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.PrepareUserSaveImportAsync(
            sessionId,
            new PrepareUserSaveImportRequest
            {
                ExpectedRevision = expectedRevision,
            },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "A single-use owned-copy import plan was prepared with no game or save side effect. Ask the user the returned confirmationPrompt now; do not commit before their subsequent explicit confirmation.");
    }

    [McpServerTool(
        Name = "spherewright_commit_save_import",
        Title = "Clone and adopt the user-confirmed loaded save",
        ReadOnly = false,
        Destructive = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Consumes one prepared import plan only after a subsequent explicit user confirmation in the current conversation. Set all three confirmation fields true only when that later user message clearly authorizes this exact prepared import. Uses DSP's normal save API to create and verify a new internally named owned copy; it never names, enumerates, loads, overwrites, renames, or deletes the original save.")]
    public static async Task<CallToolResult> CommitUserSaveImportAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        string planToken,
        string idempotencyKey,
        [Description("True only after a subsequent user message explicitly confirms this exact prepared import in the current conversation.")] bool userConfirmedInConversation,
        [Description("Acknowledge that the original save will not be overwritten, renamed, or deleted.")] bool acknowledgeOriginalSaveRemainsUnchanged,
        [Description("Acknowledge that the journal starts at import and does not reconstruct earlier first-time events.")] bool acknowledgeJournalStartsAtImport,
        CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.CommitUserSaveImportAsync(
            sessionId,
            new CommitUserSaveImportRequest
            {
                PlanToken = planToken,
                IdempotencyKey = idempotencyKey,
                UserConfirmedInConversation = userConfirmedInConversation,
                AcknowledgeOriginalSaveRemainsUnchanged = acknowledgeOriginalSaveRemainsUnchanged,
                AcknowledgeJournalStartsAtImport = acknowledgeJournalStartsAtImport,
            },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "The explicitly confirmed current-world import reached a terminal result; the original save was not addressed or modified.");
    }

    [McpServerTool(
        Name = "spherewright_prepare_resume_owned_game",
        Title = "Prepare one-time resume of the exact owned world",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false)]
    [Description("Prepare from an idle main menu: gameLoaded=false is expected, not a load failure. Use the current restartResumeAvailable/restartResumeToken, then let prepare verify native preload/menu/no-loader readiness, ticket, source header and durable Journal checkpoint. Do not wait for gameLoaded=true before prepare. Healthy planned restarts select only the exact primary sealed in the ticket; quarantine recovery alone selects a qualifying fixed LastExit. It never enumerates saves, accepts a save name, consumes the ticket or loads anything during prepare.")]
    public static async Task<CallToolResult> PrepareOwnedWorldResumeAsync(
        IBridgeClient bridgeClient,
        string resumeToken,
        CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.PrepareOwnedWorldResumeAsync(
            new PrepareOwnedWorldResumeRequest { ResumeToken = resumeToken },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "One-time exact owned-world resume plan prepared; no save was loaded or enumerated.");
    }

    [McpServerTool(
        Name = "spherewright_commit_resume_owned_game",
        Title = "Resume the exact normally closed owned world",
        ReadOnly = false,
        Destructive = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Healthy planned restarts load only the exact ticket-bound primary through DSPGame.StartGame. Quarantine recovery alone may load the qualifying fixed LastExit. Both revalidate native menu readiness, source header/minimum tick and durable Journal checkpoint before loading; adoption must prove the embedded owned identity, planet, peaceful state and Journal continuity. Sandbox state/resource multiplier are reported, not gates. Poll the unique action to terminal, then fresh-read gameLoaded=true, owned/saved/healthy state, save tick and durable Journal. Never replay an accepted resume or choose another save to satisfy a mistaken readiness wait.")]
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
        return ToToolResult(result, "DSP accepted the exact protected owned-world resume; poll actionId for provenance validation and high-entropy resave.");
    }

    [McpServerTool(
        Name = "spherewright_prepare_reload_flight_checkpoint",
        Title = "Prepare the exact reusable pre-flight checkpoint",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false)]
    [Description("Validates only the protected checkpoint bound to the supplied high-entropy token, including its internally generated save name and exact saved game tick. It never accepts or enumerates save names and does not load during prepare.")]
    public static async Task<CallToolResult> PrepareFlightCheckpointReloadAsync(
        IBridgeClient bridgeClient,
        string reloadToken,
        CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.PrepareFlightCheckpointReloadAsync(
            new PrepareFlightCheckpointReloadRequest { ReloadToken = reloadToken },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Exact reusable pre-flight checkpoint reload prepared; no save was loaded.");
    }

    [McpServerTool(
        Name = "spherewright_commit_reload_flight_checkpoint",
        Title = "Reload the exact reusable pre-flight checkpoint",
        ReadOnly = false,
        Destructive = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Interrupts only its bound flight attempt, then loads the internally named checkpoint through DSPGame.StartGame. Adoption requires its exact saved tick, embedded primary owned-save identity, origin planet, and peaceful state; sandbox state and resource multiplier are preserved and reported but do not gate adoption, and the ticket remains reusable for another failed attempt.")]
    public static async Task<CallToolResult> CommitFlightCheckpointReloadAsync(
        IBridgeClient bridgeClient,
        string planToken,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.CommitFlightCheckpointReloadAsync(
            new CommitFlightCheckpointReloadRequest
            {
                PlanToken = planToken,
                IdempotencyKey = idempotencyKey,
            },
            cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "DSP accepted the exact reusable pre-flight checkpoint; poll actionId until provenance validation completes.");
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
