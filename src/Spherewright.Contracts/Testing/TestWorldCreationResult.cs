namespace Spherewright.Contracts.Testing;

public sealed class TestWorldCreationResult
{
    public string ActionId { get; set; } = string.Empty;

    public bool Accepted { get; set; }

    public bool IdempotentReplay { get; set; }

    public string SaveName { get; set; } = string.Empty;

    public int GalaxySeed { get; set; }

    public int StarCount { get; set; }

    public string State { get; set; } = string.Empty;
}
