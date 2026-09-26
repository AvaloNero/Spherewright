using System.Security.Cryptography;
using System.Text;
using InstallationMutex = System.Threading.Mutex;

namespace Spherewright.Plugin.Bootstrap;

/// <summary>
/// Keeps the manual installer and a guard-aware Bridge host mutually exclusive.
/// Acquire and dispose on the Unity main thread; a Windows mutex is thread-affine.
/// This cannot retrofit a startup gate into an older Plugin binary.
/// </summary>
internal sealed class InstallationStartupGuard : IDisposable
{
    internal const string PendingMarkerName = ".spherewright-install-pending.json";
    private InstallationMutex? _mutex;

    private InstallationStartupGuard(InstallationMutex mutex) => _mutex = mutex;

    internal static InstallationStartupGuard Acquire(string pluginDirectory, string bepInExRoot)
    {
        var target = CanonicalDirectory(pluginDirectory);
        var root = CanonicalDirectory(bepInExRoot);
        AssertExistingOrdinaryDirectoryChain(target);
        AssertExistingOrdinaryDirectoryChain(root);

        var mutex = new InstallationMutex(false, GetTargetMutexName(target));
        var ownsMutex = false;
        try
        {
            try
            {
                ownsMutex = mutex.WaitOne(0);
            }
            catch (AbandonedMutexException)
            {
                // WaitOne grants ownership even though it throws. Release it below,
                // but do not treat an interrupted install/host as a healthy startup.
                ownsMutex = true;
                throw new InvalidOperationException("Spherewright installation lock was abandoned; inspect recovery evidence before restarting.");
            }

            if (!ownsMutex)
                throw new InvalidOperationException("Spherewright installation target is busy; Bridge startup refused.");

            AssertNoPendingMarker(Path.Combine(root, PendingMarkerName));
            return new InstallationStartupGuard(mutex);
        }
        catch
        {
            try
            {
                if (ownsMutex) mutex.ReleaseMutex();
            }
            finally
            {
                mutex.Dispose();
            }
            throw;
        }
    }

    internal static string GetTargetMutexName(string target)
    {
        // Must match Get-SpherewrightInstallMutexName, including invariant casing
        // and uppercase hexadecimal on both Windows PowerShell and PowerShell 7.
        using var hash = SHA256.Create();
        var bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(CanonicalDirectory(target).ToUpperInvariant()));
        return "Local\\SpherewrightInstallTarget-" + BitConverter.ToString(bytes).Replace("-", string.Empty);
    }

    private static string CanonicalDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("An installation directory is required.", nameof(path));
        var full = Path.GetFullPath(path);
        return string.Equals(full, Path.GetPathRoot(full), StringComparison.OrdinalIgnoreCase)
            ? full
            : full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static void AssertExistingOrdinaryDirectoryChain(string path)
    {
        for (var current = path; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
        {
            var attributes = File.GetAttributes(current);
            if ((attributes & FileAttributes.Directory) == 0 || (attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Spherewright installation paths must be ordinary directories without reparse points.");
        }
    }

    private static void AssertNoPendingMarker(string marker)
    {
        try
        {
            // File.Exists would turn access errors into false. Only proven absence
            // permits startup; directories, malformed contents and links all block.
            _ = File.GetAttributes(marker);
        }
        catch (FileNotFoundException)
        {
            return;
        }
        throw new InvalidOperationException("Spherewright installation is pending; Bridge startup refused. Preserve the installation recovery evidence.");
    }

    public void Dispose()
    {
        var mutex = _mutex;
        if (mutex is null) return;
        // Keep the lease if a caller violates thread affinity instead of claiming
        // the target lock was released. Awake and OnDestroy use the same thread.
        mutex.ReleaseMutex();
        _mutex = null;
        mutex.Dispose();
    }
}
