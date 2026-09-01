namespace Spherewright.Bridge.Core.Safety;

public static class BuildConnectionSlots
{
    public static IReadOnlyList<int> SelectAvailable(
        int slotCount,
        IEnumerable<int> occupiedSlots)
    {
        if (slotCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slotCount));
        }

        if (occupiedSlots is null)
        {
            throw new ArgumentNullException(nameof(occupiedSlots));
        }

        var occupied = new HashSet<int>(occupiedSlots.Where(slot => slot >= 0 && slot < slotCount));
        var available = new List<int>(Math.Max(0, slotCount - occupied.Count));
        for (var slot = 0; slot < slotCount; slot++)
        {
            if (!occupied.Contains(slot))
            {
                available.Add(slot);
            }
        }

        return available;
    }

    public static IReadOnlyList<int> SelectVerificationCandidates(
        int preparedSlot,
        int connectionSlotCount)
    {
        if (connectionSlotCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(connectionSlotCount));
        }

        if (preparedSlot >= 0)
        {
            return preparedSlot < connectionSlotCount
                ? new[] { preparedSlot }
                : Array.Empty<int>();
        }

        return Enumerable.Range(0, connectionSlotCount).ToArray();
    }
}
