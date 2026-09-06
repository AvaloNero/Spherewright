using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Spherewright.Contracts.Actions;
using Spherewright.Mcp.BridgeClient;

namespace Spherewright.Mcp.Tools;

public static partial class SpherewrightTools
{
    [McpServerTool(Name = "spherewright_prepare_upgrade", Title = "Prepare one native building upgrade",
        ReadOnly = false, Destructive = false, Idempotent = false, OpenWorld = false)]
    [Description("Prepares one manufacturing assembler (2303/2304/2305) to a higher native family grade, or one Mk.I sorter2011 to Mk.II2012 only. Fresh inspect player/entity; supply endpoint hash, recipe ID and expectedFilterItemId (0 if none). Requires unlocked target, normal build range, settled Walk, one higher-grade device and one empty refund slot. Binds session/revision/player/identity/recipe/filter/mode and reciprocal connections. Sorters require both input/output factory links, not just cached target IDs or held cargo. Assemblers reset current/extra production progress; basic sorters retain native cycle fraction, cargo and filter. No belts, advanced stacking sorters, smelters, downgrades or batch upgrade. Prepare does not mutate the game.")]
    public static async Task<CallToolResult> PrepareUpgradeAsync(IBridgeClient bridgeClient, string sessionId,
        int planetId, int objectId, int targetItemId, int expectedRecipeId, string expectedEndpointStateHash,
        string expectedPlayerStateHash, int stateHashVersion = 1, int expectedFilterItemId = 0,
        CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.PrepareUpgradeAsync(sessionId, new PrepareUpgradeRequest
        {
            PlanetId = planetId, ObjectId = objectId, TargetItemId = targetItemId,
            ExpectedRecipeId = expectedRecipeId, ExpectedEndpointStateHash = expectedEndpointStateHash,
            ExpectedFilterItemId = expectedFilterItemId,
            ExpectedPlayerStateHash = expectedPlayerStateHash, StateHashVersion = stateHashVersion,
        }, cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Native upgrade prepared; inspect costs and family-specific timing disclosure.");
    }

    [McpServerTool(Name = "spherewright_commit_upgrade", Title = "Upgrade one building through DSP",
        ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Commits the exact fresh upgrade plan via DSP DoUpgradeObject, with normal debit/refund. Returns actionId: always poll to terminal, then fresh inspect the returned entity ID (it need not equal the old ID). Immediate proof covers recipe/filter/mode, live cargo, both connection ends, family-specific timing/power and exact inventory deltas; uncertainty quarantines writes, never replay with a new key. Assembler processing progress resets; basic sorter cycle fraction is retained. Upgrade does not guarantee upstream, power/logistics capacity or increased measured throughput. No automatic expansion loop.")]
    public static async Task<CallToolResult> CommitUpgradeAsync(IBridgeClient bridgeClient, string sessionId,
        int planetId, string planToken, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var result = await bridgeClient.CommitUpgradeAsync(sessionId,
            CreateCommitRequest(sessionId, planetId, planToken, idempotencyKey), cancellationToken).ConfigureAwait(false);
        return ToToolResult(result, "Upgrade commit response received; poll actionId and fresh read the proven result entity.");
    }
}
