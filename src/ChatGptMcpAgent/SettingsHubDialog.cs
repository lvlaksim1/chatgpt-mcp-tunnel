using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using System.Drawing;

namespace ChatGptMcpAgent;

// User-facing configuration hub. The existing server/client network forms are
// deliberately retained so no tunnel IDs, tokens or firewall options are lost.
internal sealed class SettingsHubDialog : Form
{
    private readonly Action _reconnect;
    private readonly Func<Task> _updates;
    private readonly Action _transfers;
    private readonly Action _overlay;
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill,
        DrawMode = TabDrawMode.OwnerDrawFixed, ItemSize = new Size(115, 38), SizeMode = TabSizeMode.Fixed };
    private readonly CheckBox _notifications = new() {
        Text = "Отображать уведомления о действиях модели на компьютере",
        AutoSize = true, Margin = new Padding(4, 12, 4, 8) };
    private readonly RadioButton _dark = new() { Text = "Тёмная", AutoSize = true };
    private readonly RadioButton _light = new() { Text = "Светлая", AutoSize = true };

    internal SettingsHubDialog(Action reconnect, Func<Task> updates, Action transfers, Action overlay)
    {
        _reconnect = reconnect;
        _updates = updates;
        _transfers = transfers;
        _overlay = overlay;
        Text = "ChatGPT MCP Tunnel — Настройки";
        Size = new Size(790, 580);
        MinimumSize = new Size(640, 465);
        StartPosition = FormStartPosition.CenterScreen;
        Icon = TrayIcons.AppIcon();
        Font = new Font("Segoe UI", 10);
        Controls.Add(_tabs);
        _tabs.DrawItem += (_, e) =>
        {
            var selected = (_tabs.SelectedIndex == e.Index);
            using var b = new SolidBrush(selected ? AppTheme.Selected : AppTheme.Background);
            e.Graphics.FillRectangle(b, e.Bounds);
            TextRenderer.DrawText(e.Graphics, _tabs.TabPages[e.Index].Text, Font, e.Bounds,
                AppTheme.Text, TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|
                TextFormatFlags.EndEllipsis);
        };

        var network = Page("Подключение");
        network.Controls.Add(Note("Параметры сервера и подключённых компьютеров."));
        network.Controls.Add(ActionButton("Настройки подключения", Connection));
        network.Controls.Add(ActionButton("Переподключиться", () =>
        {
            _reconnect();
            MessageBox.Show(this, "Переподключение запущено.", "Подключение");
        }));

        var system = Page("Система");
        system.Controls.Add(Note("Операции с повышенными правами требуют подтверждения UAC."));
        system.Controls.Add(ActionButton("Разрешить административные права", async () => await TestUacAsync()));
        system.Controls.Add(Note("Разрешение UAC относится к одной операции. Постоянно запускать весь Tunnel от администратора не требуется."));
        system.Controls.Add(ActionButton("Открыть индикатор управления", _overlay));

        var notifications = Page("Уведомления");
        notifications.Controls.Add(_notifications);
        notifications.Controls.Add(Note("При отключении не появляются всплывающие окна о действиях модели. Журнал и экстренная блокировка остаются доступны."));
        _notifications.Checked = UiPreferences.Load().ShowControlNotifications;
        _notifications.CheckedChanged += (_, _) =>
        {
            var prefs = UiPreferences.Load();
            prefs.ShowControlNotifications = _notifications.Checked;
            prefs.Save();
        };

        var appearance = Page("Оформление");
        appearance.Controls.Add(Note("Выберите единое оформление чата, настроек, диалогов и уведомлений."));
        appearance.Controls.Add(_dark);
        appearance.Controls.Add(_light);
        _dark.Checked = UiPreferences.Load().Theme == "dark";
        _light.Checked = !_dark.Checked;
        _dark.CheckedChanged += (_, _) => { if (_dark.Checked) SaveTheme("dark"); };
        _light.CheckedChanged += (_, _) => { if (_light.Checked) SaveTheme("light"); };

        var files = Page("Файлы");
        files.Controls.Add(Note("Передача файлов между подключёнными компьютерами."));
        files.Controls.Add(ActionButton("Открыть передачу файлов", _transfers));

        var updatesPage = Page("Обновления");
        updatesPage.Controls.Add(Note("Проверка доступной официальной версии и обновление приложения."));
        updatesPage.Controls.Add(ActionButton("Проверить обновления", async () => await _updates()));

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(6) };
        var close = new Button { Text = "Закрыть", Dock = DockStyle.Right, Width = 140 };
        close.Click += (_, _) => Close();
        footer.Controls.Add(close);
        Controls.Add(footer);
        footer.BringToFront();
        Shown += (_, _) => AppTheme.Apply(this);
        AppTheme.Apply(this);
    }

    private FlowLayoutPanel Page(string name)
    {
        var page = new TabPage(name);
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, AutoScroll = true, Padding = new Padding(20, 20, 10, 10) };
        page.Controls.Add(flow);
        _tabs.TabPages.Add(page);
        return flow;
    }

    private static Label Note(string text) => new() {
        Text = text, AutoSize = false, Width = 660, Height = 64,
        Margin = new Padding(4, 2, 0, 8) };
    private static Button ActionButton(string text, Action click)
    {
        var b = new Button { Text = text, Width = 395, Height = 42, Margin = new Padding(4, 4, 4, 12) };
        b.Click += (_, _) => click();
        return b;
    }
    private static Button ActionButton(string text, Func<Task> click)
    {
        var b = new Button { Text = text, Width = 395, Height = 42, Margin = new Padding(4, 4, 4, 12) };
        b.Click += async (_, _) =>
        {
            b.Enabled = false;
            try { await click(); }
            finally { if (!b.IsDisposed) b.Enabled = true; }
        };
        return b;
    }
    private void SaveTheme(string theme)
    {
        var prefs = UiPreferences.Load();
        prefs.Theme = theme;
        prefs.Save();
        _tabs.Invalidate(true);
    }

    private void Connection()
    {
        using Form dialog = AgentStorage.SelectedRole == "server"
            ? new ServerSettingsDialog(AgentStorage.Load())
            : new SettingsDialog(AgentStorage.Load());
        AppTheme.Apply(dialog);
        if (dialog.ShowDialog(this) == DialogResult.OK) _reconnect();
    }

    private async Task TestUacAsync()
    {
        try
        {
            var info = new ProcessStartInfo(Application.ExecutablePath)
            {
                UseShellExecute = true,
                Verb = "runas",
                Arguments = "--uac-elevated-check"
            };
            using var child = Process.Start(info) ??
                throw new InvalidOperationException("Запуск с UAC не состоялся");
            await child.WaitForExitAsync();
            MessageBox.Show(this,
                child.ExitCode == 0
                    ? "Подтверждение UAC прошло успешно. Для последующих административных операций Windows запросит разрешение отдельно."
                    : "Не удалось подтвердить административные права.",
                "Административные права", MessageBoxButtons.OK,
                child.ExitCode == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        catch (Win32Exception e) when (e.NativeErrorCode == 1223)
        {
            MessageBox.Show(this, "Подтверждение UAC отменено.");
        }
        catch (Exception e)
        {
            MessageBox.Show(this, "Ошибка UAC: " + e.GetType().Name,
                "Административные права", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
