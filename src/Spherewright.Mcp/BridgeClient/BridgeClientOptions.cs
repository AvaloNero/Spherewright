namespace Spherewright.Mcp.BridgeClient;

internal sealed class BridgeClientOptions
{
    public string? ExplicitDescriptorPath { get; init; }

    public string? EnvironmentDescriptorPath { get; init; }

    public string RuntimeDirectory { get; init; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Spherewright",
        "runtime");

    public int ConnectTimeoutMilliseconds { get; init; } = 5000;

    public int RequestTimeoutSeconds { get; init; } = 10;

    public static BridgeClientOptions FromArgs(string[] args)
    {
        string? explicitPath = null;
        for (var index = 0; index < args.Length; index++)
        {
            if (!string.Equals(args[index], "--bridge-descriptor", StringComparison.Ordinal))
            {
                continue;
            }

            if (index + 1 >= args.Length || string.IsNullOrWhiteSpace(args[index + 1]))
            {
                throw new ArgumentException("--bridge-descriptor requires a path.", nameof(args));
            }

            explicitPath = args[++index];
        }

        return new BridgeClientOptions
        {
            ExplicitDescriptorPath = explicitPath,
            EnvironmentDescriptorPath = Environment.GetEnvironmentVariable("SPHEREWRIGHT_BRIDGE_DESCRIPTOR"),
        };
    }
}

