using Spherewright.Contracts.Errors;
using Spherewright.Contracts.Protocol;
using Spherewright.Mcp.BridgeClient;
using Xunit;

namespace Spherewright.Mcp.Tests;

public sealed class BridgeDescriptorLocatorTests : IDisposable
{
    private readonly string _temporaryDirectory = Path.Combine(
        Path.GetTempPath(),
        "Spherewright.Mcp.Tests",
        Guid.NewGuid().ToString("N"));

    public BridgeDescriptorLocatorTests()
    {
        Directory.CreateDirectory(_temporaryDirectory);
    }

    [Fact]
    public void ExplicitDescriptor_TakesPriorityOverEnvironmentDescriptor()
    {
        var explicitPath = WriteDescriptor("explicit.json", 123);
        var environmentPath = WriteDescriptor("environment.json", 456);
        var locator = new BridgeDescriptorLocator(
            new BridgeClientOptions
            {
                ExplicitDescriptorPath = explicitPath,
                EnvironmentDescriptorPath = environmentPath,
                RuntimeDirectory = _temporaryDirectory,
            },
            processId => processId == 123,
            _ => true);

        var result = locator.Locate();

        Assert.True(result.Success);
        Assert.Equal(123, result.Value!.Descriptor.ProcessId);
    }

    [Fact]
    public void AutoDiscovery_RejectsMultipleLiveDescriptors()
    {
        WriteDescriptor("bridge-1.json", 1);
        WriteDescriptor("bridge-2.json", 2);
        var locator = new BridgeDescriptorLocator(
            new BridgeClientOptions { RuntimeDirectory = _temporaryDirectory },
            _ => true,
            _ => true);

        var result = locator.Locate();

        Assert.False(result.Success);
        Assert.Equal(BridgeErrorCodes.BridgeNotReady, result.Error!.Code);
    }

    [Fact]
    public void MissingRuntimeDirectory_ReturnsStructuredNotReadyError()
    {
        var locator = new BridgeDescriptorLocator(
            new BridgeClientOptions { RuntimeDirectory = Path.Combine(_temporaryDirectory, "missing") },
            _ => true,
            _ => true);

        var result = locator.Locate();

        Assert.False(result.Success);
        Assert.Equal(BridgeErrorCodes.BridgeNotReady, result.Error!.Code);
        Assert.True(result.Error.Retryable);
        Assert.DoesNotContain(_temporaryDirectory, result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, true);
        }
    }

    private string WriteDescriptor(string fileName, int processId)
    {
        var path = Path.Combine(_temporaryDirectory, fileName);
        var descriptor = new BridgeRuntimeDescriptor
        {
            ProcessId = processId,
            BridgeInstanceId = Guid.NewGuid().ToString("N"),
            PipeName = "test-pipe",
            AuthToken = "test-token",
            ProtocolVersion = ProtocolConstants.CurrentVersion,
            PluginVersion = "0.1.0",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        File.WriteAllText(path, McpBridgeJson.Serialize(descriptor));
        return path;
    }
}

