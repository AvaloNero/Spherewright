using Spherewright.Bridge.Core.Factory;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class BeltPathHeaderProbeTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(44)]
    [InlineData(45)]
    [InlineData(46)]
    [InlineData(4096)]
    public void StopsExporterAtFixedHeaderBeforeBodyForAnyWriteBoundaries(int chunk)
    {
        var bytes = new byte[4096];
        Header().CopyTo(bytes, 0);
        var bodyReached = false;
        Assert.True(BeltPathHeaderProbe.TryCapture(writer =>
        {
            for (var offset = 0; offset < bytes.Length; offset += chunk)
                writer.Write(bytes, offset, Math.Min(chunk, bytes.Length - offset));
            bodyReached = true;
        }, out var header));
        Assert.False(bodyReached);
        Assert.Equal(Header(), header);
        Assert.True(Valid(header!, out var count));
        Assert.Equal(1, count);
    }

    [Fact]
    public void NativePrimitiveSequenceStopsBeforeBufferOrUnsafeGeometryCall()
    {
        var reachedNativeCopy = false;
        Assert.True(BeltPathHeaderProbe.TryCapture(writer =>
        {
            WriteHeader(writer);
            reachedNativeCopy = true;
            throw new Exception("Native body must not execute.");
        }, out var header));
        Assert.False(reachedNativeCopy);
        Assert.Equal(Header(), header);
    }

    [Fact]
    public void ShortExportDoesNotReturnPartialProof()
    {
        for (var length = 0; length < 45; length++)
        {
            Assert.False(BeltPathHeaderProbe.TryCapture(w => w.Write(new byte[length]), out var header));
            Assert.Null(header);
        }
        Assert.False(BeltPathHeaderProbe.TryCapture(null!, out _));
    }

    [Fact]
    public void UnrelatedExporterFailureIsNotSwallowedAsHeaderSuccess()
    {
        Assert.Throws<IOException>(() => BeltPathHeaderProbe.TryCapture(_ => throw new IOException("real failure"), out _));
        Assert.Throws<InvalidOperationException>(() => BeltPathHeaderProbe.TryCapture(_ => throw new InvalidOperationException(), out _));
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(4, 8)]
    [InlineData(8, 39)]
    [InlineData(12, 41)]
    [InlineData(16, 0)]
    [InlineData(16, int.MaxValue)]
    [InlineData(20, 0)]
    [InlineData(20, 2)]
    [InlineData(20, int.MaxValue)]
    [InlineData(24, 41)]
    [InlineData(29, -1)]
    [InlineData(29, 7)]
    [InlineData(33, 0)]
    [InlineData(37, 0)]
    [InlineData(37, 513)]
    [InlineData(41, 129)]
    public void MalformedOrMismatchedHeaderIsRefusedBeforeNativeArrayCopy(int offset, int value)
    {
        var header = Header();
        BitConverter.GetBytes(value).CopyTo(header, offset);
        Assert.False(Valid(header, out var count));
        Assert.Equal(0, count);
    }

    [Theory]
    [InlineData(39, 40, 40, 3)]
    [InlineData(40, 39, 40, 3)]
    [InlineData(40, 40, 39, 3)]
    [InlineData(40, 41, 40, 3)]
    [InlineData(40, 40, 40, 2)]
    [InlineData(40, 40, 40, 4)]
    [InlineData(40, 40, 40, 6)]
    public void NativeBackingArrayShapesMustAgreeWithCapturedHeader(int buffer, int positions, int rotations, int chunks) =>
        Assert.False(BeltPathHeaderProbe.TryValidate(Header(), 7, 40, buffer, positions, rotations, chunks, 3, 0, out _));

    [Fact]
    public void ClosedPathMustHaveNativeBuckleAndSelfOutput()
    {
        var header = Header();
        header[28] = 1;
        Assert.False(Valid(header, out _));
        BitConverter.GetBytes(7).CopyTo(header, 29);
        BitConverter.GetBytes(4).CopyTo(header, 33);
        Assert.False(Valid(header, out _));
        BitConverter.GetBytes(1).CopyTo(header, 41);
        Assert.True(BeltPathHeaderProbe.TryValidate(header, 7, 40, 40, 40, 40, 3, 3, 1, out _));
        header[28] = 2;
        Assert.False(Valid(header, out _));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(8193)]
    [InlineData(int.MaxValue)]
    public void LogicalLengthIsBoundedBeforeBodyAllocation(int length) =>
        Assert.False(BeltPathHeaderProbe.TryValidate(Header(), 7, length,
            int.MaxValue, int.MaxValue, int.MaxValue, 3, 3, 0, out _));

    [Fact]
    public void LargestAllowedHeaderStillHasBoundedManagedPayload()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        foreach(var value in new[] { 1, 7, 8192, 8192, 8192, 8192, 8192 }) writer.Write(value);
        writer.Write(false);
        foreach(var value in new[] { 0, -1, 512, 128 }) writer.Write(value);
        Assert.True(BeltPathHeaderProbe.TryValidate(stream.ToArray(), 7, 8192,
            8192, 8192, 8192, 8192 * 3, 512, 128, out var chunks));
        Assert.Equal(8192, chunks);
    }

    private static bool Valid(byte[] header, out int count) =>
        BeltPathHeaderProbe.TryValidate(header, 7, 40, 40, 40, 40, 3, 3, 0, out count);
    private static byte[] Header()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        WriteHeader(writer);
        return stream.ToArray();
    }
    private static void WriteHeader(BinaryWriter writer)
    {
        foreach (var value in new[] { 1, 7, 40, 40, 1, 1, 40 }) writer.Write(value);
        writer.Write(false);
        foreach (var value in new[] { 0, -1, 3, 0 }) writer.Write(value);
    }
}
