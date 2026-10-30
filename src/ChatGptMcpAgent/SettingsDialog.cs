using System.Drawing;
using System.Net.Security;
using System.Security.Cryptography;

namespace ChatGptMcpAgent;

internal sealed class SettingsDialog : Form
{
    private readonly TextBox _computer = new();
    private readonly TextBox _serverAddress = new();
    private readonly Label _keyState = new();
    private readonly CheckBox _autostart = new()
    {
        Text = "Автоматически запускать при входе в Windows", AutoSize = true
    };
    private readonly CheckBox _secure = new() { Text = "Прямое защищённое подключение через Интернет (WSS)", AutoSize = true };
    private readonly TextBox _fingerprint = new();
    private string? _pendingKey;

    internal SettingsDialog(AgentSettings existing)
    {
        Text = "ChatGPT MCP Tunnel — Клиент";
        ClientSize = new Size(500, 475);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Shown += (_, _) => AppTheme.Apply(this);
        Font = new Font("Segoe UI", 10);
        Icon = TrayIcons.AppIcon();

        Controls.Add(new Label
        {
            Text = "Подключение к основному компьютеру",
            Left = 20, Top = 16, Width = 460, Height = 28,
            Font = new Font(Font, FontStyle.Bold)
        });
        AddField("Имя этого компьютера", _computer, 58, existing.Computer);
        var host = "";
        if (Uri.TryCreate(existing.RouterUrl, UriKind.Absolute, out var savedUri))
        {
            host = savedUri.Host;
            _secure.Checked = savedUri.Scheme == "wss";
        }
        AddField("IP-адрес или имя сервера", _serverAddress, 121, host);
        _secure.SetBounds(20, 180, 465, 29);
        Controls.Add(_secure);
        var portHint = new Label
        {
            Text = "", Left = 20, Top = 209, Width = 460, Height = 25,
            ForeColor = SystemColors.GrayText
        };
        void UpdateMode()
        {
            _fingerprint.Enabled = _secure.Checked;
            portHint.Text = _secure.Checked
                ? "Защищённый порт 55202, доступ из другой локальной сети."
                : "Локальный порт 55201.";
        }
        _secure.CheckedChanged += (_, _) => UpdateMode();
        Controls.Add(portHint);
        Controls.Add(new Label
        {
            Text = "SHA-256 отпечаток сертификата сервера (для WSS)",
            Left = 20, Top = 243, Width = 460, Height = 23
        });
        _fingerprint.SetBounds(20, 269, 460, 28);
        _fingerprint.Text = existing.TlsFingerprint;
        Controls.Add(_fingerprint);
        UpdateMode();
        Controls.Add(new Label
        {
            Text = "Ключ подключения к серверу",
            Left = 20, Top = 308, Width = 450, Height = 25
        });
        _keyState.SetBounds(20, 339, 278, 26);
        _keyState.Text = AgentStorage.HasCredential() ? "Ключ сохранён" : "Ключ не указан";
        Controls.Add(_keyState);
        var keyButton = new Button
        {
            Text = "Указать ключ",
            Left = 330, Top = 331, Width = 150, Height = 32
        };
        keyButton.Click += (_, _) =>
        {
            using var dialog = new SecretInputDialog(
                "Введите ключ, полученный от администратора сервера.");
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            _pendingKey = dialog.EnteredValue;
            _keyState.Text = "Новый ключ ожидает сохранения";
        };
        Controls.Add(keyButton);
        // The client MUST NOT have any "show", "reveal", "copy" or
        // exported key action. The stored DPAPI secret is not read here.

        _autostart.SetBounds(20, 381, 470, 26);
        _autostart.Checked = existing.AutoStart;
        Controls.Add(_autostart);

        var check = new Button
        {
            Text = "Проверить", Left = 20, Top = 415, Width = 120, Height = 33
        };
        check.Click += async (_, _) =>
        {
            check.Enabled = false;
            try
            {
                var host = _serverAddress.Text.Trim();
                if (host.Length == 0 || host.Contains('/') || host.Contains(':') ||
                    host.Contains(' ') || host.Contains('@'))
                    throw new ArgumentException("Укажите IP-адрес или имя сервера.");
                var url = new UriBuilder(_secure.Checked ? "wss" : "ws", host, _secure.Checked ? 55202 : 55201, "agent").Uri.AbsoluteUri;
                if (!AgentStorage.ValidateRouter(url, out var reason))
                    throw new ArgumentException(reason);
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(4));
                using var tcp = new System.Net.Sockets.TcpClient();
                await tcp.ConnectAsync(host, _secure.Checked ? 55202 : 55201, timeout.Token);
                if (_secure.Checked)
                {
                    var pin = Convert.FromHexString(DirectTls.NormalizeFingerprint(_fingerprint.Text));
                    using var tls = new SslStream(tcp.GetStream(), false, (_, certificate, _, _) =>
                        certificate is not null &&
                        CryptographicOperations.FixedTimeEquals(pin, SHA256.HashData(certificate.GetRawCertData())));
                    await tls.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                    {
                        TargetHost = host
                    }, timeout.Token);
                }
                MessageBox.Show(this,
                    _secure.Checked
                        ? "TLS-соединение установлено. Отпечаток сертификата совпадает. Ключ проверяется при подключении."
                        : "Сервер доступен по сети. Проверка ключа выполняется при подключении.",
                    "Проверка сервера", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Сервер недоступен: " + ex.Message,
                    "Проверка сервера", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally { check.Enabled = true; }
        };
        Controls.Add(check);
        var save = new Button
        {
            Text = "Сохранить и подключиться",
            Left = 190, Top = 415, Width = 190, Height = 33
        };
        var close = new Button { Text = "Отмена", Left = 389, Top = 415, Width = 91, Height = 33 };
        save.Click += (_, _) => Save();
        close.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        Controls.Add(save);
        Controls.Add(close);
        AcceptButton = save;
        CancelButton = close;
    }

    private void AddField(string label, TextBox entry, int top, string text)
    {
        Controls.Add(new Label { Text = label, Left = 20, Top = top, Width = 460, Height = 23 });
        entry.SetBounds(20, top + 25, 460, 28);
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

            var host = _serverAddress.Text.Trim();
            if (host.Length == 0 || host.Contains('/') || host.Contains('\\') ||
                host.Contains(':') || host.Contains(' ') || host.Contains('@'))
                throw new ArgumentException("Укажите IP-адрес или имя компьютера сервера.");

            var url = new UriBuilder(_secure.Checked ? "wss" : "ws", host, _secure.Checked ? 55202 : 55201, "agent").Uri.AbsoluteUri;
            if (!AgentStorage.ValidateRouter(url, out var reason))
                throw new ArgumentException(reason);
            if (_pendingKey is null && !AgentStorage.HasCredential())
                throw new ArgumentException("Необходимо указать ключ подключения.");
            AgentStorage.Save(new AgentSettings
            {
                Computer = name, RouterUrl = url, AutoStart = _autostart.Checked,
                TlsFingerprint = _secure.Checked ? DirectTls.NormalizeFingerprint(_fingerprint.Text) : ""
            }, _pendingKey);
            _pendingKey = null;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception error)
        {
            MessageBox.Show(this, error.Message, "Настройки клиента",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
