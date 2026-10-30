using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ChatGptMcpAgent;

// Local-only recovery of an ordinary ChatGPT conversation with a verified URL.
// Unlike the old Hub this never issues ui/message, opens Work, or changes queue markers.
internal sealed class ChatThreadActivator
{
    private static string Root =>
        Environment.GetEnvironmentVariable("CHATGPT_MCP_CHAT_TEST_ROOT") ??
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChatGPTMCPAgent", "Chat");
    private static string MessagesDir => Path.Combine(Root, "messages");
    private static string ForwardedDir => Path.Combine(Root, "ui-forwarded");
    private static string BindingsDir => Path.Combine(Root, "thread-bindings");
    private static readonly Regex Id = new("^[0-9a-f]{32}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ChatPath = new(
        "^/c/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private DateTimeOffset _lastCheck = DateTimeOffset.MinValue;
    private readonly Dictionary<string, DateTimeOffset> _lastOpened = new();
    private bool _working;

    internal static bool ValidUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 512 ||
            !Uri.TryCreate(value, UriKind.Absolute, out var url))
            return false;
        return url.Scheme == Uri.UriSchemeHttps &&
               url.Host.Equals("chatgpt.com", StringComparison.OrdinalIgnoreCase) &&
               url.IsDefaultPort && url.UserInfo.Length == 0 &&
               string.IsNullOrEmpty(url.Query) && string.IsNullOrEmpty(url.Fragment) &&
               ChatPath.IsMatch(url.AbsolutePath);
    }

    internal static string? SelectDestination(DateTimeOffset now)
    {
        if (!Directory.Exists(MessagesDir) || !Directory.Exists(BindingsDir)) return null;
        var pending = new List<(DateTimeOffset Time, string Chat)>();
        foreach (var file in Directory.EnumerateFiles(MessagesDir, "*.json"))
        {
            try
            {
                var fileInfo = new FileInfo(file);
                if (now.UtcDateTime - fileInfo.LastWriteTimeUtc > TimeSpan.FromDays(2))
                    continue;
                using var doc = JsonDocument.Parse(File.ReadAllText(file));
                var obj = doc.RootElement;
                if (!obj.TryGetProperty("Status", out var status) ||
                    status.GetString() != "pending" ||
                    !obj.TryGetProperty("ChatId", out var chat) ||
                    chat.ValueKind != JsonValueKind.String)
                    continue;
                var id = chat.GetString() ?? "";
                if (!Id.IsMatch(id) || id.All(c => c=='0')) continue;
                var mid = Path.GetFileNameWithoutExtension(file);
                if (!Id.IsMatch(mid) || File.Exists(Path.Combine(ForwardedDir,mid+".json")))
                    continue;
                if (!obj.TryGetProperty("CreatedUtc", out var created) ||
                    created.ValueKind != JsonValueKind.String ||
                    !created.TryGetDateTimeOffset(out var time) ||
                    now-time < TimeSpan.FromSeconds(12) ||
                    now-time > TimeSpan.FromDays(2))
                    continue;
                pending.Add((time,id));
            }
            catch (Exception ex) when(ex is IOException or UnauthorizedAccessException or JsonException) { }
        }

        foreach (var (_,chat) in pending.OrderBy(x => x.Time))
        {
            try
            {
                var file = Path.Combine(BindingsDir,chat.ToLowerInvariant()+".json");
                if (!File.Exists(file)) continue;
                using var doc = JsonDocument.Parse(File.ReadAllText(file));
                var bound = doc.RootElement;
                if (!bound.TryGetProperty("ChatId", out var id) ||
                    !string.Equals(id.GetString(),chat,StringComparison.OrdinalIgnoreCase) ||
                    !bound.TryGetProperty("Url", out var url))
                    continue;
                var destination = url.GetString();
                if (ValidUrl(destination)) return destination;
            }
            catch (Exception ex) when(ex is IOException or UnauthorizedAccessException or JsonException) { }
        }
        return null;
    }

    internal void Tick(bool isServerAvailable)
    {
        if (!OperatingSystem.IsWindows() || !isServerAvailable || _working) return;
        var now=DateTimeOffset.UtcNow;
        if (now-_lastCheck < TimeSpan.FromSeconds(8)) return;
        _lastCheck=now;
        _working=true;
        try
        {
            var destination=SelectDestination(now);
            if (destination is null || !ValidUrl(destination)) return;
            if (_lastOpened.TryGetValue(destination,out var last) &&
                now-last < TimeSpan.FromSeconds(95)) return;
            _lastOpened[destination]=now;
            Process.Start(new ProcessStartInfo(destination)
            {
                UseShellExecute=true,
                WindowStyle=ProcessWindowStyle.Minimized
            })?.Dispose();
        }
        catch (Exception ex) when(ex is IOException or UnauthorizedAccessException or
                   System.ComponentModel.Win32Exception or InvalidOperationException) { }
        finally { _working=false; }
    }

    internal static int SelfTest()
    {
        if (!ValidUrl("https://chatgpt.com/c/01234567-89ab-cdef-0123-456789abcdef") ||
            ValidUrl("https://chatgpt.com/work") ||
            ValidUrl("https://chatgpt.com/c/01234567-89ab-cdef-0123-456789abcdef?x=y") ||
            ValidUrl("https://evil.example/c/01234567-89ab-cdef-0123-456789abcdef") ||
            ValidUrl("https://chatgpt.com.evil.test/c/01234567-89ab-cdef-0123-456789abcdef") ||
            ValidUrl("http://chatgpt.com/c/01234567-89ab-cdef-0123-456789abcdef"))
            return 91;

        var previous=Environment.GetEnvironmentVariable("CHATGPT_MCP_CHAT_TEST_ROOT");
        var folder=Path.Combine(Path.GetTempPath(),"mcp-bindings-"+Guid.NewGuid().ToString("N"));
        const string chatId="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        const string msgId="bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
        const string url="https://chatgpt.com/c/01234567-89ab-cdef-0123-456789abcdef";
        try
        {
            Environment.SetEnvironmentVariable("CHATGPT_MCP_CHAT_TEST_ROOT",folder);
            Directory.CreateDirectory(MessagesDir);
            Directory.CreateDirectory(BindingsDir);
            var now=DateTimeOffset.UtcNow;
            File.WriteAllText(Path.Combine(MessagesDir,msgId+".json"),JsonSerializer.Serialize(new
            {
                Id=msgId,ChatId=chatId,Status="pending",
                CreatedUtc=now.AddSeconds(-30)
            }));
            File.WriteAllText(Path.Combine(BindingsDir,chatId+".json"),JsonSerializer.Serialize(new
            {
                ChatId=chatId,Url=url,CapturedUtc=now
            }));
            if (SelectDestination(now)!=url) return 92;
            Directory.CreateDirectory(ForwardedDir);
            File.WriteAllText(Path.Combine(ForwardedDir,msgId+".json"),"{}");
            if (SelectDestination(now)!=null) return 93;
            File.Delete(Path.Combine(ForwardedDir,msgId+".json"));
            File.WriteAllText(Path.Combine(BindingsDir,chatId+".json"),JsonSerializer.Serialize(new
            {
                ChatId=chatId,Url="https://evil.example/c/01234567-89ab-cdef-0123-456789abcdef"
            }));
            if (SelectDestination(now)!=null) return 94;
            return 0;
        }
        finally
        {
            Environment.SetEnvironmentVariable("CHATGPT_MCP_CHAT_TEST_ROOT",previous);
            if (Directory.Exists(folder)) Directory.Delete(folder,recursive:true);
        }
    }
}
