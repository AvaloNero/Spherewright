using BepInEx.Configuration;
using Spherewright.Contracts.Protocol;

namespace Spherewright.Plugin.Bootstrap;

internal sealed class SpherewrightConfiguration
{
    private SpherewrightConfiguration()
    {
    }

    public bool Enabled { get; private set; }

    public string PipeNamePrefix { get; private set; } = "Spherewright";

    public int MaxConnections { get; private set; }

    public int MaxQueuedRequests { get; private set; }

    public int MaxInFlightRequests { get; private set; }

    public int MaxMainThreadQueue { get; private set; }

    public int MaxRequestsPerFrame { get; private set; }

    public int FrameBudgetMs { get; private set; }

    public int MaxFrameBytes { get; private set; }

    public int ReadRequestTimeoutSeconds { get; private set; }

    public int CommitWaitTimeoutSeconds { get; private set; }

    public bool RequireCurrentUserAcl { get; private set; }

    public string RuntimeDescriptorDirectory { get; private set; } = string.Empty;

    public bool RotateBridgeTokenOnStart { get; private set; }

    public bool AllowWrites { get; private set; }

    public bool RequirePeacefulSave { get; private set; }

    public int PlanTokenLifetimeSeconds { get; private set; }

    public int IdempotencyRetentionMinutes { get; private set; }

    public int MaxIdempotencyEntriesPerSession { get; private set; }

    public bool AutoAcknowledgeResearchResults { get; private set; }

    public static SpherewrightConfiguration Load(ConfigFile config)
    {
        if (config is null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        var result = new SpherewrightConfiguration
        {
            Enabled = config.Bind("Bridge", "Enabled", true, "Enable the local Spherewright bridge.").Value,
            PipeNamePrefix = config.Bind("Bridge", "PipeNamePrefix", "Spherewright", "Prefix for the randomized local Named Pipe.").Value,
            MaxConnections = config.Bind("Bridge", "MaxConnections", 1, "Maximum authenticated Pipe connections.").Value,
            MaxQueuedRequests = config.Bind("Bridge", "MaxQueuedRequests", 64, "Maximum queued bridge requests.").Value,
            MaxInFlightRequests = config.Bind("Bridge", "MaxInFlightRequests", 8, "Maximum in-flight requests per connection.").Value,
            MaxMainThreadQueue = config.Bind("Bridge", "MaxMainThreadQueue", 32, "Maximum Unity main-thread work items.").Value,
            MaxRequestsPerFrame = config.Bind("Bridge", "MaxRequestsPerFrame", 4, "Maximum main-thread requests pumped per frame.").Value,
            FrameBudgetMs = config.Bind("Bridge", "FrameBudgetMs", 2, "Unity main-thread bridge budget in milliseconds.").Value,
            MaxFrameBytes = config.Bind("Bridge", "MaxFrameBytes", ProtocolConstants.DefaultMaxFrameBytes, "Maximum bridge frame payload size.").Value,
            ReadRequestTimeoutSeconds = config.Bind("Bridge", "ReadRequestTimeoutSeconds", 10, "Read request timeout in seconds.").Value,
            CommitWaitTimeoutSeconds = config.Bind("Bridge", "CommitWaitTimeoutSeconds", 15, "Commit result wait timeout in seconds.").Value,
            RequireCurrentUserAcl = config.Bind("Security", "RequireCurrentUserAcl", true, "Require current-user-only ACLs for Pipe and descriptor.").Value,
            RuntimeDescriptorDirectory = config.Bind("Security", "RuntimeDescriptorDirectory", "%LOCALAPPDATA%/Spherewright/runtime", "Directory used for protected runtime descriptors. Use forward slashes so BepInEx does not interpret backslash escapes.").Value,
            RotateBridgeTokenOnStart = config.Bind("Security", "RotateBridgeTokenOnStart", true, "Rotate the bridge token on each Plugin start.").Value,
            AllowWrites = config.Bind("Safety", "AllowWrites", false, "Allow explicitly committed game writes. The default remains read-only.").Value,
            RequirePeacefulSave = config.Bind("Safety", "RequirePeacefulSave", true, "Require confirmed peaceful mode before any future write.").Value,
            PlanTokenLifetimeSeconds = config.Bind("Safety", "PlanTokenLifetimeSeconds", 60, "Lifetime of a dry-run plan token in seconds.").Value,
            IdempotencyRetentionMinutes = config.Bind("Safety", "IdempotencyRetentionMinutes", 30, "Configured action-result retention window in minutes.").Value,
            MaxIdempotencyEntriesPerSession = config.Bind("Safety", "MaxIdempotencyEntriesPerSession", 1024, "Maximum cached idempotent action results per Plugin process.").Value,
            AutoAcknowledgeResearchResults = config.Bind(
                "Experience",
                "AutoAcknowledgeResearchResults",
                true,
                "Dismiss DSP's research-result modal through its native FadeOut flow after it becomes ready.").Value,
        };

        result.Validate();
        return result;
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(PipeNamePrefix)
            || PipeNamePrefix.Length > 32
            || PipeNamePrefix.Any(character => !char.IsLetterOrDigit(character) && character != '-' && character != '_' && character != '.'))
        {
            throw new InvalidOperationException("Bridge PipeNamePrefix contains unsupported characters.");
        }

        if (MaxConnections != 1)
        {
            throw new InvalidOperationException("Gate A supports exactly one authenticated Pipe connection.");
        }

        if (MaxQueuedRequests <= 0
            || MaxInFlightRequests <= 0
            || MaxMainThreadQueue <= 0
            || MaxRequestsPerFrame <= 0
            || FrameBudgetMs <= 0
            || MaxFrameBytes <= 0
            || MaxFrameBytes > ProtocolConstants.DefaultMaxFrameBytes
            || ReadRequestTimeoutSeconds <= 0
            || CommitWaitTimeoutSeconds <= 0)
        {
            throw new InvalidOperationException("One or more Bridge capacity settings are invalid.");
        }

        if (!RequireCurrentUserAcl)
        {
            throw new InvalidOperationException("Gate A does not allow disabling current-user ACL protection.");
        }

        if (!RotateBridgeTokenOnStart)
        {
            throw new InvalidOperationException("Gate A requires bridge token rotation on every start.");
        }

        if (string.IsNullOrWhiteSpace(RuntimeDescriptorDirectory))
        {
            throw new InvalidOperationException("RuntimeDescriptorDirectory is required.");
        }

        if (PlanTokenLifetimeSeconds <= 0
            || IdempotencyRetentionMinutes <= 0
            || MaxIdempotencyEntriesPerSession <= 0)
        {
            throw new InvalidOperationException("One or more Safety lifetime or capacity settings are invalid.");
        }
    }
}
