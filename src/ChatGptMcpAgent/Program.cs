using System.Diagnostics;
using System.Drawing;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace ChatGptMcpAgent;

internal static class Program
{
    private const string ShutdownEventName = @"Local\ChatGptMcpAgentShutdown";
    private const string SettingsEventName = @"Local\ChatGptMcpAgentShowSettings";
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--admin-service")
        {
            Environment.ExitCode = AdminService.RunAsService();
            return;
        }
        if (args.Length == 2 && args[0] == "--admin-install")
        {
            Environment.ExitCode = AdminService.InstallElevated(args[1]);
            return;
        }
        if (args.Length == 1 && args[0] == "--admin-uninstall")
        {
            Environment.ExitCode = AdminService.UninstallElevated();
            return;
        }
        if (args.Length == 1 && args[0] == "--test-admin-pipe-exec")
        {
            Environment.ExitCode = AdminService.SelfTestCommandAsync().GetAwaiter().GetResult() ? 0 : 105;
            return;
        }
        if (args.Length == 1 && args[0] == "--test-admin-service")
        {
            Environment.ExitCode = AdminService.SelfTestAsync().GetAwaiter().GetResult() ? 0 : 104;
            return;
        }
        if (args.Length == 2 && args[0] == "--configure-wan-firewall")
        {
            Environment.ExitCode = LanFirewall.ConfigureElevatedWan(args[1]);
            return;
        }
        if (args.Length == 3 && args[0] == "--configure-tailnet-firewall")
        {
            Environment.ExitCode = LanFirewall.ConfigureElevatedTailnet(args[1], args[2]);
            return;
        }
        if (args.Length == 1 && args[0] == "--test-tailnet-firewall")
        {
            Environment.ExitCode = LanFirewall.TestTailnetRanges() ? 0 : 44;
            return;
        }
        if (args.Length == 2 && args[0] == "--configure-lan-firewall")
        {
            Environment.ExitCode = LanFirewall.ConfigureElevated(args[1]);
            return;
        }
        if (args.Length == 1 && args[0] == "--test-voice-dom")
        {
            ApplicationConfiguration.Initialize();
            Environment.ExitCode = VoiceDomTests.Run();
            return;
        }
        if (args.Length == 1 && args[0] == "--test-server-adoption")
        {
            // Pure path classification and live read-only discovery.
            // Never starts, terminates, or reconnects a tunnel.
            Environment.ExitCode = ServerProcessOwnership.SelfTest() ? 0 : 46;
            return;
        }
        if (args.Length == 1 && args[0] == "--test-client-errors")
        {
            Environment.ExitCode =
                TrayContext.ClientStatusFromLine("Router Lite agent 'CLIENT' connected.") == "Подключён" &&
                TrayContext.ClientStatusFromLine("Router Lite agent: WebSocketException: server status code '401'") == "Ключ отклонён" &&
                TrayContext.ClientStatusFromLine("Router Lite agent: WebSocketException: server status code '409'") == "Имя компьютера занято" &&
                TrayContext.ClientStatusFromLine("Router Lite agent: WebSocketException: destination unavailable") == "Нет соединения" &&
                TrayContext.ClientStatusFromLine("ordinary status") is null ? 0 : 35;
            return;
        }
        if (args.Length == 1 && args[0] == "--test-lan-firewall")
        {
            Environment.ExitCode = LanFirewall.TestLocalNetwork() ? 0 : 34;
            return;
        }
        if (args.Length == 1 && args[0] == "--test-updater-download")
        {
            Environment.ExitCode = AgentUpdates.TestDownloadStagingAsync()
                .GetAwaiter().GetResult();
            return;
        }
        if (args.Length == 1 && args[0] == "--watchdog-check")
        {
            Environment.ExitCode = AgentWatchdog.Check();
            return;
        }
        if (args.Length == 1 && args[0] == "--watchdog-register")
        {
            Environment.ExitCode = AgentWatchdog.Register();
            return;
        }
        if (args.Length == 1 && args[0] == "--watchdog-unregister")
        {
            Environment.ExitCode = AgentWatchdog.Unregister();
            return;
        }
        if (args.Length == 1 && args[0] == "--test-watchdog")
        {
            Environment.ExitCode = AgentWatchdog.SelfTest();
            return;
        }
        if (args.Contains("--selftest", StringComparer.OrdinalIgnoreCase))
        {
            Environment.ExitCode = AgentStorage.RunSelfTest();
            return;
        }
        if (args.Length == 1 && args[0] == "--uac-elevated-check")
        {
            Environment.ExitCode = new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator) ? 0 : 5;
            return;
        }
        if (args.Length == 1 && args[0] == "--test-settings-hub")
        {
            ApplicationConfiguration.Initialize();
            using var dialog = new SettingsHubDialog(() => { }, () => Task.CompletedTask,
                () => { }, () => { });
            Environment.ExitCode = dialog.Controls.OfType<TabControl>().Any(t => t.TabPages.Count >= 6) ? 0 : 92;
            return;
        }
        if (args.Length == 1 && args[0] == "--test-toolbar-dom")
        {
            ApplicationConfiguration.Initialize();
            Environment.ExitCode = ToolbarUiTests.Run();
            return;
        }
        if (args.Length == 1 && args[0] == "--test-main-chat-ui")
        {
            ApplicationConfiguration.Initialize();
            using var main = new ChatGptMainForm();
            Environment.ExitCode = main.HasCustomCaption && main.HasBrowserChrome &&
                main.TabCount == 0 &&
                !main.Controls.OfType<RichTextBox>().Any() &&
                main.Text.Contains("ChatGPT MCP Tunnel v", StringComparison.Ordinal) ? 0 : 102;
            return;
        }
        if (args.Length == 1 && args[0] == "--test-main-chat-live")
        {
            // Isolated browser profile: no user cookies, no cloud messages.
            ApplicationConfiguration.Initialize();
            var isolated = Path.Combine(Path.GetTempPath(), "McpWebViewMain-" + Guid.NewGuid().ToString("N"));
            var old = Environment.GetEnvironmentVariable("CHATGPT_MCP_WEBVIEW_TEST_PROFILE");
            Environment.SetEnvironmentVariable("CHATGPT_MCP_WEBVIEW_TEST_PROFILE", isolated);
            try
            {
                using var main = new ChatGptMainForm();
                using var timer = new System.Windows.Forms.Timer { Interval = 18000 };
                timer.Tick += async (_, _) =>
                {
                    timer.Stop();
                    var navigationOk = main.BrowserInitialized && main.LastNavigationSucceeded &&
                        main.CurrentPage?.StartsWith("https://chatgpt.com/", StringComparison.Ordinal) == true;
                    var chromeOk = main.ChromeInitialized && await main.SmokeChromeAsync();
                    var tabsOk = await main.SmokeTabsAsync();
                    Environment.ExitCode = navigationOk && chromeOk && tabsOk &&
                        main.HasCustomCaption && main.HasBrowserChrome &&
                        await main.PaletteAppliedAsync() &&
                        await main.ToolbarScriptLoadedAsync() ? 0 : 103;
                    if (Environment.GetEnvironmentVariable("CHATGPT_MCP_PALETTE_PROBE") == "1" &&
                        main.BrowserControl.CoreWebView2 is { } browser)
                    {
                        var probe = await browser.ExecuteScriptAsync("""
                            (() => {
                                const points=[[.2,.5],[.5,.5],[.75,.35],[.05,.25]];
                                const result=points.map(([x,y])=>{
                                    let el=document.elementFromPoint(Math.floor(innerWidth*x),Math.floor(innerHeight*y));
                                    const parents=[];
                                    for(let i=0;el&&i<9;i++,el=el.parentElement){
                                        const css=getComputedStyle(el);
                                        parents.push({tag:el.tagName,cls:typeof el.className==='string'?el.className.slice(0,160):'',
                                            id:el.id,bg:css.backgroundColor});
                                    }
                                    return {x,y,parents};
                                });
                                return {points:result,body:getComputedStyle(document.body).backgroundColor,
                                vars:['--main-surface-primary','--sidebar-surface-primary','--main-surface-secondary']
                                    .map(k=>({key:k,value:getComputedStyle(document.documentElement).getPropertyValue(k)}))};
                            })()
                            """);
                        File.WriteAllText(Path.Combine(Path.GetTempPath(), "ChatGptMcpPaletteProbe.json"), probe);
                    }
                    main.ExitApplication();
                };
                timer.Start();
                Application.Run(main);
            }
            finally
            {
                Environment.SetEnvironmentVariable("CHATGPT_MCP_WEBVIEW_TEST_PROFILE", old);
                // Chromium may retain handles momentarily. Never delete the real profile.
                try { if (Directory.Exists(isolated)) Directory.Delete(isolated, true); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
            }
            return;
        }
        if (args.Length == 1 && args[0] == "--test-vpn-webview")
        {
            ApplicationConfiguration.Initialize();
            Environment.ExitCode = VpnWebViewPilotTests.Run();
            return;
        }
        if (args.Length == 1 && args[0] == "--test-webview-network")
        {
            Environment.ExitCode = WebViewNetworkProxy.SelfTest() ? 0 : 98;
            return;
        }
        if (args.Length == 1 && args[0] == "--test-theme")
        {
            ApplicationConfiguration.Initialize();
            Environment.ExitCode = AppTheme.SelfTest() ? 0 : 97;
            return;
        }
        if (args.Length == 1 && args[0] == "--test-overlay-tray")
        {
            Environment.ExitCode = ControlTests.TestTrayIntegration();
            return;
        }
        if (args.Length == 1 && args[0] == "--test-overlay-auto")
        {
            Environment.ExitCode = ControlTests.TestAutomaticOverlay();
            return;
        }
        if (args.Length == 1 && args[0] == "--test-control")
        {
            Environment.ExitCode = ControlTests.Run();
            return;
        }
        if (args.Length == 1 && args[0] == "--test-chat-thread-activator")
        {
            Environment.ExitCode = ChatThreadActivator.SelfTest();
            return;
        }
        if (args.Length == 1 && args[0] == "--test-host-activator")
        {
            Environment.ExitCode = ChatHostActivator.SelfTest();
            return;
        }
        if (args.Length == 1 && args[0] == "--test-transfer-window")
        {
            Application.EnableVisualStyles();
            using var form = new TransferProgressWindow();
            var hwnd = form.Handle;
            if (hwnd == IntPtr.Zero || form.Text.Length < 5)
                throw new InvalidOperationException("Transfer progress window did not initialize.");
            return;
        }
        if (args.Length == 1 && args[0] == "--test-secret-forms")
        {
            try
            {
                var dummy = new AgentSettings
                {
                    Role = "server",
                    TunnelId = "tunnel_" + new string('a', 32),
                    ListenAddress = "127.0.0.1",
                    RouterUrl = "ws://192.168.0.3:55201/agent"
                };
                using var server = new ServerSettingsDialog(dummy);
                using var client = new SettingsDialog(dummy);
                using var entry = new SecretInputDialog("Тест ввода");
                var serverControls = server.Controls.Cast<Control>().ToArray();
                var clientControls = client.Controls.Cast<Control>().ToArray();
                var secretControls = entry.Controls.Cast<Control>().ToArray();
                if (serverControls.OfType<TextBox>().Any(tb => tb.UseSystemPasswordChar) ||
                    clientControls.OfType<TextBox>().Any(tb => tb.UseSystemPasswordChar) ||
                    !secretControls.OfType<TextBox>().Any(tb => tb.UseSystemPasswordChar) ||
                    clientControls.OfType<Button>().Any(b =>
                        b.Text.Contains("копир", StringComparison.OrdinalIgnoreCase) ||
                        b.Text.Contains("показ", StringComparison.OrdinalIgnoreCase)) ||
                    serverControls.OfType<Button>().Count(b => b.Text.Contains("ключ")) < 2)
                    Environment.ExitCode = 14;
            }
            catch { Environment.ExitCode = 15; }
            return;
        }

        if (args.Length == 1 && args[0] == "--test-icons")
        {
            try
            {
                using var icons = new TrayIcons();
                // Ensure all four actual Windows native icon handles can be
                // created with the embedded branding asset, even on Windows CI.
                if (icons.Base.Width < 16 ||
                    icons.ForStatus("Сервер подключён").Handle == IntPtr.Zero ||
                    icons.ForStatus("Подключение…").Handle == IntPtr.Zero ||
                    icons.ForStatus("Нет соединения").Handle == IntPtr.Zero)
                    Environment.ExitCode = 12;
            }
            catch { Environment.ExitCode = 13; }
            return;
        }
        // Read-only diagnostic for the server role. Never launches a second
        // tunnel, reads token plaintext, or modifies user configuration.
        if (args.Length == 1 && args[0] == "--probe-server")
        {
            try
            {
                var settings = AgentStorage.Load();
                if (settings.Role != "server")
                {
                    Environment.ExitCode = 11;
                    return;
                }
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
                var result = ServerStatusProbe.CheckAsync(
                    settings.ListenAddress, cts.Token).GetAwaiter().GetResult();
                Environment.ExitCode = result switch
                {
                    ServerConnectivity.Ready => 0,
                    ServerConnectivity.RouterUnavailable => 8,
                    _ => 7
                };
            }
            catch { Environment.ExitCode = 9; }
            return;
        }
        if (args.Length == 4 && args[0] == "--apply-update")
        {
            if (!int.TryParse(args[1], out var parentId) ||
                !Uri.TryCreate(args[2], UriKind.Absolute, out var url) ||
                !AgentUpdates.IsAllowedInstaller(url) ||
                args[3].Length is < 12 or > 100)
                return;
            AgentUpdates.RunUpdateAsync(parentId, url, args[3]).GetAwaiter().GetResult();
            return;
        }
        if (args.Length == 1 && args[0] == "--settings")
        {
            // Open the settings window in the existing tray instance; never
            // start another OpenAI tunnel or duplicate this process.
            try
            {
                using var existing = EventWaitHandle.OpenExisting(SettingsEventName);
                existing.Set();
            }
            catch (WaitHandleCannotBeOpenedException) { Environment.ExitCode = 10; }
            return;
        }
        if (args.Length == 1 && args[0] == "--shutdown")
        {
            try
            {
                using var evt = EventWaitHandle.OpenExisting(ShutdownEventName);
                evt.Set();
            }
            catch (WaitHandleCannotBeOpenedException) { /* No active tray yet. */ }
            // Inno Setup needs the executable released BEFORE attempting to
            // delete or replace it. Merely sending an event causes a race.
            try
            {
                using var running = Mutex.OpenExisting(@"Local\ChatGptMcpAgent");
                bool finished;
                try { finished = running.WaitOne(TimeSpan.FromSeconds(25)); }
                catch (AbandonedMutexException) { finished = true; }
                if (!finished) { Environment.ExitCode = 6; return; }
                running.ReleaseMutex();
            }
            catch (WaitHandleCannotBeOpenedException) { /* No running agent. */ }
            return;
        }

        using var instance = new Mutex(initiallyOwned: true, @"Local\ChatGptMcpAgent", out var first);
        if (!first) return;
        AgentWatchdog.ClearManualPause();
        ApplicationConfiguration.Initialize();
        using var stopEvent = new EventWaitHandle(false, EventResetMode.ManualReset, ShutdownEventName);
        using var settingsEvent = new EventWaitHandle(false, EventResetMode.AutoReset, SettingsEventName);
        using var context = new TrayContext(stopEvent, settingsEvent);
        Application.Run(context);
    }
}

internal sealed class TrayContext : ApplicationContext
{
    private readonly TrayIcons _icons = new();
    private readonly NotifyIcon _tray;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1500 };
    // Reopen verified ordinary ChatGPT threads when local delivery stalls.
    private readonly ChatThreadActivator _chatThreadActivator = new();
    private readonly bool _diagnosticMode;
    private readonly ControlOverlay _controlOverlay = new();
    internal bool ControlOverlayVisible => _controlOverlay.NativeVisible;
    internal bool HasControlMenuItem => _tray.ContextMenuStrip?.Items.Cast<ToolStripItem>()
        .Any(item => item.Text?.Contains("Индикатор управления", StringComparison.OrdinalIgnoreCase) == true) == true;
    internal string ControlOverlayExecutorCaption => _controlOverlay.ExecutorCaption;
    internal string ControlOverlayVersionCaption => _controlOverlay.VersionCaption;
    internal void ShowControlOverlay() => _controlOverlay.ShowStatus();

    private readonly EventWaitHandle _shutdown;
    private readonly EventWaitHandle _showSettings;
    private Process? _child;
    // A previous GUI may have exited while its official server was still
    // running. Adopt that installed server instead of launching a duplicate.
    private Process? _adoptedServerClient;
    private bool ServerProcessAlive =>
        _child is { HasExited: false } || _adoptedServerClient is { HasExited: false };
    private string _connection = "Не настроен";
    private DateTime _nextStart = DateTime.MinValue;
    private bool _stopping;
    private bool _updating;
    private Form? _settingsWindow;
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hwnd, int command);

    private static void EnsureSettingsWindowVisible(Form form)
    {
        // On some Windows 10 sessions, Form.Show() under a tray-only
        // ApplicationContext creates a native top-level HWND without the
        // WS_VISIBLE bit. The managed Visible flag can be misleading.
        if (!form.Visible)
            form.Show();
        if (form.WindowState == FormWindowState.Minimized)
            form.WindowState = FormWindowState.Normal;
        var handle = form.Handle;
        if (!IsWindowVisible(handle))
            ShowWindow(handle, 5); // SW_SHOW (verified on live DOMKAT HWND)
        form.BringToFront();
        form.Activate();
        if (!IsWindowVisible(handle))
            AgentStorage.Log("Settings window native visibility check failed.");
    }

    internal static string? ClientStatusFromLine(string line)
    {
        if (line.Contains(" connected.", StringComparison.OrdinalIgnoreCase))
            return "Подключён";
        if (line.Contains("401", StringComparison.OrdinalIgnoreCase) &&
            line.Contains("Router Lite agent:", StringComparison.OrdinalIgnoreCase))
            return "Ключ отклонён";
        if (line.Contains("409", StringComparison.OrdinalIgnoreCase) &&
            line.Contains("Router Lite agent:", StringComparison.OrdinalIgnoreCase))
            return "Имя компьютера занято";
        if (line.Contains("Router Lite agent:", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("error", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("failed", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("refused", StringComparison.OrdinalIgnoreCase))
            return "Нет соединения";
        return null;
    }

    private bool _checkedUpdates;
    private bool _serverProbePending;
    private DateTime _nextServerProbe = DateTime.MinValue;
    private int _serverGeneration;

    internal TrayContext(EventWaitHandle shutdown, EventWaitHandle settingsEvent, bool diagnosticMode = false)
    {
        _shutdown = shutdown;
        _showSettings = settingsEvent;
        _diagnosticMode = diagnosticMode;
        if (_diagnosticMode) _controlOverlay.DisableOwnTimerForIntegrationTest();
        var menu = new ContextMenuStrip();
        menu.Items.Add("Настройки", null, (_, _) => ShowSettings());
        var vpnMenu = new ToolStripMenuItem("Подключение ChatGPT");
        vpnMenu.DropDownOpening += (_, _) =>
        {
            foreach (ToolStripItem item in vpnMenu.DropDownItems)
                if (item is ToolStripMenuItem choice)
                    choice.Checked = choice.Tag is VpnConnectionMode mode && mode == VpnModeController.Current;
        };
        foreach (var mode in new[] { VpnConnectionMode.Happ, VpnConnectionMode.FreeReality })
        {
            var item = new ToolStripMenuItem(mode == VpnConnectionMode.Happ ?
                "Happ — как сейчас" : "Бесплатные серверы (Xray)") { Tag = mode };
            item.Click += (_, _) =>
            {
                if (_chatWindow is ChatGptMainForm live && !live.IsDisposed)
                    live.SelectVpnModeFromTray(mode);
                else
                {
                    // Open ChatGPT UI to display an explicit consent prompt.
                    ShowChat();
                    if (_chatWindow is ChatGptMainForm opened)
                        opened.SelectVpnModeFromTray(mode);
                }
            };
            vpnMenu.DropDownItems.Add(item);
        }
        menu.Items.Add(vpnMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Выход", null, (_, _) => { AgentWatchdog.PauseForManualExit(); ExitThread(); });
        _tray = new NotifyIcon
        {
            Icon = _icons.ForStatus(_connection),
            Text = "Подключение…",
            Visible = true,
            ContextMenuStrip = menu
        };
        _tray.DoubleClick += (_, _) => ShowChat();
        _timer.Tick += (_, _) =>
        {
            // The actual tray message loop drives overlay discovery.
            _controlOverlay.Poll();
            ShowVpnRecoveryNotice();
            if (!_recreatingBrowser && !VpnModeController.IsTransitioning &&
                _chatWindow is ChatGptMainForm browser &&
                !browser.IsDisposed && browser.BrowserNetworkMode != VpnModeController.Current)
                _ = RecreateBrowserAfterVpnChangeAsync(browser);
            if (_shutdown.WaitOne(0))
            {
                ExitThread();
                return;
            }
            if (_showSettings.WaitOne(0))
                ShowSettings();
            if (!_diagnosticMode && !_stopping && !_updating &&
                _adoptedServerClient is { HasExited: true })
            {
                AgentStorage.Log("Adopted bundled tunnel-client exited.");
                _adoptedServerClient.Dispose();
                _adoptedServerClient = null;
                _connection = "Переподключение…";
                _nextStart = DateTime.UtcNow.AddSeconds(5);
            }
            if (!_diagnosticMode && !_stopping && !_updating && _child is { HasExited: true })
            {
                // Record only the numeric exit code; never log CLI stderr verbatim
                // because it may contain sensitive diagnostic fields.
                AgentStorage.Log("Managed process exited; code=" + _child.ExitCode);
                _child.Dispose();
                _child = null;
                _connection = "Переподключение…";
                _nextStart = DateTime.UtcNow.AddSeconds(5);
            }
            if (!_diagnosticMode && !_stopping && !_updating &&
                _child is null && _adoptedServerClient is null &&
                DateTime.UtcNow >= _nextStart &&
                File.Exists(AgentStorage.SettingsPath))
                StartAgent();
            if (!_diagnosticMode && !_stopping && !_updating && ServerProcessAlive &&
                AgentStorage.SelectedRole == "server" &&
                !_serverProbePending && DateTime.UtcNow >= _nextServerProbe)
                _ = PollServerStatusAsync();
            // Chat messages are handled by the in-process WebView2 host.
            // Never start a second Chromium, Node.js or browser-based sender.
            var status = _connection.Contains("подключ", StringComparison.OrdinalIgnoreCase) &&
                !_connection.Contains("переподключ", StringComparison.OrdinalIgnoreCase)
                ? "Подключено" : _connection;
            _tray.Text = status.Length > 63 ? status[..63] : status;
            var displayIcon = _icons.ForStatus(_connection);
            if (!ReferenceEquals(_tray.Icon, displayIcon))
                _tray.Icon = displayIcon;
        };
        _timer.Start();
        if (!_diagnosticMode) Application.Idle += OnIdle;
    }

    private void OnIdle(object? sender, EventArgs e)
    {
        Application.Idle -= OnIdle;
        // One visible WebView2 renders the original ChatGPT UI.
        // The text-only relay host is no longer started.
        ShowChat();
        if (!File.Exists(AgentStorage.SettingsPath) || !AgentStorage.HasCredential() ||
            (AgentStorage.SelectedRole == "server" && !AgentStorage.HasServerApiKey()))
            ShowSettings();
        else
        {
            StartAgent();
            _ = CheckUpdatesAsync(false);
        }
    }

    private DateTimeOffset _lastVpnNoticeUtc = DateTimeOffset.UtcNow.AddMinutes(-3);
    private void ShowVpnRecoveryNotice()
    {
        if (_diagnosticMode) return;
        string path = Path.Combine(VpnModeController.Folder, "network-notice.json");
        if (!File.Exists(path)) return;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            string reason = root.GetProperty("reason").GetString() ?? "";
            var when = root.GetProperty("time").GetDateTimeOffset();
            if (when <= _lastVpnNoticeUtc ||
                when > DateTimeOffset.UtcNow.AddMinutes(2)) return;
            _lastVpnNoticeUtc = when;
            if (DateTimeOffset.UtcNow - when > TimeSpan.FromMinutes(3)) return;
            string message = reason switch
            {
                "free-node-rotated" =>
                    "Бесплатный VPN: подключён резервный рабочий сервер.",
                "free-route-health-failed" =>
                    "Бесплатные серверы недоступны. Подключение восстановлено через Happ.",
                _ => ""
            };
            if (message.Length == 0) return;
            _tray.ShowBalloonTip(7500, "VPN — изменение подключения",
                message, ToolTipIcon.Info);
            AgentStorage.Log("VPN connection notice: " + reason);
        }
        catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException or
                                    FormatException or InvalidOperationException)
        {
            AgentStorage.Log("Invalid VPN status notice: " + ex.GetType().Name);
        }
    }

    private bool _recreatingBrowser;
    private async Task RecreateBrowserAfterVpnChangeAsync(ChatGptMainForm chat)
    {
        if (_recreatingBrowser || chat.IsDisposed) return;
        _recreatingBrowser = true;
        try
        {
            var browserExit = chat.BrowserProcessExitTask;
            chat.ExitApplication();
            // Microsoft WebView2 requires the old browser process to release
            // its ONE user-data folder before recreating with different proxy flags.
            // Waiting here is asynchronous; the UI message pump remains responsive.
            await browserExit.WaitAsync(TimeSpan.FromSeconds(25));
            if (!_stopping && !_updating) ShowChat();
        }
        catch (TimeoutException)
        {
            AgentStorage.Log("VPN browser switch: WebView2 browser process did not exit in 25 seconds");
            MessageBox.Show("Невозможно переключить VPN: предыдущий процесс WebView2 не завершился. " +
                "Откройте окно ChatGPT после закрытия процесса браузера.",
                "ChatGPT MCP Tunnel", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            AgentStorage.Log("VPN browser switch: " + ex.GetType().Name);
            MessageBox.Show("Переключение браузера: " + ex.Message,
                "ChatGPT MCP Tunnel", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally { _recreatingBrowser = false; }
    }

    private Form? _chatWindow;
    private void ShowChat()
    {
        if (_chatWindow is { IsDisposed: false } existing)
        {
            EnsureSettingsWindowVisible(existing);
            return;
        }
        var chat = new ChatGptMainForm();
        // Recreate ONLY this WebView2 GUI on an app-scoped VPN mode change.
        // The existing official MCP transport stays alive.
        chat.VpnModeChanged += mode => { _ = RecreateBrowserAfterVpnChangeAsync(chat); };
        _chatWindow = chat;
        AppTheme.Apply(chat);
        chat.FormClosed += (_, _) =>
        {
            if (ReferenceEquals(_chatWindow, chat)) _chatWindow = null;
            chat.Dispose();
        };
        EnsureSettingsWindowVisible(chat);
    }

    private Form? _transferWindow;
    private void ShowTransfers()
    {
        if (_transferWindow is { IsDisposed: false } existing)
        {
            EnsureSettingsWindowVisible(existing);
            return;
        }
        var form = new TransferProgressWindow();
        _transferWindow = form;
        form.FormClosed += (_, _) => { if (ReferenceEquals(_transferWindow, form)) _transferWindow = null; form.Dispose(); };
        EnsureSettingsWindowVisible(form);
    }
    private void ShowSettings()
    {
        if (_updating || _stopping) return;
        // A modeless window avoids the nested message loop created by
        // Form.ShowDialog() from Application.Idle. That loop could leave
        // _openingSettings=true with a hidden form and a dead menu command.
        if (_settingsWindow is { IsDisposed: false } existing)
        {
            EnsureSettingsWindowVisible(existing);
            return;
        }
        Form form = new SettingsHubDialog(RestartAgent,
            () => CheckUpdatesAsync(true), ShowTransfers, _controlOverlay.ShowStatus);
        AppTheme.Apply(form);
        _settingsWindow = form;
        form.FormClosed += (_, _) =>
        {
            var saved = form.DialogResult == DialogResult.OK;
            if (ReferenceEquals(_settingsWindow, form))
                _settingsWindow = null;
            form.Dispose();
            if (saved && !_stopping && !_updating)
                RestartAgent();
        };
        EnsureSettingsWindowVisible(form);
        AgentStorage.Log("Settings window opened, native visible=" +
            IsWindowVisible(form.Handle));
    }

    private void StartAgent()
    {
        if (_stopping || _updating || _child is not null ||
            _adoptedServerClient is not null) return;
        try
        {
            var settings = AgentStorage.Load();
            ProcessStartInfo info;
            if (settings.Role == "server")
            {
                // Match the exact installed executable, not just its name.
                // A GUI restart must reuse its own existing tunnel-client,
                // while a foreign or uninspectable process blocks duplicates.
                var existing = ServerProcessOwnership.Inspect(
                    Path.Combine(AppContext.BaseDirectory, "OpenAI", "tunnel-client.exe"));
                if (existing.Kind == ExistingTunnelKind.Own)
                {
                    _adoptedServerClient = existing.Process;
                    _connection = "Проверка сервера…";
                    _nextServerProbe = DateTime.MinValue;
                    _nextStart = DateTime.UtcNow.AddSeconds(5);
                    AgentStorage.Log("Bundled tunnel-client adopted after GUI restart.");
                    _ = PollServerStatusAsync();
                    return;
                }
                if (existing.Kind == ExistingTunnelKind.Other)
                {
                    _connection = "Другой туннель уже запущен";
                    _nextStart = DateTime.UtcNow.AddMinutes(1);
                    AgentStorage.Log("Foreign or ambiguous tunnel-client detected; startup skipped.");
                    return;
                }
                var launcher = Path.Combine(AppContext.BaseDirectory, "ServerStart.ps1");
                if (!File.Exists(launcher))
                    throw new FileNotFoundException("Сценарий сервера не найден", launcher);
                var serverKey = AgentStorage.LoadServerAgentKey();
                var openAiKey = AgentStorage.LoadServerApiKey();
                if (serverKey.Length < 16 ||
                    string.IsNullOrWhiteSpace(openAiKey) ||
                    string.IsNullOrWhiteSpace(settings.TunnelId))
                {
                    _connection = "Настройте сервер";
                    _nextStart = DateTime.UtcNow.AddMinutes(1);
                    return;
                }
                info = new ProcessStartInfo("powershell.exe")
                {
                    WorkingDirectory = AppContext.BaseDirectory,
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardError = true
                };
                info.ArgumentList.Add("-NoProfile");
                info.ArgumentList.Add("-ExecutionPolicy");
                info.ArgumentList.Add("Bypass");
                info.ArgumentList.Add("-File");
                info.ArgumentList.Add(launcher);
                info.Environment["CONTROL_PLANE_API_KEY"] = openAiKey;
                info.Environment["CHATGPT_MCP_TUNNEL_ID"] = settings.TunnelId;
                info.Environment["CHATGPT_MCP_AGENT_TOKEN"] = serverKey;
                info.Environment["CHATGPT_MCP_ROUTER_ENABLED"] = "1";
                info.Environment["CHATGPT_MCP_ROUTER_LISTEN"] =
                    "http://" + settings.ListenAddress + ":55201";
                if (settings.DirectWanEnabled)
                {
                    var fingerprint = DirectTls.Fingerprint();
                    if (fingerprint.Length != 64)
                        throw new InvalidOperationException("Сертификат защищённого подключения не найден.");
                    info.Environment["CHATGPT_MCP_ROUTER_TLS_PFX"] = DirectTls.PfxPath;
                    info.Environment["CHATGPT_MCP_ROUTER_TLS_PASSWORD"] = AgentStorage.LoadTlsPassword();
                }
            }
            else
            {
                if (!AgentStorage.ValidateRouter(settings.RouterUrl, out var reason))
                {
                    _connection = "Проверьте адрес";
                    AgentStorage.Log("Configuration rejected: " + reason);
                    _nextStart = DateTime.UtcNow.AddSeconds(30);
                    return;
                }
                var token = AgentStorage.LoadCredential();
                var runtime = Path.Combine(AppContext.BaseDirectory, "Runtime", "ChatGptMcpTunnel.exe");
                if (!File.Exists(runtime)) throw new FileNotFoundException("Модуль агента не найден", runtime);
                info = new ProcessStartInfo(runtime)
                {
                    WorkingDirectory = Path.GetDirectoryName(runtime)!,
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardError = true
                };
                info.ArgumentList.Add("--agent");
                info.Environment["CHATGPT_MCP_COMPUTER_ID"] = settings.Computer;
                info.Environment["CHATGPT_MCP_AGENT_TOKEN"] = token;
                info.Environment["CHATGPT_MCP_ROUTER_URL"] = settings.RouterUrl;
                if (settings.RouterUrl.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
                    info.Environment["CHATGPT_MCP_ROUTER_TLS_SHA256"] =
                        DirectTls.NormalizeFingerprint(settings.TlsFingerprint);
            }
            _child = new Process { StartInfo = info, EnableRaisingEvents = true };
            _child.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is null) return;
                // Only client agents expose their connection state through
                // textual status messages. The server uses HTTP readiness and
                // a local Router Lite TCP probe instead.
                if (settings.Role == "client")
                {
                    var status = ClientStatusFromLine(e.Data);
                    if (status is not null) _connection = status;
                    AgentStorage.Log(e.Data);
                }
                else if (e.Data.Contains("Router Lite", StringComparison.OrdinalIgnoreCase))
                    AgentStorage.Log("Server event: Router Lite");
                // No authorization data or other native stderr is logged.
            };
            _child.Start();
            _child.BeginErrorReadLine();
            _connection = settings.Role == "server" ? "Запуск сервера…" : "Подключение…";
            _nextServerProbe = DateTime.MinValue;
            _nextStart = DateTime.UtcNow.AddSeconds(5);
            AgentStorage.Log("Agent process started.");
        }
        catch (Exception error)
        {
            _connection = "Ошибка запуска";
            AgentStorage.Log("Startup error: " + error.GetType().Name + " " + error.Message);
            _child?.Dispose();
            _child = null;
            _nextStart = DateTime.UtcNow.AddSeconds(15);
        }
    }

    private async Task PollServerStatusAsync()
    {
        _serverProbePending = true;
        _nextServerProbe = DateTime.UtcNow.AddSeconds(5);
        var generation = _serverGeneration;
        try
        {
            // This is the Server role: do not interpret MCP informational
            // stderr as "Нет соединения", and do not wait for an agent message.
            var address = AgentStorage.Load().ListenAddress;
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var result = await ServerStatusProbe.CheckAsync(address, timeout.Token);
            if (generation == _serverGeneration && !_stopping && !_updating &&
                ServerProcessAlive)
                _connection = ServerStatusProbe.Describe(result);
        }
        catch (Exception error)
        {
            if (generation == _serverGeneration && !_stopping)
            {
                _connection = "Проверка сервера недоступна";
                AgentStorage.Log("Server health check error: " + error.GetType().Name);
            }
        }
        finally
        {
            _serverProbePending = false;
        }
    }

    private void StopAgent()
    {
        // Prevent a previous readiness probe from changing a new server's
        // status after reconnect or update.
        _serverGeneration++;
        _nextServerProbe = DateTime.MinValue;
        var child = _child;
        var adopted = _adoptedServerClient;
        _child = null;
        _adoptedServerClient = null;
        // Explicit Exit/Reconnect stops our own verified tunnel process.
        // Closing the GUI after a crash does not change an unrelated process.
        foreach (var process in new[] { child, adopted })
        {
            if (process is null) continue;
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(3000);
                }
            }
            catch (InvalidOperationException) { }
            finally { process.Dispose(); }
        }
    }

    private void RestartAgent()
    {
        if (_updating || _stopping) return;
        // Network reconnect does not close the primary ChatGPT UI or disrupt WebView2.
        StopAgent();
        _connection = "Переподключение…";
        _nextStart = DateTime.MinValue;
        StartAgent();
    }

    private async Task CheckUpdatesAsync(bool showResult)
    {
        if (_updating || _stopping) return;
        if (!showResult && _checkedUpdates) return;
        _checkedUpdates = true;
        try
        {
            var (version, uri) = await AgentUpdates.GetLatestAsync();
            if (_stopping || _updating) return;
            var installed = typeof(Program).Assembly.GetName().Version ?? new Version(0, 0, 0);
            if (version is null || uri is null || version <= installed)
            {
                if (showResult)
                    MessageBox.Show("Установлена актуальная версия.", "ChatGPT MCP Agent",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show($"Доступно обновление {version}.\nУстановить сейчас?",
                    "ChatGPT MCP Agent", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
                await StartUpdaterAsync(uri);
        }
        catch (Exception error)
        {
            AgentStorage.Log("Update check failed: " + error.GetType().Name);
            if (showResult)
                MessageBox.Show("Не удалось проверить обновления: " + error.Message,
                    "ChatGPT MCP Agent", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task StartUpdaterAsync(Uri downloadUrl)
    {
        if (!AgentUpdates.IsAllowedInstaller(downloadUrl)) return;
        _updating = true;
        try
        {
            Directory.CreateDirectory(AgentStorage.UpdatesFolder);
            var updater = Path.Combine(AgentStorage.UpdatesFolder, "AgentUpdater.exe");
            File.Copy(Application.ExecutablePath, updater, overwrite: true);
            var pipeName = "ChatGptMcpAgentUpdate-" + Guid.NewGuid().ToString("N");
            using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.In, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            var start = new ProcessStartInfo(updater)
            {
                UseShellExecute = false,
                WorkingDirectory = AgentStorage.UpdatesFolder
            };
            start.ArgumentList.Add("--apply-update");
            start.ArgumentList.Add(Environment.ProcessId.ToString());
            start.ArgumentList.Add(downloadUrl.AbsoluteUri);
            start.ArgumentList.Add(pipeName);
            using var child = Process.Start(start)
                ?? throw new InvalidOperationException("Не удалось запустить обновление.");
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(12));
            await pipe.WaitForConnectionAsync(timeout.Token);
            using var reader = new StreamReader(pipe);
            if (await reader.ReadLineAsync(timeout.Token) != "UPDATE_READY")
                throw new InvalidDataException("Помощник обновления не подтвердил загрузку.");
            // The updater now has the installer and is waiting for this process to exit.
            ExitThread();
        }
        catch (Exception e)
        {
            _updating = false;
            AgentStorage.Log("Updater launch failed: " + e.GetType().Name);
            MessageBox.Show("Обновление не запущено: " + e.Message,
                "ChatGPT MCP Agent", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    protected override void ExitThreadCore()
    {
        if (_stopping) return;
        _stopping = true;
        _timer.Stop();
        if (_chatWindow is ChatGptMainForm chat) chat.ExitApplication();
        if (_settingsWindow is { IsDisposed: false } window)
            window.Close();
        if (_transferWindow is { IsDisposed: false } transfers) transfers.Close();
        StopAgent();
        _controlOverlay.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        _icons.Dispose();
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }
}
