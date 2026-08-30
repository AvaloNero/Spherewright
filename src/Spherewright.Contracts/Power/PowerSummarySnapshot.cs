namespace Spherewright.Contracts.Power;

public sealed class PowerSummarySnapshot
{
    public string SessionId { get; set; } = string.Empty;

    public int PlanetId { get; set; }

    public long CapturedAtGameTick { get; set; }

    public long TotalEnergyRequired { get; set; }

    public long TotalEnergyServed { get; set; }

    public long TotalEnergyCapacity { get; set; }

    public long TotalEnergyGenerated { get; set; }

    public List<PowerNetworkSnapshot> Networks { get; set; } = new List<PowerNetworkSnapshot>();
}

public sealed class PowerNetworkSnapshot
{
    public int NetworkId { get; set; }

    public int NodeCount { get; set; }

    public int ConsumerCount { get; set; }

    public int GeneratorCount { get; set; }

    public int AccumulatorCount { get; set; }

    public int ExchangerCount { get; set; }

    public long EnergyRequired { get; set; }

    public long EnergyServed { get; set; }

    public long EnergyCapacity { get; set; }

    public long EnergyGenerated { get; set; }

    public long EnergyStored { get; set; }

    public double ConsumerRatio { get; set; }

    public double GeneratorRatio { get; set; }
}
