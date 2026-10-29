using System.Diagnostics;
using System.Drawing;
using System.IO.Pipes;
using System.Runtime.InteropServices;

namespace ChatGptMcpAgent;

internal static class Program
{
    private const string ShutdownEventName = @"Local\ChatGptMcpAgentShutdown";
    private const string SettingsEventName = @"Local\ChatGptMcpAgentShowSettings";
    [STAThread]
    private static void Main(string[] args)
    {
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
    private readonly EventWaitHandle _shutdown;
    private readonly EventWaitHandle _showSettings;
    private Process? _child;
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

    internal TrayContext(EventWaitHandle shutdown, EventWaitHandle settingsEvent)
    {
        _shutdown = shutdown;
        _showSettings = settingsEvent;
        var menu = new ContextMenuStrip();
        menu.Items.Add("Настройки подключения", null, (_, _) => ShowSettings());
        menu.Items.Add("Переподключиться", null, (_, _) => RestartAgent());
        menu.Items.Add("Проверить обновления", null, async (_, _) => await CheckUpdatesAsync(true));
        menu.Items.Add("Передача файлов", null, (_, _) => ShowTransfers());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Выход", null, (_, _) => { AgentWatchdog.PauseForManualExit(); ExitThread(); });
        _tray = new NotifyIcon
        {
            Icon = _icons.ForStatus(_connection),
            Text = "ChatGPT MCP Tunnel",
            Visible = true,
            ContextMenuStrip = menu
        };
        _tray.DoubleClick += (_, _) => ShowSettings();
        _timer.Tick += (_, _) =>
        {
            if (_shutdown.WaitOne(0))
            {
                ExitThread();
                return;
            }
            if (_showSettings.WaitOne(0))
                ShowSettings();
            if (!_stopping && !_updating && _child is { HasExited: true })
            {
                // Record only the numeric exit code; never log CLI stderr verbatim
                // because it may contain sensitive diagnostic fields.
                AgentStorage.Log("Managed process exited; code=" + _child.ExitCode);
                _child.Dispose();
                _child = null;
                _connection = "Переподключение…";
                _nextStart = DateTime.UtcNow.AddSeconds(5);
            }
            if (!_stopping && !_updating && _child is null && DateTime.UtcNow >= _nextStart &&
                File.Exists(AgentStorage.SettingsPath))
                StartAgent();
            if (!_stopping && !_updating && _child is { HasExited: false } &&
                AgentStorage.SelectedRole == "server" &&
                !_serverProbePending && DateTime.UtcNow >= _nextServerProbe)
                _ = PollServerStatusAsync();
            var label = "ChatGPT MCP Tunnel — " + _connection;
            _tray.Text = label.Length > 63 ? label[..63] : label;
            var displayIcon = _icons.ForStatus(_connection);
            if (!ReferenceEquals(_tray.Icon, displayIcon))
                _tray.Icon = displayIcon;
        };
        _timer.Start();
        Application.Idle += OnIdle;
    }

    private void OnIdle(object? sender, EventArgs e)
    {
        Application.Idle -= OnIdle;
        if (!File.Exists(AgentStorage.SettingsPath) || !AgentStorage.HasCredential() ||
            (AgentStorage.SelectedRole == "server" && !AgentStorage.HasServerApiKey()))
            ShowSettings();
        else
        {
            StartAgent();
            _ = CheckUpdatesAsync(false);
        }
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
        Form form = AgentStorage.SelectedRole == "server"
            ? new ServerSettingsDialog(AgentStorage.Load())
            : new SettingsDialog(AgentStorage.Load());
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
        if (_stopping || _updating || _child is not null) return;
        try
        {
            var settings = AgentStorage.Load();
            ProcessStartInfo info;
            if (settings.Role == "server")
            {
                // Existing OpenAI tunnel must never be duplicated during migration.
                if (Process.GetProcessesByName("tunnel-client").Any())
                {
                    _connection = "Другой туннель уже запущен";
                    _nextStart = DateTime.UtcNow.AddMinutes(1);
                    AgentStorage.Log("Existing tunnel-client detected; server startup skipped.");
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
                _child is { HasExited: false })
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
        _child = null;
        if (child is null) return;
        try
        {
            if (!child.HasExited)
            {
                child.Kill(entireProcessTree: true);
                child.WaitForExit(3000);
            }
        }
        catch (InvalidOperationException) { }
        finally { child.Dispose(); }
    }

    private void RestartAgent()
    {
        if (_updating || _stopping) return;
        if (_transferWindow is { IsDisposed: false } transfers) transfers.Close();
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
        if (_settingsWindow is { IsDisposed: false } window)
            window.Close();
        if (_transferWindow is { IsDisposed: false } transfers) transfers.Close();
        StopAgent();
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
