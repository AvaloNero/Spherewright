using System.Text;

namespace Spherewright.Bridge.Core.Framing;

public sealed class FrameCodec
{
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
    private readonly int _maxFrameBytes;

    public FrameCodec(int maxFrameBytes)
    {
        if (maxFrameBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxFrameBytes));
        }

        _maxFrameBytes = maxFrameBytes;
    }

    public async Task<byte[]?> ReadFrameAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        var header = new byte[4];
        var firstRead = await stream.ReadAsync(header, 0, header.Length, cancellationToken).ConfigureAwait(false);
        if (firstRead == 0)
        {
            return null;
        }

        await ReadRemainderAsync(stream, header, firstRead, header.Length, cancellationToken).ConfigureAwait(false);

        var length = header[0]
            | (header[1] << 8)
            | (header[2] << 16)
            | (header[3] << 24);

        if (length < 0)
        {
            throw new FrameProtocolException("Frame length must not be negative.");
        }

        if (length > _maxFrameBytes)
        {
            throw new FrameProtocolException($"Frame length {length} exceeds the configured maximum {_maxFrameBytes}.");
        }

        var payload = new byte[length];
        if (length > 0)
        {
            await ReadRemainderAsync(stream, payload, 0, length, cancellationToken).ConfigureAwait(false);
        }

        return payload;
    }

    public async Task WriteFrameAsync(Stream stream, byte[] payload, CancellationToken cancellationToken)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (payload is null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        if (payload.Length > _maxFrameBytes)
        {
            throw new FrameProtocolException($"Frame length {payload.Length} exceeds the configured maximum {_maxFrameBytes}.");
        }

        var length = payload.Length;
        var header = new[]
        {
            (byte)length,
            (byte)(length >> 8),
            (byte)(length >> 16),
            (byte)(length >> 24),
        };

        await stream.WriteAsync(header, 0, header.Length, cancellationToken).ConfigureAwait(false);
        if (payload.Length > 0)
        {
            await stream.WriteAsync(payload, 0, payload.Length, cancellationToken).ConfigureAwait(false);
        }

        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public static string DecodeUtf8(byte[] payload)
    {
        if (payload is null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        try
        {
            return StrictUtf8.GetString(payload);
        }
        catch (DecoderFallbackException exception)
        {
            throw new FrameProtocolException("Frame payload is not valid UTF-8.", exception);
        }
    }

    public static byte[] EncodeUtf8(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        return StrictUtf8.GetBytes(text);
    }

    private static async Task ReadRemainderAsync(
        Stream stream,
        byte[] buffer,
        int offset,
        int expectedCount,
        CancellationToken cancellationToken)
    {
        var readTotal = offset;
        while (readTotal < expectedCount)
        {
            var read = await stream.ReadAsync(
                buffer,
                readTotal,
                expectedCount - readTotal,
                cancellationToken).ConfigureAwait(false);

            if (read == 0)
            {
                throw new EndOfStreamException("Connection ended before the complete frame was received.");
            }

            readTotal += read;
        }
    }
}

