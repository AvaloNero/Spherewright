using Spherewright.Contracts.Factory;

namespace Spherewright.Contracts.Actions;

public sealed class MoveStateBindingSnapshot
{
    public string Mode { get; set; } = "bounded_passive_drift";
    public string InspectionStateHash { get; set; } = string.Empty;
    public long InspectionCapturedAtGameTick { get; set; }
    public Vector3Snapshot OriginPosition { get; set; } = new Vector3Snapshot();
    public double MaximumDisplacementMetres { get; set; }
    public double MaximumSpeedMetresPerSecond { get; set; }
    public double MaximumAgeSeconds { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
}
