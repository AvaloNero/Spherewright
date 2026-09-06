using System.IO;

namespace Spherewright.Bridge.Core.Factory;

/// <summary>
/// Captures only the fixed native CargoPath.Export header. The sentinel stops the
/// exporter BEFORE its buffer loops or UnsafeIO geometry copy. This is a read-only
/// adapter helper, not a deserializer, native memory reader or upgrade permission.
/// </summary>
public static class BeltPathHeaderProbe
{
    public const int HeaderBytes = 45;

    public static bool TryCapture(Action<BinaryWriter> export, out byte[]? header)
    {
        header = null;
        if (export is null) return false;
        using var stream = new HeaderStream();
        using var writer = new BinaryWriter(stream);
        try { export(writer); }
        catch (HeaderCompleteException exception) when (ReferenceEquals(exception.Owner, stream))
        {
            header = stream.CopyCompletedHeader();
            return true;
        }
        // An exporter which returns early must never masquerade as a complete header.
        return false;
    }

    public static bool TryValidate(byte[]? header, int expectedPathId, int expectedLength,
        int bufferArrayLength, int positionArrayLength, int rotationArrayLength, int chunkArrayLength,
        int expectedBeltCount, int expectedInputCount, out int chunkCount)
    {
        chunkCount = 0;
        if (header is null || header.Length != HeaderBytes || expectedPathId <= 0
            || expectedLength < 1 || expectedLength > BeltUpgradePathPolicy.MaximumPathCells
            || bufferArrayLength < expectedLength || positionArrayLength != bufferArrayLength
            || rotationArrayLength != bufferArrayLength || chunkArrayLength < 3
            || chunkArrayLength % 3 != 0 || expectedBeltCount < 1
            || expectedBeltCount > BeltUpgradePathPolicy.MaximumBelts || expectedInputCount < 0
            || expectedInputCount > BeltUpgradePathPolicy.MaximumInputPaths) return false;
        using var stream = new MemoryStream(header, writable: false);
        using var reader = new BinaryReader(stream);
        if (reader.ReadInt32() != 1 || reader.ReadInt32() != expectedPathId
            || reader.ReadInt32() != bufferArrayLength || reader.ReadInt32() != expectedLength
            || reader.ReadInt32() != chunkArrayLength / 3) return false;
        var chunks = reader.ReadInt32();
        var updateLength = reader.ReadInt32();
        var closed = reader.ReadByte();
        var outputId = reader.ReadInt32();
        var outputIndex = reader.ReadInt32();
        if (chunks < 1 || chunks > expectedLength || chunks > chunkArrayLength / 3
            || updateLength < 0 || updateLength > expectedLength || closed > 1
            || outputId < 0 || (outputId == 0 ? outputIndex != -1 : outputIndex < 0)
            || (closed == 1 ? expectedLength < 19 || outputId != expectedPathId || outputIndex != 4 || expectedInputCount < 1
                : outputId == expectedPathId)
            || reader.ReadInt32() != expectedBeltCount || reader.ReadInt32() != expectedInputCount
            || HeaderBytes + 29L * expectedLength + 12L * chunks
                + 4L * (expectedBeltCount + expectedInputCount) > BeltUpgradePathPolicy.MaximumExportBytes)
            return false;
        chunkCount = chunks;
        return true;
    }

    private sealed class HeaderCompleteException : IOException
    {
        public HeaderCompleteException(HeaderStream owner) : base("Native belt header captured; body intentionally not executed.") => Owner = owner;
        public HeaderStream Owner { get; }
    }

    private sealed class HeaderStream : Stream
    {
        private readonly byte[] _header = new byte[HeaderBytes];
        private int _written;
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _written;
        public override long Position { get => _written; set => throw new NotSupportedException(); }
        public byte[] CopyCompletedHeader() => _written == HeaderBytes
            ? (byte[])_header.Clone() : throw new InvalidOperationException("Incomplete native belt header.");
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer is null || offset < 0 || count < 0 || (long)offset + count > buffer.Length)
                throw new ArgumentException("Invalid native belt header write.");
            // Do not retain, allocate for, or traverse any bytes beyond the fixed header.
            var copied = Math.Min(count, HeaderBytes - _written);
            Array.Copy(buffer, offset, _header, _written, copied);
            _written += copied;
            if (_written == HeaderBytes) throw new HeaderCompleteException(this);
        }
        public override void WriteByte(byte value)
        {
            if (_written == HeaderBytes) throw new HeaderCompleteException(this);
            _header[_written++] = value;
            if (_written == HeaderBytes) throw new HeaderCompleteException(this);
        }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
