using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

/// <summary>
/// Bounds and validates a COPY of the current native ten-cell cargo representation.
/// The adapter still checks each reference with CargoPath.GetCargoAtIndex. This does
/// not scan a whole path, traverse neighbours, or prove preservation during upgrades.
/// </summary>
public static class BeltCargoObservationPolicy
{
    public const int MaximumSegmentCells = 512;
    public const int CargoCellLength = 10;
    public const int MaximumWindowCells = MaximumSegmentCells + 2 * (CargoCellLength - 1);
    public const int MaximumCargoStacks = 64;

    public static bool TryGetWindow(int pathLength, int segmentStart, int segmentLength,
        out int windowStart, out int windowLength, out string? reason)
    {
        windowStart = windowLength = 0;
        reason = null;
        if (pathLength < 1 || segmentStart < 0 || segmentLength < 1
            || segmentLength > MaximumSegmentCells || (long)segmentStart + segmentLength > pathLength)
        {
            reason = "belt_segment_outside_observation_bound";
            return false;
        }
        windowStart = Math.Max(0, segmentStart - (CargoCellLength - 1));
        var end = Math.Min((long)pathLength, (long)segmentStart + segmentLength + CargoCellLength - 1);
        windowLength = (int)(end - windowStart);
        return true;
    }

    public static bool TryLocateCargo(byte[]? window, int windowStart, int pathLength,
        int segmentStart, int segmentLength, bool closed,
        out List<BeltCargoReference> references, out string? reason)
    {
        references = new List<BeltCargoReference>();
        if (!TryGetWindow(pathLength, segmentStart, segmentLength,
                out var expectedStart, out var expectedLength, out reason)) return false;
        if (window is null || windowStart != expectedStart || window.Length != expectedLength)
        {
            reason = "cargo_window_size_invalid";
            return false;
        }

        var seen = new Dictionary<int, int>();
        var found = new List<BeltCargoReference>();
        var first = segmentStart - windowStart;
        // A corrupt zero inside a touching stack must not look like an empty one-cell
        // segment. Guard framing can establish that the zero belongs to that stack.
        // Ignore complete stacks wholly outside the selected segment.
        for (var guard = 0; guard < window.Length; guard++)
        {
            if (!IsFramingByte(window[guard])) continue;
            var sign = guard - (window[guard] - 250);
            var overlapStart = Math.Max(first, sign - 4);
            var overlapEnd = Math.Min(first + segmentLength, sign + 6);
            for (var cell = overlapStart; cell < overlapEnd; cell++)
                if (window[cell] == 0)
                {
                    reason = "cargo_marker_invalid";
                    return false;
                }
        }
        for (var cell = first; cell < first + segmentLength; cell++)
        {
            var value = window[cell];
            if (value == 0) continue;
            var sign = -1;
            if (IsFramingByte(value))
                sign = cell - (value - 250);
            else if (value >= 1 && value <= 100)
            {
                for (var candidate = cell - 1; candidate >= Math.Max(0, cell - 4); candidate--)
                    if (window[candidate] == 250) { sign = candidate; break; }
            }
            else
            {
                reason = "cargo_marker_invalid";
                return false;
            }

            // Require the COMPLETE touching stack, including the guard bytes. Do not
            // guess closed-path seam fragments or turn an unresolved marker into empty.
            if (sign < 4 || sign + 5 >= window.Length)
            {
                reason = closed ? "closed_path_seam_not_observed" : "cargo_marker_outside_window";
                return false;
            }
            for (var offset = -4; offset <= 0; offset++)
                if (window[sign + offset] != 250 + offset)
                {
                    reason = "cargo_marker_invalid";
                    return false;
                }
            if (window[sign + 5] != 255)
            {
                reason = "cargo_marker_invalid";
                return false;
            }
            var cargoId = 0;
            var multiplier = 1;
            for (var offset = 1; offset <= 4; offset++, multiplier *= 100)
            {
                var digit = window[sign + offset];
                if (digit < 1 || digit > 100)
                {
                    reason = "cargo_reference_digits_invalid";
                    return false;
                }
                cargoId += (digit - 1) * multiplier;
            }
            // Unlike entity/component pools, native CargoContainer allocates ID ZERO.
            if (seen.TryGetValue(cargoId, out var previousSign))
            {
                if (previousSign != sign)
                {
                    reason = "ambiguous_cargo_reference";
                    return false;
                }
                continue;
            }
            if (found.Count >= MaximumCargoStacks)
            {
                reason = "cargo_stack_observation_bound_exceeded";
                return false;
            }
            seen.Add(cargoId, sign);
            found.Add(new BeltCargoReference(cargoId, windowStart + cell));
        }
        references = found;
        return true;
    }

    private static bool IsFramingByte(byte value) => (value >= 246 && value <= 250) || value == 255;

    public static bool TrySummarize(IReadOnlyList<BeltCargoReference>? references,
        IReadOnlyList<BeltCargoSample>? samples, out List<BeltCargoItemSnapshot> items, out string? reason)
    {
        items = new List<BeltCargoItemSnapshot>();
        reason = "cargo_readback_incomplete";
        if (references is null || samples is null || references.Count > MaximumCargoStacks
            || samples.Count != references.Count) return false;
        var wanted = new HashSet<int>();
        foreach (var reference in references)
            if (reference is null || reference.CargoId < 0 || !wanted.Add(reference.CargoId)) return false;
        var totals = new SortedDictionary<int, BeltCargoItemSnapshot>();
        foreach (var sample in samples)
        {
            if (sample is null || !wanted.Remove(sample.CargoId)
                || sample.ItemId < 1 || sample.ItemId > short.MaxValue
                || sample.StackCount < 1 || sample.StackCount > byte.MaxValue
                || sample.Inc < 0 || sample.Inc > byte.MaxValue) return false;
            if (!totals.TryGetValue(sample.ItemId, out var item))
            {
                item = new BeltCargoItemSnapshot { ItemId = sample.ItemId };
                totals.Add(sample.ItemId, item);
            }
            item.CargoStackCount++;
            item.Count += sample.StackCount;
            item.Inc += sample.Inc;
        }
        if (wanted.Count != 0) return false;
        items = totals.Values.ToList();
        reason = null;
        return true;
    }
}

public sealed class BeltCargoReference
{
    public BeltCargoReference(int cargoId, int observedPathCell)
    { CargoId = cargoId; ObservedPathCell = observedPathCell; }

    public int CargoId { get; }
    public int ObservedPathCell { get; }
}

public sealed class BeltCargoSample
{
    public int CargoId { get; set; }
    public int ItemId { get; set; }
    public int StackCount { get; set; }
    public int Inc { get; set; }
}
