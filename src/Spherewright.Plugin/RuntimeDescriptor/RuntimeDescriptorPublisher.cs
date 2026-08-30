using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using BepInEx.Logging;
using Spherewright.Contracts.Protocol;
using Spherewright.Plugin.Security;
using Spherewright.Plugin.Transport;

namespace Spherewright.Plugin.RuntimeDescriptor;

internal sealed class RuntimeDescriptorPublisher : IDisposable
{
    private readonly string _runtimeDirectory;
    private readonly ManualLogSource _logger;
    private string? _descriptorPath;

    public RuntimeDescriptorPublisher(string configuredDirectory, ManualLogSource logger)
    {
        try
        {
            _runtimeDirectory = ResolveRuntimeDirectory(configuredDirectory);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("Runtime descriptor directory normalization failed.", exception);
        }

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private static string ResolveRuntimeDirectory(string configuredDirectory)
    {
        const string localAppDataToken = "%LOCALAPPDATA%";
        if (configuredDirectory.StartsWith(localAppDataToken, StringComparison.OrdinalIgnoreCase))
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(localAppData))
            {
                throw new InvalidOperationException("The local application-data directory is unavailable.");
            }

            var remainder = configuredDirectory.Substring(localAppDataToken.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return Path.Combine(localAppData, remainder);
        }

        var expanded = Environment.ExpandEnvironmentVariables(configuredDirectory);
        if (!Path.IsPathRooted(expanded))
        {
            throw new InvalidOperationException("The runtime descriptor directory must be an absolute path.");
        }

        return expanded;
    }

    public void Publish(BridgeRuntimeDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        EnsureSecureDirectory();
        CleanupStaleDescriptors();

        var finalPath = Path.Combine(_runtimeDirectory, $"bridge-{descriptor.ProcessId}.json");
        var temporaryPath = Path.Combine(
            _runtimeDirectory,
            $".bridge-{descriptor.ProcessId}-{Guid.NewGuid():N}.tmp");

        var json = PluginJson.Serialize(descriptor);
        WindowsCurrentUserSecurity.WriteSecureNewFile(temporaryPath, new UTF8Encoding(false).GetBytes(json));
        File.Move(temporaryPath, finalPath);
        _descriptorPath = finalPath;
    }

    public void Dispose()
    {
        var path = _descriptorPath;
        _descriptorPath = null;
        if (path is null)
        {
            return;
        }

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            _logger.LogWarning($"Spherewright could not remove its runtime descriptor: {exception.Message}");
        }
    }

    private void EnsureSecureDirectory()
    {
        WindowsCurrentUserSecurity.EnsureSecureDirectory(_runtimeDirectory);
    }

    private void CleanupStaleDescriptors()
    {
        foreach (var path in Directory.GetFiles(_runtimeDirectory, "bridge-*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var descriptor = PluginJson.Deserialize<BridgeRuntimeDescriptor>(File.ReadAllText(path));
                if (descriptor is null || !IsLiveDspProcess(descriptor.ProcessId))
                {
                    File.Delete(path);
                }
            }
            catch (Exception exception) when (
                exception is IOException
                || exception is UnauthorizedAccessException
                || exception is Newtonsoft.Json.JsonException
                || exception is ArgumentException)
            {
                _logger.LogWarning($"Spherewright ignored an unreadable stale descriptor candidate: {exception.Message}");
            }
        }
    }

    private static bool IsLiveDspProcess(int processId)
    {
        try
        {
            using (var process = Process.GetProcessById(processId))
            {
                return !process.HasExited
                    && string.Equals(process.ProcessName, "DSPGAME", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is InvalidOperationException
            || exception is Win32Exception)
        {
            return false;
        }
    }
}
