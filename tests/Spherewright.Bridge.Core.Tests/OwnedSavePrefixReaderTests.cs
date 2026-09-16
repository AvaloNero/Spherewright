using System.Text;
using Spherewright.Bridge.Core.Safety;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class OwnedSavePrefixReaderTests
{
    private const string Identity = "synthetic-protected-world-identity";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReadsOnlyIdentityPrefixAndKeepsStreamOpen(bool sandbox)
    {
        using var stream = Fixture(sandbox: sandbox);
        var evidence = OwnedSavePrefixReader.Read(stream, Identity);
        Assert.True(evidence.MatchesExpectedIdentity);
        Assert.Equal(12345, evidence.GameTick);
        Assert.Equal("0.10.34.28529", evidence.GameVersion);
        Assert.True(evidence.Peaceful);
        Assert.Equal(sandbox, evidence.Sandbox);
        Assert.Equal(stream.Length - 4, stream.Position);
        Assert.Equal(stream.Position, evidence.PrefixEnd);
        Assert.Equal(99, new BinaryReader(stream).ReadInt32()); // Unread GameDesc sentinel.
        Assert.DoesNotContain(typeof(OwnedSavePrefixEvidence).GetProperties(), p => p.Name.Contains("Name"));
    }

    [Theory]
    [InlineData("different-world")]
    [InlineData("synthetic-protected-world-IDENTITY")]
    [InlineData("synthetic-protected-world-identity-extra")]
    public void SimilarOrWrongNamesDoNotEstablishOwnership(string actualIdentity)
    {
        using var stream = Fixture(identity: actualIdentity);
        Assert.False(OwnedSavePrefixReader.Read(stream, Identity).MatchesExpectedIdentity);
    }

    [Theory]
    [InlineData(0, 0)] // Magic.
    [InlineData(6, 0)] // Stored file length.
    [InlineData(14, 6)] // Unsupported header.
    [InlineData(18, 2)] // Noncanonical mode.
    [InlineData(19, 2)]
    [InlineData(20, -1)] // Invalid version.
    [InlineData(36, -1)] // Negative game tick (low and high words patched below).
    [InlineData(52, -1)] // Negative screenshot size.
    [InlineData(52, 16777217)]
    public void RejectsMalformedHeader(int offset, int value)
    {
        using var stream = Fixture();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            stream.Position = offset;
            if (offset == 36) writer.Write(-1L);
            else writer.Write(value);
        }
        stream.Position = 0;
        Assert.Throws<InvalidDataException>(() => OwnedSavePrefixReader.Read(stream, Identity));
    }

    [Theory]
    [InlineData(1, 13, 22)]
    [InlineData(0, 12, 22)]
    [InlineData(0, 13, 21)]
    public void RejectsUnresearchedLayouts(int accountVersion, int dataVersion, int patch)
    {
        using var stream = Fixture(accountVersion: accountVersion, dataVersion: dataVersion, patch: patch);
        Assert.Throws<InvalidDataException>(() => OwnedSavePrefixReader.Read(stream, Identity));
    }

    [Fact]
    public void OversizedIdentityIsRejectedBeforeAllocation()
    {
        using var stream = Fixture(identity: new string('x', OwnedSavePrefixReader.MaximumIdentityBytes + 1));
        Assert.Throws<InvalidDataException>(() => OwnedSavePrefixReader.Read(stream, Identity));
    }

    [Fact]
    public void OversizedAccountIsRejectedWithoutReadingItsValue()
    {
        using var stream = Fixture(accountName: new string('x', OwnedSavePrefixReader.MaximumAccountNameBytes + 1));
        Assert.Throws<InvalidDataException>(() => OwnedSavePrefixReader.Read(stream, Identity));
    }

    [Fact]
    public void InvalidUtf8IdentityIsRejected()
    {
        using var stream = Fixture(identity: "x");
        stream.Position = stream.Length - 5;
        stream.WriteByte(0xff);
        stream.Position = 0;
        Assert.Throws<DecoderFallbackException>(() => OwnedSavePrefixReader.Read(stream, Identity));
    }

    [Fact]
    public void RejectsTruncatedOrWrongOffsetStreams()
    {
        using var stream = Fixture();
        stream.SetLength(53);
        Assert.Throws<InvalidDataException>(() => OwnedSavePrefixReader.Read(stream, Identity));
        stream.Position = 1;
        Assert.Throws<ArgumentException>(() => OwnedSavePrefixReader.Read(stream, Identity));
    }

    [Theory]
    [InlineData(128, 0)] // Noncanonical two-byte zero.
    [InlineData(255, 127)] // Bounded before any identity allocation.
    public void RejectsInvalidStringLengths(int first, int second)
    {
        using var stream = Fixture(identity: "x");
        stream.Position = stream.Length - 6;
        stream.WriteByte((byte)first);
        stream.WriteByte((byte)second);
        stream.Position = 0;
        Assert.Throws<InvalidDataException>(() => OwnedSavePrefixReader.Read(stream, Identity));
    }

    [Theory]
    [InlineData(58)] // Screenshot skip beyond actual stream.
    [InlineData(65)] // Account metadata skip beyond actual stream.
    public void TruncationWithCorrectLengthStillFailsClosed(int length)
    {
        using var stream = Fixture();
        stream.SetLength(length);
        stream.Position = 6;
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true)) writer.Write((long)length);
        stream.Position = 0;
        Assert.Throws<EndOfStreamException>(() => OwnedSavePrefixReader.Read(stream, Identity));
    }

    private static MemoryStream Fixture(string identity = Identity, bool sandbox = false,
        int accountVersion = 0, int dataVersion = 13, int patch = 22, string accountName = "not-exposed")
    {
        var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(Encoding.ASCII.GetBytes("VFSAVE"));
            writer.Write(0L); writer.Write(7); writer.Write(sandbox); writer.Write(true);
            foreach (var value in new[] { 0, 10, 34, 28529 }) writer.Write(value);
            writer.Write(12345L); writer.Write(new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc).Ticks);
            writer.Write(3); writer.Write(new byte[] { 1, 2, 3 });
            Account(writer, accountVersion, accountName);
            writer.Write(0UL);
            writer.Write(dataVersion); writer.Write(patch);
            Account(writer, accountVersion, accountName);
            writer.Write(identity);
            writer.Write(99);
            stream.Position = 6; writer.Write(stream.Length);
        }
        stream.Position = 0;
        return stream;
    }

    private static void Account(BinaryWriter writer, int version, string name)
    {
        writer.Write(version); writer.Write(1); writer.Write(987UL); writer.Write(name);
    }
}
