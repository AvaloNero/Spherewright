using System.IO;
using Spherewright.Bridge.Core.Factory;

namespace Spherewright.Plugin.Game;

/// <summary>
/// Read-only native path adapter building block. Call only on the Unity thread,
/// after owned/factory identity checks and under the native buffer lock.
/// This does not enable a belt upgrade or replace full reciprocal/cargo proof.
/// </summary>
internal static class NativeBeltPathCapture
{
    internal static bool TryRead(CargoPath path, out BeltPathExportSnapshot? snapshot, out string? reason)
    {
        snapshot = null;
        reason = "belt_path_native_bounds_unavailable";
        if (path is null || path.id <= 0 || path.pathLength < 1
            || path.pathLength > BeltUpgradePathPolicy.MaximumPathCells
            || path.buffer is null || path.chunks is null || path.pointPos is null || path.pointRot is null
            || path.belts is null || path.inputPaths is null
            || path.belts.Count < 1 || path.belts.Count > BeltUpgradePathPolicy.MaximumBelts
            || path.inputPaths.Count > BeltUpgradePathPolicy.MaximumInputPaths) return false;

        // Export writes its private counts in a fixed45-byte header first. A private
        // sentinel unwinds that read-only method before its first payload operation;
        // its UnsafeIO.WriteMassive (which lacks array-length checks) is NEVER reached.
        // The following typed managed reads are bounded by verified native lengths.
        try
        {
            if (!BeltPathHeaderProbe.TryCapture(path.Export, out var header)
                || !BeltPathHeaderProbe.TryValidate(header, path.id, path.pathLength,
                    path.buffer.Length, path.pointPos.Length, path.pointRot.Length, path.chunks.Length,
                    path.belts.Count, path.inputPaths.Count, out var chunkCount)) return false;
            var bytesRequired = checked(BeltPathHeaderProbe.HeaderBytes + 29 * path.pathLength
                + 12 * chunkCount + 4 * (path.belts.Count + path.inputPaths.Count));
            using var stream = new MemoryStream(bytesRequired);
            using var writer = new BinaryWriter(stream);
            writer.Write(header!);
            writer.Write(path.buffer, 0, path.pathLength);
            for (var i = 0; i < chunkCount * 3; i++) writer.Write(path.chunks[i]);
            // Native version1 layout stores ALL positions, then ALL rotations.
            for (var i = 0; i < path.pathLength; i++)
            {
                var position = path.pointPos[i];
                writer.Write(position.x); writer.Write(position.y); writer.Write(position.z);
            }
            for (var i = 0; i < path.pathLength; i++)
            {
                var rotation = path.pointRot[i];
                writer.Write(rotation.x); writer.Write(rotation.y); writer.Write(rotation.z); writer.Write(rotation.w);
            }
            foreach (var id in path.belts) writer.Write(id);
            foreach (var id in path.inputPaths) writer.Write(id);
            if (stream.Length != bytesRequired) return false;
            return BeltUpgradePathPolicy.TryReadExport(stream.ToArray(), path.id, path.pathLength,
                out snapshot, out reason);
        }
        catch (Exception exception) when (exception is IOException || exception is ArgumentException
            || exception is OverflowException || exception is InvalidOperationException)
        {
            // No partially decoded path or exception details become public evidence.
            snapshot = null;
            reason = "belt_path_native_capture_failed";
            return false;
        }
    }
}
