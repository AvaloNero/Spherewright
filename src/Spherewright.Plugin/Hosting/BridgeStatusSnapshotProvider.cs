using Spherewright.Contracts.Protocol;
using Spherewright.Contracts.Sessions;

namespace Spherewright.Plugin.Hosting;

internal sealed class BridgeStatusSnapshotProvider
{
    private readonly object _gate = new object();
    private readonly string _bridgeInstanceId;
    private readonly string _pluginVersion;
    private readonly bool _writesConfigured;
    private string _gameVersion;
    private bool _gameLoaded;

    public BridgeStatusSnapshotProvider(
        string bridgeInstanceId,
        string pluginVersion,
        string gameVersion,
        bool writesConfigured)
    {
        _bridgeInstanceId = bridgeInstanceId;
        _pluginVersion = pluginVersion;
        _gameVersion = gameVersion;
        _writesConfigured = writesConfigured;
    }

    public void UpdateGameVersionOnMainThread(string gameVersion)
    {
        lock (_gate)
        {
            _gameVersion = gameVersion;
        }
    }

    public void UpdateGameLoadedOnMainThread(bool gameLoaded)
    {
        lock (_gate)
        {
            _gameLoaded = gameLoaded;
        }
    }

    public BridgeStatus Capture()
    {
        lock (_gate)
        {
            return new BridgeStatus
            {
                BridgeConnected = true,
                BridgeInstanceId = _bridgeInstanceId,
                PluginVersion = _pluginVersion,
                ProtocolVersion = ProtocolConstants.CurrentVersion,
                GameVersion = _gameVersion,
                GameLoaded = _gameLoaded,
                WritesConfigured = _writesConfigured,
                WriteHealth = WriteHealthStates.Healthy,
            };
        }
    }
}
