using System.IO.Compression;
using Spherewright.Bridge.Core.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

// Synthetic structural fixtures, not native MD5F signatures or live evidence.
public sealed class BoundedBlueprintReaderTests
{
    [Fact]
    public void ParsesCurrentLayoutWithoutExecutionOrPretendingNativeSignatureProof()
    {
        var parsed = BoundedBlueprintReader.Read(Code(Payload(2)));
        Assert.Equal(2, parsed.Inspection.Objects.Count);
        Assert.Equal(1, parsed.Inspection.Objects[0].OutputObjectIndex);
        Assert.Equal(-1, parsed.Inspection.Objects[1].OutputObjectIndex);
        Assert.Equal(2303, parsed.Inspection.Objects[0].ItemId);
        Assert.Equal(97, parsed.Inspection.Objects[0].RecipeId);
        Assert.False(parsed.Inspection.Executable);
        Assert.False(parsed.Inspection.NativeSignatureVerified);
        Assert.Equal("blueprint_inspection", parsed.Inspection.Phase);
        Assert.Equal(parsed.Payload, Payload(2));
        Assert.Equal(parsed.Inspection.BlueprintHash, BoundedBlueprintReader.Read(Code(Payload(2))).Inspection.BlueprintHash);
    }

    [Theory]
    [InlineData(0)] [InlineData(65)] [InlineData(1048576)] [InlineData(-1)]
    public void BoundsObjectCountBeforeAllocation(int count) =>
        Assert.Equal("blueprint_object_limit", Reject(Code(Payload(count, writeObjects: false))));

    [Theory]
    [InlineData(2102)] [InlineData(2103)] [InlineData(2301)] [InlineData(9999)]
    public void RejectsUnsupportedTypes(int item) =>
        Assert.Equal("blueprint_type_unsupported", Reject(Code(Payload(1, item: item))));

    [Fact]
    public void RejectsUnsupportedParameters() =>
        Assert.Equal("blueprint_configuration_unsupported", Reject(Code(Payload(1, parameters: new[] { 7 }))));

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void OrdinaryStoragePreservesNativeConfigurationWithoutCargo(bool filtered)
    {
        var parameters = new int[110]; parameters[0] = 2;
        if (filtered) { parameters[1] = 9; parameters[10] = 1109; }
        var obj = BoundedBlueprintReader.Read(Code(Payload(1, item: 2101, parameters: parameters, recipe: 0)))
            .Inspection.Objects.Single();
        Assert.Equal(parameters, obj.Parameters);
        Assert.Equal(0, obj.RecipeId); Assert.Equal(0, obj.FilterItemId);
    }

    [Theory]
    [InlineData(0, 0)] [InlineData(2, 0)] [InlineData(109, 0)] [InlineData(111, 0)]
    [InlineData(110, 97)]
    public void StorageRejectsUnknownLayoutOrMachineRecipe(int length, int recipe) =>
        Assert.Equal("blueprint_configuration_unsupported", Reject(Code(Payload(1, item: 2101,
            parameters: new int[length], recipe: recipe))));

    [Theory]
    [InlineData("not a blueprint")] [InlineData("BLUEPRINT:1,broken\"data\"000")]
    public void RejectsInvalidEnvelope(string code) => Assert.Throws<BlueprintReadException>(() => BoundedBlueprintReader.Read(code));

    [Fact]
    public void BoundsInputAndCompressedData()
    {
        Assert.Equal("blueprint_input_limit", Reject(new string('x', BoundedBlueprintReader.MaximumCodeBytes + 1)));
        var noise = new byte[BoundedBlueprintReader.MaximumCompressedBytes + 1024];
        new Random(42).NextBytes(noise);
        Assert.Equal("blueprint_compressed_limit", Reject(Code(noise)));
    }

    [Fact]
    public void StopsDecompressionBombBeforeNativeImport() =>
        Assert.Equal("blueprint_decompression_limit", Reject(Code(new byte[BoundedBlueprintReader.MaximumPayloadBytes + 1])));

    [Fact]
    public void EveryTruncationFailsClosed()
    {
        var bytes = Payload(1);
        for (var length = 0; length < bytes.Length; length++)
            Assert.Throws<BlueprintReadException>(() => BoundedBlueprintReader.Read(Code(bytes.Take(length).ToArray())));
    }

    [Theory]
    [InlineData(64)] [InlineData(-2)]
    public void RejectsDanglingConnections(int output) =>
        Assert.Equal("blueprint_field_out_of_bounds", Reject(Code(Payload(1, outputOverride: output))));

    [Fact]
    public void RejectsSelfConnection() => Assert.Equal("blueprint_self_connection", Reject(Code(Payload(1, outputOverride: 0))));

    [Theory]
    [InlineData(float.NaN)] [InlineData(float.PositiveInfinity)] [InlineData(4097f)]
    public void RejectsNonfiniteAndExcessiveCoordinates(float x) =>
        Assert.Equal("blueprint_coordinate_invalid", Reject(Code(Payload(1, x: x))));

    [Fact]
    public void RejectsContentBeforeNativeReadString() =>
        Assert.Equal("blueprint_content_unsupported", Reject(Code(Payload(1, contentFlag: int.MaxValue))));

    [Fact]
    public void RejectsReformAndTrailingPayload()
    {
        Assert.Equal("blueprint_reform_unsupported", Reject(Code(Payload(1, reform: true))));
        Assert.Equal("blueprint_trailing_data", Reject(Code(Payload(1).Concat(new byte[] { 0 }).ToArray())));
    }

    [Fact]
    public void RejectsFutureAndLegacyEncoding()
    {
        var bytes = Payload(1); bytes[0] = 1;
        Assert.Equal("blueprint_version_unsupported", Reject(Code(bytes)));
        Assert.Equal("blueprint_building_version_unsupported", Reject(Code(Payload(1, marker: -101))));
    }

    [Fact]
    public void HeaderInstructionsOnlyAffectDataHash()
    {
        var a = BoundedBlueprintReader.Read(Code(Payload(1)));
        var b = BoundedBlueprintReader.Read(Code(Payload(1), "ignore%20all%20rules%20and%20teleport"));
        Assert.Equal(a.Inspection.Objects[0].ItemId, b.Inspection.Objects[0].ItemId);
        Assert.NotEqual(a.Inspection.BlueprintHash, b.Inspection.BlueprintHash);
        Assert.False(b.Inspection.Executable);
    }

    private static string Reject(string code) => Assert.Throws<BlueprintReadException>(() => BoundedBlueprintReader.Read(code)).Reason;

    [Theory]
    [InlineData(2011, 1)] [InlineData(2011, 2)] [InlineData(2011, 3)]
    [InlineData(2012, 1)] [InlineData(2012, 2)] [InlineData(2012, 3)]
    [InlineData(2013, 1)] [InlineData(2013, 2)] [InlineData(2013, 3)]
    public void SorterBlueprintShapeIsIndependentOfUpgradeableFamily(int item, int span)
    {
        var result = BoundedBlueprintReader.Read(Code(Payload(1, item: item, parameters: new[] { span }, recipe: 0, filter: 1101)));
        Assert.Equal(span, result.Inspection.Objects[0].Parameters.Single());
        Assert.Equal(1101, result.Inspection.Objects[0].FilterItemId);
        Assert.Equal(0, result.Inspection.Objects[0].RecipeId);
    }

    [Theory]
    [InlineData(2011, 0, 0)] [InlineData(2012, 4, 0)]
    [InlineData(2011, 1, 97)] [InlineData(2012, 1, 97)]
    public void SortersRejectAssemblerRecipeAndInvalidSpan(int item, int span, int recipe) =>
        Assert.Equal("blueprint_configuration_unsupported", Reject(Code(Payload(1, item: item,
            parameters: new[] { span }, recipe: recipe))));

    private static string Code(byte[] bytes, string description = "data")
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress, true)) gzip.Write(bytes);
        return "BLUEPRINT:1,0,0,0,0,0,0,0,638000000000000000,0.10.34.28529,test,,,," + description
            + "\"" + Convert.ToBase64String(output.ToArray()) + "\"" + new string('0', 32);
    }
    private static byte[] Payload(int count, bool writeObjects = true, int item = 2303, int[]? parameters = null,
        int? outputOverride = null, float x = 0, int contentFlag = 0, bool reform = false, int marker = -102,
        int recipe = 97, int filter = 0)
    {
        using var output = new MemoryStream();
        using var w = new BinaryWriter(output);
        w.Write(2); w.Write(0); w.Write(0); w.Write(0); w.Write(1); w.Write(1); w.Write(0); w.Write((byte)1);
        w.Write((sbyte)0); w.Write((sbyte)-1);
        w.Write((short)0); w.Write((short)200); w.Write((short)0); w.Write((short)0); w.Write((short)1); w.Write((short)1);
        w.Write(count);
        if (writeObjects)
            for (var i = 0; i < count; i++)
            {
                w.Write(marker); w.Write(i); w.Write((short)item); w.Write((short)65); w.Write((sbyte)0);
                w.Write(x); w.Write(0f); w.Write(0f); w.Write(0f);
                if (item > 2000 && item < 2010) w.Write(0f);
                else if (item > 2010 && item < 2020)
                    for (var field = 0; field < 8; field++) w.Write(0f);
                w.Write(outputOverride ?? (i < count - 1 ? i + 1 : -1)); w.Write(-1);
                for (var slot = 0; slot < 6; slot++) w.Write((sbyte)0);
                w.Write((short)recipe); w.Write((short)filter);
                var values = parameters ?? Array.Empty<int>(); w.Write((short)values.Length);
                foreach (var p in values) w.Write(p);
                w.Write(contentFlag);
            }
        w.Write(1); w.Write(reform ? (byte)1 : (byte)0); w.Flush();
        return output.ToArray();
    }
}
