using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

public static class FactoryBufferSemantics
{
    public const string PowerGenerationRole = "power-generation-current-tick";

    public static FactoryBufferSnapshot PowerGeneration(long joulesThisTick, int currentFuelItemId, string currentFuelName) => new()
    {
        Role = PowerGenerationRole,
        CountUnit = "joules_per_tick",
        UnitsPerItem = 0, // Energy has no conversion to a count of fuel items.
        ItemId = currentFuelItemId,
        Name = currentFuelName,
        Count = (int)Math.Min(int.MaxValue, Math.Max(0L, joulesThisTick)),
    };

    // The role check also rejects legacy snapshots that mislabeled generation as items/1.
    public static bool IsItemCount(FactoryBufferSnapshot buffer) => buffer.CountUnit == "items"
        && buffer.UnitsPerItem == 1 && buffer.Role != PowerGenerationRole;
}
