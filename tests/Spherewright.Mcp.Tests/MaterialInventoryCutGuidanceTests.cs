using System.ComponentModel;
using System.Reflection;
using Spherewright.Contracts.Factory;
using Spherewright.Mcp.BridgeClient;
using Spherewright.Mcp.Resources;
using Spherewright.Mcp.Tools;
using Xunit;

namespace Spherewright.Mcp.Tests;

public sealed class MaterialInventoryCutGuidanceTests
{
    [Fact]
    public async Task ExistingReadForwardsExplicitCutOnceAndKeepsDefaultEmpty()
    {
        var client = DispatchProxy.Create<IBridgeClient, InventoryBridgeProxy>();
        var proxy = (InventoryBridgeProxy)client;
        var ids = new[] { 5329, 5941 };
        await SpherewrightTools.InspectFactoryEntityAsync(client, "test-session", 104, 5329, ids);
        Assert.Equal(1, proxy.Calls);
        Assert.Equal(ids, proxy.Request!.MaterialInventoryObjectIds);
        Assert.Equal(104, proxy.Request.PlanetId);
        Assert.Equal(5329, proxy.Request.ObjectId);
        ids[0] = 1;
        Assert.Equal(5329, proxy.Request.MaterialInventoryObjectIds[0]);
        await SpherewrightTools.InspectFactoryEntityAsync(client, "test-session", 104, 5329);
        Assert.Equal(2, proxy.Calls);
        Assert.Empty(proxy.Request.MaterialInventoryObjectIds);
    }

    [Fact]
    public void SingleEntityReadDefaultsToNoAdditionalInventoryWork()
    {
        Assert.Empty(new InspectFactoryEntityRequest().MaterialInventoryObjectIds);
        Assert.Null(new FactoryEntitySnapshot().MaterialInventoryCut);
        var parameter = typeof(SpherewrightTools).GetMethod(nameof(SpherewrightTools.InspectFactoryEntityAsync))!
            .GetParameters().Single(item => item.Name == "materialInventoryObjectIds");
        Assert.True(parameter.HasDefaultValue);
        Assert.Null(parameter.DefaultValue);
        var description = parameter.GetCustomAttribute<DescriptionAttribute>()!.Description;
        Assert.Contains("same game tick", description);
        Assert.Contains("unknown, not zero", description);
        Assert.Contains("outside the selection", description);
        Assert.Contains("not production, flow, source allocation", description);
    }

    [Fact]
    public void EmbeddedGuideDisclosesCoverageAndDoesNotClaimFlow()
    {
        var guide = AgentPlaybookResources.GetOpeningMovementPlaybook().Text;
        Assert.Contains("materialInventoryObjectIds", guide);
        Assert.Contains("Each path occurs once", guide);
        Assert.Contains("do not prorate", guide);
        Assert.Contains("32768 total cells", guide);
        Assert.Contains("missing/unavailable", guide);
        Assert.Contains("A cut does not prove flow", guide);
        Assert.Contains("Do not stitch different-tick cuts", guide);
        Assert.Contains("supports only belt, storage, tank, inserter, assembler and miner", guide);
        Assert.Contains("`power-node` and `power-generator` are unsupported", guide);
        Assert.Contains("generation in J/t", guide);
        Assert.Contains("Native `energyCapacity` is a dynamic observation, not network membership", guide);
        Assert.Contains("approved capacity floor and served ratio", guide);
        Assert.Contains("Neither value proves fuel stock, burn or continuity", guide);
        Assert.Contains("does not establish its cause", guide);
    }

    public class InventoryBridgeProxy : DispatchProxy
    {
        public int Calls { get; private set; }
        public InspectFactoryEntityRequest? Request { get; private set; }
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method?.Name != nameof(IBridgeClient.InspectFactoryEntityAsync))
                throw new InvalidOperationException("Unexpected read; no other Bridge call is permitted by this fixture.");
            Calls++;
            Request = (InspectFactoryEntityRequest)args![1]!;
            return Task.FromResult(BridgeCallResult<FactoryEntitySnapshot>.Succeeded(
                new FactoryEntitySnapshot { SessionId = "test-session", PlanetId = Request.PlanetId, ObjectId = Request.ObjectId }));
        }
    }
}
