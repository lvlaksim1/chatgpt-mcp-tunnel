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
        [Description("Maximum controls to return: 1..500, default 150.")] int max_results = 150,
        [Description("Traversal view: control (default) or raw. RawView includes otherwise hidden provider nodes.")] string view = "control",
        [Description("Target computer from computers.list; omitted means local.")] string? computer = null,
        [Description("Display-only executor identity: project-manager, supervisor, chatgpt, or persona:Name. Defaults to ChatGPT; never used for authorization.")] string? executor = null) => RouterLite.IsRemote(computer) ? RouterLite.CallStringAsync(computer!, "desktop.uia.tree", new { hwnd, max_depth, max_results, view, executor }) :
        Run(new { operation = "tree", hwnd, max_depth, max_results, view, executor });

    [McpServerTool(Name = "desktop.uia.find", Title = "Мультимедиа · Поиск элементов интерфейса")]
    [Description("Категория «Мультимедиа»: find UI elements inside a selected window by accessible name, AutomationId, control type (Button, Edit, CheckBox, etc.) or class. Returns stable-ish index paths to use in desktop.uia.get and desktop.uia.action. Paths may change when the UI changes.")]
    public static Task<string> Find(
        [Description("Window handle from desktop.window.list.")] string hwnd,
        [Description("Case-insensitive substring of accessible element name.")] string? name = null,
        [Description("Exact UI Automation AutomationId.")] string? automation_id = null,
        [Description("UI Automation type such as Button, Edit, Text, CheckBox, ComboBox, ListItem.")] string? control_type = null,
        [Description("Exact Win32 class name.")] string? class_name = null,
        [Description("Maximum nesting depth (0..20, default 10).")] int max_depth = 10,
        [Description("Maximum search results (1..100, default 30).")] int max_results = 30,
        [Description("Traversal view: control or raw, default control.")] string view = "control",
        [Description("Target computer from computers.list; omitted means local.")] string? computer = null,
        [Description("Display-only executor identity: project-manager, supervisor, chatgpt, or persona:Name. Defaults to ChatGPT; never used for authorization.")] string? executor = null) => RouterLite.IsRemote(computer) ? RouterLite.CallStringAsync(computer!, "desktop.uia.find", new { hwnd, name, automation_id, control_type, class_name, max_depth, max_results, view, executor }) :
        Run(new { operation = "find", hwnd, name, automation_id, control_type, class_name, max_depth, max_results, view, executor });

    [McpServerTool(Name = "desktop.uia.get", Title = "Мультимедиа · Свойства и текст элемента")]
    [Description("Категория «Мультимедиа»: read UI Automation properties, supported interaction patterns, values, available text, selection, toggle and expand states for one element identified by a path from desktop.uia.tree or desktop.uia.find.")]
    public static Task<string> Get(
        [Description("Window handle from desktop.window.list.")] string hwnd,
        [Description("Element path from desktop.uia.tree or desktop.uia.find. Empty string means the window itself.")] string path,
        [Description("View used when the path was discovered: control or raw (default control).")] string view = "control",
        [Description("Target computer from computers.list; omitted means local.")] string? computer = null,
        [Description("Display-only executor identity: project-manager, supervisor, chatgpt, or persona:Name. Defaults to ChatGPT; never used for authorization.")] string? executor = null) => RouterLite.IsRemote(computer) ? RouterLite.CallStringAsync(computer!, "desktop.uia.get", new { hwnd, path, view, executor }) :
        Run(new { operation = "get", hwnd, path, view, executor });

    [McpServerTool(Name = "desktop.uia.action", Title = "Мультимедиа · Действие над элементом")]
    [Description("Категория «Мультимедиа»: perform a UI Automation action directly on a window element without mouse coordinates: invoke (press button), set_value (enter text), select, toggle, expand, collapse, focus or scroll_into_view. Use desktop.uia.get to see supported patterns; fall back to existing desktop.mouse and desktop.keyboard for unsupported UI elements.")]
    public static Task<string> Action(
        [Description("Window handle from desktop.window.list.")] string hwnd,
        [Description("Index path to element from desktop.uia.find/tree, empty string for the window itself.")] string path,
        [Description("Action: invoke, set_value, select, toggle, expand, collapse, focus, scroll_into_view.")] string action,
        [Description("Required text only when action=set_value.")] string? value = null,
        [Description("View used to find the element path: control or raw (default control).")] string view = "control",
        [Description("Target computer from computers.list; omitted means local.")] string? computer = null,
        [Description("Display-only executor identity: project-manager, supervisor, chatgpt, or persona:Name. Defaults to ChatGPT; never used for authorization.")] string? executor = null) => RouterLite.IsRemote(computer) ? RouterLite.CallStringAsync(computer!, "desktop.uia.action", new { hwnd, path, action, value, view, executor }) :
        Run(new { operation = "action", hwnd, path, action, value, view, executor });

    private static readonly SemaphoreSlim WorkerGate = new(1, 1);
    private static Process? Worker;

    static UiAutomationTools()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) => KillWorker();
    }

    private static void KillWorker()
    {
        var process = Worker;
        Worker = null;
        if (process is null) return;
        try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
        catch (InvalidOperationException) { }
        finally { process.Dispose(); }
    }

    private static void EnsureWorker(string workerFile)
    {
        if (Worker is { HasExited: false }) return;
        KillWorker();
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
        foreach (var arg in new[] { "-NoProfile", "-NonInteractive", "-STA", "-ExecutionPolicy",
                                    "Bypass", "-File", workerFile })
            info.ArgumentList.Add(arg);
        Worker = new Process { StartInfo = info };
        if (!Worker.Start()) throw new InvalidOperationException("UI Automation worker did not start.");
        // Drain stderr asynchronously so startup errors never block on a full pipe.
        _ = Worker.StandardError.ReadToEndAsync();
    }

    private static async Task<string> Run(object request)
    {
        if (!OperatingSystem.IsWindows())
            return "{\"success\":false,\"error\":\"Windows UI Automation is available only on Windows.\"}";

        var operation = JsonSerializer.SerializeToElement(request, JsonOptions)
            .GetProperty("operation").GetString() ?? "unknown";
        var requestJson = JsonSerializer.SerializeToElement(request, JsonOptions);
        string? actionName = operation == "action" &&
            requestJson.TryGetProperty("action", out var actionJson)
            ? actionJson.GetString() : null;
        string? executor = requestJson.TryGetProperty("executor", out var identityJson)
            && identityJson.ValueKind == JsonValueKind.String ? identityJson.GetString() : null;
        using var control = ControlState.Begin("desktop.uia." + operation,
            guiAction: operation == "action", readOnly: operation != "action",
            activity: actionName, executor: executor);

        var workerFile = Path.Combine(AppContext.BaseDirectory, "AutomationWorker.ps1");
        if (!File.Exists(workerFile))
            return JsonSerializer.Serialize(new { success = false, error = "UI Automation worker missing." });
        await WorkerGate.WaitAsync();
        try
        {
            EnsureWorker(workerFile);
            var active = Worker!;
            // If STOP occurs while UIA is blocking, terminate the worker, not the tray.
            using var stopWatcher = new System.Threading.Timer(_ =>
            {
                if (operation != "action") return;
                try
                {
                    if (ControlState.IsStopped && !active.HasExited)
                        active.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException) { }
            }, null, 150, 150);

            await active.StandardInput.WriteLineAsync(JsonSerializer.Serialize(request, JsonOptions));
            await active.StandardInput.FlushAsync();
            var output = await active.StandardOutput.ReadLineAsync()
                .WaitAsync(TimeSpan.FromSeconds(30));
            if (operation == "action") ControlState.EnsureAllowed();
            if (string.IsNullOrWhiteSpace(output))
                throw new InvalidDataException("UI Automation worker produced no response.");
            return output.Trim();
        }
        catch (Exception ex)
        {
            KillWorker(); // Next call will start a fresh, independent STA worker.
            if (ex is OperationCanceledException) throw;
            return JsonSerializer.Serialize(new { success = false,
                error = ex is TimeoutException ? "UI Automation timed out (30 seconds)" :
                    "UI Automation worker failed: " + ex.GetType().Name });
        }
        finally { WorkerGate.Release(); }
    }
}
