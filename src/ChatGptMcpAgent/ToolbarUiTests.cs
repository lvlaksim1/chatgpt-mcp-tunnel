using System.Drawing;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace ChatGptMcpAgent;

// Offline DOM test: virtual chatgpt.com is mapped to a local mock page.
// No user browser profile, cookies, real chats or external requests are used.
internal static class ToolbarUiTests
{
    internal static int Run()
    {
        var root = Path.Combine(Path.GetTempPath(), "McpToolbarUi-" + Guid.NewGuid().ToString("N"));
        var site = Path.Combine(root, "site");
        Directory.CreateDirectory(site);
        var page = """
          <!doctype html><html><head><meta charset="utf-8"></head><body>
          <header style="display:flex;justify-content:flex-end;width:100%">
            <div id="toolbar" style="display:flex;justify-content:flex-end;width:300px;gap:8px">
              <button aria-label="Chat actions" type="button"
                onclick="document.getElementById('menu').innerHTML='<div role=&quot;menuitem&quot; tabindex=&quot;0&quot;>Переименовать</div><div role=&quot;menuitem&quot; tabindex=&quot;0&quot;>Закрепить</div>'">•••</button>
              <button aria-label="Поделиться" type="button"
                onclick="window.__shareClicks=(window.__shareClicks||0)+1">Поделиться</button>
            </div>
          </header>
          <div id="menu" role="menu"></div>
          <script>window.__shareClicks=0;</script>
          </body></html>
          """;
        File.WriteAllText(Path.Combine(site, "test.html"), page);
        var result = 199;
        using var form = new Form { Size = new Size(650, 420), ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
        using var web = new WebView2 { Dock = DockStyle.Fill };
        form.Controls.Add(web);
        using var deadline = new System.Windows.Forms.Timer { Interval = 12000 };
        deadline.Tick += (_, _) => { deadline.Stop(); result = 198; form.Close(); };
        form.Shown += async (_, _) =>
        {
            try
            {
                var env = await CoreWebView2Environment.CreateAsync(
                    userDataFolder: Path.Combine(root, "profile"));
                await web.EnsureCoreWebView2Async(env);
                var core = web.CoreWebView2;
                core.SetVirtualHostNameToFolderMapping("chatgpt.com", site,
                    CoreWebView2HostResourceAccessKind.Allow);
                var loaded = new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                core.NavigationCompleted += (_, e) => loaded.TrySetResult(e.IsSuccess);
                core.Navigate("https://chatgpt.com/test.html");
                if (!await loaded.Task) throw new InvalidOperationException("Mock page failed");
                var script = File.ReadAllText(Path.Combine(
                    AppContext.BaseDirectory, "Themes", "ChatGPT-Toolbar.js"));
                await core.ExecuteScriptAsync(script);
                await Task.Delay(350);
                var test = """
                  (() => {
                    const actions = document.querySelector('[aria-label="Chat actions"]');
                    const share = document.querySelector('button[aria-label="Поделиться"]');
                    const hidden = getComputedStyle(share).display === 'none';
                    const colored = getComputedStyle(actions).backgroundColor !== 'rgba(0, 0, 0, 0)';
                    actions.click();
                    return hidden && colored;
                  })()
                  """;
                var first = await core.ExecuteScriptAsync(test);
                await Task.Delay(400);
                var second = await core.ExecuteScriptAsync("""
                  (() => {
                    const menu = document.getElementById('menu');
                    const injected = menu.querySelector('[data-mcp-share-action="true"]');
                    if (!injected || !injected.textContent.includes('Поделиться')) return false;
                    injected.click();
                    return window.__shareClicks === 1 &&
                      getComputedStyle(document.querySelector('button[aria-label="Поделиться"]')).display === 'none';
                  })()
                  """);
                result = first == "true" && second == "true" ? 0 : 197;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("TOOLBAR_DOM_TEST_FAILED " + e.GetType().Name + " " + e.Message);
                result = 196;
            }
            deadline.Stop();
            form.Close();
        };
        deadline.Start();
        Application.Run(form);
        try { Directory.Delete(root, recursive: true); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        return result;
    }
}
