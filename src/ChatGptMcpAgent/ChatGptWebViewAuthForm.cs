using System.Diagnostics;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace ChatGptMcpAgent;

// Authentication UI only. This WebView2 does not inspect ChatGPT messages,
// inject JavaScript, call the model, or transmit chat text.
internal sealed class ChatGptWebViewAuthForm : Form
{
    private static ChatGptWebViewAuthForm? _active;
    private readonly WebView2 _web = new() { Dock = DockStyle.Fill };
    private readonly Label _status = new()
    {
        Dock = DockStyle.Bottom, Height = 40,
        Padding = new Padding(10, 8, 10, 4),
        Text = "Открываем ChatGPT. Войдите один раз; профиль сохраняется отдельно."
    };
    private readonly Button _reload = new() { Text = "Обновить страницу", Width = 164, Dock = DockStyle.Right };
    private readonly Button _home = new() { Text = "Открыть ChatGPT", Width = 164, Dock = DockStyle.Right };
    private readonly Panel _toolbar = new() { Height = 43, Dock = DockStyle.Top };
    private readonly List<Form> _popups = new();
    private static readonly Uri ChatGptHome = new("https://chatgpt.com/");

    internal static string ProfileFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ChatGPTMCPAgent", "WebView2", "ChatGPT");

    internal static bool TestConfiguration()
    {
        var path=Path.GetFullPath(ProfileFolder);
        return Path.IsPathFullyQualified(path) &&
            path.Contains("ChatGPTMCPAgent", StringComparison.OrdinalIgnoreCase) &&
            ChatGptHome.Scheme == Uri.UriSchemeHttps &&
            ChatGptHome.Host == "chatgpt.com";
    }

    internal static void RunPreview()
    {
        using var preview = new ChatGptWebViewAuthForm();
        _active = preview;
        Application.Run(preview);
        _active = null;
    }

    internal static void ShowOrActivate(Form? owner = null)
    {
        if (_active is { IsDisposed: false } existing)
        {
            if (existing.WindowState == FormWindowState.Minimized)
                existing.WindowState = FormWindowState.Normal;
            existing.Show();
            existing.BringToFront();
            existing.Activate();
            return;
        }
        var form = new ChatGptWebViewAuthForm();
        _active = form;
        form.FormClosed += (_, _) => { if (ReferenceEquals(_active, form)) _active = null; };
        if (owner is { IsDisposed: false }) form.Show(owner);
        else form.Show();
        form.BringToFront();
        form.Activate();
    }

    private ChatGptWebViewAuthForm()
    {
        Text = "ChatGPT MCP Tunnel — Вход в ChatGPT (WebView2)";
        Size = new Size(1120, 800);
        MinimumSize = new Size(720, 560);
        StartPosition = FormStartPosition.CenterScreen;
        Icon = TrayIcons.AppIcon();
        Font = new Font("Segoe UI", 10);
        _toolbar.Padding = new Padding(5);
        _toolbar.Controls.Add(_home);
        _toolbar.Controls.Add(_reload);
        Controls.Add(_web);
        Controls.Add(_status);
        Controls.Add(_toolbar);
        _home.Click += (_, _) => NavigateHome();
        _reload.Click += (_, _) => _web.CoreWebView2?.Reload();
        Shown += async (_, _) => await InitializeAsync();
        FormClosed += (_, _) =>
        {
            foreach (var popup in _popups.ToArray())
            {
                if (!popup.IsDisposed) popup.Close();
            }
            _web.Dispose();
        };
    }

    private void NavigateHome()
    {
        if (_web.CoreWebView2 is not null)
            _web.CoreWebView2.Navigate(ChatGptHome.AbsoluteUri);
    }

    private async Task InitializeAsync()
    {
        try
        {
            Directory.CreateDirectory(ProfileFolder);
            var environment = await CoreWebView2Environment.CreateAsync(
                userDataFolder: ProfileFolder);
            if (IsDisposed) return;
            await _web.EnsureCoreWebView2Async(environment);
            if (IsDisposed) return;
            // Only WebView navigation/authentication. No DOM access and no
            // application-level message or response interception.
            _web.CoreWebView2.NavigationStarting += (_, _) =>
            {
                if (!IsDisposed) _status.Text = "Загрузка страницы авторизации…";
            };
            _web.CoreWebView2.NavigationCompleted += (_, args) =>
            {
                if (IsDisposed) return;
                _status.Text = args.IsSuccess
                    ? "Сессия WebView2 хранится локально. Вход завершите на странице ChatGPT."
                    : "Ошибка загрузки ChatGPT: " + args.WebErrorStatus;
            };
            _web.CoreWebView2.NewWindowRequested += OnNewWindowRequested;
            _web.CoreWebView2.Navigate(ChatGptHome.AbsoluteUri);
        }
        catch (Exception exception)
        {
            if (!IsDisposed)
                _status.Text = "Не удалось открыть WebView2: " +
                    exception.GetType().Name;
            AgentStorage.Log("ChatGPT WebView2 initialization: " +
                exception.GetType().Name);
        }
    }

    private async void OnNewWindowRequested(object? sender,
        CoreWebView2NewWindowRequestedEventArgs args)
    {
        // OAuth sign-in may open a popup. Keep it in the same WebView2
        // environment instead of forwarding it to an unrelated browser profile.
        var deferral = args.GetDeferral();
        Form? popup = null;
        try
        {
            var engine = _web.CoreWebView2;
            if (engine is null || IsDisposed)
            {
                args.Handled = true;
                return;
            }
            popup = new Form
            {
                Text = "ChatGPT — окно авторизации",
                Size = new Size(650, 750),
                StartPosition = FormStartPosition.CenterParent
            };
            var browser = new WebView2 { Dock = DockStyle.Fill };
            popup.Controls.Add(browser);
            popup.FormClosed += (_, _) =>
            {
                _popups.Remove(popup);
                browser.Dispose();
            };
            _popups.Add(popup);
            popup.Show(this);
            await browser.EnsureCoreWebView2Async(engine.Environment);
            if (!popup.IsDisposed && !IsDisposed)
                args.NewWindow = browser.CoreWebView2;
            else args.Handled = true;
        }
        catch (Exception exception)
        {
            args.Handled = true;
            if (popup is { IsDisposed: false }) popup.Close();
            AgentStorage.Log("ChatGPT auth popup: " +
                exception.GetType().Name);
        }
        finally
        {
            deferral.Complete();
        }
    }
}
