using System.ComponentModel;
using System.Diagnostics;

namespace ChatGptMcpAgent;

internal enum ExistingTunnelKind { None, Own, Other }

/// <summary>
/// The official tunnel-client can survive a UI restart. A process-name-only
/// guard mislabeled that same healthy server as a foreign instance.
/// Only adopt a single client whose EXE path equals our installed bundle.
/// Anything ambiguous is left untouched and does not launch duplicates.
/// </summary>
internal static class ServerProcessOwnership
{
    internal static ExistingTunnelKind Classify(
        IEnumerable<string?> executablePaths, string bundledExecutable)
    {
        var own = 0;
        var foreign = 0;
        foreach (var path in executablePaths)
        {
            if (path is not null &&
                string.Equals(Path.GetFullPath(path), Path.GetFullPath(bundledExecutable),
                    StringComparison.OrdinalIgnoreCase))
                own++;
            else
                foreign++;
        }
        return foreign > 0 || own > 1 ? ExistingTunnelKind.Other :
            own == 1 ? ExistingTunnelKind.Own : ExistingTunnelKind.None;
    }

    internal static (ExistingTunnelKind Kind, Process? Process) Inspect(string installedExe)
    {
        Process? owned = null;
        var paths = new List<string?>();
        foreach (var p in Process.GetProcessesByName("tunnel-client"))
        {
            try
            {
                if (p.HasExited)
                {
                    p.Dispose();
                    continue;
                }
                string? path = null;
                try { path = p.MainModule?.FileName; }
                catch (Exception e) when (e is Win32Exception or InvalidOperationException or
                    UnauthorizedAccessException) { /* Unknown == foreign, never take ownership. */ }
                paths.Add(path);
                if (path is not null &&
                    string.Equals(Path.GetFullPath(path), Path.GetFullPath(installedExe),
                        StringComparison.OrdinalIgnoreCase) && owned is null)
                    owned = p;
                else
                    p.Dispose();
            }
            catch (InvalidOperationException) { p.Dispose(); }
        }
        var kind = Classify(paths, installedExe);
        if (kind != ExistingTunnelKind.Own)
        {
            owned?.Dispose();
            owned = null;
        }
        return (kind, owned);
    }

    internal static bool SelfTest()
    {
        var bundled = Path.Combine(Path.GetTempPath(), "mcp-test",
            "OpenAI", "tunnel-client.exe");
        var foreign = Path.Combine(Path.GetTempPath(), "unrelated",
            "tunnel-client.exe");
        var own = Classify(new[] { bundled }, bundled) == ExistingTunnelKind.Own &&
            Classify(new[] { bundled.ToUpperInvariant() }, bundled) == ExistingTunnelKind.Own &&
            Classify(Array.Empty<string>(), bundled) == ExistingTunnelKind.None &&
            Classify(new[] { foreign }, bundled) == ExistingTunnelKind.Other &&
            Classify(new[] { bundled, foreign }, bundled) == ExistingTunnelKind.Other &&
            Classify(new[] { bundled, bundled }, bundled) == ExistingTunnelKind.Other &&
            Classify(new string?[] { null }, bundled) == ExistingTunnelKind.Other;
        // Running process inspection is read-only. This is not a startup test.
        var current = Inspect(Path.Combine(AppContext.BaseDirectory,
            "OpenAI", "tunnel-client.exe"));
        current.Process?.Dispose();
        return own && (!File.Exists(Path.Combine(AppContext.BaseDirectory, "OpenAI", "tunnel-client.exe")) || current.Kind != ExistingTunnelKind.Other);
    }
}
