using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using ModelContextProtocol.Server;

[McpServerToolType]
public static class ChatTools
{
    // Shared by the local WinForms agent and this server under the same Windows user.
    private static string MessagesDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ChatGPTMCPAgent", "Chat", "messages");

    [McpServerTool(Name = "chat.receive", Title = "Чат · Получить сообщения")]
    [Description("Категория «Чат»: получить входящие сообщения, написанные пользователем в окне ChatGPT MCP Tunnel. Можно ждать появления сообщения до 45 секунд. Если сообщений нет, возвращает пустой список. Не запускает новые пользовательские ходы ChatGPT.")]
    public static async Task<string> chat_receive(int wait_seconds = 0, int max_messages = 10)
    {
        var wait = Math.Clamp(wait_seconds, 0, 45);
        var take = Math.Clamp(max_messages, 1, 30);
        var deadline = DateTime.UtcNow.AddSeconds(wait);
        List<ChatRecord> pending;
        do
        {
            pending = ReadPending(take);
            if (pending.Count != 0 || DateTime.UtcNow >= deadline) break;
            await Task.Delay(500);
        } while (true);

        return JsonSerializer.Serialize(new
        {
            count = pending.Count,
            messages = pending.Select(m => new
            {
                id = m.Id,
                created_utc = m.CreatedUtc,
                text = m.Text
            })
        });
    }

    [McpServerTool(Name = "chat.reply", Title = "Чат · Отправить ответ")]
    [Description("Категория «Чат»: отправить ответ пользователю в окно ChatGPT MCP Tunnel. message_id — идентификатор из chat_receive, reply — готовый текст ответа. Ответ сохраняется локально и отображается в окне. Не отправляет никаких сообщений в ChatGPT.")]
    public static string chat_reply(string message_id, string reply)
    {
        if (string.IsNullOrWhiteSpace(message_id) ||
            !Regex.IsMatch(message_id, "^[a-f0-9]{32}$", RegexOptions.IgnoreCase))
            return JsonSerializer.Serialize(new { ok = false, error = "invalid_message_id" });
        if (string.IsNullOrWhiteSpace(reply) || reply.Length > 50000)
            return JsonSerializer.Serialize(new { ok = false, error = "invalid_reply" });

        var file = Path.Combine(MessagesDirectory, message_id.ToLowerInvariant() + ".json");
        if (!File.Exists(file))
            return JsonSerializer.Serialize(new { ok = false, error = "message_not_found" });

        try
        {
            var message = JsonSerializer.Deserialize<ChatRecord>(File.ReadAllText(file));
            if (message is null || message.Id != message_id ||
                message.Status is not ("pending" or "answered"))
                return JsonSerializer.Serialize(new { ok = false, error = "message_invalid" });
            if (message.Status == "answered")
                return JsonSerializer.Serialize(new { ok = true, already_answered = true });

            message.Reply = reply;
            message.RepliedUtc = DateTimeOffset.UtcNow;
            message.Status = "answered";
            var temp = file + ".response-" + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temp, JsonSerializer.Serialize(message));
                File.Move(temp, file, overwrite: true);
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
            return JsonSerializer.Serialize(new { ok = true, id = message_id });
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return JsonSerializer.Serialize(new { ok = false, error = ex.GetType().Name });
        }
    }

    private static List<ChatRecord> ReadPending(int max)
    {
        if (!Directory.Exists(MessagesDirectory)) return new List<ChatRecord>();
        var result = new List<ChatRecord>();
        foreach (var file in Directory.EnumerateFiles(MessagesDirectory, "*.json")
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var message = JsonSerializer.Deserialize<ChatRecord>(File.ReadAllText(file));
                if (message is not null && message.Status == "pending" &&
                    message.Id.Length == 32 && message.Text.Length <= 20000)
                    result.Add(message);
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                // A partially written message is retried at the next read.
            }
        }
        return result.OrderBy(m => m.CreatedUtc).Take(max).ToList();
    }

    private sealed class ChatRecord
    {
        public string Id { get; set; } = "";
        public DateTimeOffset CreatedUtc { get; set; }
        public string Text { get; set; } = "";
        public string Status { get; set; } = "pending";
        public string? Reply { get; set; }
        public DateTimeOffset? RepliedUtc { get; set; }
    }
}
