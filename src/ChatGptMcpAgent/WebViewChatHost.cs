using System.ComponentModel;
using System.Net.Sockets;
using System.Diagnostics;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace ChatGptMcpAgent;

// The only chat transport. No separate Chromium/Node child, API key or browser tabs.
// All WebView2/CDP calls execute on the WinForms UI thread.
internal sealed class WebViewChatHost : Form
{
    private static WebViewChatHost? _current;
    private readonly WebView2 _web = new() { Dock = DockStyle.Fill };
    private readonly Label _note = new() { Dock = DockStyle.Bottom, Height = 36,
        Text = "Авторизация ChatGPT во встроенном WebView2", Padding = new Padding(10, 7, 0, 0) };
    private readonly Button _closeLogin = new() { Text = "Вернуться в чат", Dock = DockStyle.Right, Width = 155 };
    private readonly Button _refreshPage = new() { Text = "Обновить", Dock = DockStyle.Right, Width = 120 };
    private readonly Panel _toolbar = new() { Dock = DockStyle.Top, Height = 42 };
    private readonly List<Form> _popups = new();
    private readonly System.Windows.Forms.Timer _tick = new() { Interval = 1400 };
    private bool _initialized, _busy, _closing, _loginVisible, _loggedIn;
    private DateTimeOffset _catalogNext = DateTimeOffset.MinValue;
    private readonly DateTimeOffset _activationAt;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static string Root => Environment.GetEnvironmentVariable("CHATGPT_MCP_CHAT_TEST_ROOT") ??
        Path.Combine(AgentStorage.DataFolder, "Chat");
    private static string Bridge => Path.Combine(Root, "webview-bridge");
    private static string Messages => Path.Combine(Root, "messages");
    private static string Claims => Path.Combine(Root, "browser-claims");
    private static string Bindings => Path.Combine(Root, "browser-threads");
    private static string Sessions => Path.Combine(Root, "sessions");
    private static string Partials => Path.Combine(Root, "browser-partials");
    private static string History => Path.Combine(Root, "browser-history");
    private static string Activation => Path.Combine(Bridge, "activated.json");
    private static readonly Regex CloudUrl = new(
        @"^https://chatgpt\.com/c/[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private const string Home = "https://chatgpt.com/";
    private const string Editor = "#prompt-textarea,[data-testid='composer-text-input'],[contenteditable='true'][role='textbox']";
    private const string SnapshotScript = """
        (() => {
          const editor = document.querySelector("#prompt-textarea,[data-testid='composer-text-input'],[contenteditable='true'][role='textbox']");
          const modern = [...document.querySelectorAll("h4[data-conversation-role=assistant]")];
          const legacy = [...document.querySelectorAll("[data-message-author-role=assistant]")];
          const userHeadings = [...document.querySelectorAll("h4[data-conversation-role=user]")];
          let last = "";
          if (modern.length) {
            const p = modern.at(-1).parentElement;
            last = (p?.querySelector('[data-markdown-text-style=assistant-message]')?.innerText ||
              p?.innerText?.replace(/^ChatGPT[^\n]*\n/, "") || "").trim();
          } else last = (legacy.at(-1)?.innerText || "").trim();
          return {
            url: location.origin + location.pathname,
            ready: !!editor, assistants: modern.length || legacy.length,
            users: userHeadings.length || document.querySelectorAll("[data-message-author-role=user]").length,
            last: last,
            generating: !!document.querySelector('[data-testid=stop-button],button[aria-label^=Stop],button[aria-label^=Остановить]')
          };
        })()
        """;
    private sealed class Snapshot
    {
        public string Url { get; set; } = "";
        public bool Ready { get; set; }
        public int Assistants { get; set; }
        public int Users { get; set; }
        public string Last { get; set; } = "";
        public bool Generating { get; set; }
    }
    private sealed class Attachment
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public long SizeBytes { get; set; }
        public string Sha256 { get; set; } = "";
    }
    private sealed class Message
    {
        public string Id { get; set; } = "";
        public string ChatId { get; set; } = "";
        public string Status { get; set; } = "";
        public string Source { get; set; } = "";
        public DateTimeOffset CreatedUtc { get; set; }
        public string Text { get; set; } = "";
        public string? Reply { get; set; }
        public DateTimeOffset? RepliedUtc { get; set; }
        public List<Attachment> Attachments { get; set; } = new();
    }
    private sealed class Claim
    {
        public string Id { get; set; } = "";
        public string ChatId { get; set; } = "";
        public string State { get; set; } = "";
        public int Baseline { get; set; }
        public string Url { get; set; } = "";
        public DateTimeOffset At { get; set; } = DateTimeOffset.UtcNow;
    }

    internal static bool IsCreated => _current is { IsDisposed: false };
    internal static bool IsReady => _current is { _loggedIn: true, _initialized: true };
    internal static string Status => _current is { _initialized: true } host
        ? host._loggedIn ? "webview" : "waiting_login" : "starting";

    internal static void Start()
    {
        if (_current is { IsDisposed: false }) return;
        var host = new WebViewChatHost();
        _current = host;
        // Keep the browser engine actually visible to the compositor, but physically
        // outside all monitors. Hidden controls can suspend timers and network requests.
        host.Show();
        host.SendToBackground();
    }

    internal static void ShowSignIn(IWin32Window? owner = null)
    {
        if (!IsCreated) Start();
        _current?.Reveal();
    }

    internal static void Shutdown()
    {
        if (_current is not { IsDisposed: false } host) return;
        _current = null;
        host._closing = true;
        host._tick.Stop();
        foreach (var popup in host._popups.ToArray())
            if (!popup.IsDisposed) popup.Close();
        host.Close();
        host.Dispose();
    }

    private WebViewChatHost()
    {
        Text = "ChatGPT MCP Tunnel — Вход в ChatGPT (WebView2)";
        Icon = TrayIcons.AppIcon();
        Size = new Size(1120, 800);
        MinimumSize = new Size(720, 540);
        Font = new Font("Segoe UI", 10);
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        _toolbar.Padding = new Padding(5);
        _toolbar.Controls.Add(_closeLogin);
        _toolbar.Controls.Add(_refreshPage);
        Controls.Add(_web);
        Controls.Add(_note);
        Controls.Add(_toolbar);
        _closeLogin.Click += (_, _) => SendToBackground();
        _refreshPage.Click += (_, _) => _web.CoreWebView2?.Reload();
        FormClosing += (_, args) => { if (!_closing && args.CloseReason == CloseReason.UserClosing)
        { args.Cancel = true; SendToBackground(); } };
        Shown += async (_, _) => await InitializeAsync();
        _tick.Tick += async (_, _) => await TickAsync();
        _activationAt = ReadActivation();
        AppTheme.Apply(this);
    }

    private static DateTimeOffset ReadActivation()
    {
        Directory.CreateDirectory(Bridge);
        try
        {
            if (File.Exists(Activation))
            {
                using var json = JsonDocument.Parse(File.ReadAllText(Activation));
                if (DateTimeOffset.TryParse(json.RootElement.GetProperty("at").GetString(), out var at))
                    return at;
            }
        }
        catch (IOException) { }
        catch (JsonException) { }
        var now = DateTimeOffset.UtcNow;
        Atomic(Activation, new { at = now.ToString("O") });
        return now;
    }

    private void SendToBackground()
    {
        if (_closing || IsDisposed) return;
        _loginVisible = false;
        ShowInTaskbar = false;
        WindowState = FormWindowState.Normal;
        Opacity = .01;
        Location = new Point(-30000, -30000);
    }

    private void Reveal()
    {
        if (IsDisposed || _closing) return;
        _loginVisible = true;
        Opacity = 1;
        StartPosition = FormStartPosition.Manual;
        var area = (Screen.PrimaryScreen ?? Screen.FromPoint(Cursor.Position)).WorkingArea;
        Location = new Point(area.Left + Math.Max(0, (area.Width - Width) / 2),
            area.Top + Math.Max(0, (area.Height - Height) / 2));
        ShowInTaskbar = true;
        if (!Visible) Show();
        BringToFront();
        Activate();
        if (_initialized && !_loggedIn && _web.CoreWebView2 is not null)
            _web.CoreWebView2.Navigate(Home);
    }

    private async Task InitializeAsync()
    {
        try
        {
            // Happ exposes a local HTTP proxy on DOMKAT. Windows does not expose
            // it as a system proxy, so WebView2 cannot reach ChatGPT directly.
            // Detect the local proxy and pass it to the Chromium engine itself.
            // This does not change Happ or any machine-wide proxy settings.
            var profile = Environment.GetEnvironmentVariable("CHATGPT_MCP_CHAT_TEST_ROOT") is string testRoot
                ? Path.Combine(testRoot, "WebView2Profile")
                : ChatGptWebViewAuthForm.ProfileFolder;
            Directory.CreateDirectory(profile);
            var args = WebViewNetworkProxy.BrowserArguments();
            var options = new CoreWebView2EnvironmentOptions();
            options.AdditionalBrowserArguments = args;
            AgentStorage.Log(args.Length == 0
                ? "WebView2 network: direct (Happ local proxy unavailable)"
                : "WebView2 network: Happ local proxy selected");
            var env = await CoreWebView2Environment.CreateAsync(
                userDataFolder: profile, options: options);
            if (_closing || IsDisposed) return;
            await _web.EnsureCoreWebView2Async(env);
            if (_closing || IsDisposed) return;
            _web.CoreWebView2.NewWindowRequested += NewWindow;
            _web.CoreWebView2.NavigationCompleted += (_, e) =>
            {
                Atomic(Path.Combine(Bridge, "navigation.json"), new {
                    at = DateTimeOffset.UtcNow, success = e.IsSuccess,
                    status = e.WebErrorStatus.ToString(),
                    origin = "chatgpt.com"
                });
                if (_loginVisible)
                    _note.Text = e.IsSuccess
                        ? "ChatGPT загружен через WebView2. Авторизуйтесь на странице."
                        : "Не удалось загрузить ChatGPT: " + e.WebErrorStatus;
            };
            _web.CoreWebView2.Navigate(Home);
            _initialized = true;
            _tick.Start();
            Beat("waiting_login");
        }
        catch (Exception e)
        {
            _note.Text = "Ошибка запуска WebView2: " + e.GetType().Name;
            AgentStorage.Log("WebView2 chat host startup: " + e.GetType().Name);
            Beat("error");
        }
    }

    private async void NewWindow(object? sender, CoreWebView2NewWindowRequestedEventArgs args)
    {
        // OAuth providers can require a separate window. It shares this very same
        // WebView2 environment, with no external Chromium and no cookie copying.
        var deferral = args.GetDeferral();
        Form? popup = null;
        try
        {
            popup = new Form
            {
                Text = "ChatGPT — авторизация", Icon = TrayIcons.AppIcon(),
                Size = new Size(710, 780), StartPosition = FormStartPosition.CenterScreen
            };
            var child = new WebView2 { Dock = DockStyle.Fill };
            popup.Controls.Add(child);
            var form = popup;
            popup.FormClosed += (_, _) => { _popups.Remove(form); child.Dispose(); };
            _popups.Add(popup);
            popup.Show();
            await child.EnsureCoreWebView2Async(_web.CoreWebView2!.Environment);
            if (!popup.IsDisposed && !_closing)
                args.NewWindow = child.CoreWebView2;
            else args.Handled = true;
        }
        catch (Exception e)
        {
            args.Handled = true;
            AgentStorage.Log("WebView2 sign-in popup: " + e.GetType().Name);
            if (popup is not null && !popup.IsDisposed) popup.Close();
        }
        finally { deferral.Complete(); }
    }

    private async Task TickAsync()
    {
        if (!_initialized || _closing || IsDisposed) return;
        Beat(_loggedIn ? "webview" : "waiting_login");
        if (_busy) return;
        _busy = true;
        try
        {
            if (!_loggedIn)
            {
                var state = await StateAsync();
                if (state?.Ready == true && Uri.TryCreate(state.Url, UriKind.Absolute, out var uri) &&
                    uri.Host.Equals("chatgpt.com", StringComparison.OrdinalIgnoreCase))
                {
                    _loggedIn = true;
                    _note.Text = "Вход выполнен. Теперь используйте окно чата MCP Tunnel.";
                    if (_loginVisible) SendToBackground();
                }
                else return;
            }
            if (await RecoverAsync()) return;
            var next = FindPending();
            if (next is not null)
            {
                await DeliverAsync(next);
                return;
            }
            if (DateTimeOffset.UtcNow > _catalogNext)
            {
                _catalogNext = DateTimeOffset.UtcNow.AddMinutes(2);
                await CatalogAsync();
            }
            await ImportedHistoryAsync();
        }
        catch (Exception e)
        {
            Event("worker_error", new { reason = e.GetType().Name, message = e.Message[..Math.Min(150,e.Message.Length)] });
        }
        finally { _busy = false; }
    }

    private static void Atomic(string file, object value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        var tmp = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(value), new UTF8Encoding(false));
        File.Move(tmp, file, overwrite: true);
    }
    private static T? Read<T>(string file)
    {
        try { return File.Exists(file) ? JsonSerializer.Deserialize<T>(File.ReadAllText(file)) : default; }
        catch (IOException) { return default; }
        catch (JsonException) { return default; }
    }
    private static bool SafeUrl(string? url) => url is not null && CloudUrl.IsMatch(url);
    private static bool Id(string? id) => id is { Length: 32 } && id.All(Uri.IsHexDigit);
    private static void Event(string type, object? details = null)
    {
        try
        {
            Directory.CreateDirectory(Bridge);
            File.AppendAllText(Path.Combine(Bridge, "events.jsonl"),
                JsonSerializer.Serialize(new { at = DateTimeOffset.UtcNow, type, details }) + Environment.NewLine);
        }
        catch (IOException) { }
    }
    private void Beat(string mode) => Atomic(Path.Combine(Bridge, "heartbeat.json"),
        new { at = DateTimeOffset.UtcNow, pid = Environment.ProcessId, mode });

    private Message? FindPending()
    {
        if (!Directory.Exists(Messages)) return null;
        foreach (var m in Directory.EnumerateFiles(Messages, "*.json")
                     .Select(Read<Message>).Where(m => m is not null)
                     .OrderBy(m => m!.CreatedUtc))
        {
            if (m is null || !Id(m.Id) || !Id(m.ChatId) || m.Status != "pending" ||
                m.Source != "user" || (m.Text.Trim().Length == 0 && m.Attachments.Count == 0) ||
                m.CreatedUtc < _activationAt || !string.IsNullOrEmpty(m.Reply) ||
                File.Exists(Path.Combine(Claims, m.Id + ".json")) ||
                File.Exists(Path.Combine(Root, "ui-forwarded", m.Id + ".json"))) continue;
            if (Route(m.ChatId) is not null) return m;
        }
        return null;
    }
    private static string? Route(string chatId)
    {
        var bound = Read<JsonElement>(Path.Combine(Bindings, chatId + ".json"));
        if (bound.ValueKind == JsonValueKind.Object &&
            bound.TryGetProperty("url", out var url) && SafeUrl(url.GetString()))
            return url.GetString();
        var old = Read<JsonElement>(Path.Combine(Root, "thread-bindings", chatId + ".json"));
        if (old.ValueKind == JsonValueKind.Object && old.TryGetProperty("Url", out var v) &&
            SafeUrl(v.GetString())) return v.GetString();
        var session = Read<ChatSession>(Path.Combine(Sessions, chatId + ".json"));
        if (SafeUrl(session?.CloudUrl)) return session!.CloudUrl;
        return session?.CreateCloudThread == true ? Home : null;
    }
    private static void Bind(string chatId, string url)
    {
        if (!SafeUrl(url) || !Id(chatId)) throw new InvalidDataException("Недопустимый адрес диалога");
        var path = Path.Combine(Bindings, chatId + ".json");
        var prior = Read<JsonElement>(path);
        if (prior.ValueKind == JsonValueKind.Object &&
            prior.TryGetProperty("url", out var value) && value.GetString() != url)
            throw new InvalidOperationException("Конфликт привязки облачного диалога");
        Atomic(path, new { chatId, url });
    }
    private async Task<T?> Js<T>(string expression)
    {
        if (_web.CoreWebView2 is null) return default;
        var result = await _web.CoreWebView2.ExecuteScriptAsync(expression);
        return result is "null" or "" ? default : JsonSerializer.Deserialize<T>(result, _jsonOptions);
    }
    private Task<Snapshot?> StateAsync() => Js<Snapshot>(SnapshotScript);
    private async Task<Snapshot?> NavigateAsync(string url)
    {
        if (url != Home && !SafeUrl(url)) throw new ArgumentException("Only canonical ChatGPT thread URLs allowed");
        var current = await StateAsync();
        if (current?.Url != url || current.Ready != true)
            _web.CoreWebView2!.Navigate(url);
        for (var i = 0; i < 75; i++)
        {
            await Task.Delay(500);
            current = await StateAsync();
            if (current?.Ready == true && current.Url == url) return current;
        }
        _loggedIn = false;
        return null;
    }

    private async Task SendAsync(string text)
    {
        if (!await Js<bool>("(()=>{const e=document.querySelector(" +
                JsonSerializer.Serialize(Editor) +
                ");if(!e)return false;e.focus();return document.activeElement===e||e.contains(document.activeElement)})()"))
            throw new InvalidOperationException("Не найдено поле ввода ChatGPT");
        if (text.Length != 0)
        {
            await _web.CoreWebView2!.CallDevToolsProtocolMethodAsync(
                "Input.insertText", JsonSerializer.Serialize(new { text }));
            await Task.Delay(450);
            var actual = await Js<string>("document.querySelector(" +
                JsonSerializer.Serialize(Editor) + ")?.innerText||''");
            if (actual is null || !actual.Contains(text, StringComparison.Ordinal))
                throw new InvalidOperationException("Текст не появился в редакторе ChatGPT");
        }
        if (!await Js<bool>("(()=>{const b=document.querySelector(" +
            JsonSerializer.Serialize("[data-testid='send-button'],button[aria-label='Send prompt'],button[aria-label='Отправить сообщение']") +
            ");if(!b||b.disabled)return false;b.click();return true})()"))
        {
            await _web.CoreWebView2!.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent",
                """{"type":"keyDown","key":"Enter","code":"Enter","windowsVirtualKeyCode":13}""");
            await _web.CoreWebView2!.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent",
                """{"type":"keyUp","key":"Enter","code":"Enter","windowsVirtualKeyCode":13}""");
        }
    }
    private async Task AttachAsync(Message m)
    {
        if (m.Attachments.Count == 0) return;
        if (m.Attachments.Count > 20) throw new InvalidOperationException("Слишком много вложений");
        var files = new List<string>();
        foreach (var a in m.Attachments)
        {
            if (!Id(a.Id)) throw new InvalidDataException("Invalid attachment ID");
            var file = Path.Combine(Root, "attachments", m.Id, a.Id);
            var info = new FileInfo(file);
            if (!info.Exists || info.Length != a.SizeBytes) throw new InvalidDataException("Attachment length");
            using var input = info.OpenRead();
            var sha = Convert.ToHexString(SHA256.HashData(input));
            if (!sha.Equals(a.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Attachment SHA256 mismatch");
            files.Add(file);
        }
        async Task<int> FileNode()
        {
            using var dom = JsonDocument.Parse(await _web.CoreWebView2!.CallDevToolsProtocolMethodAsync(
                "DOM.getDocument", """{"depth":1,"pierce":true}"""));
            var rootId = dom.RootElement.GetProperty("root").GetProperty("nodeId").GetInt32();
            using var found = JsonDocument.Parse(await _web.CoreWebView2!.CallDevToolsProtocolMethodAsync(
                "DOM.querySelector", JsonSerializer.Serialize(new { nodeId = rootId, selector = "input[type=file]" })));
            return found.RootElement.GetProperty("nodeId").GetInt32();
        }
        var id = await FileNode();
        if (id == 0)
        {
            await Js<bool>("(()=>{const b=document.querySelector('[data-testid=composer-plus-btn],button[aria-label*=Attach],button[aria-label*=Добавить]');if(b)b.click();return !!b})()");
            await Task.Delay(450);
            id = await FileNode();
        }
        if (id == 0) throw new InvalidOperationException("ChatGPT не показывает поле прикрепления файлов");
        await _web.CoreWebView2!.CallDevToolsProtocolMethodAsync("DOM.setFileInputFiles",
            JsonSerializer.Serialize(new { files, nodeId = id }));
        await Task.Delay(1800);
    }

    private async Task<(string Reply, string Url)> WaitReplyAsync(int baseline, string messageId)
    {
        var last = ""; DateTimeOffset stable = DateTimeOffset.MinValue;
        for (var i = 0; i < 160; i++)
        {
            await Task.Delay(1150);
            var s = await StateAsync();
            if (s is null) continue;
            if (s.Assistants > baseline && !string.IsNullOrWhiteSpace(s.Last))
            {
                Atomic(Path.Combine(Partials, messageId + ".json"),
                    new { text = s.Last, at = DateTimeOffset.UtcNow });
                if (!s.Generating)
                {
                    if (last != s.Last) { last = s.Last; stable = DateTimeOffset.UtcNow; }
                    else if (DateTimeOffset.UtcNow - stable > TimeSpan.FromSeconds(3))
                        return (s.Last, s.Url);
                }
                else stable = DateTimeOffset.MinValue;
            }
        }
        throw new TimeoutException("ChatGPT response timed out. Message retained for reconciliation.");
    }

    private async Task DeliverAsync(Message m)
    {
        var url = Route(m.ChatId);
        if (url is null) return;
        var before = await NavigateAsync(url);
        if (before is null) return;
        await AttachAsync(m);
        var claim = new Claim { Id = m.Id, ChatId = m.ChatId, Baseline = before.Assistants,
            State = "reserved", Url = url };
        Atomic(Path.Combine(Claims, m.Id + ".json"), claim);
        try
        {
            await SendAsync(m.Text);
            string? cloud = null;
            for (var i = 0; i < 45; i++)
            {
                await Task.Delay(250);
                var state = await StateAsync();
                if (SafeUrl(state?.Url)) { cloud = state!.Url; break; }
            }
            claim.Url = cloud ?? url;
            claim.State = "sent";
            claim.At = DateTimeOffset.UtcNow;
            Atomic(Path.Combine(Claims, m.Id + ".json"), claim);
            if (SafeUrl(cloud) && url == Home) Bind(m.ChatId, cloud!);
            Event("message_sent", new { m.Id, m.ChatId });
            var result = await WaitReplyAsync(before.Assistants, m.Id);
            if (!SafeUrl(result.Url)) throw new InvalidDataException("Cloud URL not confirmed");
            if (url == Home) Bind(m.ChatId, result.Url);
            Answer(m, result.Reply);
            claim.State = "answered"; claim.Url = result.Url;
            Atomic(Path.Combine(Claims, m.Id + ".json"), claim);
            Event("answer_saved", new { m.Id, m.ChatId, length = result.Reply.Length });
        }
        catch (Exception e)
        {
            Event("send_not_confirmed", new { m.Id, reason = e.GetType().Name });
            // Reserved/sent is never silently retried: this avoids duplicate prompts.
        }
    }
    private static void Answer(Message m, string reply)
    {
        var file = Path.Combine(Messages, m.Id + ".json");
        var current = Read<Message>(file);
        if (current?.Status != "pending" || current.ChatId != m.ChatId)
            throw new InvalidOperationException("Queued message was changed");
        current.Reply = reply;
        current.RepliedUtc = DateTimeOffset.UtcNow;
        current.Status = "answered";
        Atomic(file, current);
    }

    private async Task<bool> RecoverAsync()
    {
        if (!Directory.Exists(Claims)) return false;
        foreach (var file in Directory.EnumerateFiles(Claims, "*.json"))
        {
            var claim = Read<Claim>(file);
            if (claim?.State != "sent" || !SafeUrl(claim.Url) ||
                claim.At < _activationAt ||
                DateTimeOffset.UtcNow - claim.At < TimeSpan.FromSeconds(20)) continue;
            var m = Read<Message>(Path.Combine(Messages, claim.Id + ".json"));
            if (m?.Status != "pending" || m.ChatId != claim.ChatId) continue;
            var snap = await NavigateAsync(claim.Url);
            if (snap is null) return true;
            var response = await WaitReplyAsync(claim.Baseline, m.Id);
            Answer(m, response.Reply);
            if (Read<ChatSession>(Path.Combine(Sessions, m.ChatId + ".json"))?.CreateCloudThread == true)
                Bind(m.ChatId, claim.Url);
            claim.State = "answered";
            Atomic(file, claim);
            Event("reply_recovered", new { m.Id });
            return true;
        }
        return false;
    }

    private async Task CatalogAsync()
    {
        if (await NavigateAsync(Home) is null) return;
        var items = await Js<List<JsonElement>>("""
            (() => Array.from(document.querySelectorAll("a[href^='/c/']")).map(a => ({
              url: new URL(a.getAttribute("href"),location.origin).origin +
                new URL(a.getAttribute("href"),location.origin).pathname,
              title: (a.innerText || a.getAttribute("aria-label") || "").trim()
            })))()
            """);
        if (items is null) return;
        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var output = new List<object>();
        foreach (var e in items)
        {
            var url = e.GetProperty("url").GetString();
            if (!SafeUrl(url) || !unique.Add(url!)) continue;
            output.Add(new { url, title = e.GetProperty("title").GetString() });
        }
        Atomic(Path.Combine(Bridge, "catalog.json"), new { at = DateTimeOffset.UtcNow, items = output });
    }

    private async Task ImportedHistoryAsync()
    {
        if (!Directory.Exists(Sessions)) return;
        foreach (var file in Directory.EnumerateFiles(Sessions, "*.json"))
        {
            var session = Read<ChatSession>(file);
            if (!SafeUrl(session?.CloudUrl)) continue;
            var target = Path.Combine(History, session!.Id + ".json");
            if (File.Exists(target)) continue;
            if (await NavigateAsync(session.CloudUrl!) is null) return;
            var items = await Js<List<JsonElement>>("""
                (() => [...document.querySelectorAll('h4[data-conversation-role], [data-message-author-role]')]
                  .map(el => {
                    const role = el.getAttribute('data-conversation-role') || el.getAttribute('data-message-author-role');
                    if (!['user','assistant'].includes(role)) return null;
                    const p = el.getAttribute('data-conversation-role') ? el.parentElement : el;
                    const body = role === 'assistant'
                      ? p?.querySelector('[data-markdown-text-style=assistant-message]') : null;
                    const text = (body?.innerText || p?.innerText || '').replace(/^(ChatGPT|Вы сказали)[^\n]*\n/,'').trim();
                    return {role,text};
                  }).filter(x=>x&&x.text))()
                """);
            if (items is null || items.Count == 0) return;
            Atomic(target, new { at = DateTimeOffset.UtcNow, url = session.CloudUrl, items });
            return;
        }
    }

    internal static bool SelfTestConfig()
    {
        return CloudUrl.IsMatch("https://chatgpt.com/c/01234567-89ab-cdef-0123-456789abcdef") &&
            !CloudUrl.IsMatch("https://chatgpt.com.work/c/01234567-89ab-cdef-0123-456789abcdef") &&
            SnapshotScript.Contains("data-conversation-role=assistant") &&
            ChatGptWebViewAuthForm.ProfileFolder.Contains("WebView2");
    }
}
