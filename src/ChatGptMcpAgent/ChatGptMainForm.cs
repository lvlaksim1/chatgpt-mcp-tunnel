using System.Drawing;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace ChatGptMcpAgent;

/// <summary>
/// Primary ChatGPT window. The original chatgpt.com Web UI is the ONLY user-facing
/// conversation renderer and composer. No DOM scraping, duplicate local message
/// bubbles, queued text relay, or second browser process is involved.
/// </summary>
internal sealed class ChatGptMainForm : Form
{
    private const string Home = "https://chatgpt.com/";
    private readonly WebView2 _browser = new()
    {
        Dock = DockStyle.Fill,
        DefaultBackgroundColor = Color.FromArgb(37, 37, 35)
    };
    private readonly Label _error = new()
    {
        Dock = DockStyle.Top,
        Height = 64,
        TextAlign = ContentAlignment.MiddleCenter,
        Visible = false
    };
    private readonly List<Form> _popups = new();
    private bool _initializing;
    private bool _ready;
    private bool _lastNavigationSucceeded;
    private bool _exitRequested;
    private CoreWebView2Environment? _environment;
    private FileSystemWatcher? _themeWatcher;
    private readonly System.Windows.Forms.Timer _themeReload = new() { Interval = 320 };
    private static string WarmCssFile => Path.Combine(AppContext.BaseDirectory,
        "Themes", "ChatGPT-Warm-Dark.css");

    internal static string ProfileFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ChatGPTMCPAgent", "WebView2", "ChatGPT");
    internal static bool ProfileValid => Path.IsPathFullyQualified(ProfileFolder) &&
        ProfileFolder.Contains("ChatGPTMCPAgent", StringComparison.OrdinalIgnoreCase);

    internal bool BrowserInitialized => _ready;
    internal bool LastNavigationSucceeded => _lastNavigationSucceeded;
    internal WebView2 BrowserControl => _browser;
    internal string? CurrentPage => _browser.CoreWebView2?.Source;

    internal ChatGptMainForm()
    {
        Text = "ChatGPT MCP Tunnel v" + Application.ProductVersion.Split('+')[0];
        Icon = TrayIcons.AppIcon();
        Font = new Font("Segoe UI", 10);
        Size = new Size(1370, 900);
        MinimumSize = new Size(800, 530);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = AppTheme.Background;
        Controls.Add(_browser);
        Controls.Add(_error);
        _browser.BringToFront();
        ApplyBrowserAppearance();
        AppTheme.Apply(this);
        _themeReload.Tick += async (_, _) =>
        {
            _themeReload.Stop();
            await ApplySitePaletteAsync();
        };
        Shown += async (_, _) => await InitializeAsync();
        FormClosing += (_, e) =>
        {
            if (!_exitRequested && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        };
        FormClosed += (_, _) =>
        {
            foreach (var popup in _popups.ToArray())
                if (!popup.IsDisposed) popup.Close();
            _themeReload.Stop();
            _themeReload.Dispose();
            _themeWatcher?.Dispose();
            _browser.Dispose();
        };
        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (!_ready) return;
            if (e.KeyCode == Keys.F5 || (e.Control && e.KeyCode == Keys.R))
            {
                _browser.CoreWebView2?.Reload();
                e.Handled = e.SuppressKeyPress = true;
            }
        };
    }

    internal void ShowChat()
    {
        if (IsDisposed || Disposing) return;
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        if (!Visible) Show();
        BringToFront();
        Activate();
    }

    internal void ExitApplication()
    {
        _exitRequested = true;
        Close();
    }

    // Palette only. The page still owns all messages, chat lists, media,
    // editing, uploads, playback and interaction. No DOM scraping or relay.
    internal async Task<bool> PaletteAppliedAsync()
    {
        if (_browser.CoreWebView2 is null || !_lastNavigationSucceeded) return false;
        try
        {
            var result = await _browser.CoreWebView2.ExecuteScriptAsync(
                "getComputedStyle(document.body).backgroundColor");
            var color = JsonSerializer.Deserialize<string>(result);
            return AppTheme.Dark ? color == "rgb(37, 37, 35)" : color == "rgb(255, 255, 255)";
        }
        catch (Exception e) when (e is InvalidOperationException or
            System.Runtime.InteropServices.COMException) { return false; }
    }

    private static string ReadWarmCss()
    {
        if (!File.Exists(WarmCssFile))
            return ":root { --main-surface-primary: #252523 !important; }" +
                "html,body {background-color:#252523!important;color:#E5D9BF!important;}";
        return File.ReadAllText(WarmCssFile);
    }

    private void WatchWarmCss()
    {
        var folder = Path.GetDirectoryName(WarmCssFile)!;
        if (!Directory.Exists(folder)) return;
        _themeWatcher = new FileSystemWatcher(folder, Path.GetFileName(WarmCssFile))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        _themeWatcher.Changed += (_, _) => QueueCssReload();
        _themeWatcher.Created += (_, _) => QueueCssReload();
        _themeWatcher.Renamed += (_, _) => QueueCssReload();
    }

    private void QueueCssReload()
    {
        if (IsDisposed || Disposing || !IsHandleCreated) return;
        try
        {
            BeginInvoke((Action)(() =>
            {
                if (IsDisposed || Disposing) return;
                _themeReload.Stop();
                _themeReload.Start();
            }));
        }
        catch (InvalidOperationException) { }
    }

    private async Task ApplySitePaletteAsync()
    {
        var core = _browser.CoreWebView2;
        if (core is null || !core.Source.StartsWith("https://chatgpt.com/", StringComparison.Ordinal))
            return;
        // This user-supplied stylesheet is loaded from disk, not hardcoded in
        // the executable, so changes can apply to the OPEN WebView2 immediately.
        var css = AppTheme.Dark
            ? ReadWarmCss()
            : """
              :root {
                --main-surface-primary: #ffffff !important;
                --main-surface-secondary: #f1f1f1 !important;
                --main-surface-tertiary: #e5e5e5 !important;
                --sidebar-surface-primary: #f7f7f7 !important;
                --sidebar-surface-secondary: #eaeaea !important;
                --text-primary: #202020 !important;
                --text-secondary: #666666 !important;
                color-scheme: light !important;
              }
              html, body, #__next, #root,
              .bg-token-main-surface-primary { background-color: #ffffff !important; }
              .bg-token-sidebar-surface-primary { background-color: #f7f7f7 !important; }
              .bg-token-main-surface-secondary { background-color: #f1f1f1 !important; }
              """;
        var code = """
            (() => {
                if (location.hostname !== 'chatgpt.com') return;
                let style = document.getElementById('mcp-graphite-palette');
                if (!style) {
                    style = document.createElement('style');
                    style.id = 'mcp-graphite-palette';
                    (document.head || document.documentElement).appendChild(style);
                }
                style.textContent =
            """ + JsonSerializer.Serialize(css) + "; })()";
        try { await core.ExecuteScriptAsync(code); }
        catch (Exception e) when (e is InvalidOperationException or
            System.Runtime.InteropServices.COMException) { }
    }

    internal void ApplyBrowserAppearance()
    {
        var dark = AppTheme.Dark;
        BackColor = AppTheme.Background;
        _browser.DefaultBackgroundColor = dark ? Color.FromArgb(37, 37, 35) : Color.White;
        if (_browser.CoreWebView2 is not null)
        {
            _browser.CoreWebView2.Profile.PreferredColorScheme = dark
                ? CoreWebView2PreferredColorScheme.Dark
                : CoreWebView2PreferredColorScheme.Light;
            _ = ApplySitePaletteAsync();
        }
        // The website itself owns its layout and theme. Its stored preference
        // may override the OS preference; do not inject styles into ChatGPT.
    }

    private async Task InitializeAsync()
    {
        if (_ready || _initializing || Disposing || IsDisposed) return;
        _initializing = true;
        try
        {
            var profile = Environment.GetEnvironmentVariable("CHATGPT_MCP_WEBVIEW_TEST_PROFILE")
                ?? ProfileFolder;
            Directory.CreateDirectory(profile);
            var options = new CoreWebView2EnvironmentOptions
            {
                AdditionalBrowserArguments = WebViewNetworkProxy.BrowserArguments()
            };
            _environment = await CoreWebView2Environment.CreateAsync(userDataFolder: profile, options: options);
            if (IsDisposed) return;
            await _browser.EnsureCoreWebView2Async(_environment);
            if (IsDisposed) return;
            var core = _browser.CoreWebView2
                ?? throw new InvalidOperationException("WebView2 is not initialized");
            core.NewWindowRequested += OnPopup;
            core.NavigationCompleted += (_, args) =>
            {
                if (IsDisposed) return;
                _lastNavigationSucceeded = args.IsSuccess;
                if (args.IsSuccess) _ = ApplySitePaletteAsync();
                _error.Visible = !args.IsSuccess;
                if (!args.IsSuccess)
                    _error.Text = "Не удалось открыть ChatGPT: " + args.WebErrorStatus +
                        ". Проверьте подключение Happ и нажмите F5.";
            };
            core.ProcessFailed += (_, args) =>
            {
                if (IsDisposed) return;
                AgentStorage.Log("WebView2 main process failure: " + args.ProcessFailedKind);
                _error.Text = "WebView2 завершился с ошибкой. Перезапустите приложение.";
                _error.Visible = true;
            };
            core.Settings.AreDevToolsEnabled = false;
            core.Settings.AreDefaultContextMenusEnabled = true;
            _ready = true;
            WatchWarmCss();
            ApplyBrowserAppearance();
            core.Navigate(Home);
            AgentStorage.Log("ChatGPT main WebView2 initialized; single browser UI");
        }
        catch (Exception e)
        {
            AgentStorage.Log("ChatGPT main WebView2 startup failed: " + e.GetType().Name);
            _error.Text = "WebView2 не запущен: " + e.Message;
            _error.Visible = true;
        }
        finally
        {
            _initializing = false;
        }
    }

    private async void OnPopup(object? sender, CoreWebView2NewWindowRequestedEventArgs args)
    {
        // OAuth sometimes requires a separate window. Keep the same user data
        // folder and WebView2 environment, never a standalone browser profile.
        var deferral = args.GetDeferral();
        Form? popup = null;
        try
        {
            if (_environment is null || IsDisposed)
            {
                args.Handled = true;
                return;
            }
            popup = new Form
            {
                Text = "ChatGPT — авторизация",
                Icon = TrayIcons.AppIcon(),
                Size = new Size(780, 790),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = AppTheme.Background
            };
            var view = new WebView2
            {
                Dock = DockStyle.Fill,
                DefaultBackgroundColor = AppTheme.Background
            };
            popup.Controls.Add(view);
            Form window = popup;
            popup.FormClosed += (_, _) =>
            {
                _popups.Remove(window);
                view.Dispose();
            };
            _popups.Add(popup);
            AppTheme.Apply(popup);
            popup.Show(this);
            await view.EnsureCoreWebView2Async(_environment);
            if (!popup.IsDisposed && !IsDisposed)
            {
                view.CoreWebView2.Profile.PreferredColorScheme =
                    AppTheme.Dark ? CoreWebView2PreferredColorScheme.Dark :
                    CoreWebView2PreferredColorScheme.Light;
                args.NewWindow = view.CoreWebView2;
            }
            else args.Handled = true;
        }
        catch (Exception e)
        {
            args.Handled = true;
            AgentStorage.Log("ChatGPT WebView2 popup: " + e.GetType().Name);
            if (popup is { IsDisposed: false }) popup.Close();
        }
        finally { deferral.Complete(); }
    }
}
