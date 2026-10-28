using System.Drawing;

namespace ChatGptMcpAgent;

internal sealed class SettingsDialog : Form
{
    private readonly TextBox _computer = new();
    private readonly TextBox _url = new();
    private readonly TextBox _secret = new() { UseSystemPasswordChar = true };
    private readonly CheckBox _autostart = new() { Text = "Автоматически запускать при входе в Windows", AutoSize = true };
    internal bool Saved { get; private set; }

    internal SettingsDialog(AgentSettings existing)
    {
        Text = "ChatGPT MCP Tunnel — Клиент";
        ClientSize = new Size(490, 325);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        Icon = SystemIcons.Application;
        var intro = new Label
        {
            Text = "Клиент подключается к основному компьютеру с маршрутизатором",
            Left = 20, Top = 14, Width = 460, Height = 34,
            ForeColor = SystemColors.GrayText
        };
        Controls.Add(intro);

        AddField("Имя компьютера", _computer, 53, existing.Computer);
        AddField("Адрес маршрутизатора", _url, 114, existing.RouterUrl);
        AddField("Ключ подключения", _secret, 175, "");
        _secret.PlaceholderText = AgentStorage.HasCredential()
            ? "Сохранён (оставьте пустым, чтобы не изменять)"
            : "Введите ключ, выданный маршрутизатором";
        _autostart.Left = 21;
        _autostart.Top = 246;
        _autostart.Checked = existing.AutoStart;
        Controls.Add(_autostart);
        var save = new Button
        {
            Text = "Сохранить и подключиться", Left = 200, Top = 281, Width = 190, Height = 33
        };
        var close = new Button { Text = "Отмена", Left = 400, Top = 281, Width = 78, Height = 33 };
        close.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        save.Click += (_, _) => Save();
        AcceptButton = save;
        CancelButton = close;
        Controls.Add(save);
        Controls.Add(close);
    }

    private void AddField(string label, TextBox entry, int top, string text)
    {
        Controls.Add(new Label { Text = label, Left = 21, Top = top, Width = 453, Height = 22 });
        entry.SetBounds(21, top + 24, 457, 28);
        entry.Text = text;
        Controls.Add(entry);
    }

    private void Save()
    {
        try
        {
            var name = _computer.Text.Trim();
            if (name.Length is < 1 or > 80 ||
                name.Any(ch => !(char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_' or '.')))
                throw new ArgumentException("Имя компьютера: 1–80 латинских букв, цифр, точек, _ или -.");
            var url = _url.Text.Trim();
            if (!AgentStorage.ValidateRouter(url, out var message))
                throw new ArgumentException(message);
            var secret = _secret.Text;
            if (secret.Length == 0 && !AgentStorage.HasCredential())
                throw new ArgumentException("Необходимо указать ключ подключения.");
            AgentStorage.Save(new AgentSettings
            {
                Computer = name, RouterUrl = url, AutoStart = _autostart.Checked
            }, secret.Length == 0 ? null : secret);
            Saved = true;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception error)
        {
            MessageBox.Show(this, error.Message, "Настройки агента",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
