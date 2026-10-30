using System.Text.Json;
using System.Text.RegularExpressions;

namespace ChatGptMcpAgent;

// Import an existing account conversation by its canonical URL, without cloning the model context.
internal sealed class ChatCloudImportDialog : Form
{
    private static readonly Regex CloudId = new(
        @"^https://chatgpt\.com/c/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private readonly ListBox _known = new() { Dock = DockStyle.Fill };
    private readonly TextBox _url = new() { Dock = DockStyle.Top, PlaceholderText = "https://chatgpt.com/c/..." };
    private readonly TextBox _title = new() { Dock = DockStyle.Top, PlaceholderText = "Название (необязательно)" };
    private readonly Label _hint = new() { Dock = DockStyle.Top, Height = 56, Text = "Выберите найденный диалог или вставьте адрес существующего чата ChatGPT. Контекст остаётся в исходной беседе.", Padding = new Padding(4) };
    private readonly Button _import = new() { Text = "Привязать чат", Dock = DockStyle.Right, Width = 138 };
    private readonly Button _refresh = new() { Text = "Обновить список", Dock = DockStyle.Left, Width = 138 };
    internal string? ImportedChatId { get; private set; }

    private sealed record Candidate(string Title, string Url)
    {
        public override string ToString() => Title;
    }

    internal ChatCloudImportDialog()
    {
        Text = "ChatGPT MCP Tunnel — Импорт диалога";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(680, 510);
        MinimumSize = new Size(500, 330);
        Font = new Font("Segoe UI", 10);
        var buttons = new Panel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(4) };
        buttons.Controls.Add(_import);
        buttons.Controls.Add(_refresh);
        Controls.Add(_known);
        Controls.Add(_title);
        Controls.Add(_url);
        Controls.Add(_hint);
        Controls.Add(buttons);
        _known.SelectedIndexChanged += (_, _) =>
        {
            if (_known.SelectedItem is Candidate c) { _url.Text = c.Url; _title.Text = c.Title; }
        };
        _refresh.Click += (_, _) => LoadCatalog();
        _import.Click += (_, _) =>
        {
            if (!CloudId.IsMatch(_url.Text.Trim()))
            {
                MessageBox.Show(this, "Нужен действительный адрес диалога https://chatgpt.com/c/<UUID>.",
                    "Некорректный адрес", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                ImportedChatId = ChatSessionStore.ImportCloud(_url.Text.Trim(), _title.Text.Trim());
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                MessageBox.Show(this, "Импорт не выполнен: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };
        LoadCatalog();
    }

    private static string Catalog => Path.Combine(AgentStorage.DataFolder, "Chat", "browser-bridge", "catalog.json");

    private void LoadCatalog()
    {
        _known.Items.Clear();
        if (!File.Exists(Catalog)) return;
        try
        {
            using var json = JsonDocument.Parse(File.ReadAllText(Catalog));
            if (!json.RootElement.TryGetProperty("items", out var items) ||
                items.ValueKind != JsonValueKind.Array) return;
            foreach (var item in items.EnumerateArray().Take(200))
            {
                var url = item.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "";
                if (!CloudId.IsMatch(url)) continue;
                var title = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                _known.Items.Add(new Candidate(string.IsNullOrWhiteSpace(title) ? url : title, url));
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException) { }
    }
}
