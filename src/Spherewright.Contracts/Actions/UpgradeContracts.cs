using Spherewright.Contracts.Factory;

namespace Spherewright.Contracts.Actions;

public sealed class PrepareUpgradeRequest
{
    public int PlanetId { get; set; }
    public int ObjectId { get; set; }
    public int TargetItemId { get; set; }
    public int ExpectedRecipeId { get; set; }
    public int ExpectedFilterItemId { get; set; }
    public string ExpectedEndpointStateHash { get; set; } = string.Empty;
    public string ExpectedPlayerStateHash { get; set; } = string.Empty;
    public int StateHashVersion { get; set; } = 1;
}

// Immutable-in-practice terminal evidence copied from the immediate native-call boundary,
// not from a later poll after the upgraded machine has continued producing or carrying items.
public sealed class UpgradeReadback
{
    public long CapturedAtGameTick { get; set; }
    public int SourceObjectId { get; set; }
    public int ResultObjectId { get; set; }
    public int SourceItemId { get; set; }
    public int TargetItemId { get; set; }
    public int RecipeId { get; set; }
    public int FilterItemId { get; set; }
    public int VerifiedConnectionCount { get; set; }
    public string NativeTimingPolicy { get; set; } = string.Empty;
    public int ProgressBefore { get; set; }
    public int ProgressAfter { get; set; }
    public int ProgressRequiredBefore { get; set; }
    public int ProgressRequiredAfter { get; set; }
    public List<FactoryBufferSnapshot> BuffersBefore { get; set; } = new List<FactoryBufferSnapshot>();
    public List<FactoryBufferSnapshot> BuffersAfter { get; set; } = new List<FactoryBufferSnapshot>();
}
