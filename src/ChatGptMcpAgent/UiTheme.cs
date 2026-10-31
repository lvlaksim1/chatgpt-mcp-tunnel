using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace ChatGptMcpAgent;

internal sealed class UiPreferences
{
    public string Theme { get; set; } = "dark";
    public bool ShowControlNotifications { get; set; } = true;
    public static string FilePath => Path.Combine(AgentStorage.DataFolder, "ui-preferences.json");

    internal static UiPreferences Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var settings = JsonSerializer.Deserialize<UiPreferences>(File.ReadAllText(FilePath));
                if (settings?.Theme is "light" or "dark") return settings;
            }
        }
        catch (IOException) { }
        catch (JsonException) { }
        return new UiPreferences();
    }

    internal void Save()
    {
        Directory.CreateDirectory(AgentStorage.DataFolder);
        var target = FilePath + ".tmp";
        File.WriteAllText(target, JsonSerializer.Serialize(this,
            new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
        File.Move(target, FilePath, overwrite: true);
        AppTheme.ApplyAll();
        foreach (var main in Application.OpenForms.OfType<ChatGptMainForm>())
            main.ApplyBrowserAppearance();
    }
}

// One palette for all application-owned WinForms windows, including notifications,
// login, imported-chat, settings, updater and popups.
internal static class AppTheme
{
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hWnd, int attribute, ref int value, int size);
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmFlush();
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y,
        int width, int height, uint flags);
    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    private static extern int SetWindowTheme(IntPtr hWnd, string? subAppName, string? subIdList);

    internal static bool Dark => UiPreferences.Load().Theme != "light";
    internal static Color Background => Dark ? Color.FromArgb(37, 37, 35) : Color.FromArgb(250, 250, 250);
    internal static Color Sidebar => Dark ? Color.FromArgb(37, 37, 35) : Color.FromArgb(241, 241, 241);
    internal static Color Surface => Dark ? Color.FromArgb(48, 48, 45) : Color.White;
    internal static Color Hover => Dark ? Color.FromArgb(57, 57, 52) : Color.FromArgb(230, 230, 230);
    internal static Color Selected => Dark ? Color.FromArgb(57, 57, 52) : Color.FromArgb(223, 229, 238);
    internal static Color Text => Dark ? Color.FromArgb(229, 217, 191) : Color.FromArgb(31, 31, 31);
    internal static Color Secondary => Dark ? Color.FromArgb(189, 179, 158) : Color.FromArgb(98, 98, 98);
    internal static Color Accent => Dark ? Color.FromArgb(201, 213, 239) : Color.FromArgb(36, 95, 148);
    internal static Color Assistant => Dark ? Color.FromArgb(148, 206, 170) : Color.FromArgb(30, 125, 86);
    internal static Color Border => Dark ? Color.FromArgb(80, 80, 73) : Color.FromArgb(216, 216, 216);

    internal static void ApplyAll()
    {
        foreach (Form f in Application.OpenForms.Cast<Form>().ToArray())
            if (!f.IsDisposed) Apply(f);
    }

    internal static void Apply(Form form)
    {
        form.BackColor = Background;
        form.ForeColor = Text;
        if (form.IsHandleCreated) SetNative(form);
        else form.HandleCreated += (_, _) => { if (!form.IsDisposed) SetNative(form); };
        ApplyControls(form);
        // WinForms controls sometimes recreate HWNDs (font and theme changes).
        form.Invalidate(true);
    }

    private static void SetNative(Form form)
    {
        var enable = Dark ? 1 : 0;
        try
        {
            if (DwmSetWindowAttribute(form.Handle, 20, ref enable, sizeof(int)) != 0)
                DwmSetWindowAttribute(form.Handle, 19, ref enable, sizeof(int));
        }
        catch (DllNotFoundException) { }
        // Redraw DWM-managed title bars immediately after the theme switch.
        // Win10 otherwise often waits until the user clicks the caption.
        try
        {
            SetWindowPos(form.Handle, IntPtr.Zero, 0, 0, 0, 0, 0x0027);
            DwmFlush();
        }
        catch (DllNotFoundException) { }
    }

    internal static void ApplyControls(Control container)
    {
        foreach (Control c in container.Controls)
        {
            c.ForeColor = Text;
            if (c is Panel or TableLayoutPanel or FlowLayoutPanel or SplitContainer)
                c.BackColor = c == container ? Background : Surface;
            else if (c is Button button)
            {
                button.BackColor = Surface;
                button.ForeColor = Text;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = Border;
                button.FlatAppearance.MouseOverBackColor = Hover;
                button.FlatAppearance.MouseDownBackColor = Selected;
            }
            else if (c is TextBoxBase or ListBox or ListView or TreeView or ComboBox or DataGridView)
            {
                c.BackColor = Surface;
                c.ForeColor = Text;
            }
            else if (c is Label or CheckBox or RadioButton or GroupBox)
            {
                c.BackColor = container.BackColor;
                c.ForeColor = Text;
            }
            else c.BackColor = Surface;
            if (c.IsHandleCreated) SetScrollbars(c);
            else c.HandleCreated += (_, _) => { if (!c.IsDisposed) SetScrollbars(c); };
            if (c.HasChildren) ApplyControls(c);
        }
    }

    private static void SetScrollbars(Control c)
    {
        if (!Dark)
        {
            try { SetWindowTheme(c.Handle, "Explorer", null); } catch (DllNotFoundException) { }
            return;
        }
        try
        {
            // Explorer dark scrollbars are supported on modern Win10 WebView2
            // runtimes; unsupported builds fall back to the native Windows look.
            SetWindowTheme(c.Handle, "DarkMode_Explorer", null);
        }
        catch (DllNotFoundException) { }
    }

    internal static bool SelfTest()
    {
        var old = Environment.GetEnvironmentVariable("CHATGPT_MCP_CHAT_TEST_ROOT");
        using var window = new Form();
        window.Controls.Add(new TextBox());
        Apply(window);
        return window.BackColor == Background && window.Controls[0].ForeColor == Text;
    }
}
