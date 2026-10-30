using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ChatGptMcpAgent;

// Local queue for a connected ChatGPT MCP session; never starts a second model or cloud tunnel.
internal sealed class ChatWindow : Form
{
    private const long MaxFileSize = 2L * 1024 * 1024 * 1024;
    private const int MaxAttachments = 20;
    private readonly RichTextBox _history = new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None,
        BackColor = Color.FromArgb(248, 249, 251), Font = new Font("Segoe UI", 10),
        DetectUrls = true
    };
    private readonly TextBox _input = new()
    {
        Multiline = true, AcceptsReturn = true, ScrollBars = ScrollBars.Vertical,
        Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10),
        AllowDrop = true
    };
    private readonly Button _send = new() { Text = "Отправить", Dock = DockStyle.Right, Width = 117 };
    private readonly Button _attach = new()
    {
        Text = "📎", Dock = DockStyle.Left, Width = 47,
        AccessibleName = "Прикрепить файлы"
    };
    private readonly FlowLayoutPanel _attachmentStrip = new()
    {
        Dock = DockStyle.Bottom, Height = 73, AutoScroll = true,
        FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
        BackColor = Color.FromArgb(242, 245, 249), Padding = new Padding(3)
    };
    private readonly Label _status = new()
    {
        Text = "Чат: автономный браузерный транспорт · авторизация через отдельный профиль ChatGPT.",
        Dock = DockStyle.Bottom, Height = 43, ForeColor = Color.DimGray
    };
    private readonly System.Windows.Forms.Timer _refresh = new() { Interval = 1000 };
    private readonly ListBox _chatList = new() {
        Dock = DockStyle.Fill, BorderStyle = BorderStyle.None,
        Font = new Font("Segoe UI", 9.5f), IntegralHeight = false,
        BackColor = Color.FromArgb(242, 245, 249), HorizontalScrollbar = false,
        DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 66
    };
    private readonly Label _header = new()
    {
        Dock = DockStyle.Top, Height = 48, Padding = new Padding(13, 11, 10, 0),
        Text = "Чат с ChatGPT · обычный режим, через MCP Tunnel",
        Font = new Font("Segoe UI Semibold", 11)
    };
    private readonly Button _signIn = new() {
        Text = "Вход в ChatGPT", Dock = DockStyle.Right, Width = 150
    };
    private readonly Button _importChat = new() { Text = "Импорт чата", Dock = DockStyle.Top, Height = 36 };
    private readonly Button _newChat = new() {
        Text = "＋ Новый чат", Dock = DockStyle.Top, Height = 44,
        Font = new Font("Segoe UI Semibold", 10), FlatStyle = FlatStyle.Flat
    };
    private string _activeChatId = "";
    private string _chatListSnapshot = "";
    private bool _changingChat;
    private readonly Dictionary<string, (string Text, PendingAttachment[] Attachments)> _drafts = new();
    private DateTimeOffset? _lastReplyUtc;
    private readonly List<PendingAttachment> _pending = new();
    private string _snapshot = "";
    private string _messageStatus = "Сообщений в очереди нет.";
    private bool _sending;
    private static string ChatRoot => Environment.GetEnvironmentVariable("CHATGPT_MCP_CHAT_TEST_ROOT") ??
        Path.Combine(AgentStorage.DataFolder, "Chat");
    private static string CycleStatusPath => Path.Combine(ChatRoot, "cycle-status.json");
    private static string ChatFolder => Path.Combine(ChatRoot, "messages");
    private static string AttachmentFolder => Path.Combine(ChatRoot, "attachments");

    public ChatWindow()
    {
        Text = "ChatGPT MCP Tunnel — Чат";
        Icon = TrayIcons.AppIcon();
        MinimumSize = new Size(770, 450);
        Size = new Size(1030, 685);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        AllowDrop = true;

        var entry = new Panel { Dock = DockStyle.Bottom, Height = 222, Padding = new Padding(12, 6, 12, 7) };
        var inputAndButton = new Panel { Dock = DockStyle.Fill };
        inputAndButton.Controls.Add(_input);
        inputAndButton.Controls.Add(_send);
        inputAndButton.Controls.Add(_attach);
        entry.Controls.Add(inputAndButton);
        entry.Controls.Add(_attachmentStrip);
        entry.Controls.Add(_status);
        var sidebar = new Panel {
            Dock = DockStyle.Left, Width = 224,
            Padding = new Padding(8), BackColor = Color.FromArgb(242, 245, 249)
        };
        var caption = new Label {
            Text = "Чаты этого компьютера", Dock = DockStyle.Top, Height = 30,
            Font = new Font("Segoe UI Semibold", 9.5f),
            ForeColor = Color.FromArgb(65, 75, 93)
        };
        sidebar.Controls.Add(_chatList);
        sidebar.Controls.Add(_importChat);
        sidebar.Controls.Add(caption);
        sidebar.Controls.Add(_newChat);
        _newChat.FlatAppearance.BorderSize = 0;
        _newChat.Click += (_, _) => CreateChat();
        _importChat.Click += (_, _) => ImportCloudChat();
        _chatList.DrawItem += DrawChatItem;
        _chatList.SelectedIndexChanged += (_, _) => {
            if (!_changingChat && _chatList.SelectedItem is ChatListItem item)
                SwitchChat(item.Id);
        };
        _activeChatId = ChatSessionStore.Active();
        Controls.Add(_history);
        Controls.Add(sidebar);
        Controls.Add(entry);
        Controls.Add(_header);
        var login = new Panel { Dock = DockStyle.Top, Height = 37 };
        login.Controls.Add(_signIn);
        Controls.Add(login);
        login.BringToFront();
        _signIn.Click += (_, _) => BrowserChatWorker.ShowSignIn(this);

        _attach.Click += (_, _) => SelectFiles();
        _send.Click += (_, _) => SendMessage();
        _input.KeyDown += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.V && TryPasteImage())
            {
                e.SuppressKeyPress = true;
                return;
            }
            if (e.KeyCode != Keys.Enter || e.Shift) return;
            e.SuppressKeyPress = true;
            SendMessage();
        };
        _input.DragEnter += FileDragEnter;
        _input.DragDrop += FileDragDrop;
        DragEnter += FileDragEnter;
        DragDrop += FileDragDrop;
        _refresh.Tick += (_, _) => RefreshMessages();
        _refresh.Start();
        Shown += (_, _) => { RefreshSessions(); RefreshMessages(); _input.Focus(); };
        FormClosed += (_, _) => _refresh.Dispose();
    }

    private sealed record ChatListItem(string Id, string Title, string Preview, DateTimeOffset Updated) {
        public override string ToString() => Title;
    }

    private void DrawChatItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _chatList.Items.Count) return;
        var item = (ChatListItem)_chatList.Items[e.Index]!;
        var selected = (e.State & DrawItemState.Selected) != 0;
        using var bg = new SolidBrush(selected
            ? Color.FromArgb(216, 232, 253) : Color.FromArgb(242, 245, 249));
        e.Graphics.FillRectangle(bg, e.Bounds);
        var r = e.Bounds;
        var w = Math.Max(30, r.Width - 18);
        using var titleFont = new Font("Segoe UI Semibold", 9.5f);
        TextRenderer.DrawText(e.Graphics, item.Title, titleFont,
            new Rectangle(r.X + 8, r.Y + 6, w, 23),
            Color.FromArgb(32, 43, 57),
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        var preview = string.IsNullOrWhiteSpace(item.Preview)
            ? item.Updated.ToLocalTime().ToString("dd.MM HH:mm") : item.Preview;
        TextRenderer.DrawText(e.Graphics, preview, Font,
            new Rectangle(r.X + 8, r.Y + 32, w, 23),
            Color.FromArgb(103, 114, 128),
            TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        if (selected) e.DrawFocusRectangle();
    }

    private void RefreshSessions()
    {
        try
        {
            var sessions = ChatSessionStore.List();
            var snapshot = JsonSerializer.Serialize(sessions.Select(s =>
                new { s.Id, s.Title, s.UpdatedUtc, s.LastPreview }));
            if (snapshot == _chatListSnapshot) return;
            _chatListSnapshot = snapshot;
            _changingChat = true;
            try
            {
                _chatList.BeginUpdate();
                _chatList.Items.Clear();
                foreach (var session in sessions)
                    _chatList.Items.Add(new ChatListItem(session.Id, session.Title,
                        session.LastPreview, session.UpdatedUtc));
                for (var i = 0; i < _chatList.Items.Count; i++)
                    if (((ChatListItem)_chatList.Items[i]!).Id == _activeChatId) {
                        _chatList.SelectedIndex = i;
                        break;
                    }
            }
            finally { _chatList.EndUpdate(); _changingChat = false; }
            var active = sessions.FirstOrDefault(s => s.Id == _activeChatId);
            if (active is not null)
                _header.Text = "Чат: " + active.Title + "  ·  ChatGPT MCP Tunnel";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) {
            _status.Text = "Ошибка списка чатов: " + ex.GetType().Name;
        }
    }

    private void SaveDraft()
    {
        if (!string.IsNullOrWhiteSpace(_activeChatId))
            _drafts[_activeChatId] = (_input.Text, _pending.ToArray());
    }

    private void SwitchChat(string id)
    {
        if (_sending || id == _activeChatId) return;
        SaveDraft();
        ChatSessionStore.Select(id);
        _activeChatId = id;
        _input.Text = _drafts.TryGetValue(id, out var draft) ? draft.Text : "";
        _pending.Clear();
        if (_drafts.TryGetValue(id, out draft)) _pending.AddRange(draft.Attachments);
        UpdateAttachmentStrip();
        _snapshot = "";
        _lastReplyUtc = null;
        RefreshSessions();
        RefreshMessages();
        _input.Focus();
    }

    private void CreateChat()
    {
        if (_sending) return;
        SaveDraft();
        var session = ChatSessionStore.Create();
        _activeChatId = session.Id;
        _input.Clear();
        _pending.Clear();
        UpdateAttachmentStrip();
        _snapshot = "";
        _lastReplyUtc = null;
        RefreshSessions();
        RefreshMessages();
        _input.Focus();
    }

    private void ImportCloudChat()
    {
        using var dialog = new ChatCloudImportDialog();
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.ImportedChatId is null) return;
        SaveDraft();
        _activeChatId = dialog.ImportedChatId;
        ChatSessionStore.Select(_activeChatId);
        _snapshot = "";
        RefreshSessions();
        RefreshMessages();
    }

    private void SelectFiles()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Прикрепить файлы к сообщению",
            Filter = "Все файлы (*.*)|*.*", Multiselect = true, CheckFileExists = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK) AddFiles(dialog.FileNames);
    }

    private void FileDragEnter(object? sender, DragEventArgs e) =>
        e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
            ? DragDropEffects.Copy : DragDropEffects.None;

    private void FileDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] files) AddFiles(files);
    }

    private void AddFiles(IEnumerable<string> paths)
    {
        if (_sending) return;
        foreach (var path in paths)
        {
            try
            {
                if (_pending.Count >= MaxAttachments)
                    throw new InvalidOperationException($"Не больше {MaxAttachments} вложений.");
                var file = new FileInfo(path);
                if (!file.Exists) throw new FileNotFoundException("Файл не найден.", path);
                if (file.Length > MaxFileSize)
                    throw new InvalidOperationException("Файл превышает 2 ГиБ.");
                _pending.Add(new PendingAttachment
                {
                    Name = file.Name, Path = file.FullName,
                    SizeBytes = file.Length, IsImage = IsImageName(file.Name)
                });
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                MessageBox.Show(this, $"Не удалось прикрепить {Path.GetFileName(path)}: {ex.Message}",
                    "Вложения", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        UpdateAttachmentStrip();
    }

    private bool TryPasteImage()
    {
        try
        {
            if (!Clipboard.ContainsImage()) return false;
            if (_pending.Count >= MaxAttachments)
            {
                MessageBox.Show(this, $"Не больше {MaxAttachments} вложений.");
                return true;
            }
            using var image = Clipboard.GetImage();
            if (image is null) return false;
            using var memory = new MemoryStream();
            image.Save(memory, ImageFormat.Png);
            if (memory.Length > MaxFileSize) throw new InvalidOperationException("Изображение слишком большое.");
            var bytes = memory.ToArray();
            _pending.Add(new PendingAttachment
            {
                Name = $"Снимок-{DateTime.Now:yyyyMMdd-HHmmss}.png",
                ImageBytes = bytes, SizeBytes = bytes.Length, IsImage = true
            });
            UpdateAttachmentStrip();
            return true;
        }
        catch (Exception ex) when (ex is System.Runtime.InteropServices.ExternalException
                                    or InvalidOperationException or ArgumentException)
        {
            MessageBox.Show(this, "Ошибка вставки изображения: " + ex.Message,
                "Буфер обмена", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return true;
        }
    }

    private void UpdateAttachmentStrip()
    {
        _attachmentStrip.SuspendLayout();
        foreach (Control control in _attachmentStrip.Controls)
        {
            if (control is Panel panel)
                foreach (Control child in panel.Controls)
                    if (child is PictureBox picture) picture.Image?.Dispose();
        }
        _attachmentStrip.Controls.Clear();
        foreach (var item in _pending.ToArray())
        {
            var card = new Panel
            {
                Width = 220, Height = 58, BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White, Margin = new Padding(3)
            };
            var remove = new Button
            {
                Text = "×", Dock = DockStyle.Right, Width = 27,
                AccessibleName = $"Удалить вложение {item.Name}",
                FlatStyle = FlatStyle.Flat
            };
            remove.FlatAppearance.BorderSize = 0;
            remove.Click += (_, _) => { _pending.Remove(item); UpdateAttachmentStrip(); };
            var label = new Label
            {
                Text = item.Name + Environment.NewLine + FormatSize(item.SizeBytes),
                Dock = DockStyle.Fill, AutoEllipsis = true, Padding = new Padding(5, 8, 1, 0),
                Font = new Font("Segoe UI", 8.2f)
            };
            if (item.IsImage)
            {
                var preview = new PictureBox
                {
                    Dock = DockStyle.Left, Width = 54, SizeMode = PictureBoxSizeMode.Zoom
                };
                try
                {
                    if (item.ImageBytes is not null)
                    {
                        using var stream = new MemoryStream(item.ImageBytes);
                        using var image = Image.FromStream(stream);
                        preview.Image = new Bitmap(image, new Size(50, 50));
                    }
                    else if (item.Path is not null && item.SizeBytes <= 16 * 1024 * 1024)
                    {
                        using var image = Image.FromFile(item.Path);
                        preview.Image = new Bitmap(image, new Size(50, 50));
                    }
                }
                catch (Exception ex) when (ex is IOException or ArgumentException or OutOfMemoryException) { }
                card.Controls.Add(preview);
            }
            card.Controls.Add(label);
            card.Controls.Add(remove);
            _attachmentStrip.Controls.Add(card);
        }
        _attachmentStrip.ResumeLayout();
    }

    private static bool IsImageName(string name) =>
        new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp" }
            .Contains(Path.GetExtension(name).ToLowerInvariant());

    private static string FormatSize(long bytes) =>
        bytes < 1024 ? $"{bytes} Б" :
        bytes < 1048576 ? $"{bytes / 1024.0:F1} КиБ" :
        $"{bytes / 1048576.0:F1} МиБ";

    private async void SendMessage()
    {
        if (_sending) return;
        var text = _input.Text.Trim();
        if (text.Length == 0 && _pending.Count == 0) return;
        if (text.Length > 20000)
        {
            MessageBox.Show(this, "Ограничение текста — 20 000 символов.",
                "ChatGPT MCP Tunnel", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        _sending = true;
        _send.Enabled = _attach.Enabled = _newChat.Enabled = _importChat.Enabled = _chatList.Enabled = false;
        _messageStatus = "Сохранение вложений…";
        UpdateCycleStatus();
        var attachments = _pending.ToArray();
        var chatId = _activeChatId;
        try
        {
            await Task.Run(() => PersistMessage(text, attachments, chatId));
            if (IsDisposed) return;
            _input.Clear();
            _pending.Clear();
            _drafts.Remove(chatId);
            UpdateAttachmentStrip();
            _snapshot = "";
            RefreshSessions();
            RefreshMessages();
        }
        catch (Exception ex)
        {
            _messageStatus = "Ошибка отправки: " + ex.GetType().Name;
            if (!IsDisposed) UpdateCycleStatus();
            if (!IsDisposed) MessageBox.Show(this, "Не удалось отправить сообщение: " + ex.Message,
                "ChatGPT MCP Tunnel", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _sending = false;
            if (!IsDisposed) _send.Enabled = _attach.Enabled = _newChat.Enabled = _importChat.Enabled = _chatList.Enabled = true;
        }
    }

    private static void PersistMessage(string text, PendingAttachment[] attachments, string chatId)
    {
        if (!ChatSessionStore.Valid(chatId))
            throw new ArgumentException("Не указан идентификатор чата.", nameof(chatId));
        var id = Guid.NewGuid().ToString("N");
        var destination = Path.Combine(AttachmentFolder, id);
        var saved = new List<ChatAttachment>();
        try
        {
            if (attachments.Length != 0) Directory.CreateDirectory(destination);
            foreach (var item in attachments)
            {
                var attachmentId = Guid.NewGuid().ToString("N");
                var target = Path.Combine(destination, attachmentId);
                if (item.ImageBytes is not null)
                    File.WriteAllBytes(target, item.ImageBytes);
                else if (item.Path is not null)
                    File.Copy(item.Path, target, overwrite: false);
                else throw new InvalidOperationException("Отсутствует источник вложения.");
                using var file = File.OpenRead(target);
                saved.Add(new ChatAttachment
                {
                    Id = attachmentId, Name = item.Name,
                    SizeBytes = file.Length,
                    Sha256 = Convert.ToHexString(SHA256.HashData(file)).ToLowerInvariant(),
                    MediaType = MediaTypeFor(item.Name)
                });
            }

            Directory.CreateDirectory(ChatFolder);
            var message = new ChatRecord
            {
                Id = id, ChatId = chatId, CreatedUtc = DateTimeOffset.UtcNow,
                Text = text, Status = "pending", Attachments = saved
            };
            var filePath = Path.Combine(ChatFolder, id + ".json");
            var temp = filePath + ".tmp";
            try
            {
                File.WriteAllText(temp, JsonSerializer.Serialize(message), new UTF8Encoding(false));
                File.Move(temp, filePath);
                ChatSessionStore.Touch(chatId, text.Length > 0 ? text : "📎 Вложения");
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        catch
        {
            if (Directory.Exists(destination)) Directory.Delete(destination, recursive: true);
            throw;
        }
    }

    private static string MediaTypeFor(string name) => Path.GetExtension(name).ToLowerInvariant() switch
    {
        ".png" => "image/png", ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif", ".webp" => "image/webp", ".bmp" => "image/bmp",
        ".txt" or ".md" or ".csv" or ".json" or ".xml" or ".log" or ".cs" or ".py" => "text/plain",
        ".pdf" => "application/pdf",
        _ => "application/octet-stream"
    };

    private void RefreshMessages()
    {
        RefreshSessions();
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
                    .Where(m => m is not null &&
                        (string.IsNullOrWhiteSpace(m.ChatId) ? ChatSessionStore.LegacyId : m.ChatId) == _activeChatId)
                    .OrderBy(m => m!.CreatedUtc)
                    .TakeLast(100)
                    .ToArray()
                : Array.Empty<ChatRecord?>();

            var historyPath = Path.Combine(ChatRoot, "browser-history", _activeChatId + ".json");
            var historyJson = File.Exists(historyPath) ? File.ReadAllText(historyPath) : "";
            var partials = new Dictionary<string, string>();
            foreach (var row in rows)
            {
                if (row is not { Status: "pending" }) continue;
                var part = Path.Combine(ChatRoot, "browser-partials", row.Id + ".json");
                if (!File.Exists(part)) continue;
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(part));
                    partials[row.Id] = doc.RootElement.GetProperty("text").GetString() ?? "";
                }
                catch (Exception ex) when (ex is IOException or JsonException) { }
            }
            var snapshot = JsonSerializer.Serialize(rows) + historyJson + JsonSerializer.Serialize(partials);
            if (snapshot == _snapshot) return;
            _snapshot = snapshot;
            _history.SuspendLayout();
            _history.Clear();
            if (historyJson.Length != 0)
            {
                try
                {
                    using var cloud = JsonDocument.Parse(historyJson);
                    if (cloud.RootElement.TryGetProperty("items", out var entries))
                        foreach (var item in entries.EnumerateArray())
                        {
                            var role = item.GetProperty("role").GetString();
                            var body = item.GetProperty("text").GetString() ?? "";
                            if (role is not ("assistant" or "user") || body.Length == 0) continue;
                            Append(role == "assistant" ? "ChatGPT · история" : "Вы · история",
                                role == "assistant" ? Color.FromArgb(24,125,86) :
                                Color.FromArgb(24,95,160), true);
                            Append(body, Color.Black, false);
                            Append("", Color.Gray, false);
                        }
                }
                catch (JsonException) { }
            }
            _lastReplyUtc = rows.Where(m => m?.Status == "answered" && m.RepliedUtc is not null)
                .Max(m => m?.RepliedUtc);
            foreach (var row in rows)
            {
                if (row is null) continue;
                if (row.Source == "system")
                {
                    Append("ChatGPT · " + row.Text + " · " + row.CreatedUtc.ToLocalTime().ToString("HH:mm"),
                        Color.FromArgb(24, 125, 86), true);
                    Append(row.Reply ?? "", Color.Black, false);
                    Append("", Color.Gray, false);
                    continue;
                }
                Append("Вы  ·  " + row.CreatedUtc.ToLocalTime().ToString("dd.MM HH:mm"),
                    Color.FromArgb(24, 95, 160), true);
                if (!string.IsNullOrEmpty(row.Text)) Append(row.Text, Color.Black, false);
                foreach (var attachment in row.Attachments ?? new())
                    Append("📎 " + attachment.Name + " (" + FormatSize(attachment.SizeBytes) + ")",
                        Color.FromArgb(75, 89, 111), false);
                if (row.Status == "answered" && row.Reply is not null)
                {
                    Append("ChatGPT  ·  " + (row.RepliedUtc?.ToLocalTime().ToString("dd.MM HH:mm") ?? ""),
                        Color.FromArgb(24, 125, 86), true);
                    Append(row.Reply, Color.Black, false);
                }
                else if (partials.TryGetValue(row.Id, out var draftReply) && draftReply.Length != 0)
                {
                    Append("ChatGPT · печатает…", Color.FromArgb(24,125,86), true);
                    Append(draftReply, Color.Black, false);
                }
                else Append("Ожидает ответа", Color.Gray, false);
                Append("", Color.Gray, false);
            }
            _history.SelectionStart = _history.TextLength;
            _history.ScrollToCaret();
            _history.ResumeLayout();
            var waiting = rows.Count(m => m is { Status: "pending" });
            _messageStatus = waiting > 0
                ? $"Ожидают ответа в этом чате: {waiting}."
                : "Сообщения этого чата обработаны.";
            UpdateCycleStatus();
        }
        catch (Exception ex) { _status.Text = "Ошибка чтения очереди: " + ex.GetType().Name; }
    }

    private void UpdateCycleStatus()
    {
        var heartbeatPath = Path.Combine(ChatRoot, "browser-bridge", "heartbeat.json");
        try
        {
            if (File.Exists(heartbeatPath))
            {
                using var heartbeat = JsonDocument.Parse(File.ReadAllText(heartbeatPath));
                var mode = heartbeat.RootElement.GetProperty("mode").GetString();
                var at = heartbeat.RootElement.GetProperty("at").GetDateTimeOffset();
                var fresh = DateTimeOffset.UtcNow - at < TimeSpan.FromSeconds(12);
                _status.Text = _messageStatus + Environment.NewLine +
                    (fresh ? (mode == "browser" ? "Браузерный транспорт работает · ChatGPT" :
                        "Ожидается вход в ChatGPT: нажмите «Вход в ChatGPT»") :
                        "Браузерный транспорт отключён");
                return;
            }
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException) { }
        _status.Text = _messageStatus + Environment.NewLine + "Браузерный транспорт пока не запущен.";
        return;
    }

    private void Append(string text, Color color, bool bold)
    {
        _history.SelectionColor = color;
        using var font = new Font(_history.Font, bold ? FontStyle.Bold : FontStyle.Regular);
        _history.SelectionFont = font;
        _history.AppendText(text + Environment.NewLine);
    }

    // Windows Forms integration, isolated on a temporary chat directory.
    internal static int SelfTestSessions()
    {
        var previous = Environment.GetEnvironmentVariable("CHATGPT_MCP_CHAT_TEST_ROOT");
        var root = Path.Combine(Path.GetTempPath(), "mcp-chat-sessions-" + Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable("CHATGPT_MCP_CHAT_TEST_ROOT", root);
        try
        {
            var messages = Path.Combine(root, "messages");
            Directory.CreateDirectory(messages);
            // Simulate an old user message without ChatId: it must remain intact.
            var original = new ChatRecord {
                Id = Guid.NewGuid().ToString("N"), Text = "LEGACY_MESSAGE",
                CreatedUtc = DateTimeOffset.UtcNow.AddDays(-1), Status = "answered",
                Reply = "LEGACY_REPLY", RepliedUtc = DateTimeOffset.UtcNow.AddDays(-1)
            };
            var oldPath = Path.Combine(messages, original.Id + ".json");
            var oldBytes = JsonSerializer.Serialize(original);
            File.WriteAllText(oldPath, oldBytes);
            using var window = new ChatWindow();
            if (window._activeChatId != ChatSessionStore.LegacyId) return 70;
            if (ChatSessionStore.List().Count != 1) return 71;
            window.RefreshSessions();
            window.RefreshMessages();
            if (!window._history.Text.Contains("LEGACY_MESSAGE")) return 72;

            window.CreateChat();
            var first = window._activeChatId;
            if (first == ChatSessionStore.LegacyId) return 73;
            if (!ChatSessionStore.List().Any(x => x.Id == first && x.CreateCloudThread)) return 84;
            window._input.Text = "DRAFT_ONE";
            PersistMessage("FIRST_CHAT_MESSAGE", Array.Empty<PendingAttachment>(), first);
            window.RefreshMessages();
            if (!window._history.Text.Contains("FIRST_CHAT_MESSAGE") ||
                window._history.Text.Contains("LEGACY_MESSAGE")) return 74;
            window.CreateChat();
            var second = window._activeChatId;
            if (first == second) return 75;
            if (!ChatSessionStore.List().Any(x => x.Id == second && x.CreateCloudThread)) return 85;
            PersistMessage("SECOND_CHAT_MESSAGE", Array.Empty<PendingAttachment>(), second);
            window.RefreshMessages();
            if (window._history.Text.Contains("FIRST_CHAT_MESSAGE") ||
                !window._history.Text.Contains("SECOND_CHAT_MESSAGE")) return 76;
            window.SwitchChat(first);
            if (window._input.Text != "DRAFT_ONE" ||
                !window._history.Text.Contains("FIRST_CHAT_MESSAGE") ||
                window._history.Text.Contains("SECOND_CHAT_MESSAGE")) return 77;
            window.SwitchChat(ChatSessionStore.LegacyId);
            if (!window._history.Text.Contains("LEGACY_REPLY")) return 78;
            if (File.ReadAllText(oldPath) != oldBytes) return 79;
            if (ChatSessionStore.List().Count != 3) return 80;
            if (ChatSessionStore.List().All(s => s.Id != first || !s.Title.Contains("FIRST"))) return 81;
            window.SwitchChat(second);
            using var reopened = new ChatWindow();
            if (reopened._activeChatId != second) return 82;
            if (Directory.EnumerateFiles(messages, "*.json").Count() != 3) return 83;
            var cloudUrl = "https://chatgpt.com/c/01234567-89ab-cdef-0123-456789abcdef";
            var imported = ChatSessionStore.ImportCloud(cloudUrl, "Исторический чат");
            if (ChatSessionStore.ImportCloud(cloudUrl, "Другой заголовок") != imported) return 87;
            var cloud = ChatSessionStore.List().Single(s => s.Id == imported);
            if (cloud.CreateCloudThread || cloud.CloudUrl != cloudUrl ||
                cloud.Title != "Исторический чат") return 88;
            if (!File.Exists(Path.Combine(root, "browser-threads", imported + ".json"))) return 89;
            return 0;
        }
        finally
        {
            Environment.SetEnvironmentVariable("CHATGPT_MCP_CHAT_TEST_ROOT", previous);
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    // Isolated GUI and attachment persistence test. Does not touch the real queue or clipboard.
    internal static int SelfTestAttachments()
    {
        var previous = Environment.GetEnvironmentVariable("CHATGPT_MCP_CHAT_TEST_ROOT");
        var root = Path.Combine(Path.GetTempPath(), "mcp-chat-attachments-" + Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable("CHATGPT_MCP_CHAT_TEST_ROOT", root);
        try
        {
            var original = Path.Combine(root, "sample.txt");
            Directory.CreateDirectory(root);
            File.WriteAllText(original, "hello attachment", Encoding.UTF8);
            using var bitmap = new Bitmap(30, 20);
            using var memory = new MemoryStream();
            bitmap.Save(memory, ImageFormat.Png);
            using var window = new ChatWindow();
            if (window.Handle == IntPtr.Zero) return 40;
            window.AddFiles([original]);
            window._pending.Add(new PendingAttachment
            {
                Name = "clipboard.png", ImageBytes = memory.ToArray(),
                SizeBytes = memory.Length, IsImage = true
            });
            window.UpdateAttachmentStrip();
            if (window._pending.Count != 2 || window._attachmentStrip.Controls.Count != 2) return 41;
            PersistMessage("", window._pending.ToArray(), ChatSessionStore.Active());
            var messages = Directory.GetFiles(ChatFolder, "*.json");
            if (messages.Length != 1) return 42;
            using var doc = JsonDocument.Parse(File.ReadAllText(messages[0]));
            var id = doc.RootElement.GetProperty("Id").GetString()!;
            var att = doc.RootElement.GetProperty("Attachments");
            if (att.GetArrayLength() != 2) return 43;
            foreach (var item in att.EnumerateArray())
            {
                var attachmentId = item.GetProperty("Id").GetString()!;
                var stored = Path.Combine(AttachmentFolder, id, attachmentId);
                if (!File.Exists(stored)) return 44;
                using var file = File.OpenRead(stored);
                var sha = Convert.ToHexString(SHA256.HashData(file));
                if (!sha.Equals(item.GetProperty("Sha256").GetString(), StringComparison.OrdinalIgnoreCase))
                    return 45;
            }
            return 0;
        }
        finally
        {
            Environment.SetEnvironmentVariable("CHATGPT_MCP_CHAT_TEST_ROOT", previous);
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private sealed class PendingAttachment
    {
        public string Name { get; init; } = "";
        public string? Path { get; init; }
        public byte[]? ImageBytes { get; init; }
        public long SizeBytes { get; init; }
        public bool IsImage { get; init; }
    }
    private sealed class ChatAttachment
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string MediaType { get; set; } = "";
        public long SizeBytes { get; set; }
        public string Sha256 { get; set; } = "";
    }
    private sealed class ChatRecord
    {
        public string Id { get; set; } = "";
        public string? ChatId { get; set; }
        public DateTimeOffset CreatedUtc { get; set; }
        public string Text { get; set; } = "";
        public string Source { get; set; } = "user";
        public string Status { get; set; } = "pending";
        public string? Reply { get; set; }
        public DateTimeOffset? RepliedUtc { get; set; }
        public List<ChatAttachment> Attachments { get; set; } = new();
    }
}
