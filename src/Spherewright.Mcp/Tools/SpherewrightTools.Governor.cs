using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Spherewright.Contracts.Factory;
using Spherewright.Mcp.BridgeClient;

namespace Spherewright.Mcp.Tools;

public static partial class SpherewrightTools
{
    [McpServerTool(Name = "spherewright_get_governor_plan", Title = "Compare measured expansion and balancing alternatives",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Reads current owned local-planet Overseer counters and1..32 explicitly selected fresh entities, reusing Foundry recipes/scale to compare in-place upgrade, additional production and supported module copy. Returns non-executable proposal, machine costs/refunds/power and theoretical potential, never promised actual gain. Three independent600-tick nonzero stable windows are needed for baseline ready; call again after enough GAME ticks, no busy polling. Candidate windows reset with changed static source settings/topology/network/session; normal cargo/processing progress is a separate observation. A server-locked declaration is persisted separately (declarationDurable); only a covering protected planned resume restores it, with all continuous observation credit reset to zero. Rates are planet-item counters, not per-entity; all direct target producers must be selected for attribution. Supply reports target demand, actual production/consumption, allocatable surplus, lower-bound deficit, and separate selected-buffer delta (not production minus consumption). Actual consumption under starvation is not full demand. Prespecify target, tolerance and at least36000 validation ticks; store the pre-execution proposal, then use existing fresh prepare/commit paths. balanced remains false until separate sustained live evidence; this tool does not run an expansion loop or claim10-minute acceptance.")]
    public static async Task<CallToolResult> GetGovernorPlanAsync(IBridgeClient bridgeClient, string sessionId, int planetId,
        int targetItemId, decimal targetRatePerMinute, BlueprintSelectedEntity[] sourceEntities,
        decimal toleranceFraction = .1m, int validationGameTicks = 36000, int[]? externalSupplyItemIds = null,
        FoundryRecipeChoice[]? recipeChoices = null,
        [Description("Exact ready proposalHash from this session, or the same server-persisted lock after protected planned resume. First pass it BEFORE any expansion/write (same revision/source, within3600 game ticks), and require throughputValidation.declarationDurable=true before expanding. Retain the original hash with fresh expanded-source entities. At most8 declarations per owned identity; covering planned restart and Journal continuity are required, no caller-supplied historical rates. Restart resets observation credit to zero; durable=false still describes the non-durable samples, not the declaration. Health is sampled, not complete balance certification.")] string? validationBaselineProposalHash = null,
        [Description("Optional one explicit finite blueprint/site/free-boundary-port layout for the additional parallel chain. Once the measured baseline is ready, reuse Foundry to budget target-minus-baseline, ALL module objects, native placement, full-base-load power and rated transport. parallelExpansion includes the exact intent and existing construction hash for fresh prepare_blueprint_build, never a new executor or write token. External infrastructure outside the explicit layout is excluded. Omit during post-expansion observation; never submit replacement historical rates.")] FoundryBlueprintRequest? parallelExpansionBlueprint = null,
        CancellationToken cancellationToken = default) =>
        ToToolResult(await bridgeClient.GetGovernorPlanAsync(sessionId, new GetGovernorPlanRequest {
            PlanetId = planetId, TargetItemId = targetItemId, TargetRatePerMinute = targetRatePerMinute,
            SourceEntities = sourceEntities?.ToList()!, ToleranceFraction = toleranceFraction, ValidationGameTicks = validationGameTicks,
            ExternalSupplyItemIds = externalSupplyItemIds?.ToList() ?? new List<int>(),
            RecipeChoices = recipeChoices?.ToList() ?? new List<FoundryRecipeChoice>(),
            ValidationBaselineProposalHash = validationBaselineProposalHash,
            ParallelExpansionBlueprint = parallelExpansionBlueprint,
        }, cancellationToken).ConfigureAwait(false), "Compare conditional costs and measured gaps; retain the declared proposal before any chosen execution.");
}
