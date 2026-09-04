using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Spherewright.Mcp.BridgeClient;

var options = BridgeClientOptions.FromArgs(args);
var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole(consoleOptions =>
{
    consoleOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddSingleton(options);
builder.Services.AddSingleton<BridgeDescriptorLocator>();
builder.Services.AddSingleton<IBridgeClient, NamedPipeBridgeClient>();
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly()
    .WithResourcesFromAssembly();

await builder.Build().RunAsync().ConfigureAwait(false);
