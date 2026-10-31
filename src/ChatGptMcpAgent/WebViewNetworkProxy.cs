using System.Net.Sockets;

namespace ChatGptMcpAgent;

/// <summary>
/// Application-scoped WebView2 network selection.
/// Existing Happ behavior is unchanged unless an explicit loopback pilot port
/// is provided to a NEW process. No Windows/system-wide proxy changes.
/// </summary>
internal static class WebViewNetworkProxy
{
    private static bool PortOpen(int port)
    {
        try
        {
            using var client = new TcpClient();
            var result = client.BeginConnect("127.0.0.1", port, null, null);
            if (!result.AsyncWaitHandle.WaitOne(450)) return false;
            client.EndConnect(result);
            return true;
        }
        catch (SocketException) { return false; }
        catch (ObjectDisposedException) { return false; }
    }

    internal static string BrowserArguments()
    {
        // Opt-in for the isolated Xray pilot; never fall back silently when
        // the explicit proxy is unavailable (that would leak browser traffic).
        var pilot = Environment.GetEnvironmentVariable("CHATGPT_MCP_WEBVIEW_PROXY_PORT");
        if (!string.IsNullOrWhiteSpace(pilot))
        {
            if (!int.TryParse(pilot, out var port) || port < 1025 || port > 65535 ||
                port == 10808 || port == 10809)
                throw new InvalidOperationException("Некорректный порт встроенного VPN-прокси.");
            if (!PortOpen(port))
                throw new InvalidOperationException("Встроенный VPN-прокси недоступен; прямое соединение запрещено.");
            return "--proxy-server=http://127.0.0.1:" + port;
        }

        // Legacy Happ proxy, retained until the independent endpoint is accepted.
        if (PortOpen(10809)) return "--proxy-server=http://127.0.0.1:10809";
        if (PortOpen(10808)) return "--proxy-server=socks5://127.0.0.1:10808";
        return "";
    }

    internal static bool SelfTest()
    {
        var arguments = BrowserArguments();
        return arguments == "" || arguments.StartsWith("--proxy-server=", StringComparison.Ordinal);
    }
}
