using System.Text.Json;

internal sealed class TransferProgressWindow : Form
{
    private readonly ListView _list = new()
    {
        Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true,
        GridLines = true, HideSelection = false, MultiSelect = false
    };
    private readonly ProgressBar _progress = new() { Dock = DockStyle.Top, Height = 22 };
    private readonly Label _details = new() { Dock = DockStyle.Top, Height = 43, Text = "Нет передач файлов." };
    private readonly Button _cancel = new() { Text = "Прервать выбранную передачу", AutoSize = true };
    private readonly System.Windows.Forms.Timer _refresh = new() { Interval = 900 };
    private readonly string _directory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ChatGptMcpTunnel", "Transfers");
    private readonly Dictionary<string, JsonElement> _jobs = new(StringComparer.OrdinalIgnoreCase);

    public TransferProgressWindow()
    {
        Text = "ChatGPT MCP Tunnel — Передача файлов";
        MinimumSize = new Size(680, 330);
        Size = new Size(950, 420);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);
        _list.Columns.Add("Файл", 185);
        _list.Columns.Add("Откуда", 115);
        _list.Columns.Add("Куда", 115);
        _list.Columns.Add("Выполнено", 90);
        _list.Columns.Add("Скорость", 105);
        _list.Columns.Add("Состояние", 140);
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 108, Padding = new Padding(12) };
        _cancel.Dock = DockStyle.Right;
        _cancel.Width = 230;
        _cancel.Click += (_, _) => CancelSelected();
        _list.SelectedIndexChanged += (_, _) => ShowSelected();
        footer.Controls.Add(_cancel);
        footer.Controls.Add(_details);
        footer.Controls.Add(_progress);
        Controls.Add(_list);
        Controls.Add(footer);
        _refresh.Tick += (_, _) => RefreshJobs();
        _refresh.Start();
        RefreshJobs();
    }

    private static string String(JsonElement job, string name) =>
        job.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? "" : "";
    private static long Number(JsonElement job, string name) =>
        job.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt64() : 0;
    private static string Bytes(double count)
    {
        if (count < 1024) return $"{count:F0} Б";
        if (count < 1048576) return $"{count / 1024:F1} КиБ";
        if (count < 1073741824) return $"{count / 1048576:F1} МиБ";
        return $"{count / 1073741824:F2} ГиБ";
    }
    private static string State(string value) => value switch
    {
        "queued" => "В очереди",
        "preparing" => "Подготовка",
        "transferring" => "Передача",
        "reconnecting" => "Переподключение",
        "verifying" => "Проверка SHA-256",
        "completed" => "Завершено",
        "failed" => "Ошибка",
        "cancelled" => "Прервано",
        _ => value
    };

    private void RefreshJobs()
    {
        var file = Path.Combine(_directory, "state.json");
        if (!File.Exists(file)) { ShowSelected(); return; }
        JsonElement[] values;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return;
            values = doc.RootElement.EnumerateArray().Select(item => item.Clone()).ToArray();
        }
        catch (IOException) { return; }
        catch (JsonException) { return; }

        var selected = _list.SelectedItems.Count == 1 ? _list.SelectedItems[0].Name : null;
        _jobs.Clear();
        _list.BeginUpdate();
        try
        {
            _list.Items.Clear();
            foreach (var job in values)
            {
                var id = String(job, "id");
                if (id.Length == 0) continue;
                _jobs[id] = job;
                var total = Number(job, "total_bytes");
                var done = Number(job, "transferred_bytes");
                var rate = Number(job, "bytes_per_second");
                var row = new ListViewItem(Path.GetFileName(String(job, "source_path"))) { Name = id };
                row.SubItems.Add(String(job, "source_computer"));
                row.SubItems.Add(String(job, "destination_computer"));
                row.SubItems.Add(total > 0 ? $"{100.0 * done / total:F1} %" : "0 %");
                row.SubItems.Add(rate > 0 ? $"{Bytes(rate)}/с" : "—");
                row.SubItems.Add(State(String(job, "status")));
                _list.Items.Add(row);
                if (id == selected) row.Selected = true;
            }
            if (_list.SelectedItems.Count == 0 && _list.Items.Count > 0)
                _list.Items[0].Selected = true;
        }
        finally { _list.EndUpdate(); }
        ShowSelected();
    }

    private void ShowSelected()
    {
        if (_list.SelectedItems.Count == 0 ||
            !_jobs.TryGetValue(_list.SelectedItems[0].Name, out var job))
        {
            _details.Text = "Нет выбранной передачи.";
            _progress.Value = 0;
            _cancel.Enabled = false;
            return;
        }
        var total = Number(job, "total_bytes");
        var done = Number(job, "transferred_bytes");
        var rate = Number(job, "bytes_per_second");
        var eta = job.TryGetProperty("eta_seconds", out var e) && e.ValueKind == JsonValueKind.Number
            ? TimeSpan.FromSeconds(e.GetInt64()).ToString(@"hh\:mm\:ss") : "—";
        var state = String(job, "status");
        _progress.Value = total > 0 ? Math.Clamp((int)(100.0 * done / total), 0, 100)
            : state == "completed" ? 100 : 0;
        _details.Text = $"{Bytes(done)} / {Bytes(total)} · {State(state)} · {Bytes(rate)}/с · Осталось: {eta}"
            + Environment.NewLine + $"{String(job, "source_path")} → {String(job, "destination_path")}"
            + (state == "failed" ? Environment.NewLine + String(job, "error") : "");
        _cancel.Enabled = state is "queued" or "preparing" or "transferring" or "reconnecting" or "verifying";
    }

    private void CancelSelected()
    {
        if (_list.SelectedItems.Count == 0) return;
        var id = _list.SelectedItems[0].Name;
        if (id.Length != 32 || !id.All(Uri.IsHexDigit)) return;
        if (MessageBox.Show(this, "Прервать передачу? Уже переданные данные сохранятся для продолжения.",
                Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        var folder = Path.Combine(_directory, "Cancel");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, id + ".cancel"), "");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _refresh.Dispose();
        base.Dispose(disposing);
    }
}
