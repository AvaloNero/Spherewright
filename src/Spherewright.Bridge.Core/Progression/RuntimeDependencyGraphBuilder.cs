using Spherewright.Contracts.Progression;

namespace Spherewright.Bridge.Core.Progression;

public static class RuntimeDependencyGraphBuilder
{
    public static RuntimeDependencyGraph Build(
        int targetItemId,
        string targetItemName,
        IReadOnlyList<RecipeCatalogEntry> recipes)
    {
        var graph = new RuntimeDependencyGraph
        {
            TargetItemId = targetItemId,
            TargetItemName = targetItemName ?? string.Empty,
        };
        if (targetItemId <= 0)
        {
            return graph;
        }

        var producers = new Dictionary<int, List<RecipeCatalogEntry>>();
        foreach (var recipe in recipes)
        {
            foreach (var output in recipe.Outputs)
            {
                if (!producers.TryGetValue(output.ItemId, out var entries))
                {
                    entries = new List<RecipeCatalogEntry>();
                    producers.Add(output.ItemId, entries);
                }

                entries.Add(recipe);
            }
        }

        var pendingItems = new Stack<int>();
        var visitedItems = new HashSet<int>();
        var visitedRecipes = new HashSet<int>();
        pendingItems.Push(targetItemId);
        while (pendingItems.Count > 0)
        {
            var itemId = pendingItems.Pop();
            if (!visitedItems.Add(itemId) || !producers.TryGetValue(itemId, out var itemProducers))
            {
                continue;
            }

            foreach (var recipe in itemProducers.OrderBy(entry => entry.RecipeId))
            {
                if (!visitedRecipes.Add(recipe.RecipeId))
                {
                    continue;
                }

                foreach (var input in recipe.Inputs)
                {
                    graph.Edges.Add(new RuntimeDependencyEdge
                    {
                        FromKind = "item",
                        FromId = input.ItemId,
                        ToKind = "recipe",
                        ToId = recipe.RecipeId,
                    });
                    pendingItems.Push(input.ItemId);
                }

                foreach (var output in recipe.Outputs)
                {
                    graph.Edges.Add(new RuntimeDependencyEdge
                    {
                        FromKind = "recipe",
                        FromId = recipe.RecipeId,
                        ToKind = "item",
                        ToId = output.ItemId,
                    });
                }
            }
        }

        graph.ItemIds = graph.Edges
            .SelectMany(edge => new[]
            {
                edge.FromKind == "item" ? edge.FromId : 0,
                edge.ToKind == "item" ? edge.ToId : 0,
            })
            .Where(id => id > 0)
            .Append(targetItemId)
            .Distinct()
            .OrderBy(id => id)
            .ToList();
        graph.RecipeIds = visitedRecipes.OrderBy(id => id).ToList();
        graph.Edges = graph.Edges
            .OrderBy(edge => edge.FromKind, StringComparer.Ordinal)
            .ThenBy(edge => edge.FromId)
            .ThenBy(edge => edge.ToKind, StringComparer.Ordinal)
            .ThenBy(edge => edge.ToId)
            .ToList();
        return graph;
    }
}
