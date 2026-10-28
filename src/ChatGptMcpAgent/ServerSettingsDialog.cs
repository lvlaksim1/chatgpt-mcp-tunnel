using System.Drawing;
using System.Security.Cryptography;

namespace ChatGptMcpAgent;

internal sealed class ServerSettingsDialog : Form
{
    private readonly TextBox tunnel = new();
    private readonly TextBox address = new();
    private readonly TextBox api = new() { UseSystemPasswordChar = true };
    private readonly TextBox agents = new() { UseSystemPasswordChar = true };
    private readonly CheckBox autostart = new()
    {
        Text = "Запускать автоматически при входе в Windows", AutoSize = true
    };
    internal ServerSettingsDialog(AgentSettings settings)
    {
        Text = "ChatGPT MCP Tunnel — Сервер";
        Icon = TrayIcons.AppIcon();
        ClientSize = new Size(520, 420);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 10);
        Controls.Add(new Label {
            Text = "Основной компьютер: один туннель OpenAI и маршрутизатор клиентов",
            Left = 20, Top = 14, Width = 490, Height = 30
        });
        Field("ID существующего туннеля OpenAI (не изменяется)", tunnel, 50, settings.TunnelId);
        Field("IP-адрес интерфейса маршрутизатора", address, 111, settings.ListenAddress);
        Field("Ключ OpenAI API (доступ к туннелю)", api, 172, "");
        Field("Общий ключ подключения клиентов", agents, 233, "");
        api.PlaceholderText = AgentStorage.HasServerApiKey()
            ? "Сохранён — оставьте пустым для сохранения"
            : "Введите токен доступа к существующему туннелю";
        agents.PlaceholderText = AgentStorage.HasCredential()
            ? "Сохранён — пустое поле не изменяет ключ"
            : "Введите общий ключ для клиентов";
        if (!AgentStorage.HasCredential())
            agents.Text = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var reveal = new CheckBox
        {
            Text = "Показать ключ клиентов", Left = 20, Top = 299, AutoSize = true
        };
        reveal.CheckedChanged += (_, _) => agents.UseSystemPasswordChar = !reveal.Checked;
        var copy = new Button { Text = "Копировать ключ", Left = 333, Top = 295,
            Width = 172, Height = 31 };
        copy.Click += (_, _) =>
        {
            if (!string.IsNullOrEmpty(agents.Text)) Clipboard.SetText(agents.Text);
        };
        Controls.Add(reveal);
        Controls.Add(copy);
        autostart.Left = 20;
        autostart.Top = 339;
        autostart.Checked = settings.AutoStart;
        Controls.Add(autostart);
        var save = new Button { Text = "Сохранить", Left = 315, Top = 378, Width = 105, Height = 32 };
        var cancel = new Button { Text = "Отмена", Left = 430, Top = 378, Width = 78, Height = 32 };
        save.Click += (_, _) => Save();
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.Add(save);
        Controls.Add(cancel);
        AcceptButton = save;
        CancelButton = cancel;
    }

    private void Field(string name, TextBox input, int top, string current)
    {
        Controls.Add(new Label { Text = name, Left = 20, Top = top, Width = 480, Height = 23 });
        input.SetBounds(20, top + 23, 485, 28);
        input.Text = current;
        Controls.Add(input);
    }

    private void Save()
    {
        try
        {
            var ip = address.Text.Trim();
            if (!System.Net.IPAddress.TryParse(ip, out var parsed) ||
                parsed.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork ||
                System.Net.IPAddress.IsLoopback(parsed) == false &&
                !IsLocalIpv4(parsed))
                throw new ArgumentException("Нужно указать локальный IPv4-адрес этого компьютера.");
            var agentKey = agents.Text.Length == 0 ? null : agents.Text;
            var apiKey = api.Text.Length == 0 ? null : api.Text;
            if (agentKey is null && !AgentStorage.HasCredential())
                throw new ArgumentException("Необходимо создать ключ подключения клиентов.");
            if (apiKey is null && !AgentStorage.HasServerApiKey())
                throw new ArgumentException("Необходимо указать ключ доступа OpenAI.");
            if (!AgentStorage.IsValidTunnelId(tunnel.Text.Trim()))
                throw new ArgumentException("Укажите ID уже существующего туннеля OpenAI.");
            AgentStorage.SaveServer(new AgentSettings
            {
                Computer = Environment.MachineName,
                Role = "server",
                TunnelId = tunnel.Text.Trim(),
                ListenAddress = ip,
                AutoStart = autostart.Checked
            }, agentKey, apiKey);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Настройки сервера",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static bool IsLocalIpv4(System.Net.IPAddress requested) =>
        System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
            .SelectMany(i => i.GetIPProperties().UnicastAddresses)
            .Any(a => a.Address.Equals(requested));
}
