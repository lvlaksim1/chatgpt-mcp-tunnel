using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

// Executes a small, explicitly allowlisted sequence locally in ONE MCP roundtrip.
// Each GUI mutation is paced by ControlState (one second), including STOP checks.
[McpServerToolType]
public static class DesktopBatchTools
{
    private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
    {
        "desktop.window.list", "desktop.window.activate", "desktop.window.move",
        "desktop.window.close", "desktop.screen.info",
        "desktop.mouse.move", "desktop.mouse.click", "desktop.mouse.scroll",
        "desktop.keyboard.type", "desktop.keyboard.hotkey",
        "desktop.uia.tree", "desktop.uia.find", "desktop.uia.get", "desktop.uia.action"
    };

    [McpServerTool(Name = "desktop.batch", Title = "Мультимедиа · Серия действий с интерфейсом")]
    [Description("Категория «Мультимедиа»: Execute 1..16 allowlisted Windows GUI steps in one MCP call. Provide steps_json as a JSON array of {tool,arguments}. A UI Automation find may be followed by an action using path=\"$last.path\" to select the first matched element. Exactly one-second pacing between GUI mutations; STOP is checked before each step. Stops on first error. No arbitrary scripts.")]
    public static async Task<string> Batch(
        [Description("JSON array of 1..16 {tool,arguments} steps, max 20000 characters.")] string steps_json,
        [Description("Target computer; omit for local.")] string? computer = null)
    {
        if (RouterLite.IsRemote(computer))
            return await RouterLite.CallStringAsync(computer!, "desktop.batch", new { steps_json });
        using var control = ControlState.Begin("desktop.batch");
        if (steps_json.Length > 20000) throw new ArgumentOutOfRangeException(nameof(steps_json));
        using var doc = JsonDocument.Parse(steps_json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array ||
            doc.RootElement.GetArrayLength() is < 1 or > 16)
            throw new ArgumentException("Batch must contain 1..16 steps.");
        var results = new List<object>();
        string? lastPath = null;
        foreach (var step in doc.RootElement.EnumerateArray())
        {
            ControlState.EnsureAllowed();
            if (step.ValueKind != JsonValueKind.Object ||
                !step.TryGetProperty("tool", out var nameProp) ||
                nameProp.ValueKind != JsonValueKind.String)
                throw new ArgumentException("Invalid batch step.");
            var name = nameProp.GetString() ?? "";
            if (!Allowed.Contains(name))
                throw new ArgumentException("Not an allowlisted GUI operation: " + name);
            if (!step.TryGetProperty("arguments", out var args) || args.ValueKind != JsonValueKind.Object)
                throw new ArgumentException("Each GUI step requires arguments.");
            // Only this explicit placeholder is supported, not arbitrary expressions.
            var fields = new Dictionary<string, object?>();
            foreach (var prop in args.EnumerateObject())
            {
                if (prop.Name == "computer") throw new ArgumentException("Nested computer target forbidden.");
                fields[prop.Name] = prop.Value.ValueKind == JsonValueKind.String &&
                    prop.Value.GetString() == "$last.path"
                        ? lastPath ?? throw new ArgumentException("No previous UIA element path.")
                        : prop.Value.Clone();
            }
            var payload = JsonSerializer.SerializeToElement(fields);
            var result = await RouterLite.InvokeLocalAsync(name, payload);
            var serialized = result as string ?? throw new InvalidOperationException("Unexpected GUI result.");
            // Stop on errors returned as JSON, not just exceptions.
            using var resultDoc = JsonDocument.Parse(serialized);
            var obj = resultDoc.RootElement;
            if (obj.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.False)
                throw new InvalidOperationException("Batch step failed: " + name);
            if (obj.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
                throw new InvalidOperationException("Batch step error: " + name);
            if (name == "desktop.uia.find" && obj.TryGetProperty("matches", out var elements) &&
                elements.ValueKind == JsonValueKind.Array && elements.GetArrayLength() > 0 &&
                elements[0].TryGetProperty("path", out var path))
                lastPath = path.GetString();
            results.Add(new { tool = name, success = true,
                result = serialized.Length > 2500 ? serialized[..2500] : serialized });
        }
        return JsonSerializer.Serialize(new { ok = true, completed = results.Count, steps = results });
    }
}
