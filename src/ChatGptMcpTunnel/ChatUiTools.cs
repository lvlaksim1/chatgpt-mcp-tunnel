using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ModelContextProtocol.Server;

[McpServerToolType]
public static class ChatUiTools
{
    public const string ResourceUri = "ui://chatgpt-mcp-tunnel/relay";
    private static string Root => Environment.GetEnvironmentVariable("CHATGPT_MCP_CHAT_TEST_ROOT") ??
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChatGPTMCPAgent", "Chat");
    private static string MessageDir => Path.Combine(Root, "messages");
    private static string ForwardedDir => Path.Combine(Root, "ui-forwarded");
    private static readonly Regex IdPattern = new("^[a-f0-9]{32}$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly object AckGate = new();

    [McpServerTool(Name = "chat.ui.open", Title = "Чат · Включить MCP-посредник", ReadOnly = true)]
    [McpMeta("ui", JsonValue = """{"resourceUri":"ui://chatgpt-mcp-tunnel/relay","visibility":["model","app"]}""")]
    [McpMeta("openai/ui", JsonValue = """{"entrypoints":[{"type":"global"},{"type":"thread"}]}""")]
    [Description("Открыть встроенное MCP-приложение, которое проверяет входящие сообщения из локального окна ChatGPT MCP Tunnel и отправляет их через ui/message в текущий обычный чат. В самом компоненте требуется явно включить пересылку. Если ChatGPT закрывает компонент, пересылка прекращается. Это экспериментальная проверка без API, Codex, Work и планировщика.")]
    public static string Open() => JsonSerializer.Serialize(new
    {
        ok = true, mode = "experimental_mcp_apps_ui_message",
        resource = ResourceUri,
        note = "Откройте компонент и нажмите «Начать передачу». Работа возможна только пока ChatGPT удерживает компонент активным."
    });

    [McpServerTool(Name = "chat.ui.pending", Title = "Чат · Проверить неотправленное", ReadOnly = true)]
    [McpMeta("ui", JsonValue = """{"visibility":["app"]}""")]
    [Description("Внутренний инструмент MCP Apps: показать одно ожидающее и ещё не переправленное в ChatGPT сообщение, без изменения его состояния.")]
    public static string Pending()
    {
        if (!Directory.Exists(MessageDir))
            return JsonSerializer.Serialize(new { ok = true, count = 0, message = (object?)null });

        var candidates = new List<RelayMessage>();
        foreach (var file in Directory.EnumerateFiles(MessageDir, "*.json"))
        {
            try
            {
                var item = JsonSerializer.Deserialize<RelayMessage>(File.ReadAllText(file));
                if (item is not null && IdPattern.IsMatch(item.Id) &&
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

    [McpServerTool(Name = "chat.ui.ack", Title = "Чат · Подтвердить пересылку")]
    [McpMeta("ui", JsonValue = """{"visibility":["app"]}""")]
    [Description("Внутренний инструмент MCP Apps: подтвердить успешную отправку ui/message. Передача отмечается только после успешного ответа хоста. Не меняет само сообщение, вложения или ответ модели.")]
    public static string Ack(string message_id)
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
    [Description("Встроенный компонент MCP Apps для отправки сообщений локального окна через ui/message.")]
    public static string Relay() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ChatRelayApp.html"), Encoding.UTF8);
}
