using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ModelContextProtocol.Server;

// Experimental, non-destructive probe. Not a replacement for the accepted
// v0.2.18 chat.ui.open relay. Lifecycle events never contain chat messages.
[McpServerToolType]
public static class ChatLifecycleTools
{
    public const string GlobalUri = "ui://chatgpt-mcp-tunnel/hub-observer-v0229";
    public const string BootstrapUri = "ui://chatgpt-mcp-tunnel/chat-bootstrap-v0229";
    // Older connector snapshots in ChatGPT may still request these exact UI
    // pointers. Return the latest SAFE ordinary-chat bootstrap for both.
    public const string BootstrapLegacyV0224Uri = "ui://chatgpt-mcp-tunnel/chat-bootstrap-v0224";
    public const string BootstrapLegacyV0225Uri = "ui://chatgpt-mcp-tunnel/chat-bootstrap-v0225";
    public const string ThreadUri = ChatThreadRelayProbeTools.Uri;
    public const string LegacyThreadUri = "ui://chatgpt-mcp-tunnel/lifecycle-thread-v0220";
    private static string Root => Environment.GetEnvironmentVariable("CHATGPT_MCP_CHAT_TEST_ROOT") ??
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChatGPTMCPAgent", "Chat");
    private static string EventsDirectory => Path.Combine(Root, "lifecycle-probe");
    private static readonly object Sync = new();
    private static readonly Regex InstanceId = new("^[a-zA-Z0-9_-]{1,80}$");
    private static readonly HashSet<string> Kinds = new(StringComparer.Ordinal)
    {
        "tool-open", "resource-read", "ui-initialize", "ui-initialized",
        "tool-input", "tool-result", "host-context-changed", "heartbeat",
        "teardown", "ui-error", "tool-call", "visibility-change",
        "hub-forwarded", "hub-error"
    };

    [McpServerTool(Name = "chat.hub.open", Title = "ChatGPT MCP Tunnel · Global Hub (diagnostic)", ReadOnly = true)]
    [McpMeta("ui", JsonValue = """{"resourceUri":"ui://chatgpt-mcp-tunnel/hub-observer-v0229","visibility":["model","app"]}""")]
    [McpMeta("openai/ui", JsonValue = """{"entrypoints":[{"type":"global"}]}""")]
    [Description("Experimental global MCP Apps lifecycle entrypoint. It records technical handshake events only; it does not send user messages or replace the existing relay.")]
    public static string OpenHub()
    {
        WriteEvent("global", "tool-open", "server", "{}");
        return JsonSerializer.Serialize(new {ok=true,mode="global",resource=GlobalUri});
    }

    [McpServerTool(Name = "chat.bootstrap.open", Title = "ChatGPT MCP Tunnel · Ordinary Chat Transport", ReadOnly = true)]
    [McpMeta("ui", JsonValue = """{"resourceUri":"ui://chatgpt-mcp-tunnel/chat-bootstrap-v0229","visibility":["model","app"]}""")]
    [Description("Activate the automatic MCP Apps message dispatcher in an EXISTING NORMAL ChatGPT conversation. No Global or Work entrypoint. Creates new ordinary ChatGPT threads through ui/message target:new.")]
    public static string OpenChatBootstrap()
    {
        WriteEvent("chat", "tool-open", "server", """{"source":"chat"}""");
        return JsonSerializer.Serialize(new
        {
            ok = true, mode = "mcp_apps_multi_chat", resource = BootstrapUri,
            chat_id = "00000000000000000000000000000000"
        });
    }

    [McpServerTool(Name = "chat.thread.open", Title = "ChatGPT MCP Tunnel · Thread Relay (diagnostic)", ReadOnly = true)]
    [McpMeta("ui", JsonValue = """{"resourceUri":"ui://chatgpt-mcp-tunnel/thread-relay-probe-v0240","visibility":["model","app"]}""")]
    [McpMeta("openai/ui", JsonValue = """{"entrypoints":[{"type":"thread"}]}""")]
    [Description("Experimental thread MCP Apps lifecycle entrypoint. It records technical handshake events only; it does not send user messages or replace the existing relay.")]
    public static string OpenThread()
    {
        WriteEvent("thread", "tool-open", "server",
            """{"source":"thread-relay-probe"}""");
        return JsonSerializer.Serialize(new {
            ok=true,mode="thread",resource=ThreadUri,
            chat_id=ChatThreadRelayProbeTools.SelectedTestChatId()
        });
    }

    [McpServerTool(Name = "chat.lifecycle.event", Title = "ChatGPT MCP Tunnel · Lifecycle event")]
    [McpMeta("ui", JsonValue = """{"visibility":["app"]}""")]
    [Description("Records one sanitized UI lifecycle probe event for diagnostics. No message content, credentials, cookies, or URLs are stored.")]
    public static string Record(string mode, string kind, string instance_id, string details_json = "{}")
    {
        if (mode is not ("global" or "chat" or "thread") || !Kinds.Contains(kind) ||
            !InstanceId.IsMatch(instance_id ?? "") ||
            (details_json?.Length ?? 0) > 2000)
            return JsonSerializer.Serialize(new {ok=false,error="invalid_event"});

        try
        {
            // Accept only known scalar fields; never persist arbitrary host data.
            using var doc = JsonDocument.Parse(details_json ?? "{}");
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return JsonSerializer.Serialize(new {ok=false,error="invalid_details"});
            var allowed = new HashSet<string>(StringComparer.Ordinal)
            {
                "protocol", "host", "mode", "visibility", "displayMode",
                "hasMessage", "hasTools", "hasResources", "hasDeepLink",
                "hasOpenAiMessage", "hasContextUpdate", "pathKind",
                "source", "errorCode"
            };
            var sanitized = new Dictionary<string, object?>();
            foreach (var p in doc.RootElement.EnumerateObject())
            {
                if (!allowed.Contains(p.Name)) continue;
                if (p.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    sanitized[p.Name] = p.Value.GetBoolean();
                else if (p.Value.ValueKind == JsonValueKind.String)
                {
                    var str = p.Value.GetString() ?? "";
                    // Only short symbolic values; redact possible tokens and URLs.
                    if (str.Length <= 64 && !str.Contains("://") && !str.Contains('/') &&
                        !str.Contains('\\') && !str.Contains('=') && !str.Contains('?'))
                        sanitized[p.Name] = str;
                }
            }
            WriteEvent(mode, kind, instance_id!, JsonSerializer.Serialize(sanitized));
            return JsonSerializer.Serialize(new {ok=true});
        }
        catch (JsonException)
        {
            return JsonSerializer.Serialize(new {ok=false,error="invalid_json"});
        }
        catch (IOException)
        {
            return JsonSerializer.Serialize(new {ok=false,error="storage_error"});
        }
    }

    [McpServerTool(Name = "chat.lifecycle.status", Title = "ChatGPT MCP Tunnel · Lifecycle status", ReadOnly = true)]
    [McpMeta("ui", JsonValue = """{"visibility":["app"]}""")]
    [Description("Reads sanitized lifecycle probe records, last 50 events for diagnostics.")]
    public static string Status()
    {
        var path = Path.Combine(EventsDirectory, "events.jsonl");
        lock (Sync)
        {
            if (!File.Exists(path))
                return JsonSerializer.Serialize(new {ok=true,count=0,events=Array.Empty<object>()});
            // bounded display read; older data remains untouched
            var lines = File.ReadLines(path).TakeLast(50).ToArray();
            return JsonSerializer.Serialize(new {ok=true,count=lines.Length,events=lines.Select(x => JsonSerializer.Deserialize<JsonElement>(x)).ToArray()});
        }
    }

    internal static void WriteEvent(string mode, string kind, string instance, string details)
    {
        lock (Sync)
        {
            Directory.CreateDirectory(EventsDirectory);
            var line = JsonSerializer.Serialize(new
            {
                utc=DateTimeOffset.UtcNow,
                mode, kind, instance, details=JsonDocument.Parse(details).RootElement.Clone()
            });
            File.AppendAllText(Path.Combine(EventsDirectory, "events.jsonl"),
                line + Environment.NewLine, new UTF8Encoding(false));
        }
    }
}

[McpServerResourceType]
public static class ChatLifecycleResources
{
    private static string Load(string mode, string htmlFile)
    {
        ChatLifecycleTools.WriteEvent(mode, "resource-read", "server", "{}");
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
                htmlFile), Encoding.UTF8)
            .Replace("__LIFECYCLE_MODE__", mode, StringComparison.Ordinal);
    }

    [McpServerResource(UriTemplate = ChatLifecycleTools.GlobalUri, Name = "chat_hub_observer_v0225", MimeType = "text/html;profile=mcp-app")]
    [Description("Global MCP Apps status observer. Does not dispatch model requests.")]
    public static string Global() => Load("global", "ChatGlobalHubApp.html");

    [McpServerResource(UriTemplate = ChatLifecycleTools.BootstrapUri, Name = "chat_bootstrap_v0225", MimeType = "text/html;profile=mcp-app")]
    [Description("MCP Apps dispatcher invoked only from an ordinary ChatGPT conversation.")]
    public static string Bootstrap() => Load("chat", "ChatBootstrapApp.html");

    [McpServerResource(UriTemplate = ChatLifecycleTools.BootstrapLegacyV0224Uri, Name = "chat_bootstrap_legacy_v0224", MimeType = "text/html;profile=mcp-app")]
    [Description("Compatibility resource for old ChatGPT connector snapshots; serves the current protected ordinary-chat transport.")]
    public static string BootstrapLegacyV0224() => Load("chat", "ChatBootstrapApp.html");

    [McpServerResource(UriTemplate = ChatLifecycleTools.BootstrapLegacyV0225Uri, Name = "chat_bootstrap_legacy_v0225", MimeType = "text/html;profile=mcp-app")]
    [Description("Compatibility resource for old ChatGPT connector snapshots; serves the current protected ordinary-chat transport.")]
    public static string BootstrapLegacyV0225() => Load("chat", "ChatBootstrapApp.html");

    [McpServerResource(UriTemplate = ChatLifecycleTools.LegacyThreadUri, Name = "chat_lifecycle_thread_v0219", MimeType = "text/html;profile=mcp-app")]
    [Description("Diagnostic Thread MCP Apps lifecycle view; no messaging.")]
    public static string Thread() => Load("thread", "ChatLifecycleApp.html");
}
