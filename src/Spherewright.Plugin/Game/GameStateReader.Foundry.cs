using Spherewright.Bridge.Core.Factory;
using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Factory;
using Spherewright.Contracts.Sessions;

namespace Spherewright.Plugin.Game;

internal sealed partial class GameStateReader
{
    public GameCallResult<FoundryPlanSnapshot> GetFoundryPlanOnMainThread(
        string? requestedSessionId, GetFoundryPlanRequest request)
    {
        var recipes = GetRecipeCatalogOnMainThread(requestedSessionId, new LocalPlanetRequest { PlanetId = request.PlanetId });
        if (!recipes.Success || recipes.Value is null) return GameCallResult<FoundryPlanSnapshot>.Failed(recipes.Error!);
        var catalog = GetBuildCatalogOnMainThread(requestedSessionId);
        if (!catalog.Success || catalog.Value is null) return GameCallResult<FoundryPlanSnapshot>.Failed(catalog.Error!);
        try
        {
            return GameCallResult<FoundryPlanSnapshot>.Succeeded(FoundryPlanCompiler.Compile(request, recipes.Value, catalog.Value.Buildings));
        }
        catch (FoundryPlanningException error)
        {
            return GameCallResult<FoundryPlanSnapshot>.Failed(BridgeError.Create(
                BridgeErrorCodes.InvalidRequest, $"Foundry {error.Reason}: {error.Message}", false,
                "Read the current recipe/build catalogs and supply an available recipe, explicit external input, or bounded target rate."));
        }
    }
}
