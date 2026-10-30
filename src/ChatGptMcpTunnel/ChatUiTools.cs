using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ModelContextProtocol.Server;

[McpServerToolType]
public static class ChatUiTools
{
    public const string ResourceUri = "ui://chatgpt-mcp-tunnel/relay-v0237";
    public const string LegacyV0235ResourceUri = "ui://chatgpt-mcp-tunnel/relay-v0235";
    public const string LegacyV0232ResourceUri = "ui://chatgpt-mcp-tunnel/relay-v0232";
    public const string LegacyV0231ResourceUri = "ui://chatgpt-mcp-tunnel/relay-v0231";
    public const string LegacyResourceUri = "ui://chatgpt-mcp-tunnel/relay-v0218";
    private static string Root => Environment.GetEnvironmentVariable("CHATGPT_MCP_CHAT_TEST_ROOT") ??
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChatGPTMCPAgent", "Chat");
    private static string MessageDir => Path.Combine(Root, "messages");
    private static string ForwardedDir => Path.Combine(Root, "ui-forwarded");
    private static string BrowserClaimDir => Path.Combine(Root, "browser-claims");
    private static string SessionDir => Path.Combine(Root, "sessions");
    private static string ActiveChatPath => Path.Combine(Root, "active-chat.txt");
    private static string RoutesDir => Path.Combine(Root, "ui-routes");
    private const string LegacyId = "00000000000000000000000000000000";
    private static readonly Regex IdPattern = new("^[a-f0-9]{32}$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly object AckGate = new();

    [McpServerTool(Name = "chat.ui.open", Title = "ChatGPT MCP Tunnel - Chat relay", ReadOnly = true)]
    [McpMeta("ui", JsonValue = """{"resourceUri":"ui://chatgpt-mcp-tunnel/relay-v0237","visibility":["model","app"]}""")]
    [Description("Открывает ретранслятор MCP Apps для сообщений из локального чата. Используется в обычной беседе ChatGPT; после активации отправляет запросы через ui/message.")]
    public static string Open(string chat_id = "")
    {
        var id = IdPattern.IsMatch(chat_id ?? "") ? chat_id : DefaultChatId();
        TouchRoute(id!, "ready");
        ChatThreadBindings.BeginCapture(id!);
        return JsonSerializer.Serialize(new
    {
        ok = true, mode = "mcp_apps_multi_chat",
        resource = ResourceUri,
        chat_id = id,

        note = "MCP Apps начнёт передачу автоматически после открытия компонента хостом ChatGPT."
    });
    }

    private static string DefaultChatId() => LegacyId;

    private sealed class RouteState
    {
        public string Status { get; set; } = "";
        public DateTimeOffset LastSeenUtc { get; set; }
    }
    private static string RoutePath(string id) => Path.Combine(RoutesDir, id + ".json");
    private static void TouchRoute(string id, string status)
    {
        if (!IdPattern.IsMatch(id)) return;
        lock (AckGate)
        {
            Directory.CreateDirectory(RoutesDir);
            var path = RoutePath(id);
            var tmp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(tmp, JsonSerializer.Serialize(new RouteState
                {
                    Status = status, LastSeenUtc = DateTimeOffset.UtcNow
                }));
                File.Move(tmp, path, overwrite: true);
            }
            finally { if (File.Exists(tmp)) File.Delete(tmp); }
        }
    }
    private static bool RouteAvailable(string id)
    {
        try
        {
            var path = RoutePath(id);
            if (!File.Exists(path)) return false;
            var value = JsonSerializer.Deserialize<RouteState>(File.ReadAllText(path));
            if (value is null) return false;
            var threshold = value.Status == "creating" ? 86400 :
                value.Status == "waiting" ? 120 : 45;
            return DateTimeOffset.UtcNow - value.LastSeenUtc < TimeSpan.FromSeconds(threshold);
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException) {
            return false;
        }
    }

    private static string TitleFor(string id)
    {
        try
        {
            var path = Path.Combine(SessionDir, id + ".json");
            if (File.Exists(path))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                if (doc.RootElement.TryGetProperty("Title", out var title))
                    return title.GetString() ?? "Чат";
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) { }
        return id == LegacyId ? "Общий чат" : "Новый чат";
    }

    private static string HistoryFor(string chatId, string? excludeMessageId)
    {
        // Reconstruct logical chat context from local answered turns.
        // Only recent turns are sent, with hard bounds on every field.
        if (!IdPattern.IsMatch(chatId) || !Directory.Exists(MessageDir)) return "";
        var answered = new List<RelayMessage>();
        foreach (var file in Directory.EnumerateFiles(MessageDir, "*.json"))
        {
            try
            {
                var item = JsonSerializer.Deserialize<RelayMessage>(File.ReadAllText(file));
                if (item is null || (item.ChatId ?? LegacyId) != chatId ||
                    item.Id == excludeMessageId || item.Status != "answered" ||
                    string.IsNullOrWhiteSpace(item.Reply)) continue;
                answered.Add(item);
            }
            catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException) { }
        }
        var recent = answered.OrderBy(x => x.CreatedUtc).TakeLast(5);
        var sb = new StringBuilder();
        foreach (var turn in recent)
        {
            if (sb.Length > 3600) break;
            var input = turn.Text ?? "";
            var reply = turn.Reply ?? "";
            sb.Append("Пользователь: ");
            sb.Append(input.Length > 700 ? input[..700] : input);
            sb.Append("\nАссистент: ");
            sb.Append(reply.Length > 700 ? reply[..700] : reply);
            sb.Append("\n---\n");
        }
        return sb.Length > 4200 ? sb.ToString(0,4200) : sb.ToString();
    }

    private static bool ExplicitNewCloudChat(string chatId)
    {
        try
        {
            var path = Path.Combine(SessionDir, chatId + ".json");
            if (!File.Exists(path)) return false;
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.TryGetProperty("CreateCloudThread", out var flag) &&
                flag.ValueKind == JsonValueKind.True;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        { return false; }
    }

    private static bool CloudThreadAlreadyRequested(string chatId)
    {
        if (!Directory.Exists(ForwardedDir)) return false;
        // If one message was sent from this local chat, never create another
        // cloud dialog automatically even if the binding heartbeat expires.
        foreach (var path in Directory.EnumerateFiles(ForwardedDir, "*.json"))
        {
            try
            {
                var id = Path.GetFileNameWithoutExtension(path);
                if (!IdPattern.IsMatch(id)) continue;
                var messagePath = Path.Combine(MessageDir, id + ".json");
                if (!File.Exists(messagePath)) continue;
                var message = JsonSerializer.Deserialize<RelayMessage>(File.ReadAllText(messagePath));
                if (string.Equals(message?.ChatId ?? LegacyId, chatId,
                    StringComparison.OrdinalIgnoreCase)) return true;
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) { }
        }
        return false;
    }

    private static bool BrowserTransportActive()
    {
        var path=Path.Combine(Root,"browser-bridge","heartbeat.json");
        try
        {
            if (!File.Exists(path)) return false;
            using var doc=JsonDocument.Parse(File.ReadAllText(path));
            if (!doc.RootElement.TryGetProperty("mode",out var mode) ||
                mode.GetString()!="browser" ||
                !doc.RootElement.TryGetProperty("at",out var date) ||
                !date.TryGetDateTimeOffset(out var utc)) return false;
            return (DateTimeOffset.UtcNow-utc).Duration()<TimeSpan.FromSeconds(12);
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or JsonException)
        { return false; }
    }

    [McpServerTool(Name = "chat.ui.pending", Title = "ChatGPT MCP Tunnel · Проверить очередь", ReadOnly = true)]
    [McpMeta("ui", JsonValue = """{"visibility":["app"]}""")]
    [Description("Инструмент MCP Apps для просмотра ожидающих сообщений локального чата без подтверждения доставки.")]
    public static string Pending(string chat_id = "", bool discover = false)
    {
        // Only one transport may own the queue at a time.
        if (BrowserTransportActive())
            return JsonSerializer.Serialize(new
            { ok=true, count=0, transport="browser", message=(object?)null });
        // Pre-0.2.17 widgets do not send chat_id. Restrict them to the
        // legacy conversation, never silently consume another chat.
        if (string.IsNullOrEmpty(chat_id)) chat_id = LegacyId;
        if (!discover && IdPattern.IsMatch(chat_id))
        {
            // Heartbeat of the iframe serving this ChatGPT conversation.
            if (!RouteAvailable(chat_id)) TouchRoute(chat_id, "ready");
        }
        if (!string.IsNullOrEmpty(chat_id) && !IdPattern.IsMatch(chat_id))
            return JsonSerializer.Serialize(new { ok = false, error = "invalid_chat_id" });
        if (!Directory.Exists(MessageDir))
            return JsonSerializer.Serialize(new { ok = true, count = 0, message = (object?)null });

        // Atomic discovery lease: only one open MCP iframe may create a cloud
        // thread for a newly-created local chat. An uncertain host reply must
        // never cause a second cloud dialog to be created behind the user's back.
        lock (AckGate)
        {
        var candidates = new List<RelayMessage>();
        foreach (var file in Directory.EnumerateFiles(MessageDir, "*.json"))
        {
            try
            {
                var item = JsonSerializer.Deserialize<RelayMessage>(File.ReadAllText(file));
                if (item is not null && IdPattern.IsMatch(item.Id) &&
                    (discover
                        ? (item.ChatId ?? LegacyId) != chat_id &&
                          ExplicitNewCloudChat(item.ChatId ?? LegacyId) &&
                          !RouteAvailable(item.ChatId ?? LegacyId) &&
                          !CloudThreadAlreadyRequested(item.ChatId ?? LegacyId)
                        : (item.ChatId ?? LegacyId) == chat_id) &&
                    item.Status == "pending" && (item.Text?.Length ?? 0) <= 20000 &&
                    !File.Exists(Path.Combine(ForwardedDir, item.Id.ToLowerInvariant() + ".json")) &&
                    !File.Exists(Path.Combine(BrowserClaimDir, item.Id.ToLowerInvariant() + ".json")))
                    candidates.Add(item);
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) { }
        }

        var first = candidates.OrderBy(x => x.CreatedUtc).FirstOrDefault();
        if (discover && first is not null)
            TouchRoute(first.ChatId ?? LegacyId, "creating");
        return JsonSerializer.Serialize(new
        {
            ok = true,
            count = candidates.Count,
            context = first is not null ? HistoryFor(first.ChatId ?? LegacyId, first.Id) : "",
            message = first is null ? null : new
            {
                id = first.Id,
                chat_id = first.ChatId ?? LegacyId,
                chat_title = TitleFor(first.ChatId ?? LegacyId),
                text = first.Text,
                created_utc = first.CreatedUtc,
                attachments = (first.Attachments ?? new()).Select(a => new
                {
                    id = a.Id, name = a.Name, size_bytes = a.SizeBytes,
                    media_type = a.MediaType, sha256 = a.Sha256
                })
            } as object
        });
        }
    }


    [McpServerTool(Name = "chat.ui.ack", Title = "ChatGPT MCP Tunnel · Подтвердить доставку")]
    [McpMeta("ui", JsonValue = """{"visibility":["app"]}""")]
    [Description("Подтверждает успешную передачу локального сообщения в ui/message. Не отправляет сообщение повторно.")]
    public static string Ack(string message_id, bool new_chat = false)
    {
        if (!IdPattern.IsMatch(message_id ?? ""))
            return JsonSerializer.Serialize(new { ok = false, error = "invalid_message_id" });

        lock (AckGate)
        {
            var id = message_id!.ToLowerInvariant();
            if (!File.Exists(Path.Combine(MessageDir, id + ".json")))
                return JsonSerializer.Serialize(new { ok = false, error = "message_not_found" });
            Directory.CreateDirectory(ForwardedDir);
            var target = Path.Combine(ForwardedDir, id + ".json");
            if (File.Exists(target))
                return JsonSerializer.Serialize(new { ok = true, already_acknowledged = true });
            var temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temp, JsonSerializer.Serialize(new
                {
                    message_id = id, forwarded_utc = DateTimeOffset.UtcNow,
                    transport = "mcp_apps_ui_message"
                }), new UTF8Encoding(false));
                File.Move(temp, target);
                if (new_chat)
                {
                    try {
                        var message = JsonSerializer.Deserialize<RelayMessage>(File.ReadAllText(Path.Combine(MessageDir, id + ".json")));
                        if (message is not null && IdPattern.IsMatch(message.ChatId ?? ""))
                        {
                            // A new ChatGPT conversation is starting. Suppress duplicate
                            // bootstrap while waiting for chat.ui.open(chat_id).
                            Directory.CreateDirectory(RoutesDir);
                            var route = RoutePath(message.ChatId!);
                            File.WriteAllText(route, JsonSerializer.Serialize(new RouteState {
                                Status="waiting", LastSeenUtc=DateTimeOffset.UtcNow
                            }));
                        }
                    } catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) { }
                }
                return JsonSerializer.Serialize(new { ok = true, message_id = id });
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }
    }

    private sealed class RelayMessage
    {
        public string Id { get; set; } = "";
        public string? ChatId { get; set; }
        public DateTimeOffset CreatedUtc { get; set; }
        public string Text { get; set; } = "";
        public string Status { get; set; } = "pending";
        public string Reply { get; set; } = "";
        public List<RelayAttachment> Attachments { get; set; } = new();
    }
    private sealed class RelayAttachment
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string MediaType { get; set; } = "";
        public long SizeBytes { get; set; }
        public string Sha256 { get; set; } = "";
    }
}

[McpServerResourceType]
public static class ChatUiResources
{
    [McpServerResource(UriTemplate = ChatUiTools.ResourceUri,
        Name = "chat_ui_relay", MimeType = "text/html;profile=mcp-app")]
    [Description("Интерактивная доставка локальных сообщений в обычный ChatGPT через MCP Apps.")]
    public static string Relay() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ChatRelayApp.html"), Encoding.UTF8);

    [McpServerResource(UriTemplate = ChatUiTools.LegacyV0235ResourceUri,
        Name = "chat_ui_relay_legacy_v0235", MimeType = "text/html;profile=mcp-app")]
    [Description("Compatibility alias for v0.2.35 MCP Apps widgets.")]
    public static string RelayLegacyV0235() => Relay();

    [McpServerResource(UriTemplate = ChatUiTools.LegacyV0232ResourceUri,
        Name = "chat_ui_relay_legacy_v0232", MimeType = "text/html;profile=mcp-app")]
    [Description("Compatibility alias for v0.2.32 MCP Apps widgets.")]
    public static string RelayLegacyV0232() => Relay();

    [McpServerResource(UriTemplate = ChatUiTools.LegacyV0231ResourceUri,
        Name = "chat_ui_relay_legacy_v0231", MimeType = "text/html;profile=mcp-app")]
    [Description("Compatibility alias for v0.2.31 MCP Apps widgets.")]
    public static string RelayLegacyV0231() => Relay();

    [McpServerResource(UriTemplate = ChatUiTools.LegacyResourceUri,
        Name = "chat_ui_relay_legacy_v0218", MimeType = "text/html;profile=mcp-app")]
    [Description("Compatibility alias for existing v0.2.18 MCP Apps widgets.")]
    public static string RelayLegacy() => Relay();
}
