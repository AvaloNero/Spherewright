using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Spherewright.Contracts.Factory;
using Spherewright.Contracts.Errors;
using Spherewright.Mcp.BridgeClient;

namespace Spherewright.Mcp.Tools;

public static partial class SpherewrightTools
{
    [McpServerTool(Name = "spherewright_get_governor_plan", Title = "Compare measured expansion and balancing alternatives",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Reads current owned local-planet Overseer counters and1..32 explicitly selected fresh entities, reusing Foundry recipes/scale to compare in-place upgrade, additional production and supported module copy. Returns non-executable proposal, machine costs/refunds/power and theoretical potential, never promised actual gain. Three independent nonzero stable windows of the predeclared measurementGameTicks(600/default or3600) are needed for baseline ready; call again after enough GAME ticks, no busy polling. Measurement-period changes discard candidate history, and a locked measurement period cannot change. Candidate windows reset with changed static source settings/topology/network/session or observed item scope; normal cargo/processing progress is a separate observation. Before locking, repricing only the proposed target rate preserves unchanged-source measurement history, but produces a NEW proposalHash and full scale/cost assessment. For2x, obtain a ready measured baseline, request twice that rate with the same fresh source, recheck all evidence, then lock the NEW ready proposal BEFORE any expansion/write. An unstable baseline stays unstable; never relax tolerance or select favorable windows by repeated retries. An already locked target/tolerance/scale cannot be changed. A server-locked declaration is persisted separately (declarationDurable); only a covering protected planned resume restores it, with all continuous observation credit reset to zero. Rates are planet-item counters, not per-entity; all direct target producers must be selected for attribution. Supply reports target demand, actual production/consumption, allocatable surplus, lower-bound deficit, and separate selected-buffer delta (not production minus consumption). Actual consumption under starvation is not full demand. Prespecify target, tolerance and at least36000 validation ticks; store the pre-execution proposal, then use existing fresh prepare/commit paths. balanced remains false until separate sustained live evidence; this tool does not run an expansion loop or claim10-minute acceptance.")]
    public static async Task<CallToolResult> GetGovernorPlanAsync(IBridgeClient bridgeClient, string sessionId, int planetId,
        int targetItemId, decimal targetRatePerMinute, BlueprintSelectedEntity[] sourceEntities,
        decimal toleranceFraction = .1m, int validationGameTicks = 36000, int[]? externalSupplyItemIds = null,
        FoundryRecipeChoice[]? recipeChoices = null,
        [Description("Exact ready proposalHash from this session, or the same server-persisted lock after protected planned resume. First pass it BEFORE any expansion/write (same revision/source, within3600 game ticks), and require throughputValidation.declarationDurable=true before expanding. Retain the original hash with fresh expanded-source entities. At most8 declarations per owned identity; covering planned restart and Journal continuity are required, no caller-supplied historical rates. Restart resets observation credit to zero; durable=false still describes the non-durable samples, not the declaration. Health is sampled, not complete balance certification.")] string? validationBaselineProposalHash = null,
        [Description("Optional one explicit finite blueprint/site/free-boundary-port layout for the additional parallel chain. Once the measured baseline is ready, reuse Foundry to budget target-minus-baseline, ALL module objects, native placement, full-base-load power and rated transport. parallelExpansion includes the exact intent and existing construction hash for fresh prepare_blueprint_build, never a new executor or write token. External infrastructure outside the explicit layout is excluded. Omit during post-expansion observation; never submit replacement historical rates.")] FoundryBlueprintRequest? parallelExpansionBlueprint = null,
        [Description("Fix the native automatic-production window BEFORE collecting baseline:600(default,10 game seconds) or3600(60 game seconds, six-tick-aligned level1). Low-rate batch quantization may require the latter, but never change a locked experiment or reinterpret prior failed samples. Require the Plugin to echo this value. Target/tolerance/at least36000 validation ticks remain unchanged; health diagnostics stay on the original sampled600-tick basis.")] int measurementGameTicks = 600,
        CancellationToken cancellationToken = default)
    {
        if (measurementGameTicks != 600 && measurementGameTicks != 3600)
            return ToToolResult(BridgeCallResult<GovernorPlanSnapshot>.Failed(BridgeError.Create(
                BridgeErrorCodes.InvalidRequest, "Only600 or3600 measurementGameTicks is supported.", false,
                "Choose a fixed native measurement window before collecting a baseline.")), "No declaration was requested.");
        var result = await bridgeClient.GetGovernorPlanAsync(sessionId, new GetGovernorPlanRequest {
            PlanetId = planetId, TargetItemId = targetItemId, TargetRatePerMinute = targetRatePerMinute,
            SourceEntities = sourceEntities?.ToList()!, ToleranceFraction = toleranceFraction, ValidationGameTicks = validationGameTicks,
            ExternalSupplyItemIds = externalSupplyItemIds?.ToList() ?? new List<int>(),
            RecipeChoices = recipeChoices?.ToList() ?? new List<FoundryRecipeChoice>(),
            ValidationBaselineProposalHash = validationBaselineProposalHash,
            ParallelExpansionBlueprint = parallelExpansionBlueprint,
            MeasurementGameTicks = measurementGameTicks,
        }, cancellationToken).ConfigureAwait(false);
        if (result.Success && (result.Value is null || result.Value.MeasurementGameTicks != measurementGameTicks
            || (result.Value.ThroughputValidation is { } validation && validation.MeasurementGameTicks != measurementGameTicks)))
            result = BridgeCallResult<GovernorPlanSnapshot>.Failed(BridgeError.Create(BridgeErrorCodes.BridgeNotReady,
                "The installed Plugin did not confirm the requested Governor measurement window.", false,
                "Use a matching Plugin/MCP cohort. Do not reinterpret this response or expand from it."));
        return ToToolResult(result, "Compare conditional costs and measured gaps; retain the declared proposal before any chosen execution.");
    }
}
