using Spherewright.Bridge.Core.Progression;
using Spherewright.Contracts.Progression;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class RuntimeDependencyGraphBuilderTests
{
    [Fact]
    public void Build_DoesNotLetHydrogenCoProductHideCoalGraphiteBranch()
    {
        const int coal = 1006;
        const int crudeOil = 1007;
        const int graphite = 1109;
        const int refinedOil = 1114;
        const int hydrogen = 1120;
        const int redMatrix = 6002;
        var recipes = new[]
        {
            Recipe(1, new[] { graphite, hydrogen }, new[] { redMatrix }),
            Recipe(2, new[] { crudeOil }, new[] { refinedOil, hydrogen }),
            Recipe(3, new[] { refinedOil, hydrogen }, new[] { graphite, hydrogen }),
            Recipe(4, new[] { coal }, new[] { graphite }),
        };

        var graph = RuntimeDependencyGraphBuilder.Build(redMatrix, "Red matrix", recipes);

        Assert.Equal(new[] { 1, 2, 3, 4 }, graph.RecipeIds);
        Assert.Contains(coal, graph.ItemIds);
        Assert.Contains(graph.Edges, edge =>
            edge.FromKind == "item" && edge.FromId == coal
            && edge.ToKind == "recipe" && edge.ToId == 4);
        Assert.Contains(graph.Edges, edge =>
            edge.FromKind == "recipe" && edge.FromId == 4
            && edge.ToKind == "item" && edge.ToId == graphite);
    }

    private static RecipeCatalogEntry Recipe(int recipeId, int[] inputs, int[] outputs)
    {
        return new RecipeCatalogEntry
        {
            RecipeId = recipeId,
            Inputs = inputs.Select(itemId => new CatalogItemAmount { ItemId = itemId, Count = 1 }).ToList(),
            Outputs = outputs.Select(itemId => new CatalogItemAmount { ItemId = itemId, Count = 1 }).ToList(),
        };
    }
}
