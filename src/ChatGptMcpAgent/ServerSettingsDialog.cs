using System.Drawing;
using System.Security.Cryptography;

namespace ChatGptMcpAgent;

internal sealed class ServerSettingsDialog : Form
{
    private readonly AgentSettings _original;
    private readonly TextBox _address = new();
    private readonly CheckBox _wan = new() { Text = "Прямое защищённое подключение через Интернет (WSS)", AutoSize = true };
    private readonly TextBox _publicHost = new();
    private readonly TextBox? _tunnelEntry;
    private readonly Label _tokenState = new() { AutoSize = true };
    private readonly Label _clientKeyState = new() { AutoSize = true };
    private readonly CheckBox _autostart = new()
    {
        Text = "Запускать автоматически при входе в Windows", AutoSize = true
    };
    private string? _pendingOpenAiKey;
    private string? _pendingClientKey;

    internal ServerSettingsDialog(AgentSettings settings)
    {
        _original = settings;
        Text = "ChatGPT MCP Tunnel — Сервер";
        Icon = TrayIcons.AppIcon();
        ClientSize = new Size(530, 430);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        Shown += (_, _) => AppTheme.Apply(this);
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 10);

        int y = 16;
        Controls.Add(new Label
        {
            Text = "Основной компьютер · существующий туннель OpenAI",
            Left = 20, Top = y, Width = 490, Height = 26,
            Font = new Font(Font, FontStyle.Bold)
        });
        y += 37;

        if (AgentStorage.IsValidTunnelId(settings.TunnelId))
        {
            Controls.Add(new Label
            {
                Text = "Туннель OpenAI: настроен",
                Left = 20, Top = y, Width = 485, Height = 27,
                ForeColor = Color.FromArgb(24, 113, 76)
            });
            y += 34;
        }
        else
        {
            Controls.Add(new Label
            {
                Text = "ID существующего туннеля (только первоначальная настройка)",
                Left = 20, Top = y, Width = 490, Height = 25
            });
            _tunnelEntry = new TextBox { Left = 20, Top = y + 25, Width = 490, Text = settings.TunnelId };
            Controls.Add(_tunnelEntry);
            y += 65;
        }

        Controls.Add(new Label { Text = "Токен OpenAI", Left = 20, Top = y, Width = 205, Height = 25 });
        _tokenState.SetBounds(20, y + 28, 280, 27);
        _tokenState.Text = AgentStorage.HasServerApiKey() ? "Сохранён" : "Не указан";
        Controls.Add(_tokenState);
        var tokenButton = MakeButton(335, y + 20, 175,
            AgentStorage.HasServerApiKey() ? "Сменить токен" : "Указать токен");
        tokenButton.Click += (_, _) =>
        {
            using var dialog = new SecretInputDialog("Введите новый токен OpenAI для существующего туннеля.");
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            _pendingOpenAiKey = dialog.EnteredValue;
            _tokenState.Text = "Новое значение ожидает сохранения";
            tokenButton.Text = "Сменить токен";
        };
        y += 68;

        Controls.Add(new Label
        {
            Text = "Адрес маршрутизатора Router Lite",
            Left = 20, Top = y, Width = 490, Height = 24
        });
        _address.SetBounds(20, y + 25, 330, 28);
        _address.Text = settings.ListenAddress;
        Controls.Add(_address);
        Controls.Add(new Label
        {
            Text = "Порт: 55201", Left = 375, Top = y + 29, Width = 135, Height = 24
        });
        y += 69;

        _wan.SetBounds(20, y, 490, 28);
        _wan.Checked = settings.DirectWanEnabled;
        Controls.Add(_wan);
        y += 31;
        Controls.Add(new Label
        {
            Text = "Статический публичный IP маршрутизатора или доменное имя",
            Left = 20, Top = y, Width = 490, Height = 23
        });
        _publicHost.SetBounds(20, y + 25, 330, 28);
        _publicHost.Text = settings.PublicHost;
        _publicHost.Enabled = _wan.Checked;
        _wan.CheckedChanged += (_, _) => _publicHost.Enabled = _wan.Checked;
        Controls.Add(_publicHost);
        Controls.Add(new Label
        {
            Text = "WSS: 55202", Left = 375, Top = y + 28, Width = 130, Height = 25
        });
        y += 61;
        var fingerprint = MakeButton(20, y, 290, "Копировать отпечаток TLS-сертификата");
        fingerprint.Click += (_, _) =>
        {
            try
            {
                var digest = DirectTls.Fingerprint();
                if (string.IsNullOrEmpty(digest))
                    throw new InvalidOperationException("Сначала включите WSS и сохраните настройки.");
                Clipboard.SetText(digest);
                MessageBox.Show(this,
                    "Отпечаток SHA-256 скопирован. Передайте его клиенту отдельно и сверяйте с сервером.",
                    "Сертификат сервера", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Сертификат сервера",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        y += 42;

        Controls.Add(new Label
        {
            Text = "Общий ключ подключения клиентов",
            Left = 20, Top = y, Width = 490, Height = 25
        });
        if (!AgentStorage.HasCredential())
            _pendingClientKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        _clientKeyState.SetBounds(20, y + 26, 490, 24);
        _clientKeyState.Text = _pendingClientKey is not null
            ? "Создан автоматически — сохранится с настройками" : "Сохранён";
        Controls.Add(_clientKeyState);
        y += 54;

        var specify = MakeButton(20, y, 144, "Указать ключ");
        specify.Click += (_, _) =>
        {
            using var dialog = new SecretInputDialog("Введите новый общий ключ подключения клиентов.");
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            _pendingClientKey = dialog.EnteredValue;
            _clientKeyState.Text = "Новый ключ ожидает сохранения";
        };
        var generate = MakeButton(173, y, 160, "Создать новый");
        generate.Click += (_, _) =>
        {
            if (AgentStorage.HasCredential() &&
                MessageBox.Show(this,
                    "Новый ключ отключит клиентов со старым ключом. Продолжить?",
                    "Замена ключа клиентов", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            _pendingClientKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            _clientKeyState.Text = "Новый ключ ожидает сохранения";
        };
        var copy = MakeButton(342, y, 168, "Копировать ключ");
        copy.Click += (_, _) =>
        {
            try
            {
                var value = _pendingClientKey ?? AgentStorage.LoadServerAgentKey();
                if (string.IsNullOrEmpty(value))
                {
                    MessageBox.Show(this, "Ключ ещё не создан.", "Ключ клиентов");
                    return;
                }
                Clipboard.SetText(value);
                AgentStorage.Log("Server client key copied to clipboard (value not logged).");
            }
            catch (Exception error)
            {
                MessageBox.Show(this, "Не удалось скопировать ключ: " + error.GetType().Name,
                    "Ключ клиентов", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        y += 53;

        _autostart.SetBounds(20, y, 490, 25);
        _autostart.Checked = settings.AutoStart;
        Controls.Add(_autostart);
        y += 43;

        var allowLan = MakeButton(20, y, 294, "Разрешить доступ клиентам в локальной сети");
        void UpdateFirewallLabel() =>
            allowLan.Text = LanFirewall.IsTailnetIpv4(_address.Text.Trim())
                ? "Разрешить доступ клиенту Tailscale"
                : "Разрешить доступ клиентам в локальной сети";
        _address.TextChanged += (_, _) => UpdateFirewallLabel();
        UpdateFirewallLabel();
        allowLan.Click += async (_, _) =>
        {
            allowLan.Enabled = false;
            try
            {
                var addr = _address.Text.Trim();
                if (LanFirewall.IsTailnetIpv4(addr))
                {
                    if (addr != _original.ListenAddress)
                        throw new InvalidOperationException(
                            "Сначала сохраните адрес Tailscale и откройте настройки снова.");
                    await LanFirewall.RequestTailnetSetupAsync(this, addr);
                }
                else
                    await LanFirewall.RequestSetupAsync(this, addr);
            }
            catch (System.ComponentModel.Win32Exception error)
            {
                MessageBox.Show(this,
                    error.NativeErrorCode == 1223
                        ? "Подтверждение администратора отменено."
                        : "Не удалось запросить права администратора: " + error.Message,
                    "Брандмауэр Windows", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception error)
            {
                MessageBox.Show(this, error.Message,
                    "Брандмауэр Windows", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally { allowLan.Enabled = true; }
        };
        y += 44;
        var allowWan = MakeButton(20, y, 345, "Разрешить защищённый доступ из Интернета");
        allowWan.Enabled = settings.DirectWanEnabled;
        allowWan.Click += async (_, _) =>
        {
            allowWan.Enabled = false;
            try
            {
                await LanFirewall.RequestWanSetupAsync(this, _address.Text.Trim());
            }
            catch (System.ComponentModel.Win32Exception error)
            {
                MessageBox.Show(this, error.NativeErrorCode == 1223
                    ? "Подтверждение администратора отменено." : error.Message,
                    "Брандмауэр Windows", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception error)
            {
                MessageBox.Show(this, error.Message, "Брандмауэр Windows",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally { allowWan.Enabled = true; }
        };
        y += 44;

        var check = MakeButton(20, y, 120, "Проверить");
        check.Click += async (_, _) =>
        {
            check.Enabled = false;
            try
            {
                var ip = _address.Text.Trim();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(4));
                var state = await ServerStatusProbe.CheckAsync(ip, timeout.Token);
                MessageBox.Show(this, state == ServerConnectivity.Ready
                        ? "Туннель и маршрутизатор готовы. Порт 55201 доступен."
                        : ServerStatusProbe.Describe(state),
                    "Проверка сервера", MessageBoxButtons.OK,
                    state == ServerConnectivity.Ready
                        ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception error)
            {
                MessageBox.Show(this, "Проверка завершилась с ошибкой: " +
                    error.GetType().Name, "Проверка сервера",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally { check.Enabled = true; }
        };
        var save = MakeButton(288, y, 117, "Сохранить");
        var cancel = MakeButton(414, y, 96, "Отмена");
        save.Click += (_, _) => Save();
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        AcceptButton = save;
        CancelButton = cancel;
        ClientSize = new Size(530, y + 50);
    }

    private Button MakeButton(int x, int y, int width, string title)
    {
        var button = new Button { Text = title, Left = x, Top = y, Width = width, Height = 31 };
        Controls.Add(button);
        return button;
    }

    private void Save()
    {
        try
        {
            var ip = _address.Text.Trim();
            if (!System.Net.IPAddress.TryParse(ip, out var parsed) ||
                parsed.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork ||
                (!System.Net.IPAddress.IsLoopback(parsed) && !IsLocalIpv4(parsed)))
                throw new ArgumentException("Нужно указать локальный IPv4-адрес этого компьютера.");
            var id = _tunnelEntry?.Text.Trim() ?? _original.TunnelId;
            if (!AgentStorage.IsValidTunnelId(id))
                throw new ArgumentException("Укажите ID действующего туннеля OpenAI.");
            if (_pendingOpenAiKey is null && !AgentStorage.HasServerApiKey())
                throw new ArgumentException("Нужно указать токен OpenAI.");
            if (_pendingClientKey is null && !AgentStorage.HasCredential())
                throw new ArgumentException("Нужно указать ключ подключения клиентов.");
            var publicHost = _wan.Checked ? DirectTls.ValidatePublicHost(_publicHost.Text) : _original.PublicHost;
            if (_wan.Checked && _original.DirectWanEnabled &&
                !string.Equals(publicHost, _original.PublicHost, StringComparison.OrdinalIgnoreCase) &&
                MessageBox.Show(this,
                    "Публичный адрес изменён. Будет создан новый TLS-сертификат, клиентам потребуется новый отпечаток. Продолжить?",
                    "Изменение защищённого адреса", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes) return;
            AgentStorage.SaveServer(new AgentSettings
            {
                Computer = Environment.MachineName,
                Role = "server",
                TunnelId = id,
                ListenAddress = ip,
                DirectWanEnabled = _wan.Checked,
                PublicHost = publicHost,
                AutoStart = _autostart.Checked
            }, _pendingClientKey, _pendingOpenAiKey);
            _pendingClientKey = null;
            _pendingOpenAiKey = null;
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
