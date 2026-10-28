using System.Collections.Concurrent;
using System.ComponentModel;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

// Router Lite: a single MCP endpoint dispatches to connected Windows agents.
// No separate database, broker or user-interface dependency.
public static class RouterLite
{
    private sealed class Packet
    {
        public string? id { get; set; }
        public string? tool { get; set; }
        public JsonElement arguments { get; set; } = JsonSerializer.SerializeToElement(new { });
        public bool? success { get; set; }
        public string? result { get; set; }
        public string? error { get; set; }
    }

    private sealed class Session(WebSocket socket)
    {
        public WebSocket Socket { get; } = socket;
        public SemaphoreSlim WriteGate { get; } = new(1, 1);
        public ConcurrentDictionary<string, TaskCompletionSource<Packet>> Pending { get; } = new();
    }

    private static readonly ConcurrentDictionary<string, Session> Sessions = new(StringComparer.OrdinalIgnoreCase);
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private const int MaxMessageBytes = 16 * 1024 * 1024;
    public static string LocalComputer => Environment.MachineName;

    public static string ComputersList()
    {
        var names = Sessions.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        return JsonSerializer.Serialize(new
        {
            local = LocalComputer,
            computers = new[] { new { computer = LocalComputer, connected = true, local = true } }
                .Concat(names.Where(x => !x.Equals(LocalComputer, StringComparison.OrdinalIgnoreCase))
                    .Select(x => new { computer = x, connected = true, local = false }))
                .ToArray()
        });
    }

    public static async Task<string> RemoteSystemInfoAsync(string computer, bool includeInstalledPrograms)
    {
        if (!Sessions.TryGetValue(computer, out var session))
            throw new InvalidOperationException($"Computer '{computer}' is not connected. Call computers.list.");
        var requestId = Guid.NewGuid().ToString("N");
        var pending = new TaskCompletionSource<Packet>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!session.Pending.TryAdd(requestId, pending)) throw new InvalidOperationException("Duplicate request ID.");
        try
        {
            var request = new Packet
            {
                id = requestId,
                tool = "system.info",
                arguments = JsonSerializer.SerializeToElement(new { include_installed_programs = includeInstalledPrograms })
            };
            await SendPacketAsync(session.Socket, request, session.WriteGate, CancellationToken.None);
            // Never silently resend commands after a timeout; the execution state might be unknown.
            var reply = await pending.Task.WaitAsync(TimeSpan.FromSeconds(40));
            if (reply.success != true)
                throw new InvalidOperationException(reply.error ?? "Remote tool failed.");
            return reply.result ?? "";
        }
        catch (TimeoutException)
        {
            throw new TimeoutException($"Computer '{computer}' did not answer request {requestId}; execution state unknown.");
        }
        finally { session.Pending.TryRemove(requestId, out _); }
    }

    public static async Task ServeAsync(CancellationToken stoppingToken)
    {
        var token = Environment.GetEnvironmentVariable("CHATGPT_MCP_AGENT_TOKEN");
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("CHATGPT_MCP_AGENT_TOKEN must be set to enable Router Lite.");
        var address = Environment.GetEnvironmentVariable("CHATGPT_MCP_ROUTER_LISTEN") ?? "http://127.0.0.1:55201";
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
        builder.WebHost.UseUrls(address);
        var app = builder.Build();
        app.UseWebSockets();
        app.Map("/agent", async context =>
        {
            if (!context.WebSockets.IsWebSocketRequest) { context.Response.StatusCode = 400; return; }
            var name = context.Request.Headers["X-Computer-Id"].ToString();
            var supplied = context.Request.Headers["X-Agent-Token"].ToString();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 80 ||
                name.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.')))
            {
                context.Response.StatusCode = 400;
                return;
            }
            if (name.Equals(LocalComputer, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 409;
                return;
            }
            var expectedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            var suppliedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(supplied));
            if (!CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes))
            {
                context.Response.StatusCode = 401;
                return;
            }
            // A second connection must not steal the route from an active session.
            if (Sessions.ContainsKey(name)) { context.Response.StatusCode = 409; return; }
            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            var session = new Session(socket);
            if (!Sessions.TryAdd(name, session))
            {
                await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Already connected", CancellationToken.None);
                return;
            }
            Console.Error.WriteLine($"Router Lite: {name} connected.");
            try
            {
                while (socket.State == WebSocketState.Open && !stoppingToken.IsCancellationRequested)
                {
                    var payload = await ReceiveAsync(socket, stoppingToken);
                    if (payload is null) break;
                    var reply = JsonSerializer.Deserialize<Packet>(payload, Json);
                    if (reply?.id is not null && session.Pending.TryRemove(reply.id, out var waiter))
                        waiter.TrySetResult(reply);
                }
            }
            catch (Exception e) when (e is WebSocketException or OperationCanceledException or IOException)
            {
                Console.Error.WriteLine($"Router Lite: {name} disconnected ({e.GetType().Name}).");
            }
            finally
            {
                if (Sessions.TryGetValue(name, out var current) && ReferenceEquals(current, session))
                    Sessions.TryRemove(name, out _);
                foreach (var (_, waiter) in session.Pending)
                    waiter.TrySetException(new IOException($"Computer '{name}' disconnected; execution state unknown."));
                session.WriteGate.Dispose();
            }
        });
        Console.Error.WriteLine($"Router Lite listening at {address}/agent (MCP remains on stdio).");
        await app.RunAsync(stoppingToken);
    }

    public static async Task RunAgentAsync(CancellationToken cancellationToken)
    {
        var id = Environment.GetEnvironmentVariable("CHATGPT_MCP_COMPUTER_ID") ?? LocalComputer;
        var token = Environment.GetEnvironmentVariable("CHATGPT_MCP_AGENT_TOKEN");
        var endpoint = Environment.GetEnvironmentVariable("CHATGPT_MCP_ROUTER_URL");
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(endpoint))
            throw new InvalidOperationException("Agent requires CHATGPT_MCP_AGENT_TOKEN and CHATGPT_MCP_ROUTER_URL.");
        var uri = new Uri(endpoint);
        if (uri.Scheme is not ("ws" or "wss")) throw new ArgumentException("Router URL must use ws:// or wss://.");
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var socket = new ClientWebSocket();
                socket.Options.SetRequestHeader("X-Computer-Id", id);
                socket.Options.SetRequestHeader("X-Agent-Token", token);
                await socket.ConnectAsync(uri, cancellationToken);
                Console.Error.WriteLine($"Router Lite agent '{id}' connected.");
                var gate = new SemaphoreSlim(1, 1);
                while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var payload = await ReceiveAsync(socket, cancellationToken);
                    if (payload is null) break;
                    var request = JsonSerializer.Deserialize<Packet>(payload, Json);
                    if (request?.id is null) continue;
                    Packet response;
                    try
                    {
                        var output = request.tool switch
                        {
                            "system.info" => ComputerTools.SystemInfoLocal(
                                request.arguments.ValueKind == JsonValueKind.Object &&
                                request.arguments.TryGetProperty("include_installed_programs", out var value) &&
                                value.ValueKind == JsonValueKind.True),
                            _ => throw new NotSupportedException($"Unsupported agent tool: {request.tool}")
                        };
                        response = new Packet { id = request.id, success = true, result = output };
                    }
                    catch (Exception e)
                    {
                        response = new Packet { id = request.id, success = false, error = e.Message };
                    }
                    await SendPacketAsync(socket, response, gate, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
            catch (Exception e) { Console.Error.WriteLine($"Router Lite agent: {e.GetType().Name}: {e.Message}"); }
            if (!cancellationToken.IsCancellationRequested)
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
        }
    }

    private static async Task SendPacketAsync(WebSocket socket, Packet packet, SemaphoreSlim gate, CancellationToken ct)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(packet, Json);
        if (bytes.Length > MaxMessageBytes) throw new InvalidOperationException("Agent response exceeds 16 MiB.");
        await gate.WaitAsync(ct);
        try { await socket.SendAsync(bytes.AsMemory(), WebSocketMessageType.Text, true, ct); }
        finally { gate.Release(); }
    }

    private static async Task<string?> ReceiveAsync(WebSocket socket, CancellationToken ct)
    {
        using var stream = new MemoryStream();
        var buffer = new byte[65536];
        WebSocketReceiveResult received;
        do
        {
            received = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            if (received.MessageType == WebSocketMessageType.Close) return null;
            if (received.MessageType != WebSocketMessageType.Text) throw new InvalidDataException("Text messages only.");
            stream.Write(buffer, 0, received.Count);
            if (stream.Length > MaxMessageBytes) throw new InvalidDataException("Message exceeds 16 MiB.");
        } while (!received.EndOfMessage);
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}

public sealed class RouterLiteHost : IHostedService
{
    private CancellationTokenSource? _cancel;
    private Task? _server;
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (Environment.GetEnvironmentVariable("CHATGPT_MCP_ROUTER_ENABLED") != "1")
            return Task.CompletedTask;
        _cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _server = Task.Run(() => RouterLite.ServeAsync(_cancel.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cancel is null) return;
        _cancel.Cancel();
        if (_server is not null)
        {
            try { await _server.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken); }
            catch (Exception e) when (e is OperationCanceledException or TimeoutException) { }
        }
        _cancel.Dispose();
    }
}

[McpServerToolType]
public static class RouterLiteTools
{
    [McpServerTool(Name = "computers.list")]
    [Description("Список компьютеров, подключённых к Router Lite. Локальный компьютер доступен всегда.")]
    public static string ComputersList() => RouterLite.ComputersList();
}
