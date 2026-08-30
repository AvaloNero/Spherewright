using System.Diagnostics;
using BepInEx.Logging;
using Spherewright.Bridge.Core.Abstractions;
using Spherewright.Contracts.Protocol;
using Spherewright.Plugin.Bootstrap;
using Spherewright.Plugin.Game;
using Spherewright.Plugin.RuntimeDescriptor;
using Spherewright.Plugin.Transport;

namespace Spherewright.Plugin.Hosting;

internal sealed class SpherewrightBridgeHost : IDisposable
{
    private readonly SpherewrightConfiguration _configuration;
    private readonly ManualLogSource _logger;
    private readonly GameVersionSnapshotProvider _gameVersionProvider;
    private readonly GameSessionTracker _sessionTracker;
    private readonly NormalGameActionCoordinator _normalActionCoordinator;
    private readonly ResearchResultAutoAcknowledger _researchResultAutoAcknowledger;
    private readonly BridgeStatusSnapshotProvider _statusProvider;
    private readonly BoundedMainThreadDispatcher _dispatcher;
    private readonly RuntimeDescriptorPublisher _descriptorPublisher;
    private readonly NamedPipeBridgeServer _pipeServer;
    private readonly BridgeRuntimeDescriptor _descriptor;
    private bool _started;
    private bool _pumpLogged;
    private int _framesSinceVersionRefresh;

    private SpherewrightBridgeHost(
        SpherewrightConfiguration configuration,
        ManualLogSource logger,
        GameVersionSnapshotProvider gameVersionProvider,
        GameSessionTracker sessionTracker,
        NormalGameActionCoordinator normalActionCoordinator,
        ResearchResultAutoAcknowledger researchResultAutoAcknowledger,
        BridgeStatusSnapshotProvider statusProvider,
        BoundedMainThreadDispatcher dispatcher,
        RuntimeDescriptorPublisher descriptorPublisher,
        NamedPipeBridgeServer pipeServer,
        BridgeRuntimeDescriptor descriptor)
    {
        _configuration = configuration;
        _logger = logger;
        _gameVersionProvider = gameVersionProvider;
        _sessionTracker = sessionTracker;
        _normalActionCoordinator = normalActionCoordinator;
        _researchResultAutoAcknowledger = researchResultAutoAcknowledger;
        _statusProvider = statusProvider;
        _dispatcher = dispatcher;
        _descriptorPublisher = descriptorPublisher;
        _pipeServer = pipeServer;
        _descriptor = descriptor;
    }

    public static SpherewrightBridgeHost Create(
        SpherewrightConfiguration configuration,
        ManualLogSource logger,
        string pluginVersion)
    {
        var identity = BridgeIdentity.Create(configuration.PipeNamePrefix);
        var versionProvider = new GameVersionSnapshotProvider();
        var gameVersion = versionProvider.CaptureOnMainThread();
        var statusProvider = new BridgeStatusSnapshotProvider(
            identity.BridgeInstanceId,
            pluginVersion,
            gameVersion,
            configuration.AllowWrites);
        var dispatcher = new BoundedMainThreadDispatcher(configuration.MaxMainThreadQueue);
        var sessionTracker = new GameSessionTracker(configuration.AllowWrites, gameVersion, logger);
        var gameStateReader = new GameStateReader(sessionTracker);
        var normalActionCoordinator = new NormalGameActionCoordinator(
            configuration.PlanTokenLifetimeSeconds,
            configuration.MaxIdempotencyEntriesPerSession,
            sessionTracker,
            gameStateReader);
        var researchResultAutoAcknowledger = new ResearchResultAutoAcknowledger(
            configuration.AutoAcknowledgeResearchResults,
            logger);
        var testWorldCoordinator = new TestWorldCoordinator(
            configuration.AllowWrites,
            configuration.PlanTokenLifetimeSeconds,
            configuration.MaxIdempotencyEntriesPerSession,
            sessionTracker);
        var descriptorPublisher = new RuntimeDescriptorPublisher(configuration.RuntimeDescriptorDirectory, logger);
        var pipeServer = new NamedPipeBridgeServer(
            identity,
            pluginVersion,
            configuration.MaxFrameBytes,
            configuration.ReadRequestTimeoutSeconds,
            statusProvider,
            dispatcher,
            gameStateReader,
            testWorldCoordinator,
            normalActionCoordinator,
            logger);
        var descriptor = new BridgeRuntimeDescriptor
        {
            ProcessId = Process.GetCurrentProcess().Id,
            BridgeInstanceId = identity.BridgeInstanceId,
            PipeName = identity.PipeName,
            AuthToken = identity.AuthToken,
            ProtocolVersion = ProtocolConstants.CurrentVersion,
            PluginVersion = pluginVersion,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        return new SpherewrightBridgeHost(
            configuration,
            logger,
            versionProvider,
            sessionTracker,
            normalActionCoordinator,
            researchResultAutoAcknowledger,
            statusProvider,
            dispatcher,
            descriptorPublisher,
            pipeServer,
            descriptor);
    }

    public void Start()
    {
        if (_started)
        {
            throw new InvalidOperationException("Spherewright bridge host is already started.");
        }

        try
        {
            _pipeServer.Start();
            _descriptorPublisher.Publish(_descriptor);
            _started = true;
        }
        catch
        {
            _pipeServer.Dispose();
            _descriptorPublisher.Dispose();
            throw;
        }
    }

    public void PumpMainThread()
    {
        if (!_started)
        {
            return;
        }

        _sessionTracker.UpdateOnMainThread();
        _statusProvider.UpdateGameLoadedOnMainThread(_sessionTracker.GameLoaded);
        _normalActionCoordinator.UpdateOnMainThread();
        _researchResultAutoAcknowledger.UpdateOnMainThread();

        _dispatcher.Pump(
            _configuration.MaxRequestsPerFrame,
            TimeSpan.FromMilliseconds(_configuration.FrameBudgetMs));

        _framesSinceVersionRefresh++;
        if (_framesSinceVersionRefresh >= 120)
        {
            _framesSinceVersionRefresh = 0;
            _statusProvider.UpdateGameVersionOnMainThread(_gameVersionProvider.CaptureOnMainThread());
        }

        if (!_pumpLogged)
        {
            _pumpLogged = true;
            _logger.LogInfo("Spherewright main-thread pump active");
        }
    }

    public void Dispose()
    {
        if (!_started)
        {
            return;
        }

        _started = false;
        _pipeServer.Dispose();
        _descriptorPublisher.Dispose();
        _dispatcher.Dispose();
    }
}
