using System.Drawing;

namespace ChatGptMcpAgent;

/// <summary>Temporary secret entry only. Never prefill, reveal or copy a stored key.</summary>
internal sealed class SecretInputDialog : Form
{
    private readonly TextBox _entry = new()
    {
        UseSystemPasswordChar = true,
        ShortcutsEnabled = false,
        MaxLength = 4096,
        Width = 400
    };

    internal string EnteredValue { get; private set; } = "";

    internal SecretInputDialog(string description)
    {
        Text = "Указать значение";
        Icon = TrayIcons.AppIcon();
        StartPosition = FormStartPosition.CenterParent;
        Shown += (_, _) => AppTheme.Apply(this);
        ClientSize = new Size(450, 164);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Font = new Font("Segoe UI", 10);
        Controls.Add(new Label
        {
            Text = description, Left = 18, Top = 17, Width = 412, Height = 46
        });
        _entry.SetBounds(18, 72, 410, 28);
        // Input and paste are allowed. Copy/cut, reveal, and secret export
        // are not exposed, especially on client machines.
        _entry.KeyDown += (_, e) =>
        {
            if (!e.Control) return;
            if (e.KeyCode == Keys.A)
            {
                _entry.SelectAll();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.V)
            {
                if (Clipboard.ContainsText()) _entry.SelectedText = Clipboard.GetText();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode is Keys.C or Keys.X or Keys.Insert)
                e.SuppressKeyPress = true;
        };
        var pasteMenu = new ContextMenuStrip();
        pasteMenu.Items.Add("Вставить", null, (_, _) =>
        {
            if (Clipboard.ContainsText()) _entry.SelectedText = Clipboard.GetText();
        });
        _entry.ContextMenuStrip = pasteMenu;
        Controls.Add(_entry);
        var ok = new Button { Text = "Применить", Left = 211, Top = 116, Width = 110, Height = 31 };
        var cancel = new Button { Text = "Отмена", Left = 329, Top = 116, Width = 100, Height = 31 };
        ok.Click += (_, _) =>
        {
            if (_entry.Text.Length < 16)
            {
                MessageBox.Show(this, "Ключ должен содержать не менее 16 символов.",
                    "Проверка значения", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            EnteredValue = _entry.Text;
            _entry.Clear();
            DialogResult = DialogResult.OK;
            Close();
        };
        cancel.Click += (_, _) => { _entry.Clear(); DialogResult = DialogResult.Cancel; Close(); };
        Controls.Add(ok);
        Controls.Add(cancel);
        AcceptButton = ok;
        CancelButton = cancel;
        Shown += (_, _) => _entry.Focus();
    }
}
