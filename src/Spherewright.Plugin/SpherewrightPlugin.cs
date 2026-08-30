using BepInEx;
using Spherewright.Contracts.Protocol;
using Spherewright.Plugin.Bootstrap;
using Spherewright.Plugin.Hosting;

namespace Spherewright.Plugin;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("DSPGAME.exe")]
public sealed class SpherewrightPlugin : BaseUnityPlugin
{
    public const string PluginGuid = "dev.spherewright.bridge";
    public const string PluginName = "Spherewright";
    public const string PluginVersion = "0.1.0";

    private SpherewrightBridgeHost? _host;

    private void Awake()
    {
        Logger.LogInfo("Spherewright plugin loaded");
        Logger.LogInfo($"Spherewright plugin version: {PluginVersion}");
        Logger.LogInfo($"Spherewright protocol version: {ProtocolConstants.CurrentVersion}");

        try
        {
            var configuration = SpherewrightConfiguration.Load(Config);
            _host = SpherewrightBridgeHost.Create(configuration, Logger, PluginVersion);
            Logger.LogInfo($"Spherewright writes configured: {(configuration.AllowWrites ? "enabled" : "disabled")}");

            if (!configuration.Enabled)
            {
                Logger.LogWarning("Spherewright bridge is disabled by configuration");
                _host.Dispose();
                _host = null;
                return;
            }

            _host.Start();
            Logger.LogInfo("Spherewright bridge started");
        }
        catch (Exception exception)
        {
            _host?.Dispose();
            _host = null;
            Logger.LogError($"Spherewright bridge startup failed: {FormatExceptionChain(exception)}");
            Logger.LogError(exception.ToString());
        }
    }

    private void Update()
    {
        _host?.PumpMainThread();
    }

    private void OnDestroy()
    {
        _host?.Dispose();
        _host = null;
    }

    private static string FormatExceptionChain(Exception exception)
    {
        var messages = new List<string>();
        for (var current = exception; current is not null; current = current.InnerException)
        {
            messages.Add($"{current.GetType().Name}: {current.Message}");
        }

        return string.Join(" -> ", messages);
    }
}
