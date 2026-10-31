using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ChatGptMcpAgent;

/// <summary>
/// Owner-drawn WinForms browser chrome matched to the ChatGPT warm palette.
/// The controls remain normal keyboard-accessible WinForms buttons.
/// </summary>
internal sealed class WarmChromeButton : Button
{
    private bool _hover;
    private bool _down;
    internal int Radius { get; set; } = 11;
    internal bool AccentOnHover { get; set; } = true;

    internal WarmChromeButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        TabStop = true;
        Margin = new Padding(2);
        Font = new Font("Segoe UI", 11f);
        Cursor = Cursors.Hand;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }
    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        _down = false;
        Invalidate();
        base.OnMouseLeave(e);
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left) _down = true;
        Invalidate();
        base.OnMouseDown(e);
    }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        _down = false;
        Invalidate();
        base.OnMouseUp(e);
    }
    protected override void OnEnabledChanged(EventArgs e)
    {
        Invalidate();
        base.OnEnabledChanged(e);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rectangle = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
        using var path = ChromeShapes.Round(rectangle, Radius);
        var background = !Enabled ? BackColor :
            (_down ? AppTheme.Selected : (_hover && AccentOnHover ? AppTheme.Hover : BackColor));
        using var brush = new SolidBrush(background);
        e.Graphics.FillPath(brush, path);
        if (_hover && Enabled)
        {
            using var pen = new Pen(AppTheme.Border, 1f);
            e.Graphics.DrawPath(pen, path);
        }
        var color = !Enabled ? AppTheme.Secondary : ForeColor;
        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, color,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        if (Focused && ShowFocusCues)
        {
            var focus = Rectangle.Inflate(ClientRectangle, -5, -5);
            ControlPaint.DrawFocusRectangle(e.Graphics, focus, color, background);
        }
    }
}

internal static class ChromeShapes
{
    internal static GraphicsPath Round(Rectangle area, int radius)
    {
        var path = new GraphicsPath();
        var d = Math.Max(2, Math.Min(radius * 2, Math.Min(area.Width, area.Height)));
        path.AddArc(area.X, area.Y, d, d, 180, 90);
        path.AddArc(area.Right - d, area.Y, d, d, 270, 90);
        path.AddArc(area.Right - d, area.Bottom - d, d, d, 0, 90);
        path.AddArc(area.X, area.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class WarmChromePanel : Panel
{
    internal int Radius { get; set; } = 12;
    internal bool ShowOutline { get; set; }
    internal WarmChromePanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Margin = Padding.Empty;
    }
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = ChromeShapes.Round(
            new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1)), Radius);
        using var brush = new SolidBrush(BackColor);
        e.Graphics.FillPath(brush, path);
        if (ShowOutline)
        {
            using var pen = new Pen(AppTheme.Border, 1f);
            e.Graphics.DrawPath(pen, path);
        }
    }
}
