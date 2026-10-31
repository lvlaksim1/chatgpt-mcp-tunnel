using System.Net.Sockets;
using System.Text.Json;

namespace ChatGptMcpAgent;

internal enum VpnConnectionMode { Happ, FreeReality }

/// <summary>
/// UI-owned local Xray, independent of the official MCP tunnel-client.
/// Never touches Windows default routes or the installed Happ process.
/// An explicit FreeReality mode must fail closed if it can't start Xray.
/// </summary>
internal static class VpnModeController
{
    internal const int LocalProxyPort = 11821;
    // A user-requested change is validated before the GUI is recreated.
    internal static bool IsTransitioning { get; set; }

    internal static string Folder => Path.Combine(AgentStorage.DataFolder, "Vpn");
    internal static string ProfileFolder => Path.Combine(Folder, "profiles");
    // One original authorized WebView2 user data folder for all network modes.
    // Browser processes MUST fully exit before recreating with new proxy flags.
    internal static string BrowserProfileFolder(string existingProfile) => existingProfile;
    internal static string PreferencesFile => Path.Combine(Folder, "network-mode.json");
    internal static string ProfileFile => Path.Combine(ProfileFolder, "Free-Reality-Auto.dpapi");
    internal static string StatusFile => Path.Combine(ProfileFolder, "status.json");

    internal static VpnConnectionMode Current
    {
        get
        {
            try
            {
                if (!File.Exists(PreferencesFile)) return VpnConnectionMode.Happ;
                using var doc = JsonDocument.Parse(File.ReadAllText(PreferencesFile));
                return doc.RootElement.TryGetProperty("mode", out var value) &&
                    value.GetString() == "free" ? VpnConnectionMode.FreeReality : VpnConnectionMode.Happ;
            }
            catch (Exception) { return VpnConnectionMode.Happ; }
        }
    }

    internal static void Save(VpnConnectionMode mode)
    {
        Directory.CreateDirectory(Folder);
        var file = PreferencesFile + ".tmp";
        File.WriteAllText(file, JsonSerializer.Serialize(new
        {
            mode = mode == VpnConnectionMode.FreeReality ? "free" : "happ",
            updatedAt = DateTimeOffset.UtcNow
        }));
        File.Move(file, PreferencesFile, true);
    }

    private static bool PortOpen(int port)
    {
        try
        {
            using var sock = new TcpClient();
            var result = sock.BeginConnect("127.0.0.1", port, null, null);
            if (!result.AsyncWaitHandle.WaitOne(400)) return false;
            sock.EndConnect(result);
            return true;
        }
        catch (SocketException) { return false; }
        catch (ObjectDisposedException) { return false; }
    }

    internal static bool ProfileReady(out string reason)
    {
        reason = "";
        if (!File.Exists(ProfileFile))
        {
            reason = "Нет проверенного бесплатного VPN-профиля. Дождитесь обновления списка.";
            return false;
        }
        try
        {
            if (!File.Exists(StatusFile))
                throw new InvalidOperationException("Нет результатов проверки серверов.");
            using var status = JsonDocument.Parse(File.ReadAllText(StatusFile));
            var selected = status.RootElement.GetProperty("selected");
            var stamp = DateTimeOffset.Parse(selected.GetProperty("verifiedAt").GetString()!);
            if (DateTimeOffset.UtcNow - stamp > TimeSpan.FromHours(6) ||
                stamp > DateTimeOffset.UtcNow.AddMinutes(10))
                throw new InvalidOperationException("Проверка сервера устарела. Обновите список.");
            return true;
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidOperationException or FormatException or KeyNotFoundException)
        {
            reason = ex.Message;
            return false;
        }
    }

    internal static string BrowserArguments()
    {
        if (Current == VpnConnectionMode.Happ)
            return WebViewNetworkProxy.BrowserArguments();
        // The DOMKAT MCP bridge owns Xray. The UI never starts a duplicate.
        // Confirm the bridge-probed listener is present or refuse a direct connection.
        if (!PortOpen(LocalProxyPort))
            throw new InvalidOperationException("Серверный Xray ещё не готов.");
        return "--proxy-server=http://127.0.0.1:" + LocalProxyPort +
               " --proxy-bypass-list=<-loopback>";
    }

    internal static async Task<bool> ProbeFreeViaBridgeAsync()
    {
        // One harmless HTTPS request causes the bridge to start the selected
        // free Xray and confirms its remote handshake, without browser tokens.
        try
        {
            using var handler = new HttpClientHandler
            {
                Proxy = new System.Net.WebProxy("http://127.0.0.1:11827"),
                UseProxy = true,
                UseCookies = false
            };
            using var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(18)
            };
            using var response = await client.GetAsync("https://example.com/");
            return response.IsSuccessStatusCode && PortOpen(LocalProxyPort);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
        {
            return false;
        }
    }

}
