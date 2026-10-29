using System.Collections.Concurrent;
using System.ComponentModel;
using System.Net.WebSockets;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

// Router Lite: one MCP stdio server routes to local methods or named WebSocket agents.
public static class RouterLite
{
    private sealed class Packet
    {
        public string? id { get; set; }
        public string? tool { get; set; }
        public JsonElement arguments { get; set; } = JsonSerializer.SerializeToElement(new { });
        public bool? success { get; set; }
        public string? result { get; set; }
        public List<WireBlock>? blocks { get; set; }
        public string? error { get; set; }
    }
    private sealed class WireBlock
    {
        public string type { get; set; } = "";
        public string? text { get; set; }
        public string? mime_type { get; set; }
        public string? data { get; set; }
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
    public static bool IsConnected(string computer) => Sessions.ContainsKey(computer);
    public static string LocalComputer => Environment.MachineName;
    public static bool IsRemote(string? computer) =>
        !string.IsNullOrWhiteSpace(computer) &&
        !string.Equals(computer, LocalComputer, StringComparison.OrdinalIgnoreCase);

    private static readonly Lazy<IReadOnlyDictionary<string, MethodInfo>> WindowsMethods = new(() =>
    {
        var map = new Dictionary<string, MethodInfo>(StringComparer.Ordinal);
        foreach (var group in new[] { typeof(ComputerTools), typeof(DesktopTools), typeof(UiAutomationTools) })
        foreach (var method in group.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            var attribute = method.GetCustomAttribute<McpServerToolAttribute>();
            if (attribute?.Name is { } name && !map.TryAdd(name, method))
                throw new InvalidOperationException($"Duplicate tool {name}");
        }
        if (map.Count != 23) throw new InvalidOperationException($"Expected 23 Windows tools, got {map.Count}");
        return map;
    });

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

    public static Task<string> RemoteSystemInfoAsync(string computer, bool includeInstalledPrograms) =>
        CallStringAsync(computer, "system.info", new { include_installed_programs = includeInstalledPrograms });

    public static async Task<string> CallStringAsync(string computer, string tool, object arguments)
    {
        var answer = await CallAsync(computer, tool, arguments);
        return answer.result ?? throw new InvalidDataException($"Remote '{tool}' did not return a text result.");
    }

    public static async Task<IEnumerable<ContentBlock>> CallBlocksAsync(string computer, string tool, object arguments)
    {
        var answer = await CallAsync(computer, tool, arguments);
        if (answer.blocks is null) throw new InvalidDataException($"Remote '{tool}' did not return content blocks.");
        return answer.blocks.Select(block => block.type switch
        {
            "text" => (ContentBlock)new TextContentBlock { Text = block.text ?? "" },
            "image" => ImageContentBlock.FromBytes(Convert.FromBase64String(block.data ?? ""), block.mime_type ?? "image/png"),
            _ => throw new InvalidDataException($"Unsupported content block: {block.type}")
        }).ToArray();
    }

    private static async Task<Packet> CallAsync(string computer, string tool, object arguments)
    {
        if (!WindowsMethods.Value.ContainsKey(tool) && !FileTransferTools.IsWireTool(tool)) throw new InvalidOperationException($"Unknown Windows tool: {tool}");
        if (!Sessions.TryGetValue(computer, out var session))
            throw new InvalidOperationException($"Computer '{computer}' is not connected. Call computers.list.");
        var requestId = Guid.NewGuid().ToString("N");
        var completion = new TaskCompletionSource<Packet>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!session.Pending.TryAdd(requestId, completion)) throw new InvalidOperationException("Duplicate request ID.");
        try
        {
            var request = new Packet { id = requestId, tool = tool, arguments = JsonSerializer.SerializeToElement(arguments) };
            await SendPacketAsync(session.Socket, request, session.WriteGate, CancellationToken.None);
            // shell.exec may intentionally take up to 3600 s. Do not mistake that for a transport failure.
            var waitSeconds = tool == "shell.exec" && request.arguments.TryGetProperty("timeout_seconds", out var seconds)
                && seconds.TryGetInt32(out var timeout) ? Math.Clamp(timeout + 20, 45, 3620) : 45;
            var reply = await completion.Task.WaitAsync(TimeSpan.FromSeconds(waitSeconds));
            if (reply.success != true) throw new InvalidOperationException(reply.error ?? $"Computer '{computer}' returned an error.");
            return reply;
        }
        catch (TimeoutException)
        {
            throw new TimeoutException($"Computer '{computer}' did not answer {tool}, request {requestId}; execution state unknown.");
        }
        finally { session.Pending.TryRemove(requestId, out _); }
    }

    public static async Task ServeAsync(CancellationToken stoppingToken)
    {
        var token = Environment.GetEnvironmentVariable("CHATGPT_MCP_AGENT_TOKEN");
        if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("CHATGPT_MCP_AGENT_TOKEN is required.");
        var address = Environment.GetEnvironmentVariable("CHATGPT_MCP_ROUTER_LISTEN") ?? "http://127.0.0.1:55201";
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
        var tlsPfx = Environment.GetEnvironmentVariable("CHATGPT_MCP_ROUTER_TLS_PFX");
        if (string.IsNullOrWhiteSpace(tlsPfx))
            builder.WebHost.UseUrls(address);
        else
        {
            var tlsPassword = Environment.GetEnvironmentVariable("CHATGPT_MCP_ROUTER_TLS_PASSWORD")
                ?? throw new InvalidOperationException("TLS certificate password is required.");
            var uri = new Uri(address);
            if (uri.Scheme != "http" || !IPAddress.TryParse(uri.Host, out var ip))
                throw new InvalidOperationException("Router listen address must be a local IPv4 when TLS is enabled.");
            var securePortText = Environment.GetEnvironmentVariable("CHATGPT_MCP_ROUTER_TLS_PORT") ?? "55202";
            if (!int.TryParse(securePortText, out var securePort) ||
                securePort is < 1024 or > 65535 || securePort == uri.Port)
                throw new InvalidOperationException("Invalid secure router port.");
            builder.WebHost.ConfigureKestrel(server =>
            {
                server.Listen(ip, uri.Port, endpoint => endpoint.Protocols = HttpProtocols.Http1);
                server.Listen(ip, securePort, endpoint =>
                {
                    endpoint.Protocols = HttpProtocols.Http1;
                    endpoint.UseHttps(tlsPfx, tlsPassword);
                });
            });
        }
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
                context.Response.StatusCode = 400; return;
            }
            if (name.Equals(LocalComputer, StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 409; return;
            }
            if (!CryptographicOperations.FixedTimeEquals(
                    SHA256.HashData(Encoding.UTF8.GetBytes(token)),
                    SHA256.HashData(Encoding.UTF8.GetBytes(supplied))))
            {
                context.Response.StatusCode = 401; return;
            }
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
            catch (Exception e) when (e is WebSocketException or OperationCanceledException or IOException or JsonException)
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
        Console.Error.WriteLine($"Router Lite listening at {address}/agent (MCP remains on stdio)." +
            (string.IsNullOrWhiteSpace(tlsPfx) ? "" : " WSS listener enabled on TCP 55202."));
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
        var expectedPin = Environment.GetEnvironmentVariable("CHATGPT_MCP_ROUTER_TLS_SHA256");
        byte[]? pinnedHash = null;
        if (uri.Scheme == "wss")
        {
            if (expectedPin is null || expectedPin.Length != 64 ||
                !expectedPin.All(Uri.IsHexDigit))
                throw new InvalidOperationException("WSS requires a 64-digit SHA256 certificate fingerprint.");
            pinnedHash = Convert.FromHexString(expectedPin);
        }
        // Warm up reflection mapping before accepting remote commands.
        _ = WindowsMethods.Value;
        using var desktopGate = new SemaphoreSlim(1, 1);
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var socket = new ClientWebSocket();
                if (pinnedHash is not null)
                {
                    socket.Options.RemoteCertificateValidationCallback = (_, certificate, _, _) =>
                    {
                        if (certificate is null) return false;
                        var presented = SHA256.HashData(certificate.GetRawCertData());
                        return CryptographicOperations.FixedTimeEquals(pinnedHash, presented);
                    };
                }
                socket.Options.SetRequestHeader("X-Computer-Id", id);
                socket.Options.SetRequestHeader("X-Agent-Token", token);
                await socket.ConnectAsync(uri, cancellationToken);
                Console.Error.WriteLine($"Router Lite agent '{id}' connected.");
                using var gate = new SemaphoreSlim(1, 1);
                var running = new List<Task>();
                try
                {
                    while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                    {
                        var payload = await ReceiveAsync(socket, cancellationToken);
                        if (payload is null) break;
                        var request = JsonSerializer.Deserialize<Packet>(payload, Json);
                        if (request?.id is null) continue;
                        // Non-GUI commands may run concurrently; GUI commands are serialized per session.
                        running.RemoveAll(t => t.IsCompleted);
                        running.Add(Task.Run(() => ExecuteRequestAsync(request, socket, gate, desktopGate, cancellationToken)));
                    }
                }
                finally
                {
                    // Ensure no task can send on a disposed socket or semaphore.
                    await Task.WhenAll(running);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
            catch (Exception e) { Console.Error.WriteLine($"Router Lite agent: {e.GetType().Name}: {e.Message}"); }
            if (!cancellationToken.IsCancellationRequested)
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
        }
    }

    private static async Task ExecuteRequestAsync(Packet request, WebSocket socket, SemaphoreSlim gate,
        SemaphoreSlim desktopGate, CancellationToken cancellationToken)
    {
        var isDesktop = request.tool?.StartsWith("desktop.", StringComparison.Ordinal) == true;
        var entered = false;
        Packet reply;
        try
        {
            if (isDesktop) { await desktopGate.WaitAsync(cancellationToken); entered = true; }
            var output = await InvokeLocalAsync(request.tool ?? "", request.arguments);
            reply = output is IEnumerable<ContentBlock> blocks
                ? new Packet { id = request.id, success = true, blocks = blocks.Select(WireContent).ToList() }
                : new Packet { id = request.id, success = true, result = output as string };
        }
        catch (Exception e)
        {
            reply = new Packet { id = request.id, success = false, error = e.GetBaseException().Message };
        }
        finally { if (entered) desktopGate.Release(); }
        try { await SendPacketAsync(socket, reply, gate, cancellationToken); }
        catch (InvalidOperationException e) when (e.Message.Contains("exceeds 16 MiB", StringComparison.Ordinal))
        {
            // Return a bounded error rather than tearing down the command channel.
            await SendPacketAsync(socket, new Packet
            {
                id = request.id, success = false,
                error = "Result exceeds Router Lite 16 MiB limit; request a smaller result."
            }, gate, cancellationToken);
        }
        catch (Exception e) when (e is WebSocketException or IOException or OperationCanceledException or ObjectDisposedException)
        {
            Console.Error.WriteLine($"Router Lite agent: failed to send reply ({e.GetType().Name}).");
        }
    }

    private static WireBlock WireContent(ContentBlock block) => block switch
    {
        TextContentBlock text => new WireBlock { type = "text", text = text.Text },
        ImageContentBlock image => new WireBlock
        {
            type = "image",
            mime_type = image.MimeType,
            data = Encoding.UTF8.GetString(image.Data.Span)
        },
        _ => throw new NotSupportedException($"Unsupported MCP block: {block.GetType().Name}")
    };

    private static async Task<object?> InvokeLocalAsync(string name, JsonElement arguments)
    {
        if (FileTransferTools.IsWireTool(name)) return await FileTransferTools.HandleWireAsync(name, arguments);
        if (!WindowsMethods.Value.TryGetValue(name, out var method))
            throw new NotSupportedException($"Unsupported Windows tool: {name}");
        var parameters = method.GetParameters();
        var values = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];
            if (p.Name == "computer") { values[i] = null; continue; } // never route again
            if (arguments.ValueKind == JsonValueKind.Object && arguments.TryGetProperty(p.Name!, out var raw))
                values[i] = raw.Deserialize(p.ParameterType, Json);
            else if (p.HasDefaultValue) values[i] = p.DefaultValue;
            else throw new ArgumentException($"Missing required argument '{p.Name}' for '{name}'.");
        }
        object? output;
        try { output = method.Invoke(null, values); }
        catch (TargetInvocationException e) { throw e.InnerException ?? e; }
        if (output is Task task)
        {
            await task;
            return task.GetType().GetProperty("Result")?.GetValue(task);
        }
        return output;
    }

    private static async Task SendPacketAsync(WebSocket socket, Packet packet, SemaphoreSlim gate, CancellationToken ct)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(packet, Json);
        if (bytes.Length > MaxMessageBytes) throw new InvalidOperationException("Agent packet exceeds 16 MiB.");
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
            if (stream.Length > MaxMessageBytes) throw new InvalidDataException("Packet exceeds 16 MiB.");
        } while (!received.EndOfMessage);
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}

public sealed class RouterLiteHost : IHostedService
{
    private CancellationTokenSource? _cancel;
    private Task? _server;
    private Task? _controlPipe;
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (Environment.GetEnvironmentVariable("CHATGPT_MCP_ROUTER_ENABLED") != "1")
            return Task.CompletedTask;
        _cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _server = Task.Run(() => RouterLite.ServeAsync(_cancel.Token), CancellationToken.None);
        _controlPipe = Task.Run(() => TransferControlPipe.ServeAsync(_cancel.Token), CancellationToken.None);
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
        if (_controlPipe is not null)
        {
            try { await _controlPipe.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken); }
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
