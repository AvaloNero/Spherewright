using System.IO.Compression;
using System.Text;
using Spherewright.Bridge.Core.Safety;
using Spherewright.Contracts.Factory;

namespace Spherewright.Bridge.Core.Factory;

// This structural preflight must precede BlueprintData.Import: the native decoder permits
// million-object allocations and has no decompression budget. It does not verify native MD5F.
public static class BoundedBlueprintReader
{
    public const int MaximumCodeBytes = 256 * 1024;
    public const int MaximumCompressedBytes = 128 * 1024;
    public const int MaximumPayloadBytes = 1024 * 1024;
    public const int MaximumObjects = 64;
    public const int MaximumAreas = 8;

    public static BoundedBlueprintData Read(string code)
    {
        if (code is null || code.Length > MaximumCodeBytes || Encoding.UTF8.GetByteCount(code) > MaximumCodeBytes)
            throw Invalid("blueprint_input_limit");
        if (!code.StartsWith("BLUEPRINT:", StringComparison.Ordinal)) throw Invalid("blueprint_envelope_invalid");
        var first = code.IndexOf('"');
        var last = code.LastIndexOf('"');
        if (first < 28 || first > 16384 || last <= first || code.Length - last - 1 != 32
            || code.IndexOf('"', first + 1) != last || !code.Substring(last + 1).All(IsHex))
            throw Invalid("blueprint_envelope_invalid");
        var header = code.Substring(10, first - 10).Split(',');
        if (!((header.Length == 12 && header[0] == "0") || (header.Length == 15 && header[0] == "1")))
            throw Invalid("blueprint_header_version_unsupported");
        byte[] compressed;
        try { compressed = Convert.FromBase64String(code.Substring(first + 1, last - first - 1)); }
        catch (FormatException) { throw Invalid("blueprint_base64_invalid"); }
        if (compressed.Length == 0 || compressed.Length > MaximumCompressedBytes) throw Invalid("blueprint_compressed_limit");
        try
        {
            using var source = new MemoryStream(compressed, false);
            using var gzip = new GZipStream(source, CompressionMode.Decompress);
            using var output = new MemoryStream();
            var chunk = new byte[4096];
            int count;
            while ((count = gzip.Read(chunk, 0, chunk.Length)) != 0)
            {
                if (output.Length + count > MaximumPayloadBytes) throw Invalid("blueprint_decompression_limit");
                output.Write(chunk, 0, count);
            }
            var payload = output.ToArray();
            var inspection = ReadPayload(payload);
            inspection.BlueprintHash = CanonicalStateHash.Combine("blueprint-code-v1", code);
            return new BoundedBlueprintData(payload, inspection);
        }
        catch (BlueprintReadException) { throw; }
        catch (IOException) { throw Invalid("blueprint_payload_invalid"); }
        catch (ArgumentException) { throw Invalid("blueprint_payload_invalid"); }
    }

    private static BlueprintInspection ReadPayload(byte[] payload)
    {
        using var stream = new MemoryStream(payload, false);
        using var reader = new BinaryReader(stream);
        if (reader.ReadInt32() != 2) throw Invalid("blueprint_version_unsupported");
        var result = new BlueprintInspection
        {
            CursorOffsetX = Bounded(reader.ReadInt32(), -4096, 4096),
            CursorOffsetY = Bounded(reader.ReadInt32(), -4096, 4096),
            CursorTargetArea = reader.ReadInt32(), DragBoxWidth = Bounded(reader.ReadInt32(), 1, 256),
            DragBoxHeight = Bounded(reader.ReadInt32(), 1, 256), PrimaryAreaIndex = reader.ReadInt32(),
        };
        var areaCount = Bounded(reader.ReadByte(), 1, MaximumAreas);
        Bounded(result.PrimaryAreaIndex, 0, areaCount - 1);
        Bounded(result.CursorTargetArea, -1, areaCount - 1);
        for (var i = 0; i < areaCount; i++)
        {
            var area = new BlueprintAreaSnapshot
            {
                Index = reader.ReadSByte(), ParentIndex = Bounded(reader.ReadSByte(), -1, areaCount - 1),
                TropicAnchor = reader.ReadInt16(), AreaSegments = Bounded(reader.ReadInt16(), 1, 1000),
                AnchorLocalOffsetX = reader.ReadInt16(), AnchorLocalOffsetY = reader.ReadInt16(),
                Width = Bounded(reader.ReadInt16(), 1, 256), Height = Bounded(reader.ReadInt16(), 1, 256),
            };
            if (area.Index != i) throw Invalid("blueprint_area_invalid");
            result.Areas.Add(area);
        }
        for (var i = 0; i < areaCount; i++)
        {
            var cursor = i;
            var seen = new HashSet<int>();
            while (cursor != -1)
            {
                if (!seen.Add(cursor)) throw Invalid("blueprint_area_cycle");
                var parent = result.Areas[cursor].ParentIndex;
                if (parent == -1 && cursor != result.PrimaryAreaIndex) throw Invalid("blueprint_area_invalid");
                cursor = parent;
            }
        }
        var objectCount = reader.ReadInt32();
        if (objectCount < 1 || objectCount > MaximumObjects) throw Invalid("blueprint_object_limit");
        for (var i = 0; i < objectCount; i++)
        {
            if (reader.ReadInt32() != -102) throw Invalid("blueprint_building_version_unsupported");
            var obj = new BlueprintObjectSnapshot
            {
                Index = reader.ReadInt32(), ItemId = Bounded(reader.ReadInt16(), 1, short.MaxValue),
                ModelIndex = Bounded(reader.ReadInt16(), 1, short.MaxValue),
                AreaIndex = Bounded(reader.ReadSByte(), 0, areaCount - 1),
                LocalOffset = Vector(reader), Yaw = Finite(reader),
            };
            if (obj.Index != i) throw Invalid("blueprint_object_index_invalid");
            obj.LocalOffset2 = new Vector3Snapshot { X = obj.LocalOffset.X, Y = obj.LocalOffset.Y, Z = obj.LocalOffset.Z };
            obj.Yaw2 = obj.Yaw;
            if (obj.ItemId > 2000 && obj.ItemId < 2010) obj.Tilt2 = obj.Tilt = Finite(reader);
            else if (obj.ItemId > 2010 && obj.ItemId < 2020)
            {
                obj.Tilt = Finite(reader); obj.Pitch = Finite(reader); obj.LocalOffset2 = Vector(reader);
                obj.Yaw2 = Finite(reader); obj.Tilt2 = Finite(reader); obj.Pitch2 = Finite(reader);
            }
            obj.OutputObjectIndex = Bounded(reader.ReadInt32(), -1, objectCount - 1);
            obj.InputObjectIndex = Bounded(reader.ReadInt32(), -1, objectCount - 1);
            if (obj.OutputObjectIndex == i || obj.InputObjectIndex == i) throw Invalid("blueprint_self_connection");
            obj.OutputToSlot = Bounded(reader.ReadSByte(), -1, 15);
            obj.InputFromSlot = Bounded(reader.ReadSByte(), -1, 15);
            obj.OutputFromSlot = Bounded(reader.ReadSByte(), -1, 15);
            obj.InputToSlot = Bounded(reader.ReadSByte(), -1, 15);
            obj.OutputOffset = reader.ReadSByte(); obj.InputOffset = reader.ReadSByte();
            obj.RecipeId = Bounded(reader.ReadInt16(), 0, short.MaxValue);
            obj.FilterItemId = Bounded(reader.ReadInt16(), 0, short.MaxValue);
            var parameterCount = Bounded(reader.ReadInt16(), 0, 128);
            obj.Parameters = new int[parameterCount];
            for (var p = 0; p < parameterCount; p++) obj.Parameters[p] = reader.ReadInt32();
            // Content-bearing devices/signs are unsupported: reject before native ReadString allocates.
            if (reader.ReadInt32() != 0) throw Invalid("blueprint_content_unsupported");
            ValidateSupportedObject(obj);
            result.Objects.Add(obj);
        }
        if (reader.ReadInt32() != 1) throw Invalid("blueprint_patch_unsupported");
        if (reader.ReadByte() != 0) throw Invalid("blueprint_reform_unsupported");
        if (stream.Position != stream.Length) throw Invalid("blueprint_trailing_data");
        return result;
    }

    // Blueprint shape/parameter support must not broaden when the separate upgrade allowlist grows.
    public static bool SupportsItem(int id) => (id >= 2302 && id <= 2305)
        || (id >= 2001 && id <= 2003) || (id >= 2011 && id <= 2013) || id == 2201 || id == 2203;

    private static void ValidateSupportedObject(BlueprintObjectSnapshot obj)
    {
        if (!SupportsItem(obj.ItemId)) throw Invalid("blueprint_type_unsupported");
        var assembler = obj.ItemId >= 2302 && obj.ItemId <= 2305;
        var inserter = obj.ItemId >= 2011 && obj.ItemId <= 2013;
        var parametersOk = assembler ? obj.Parameters.Length <= 1 && obj.Parameters.All(p => p == 0 || p == 1)
            : inserter ? obj.Parameters.Length == 1 && obj.Parameters[0] >= 1 && obj.Parameters[0] <= 3
            : obj.Parameters.Length == 0;
        if (!parametersOk || (!assembler && obj.RecipeId != 0) || (!inserter && obj.FilterItemId != 0))
            throw Invalid("blueprint_configuration_unsupported");
    }

    private static bool IsHex(char c) => c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F';
    private static Vector3Snapshot Vector(BinaryReader r) => new Vector3Snapshot { X = Finite(r), Y = Finite(r), Z = Finite(r) };
    private static float Finite(BinaryReader r)
    {
        var value = r.ReadSingle();
        if (float.IsNaN(value) || float.IsInfinity(value) || Math.Abs(value) > 4096) throw Invalid("blueprint_coordinate_invalid");
        return value;
    }
    private static int Bounded(int value, int min, int max)
    {
        if (value < min || value > max) throw Invalid("blueprint_field_out_of_bounds");
        return value;
    }
    private static BlueprintReadException Invalid(string reason) => new BlueprintReadException(reason);
}

public sealed class BoundedBlueprintData
{
    public BoundedBlueprintData(byte[] payload, BlueprintInspection inspection) { Payload = payload; Inspection = inspection; }
    public byte[] Payload { get; }
    public BlueprintInspection Inspection { get; }
}

public sealed class BlueprintReadException : Exception
{
    public BlueprintReadException(string reason) : base(reason) { Reason = reason; }
    public string Reason { get; }
}
