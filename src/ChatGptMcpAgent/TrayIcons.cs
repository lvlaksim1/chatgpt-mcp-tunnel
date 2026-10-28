using System.Drawing;
using System.Runtime.InteropServices;

namespace ChatGptMcpAgent;

/// <summary>Native application icon and small visible connection indicators.</summary>
internal sealed class TrayIcons : IDisposable
{
    private readonly Icon _base;
    private readonly Icon _connected;
    private readonly Icon _connecting;
    private readonly Icon _offline;

    internal TrayIcons()
    {
        _base = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? (Icon)SystemIcons.Application.Clone();
        _connected = WithDot(Color.FromArgb(48, 216, 102));
        _connecting = WithDot(Color.FromArgb(255, 197, 47));
        _offline = WithDot(Color.FromArgb(238, 78, 81));
    }

    internal Icon Base => _base;

    internal Icon ForStatus(string status)
    {
        if (status is "Подключён" or "Сервер подключён")
            return _connected;
        if (status.Contains("Подключение", StringComparison.OrdinalIgnoreCase) ||
            status.Contains("Переподключение", StringComparison.OrdinalIgnoreCase) ||
            status.Contains("Запуск", StringComparison.OrdinalIgnoreCase))
            return _connecting;
        return _offline;
    }

    private Icon WithDot(Color color)
    {
        using var bitmap = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.DrawIcon(_base, new Rectangle(0, 0, 32, 32));
            using var outline = new SolidBrush(Color.FromArgb(5, 18, 40));
            using var fill = new SolidBrush(color);
            graphics.FillEllipse(outline, 19, 19, 13, 13);
            graphics.FillEllipse(fill, 21, 21, 9, 9);
        }
        var handle = bitmap.GetHicon();
        try
        {
            using var borrowed = Icon.FromHandle(handle);
            return (Icon)borrowed.Clone();
        }
        finally { DestroyIcon(handle); }
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    public void Dispose()
    {
        _connected.Dispose();
        _connecting.Dispose();
        _offline.Dispose();
        _base.Dispose();
    }

    internal static Icon AppIcon() => Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        ?? (Icon)SystemIcons.Application.Clone();
}
