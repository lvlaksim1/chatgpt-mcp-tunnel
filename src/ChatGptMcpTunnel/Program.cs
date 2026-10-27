using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

// Standalone MCP server. JSON-RPC goes exclusively to stdout; diagnostics to stderr.
var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});
builder.Services.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly();
await builder.Build().RunAsync();

[McpServerToolType]
public static class TunnelTools
{
    [McpServerTool]
    [Description("Read-only connection test for the independent ChatGPT MCP Tunnel project. No file access or process execution.")]
    public static string bridge_ping() => "CHATGPT_MCP_TUNNEL_PING_OK";
}
