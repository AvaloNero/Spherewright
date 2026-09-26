using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using Spherewright.Plugin.Bootstrap;
using Xunit;

namespace Spherewright.Bridge.Core.Tests;

public sealed class InstallationStartupGuardTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "spherewright-startup-guard-" + Guid.NewGuid().ToString("N"));
    private readonly string _plugin;

    public InstallationStartupGuardTests()
    {
        _plugin = Path.Combine(_root, "plugins", "Spherewright");
        Directory.CreateDirectory(_plugin);
    }

    [Fact]
    public void ClearInstallationPermitsHostAndReleasesLease()
    {
        var hostStarts = 0;
        using (InstallationStartupGuard.Acquire(_plugin, _root)) hostStarts++;
        Assert.Equal(1, hostStarts);
        using var again = InstallationStartupGuard.Acquire(_plugin, _root);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not JSON")]
    [InlineData("{\"status\":\"committed\"}")]
    public void AnyPendingFileBlocksHostWithoutReadingOrRemovingItsContents(string contents)
    {
        var marker = Path.Combine(_root, InstallationStartupGuard.PendingMarkerName);
        File.WriteAllText(marker, contents);
        var hostStarts = 0;
        Assert.Throws<InvalidOperationException>(() =>
        {
            using var lease = InstallationStartupGuard.Acquire(_plugin, _root);
            hostStarts++;
        });
        Assert.Equal(0, hostStarts);
        Assert.Equal(contents, File.ReadAllText(marker));
        // Rejection must not leak the mutex on its thread.
        Assert.True(CanAcquireOnOtherThread());
    }

    [Fact]
    public void DirectoryAtMarkerAlsoBlocksHost()
    {
        Directory.CreateDirectory(Path.Combine(_root, InstallationStartupGuard.PendingMarkerName));
        Assert.Throws<InvalidOperationException>(() => InstallationStartupGuard.Acquire(_plugin, _root));
    }

    [Fact]
    public void MissingInstallationRootIsNotTreatedAsAbsentMarker()
    {
        Assert.Throws<DirectoryNotFoundException>(() => InstallationStartupGuard.Acquire(_plugin, Path.Combine(_root, "missing", "root")));
    }

    [Fact]
    public void TargetMutexMatchesInstallerCanonicalUtf8UppercaseSha256()
    {
        var full = Path.GetFullPath(_plugin);
        var expected = "Local\\SpherewrightInstallTarget-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(full.ToUpperInvariant())));
        Assert.Equal(expected, InstallationStartupGuard.GetTargetMutexName(full + Path.DirectorySeparatorChar));
        Assert.Equal(expected, InstallationStartupGuard.GetTargetMutexName(full.ToLowerInvariant()));
    }

    [Fact]
    public void HostLeaseExcludesInstallerUntilDisposed()
    {
        var lease = InstallationStartupGuard.Acquire(_plugin, _root);
        try { Assert.False(CanAcquireOnOtherThread()); }
        finally { lease.Dispose(); }
        Assert.True(CanAcquireOnOtherThread());
        lease.Dispose();
    }

    [Theory]
    [InlineData("powershell")]
    [InlineData("pwsh")]
    public void WindowsPowerShellInstallerProcessSharesTheHostMutex(string shell)
    {
        // Windows x64 support/CI: cross-process and cross-runtime, not merely
        // two threads using the same .NET implementation. No filesystem writes.
        var name = InstallationStartupGuard.GetTargetMutexName(_plugin);
        var script = "$m = [Threading.Mutex]::new($false, '" + name + "'); " +
            "try { if ($m.WaitOne(0)) { $m.ReleaseMutex(); exit 11 }; exit 0 } finally { $m.Dispose() }";
        var encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        int Probe()
        {
            using var process = Process.Start(new ProcessStartInfo(shell,
                "-NoProfile -NonInteractive -EncodedCommand " + encoded)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            }) ?? throw new InvalidOperationException("Failed to start mutex probe.");
            if (!process.WaitForExit(10000))
            {
                process.Kill();
                process.WaitForExit();
                throw new TimeoutException("Mutex probe exceeded ten seconds.");
            }
            return process.ExitCode;
        }

        using (InstallationStartupGuard.Acquire(_plugin, _root)) Assert.Equal(0, Probe());
        Assert.Equal(11, Probe());
    }

    [Fact]
    public void InstallerLeaseBlocksHostBeforeStart()
    {
        using var held = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        Exception? workerError = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var mutex = new Mutex(false, InstallationStartupGuard.GetTargetMutexName(_plugin));
                Assert.True(mutex.WaitOne(0));
                held.Set();
                try { Assert.True(release.Wait(TimeSpan.FromSeconds(10))); }
                finally { mutex.ReleaseMutex(); }
            }
            catch (Exception error) { workerError = error; held.Set(); }
        });
        thread.Start();
        try
        {
            Assert.True(held.Wait(TimeSpan.FromSeconds(10)));
            Assert.Null(workerError);
            var hostStarts = 0;
            Assert.Throws<InvalidOperationException>(() =>
            {
                using var lease = InstallationStartupGuard.Acquire(_plugin, _root);
                hostStarts++;
            });
            Assert.Equal(0, hostStarts);
        }
        finally { release.Set(); Assert.True(thread.Join(TimeSpan.FromSeconds(10))); }
        Assert.Null(workerError);
        using var after = InstallationStartupGuard.Acquire(_plugin, _root);
    }

    [Fact]
    public void AbandonedLockRefusesHostAndReleasesAcquiredOwnership()
    {
        // Keep a handle alive so the named abandoned mutex outlives its owner.
        using var keeper = new Mutex(false, InstallationStartupGuard.GetTargetMutexName(_plugin));
        var thread = new Thread(() => keeper.WaitOne());
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)));
        var error = Assert.Throws<InvalidOperationException>(() => InstallationStartupGuard.Acquire(_plugin, _root));
        Assert.Contains("abandoned", error.Message);
        Assert.True(CanAcquireOnOtherThread());
    }

    private bool CanAcquireOnOtherThread()
    {
        var acquired = false;
        Exception? workerError = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var mutex = new Mutex(false, InstallationStartupGuard.GetTargetMutexName(_plugin));
                acquired = mutex.WaitOne(0);
                if (acquired) mutex.ReleaseMutex();
            }
            catch (Exception error) { workerError = error; }
        });
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)));
        Assert.Null(workerError);
        return acquired;
    }

    public void Dispose()
    {
        var resolved = Path.GetFullPath(_root);
        var temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!resolved.StartsWith(temp, StringComparison.OrdinalIgnoreCase) ||
            !Path.GetFileName(resolved).StartsWith("spherewright-startup-guard-", StringComparison.Ordinal))
            throw new InvalidOperationException("Refusing unsafe startup-guard fixture cleanup.");
        Directory.Delete(resolved, recursive: true);
    }
}
