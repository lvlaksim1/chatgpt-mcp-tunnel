using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;

[McpServerToolType]
public static class UiAutomationTools
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    [McpServerTool(Name = "desktop.uia.tree", Title = "Мультимедиа · Структура элементов окна")]
    [Description("Категория «Мультимедиа»: inspect the accessible UI Automation control tree of a Windows application window. Start with desktop.window.list to get hwnd; returns elements with index paths, names, roles, automation IDs, coordinates and states.")]
    public static Task<string> Tree(
        [Description("Window handle from desktop.window.list (0x... format).")] string hwnd,
        [Description("Traversal depth: 0..12, default 5.")] int max_depth = 5,
        [Description("Maximum controls to return: 1..500, default 150.")] int max_results = 150) =>
        Run(new { operation = "tree", hwnd, max_depth, max_results });

    [McpServerTool(Name = "desktop.uia.find", Title = "Мультимедиа · Поиск элементов интерфейса")]
    [Description("Категория «Мультимедиа»: find UI elements inside a selected window by accessible name, AutomationId, control type (Button, Edit, CheckBox, etc.) or class. Returns stable-ish index paths to use in desktop.uia.get and desktop.uia.action. Paths may change when the UI changes.")]
    public static Task<string> Find(
        [Description("Window handle from desktop.window.list.")] string hwnd,
        [Description("Case-insensitive substring of accessible element name.")] string? name = null,
        [Description("Exact UI Automation AutomationId.")] string? automation_id = null,
        [Description("UI Automation type such as Button, Edit, Text, CheckBox, ComboBox, ListItem.")] string? control_type = null,
        [Description("Exact Win32 class name.")] string? class_name = null,
        [Description("Maximum nesting depth (0..20, default 10).")] int max_depth = 10,
        [Description("Maximum search results (1..100, default 30).")] int max_results = 30) =>
        Run(new { operation = "find", hwnd, name, automation_id, control_type, class_name, max_depth, max_results });

    [McpServerTool(Name = "desktop.uia.get", Title = "Мультимедиа · Свойства и текст элемента")]
    [Description("Категория «Мультимедиа»: read UI Automation properties, supported interaction patterns, values, available text, selection, toggle and expand states for one element identified by a path from desktop.uia.tree or desktop.uia.find.")]
    public static Task<string> Get(
        [Description("Window handle from desktop.window.list.")] string hwnd,
        [Description("Element path from desktop.uia.tree or desktop.uia.find. Empty string means the window itself.")] string path) =>
        Run(new { operation = "get", hwnd, path });

    [McpServerTool(Name = "desktop.uia.action", Title = "Мультимедиа · Действие над элементом")]
    [Description("Категория «Мультимедиа»: perform a UI Automation action directly on a window element without mouse coordinates: invoke (press button), set_value (enter text), select, toggle, expand, collapse, focus or scroll_into_view. Use desktop.uia.get to see supported patterns; fall back to existing desktop.mouse and desktop.keyboard for unsupported UI elements.")]
    public static Task<string> Action(
        [Description("Window handle from desktop.window.list.")] string hwnd,
        [Description("Index path to element from desktop.uia.find/tree, empty string for the window itself.")] string path,
        [Description("Action: invoke, set_value, select, toggle, expand, collapse, focus, scroll_into_view.")] string action,
        [Description("Required text only when action=set_value.")] string? value = null) =>
        Run(new { operation = "action", hwnd, path, action, value });

    private static async Task<string> Run(object request)
    {
        if (!OperatingSystem.IsWindows())
            return "{\"success\":false,\"error\":\"Windows UI Automation is available only on Windows.\"}";

        var workerFile = Path.Combine(AppContext.BaseDirectory, "AutomationWorker.ps1");
        if (!File.Exists(workerFile))
            return JsonSerializer.Serialize(new { success = false, error = $"Missing UI Automation worker: {workerFile}" });

        var info = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            WorkingDirectory = AppContext.BaseDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = new UTF8Encoding(false),
            StandardErrorEncoding = new UTF8Encoding(false),
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var arg in new[] { "-NoProfile", "-NonInteractive", "-STA", "-ExecutionPolicy", "Bypass", "-File", workerFile })
            info.ArgumentList.Add(arg);

        try
        {
            using var process = new Process { StartInfo = info };
            if (!process.Start()) throw new InvalidOperationException("UI Automation process did not start.");

            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await process.StandardInput.WriteAsync(JsonSerializer.Serialize(request, JsonOptions));
            process.StandardInput.Close();

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException)
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
                return "{\"success\":false,\"error\":\"UI Automation operation timed out (30 seconds).\"}";
            }

            var output = (await stdout).Trim();
            var errors = (await stderr).Trim();
            if (string.IsNullOrWhiteSpace(output))
                return JsonSerializer.Serialize(new { success = false, error = "UI Automation worker returned empty output.", details = errors });
            if (process.ExitCode != 0 && !output.StartsWith("{", StringComparison.Ordinal))
                return JsonSerializer.Serialize(new { success = false, error = "UI Automation worker failed.", details = output + " " + errors });
            return output;
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }
    }
}
