using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

/// <summary>Reads the already bounded COPY, never picks/removes native cargo.</summary>
public static class BeltRearPickupObservationPolicy
{
    public static BeltRearPickupSnapshot Capture(byte[]? window, int windowStart, int pathLength,
        int segmentStart, int segmentLength, bool closed, IReadOnlyList<BeltCargoSample>? samples,
        long capturedAtGameTick)
    {
        var result = new BeltRearPickupSnapshot { CapturedAtGameTick = capturedAtGameTick };
        if (!BeltCargoObservationPolicy.TryGetWindow(pathLength, segmentStart, segmentLength,
                out var expectedStart, out var expectedLength, out var reason))
        { result.ReasonCode = reason; return result; }
        if (window is null || windowStart != expectedStart || window.Length != expectedLength)
        { result.ReasonCode = "cargo_window_size_invalid"; return result; }
        if (closed || (long)segmentStart + segmentLength != pathLength)
        {
            result.State = "not_applicable";
            result.ReasonCode = closed ? "closed_path_has_no_open_rear" : "selected_segment_is_not_path_rear";
            return result;
        }
        if (pathLength < BeltCargoObservationPolicy.CargoCellLength || window.Length < BeltCargoObservationPolicy.CargoCellLength)
        { result.ReasonCode = "rear_packet_window_unavailable"; return result; }
        if (!BeltCargoObservationPolicy.TryLocateCargo(window, windowStart, pathLength,
                segmentStart, segmentLength, false, out var references, out reason)
            || !BeltCargoObservationPolicy.TrySummarize(references, samples, out _, out reason))
        { result.ReasonCode = reason; return result; }

        // Current native TryPickItemAtRear tests exactly pathLength-6. Seeing another
        // stack somewhere in this segment is not evidence about this pickup position.
        var marker = pathLength - 6;
        result.MarkerPathCell = marker;
        if (window[marker - windowStart] != 250)
        {
            result.State = "no_aligned_packet";
            result.ReasonCode = "no_packet_aligned_for_native_rear_pickup";
            return result;
        }
        var tail = new byte[BeltCargoObservationPolicy.CargoCellLength];
        Array.Copy(window, window.Length - tail.Length, tail, 0, tail.Length);
        if (!BeltCargoObservationPolicy.TryLocateCargo(tail, pathLength - tail.Length, pathLength,
                pathLength - 1, 1, false, out var tailReferences, out reason) || tailReferences.Count != 1)
        { result.ReasonCode = reason ?? "rear_packet_reference_unavailable"; return result; }
        var id = tailReferences[0].CargoId;
        var sample = samples!.FirstOrDefault(value => value.CargoId == id);
        if (sample is null)
        { result.ReasonCode = "rear_packet_native_readback_missing"; return result; }
        result.State = "observed";
        result.ItemId = sample.ItemId;
        result.Count = sample.StackCount;
        result.Inc = sample.Inc;
        return result;
    }
}
