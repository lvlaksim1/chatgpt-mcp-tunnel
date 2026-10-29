using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ModelContextProtocol.Server;

[McpServerToolType]
public static class ChatUiTools
{
    public const string ResourceUri = "ui://chatgpt-mcp-tunnel/relay-v0218";
    private static string Root => Environment.GetEnvironmentVariable("CHATGPT_MCP_CHAT_TEST_ROOT") ??
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChatGPTMCPAgent", "Chat");
    private static string MessageDir => Path.Combine(Root, "messages");
    private static string ForwardedDir => Path.Combine(Root, "ui-forwarded");
    private static string SessionDir => Path.Combine(Root, "sessions");
    private static string ActiveChatPath => Path.Combine(Root, "active-chat.txt");
    private static string RoutesDir => Path.Combine(Root, "ui-routes");
    private const string LegacyId = "00000000000000000000000000000000";
    private static readonly Regex IdPattern = new("^[a-f0-9]{32}$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly object AckGate = new();

    [McpServerTool(Name = "chat.ui.open", Title = "Р§Р°С‚ В· Р’РєР»СЋС‡РёС‚СЊ MCP-РїРѕСЃСЂРµРґРЅРёРє", ReadOnly = true)]
    [McpMeta("ui", JsonValue = """{"resourceUri":"ui://chatgpt-mcp-tunnel/relay-v0218","visibility":["model","app"]}""")]
    [Description("РћС‚РєСЂС‹С‚СЊ РІСЃС‚СЂРѕРµРЅРЅРѕРµ MCP-РїСЂРёР»РѕР¶РµРЅРёРµ, РєРѕС‚РѕСЂРѕРµ РїСЂРѕРІРµСЂСЏРµС‚ РІС…РѕРґСЏС‰РёРµ СЃРѕРѕР±С‰РµРЅРёСЏ РёР· Р»РѕРєР°Р»СЊРЅРѕРіРѕ РѕРєРЅР° ChatGPT MCP Tunnel Рё РѕС‚РїСЂР°РІР»СЏРµС‚ РёС… С‡РµСЂРµР· ui/message РІ С‚РµРєСѓС‰РёР№ РѕР±С‹С‡РЅС‹Р№ С‡Р°С‚. Р’ СЃР°РјРѕРј РєРѕРјРїРѕРЅРµРЅС‚Рµ С‚СЂРµР±СѓРµС‚СЃСЏ СЏРІРЅРѕ РІРєР»СЋС‡РёС‚СЊ РїРµСЂРµСЃС‹Р»РєСѓ. Р•СЃР»Рё ChatGPT Р·Р°РєСЂС‹РІР°РµС‚ РєРѕРјРїРѕРЅРµРЅС‚, РїРµСЂРµСЃС‹Р»РєР° РїСЂРµРєСЂР°С‰Р°РµС‚СЃСЏ. Р­С‚Рѕ СЌРєСЃРїРµСЂРёРјРµРЅС‚Р°Р»СЊРЅР°СЏ РїСЂРѕРІРµСЂРєР° Р±РµР· API, Codex, Work Рё РїР»Р°РЅРёСЂРѕРІС‰РёРєР°.")]
    public static string Open(string chat_id = "")
    {
        var id = IdPattern.IsMatch(chat_id ?? "") ? chat_id : DefaultChatId();
        TouchRoute(id!, "ready");
        return JsonSerializer.Serialize(new
    {
        ok = true, mode = "mcp_apps_multi_chat",
        resource = ResourceUri,
        chat_id = id,
        note = "РљРѕРјРїРѕРЅРµРЅС‚ РїСЂРёРІСЏР·С‹РІР°РµС‚СЃСЏ Рє РІС‹Р±СЂР°РЅРЅРѕРјСѓ Р»РѕРєР°Р»СЊРЅРѕРјСѓ С‡Р°С‚Сѓ; РґР»СЏ РЅРѕРІРѕРіРѕ РґРёР°Р»РѕРіР° РїРµСЂРµРґР°Р№С‚Рµ chat_id. РќР°С‡РЅРёС‚Рµ РїРµСЂРµРґР°С‡Сѓ."
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
            var threshold = value.Status == "waiting" ? 120 : 45;
            return DateTimeOffset.UtcNow - value.LastSeenUtc < TimeSpan.FromSeconds(threshold);
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException) {
            return false;
        }
    }

    private static string TitleFor(string id)
    {
        try {
            var path = Path.Combine(SessionDir, id + ".json");
            if (File.Exists(path)) {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                return doc.RootElement.TryGetProperty("Title", out var title) ? title.GetString() ?? "Р§Р°С‚" : "Р§Р°С‚";
            }
        } catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) { }
        return id == LegacyId ? "РСЃС‚РѕСЂРёСЏ РґРѕ РѕР±РЅРѕРІР»РµРЅРёСЏ" : "РќРѕРІС‹Р№ С‡Р°С‚";
    }

    [McpServerTool(Name = "chat.ui.pending", Title = "Р§Р°С‚ В· РџСЂРѕРІРµСЂРёС‚СЊ РЅРµРѕС‚РїСЂР°РІР»РµРЅРЅРѕРµ", ReadOnly = true)]
    [McpMeta("ui", JsonValue = """{"visibility":["app"]}""")]
    [Description("Р’РЅСѓС‚СЂРµРЅРЅРёР№ РёРЅСЃС‚СЂСѓРјРµРЅС‚ MCP Apps: РїРѕРєР°Р·Р°С‚СЊ РѕРґРЅРѕ РѕР¶РёРґР°СЋС‰РµРµ Рё РµС‰С‘ РЅРµ РїРµСЂРµРїСЂР°РІР»РµРЅРЅРѕРµ РІ ChatGPT СЃРѕРѕР±С‰РµРЅРёРµ, Р±РµР· РёР·РјРµРЅРµРЅРёСЏ РµРіРѕ СЃРѕСЃС‚РѕСЏРЅРёСЏ.")]
    public static string Pending(string chat_id = "", bool discover = false)
    {
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

        var candidates = new List<RelayMessage>();
        foreach (var file in Directory.EnumerateFiles(MessageDir, "*.json"))
        {
            try
            {
                var item = JsonSerializer.Deserialize<RelayMessage>(File.ReadAllText(file));
                if (item is not null && IdPattern.IsMatch(item.Id) &&
                    (discover
                        ? (item.ChatId ?? LegacyId) != chat_id &&
                          !RouteAvailable(item.ChatId ?? LegacyId)
                        : (item.ChatId ?? LegacyId) == chat_id) &&
                    item.Status == "pending" && (item.Text?.Length ?? 0) <= 20000 &&
                    !File.Exists(Path.Combine(ForwardedDir, item.Id.ToLowerInvariant() + ".json")))
                    candidates.Add(item);
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) { }
        }

        var first = candidates.OrderBy(x => x.CreatedUtc).FirstOrDefault();
        return JsonSerializer.Serialize(new
        {
            ok = true,
            count = candidates.Count,
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

    [McpServerTool(Name = "chat.ui.ack", Title = "Р§Р°С‚ В· РџРѕРґС‚РІРµСЂРґРёС‚СЊ РїРµСЂРµСЃС‹Р»РєСѓ")]
    [McpMeta("ui", JsonValue = """{"visibility":["app"]}""")]
    [Description("Р’РЅСѓС‚СЂРµРЅРЅРёР№ РёРЅСЃС‚СЂСѓРјРµРЅС‚ MCP Apps: РїРѕРґС‚РІРµСЂРґРёС‚СЊ СѓСЃРїРµС€РЅСѓСЋ РѕС‚РїСЂР°РІРєСѓ ui/message. РџРµСЂРµРґР°С‡Р° РѕС‚РјРµС‡Р°РµС‚СЃСЏ С‚РѕР»СЊРєРѕ РїРѕСЃР»Рµ СѓСЃРїРµС€РЅРѕРіРѕ РѕС‚РІРµС‚Р° С…РѕСЃС‚Р°. РќРµ РјРµРЅСЏРµС‚ СЃР°РјРѕ СЃРѕРѕР±С‰РµРЅРёРµ, РІР»РѕР¶РµРЅРёСЏ РёР»Рё РѕС‚РІРµС‚ РјРѕРґРµР»Рё.")]
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
                            if (!File.Exists(route))
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
    [Description("Р’СЃС‚СЂРѕРµРЅРЅС‹Р№ РєРѕРјРїРѕРЅРµРЅС‚ MCP Apps РґР»СЏ РѕС‚РїСЂР°РІРєРё СЃРѕРѕР±С‰РµРЅРёР№ Р»РѕРєР°Р»СЊРЅРѕРіРѕ РѕРєРЅР° С‡РµСЂРµР· ui/message.")]
    public static string Relay() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ChatRelayApp.html"), Encoding.UTF8);
}
