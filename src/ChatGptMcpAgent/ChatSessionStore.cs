using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ChatGptMcpAgent;

// Messages and attachments stay in their original directories. ChatId groups
// messages without moving or altering existing files (lossless migration).
internal sealed class ChatSession
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "Новый чат";
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
    public string LastPreview { get; set; } = "";
    // Only the explicit + New Chat action permits creating a cloud thread.
    public bool CreateCloudThread { get; set; }
}
internal static class ChatSessionStore
{
    internal const string LegacyId = "00000000000000000000000000000000";
    private static readonly Regex IdPattern = new("^[a-f0-9]{32}$", RegexOptions.IgnoreCase);
    private static readonly object Gate = new();
    private static string Root => Environment.GetEnvironmentVariable("CHATGPT_MCP_CHAT_TEST_ROOT") ??
        Path.Combine(AgentStorage.DataFolder, "Chat");
    private static string DirectoryPath => Path.Combine(Root, "sessions");
    private static string MessagesPath => Path.Combine(Root, "messages");
    private static string ActivePath => Path.Combine(Root, "active-chat.txt");
    internal static bool Valid(string? id) => id is not null && IdPattern.IsMatch(id);

    private static string PathFor(string id) => Path.Combine(DirectoryPath, id + ".json");
    private static void WriteAtomic(string path, string value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tmp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(tmp, value, new UTF8Encoding(false));
            File.Move(tmp, path, true);
        }
        finally { if (File.Exists(tmp)) File.Delete(tmp); }
    }
    private static void Save(ChatSession session) =>
        WriteAtomic(PathFor(session.Id), JsonSerializer.Serialize(session));

    private static List<ChatSession> ReadAll()
    {
        if (!Directory.Exists(DirectoryPath)) return new();
        var result = new List<ChatSession>();
        foreach (var file in Directory.EnumerateFiles(DirectoryPath, "*.json"))
        {
            try
            {
                var entry = JsonSerializer.Deserialize<ChatSession>(File.ReadAllText(file));
                if (entry is not null && Valid(entry.Id) &&
                    string.Equals(entry.Id + ".json", Path.GetFileName(file),
                        StringComparison.OrdinalIgnoreCase))
                    result.Add(entry);
            }
            catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException) { }
        }
        return result;
    }
    private static void Bootstrap()
    {
        Directory.CreateDirectory(DirectoryPath);
        var existing = ReadAll();
        if (Directory.Exists(MessagesPath) && !existing.Any(x => x.Id == LegacyId))
        {
            var oldFiles = Directory.EnumerateFiles(MessagesPath, "*.json");
            // Old records had no ChatId; never rewrite them.
            var hasLegacy = oldFiles.Any(path =>
            {
                try
                {
                    using var json = JsonDocument.Parse(File.ReadAllText(path));
                    var root = json.RootElement;
                    return !root.TryGetProperty("ChatId", out var chat) ||
                           chat.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined ||
                           string.IsNullOrWhiteSpace(chat.GetString());
                }
                catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException) { return false; }
            });
            if (hasLegacy)
            {
                var now = DateTimeOffset.UtcNow;
                Save(new ChatSession { Id = LegacyId, Title = "История до обновления",
                    CreatedUtc = now.AddDays(-1), UpdatedUtc = now });
                existing = ReadAll();
            }
        }
        if (existing.Count == 0)
        {
            var now = DateTimeOffset.UtcNow;
            Save(new ChatSession { Id = Guid.NewGuid().ToString("N"), CreatedUtc = now, UpdatedUtc = now });
        }
    }
    internal static List<ChatSession> List()
    {
        lock (Gate)
        {
            Bootstrap();
            return ReadAll().OrderByDescending(x => x.UpdatedUtc).ThenBy(x => x.Title).ToList();
        }
    }
    internal static string Active()
    {
        lock (Gate)
        {
            var chats = List();
            try
            {
                if (File.Exists(ActivePath))
                {
                    var value = File.ReadAllText(ActivePath).Trim();
                    if (chats.Any(x => x.Id == value)) return value;
                }
            }
            catch (IOException) { }
            var chosen = chats[0].Id;
            WriteAtomic(ActivePath, chosen);
            return chosen;
        }
    }
    internal static void Select(string id)
    {
        lock (Gate)
        {
            if (!Valid(id) || !List().Any(x => x.Id == id))
                throw new ArgumentException("Неизвестный чат", nameof(id));
            WriteAtomic(ActivePath, id);
        }
    }
    internal static ChatSession Create()
    {
        lock (Gate)
        {
            var now = DateTimeOffset.UtcNow;
            var item = new ChatSession { Id = Guid.NewGuid().ToString("N"), CreatedUtc = now, UpdatedUtc = now, CreateCloudThread = true };
            Save(item);
            Select(item.Id);
            return item;
        }
    }
    internal static void Touch(string id, string text)
    {
        lock (Gate)
        {
            if (!Valid(id)) return;
            var path = PathFor(id);
            if (!File.Exists(path)) return;
            try
            {
                var item = JsonSerializer.Deserialize<ChatSession>(File.ReadAllText(path));
                if (item is null) return;
                item.UpdatedUtc = DateTimeOffset.UtcNow;
                var preview = (text ?? "").Replace('\r', ' ').Replace('\n', ' ').Trim();
                if (preview.Length > 90) preview = preview[..90] + "…";
                item.LastPreview = preview;
                if (item.Title == "Новый чат" && !string.IsNullOrWhiteSpace(preview))
                    item.Title = preview.Length > 52 ? preview[..52] + "…" : preview;
                Save(item);
            }
            catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException) { }
        }
    }
    internal static string TitleFor(string id)
    {
        lock (Gate)
        {
            var item = List().FirstOrDefault(x => x.Id == id);
            return item?.Title ?? "Чат";
        }
    }
}
