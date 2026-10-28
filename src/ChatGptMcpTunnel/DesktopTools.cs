using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

[SupportedOSPlatform("windows")]
[McpServerToolType]
public static class DesktopTools
{
    private const int SwRestore = 9;
    private const int SwpNoZOrder = 0x0004;
    private const int SwpNoActivate = 0x0010;
    private const uint WmClose = 0x0010;
    private const uint InputMouse = 0;
    private const uint InputKeyboard = 1;
    private const uint MouseLeftDown = 0x0002;
    private const uint MouseLeftUp = 0x0004;
    private const uint MouseRightDown = 0x0008;
    private const uint MouseRightUp = 0x0010;
    private const uint MouseMiddleDown = 0x0020;
    private const uint MouseMiddleUp = 0x0040;
    private const uint MouseWheel = 0x0800;
    private const uint KeyUp = 0x0002;
    private const uint KeyUnicode = 0x0004;
    private const int SmXVirtualScreen = 76;
    private const int SmYVirtualScreen = 77;
    private const int SmCxVirtualScreen = 78;
    private const int SmCyVirtualScreen = 79;

    private static string AsJson(object value) => JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });

    static DesktopTools()
    {
        if (OperatingSystem.IsWindows())
        {
            // The pixel coordinates returned by tools should match physical screen pixels.
            try { SetProcessDpiAwarenessContext(new IntPtr(-4)); } catch (EntryPointNotFoundException) { }
        }
    }

    [McpServerTool(Name = "desktop.screen.info")]
    [Description("Get Windows desktop bounds in physical pixels, virtual desktop dimensions, mouse position and active window handle.")]
    public static string ScreenInfo()
    {
        RequireWindows();
        if (!GetCursorPos(out var cursor)) throw NativeError("GetCursorPos");
        var bounds = ScreenBounds();
        return AsJson(new
        {
            virtual_screen = new { bounds.x, bounds.y, bounds.width, bounds.height },
            primary_screen = new { width = GetSystemMetrics(0), height = GetSystemMetrics(1) },
            cursor = new { x = cursor.X, y = cursor.Y },
            foreground_hwnd = HandleToString(GetForegroundWindow()),
            session_id = Process.GetCurrentProcess().SessionId
        });
    }

    [McpServerTool(Name = "desktop.screen.capture")]
    [Description("Capture a real Windows desktop screenshot as a PNG image. Coordinates are physical pixels. By default includes all monitors and scales image to max_width=1920.")]
    public static IEnumerable<ContentBlock> ScreenCapture(
        [Description("Optional left x of the region; omit for the full virtual desktop.")] int? x = null,
        [Description("Optional top y of the region; omit for the full virtual desktop.")] int? y = null,
        [Description("Optional region width in pixels.")] int? width = null,
        [Description("Optional region height in pixels.")] int? height = null,
        [Description("Maximum returned image width in pixels, 320..4096 (default 1920).")] int max_width = 1920)
    {
        RequireWindows();
        if (max_width < 320 || max_width > 4096) throw new ArgumentOutOfRangeException(nameof(max_width));
        var screen = ScreenBounds();
        if ((x.HasValue || y.HasValue || width.HasValue || height.HasValue) &&
            !(x.HasValue && y.HasValue && width.HasValue && height.HasValue))
            throw new ArgumentException("Supply all four of x, y, width and height, or none.");
        var sx = x ?? screen.x;
        var sy = y ?? screen.y;
        var sw = width ?? screen.width;
        var sh = height ?? screen.height;
        if (sw <= 0 || sh <= 0 || sw > 16384 || sh > 16384 ||
            (long)sw * sh > 40_000_000L ||
            sx < screen.x || sy < screen.y ||
            (long)sx + sw > (long)screen.x + screen.width ||
            (long)sy + sh > (long)screen.y + screen.height)
            throw new ArgumentOutOfRangeException(nameof(width), "Screenshot rectangle must fit within the desktop and at most 40 MP.");

        using var bitmap = new Bitmap(sw, sh, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(bitmap))
            g.CopyFromScreen(sx, sy, 0, 0, new Size(sw, sh), CopyPixelOperation.SourceCopy);

        var outputWidth = Math.Min(sw, max_width);
        var outputHeight = (int)Math.Max(1, Math.Round((double)sh * outputWidth / sw));
        using var output = new Bitmap(outputWidth, outputHeight, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(output))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(bitmap, new Rectangle(0, 0, outputWidth, outputHeight));
        }
        using var memory = new MemoryStream();
        output.Save(memory, ImageFormat.Png);
        return
        [
            new TextContentBlock
            {
                Text = AsJson(new
                {
                    source_region = new { x = sx, y = sy, width = sw, height = sh },
                    image_width = outputWidth, image_height = outputHeight,
                    note = "Use source_region to convert displayed image coordinates back to physical screen pixels."
                })
            },
            ImageContentBlock.FromBytes(memory.ToArray(), "image/png")
        ];
    }

    [McpServerTool(Name = "desktop.window.list")]
    [Description("List visible top-level Windows application windows with titles, handles, PIDs, screen coordinates and foreground status.")]
    public static string WindowList(
        [Description("Optional case-insensitive substring to filter window titles.")] string? title_filter = null,
        [Description("Include visible windows without titles (default false).")] bool include_untitled = false)
    {
        RequireWindows();
        var active = GetForegroundWindow();
        var windows = new List<object>();
        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd)) return true;
            var length = GetWindowTextLengthW(hwnd);
            if (length == 0 && !include_untitled) return true;
            var titleBuilder = new StringBuilder(Math.Min(length + 1, 32768));
            GetWindowTextW(hwnd, titleBuilder, titleBuilder.Capacity);
            var title = titleBuilder.ToString();
            if (!string.IsNullOrWhiteSpace(title_filter) &&
                !title.Contains(title_filter, StringComparison.OrdinalIgnoreCase)) return true;
            if (!GetWindowRect(hwnd, out var rectangle)) return true;
            GetWindowThreadProcessId(hwnd, out var pid);
            string? processName = null;
            try { using var p = Process.GetProcessById((int)pid); processName = p.ProcessName; } catch { }
            windows.Add(new
            {
                hwnd = HandleToString(hwnd), title, pid, process = processName,
                foreground = hwnd == active, minimized = IsIconic(hwnd),
                bounds = new
                {
                    x = rectangle.Left, y = rectangle.Top,
                    width = rectangle.Right - rectangle.Left,
                    height = rectangle.Bottom - rectangle.Top
                }
            });
            return true;
        }, IntPtr.Zero);
        return AsJson(new { count = windows.Count, windows });
    }

    [McpServerTool(Name = "desktop.window.activate")]
    [Description("Restore and bring a Windows application window to the foreground using a handle from desktop.window.list.")]
    public static string WindowActivate([Description("Window handle as hex (0x...) or decimal string.")] string hwnd)
    {
        RequireWindows();
        var window = ParseWindow(hwnd);
        if (IsIconic(window)) ShowWindowAsync(window, SwRestore);
        BringWindowToTop(window);
        var requested = SetForegroundWindow(window);
        Thread.Sleep(160);
        return AsJson(new
        {
            hwnd = HandleToString(window),
            foreground = GetForegroundWindow() == window,
            set_foreground_returned = requested
        });
    }

    [McpServerTool(Name = "desktop.window.move")]
    [Description("Move or resize an application window to exact screen pixel coordinates.")]
    public static string WindowMove(
        [Description("Handle from desktop.window.list.")] string hwnd,
        [Description("New left position, physical pixels.")] int x,
        [Description("New top position, physical pixels.")] int y,
        [Description("New window width in pixels.")] int width,
        [Description("New window height in pixels.")] int height)
    {
        RequireWindows();
        if (width < 50 || height < 50 || width > 20000 || height > 20000)
            throw new ArgumentOutOfRangeException(nameof(width), "Window size must be between 50 and 20000 pixels.");
        var window = ParseWindow(hwnd);
        if (!SetWindowPos(window, IntPtr.Zero, x, y, width, height, SwpNoZOrder | SwpNoActivate))
            throw NativeError("SetWindowPos");
        if (!GetWindowRect(window, out var rectangle)) throw NativeError("GetWindowRect");
        return AsJson(new
        {
            hwnd = HandleToString(window),
            bounds = new { x = rectangle.Left, y = rectangle.Top,
                width = rectangle.Right - rectangle.Left, height = rectangle.Bottom - rectangle.Top }
        });
    }

    [McpServerTool(Name = "desktop.window.close")]
    [Description("Request graceful closing of a Windows application window. An application may display an unsaved-changes prompt.")]
    public static string WindowClose([Description("Handle from desktop.window.list.")] string hwnd)
    {
        RequireWindows();
        var window = ParseWindow(hwnd);
        if (!PostMessageW(window, WmClose, IntPtr.Zero, IntPtr.Zero))
            throw NativeError("PostMessage(WM_CLOSE)");
        return AsJson(new { hwnd = HandleToString(window), close_requested = true,
            note = "A program can refuse closing or ask to save; this tool does not force termination." });
    }

    [McpServerTool(Name = "desktop.mouse.move")]
    [Description("Move the Windows pointer to exact physical screen coordinates without clicking.")]
    public static string MouseMove([Description("Absolute x coordinate.")] int x,
        [Description("Absolute y coordinate.")] int y)
    {
        RequireWindows();
        CheckCoordinates(x, y);
        if (!SetCursorPos(x, y)) throw NativeError("SetCursorPos");
        return AsJson(new { x, y, moved = true });
    }

    [McpServerTool(Name = "desktop.mouse.click")]
    [Description("Click at a specified physical screen coordinate with the left, right or middle mouse button; optionally double-click.")]
    public static string MouseClick([Description("Absolute x coordinate.")] int x,
        [Description("Absolute y coordinate.")] int y,
        [Description("left, right, or middle (default left).")] string button = "left",
        [Description("1 or 2 clicks (default 1).")] int clicks = 1)
    {
        RequireWindows();
        CheckCoordinates(x, y);
        if (clicks is not (1 or 2)) throw new ArgumentOutOfRangeException(nameof(clicks));
        var (down, up) = button.ToLowerInvariant() switch
        {
            "left" => (MouseLeftDown, MouseLeftUp),
            "right" => (MouseRightDown, MouseRightUp),
            "middle" => (MouseMiddleDown, MouseMiddleUp),
            _ => throw new ArgumentException("button must be left, right or middle")
        };
        if (!SetCursorPos(x, y)) throw NativeError("SetCursorPos");
        var inputs = new List<Input>();
        for (var i = 0; i < clicks; i++)
        {
            inputs.Add(MouseInputWith(down, 0));
            inputs.Add(MouseInputWith(up, 0));
        }
        SubmitInput(inputs.ToArray());
        return AsJson(new { x, y, button, clicks });
    }

    [McpServerTool(Name = "desktop.mouse.scroll")]
    [Description("Scroll vertically with the Windows mouse wheel; positive notches go up, negative go down.")]
    public static string MouseScroll([Description("Wheel notches, positive=up, negative=down; range -100..100.")] int notches,
        [Description("Optional x coordinate of the target.")] int? x = null,
        [Description("Optional y coordinate of the target.")] int? y = null)
    {
        RequireWindows();
        if (notches == 0 || notches < -100 || notches > 100) throw new ArgumentOutOfRangeException(nameof(notches));
        if (x.HasValue != y.HasValue) throw new ArgumentException("Supply both x and y, or neither.");
        if (x.HasValue && y.HasValue)
        {
            CheckCoordinates(x.Value, y.Value);
            if (!SetCursorPos(x.Value, y.Value)) throw NativeError("SetCursorPos");
        }
        SubmitInput([MouseInputWith(MouseWheel, unchecked((uint)(notches * 120)))]);
        return AsJson(new { notches, x, y, scrolled = true });
    }

    [McpServerTool(Name = "desktop.keyboard.type")]
    [Description("Type arbitrary Unicode text into the current foreground Windows application without using the clipboard.")]
    public static string KeyboardType([Description("Exact Unicode text to type.")] string text)
    {
        RequireWindows();
        if (text.Length > 20000) throw new ArgumentOutOfRangeException(nameof(text), "Maximum 20000 UTF-16 units.");
        // KEYEVENTF_UNICODE sends UTF-16 units, including surrogate pairs.
        foreach (var chunk in text.Chunk(100))
        {
            var inputs = new List<Input>(chunk.Length * 2);
            foreach (var c in chunk)
            {
                inputs.Add(KeyInputWith(0, c, KeyUnicode));
                inputs.Add(KeyInputWith(0, c, KeyUnicode | KeyUp));
            }
            SubmitInput(inputs.ToArray());
        }
        return AsJson(new { characters = text.Length, typed = true, foreground_hwnd = HandleToString(GetForegroundWindow()) });
    }

    [McpServerTool(Name = "desktop.keyboard.hotkey")]
    [Description("Send a keyboard shortcut to the foreground application: e.g. CTRL+S, ALT+TAB, CTRL+SHIFT+ESC or ENTER.")]
    public static string KeyboardHotkey([Description("Hotkey string separated by +: CTRL+S, ALT+F4, ENTER, etc.")] string keys)
    {
        RequireWindows();
        var tokens = keys.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length is < 1 or > 6) throw new ArgumentException("Shortcut must have 1..6 keys.");
        var presses = tokens.Select(VirtualKey).ToArray();
        var inputs = new List<Input>(presses.Length * 2);
        foreach (var key in presses) inputs.Add(KeyInputWith(key, 0, 0));
        for (var i = presses.Length - 1; i >= 0; i--) inputs.Add(KeyInputWith(presses[i], 0, KeyUp));
        SubmitInput(inputs.ToArray());
        return AsJson(new { keys, sent = true, foreground_hwnd = HandleToString(GetForegroundWindow()) });
    }

    private static ushort VirtualKey(string key)
    {
        var normalized = key.ToUpperInvariant();
        return normalized switch
        {
            "CTRL" or "CONTROL" => 0x11,
            "SHIFT" => 0x10,
            "ALT" => 0x12,
            "WIN" or "WINDOWS" => 0x5B,
            "ENTER" or "RETURN" => 0x0D,
            "TAB" => 0x09,
            "ESC" or "ESCAPE" => 0x1B,
            "BACKSPACE" => 0x08,
            "DELETE" or "DEL" => 0x2E,
            "SPACE" => 0x20,
            "UP" => 0x26,
            "DOWN" => 0x28,
            "LEFT" => 0x25,
            "RIGHT" => 0x27,
            "HOME" => 0x24,
            "END" => 0x23,
            "PAGEUP" or "PGUP" => 0x21,
            "PAGEDOWN" or "PGDN" => 0x22,
            "INSERT" or "INS" => 0x2D,
            "F1" => 0x70, "F2" => 0x71, "F3" => 0x72, "F4" => 0x73,
            "F5" => 0x74, "F6" => 0x75, "F7" => 0x76, "F8" => 0x77,
            "F9" => 0x78, "F10" => 0x79, "F11" => 0x7A, "F12" => 0x7B,
            "F13" => 0x7C, "F14" => 0x7D, "F15" => 0x7E, "F16" => 0x7F,
            "F17" => 0x80, "F18" => 0x81, "F19" => 0x82, "F20" => 0x83,
            "F21" => 0x84, "F22" => 0x85, "F23" => 0x86, "F24" => 0x87,
            _ when normalized.Length == 1 &&
                   ((normalized[0] >= 'A' && normalized[0] <= 'Z') ||
                    (normalized[0] >= '0' && normalized[0] <= '9'))
                => normalized[0],
            _ => throw new ArgumentException($"Unknown hotkey: {key}")
        };
    }

    private static void RequireWindows()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Desktop tools require an interactive Windows desktop session.");
    }

    private static (int x, int y, int width, int height) ScreenBounds() =>
        (GetSystemMetrics(SmXVirtualScreen), GetSystemMetrics(SmYVirtualScreen),
         GetSystemMetrics(SmCxVirtualScreen), GetSystemMetrics(SmCyVirtualScreen));

    private static void CheckCoordinates(int x, int y)
    {
        var b = ScreenBounds();
        if (x < b.x || y < b.y || (long)x >= (long)b.x + b.width ||
            (long)y >= (long)b.y + b.height)
            throw new ArgumentOutOfRangeException(nameof(x), "Mouse coordinates outside the virtual desktop.");
    }

    private static IntPtr ParseWindow(string handle)
    {
        var value = handle.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? long.Parse(handle[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture)
            : long.Parse(handle, CultureInfo.InvariantCulture);
        var pointer = new IntPtr(value);
        if (pointer == IntPtr.Zero || !IsWindow(pointer))
            throw new ArgumentException("Window handle is invalid or window has closed.");
        return pointer;
    }

    private static string HandleToString(IntPtr handle) =>
        "0x" + handle.ToInt64().ToString("X", CultureInfo.InvariantCulture);

    private static Input MouseInputWith(uint flags, uint data) =>
        new() { Type = InputMouse, Value = new InputUnion
        {
            Mouse = new MouseInputData { Flags = flags, MouseData = data }
        } };

    private static Input KeyInputWith(ushort vk, ushort scan, uint flags) =>
        new() { Type = InputKeyboard, Value = new InputUnion
        {
            Keyboard = new KeyboardInputData { VirtualKey = vk, Scan = scan, Flags = flags }
        } };

    private static void SubmitInput(Input[] inputs)
    {
        if (inputs.Length == 0) return;
        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
        if (sent != inputs.Length) throw NativeError("SendInput");
    }

    private static Win32Exception NativeError(string function) =>
        new(Marshal.GetLastWin32Error(), function + " failed");

    [StructLayout(LayoutKind.Sequential)]
    private struct Point { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int Left; public int Top; public int Right; public int Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input { public uint Type; public InputUnion Value; }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MouseInputData Mouse;
        [FieldOffset(0)] public KeyboardInputData Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInputData
    {
        public int Dx, Dy;
        public uint MouseData, Flags, Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInputData
    {
        public ushort VirtualKey, Scan;
        public uint Flags, Time;
        public IntPtr ExtraInfo;
    }

    private delegate bool EnumWindowsCallback(IntPtr hwnd, IntPtr param);

    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll", SetLastError = true)] private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr param);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetWindowTextW")]
    private static extern int GetWindowTextW(IntPtr hwnd, StringBuilder text, int max);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetWindowTextLengthW")]
    private static extern int GetWindowTextLengthW(IntPtr hwnd);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
    [DllImport("user32.dll")] private static extern bool ShowWindowAsync(IntPtr hwnd, int command);
    [DllImport("user32.dll")] private static extern bool BringWindowToTop(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowPos(
        IntPtr hwnd, IntPtr after, int x, int y, int width, int height, int flags);
    [DllImport("user32.dll", SetLastError = true, EntryPoint = "PostMessageW")]
    private static extern bool PostMessageW(IntPtr hwnd, uint message, IntPtr wp, IntPtr lp);
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(
        uint count, [In] Input[] inputs, int size);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetProcessDpiAwarenessContext(IntPtr context);
}
