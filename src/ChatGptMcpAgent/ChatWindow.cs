using System.Drawing;
using System.Text;
using System.Text.Json;

namespace ChatGptMcpAgent;

// A local chat surface. Messages are queued for the ChatGPT MCP plugin; this
// window intentionally does not use Codex, a model API, or a browser.
internal sealed class ChatWindow : Form
{
    private readonly RichTextBox _history = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        BorderStyle = BorderStyle.None,
        BackColor = Color.FromArgb(248, 249, 251),
        Font = new Font("Segoe UI", 10),
        DetectUrls = true
    };
    private readonly TextBox _input = new() 
    {
        Multiline = true,
        AcceptsReturn = true,
        ScrollBars = ScrollBars.Vertical,
        Dock = DockStyle.Fill,
        Font = new Font("Segoe UI", 10)
    };
    private readonly Button _send = new() { Text = "Отправить", Dock = DockStyle.Right, Width = 128 };
    private readonly Label _status = new()
    {
        Text = "Сообщения передаются через подключённый MCP-плагин при проверке очереди.",
        Dock = DockStyle.Bottom,
        Height = 43,
        ForeColor = Color.DimGray
    };
    private readonly System.Windows.Forms.Timer _refresh = new() { Interval = 1000 };
    private string _snapshot = "";
    private string _messageStatus = "Сообщений в очереди нет.";
    private static string CycleStatusPath => Path.Combine(AgentStorage.DataFolder, "Chat", "cycle-status.json");

    private static string ChatFolder => Path.Combine(AgentStorage.DataFolder, "Chat", "messages");

    public ChatWindow()
    {
        Text = "ChatGPT MCP Tunnel — Чат";
        Icon = TrayIcons.AppIcon();
        MinimumSize = new Size(540, 420);
        Size = new Size(760, 650);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);

        var header = new Label
        {
            Dock = DockStyle.Top, Height = 48, Padding = new Padding(13, 11, 10, 0),
            Text = "Чат с ChatGPT  ·  обычный режим, через MCP Tunnel",
            Font = new Font("Segoe UI Semibold", 11)
        };
        var entry = new Panel { Dock = DockStyle.Bottom, Height = 142, Padding = new Padding(12, 6, 12, 7) };
        var inputAndButton = new Panel { Dock = DockStyle.Fill };
        inputAndButton.Controls.Add(_input);
        inputAndButton.Controls.Add(_send);
        entry.Controls.Add(inputAndButton);
        entry.Controls.Add(_status);

        Controls.Add(_history);
        Controls.Add(entry);
        Controls.Add(header);
        _send.Click += (_, _) => SendMessage();
        _input.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter || e.Shift) return;
            e.SuppressKeyPress = true;
            SendMessage();
        };
        _refresh.Tick += (_, _) => RefreshMessages();
        _refresh.Start();
        Shown += (_, _) => { RefreshMessages(); _input.Focus(); };
        FormClosed += (_, _) => _refresh.Dispose();
    }

    private void SendMessage()
    {
        var text = _input.Text.Trim();
        if (text.Length == 0) return;
        if (text.Length > 20000)
        {
            MessageBox.Show(this, "Ограничение сообщения — 20 000 символов.",
                "ChatGPT MCP Tunnel", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            Directory.CreateDirectory(ChatFolder);
            var message = new ChatRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                CreatedUtc = DateTimeOffset.UtcNow,
                Text = text,
                Status = "pending"
            };
            var file = Path.Combine(ChatFolder, message.Id + ".json");
            var temp = file + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(message), new UTF8Encoding(false));
            File.Move(temp, file);
            _input.Clear();
            RefreshMessages();
        }
        catch (Exception e)
        {
            MessageBox.Show(this, "Не удалось сохранить сообщение: " + e.Message,
                "ChatGPT MCP Tunnel", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RefreshMessages()
    {
        UpdateCycleStatus();
        try
        {
            var rows = Directory.Exists(ChatFolder)
                ? Directory.EnumerateFiles(ChatFolder, "*.json")
                    .Select(path =>
                    {
                        try { return JsonSerializer.Deserialize<ChatRecord>(File.ReadAllText(path)); }
                        catch (IOException) { return null; }
                        catch (JsonException) { return null; }
                    })
                    .Where(m => m is not null)
                    .OrderBy(m => m!.CreatedUtc)
                    .TakeLast(100)
                    .ToArray()
                : Array.Empty<ChatRecord?>();

            var snapshot = JsonSerializer.Serialize(rows);
            if (snapshot == _snapshot) return;
            _snapshot = snapshot;

            _history.SuspendLayout();
            _history.Clear();
            foreach (var row in rows)
            {
                if (row is null) continue;
                if (row.Source == "system")
                {
                    Append("ChatGPT · " + row.Text + " · " + row.CreatedUtc.ToLocalTime().ToString("HH:mm"), Color.FromArgb(24, 125, 86), true);
                    Append(row.Reply ?? "", Color.Black, false);
                    Append("", Color.Gray, false);
                    continue;
                }
                Append("Вы  ·  " + row.CreatedUtc.ToLocalTime().ToString("dd.MM HH:mm"), Color.FromArgb(24, 95, 160), true);
                Append(row.Text, Color.Black, false);
                if (row.Status == "answered" && row.Reply is not null)
                {
                    Append("ChatGPT  ·  " + (row.RepliedUtc?.ToLocalTime().ToString("dd.MM HH:mm") ?? ""), Color.FromArgb(24, 125, 86), true);
                    Append(row.Reply, Color.Black, false);
                }
                else
                    Append("Ожидает ответа", Color.Gray, false);
                Append("", Color.Gray, false);
            }
            _history.SelectionStart = _history.TextLength;
            _history.ScrollToCaret();
            _history.ResumeLayout();
            var waiting = rows.Count(m => m is { Status: "pending" });
            _messageStatus = waiting > 0
                ? $"В очереди: {waiting}. ChatGPT заберёт сообщения при следующей проверке."
                : "Все сообщения обработаны. Для нового сообщения нажмите Enter.";
            UpdateCycleStatus();
        }
        catch (Exception e)
        {
            _status.Text = "Ошибка чтения очереди: " + e.GetType().Name;
        }
    }

    private void UpdateCycleStatus()
    {
        var cycle = "Планировщик запускает проверку каждый час.";
        try
        {
            if (File.Exists(CycleStatusPath))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(CycleStatusPath));
                var state = doc.RootElement;
                var lastSeen = state.GetProperty("LastSeenUtc").GetDateTimeOffset();
                var number = state.GetProperty("CycleNumber").GetInt32();
                var age = DateTimeOffset.UtcNow - lastSeen;
                cycle = age <= TimeSpan.FromSeconds(120)
                    ? $"Диагностика №{number}: модель отвечает, последнее обращение {age.TotalSeconds:F0} сек. назад."
                    : $"Нет ответа от модели {age.TotalMinutes:F0} мин. Ожидается следующий почасовой запуск.";
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidOperationException or KeyNotFoundException) { }
        _status.Text = _messageStatus + Environment.NewLine + cycle;
    }

    private void Append(string text, Color color, bool bold)
    {
        _history.SelectionColor = color;
        _history.SelectionFont = new Font(_history.Font, bold ? FontStyle.Bold : FontStyle.Regular);
        _history.AppendText(text + Environment.NewLine);
    }

    private sealed class ChatRecord
    {
        public string Id { get; set; } = "";
        public DateTimeOffset CreatedUtc { get; set; }
        public string Text { get; set; } = "";
        public string Source { get; set; } = "user";
        public string Status { get; set; } = "pending";
        public string? Reply { get; set; }
        public DateTimeOffset? RepliedUtc { get; set; }
    }
}
