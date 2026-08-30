using System.Diagnostics;
using Spherewright.Mcp.Tools;
using Xunit;

namespace Spherewright.Mcp.Tests;

public sealed class McpProcessLoggingTests
{
    [Fact]
    public async Task StartupAndShutdown_DoNotWriteLogsToStdout()
    {
        var assemblyPath = typeof(SpherewrightTools).Assembly.Location;
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{assemblyPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start Spherewright.Mcp.");
        await Task.Delay(300);
        process.StandardInput.Close();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(true);
            throw;
        }

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();

        Assert.True(process.ExitCode == 0, stderr);
        Assert.Equal(string.Empty, stdout);
    }
}
