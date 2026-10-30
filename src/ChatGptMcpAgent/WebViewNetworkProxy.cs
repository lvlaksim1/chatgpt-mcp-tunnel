using System.Net.Sockets;

namespace ChatGptMcpAgent;

// HTTP local proxy belongs to already running Happ (xray) on DOMKAT.
// No machine-wide proxy changes or VPN toggling are performed.
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
        // Proxy for the Chromium WebView2 process (not for Tunnel's LAN/MCP).
        if (PortOpen(10809)) return "--proxy-server=http://127.0.0.1:10809";
        if (PortOpen(10808)) return "--proxy-server=socks5://127.0.0.1:10808";
        return "";
    }

    internal static bool SelfTest()
    {
        var arguments = BrowserArguments();
        return arguments == "" || arguments.StartsWith("--proxy-server=");
    }
}
