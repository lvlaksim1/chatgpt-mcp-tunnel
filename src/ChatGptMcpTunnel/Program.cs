using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

// One binary runs as the MCP router (default), a remote Windows agent, or
// as a restricted local MCP extension for Goose.
if (args.Length > 0 && args[0].Equals("--transfer", StringComparison.OrdinalIgnoreCase))
{
    Environment.ExitCode = await TransferControlPipe.RunClientAsync(args.Skip(1).ToArray());
    return;
}
if (args.Length == 2 && args[0] == "--elevated-shell")
{
    Environment.ExitCode = await ElevatedShell.RunHelperAsync(args[1]);
    return;
}
if (args.Length == 1 && args[0] == "--test-uac-shell")
{
    Environment.ExitCode = await ElevatedShell.SelfTestAsync() ? 0 : 96;
    return;
}
if (args.Length > 0 && args[0].Equals("--agent", StringComparison.OrdinalIgnoreCase))
{
    using var cancel = new CancellationTokenSource();
    Console.CancelKeyPress += (_, eventArgs) => { eventArgs.Cancel = true; cancel.Cancel(); };
    await RouterLite.RunAgentAsync(cancel.Token);
    return;
}

var gooseProfile = args.Length == 2 &&
    args[0].Equals("--profile", StringComparison.OrdinalIgnoreCase) &&
    args[1].Equals("goose", StringComparison.OrdinalIgnoreCase);
if (args.Length > 0 && args[0].Equals("--profile", StringComparison.OrdinalIgnoreCase) && !gooseProfile)
{
    Console.Error.WriteLine("Supported restricted profile: --profile goose");
    Environment.ExitCode = 2;
    return;
}

if (gooseProfile)
{
    // Fail before accepting MCP connections if no explicit sandbox folder was given.
    GooseSafeTools.ValidateConfiguration();
}

// JSON-RPC goes exclusively to stdout; diagnostics to stderr.
// Do not feed --profile to the Microsoft host configuration parser.
var builder = Host.CreateApplicationBuilder(gooseProfile ? Array.Empty<string>() : args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

var mcp = builder.Services.AddMcpServer().WithStdioServerTransport();
if (gooseProfile)
{
    // Explicit allowlist: excludes shell, GitHub, UI automation, router and
    // all other unrestricted tools by construction, not by prompt instructions.
    mcp.WithTools<GooseSafeTools>();
}
else
{
    // Preserve existing ChatGPT MCP tool contracts and Router Lite behavior.
    mcp.WithToolsFromAssembly();
    mcp.WithResourcesFromAssembly();
    builder.Services.AddHostedService<RouterLiteHost>();
}
await builder.Build().RunAsync();

[McpServerToolType]
public static class TunnelTools
{
    [McpServerTool]
    [Description("Read-only connection test for the independent ChatGPT MCP Tunnel project. No file access or process execution.")]
    public static string bridge_ping() => "CHATGPT_MCP_TUNNEL_PING_OK";
}
