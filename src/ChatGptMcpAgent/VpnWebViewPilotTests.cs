using System.Drawing;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace ChatGptMcpAgent;

/// <summary>
/// Isolated network smoke test. Never uses or deletes the real ChatGPT profile.
/// Tests a dedicated local proxy on loopback without touching OS routing.
/// </summary>
internal static class VpnWebViewPilotTests
{
    internal static int Run()
    {
        var portText = Environment.GetEnvironmentVariable("CHATGPT_MCP_VPN_PILOT_PORT") ?? "";
        if (!int.TryParse(portText, out int port) || port < 1025 || port > 65535)
            return 80;
        var root = Path.Combine(Path.GetTempPath(), "McpVpnWebView-" + Guid.NewGuid().ToString("N"));
        var code = 83;
        using var window = new Form
        {
            Width = 820, Height = 550, ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-19000, -19000)
        };
        using var view = new WebView2 { Dock = DockStyle.Fill };
        window.Controls.Add(view);
        using var timer = new System.Windows.Forms.Timer { Interval = 27000 };
        timer.Tick += (_, _) => { code = 82; timer.Stop(); window.Close(); };
        window.Shown += async (_, _) =>
        {
            try
            {
                var options = new CoreWebView2EnvironmentOptions
                {
                    AdditionalBrowserArguments = "--proxy-server=http://127.0.0.1:" + port
                };
                var env = await CoreWebView2Environment.CreateAsync(
                    userDataFolder: Path.Combine(root, "profile"), options: options);
                await view.EnsureCoreWebView2Async(env);
                var core = view.CoreWebView2;
                core.NavigationCompleted += async (_, e) =>
                {
                    try
                    {
                        var title = await core.ExecuteScriptAsync("document.title");
                        // HTTP navigation status alone is not enough to prove content arrived.
                        code = e.IsSuccess && title.Contains("Example Domain", StringComparison.OrdinalIgnoreCase)
                            ? 0 : 81;
                        Console.WriteLine($"VPN_WEBVIEW_RESULT success={e.IsSuccess} title={title} port={port}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("VPN_WEBVIEW_SCRIPT_FAILED " + ex.GetType().Name);
                        code = 84;
                    }
                    finally { timer.Stop(); window.Close(); }
                };
                core.Navigate("https://example.com/");
            }
            catch (Exception ex)
            {
                Console.WriteLine("VPN_WEBVIEW_FAILED " + ex.GetType().Name + " " + ex.Message);
                code = 85; timer.Stop(); window.Close();
            }
        };
        timer.Start();
        Application.Run(window);
        try { Directory.Delete(root, true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return code;
    }
}
