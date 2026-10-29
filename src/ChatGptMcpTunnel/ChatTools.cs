using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Runtime.Versioning;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

[McpServerToolType]
public static class ChatTools
{
    private static string ChatRoot => Environment.GetEnvironmentVariable("CHATGPT_MCP_CHAT_TEST_ROOT") ??
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ChatGPTMCPAgent", "Chat");
    private static string MessagesDirectory => Path.Combine(ChatRoot, "messages");
    private static string AttachmentsDirectory => Path.Combine(ChatRoot, "attachments");
    private static readonly Regex ValidId = new("^[a-f0-9]{32}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static string AsJson(object item) => JsonSerializer.Serialize(item);

    [McpServerTool(Name = "chat.receive", Title = "Чат · Получить сообщения")]
    [Description("Категория «Чат»: получить входящие сообщения пользователя. В ответе есть attachments с именем, размером, идентификатором и SHA-256. Чтобы прочитать текст файла или увидеть изображение, вызови chat.attachment.read с message_id и attachment_id. Можно ждать до 45 секунд. Не запускает новый ход ChatGPT.")]
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

        return AsJson(new
        {
            count = pending.Count,
            messages = pending.Select(m => new
            {
                id = m.Id,
                chat_id = m.ChatId ?? "00000000000000000000000000000000",
                created_utc = m.CreatedUtc,
                text = m.Text,
                attachments = (m.Attachments ?? new()).Select(a => new
                {
                    id = a.Id, name = a.Name, size_bytes = a.SizeBytes,
                    media_type = a.MediaType, sha256 = a.Sha256
                })
            })
        });
    }

    [SupportedOSPlatform("windows")]
    [McpServerTool(Name = "chat.attachment.read", Title = "Чат · Прочитать вложение")]
    [Description("Категория «Чат»: получить прикреплённый пользователем файл из окна чата. Возвращает настоящие блоки изображения для PNG/JPEG/GIF/BMP/WebP (уменьшенные при необходимости), текст для текстовых файлов; для крупного или двоичного документа — проверенный локальный путь для дальнейшей обработки через средства Windows. Аргументы: message_id из chat.receive и attachment_id из его attachments; max_chars только для текста (до 100000). Никогда не переносит большие двоичные файлы через текст MCP.")]
    public static IEnumerable<ContentBlock> chat_attachment_read(string message_id, string attachment_id, int max_chars = 60000)
    {
        List<ContentBlock> Error(string code) => [new TextContentBlock { Text = AsJson(new { ok = false, error = code }) }];
        if (!ValidId.IsMatch(message_id ?? "") || !ValidId.IsMatch(attachment_id ?? ""))
            return Error("invalid_id");
        var file = Path.Combine(MessagesDirectory, message_id!.ToLowerInvariant() + ".json");
        if (!File.Exists(file)) return Error("message_not_found");
        try
        {
            var message = JsonSerializer.Deserialize<ChatRecord>(File.ReadAllText(file));
            if (message is null || !string.Equals(message.Id, message_id, StringComparison.OrdinalIgnoreCase))
                return Error("message_invalid");
            var attachment = (message.Attachments ?? new()).FirstOrDefault(x =>
                string.Equals(x.Id, attachment_id, StringComparison.OrdinalIgnoreCase));
            if (attachment is null) return Error("attachment_not_found");
            var path = Path.Combine(AttachmentsDirectory, message_id.ToLowerInvariant(),
                attachment_id!.ToLowerInvariant());
            if (!File.Exists(path)) return Error("attachment_missing_on_disk");
            var info = new FileInfo(path);
            if (info.Length != attachment.SizeBytes) return Error("attachment_size_mismatch");

            if (!string.Equals(HashAttachment(path),
                attachment.Sha256, StringComparison.OrdinalIgnoreCase)) return Error("attachment_sha256_mismatch");
            var blocks = new List<ContentBlock>();
            var descriptor = new
            {
                ok = true, name = attachment.Name, media_type = attachment.MediaType,
                size_bytes = attachment.SizeBytes, sha256 = attachment.Sha256,
                local_path = path, note = "Вложение хранится только на сервере DOMKAT; для обработки двоичного документа используй локальный путь, не помещай его целиком в текст MCP."
            };
            blocks.Add(new TextContentBlock { Text = AsJson(descriptor) });

            if (attachment.MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                // Decode with Windows image codecs. Strict dimensions cap memory use.
                using var input = File.OpenRead(path);
                using var image = Image.FromStream(input, useEmbeddedColorManagement: false, validateImageData: true);
                if ((long)image.Width * image.Height > 50_000_000L)
                {
                    blocks.Add(new TextContentBlock { Text = AsJson(new { note = "image_too_many_pixels", width = image.Width, height = image.Height }) });
                    return blocks;
                }
                var scale = Math.Min(1.0, Math.Min(1600.0 / image.Width, 1600.0 / image.Height));
                var width = Math.Max(1, (int)(image.Width * scale));
                var height = Math.Max(1, (int)(image.Height * scale));
                using var output = new Bitmap(width, height);
                using (var graphics = Graphics.FromImage(output))
                {
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.DrawImage(image, 0, 0, width, height);
                }
                using var memory = new MemoryStream();
                output.Save(memory, ImageFormat.Png);
                blocks.Add(ImageContentBlock.FromBytes(memory.ToArray(), "image/png"));
            }
            else if (attachment.MediaType == "text/plain")
            {
                var cap = Math.Clamp(max_chars, 1, 100000);
                using var stream = File.OpenRead(path);
                var limit = Math.Min(stream.Length, Math.Min(4L * cap, 400000L));
                var bytes = new byte[(int)limit];
                var read = stream.Read(bytes);
                var text = Encoding.UTF8.GetString(bytes, 0, read);
                if (text.Length > cap) text = text[..cap];
                blocks.Add(new TextContentBlock
                {
                    Text = AsJson(new { text, truncated = stream.Length > read || text.Length >= cap })
                });
            }
            return blocks;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException
                                   or ArgumentException or OutOfMemoryException)
        {
            return Error("attachment_read_failed_" + ex.GetType().Name);
        }
    }

    [McpServerTool(Name = "chat.reply", Title = "Чат · Отправить ответ")]
    [Description("Категория «Чат»: отправить текст ответа пользователю в окно ChatGPT MCP Tunnel. message_id — идентификатор из chat.receive. Ответ сохраняется локально, не вызывает новый ход ChatGPT.")]
    public static string chat_reply(string message_id, string reply)
    {
        if (string.IsNullOrWhiteSpace(message_id) || !ValidId.IsMatch(message_id))
            return AsJson(new { ok = false, error = "invalid_message_id" });
        if (string.IsNullOrWhiteSpace(reply) || reply.Length > 50000)
            return AsJson(new { ok = false, error = "invalid_reply" });
        var file = Path.Combine(MessagesDirectory, message_id.ToLowerInvariant() + ".json");
        if (!File.Exists(file)) return AsJson(new { ok = false, error = "message_not_found" });

        try
        {
            var message = JsonSerializer.Deserialize<ChatRecord>(File.ReadAllText(file));
            if (message is null || !string.Equals(message.Id, message_id, StringComparison.OrdinalIgnoreCase) ||
                message.Status is not ("pending" or "answered"))
                return AsJson(new { ok = false, error = "message_invalid" });
            if (message.Status == "answered")
                return AsJson(new { ok = true, already_answered = true });
            message.Reply = reply;
            message.RepliedUtc = DateTimeOffset.UtcNow;
            message.Status = "answered";
            var temp = file + ".response-" + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temp, JsonSerializer.Serialize(message));
                File.Move(temp, file, overwrite: true);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
            return AsJson(new { ok = true, id = message_id });
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return AsJson(new { ok = false, error = ex.GetType().Name });
        }
    }

    private static string HashAttachment(string path)
    {
        using var input = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(input));
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
                    ValidId.IsMatch(message.Id) && message.Text.Length <= 20000 &&
                    (message.Text.Length != 0 || (message.Attachments?.Count ?? 0) != 0))
                    result.Add(message);
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                // Partially written messages will be retried.
            }
        }
        return result.OrderBy(m => m.CreatedUtc).Take(max).ToList();
    }

    private sealed class ChatAttachment
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string MediaType { get; set; } = "";
        public long SizeBytes { get; set; }
        public string Sha256 { get; set; } = "";
    }
    private sealed class ChatRecord
    {
        public string Id { get; set; } = "";
        public string? ChatId { get; set; }
        public DateTimeOffset CreatedUtc { get; set; }
        public string Text { get; set; } = "";
        public string Status { get; set; } = "pending";
        public List<ChatAttachment> Attachments { get; set; } = new();
        public string? Reply { get; set; }
        public DateTimeOffset? RepliedUtc { get; set; }
    }
}
