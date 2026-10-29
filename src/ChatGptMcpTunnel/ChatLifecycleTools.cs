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
    public const string GlobalUri = "ui://chatgpt-mcp-tunnel/lifecycle-global-v0219";
    public const string ThreadUri = "ui://chatgpt-mcp-tunnel/lifecycle-thread-v0219";
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
        "teardown", "ui-error", "tool-call", "visibility-change"
    };

    [McpServerTool(Name = "chat.hub.open", Title = "ChatGPT MCP Tunnel · Global Hub (diagnostic)", ReadOnly = true)]
    [McpMeta("ui", JsonValue = """{"resourceUri":"ui://chatgpt-mcp-tunnel/lifecycle-global-v0219","visibility":["app"]}""")]
    [McpMeta("openai/ui", JsonValue = """{"entrypoints":[{"type":"global"}]}""")]
    [Description("Experimental global MCP Apps lifecycle entrypoint. It records technical handshake events only; it does not send user messages or replace the existing relay.")]
    public static string OpenHub()
    {
        WriteEvent("global", "tool-open", "server", "{}");
        return JsonSerializer.Serialize(new {ok=true,mode="global",resource=GlobalUri});
    }

    [McpServerTool(Name = "chat.thread.open", Title = "ChatGPT MCP Tunnel · Thread Relay (diagnostic)", ReadOnly = true)]
    [McpMeta("ui", JsonValue = """{"resourceUri":"ui://chatgpt-mcp-tunnel/lifecycle-thread-v0219","visibility":["app"]}""")]
    [McpMeta("openai/ui", JsonValue = """{"entrypoints":[{"type":"thread"}]}""")]
    [Description("Experimental thread MCP Apps lifecycle entrypoint. It records technical handshake events only; it does not send user messages or replace the existing relay.")]
    public static string OpenThread()
    {
        WriteEvent("thread", "tool-open", "server", "{}");
        return JsonSerializer.Serialize(new {ok=true,mode="thread",resource=ThreadUri});
    }

    [McpServerTool(Name = "chat.lifecycle.event", Title = "ChatGPT MCP Tunnel · Lifecycle event")]
    [McpMeta("ui", JsonValue = """{"visibility":["app"]}""")]
    [Description("Records one sanitized UI lifecycle probe event for diagnostics. No message content, credentials, cookies, or URLs are stored.")]
    public static string Record(string mode, string kind, string instance_id, string details_json = "{}")
    {
        if (mode is not ("global" or "thread") || !Kinds.Contains(kind) ||
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
    private static string Load(string mode)
    {
        ChatLifecycleTools.WriteEvent(mode, "resource-read", "server", "{}");
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
                "ChatLifecycleApp.html"), Encoding.UTF8)
            .Replace("__LIFECYCLE_MODE__", mode, StringComparison.Ordinal);
    }

    [McpServerResource(UriTemplate = ChatLifecycleTools.GlobalUri, Name = "chat_lifecycle_global_v0219", MimeType = "text/html;profile=mcp-app")]
    [Description("Diagnostic Global MCP Apps lifecycle view; no messaging.")]
    public static string Global() => Load("global");

    [McpServerResource(UriTemplate = ChatLifecycleTools.ThreadUri, Name = "chat_lifecycle_thread_v0219", MimeType = "text/html;profile=mcp-app")]
    [Description("Diagnostic Thread MCP Apps lifecycle view; no messaging.")]
    public static string Thread() => Load("thread");
}
