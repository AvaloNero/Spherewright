namespace Spherewright.Bridge.Core.Diagnostics;

// Called synchronously by the Unity-main-thread adapter. No native arrays are
// retained or returned. Level1 is 600 completed six-tick buckets, NOT lifetime.
public static class NativeMinuteProductionCounters
{
    public static (long Produced, long Consumed) Read(int expectedItemId, int itemId,
        int[]? count, int[]? cursor, long[]? total)
    {
        if (expectedItemId <= 0 || itemId != expectedItemId || count is null || count.Length != 7200
            || cursor is null || cursor.Length != 12 || total is null || total.Length != 14
            || cursor[1] < 600 || cursor[1] >= 1200 || cursor[7] < 4200 || cursor[7] >= 4800
            || total[1] < 0 || total[8] < 0)
            throw new ArgumentException("The native level1 product identity, arrays or cursors are inconsistent.");
        long produced = 0, consumed = 0;
        for (var index = 0; index < 600; index++)
        {
            var p = count[600 + index]; var c = count[4200 + index];
            if (p < 0 || c < 0) throw new ArgumentException("Negative native production bucket.");
            produced += p; consumed += c;
        }
        if (produced != total[1] || consumed != total[8])
            throw new ArgumentException("The native level1 totals disagree with their bounded bucket rings.");
        return (produced, consumed);
    }
}
