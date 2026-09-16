using Spherewright.Bridge.Core.Safety;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class OwnedSaveRecoveryLeaseTests : IDisposable
{
    private const string Identity = "synthetic-protected-world-identity";
    private readonly string _path = Path.Combine(Path.GetTempPath(), "spherewright-synthetic-" + Guid.NewGuid().ToString("N"));

    public OwnedSaveRecoveryLeaseTests()
    {
        using var fixture = OwnedSavePrefixReaderTests.Fixture();
        using var file = new FileStream(_path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        fixture.CopyTo(file);
    }

    [Fact]
    public void UnchangedFileHasTheSameEvidenceAcrossPrepareAndCommit()
    {
        string prepared;
        using (var lease = OwnedSaveRecoveryLease.Open(_path, Identity)) prepared = lease.Fingerprint;
        using var commitLease = OwnedSaveRecoveryLease.Open(_path, Identity);
        Assert.Equal(prepared, commitLease.Fingerprint);
    }

    [Fact]
    public void BodyDriftWithSamePrefixLengthAndMtimeStillChangesFingerprint()
    {
        string prepared;
        DateTimeOffset written;
        using (var lease = OwnedSaveRecoveryLease.Open(_path, Identity))
        { prepared = lease.Fingerprint; written = lease.WrittenAtUtc; }
        using (var file = new FileStream(_path, FileMode.Open, FileAccess.Write))
        { file.Position = file.Length - 1; file.WriteByte(17); }
        File.SetLastWriteTimeUtc(_path, written.UtcDateTime);
        using var changed = OwnedSaveRecoveryLease.Open(_path, Identity);
        Assert.Equal(written, changed.WrittenAtUtc);
        Assert.NotEqual(prepared, changed.Fingerprint);
    }

    [Fact]
    public void WindowsLeaseAllowsNativeReaderButRejectsWriteAndDelete()
    {
        if (!OperatingSystem.IsWindows()) return; // These are the supported Windows sharing semantics.
        using (var lease = OwnedSaveRecoveryLease.Open(_path, Identity))
        {
            using var nativeRead = new FileStream(_path, FileMode.Open, FileAccess.Read);
            Assert.True(nativeRead.ReadByte() >= 0);
            Assert.Throws<IOException>(() => { using var writer = new FileStream(_path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite); });
            Assert.Throws<IOException>(() => File.Delete(_path));
        }
        using var afterDispose = new FileStream(_path, FileMode.Open, FileAccess.Write, FileShare.None);
    }

    [Fact]
    public void WrongIdentityReleasesHandleAndDoesNotExposeFileContent()
    {
        var error = Assert.Throws<InvalidDataException>(() => OwnedSaveRecoveryLease.Open(_path, "wrong-identity"));
        Assert.DoesNotContain(Identity, error.Message);
        Assert.DoesNotContain(_path, error.Message);
        using var afterFailure = new FileStream(_path, FileMode.Open, FileAccess.Write, FileShare.None);
    }

    [Fact]
    public void OversizeFileIsRejectedBeforeHashing()
    {
        using (var file = new FileStream(_path, FileMode.Open, FileAccess.Write))
            file.SetLength(OwnedSaveRecoveryLease.MaximumFileBytes + 1);
        Assert.Throws<InvalidDataException>(() => OwnedSaveRecoveryLease.Open(_path, Identity));
    }

    public void Dispose() => File.Delete(_path);
}
