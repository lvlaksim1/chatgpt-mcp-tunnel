using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ModelContextProtocol.Server;

// Isolated Thread Entrypoint canary. Never discovers other chats.
[McpServerToolType]
public static class ChatThreadRelayProbeTools
{
    public const string Uri = "ui://chatgpt-mcp-tunnel/thread-relay-probe-v0240";
    private static readonly Regex ChatId = new("^[a-f0-9]{32}$",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);

    // Explicit local test chat selection; no fallback to personal messages.
    // This is used by the older, already registered chat.thread.open tool.
    internal static string SelectedTestChatId()
    {
        var location=Environment.GetEnvironmentVariable("CHATGPT_MCP_CHAT_TEST_ROOT") ??
          Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
           "ChatGPTMCPAgent","Chat");
        var file=Path.Combine(location,"thread-probe-chat-id.txt");
        try
        {
            var id=File.Exists(file)?File.ReadAllText(file).Trim():"";
            return ChatId.IsMatch(id)?id:"";
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return ""; }
    }

    [McpServerTool(Name="chat.thread.relay.probe", Title="ChatGPT MCP Tunnel · Thread Relay E2E probe", ReadOnly=true)]
    [McpMeta("ui", JsonValue = """{"resourceUri":"ui://chatgpt-mcp-tunnel/thread-relay-probe-v0240","visibility":["model","app"]}""")]
    [McpMeta("openai/ui",JsonValue = """{"entrypoints":[{"type":"thread"}]}""")]
    [Description("Launch a diagnostic MCP-only Thread relay for a single explicit test chat_id. It never touches other chats, and it runs only in a ChatGPT Host-created iframe.")]
    public static string Open(string chat_id)
    {
        if (!ChatId.IsMatch(chat_id ?? ""))
            return JsonSerializer.Serialize(new {ok=false,error="invalid_chat_id"});
        ChatLifecycleTools.WriteEvent("thread","tool-open","server",
            """{"source":"thread-relay-probe"}""");
        return JsonSerializer.Serialize(new {
            ok=true,mode="thread",resource=Uri,chat_id
        });
    }
}

[McpServerResourceType]
public static class ChatThreadRelayProbeResources
{
    [McpServerResource(UriTemplate=ChatThreadRelayProbeTools.Uri,
        Name="chat_thread_relay_probe_v0239",MimeType="text/html;profile=mcp-app")]
    [Description("Isolated MCP Apps Thread relay diagnostic. Polls only the explicitly bound local chat.")]
    public static string Probe()
    {
        ChatLifecycleTools.WriteEvent("thread","resource-read","server",
            """{"source":"thread-relay-probe"}""");
        return File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
            "ChatThreadRelayProbeApp.html"),Encoding.UTF8);
    }
}
