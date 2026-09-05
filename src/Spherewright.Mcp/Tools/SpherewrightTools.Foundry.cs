using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Spherewright.Contracts.Factory;
using Spherewright.Mcp.BridgeClient;

namespace Spherewright.Mcp.Tools;

public static partial class SpherewrightTools
{
    [McpServerTool(Name = "spherewright_get_foundry_plan", Title = "Calculate a Foundry material and machine plan",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Compiles a bounded target item/rate using current recipes and machine speeds. Optionally provide a site for a bounded 32-machine spherical grid, native grid snapping/build-condition checks, planned-machine clearance and full machine inventory budget. Read each native condition and overlap. Both phases are non-executable: exact logistics/power, ongoing supply and restartable action graphs remain separate. A clear machine preview and either hash are not permission to build: fresh inspect/prepare/commit/terminal/readback is still required per entity. No game entities, write tokens or persistent plans are created.")]
    public static async Task<CallToolResult> GetFoundryPlanAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        [Description("Unlocked current runtime target item ID.")] int targetItemId,
        [Description("Target output items per game minute, greater than zero and no greater than 1000000.")] decimal targetRatePerMinute,
        [Description("Optional explicit externally supplied items; raw runtime items become supply boundaries automatically.")] int[]? externalSupplyItemIds = null,
        [Description("Optional exact item/recipe/building choices. Zero buildingItemId selects the lowest available grade for that recipe.")] FoundryRecipeChoice[]? recipeChoices = null,
        [Description("Optional explicit local surface origin {x,y,z}, yawDegrees [0,360), columns 1–8, columnSpacing/rowSpacing 4–32 m. Defaults: yaw 0, columns 4, spacings 12. At most 32 machines and 64 m tangent offset. Returns site_preview, never executable construction.")] FoundrySiteRequest? site = null,
        CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.GetFoundryPlanAsync(sessionId, new GetFoundryPlanRequest
        {
            PlanetId = planetId, TargetItemId = targetItemId, TargetRatePerMinute = targetRatePerMinute,
            ExternalSupplyItemIds = externalSupplyItemIds?.ToList() ?? new List<int>(),
            RecipeChoices = recipeChoices?.ToList() ?? new List<FoundryRecipeChoice>(),
            Site = site,
        }, cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Foundry draft calculated; read phase, site conditions and remaining checks before any construction.");
    }
}
