using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

// One binary runs as the MCP router (default) or as a remote Windows agent.
if (args.Length > 0 && args[0].Equals("--agent", StringComparison.OrdinalIgnoreCase))
{
    using var cancel = new CancellationTokenSource();
    Console.CancelKeyPress += (_, eventArgs) => { eventArgs.Cancel = true; cancel.Cancel(); };
    await RouterLite.RunAgentAsync(cancel.Token);
    return;
}

// JSON-RPC goes exclusively to stdout; diagnostics to stderr.
var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});
builder.Services.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly();
builder.Services.AddHostedService<RouterLiteHost>();
await builder.Build().RunAsync();

[McpServerToolType]
public static class TunnelTools
{
    [McpServerTool]
    [Description("Read-only connection test for the independent ChatGPT MCP Tunnel project. No file access or process execution.")]
    public static string bridge_ping() => "CHATGPT_MCP_TUNNEL_PING_OK";
}
