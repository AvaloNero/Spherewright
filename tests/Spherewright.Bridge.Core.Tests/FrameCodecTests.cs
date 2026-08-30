using Spherewright.Bridge.Core.Framing;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class FrameCodecTests
{
    [Fact]
    public async Task RoundTrip_PreservesPayload()
    {
        var codec = new FrameCodec(1024);
        var expected = FrameCodec.EncodeUtf8("{\"ok\":true}");
        using var stream = new MemoryStream();

        await codec.WriteFrameAsync(stream, expected, CancellationToken.None);
        stream.Position = 0;
        var actual = await codec.ReadFrameAsync(stream, CancellationToken.None);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task PartialReads_AreReassembled()
    {
        var codec = new FrameCodec(1024);
        var payload = FrameCodec.EncodeUtf8("partial");
        using var encoded = new MemoryStream();
        await codec.WriteFrameAsync(encoded, payload, CancellationToken.None);
        using var stream = new ChunkedReadStream(encoded.ToArray(), 1);

        var actual = await codec.ReadFrameAsync(stream, CancellationToken.None);

        Assert.Equal(payload, actual);
    }

    [Fact]
    public async Task StickyFrames_AreReadIndividually()
    {
        var codec = new FrameCodec(1024);
        using var stream = new MemoryStream();
        await codec.WriteFrameAsync(stream, FrameCodec.EncodeUtf8("one"), CancellationToken.None);
        await codec.WriteFrameAsync(stream, FrameCodec.EncodeUtf8("two"), CancellationToken.None);
        stream.Position = 0;

        var first = await codec.ReadFrameAsync(stream, CancellationToken.None);
        var second = await codec.ReadFrameAsync(stream, CancellationToken.None);

        Assert.Equal("one", FrameCodec.DecodeUtf8(first!));
        Assert.Equal("two", FrameCodec.DecodeUtf8(second!));
    }

    [Fact]
    public async Task ZeroLengthFrame_IsRepresentedAsEmptyPayload()
    {
        var codec = new FrameCodec(1024);
        using var stream = new MemoryStream(new byte[4]);

        var payload = await codec.ReadFrameAsync(stream, CancellationToken.None);

        Assert.NotNull(payload);
        Assert.Empty(payload);
    }

    [Fact]
    public async Task OversizedFrame_IsRejectedBeforeAllocation()
    {
        var codec = new FrameCodec(8);
        using var stream = new MemoryStream(new byte[] { 9, 0, 0, 0 });

        var exception = await Assert.ThrowsAsync<FrameProtocolException>(
            () => codec.ReadFrameAsync(stream, CancellationToken.None));

        Assert.Contains("exceeds", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NegativeLength_IsRejected()
    {
        var codec = new FrameCodec(1024);
        using var stream = new MemoryStream(new byte[] { 0xff, 0xff, 0xff, 0xff });

        await Assert.ThrowsAsync<FrameProtocolException>(
            () => codec.ReadFrameAsync(stream, CancellationToken.None));
    }

    [Fact]
    public void InvalidUtf8_IsRejected()
    {
        Assert.Throws<FrameProtocolException>(() => FrameCodec.DecodeUtf8(new byte[] { 0xc3, 0x28 }));
    }

    [Fact]
    public async Task TruncatedPayload_IsRejected()
    {
        var codec = new FrameCodec(1024);
        using var stream = new MemoryStream(new byte[] { 3, 0, 0, 0, 1, 2 });

        await Assert.ThrowsAsync<EndOfStreamException>(
            () => codec.ReadFrameAsync(stream, CancellationToken.None));
    }

    [Fact]
    public async Task CleanEndOfStream_ReturnsNull()
    {
        var codec = new FrameCodec(1024);
        using var stream = new MemoryStream();

        var payload = await codec.ReadFrameAsync(stream, CancellationToken.None);

        Assert.Null(payload);
    }

    private sealed class ChunkedReadStream : Stream
    {
        private readonly MemoryStream _inner;
        private readonly int _maxChunk;

        public ChunkedReadStream(byte[] bytes, int maxChunk)
        {
            _inner = new MemoryStream(bytes);
            _maxChunk = maxChunk;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _inner.Read(buffer, offset, Math.Min(count, _maxChunk));
        }

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            return _inner.ReadAsync(buffer, offset, Math.Min(count, _maxChunk), cancellationToken);
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

