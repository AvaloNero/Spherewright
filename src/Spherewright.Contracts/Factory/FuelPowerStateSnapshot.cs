namespace Spherewright.Contracts.Factory;

public sealed class FuelPowerStateSnapshot
{
    public string State { get; set; } = "unavailable";
    public string? ReasonCode { get; set; }
    public int? BufferedFuelItemId { get; set; }
    public int? BufferedFuelCount { get; set; }
    // Total inc on buffered items, not the level of the loaded fuel energy.
    public int? BufferedFuelInc { get; set; }
    public long? BufferedFuelHeatPerItemJoules { get; set; }
    public int? LoadedFuelItemId { get; set; }
    public long? LoadedFuelEnergyJoules { get; set; }
    public int? LoadedFuelIncLevel { get; set; }
    public bool? LoadedFuelProductive { get; set; }
    public long? RatedGenerationEnergyPerTick { get; set; }
    public long? RatedFuelEnergyUsePerTick { get; set; }
    public long? CapacityEnergyPerTick { get; set; }
    public long? GeneratedEnergyPerTick { get; set; }
}
