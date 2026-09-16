using System.Diagnostics;
using System.Security.Cryptography;

namespace Spherewright.Bridge.Core.Safety;

/// <summary>A caller-selected fixed-slot file is held read-only through native asynchronous loading.</summary>
public sealed class OwnedSaveRecoveryLease : IDisposable
{
    public const long MaximumFileBytes = 64L * 1024 * 1024;
    private readonly FileStream _stream;

    private OwnedSaveRecoveryLease(FileStream stream, OwnedSavePrefixEvidence prefix,
        DateTimeOffset writtenAtUtc, string contentHash)
    {
        _stream = stream;
        Prefix = prefix;
        WrittenAtUtc = writtenAtUtc;
        Fingerprint = CanonicalStateHash.Combine("verified-owned-last-exit-v1", prefix.FileLength,
            prefix.GameTick, prefix.SavedAtUtc, prefix.GameVersion, prefix.Peaceful, prefix.Sandbox,
            prefix.MatchesExpectedIdentity, prefix.PrefixEnd, writtenAtUtc, contentHash);
    }

    public OwnedSavePrefixEvidence Prefix { get; }
    public DateTimeOffset WrittenAtUtc { get; }
    public string Fingerprint { get; }

    public static OwnedSaveRecoveryLease Open(string fixedSlotPath, string protectedIdentity)
    {
        var stream = new FileStream(fixedSlotPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        try
        {
            if (stream.Length > MaximumFileBytes)
                throw new InvalidDataException("The recovery candidate exceeds the 64MiB bounded verification limit.");
            var timer = Stopwatch.StartNew();
            var prefix = OwnedSavePrefixReader.Read(stream, protectedIdentity);
            if (!prefix.MatchesExpectedIdentity)
                throw new InvalidDataException("The fixed slot does not match the exact protected identity.");
            var writtenAt = new DateTimeOffset(File.GetLastWriteTimeUtc(fixedSlotPath), TimeSpan.Zero);
            stream.Position = 0;
            var buffer = new byte[65536];
            using (var hash = SHA256.Create())
            {
                int count;
                long total = 0;
                while ((count = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    total += count;
                    if (total > MaximumFileBytes || timer.Elapsed > TimeSpan.FromSeconds(2))
                        throw new InvalidDataException("Bounded recovery verification exceeded its byte or time budget.");
                    hash.TransformBlock(buffer, 0, count, buffer, 0);
                }
                hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                if (total != prefix.FileLength || stream.Length != prefix.FileLength
                    || File.GetLastWriteTimeUtc(fixedSlotPath) != writtenAt.UtcDateTime)
                    throw new InvalidDataException("The recovery file changed during verification.");
                return new OwnedSaveRecoveryLease(stream, prefix, writtenAt,
                    BitConverter.ToString(hash.Hash!).Replace("-", string.Empty));
            }
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    public void Dispose() => _stream.Dispose();
}
