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
    [Description("Compiles a bounded target item/rate into a deterministic dependency-ordered material draft using current unlocked recipes and building speeds. Returns shared-input demand, machine counts, base machine power and explicit external supplies/byproducts. The draft is not executable: terrain, footprints, exact belt/sorter/storage costs, power connections and step-resume binding still need validation. It creates no game entities, write tokens or persistent plans.")]
    public static async Task<CallToolResult> GetFoundryPlanAsync(
        IBridgeClient bridgeClient,
        string sessionId,
        int planetId,
        [Description("Unlocked current runtime target item ID.")] int targetItemId,
        [Description("Target output items per game minute, greater than zero and no greater than 1000000.")] decimal targetRatePerMinute,
        [Description("Optional explicit externally supplied items; raw runtime items become supply boundaries automatically.")] int[]? externalSupplyItemIds = null,
        [Description("Optional exact item/recipe/building choices. Zero buildingItemId selects the lowest available grade for that recipe.")] FoundryRecipeChoice[]? recipeChoices = null,
        CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.GetFoundryPlanAsync(sessionId, new GetFoundryPlanRequest
        {
            PlanetId = planetId, TargetItemId = targetItemId, TargetRatePerMinute = targetRatePerMinute,
            ExternalSupplyItemIds = externalSupplyItemIds?.ToList() ?? new List<int>(),
            RecipeChoices = recipeChoices?.ToList() ?? new List<FoundryRecipeChoice>(),
        }, cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Foundry material draft calculated; read remaining checks before any construction.");
    }
}
