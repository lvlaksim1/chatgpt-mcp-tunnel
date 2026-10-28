using System.Diagnostics;
using System.Drawing;
using System.IO.Pipes;

namespace ChatGptMcpAgent;

internal static class Program
{
    private const string ShutdownEventName = @"Local\ChatGptMcpAgentShutdown";
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Contains("--selftest", StringComparer.OrdinalIgnoreCase))
        {
            Environment.ExitCode = AgentStorage.RunSelfTest();
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
        ApplicationConfiguration.Initialize();
        using var stopEvent = new EventWaitHandle(false, EventResetMode.ManualReset, ShutdownEventName);
        using var context = new TrayContext(stopEvent);
        Application.Run(context);
    }
}

internal sealed class TrayContext : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1500 };
    private readonly EventWaitHandle _shutdown;
    private Process? _child;
    private string _connection = "Не настроен";
    private DateTime _nextStart = DateTime.MinValue;
    private bool _stopping;
    private bool _updating;
    private bool _openingSettings;
    private bool _checkedUpdates;

    internal TrayContext(EventWaitHandle shutdown)
    {
        _shutdown = shutdown;
        var menu = new ContextMenuStrip();
        menu.Items.Add("Настройки подключения", null, (_, _) => ShowSettings());
        menu.Items.Add("Переподключиться", null, (_, _) => RestartAgent());
        menu.Items.Add("Проверить обновления", null, async (_, _) => await CheckUpdatesAsync(true));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Выход", null, (_, _) => ExitThread());
        _tray = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "ChatGPT MCP Agent",
            Visible = true,
            ContextMenuStrip = menu
        };
        _tray.DoubleClick += (_, _) => ShowSettings();
        _timer.Tick += (_, _) =>
        {
            if (_shutdown.WaitOne(0))
            {
                // A settings dialog can be in a nested modal message loop.
                // Close it before releasing the tray application's mutex.
                foreach (Form open in Application.OpenForms.Cast<Form>().ToArray())
                    open.Close();
                ExitThread();
                return;
            }
            if (!_stopping && !_updating && _child is { HasExited: true })
            {
                _child.Dispose();
                _child = null;
                _connection = "Переподключение…";
                _nextStart = DateTime.UtcNow.AddSeconds(5);
            }
            if (!_stopping && !_updating && _child is null && DateTime.UtcNow >= _nextStart &&
                File.Exists(AgentStorage.SettingsPath))
                StartAgent();
            var label = "ChatGPT MCP Agent — " + _connection;
            _tray.Text = label.Length > 63 ? label[..63] : label;
        };
        _timer.Start();
        Application.Idle += OnIdle;
    }

    private void OnIdle(object? sender, EventArgs e)
    {
        Application.Idle -= OnIdle;
        if (!File.Exists(AgentStorage.SettingsPath) || !AgentStorage.HasCredential())
            ShowSettings();
        else
        {
            StartAgent();
            _ = CheckUpdatesAsync(false);
        }
    }

    private void ShowSettings()
    {
        if (_openingSettings || _updating || _stopping) return;
        _openingSettings = true;
        try
        {
            using var form = new SettingsDialog(AgentStorage.Load());
            if (form.ShowDialog() == DialogResult.OK)
                RestartAgent();
        }
        finally { _openingSettings = false; }
    }

    private void StartAgent()
    {
        if (_stopping || _updating || _child is not null) return;
        try
        {
            var settings = AgentStorage.Load();
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
            var info = new ProcessStartInfo(runtime)
            {
                WorkingDirectory = Path.GetDirectoryName(runtime)!,
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardError = true
            };
            info.ArgumentList.Add("--agent");
            info.Environment["CHATGPT_MCP_COMPUTER_ID"] = settings.Computer;
            info.Environment["CHATGPT_MCP_AGENT_TOKEN"] = token;
            info.Environment["CHATGPT_MCP_ROUTER_URL"] = settings.RouterUrl;
            _child = new Process { StartInfo = info, EnableRaisingEvents = true };
            _child.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is null) return;
                if (e.Data.Contains(" connected.", StringComparison.OrdinalIgnoreCase))
                    _connection = "Подключён";
                else if (e.Data.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                         e.Data.Contains("failed", StringComparison.OrdinalIgnoreCase) ||
                         e.Data.Contains("refused", StringComparison.OrdinalIgnoreCase))
                    _connection = "Нет соединения";
                // No environment variable, authorization headers or secrets are logged.
                AgentStorage.Log(e.Data);
            };
            _child.Start();
            _child.BeginErrorReadLine();
            _connection = "Подключение…";
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

    private void StopAgent()
    {
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
        StopAgent();
        _tray.Visible = false;
        _tray.Dispose();
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }
}
