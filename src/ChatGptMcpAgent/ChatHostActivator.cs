using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ChatGptMcpAgent;

// Starts the *existing* ChatGPT host when a local message has no live
// MCP Apps Global Hub to deliver it. The configured URL is per-user and
// never checked into the repository, because it identifies a plugin
// installation. No browser automation, API key, or second tunnel.
internal sealed class ChatHostActivator
{
    private static string Root => Environment.GetEnvironmentVariable("CHATGPT_MCP_CHAT_TEST_ROOT") ??
        Path.Combine(AgentStorage.DataFolder, "Chat");
    private static string MessagesDir => Path.Combine(Root, "messages");
    private static string ForwardedDir => Path.Combine(Root, "ui-forwarded");
    private static string EventsPath => Path.Combine(Root, "lifecycle-probe", "events.jsonl");
    private static string UrlPath => Path.Combine(Root, "hub-entrypoint.url");

    private static readonly Regex HubPathPattern = new(
        "^/c/[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private DateTimeOffset _lastCheckUtc = DateTimeOffset.MinValue;
    private DateTimeOffset _lastActivationUtc = DateTimeOffset.MinValue;
    private bool _working;

    internal static bool ValidHubUrl(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Length > 512 ||
            !Uri.TryCreate(raw.Trim(), UriKind.Absolute, out var uri))
            return false;
        return uri.Scheme == Uri.UriSchemeHttps &&
            uri.Host.Equals("chatgpt.com", StringComparison.OrdinalIgnoreCase) &&
            uri.IsDefaultPort && uri.UserInfo.Length == 0 &&
            string.IsNullOrWhiteSpace(uri.Query) && uri.Fragment.Length == 0 &&
            HubPathPattern.IsMatch(uri.AbsolutePath);
    }

    internal static DateTimeOffset? LatestHubHeartbeat(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        DateTimeOffset? last = null;
        try
        {
            // File.ReadLines streams events; TakeLast bounds memory usage.
            foreach (var line in File.ReadLines(filePath).TakeLast(80))
            {
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;
                    if (!root.TryGetProperty("mode", out var mode) ||
                        mode.GetString() != "global" ||
                        !root.TryGetProperty("instance", out var instance) ||
                        instance.ValueKind != JsonValueKind.String ||
                        !(instance.GetString() ?? "").StartsWith("chatbridge-", StringComparison.Ordinal) ||
                        !root.TryGetProperty("kind", out var kind) ||
                        kind.GetString() is not ("heartbeat" or "ui-initialized") ||
                        !root.TryGetProperty("utc", out var time))
                        continue;
                    var dt = time.GetDateTimeOffset();
                    if (!last.HasValue || dt > last) last = dt;
                }
                catch (Exception ex) when (ex is JsonException or FormatException) { }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        return last;
    }

    internal static bool HasPendingUnforwarded(string messagesDir, string forwardedDir)
    {
        if (!Directory.Exists(messagesDir)) return false;
        try
        {
            foreach (var file in Directory.EnumerateFiles(messagesDir, "*.json"))
            {
                // Old messages are not a trigger for re-opening browser sessions.
                if (File.GetLastWriteTimeUtc(file) < DateTime.UtcNow.AddDays(-7))
                    continue;
                var id = Path.GetFileNameWithoutExtension(file);
                if (!Guid.TryParseExact(id, "N", out _) ||
                    File.Exists(Path.Combine(forwardedDir, id + ".json")))
                    continue;
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(file));
                    var root = doc.RootElement;
                    if (root.TryGetProperty("Status", out var status) &&
                        status.GetString() == "pending")
                        return true;
                }
                catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) { }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        return false;
    }

    internal static bool ShouldActivate(DateTimeOffset now, DateTimeOffset? lastHeartbeat,
        bool pendingUnforwarded, DateTimeOffset lastActivation)
    {
        if (!pendingUnforwarded) return false;
        if (now - lastActivation < TimeSpan.FromMinutes(2)) return false;
        if (lastHeartbeat.HasValue &&
            lastHeartbeat.Value <= now &&
            now - lastHeartbeat.Value < TimeSpan.FromSeconds(75))
            return false;
        return true;
    }

    internal void Tick(bool isServerAvailable)
    {
        if (!isServerAvailable || _working) return;
        var now = DateTimeOffset.UtcNow;
        if (now - _lastCheckUtc < TimeSpan.FromSeconds(12)) return;
        _lastCheckUtc = now;
        _working = true;
        try
        {
            if (!File.Exists(UrlPath) ||
                !HasPendingUnforwarded(MessagesDir, ForwardedDir))
                return;

            var heartbeat = LatestHubHeartbeat(EventsPath);
            if (!ShouldActivate(now, heartbeat, true, _lastActivationUtc))
                return;
            var url = File.ReadAllText(UrlPath).Trim();
            if (!ValidHubUrl(url))
            {
                AgentStorage.Log("Ordinary ChatGPT anchor URL rejected. No browser was launched.");
                return;
            }
            _lastActivationUtc = now;
            Process.Start(new ProcessStartInfo(url) {
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Minimized
            })?.Dispose();
            AgentStorage.Log("Ordinary ChatGPT MCP Bridge activation requested for an undelivered local message.");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or
            System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            AgentStorage.Log("Global MCP Hub launch: " + e.GetType().Name);
        }
        finally { _working = false; }
    }

    internal static int SelfTest()
    {
        var temp = Path.Combine(Path.GetTempPath(), "tunnel-hub-activator-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            if (!ValidHubUrl( "https://chatgpt.com/c/01234567-89ab-cdef-0123-456789abcdef"))
                return 51;
            if (ValidHubUrl( "https://evil.example/c/01234567-89ab-cdef-0123-456789abcdef") ||
                ValidHubUrl( "https://chatgpt.com.evil.example/c/01234567-89ab-cdef-0123-456789abcdef") ||
                ValidHubUrl( "https://chatgpt.com/mcp-app/asdk_app_123abc/chat.hub.open") ||
                ValidHubUrl( "https://chatgpt.com/c/01234567-89ab-cdef-0123-456789abcdef?token=x"))
                return 52;
            var now = DateTimeOffset.UtcNow;
            if (!ShouldActivate(now, null, true, DateTimeOffset.MinValue) ||
                ShouldActivate(now, now.AddSeconds(-10), true, DateTimeOffset.MinValue) ||
                ShouldActivate(now, null, false, DateTimeOffset.MinValue) ||
                ShouldActivate(now, null, true, now.AddSeconds(-40)) ||
                !ShouldActivate(now, now.AddSeconds(-90), true, DateTimeOffset.MinValue))
                return 53;
            var events = Path.Combine(temp, "events.jsonl");
            File.WriteAllLines(events, new[] {
                JsonSerializer.Serialize(new { utc=now.AddMinutes(-3),mode="global",kind="heartbeat",instance="chatbridge-old" }),
                JsonSerializer.Serialize(new { utc=now.AddSeconds(-5),mode="thread",kind="heartbeat",instance="ui-any" }),
                JsonSerializer.Serialize(new { utc=now.AddSeconds(-20),mode="global",kind="heartbeat",instance="chatbridge-new" }),
                JsonSerializer.Serialize(new { utc=now.AddSeconds(-2),mode="global",kind="heartbeat",instance="hub-work-mode" })
            });
            var last = LatestHubHeartbeat(events);
            if (!last.HasValue || (last.Value - now.AddSeconds(-20)).Duration() > TimeSpan.FromSeconds(1))
                return 54;
            var messages = Path.Combine(temp, "messages");
            var forwarded = Path.Combine(temp, "ui-forwarded");
            Directory.CreateDirectory(messages);Directory.CreateDirectory(forwarded);
            var id = Guid.NewGuid().ToString("N");
            File.WriteAllText(Path.Combine(messages,id+".json"),
                JsonSerializer.Serialize(new { Id=id, Status="pending", Text="SIMULATED" }));
            if (!HasPendingUnforwarded(messages,forwarded)) return 55;
            File.WriteAllText(Path.Combine(forwarded,id+".json"), "{}");
            if (HasPendingUnforwarded(messages,forwarded)) return 56;
            return 0;
        }
        finally { Directory.Delete(temp,recursive:true); }
    }
}
