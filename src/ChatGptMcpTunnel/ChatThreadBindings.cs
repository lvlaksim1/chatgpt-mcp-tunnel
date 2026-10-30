using System.Text.Json;
using System.Text.RegularExpressions;

internal static class ChatThreadBindings
{
    private static readonly Regex LocalId = new("^[a-f0-9]{32}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ChatPath = new(
        "^/c/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly string LegacyId = new('0',32);
    private static string Root => Environment.GetEnvironmentVariable("CHATGPT_MCP_CHAT_TEST_ROOT") ??
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChatGPTMCPAgent", "Chat");

    internal static bool ValidUrl(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Length > 512 ||
            !Uri.TryCreate(raw, UriKind.Absolute, out var uri)) return false;
        return uri.Scheme == Uri.UriSchemeHttps &&
               uri.Host.Equals("chatgpt.com", StringComparison.OrdinalIgnoreCase) &&
               uri.IsDefaultPort && uri.UserInfo.Length == 0 &&
               string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment) &&
               ChatPath.IsMatch(uri.AbsolutePath);
    }

    internal static void BeginCapture(string chatId)
    {
        if (!OperatingSystem.IsWindows() || !LocalId.IsMatch(chatId) || chatId == LegacyId)
            return;
        // Best effort: only bind a browser page that actually contains this local-chat ID.
        // An unrelated active ChatGPT tab can never silently be bound to this chat.
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500);
                await CaptureAsync(chatId);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // Optional browser context detection must not block the MCP tool.
            }
        });
    }

    private static async Task CaptureAsync(string chatId)
    {
        if (!OperatingSystem.IsWindows()) return;
        using var windows = JsonDocument.Parse(await DesktopTools.WindowList());
        if (!windows.RootElement.TryGetProperty("windows", out var list)) return;
        var browsers = list.EnumerateArray()
            .Where(x => x.TryGetProperty("process", out var p) &&
                        (p.GetString() ?? "").Equals("browser", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.TryGetProperty("foreground", out var fg) && fg.GetBoolean())
            .ToArray();
        foreach (var browser in browsers.Take(3))
        {
            var hwnd = browser.GetProperty("hwnd").GetString();
            if (string.IsNullOrWhiteSpace(hwnd)) continue;
            using var found = JsonDocument.Parse(await UiAutomationTools.Find(
                hwnd, name:chatId, max_depth:18, max_results:5));
            if (!found.RootElement.TryGetProperty("matches", out var matches) ||
                matches.GetArrayLength() == 0) continue;
            // A tool invocation mentioning the ID is not sufficient evidence:
            // the page must contain an actual forwarded local message.
            var proof = "ID локального чата: " + chatId;
            if (!matches.EnumerateArray().Any(x =>
                x.TryGetProperty("name", out var name) &&
                (name.GetString() ?? "").Contains(proof, StringComparison.OrdinalIgnoreCase)))
                continue;

            using var address = JsonDocument.Parse(await UiAutomationTools.Find(
                hwnd, automation_id:"view_1018", max_depth:16, max_results:5));
            if (!address.RootElement.TryGetProperty("matches", out var addressMatches) ||
                addressMatches.GetArrayLength() == 0) continue;
            var path = addressMatches[0].GetProperty("path").GetString();
            if (string.IsNullOrWhiteSpace(path)) continue;
            using var details = JsonDocument.Parse(await UiAutomationTools.Get(hwnd, path));
            var item = details.RootElement;
            var url = item.TryGetProperty("value", out var value) ? value.GetString() : null;
            if (!ValidUrl(url)) continue;

            var directory = Path.Combine(Root,"thread-bindings");
            Directory.CreateDirectory(directory);
            var target = Path.Combine(directory,chatId.ToLowerInvariant()+".json");
            var temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary,JsonSerializer.Serialize(new
                {
                    ChatId = chatId.ToLowerInvariant(),
                    Url = url,
                    CapturedUtc = DateTimeOffset.UtcNow
                }));
                File.Move(temporary,target,overwrite:true);
            }
            finally { if(File.Exists(temporary)) File.Delete(temporary); }
            return;
        }
    }
}
