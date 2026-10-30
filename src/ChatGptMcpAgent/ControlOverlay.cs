using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace ChatGptMcpAgent;

// Local WinForms overlay. The emergency stop gate is in ControlState,
// shared with the worker; no ChatGPT/host/browser/network interaction.
internal sealed class ControlOverlay : Form
{
    private readonly Label _header = new();
    private readonly Label _executor = new();
    private readonly Label _version = new();
    private readonly Label _current = new();
    private readonly Label _history = new();
    private readonly Button _stop = new();
    private readonly System.Windows.Forms.Timer _refresh = new() { Interval = 350 };
    // The tray owns this window for its entire lifetime: X hides, not disposes.
    private bool _hiddenByUser;
    private HashSet<string> _activeWhenHidden = new(StringComparer.Ordinal);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr hWnd, int command);

    internal bool NativeVisible => IsHandleCreated && IsWindowVisible(Handle);
    internal string ExecutorCaption => _executor.Text;
    internal string VersionCaption => _version.Text;
    internal static string InstalledVersion => Application.ProductVersion.Split('+')[0];
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            _activeWhenHidden = ControlState.Read().Active.Select(x => x.Id)
                .ToHashSet(StringComparer.Ordinal);
            _hiddenByUser = true;
            Hide();
            return;
        }
        base.OnFormClosing(e);
    }
    protected override bool ShowWithoutActivation => true;

    internal ControlOverlay()
    {
        Text = "ChatGPT MCP Tunnel v" + InstalledVersion + " - Управление";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        ShowInTaskbar = false;
        MaximizeBox = false;
        MinimizeBox = false;
        TopMost = true;
        ClientSize = new Size(388, 300);
        BackColor = Color.FromArgb(33, 39, 49);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9.5f);
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.Font;
        Opacity = 0.98;
        Icon = TrayIcons.AppIcon();

        _header.SetBounds(16, 12, 356, 28);
        _header.Font = new Font(Font, FontStyle.Bold);
        _executor.SetBounds(16, 42, 356, 25);
        _executor.ForeColor = Color.FromArgb(246, 211, 143);
        _executor.Font = new Font(Font, FontStyle.Bold);
        _current.SetBounds(16, 94, 356, 48);
        _history.SetBounds(16, 147, 356, 82);
        _history.ForeColor = Color.FromArgb(194, 203, 215);
        _history.Font = new Font("Segoe UI", 8.3f);
        _stop.SetBounds(16, 240, 356, 38);
        _stop.Font = new Font(Font, FontStyle.Bold);
        _stop.FlatStyle = FlatStyle.Flat;
        _stop.FlatAppearance.BorderSize = 0;
        _version.SetBounds(16, 70, 356, 20);
        _version.Font = new Font("Segoe UI", 8.5f);
        _version.ForeColor = Color.FromArgb(194, 203, 215);
        _version.TextAlign = ContentAlignment.MiddleLeft;
        _version.Text = "Версия: v" + InstalledVersion;
        _stop.Click += (_, _) => ToggleStop();
        Controls.AddRange(new Control[] { _header, _executor, _current, _history, _stop, _version });
        _refresh.Tick += (_, _) => RefreshFromControl();
        _refresh.Start();
    }

    private static string Friendly(string tool, string? activity = null) =>
        ControlActivityLabels.Display(tool, activity) ?? (tool switch
    {
        "shell.exec" => "Выполнение команды",
        "fs.read" => "Чтение файла",
        "fs.write" => "Запись файла",
        "fs.list" => "Просмотр папки",
        "fs.search" => "Поиск файлов",
        "process.list" => "Просмотр процессов",
        "process.kill" => "Завершение процесса",
        "desktop.screen.capture" => "Снимок экрана",
        "desktop.window.list" => "Поиск окна",
        "desktop.window.activate" => "Активация окна",
        "desktop.mouse.click" => "Нажатие мышью",
        "desktop.mouse.move" => "Перемещение курсора",
        "desktop.mouse.scroll" => "Прокрутка",
        "desktop.keyboard.type" => "Ввод текста",
        "desktop.keyboard.hotkey" => "Нажатие клавиш",
        "desktop.uia.tree" => "Чтение интерфейса",
        "desktop.uia.find" => "Поиск элемента",
        "desktop.uia.action" => "Действие с элементом",
        "desktop.batch" => "Серия действий",
        _ => tool
    });
    private static bool Recent(ControlSnapshot snap)
    {
        if (snap.Recent.Count == 0) return false;
        return DateTimeOffset.TryParse(snap.Recent[0].FinishedUtc, out var last)
            && DateTimeOffset.UtcNow - last < TimeSpan.FromSeconds(12);
    }
    // Called from the tray's already-running message loop as well as our own timer.
    // Do not rely on a not-yet-shown Form's timer to bootstrap visibility.
    internal void Poll() => RefreshFromControl();
    internal void DisableOwnTimerForIntegrationTest() => _refresh.Stop();


    private void RefreshFromControl()
    {
        try
        {
            var s = ControlState.Read();
            var active = s.Active.Count > 0;
            // Do not reopen instantly after X; a new operation may show the overlay.
            if (_hiddenByUser)
            {
                if (!s.Active.Any(x => !_activeWhenHidden.Contains(x.Id))) return;
                _hiddenByUser = false;
                _activeWhenHidden.Clear();
            }
            var visible = active || s.Stopped || Recent(s);
            if (!visible) { if (Visible) Hide(); return; }
            var status = s.Stopped ? "ОСТАНОВЛЕНО ПОЛЬЗОВАТЕЛЕМ" :
                active ? "Управление активно" : "Управление завершено";
            _header.Text = "ChatGPT MCP Tunnel · " + status;
            _header.ForeColor = s.Stopped ? Color.OrangeRed :
                active ? Color.LightGreen : Color.LightSkyBlue;
            var current = s.Active.LastOrDefault();
            // The label belongs to the current operation (or the most recent
            // operation); it is never stored globally across independent chats.
            var lastExecutor = current?.Executor ??
                s.Recent.FirstOrDefault()?.Executor;
            _executor.Text = "Исполнитель: " + ControlExecutor.Display(lastExecutor);

            _current.Text = current is not null ? "Сейчас: " + Friendly(current.Tool, current.Activity) :
                s.Stopped ? "Новые команды заблокированы на этом ПК." :
                "Все операции завершены.";
            _history.Text = "Последние операции:" + Environment.NewLine +
                string.Join(Environment.NewLine,
                    s.Recent.Take(3).Select(x => "✓ " + Friendly(x.Tool, x.Activity)));
            _stop.Text = s.Stopped ? "ВОЗОБНОВИТЬ УПРАВЛЕНИЕ" : "СТОП — ПРЕКРАТИТЬ УПРАВЛЕНИЕ";
            _stop.BackColor = s.Stopped ? Color.FromArgb(48, 119, 89) :
                Color.FromArgb(174, 48, 54);
            if (!NativeVisible)
            {
                var area = (Screen.PrimaryScreen ?? Screen.FromPoint(Cursor.Position)).WorkingArea;
                Location = new Point(area.Right - Width - 12, area.Bottom - Height - 12);
                if (!Visible) Show();
                // Win10 tray-only ApplicationContext can create a hidden HWND
                // while Form.Visible incorrectly reports true. Confirm native visibility.
                if (!IsWindowVisible(Handle))
                    ShowWindow(Handle, 4); // SW_SHOWNOACTIVATE
            }

        }
        catch (Exception e)
        {
            _header.Text = "Ошибка контроля команд";
            _current.Text = e.GetType().Name;
            _stop.Text = "СТОП";
            if (!Visible) Show();
        }
    }

    private void ToggleStop()
    {
        try
        {
            if (ControlState.IsStopped)
            {
                if (MessageBox.Show(this, "Снова разрешить удалённое управление этим компьютером?",
                        "ChatGPT MCP Tunnel", MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning) == DialogResult.Yes)
                    ControlState.Resume();
            }
            else ControlState.Stop();
            RefreshFromControl();
        }
        catch (Exception e)
        {
            MessageBox.Show(this, "Не удалось изменить состояние: " + e.GetType().Name,
                "Ошибка контроля", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    internal void ShowStatus()
    {
        _hiddenByUser = false;
        _activeWhenHidden.Clear();
        RefreshFromControl();
        if (!Visible) Show();
        Activate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _refresh.Dispose();
        base.Dispose(disposing);
    }
}
