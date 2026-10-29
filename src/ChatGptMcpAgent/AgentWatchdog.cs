using System.Diagnostics;
using System.Text.Json;
using Microsoft.Win32;

namespace ChatGptMcpAgent;

// Bundled recovery worker: the installed executable is its own scheduled-task action.
// Only missing local processes can trigger recovery; network/health failures are diagnostic.
internal static class AgentWatchdog
{
    private const string TaskName = "ChatGPT MCP Tunnel Recovery";
    private static readonly string Root = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ChatGPTMCPAgent", "Watchdog");
    private static readonly string StatePath = Path.Combine(Root, "state.json");
    private static readonly string LogPath = Path.Combine(Root, "watchdog.log");
    private static readonly string PausePath = Path.Combine(Root, "manual-stop.flag");

    private sealed class State
    {
        public int MissingAgent { get; set; }
        public int MissingChildren { get; set; }
        public DateTime LastRepairUtc { get; set; } = DateTime.MinValue;
    }

    internal static void PauseForManualExit()
    {
        try { Directory.CreateDirectory(Root); File.WriteAllText(PausePath, "Manual exit"); }
        catch { /* Manual exit must always remain possible. */ }
    }

    internal static void ClearManualPause()
    {
        try { if (File.Exists(PausePath)) File.Delete(PausePath); }
        catch { /* Launch should continue even when pause-file permissions changed. */ }
    }

    private static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(Root);
            if (File.Exists(LogPath) && new FileInfo(LogPath).Length > 2_000_000)
                File.Move(LogPath, Path.Combine(Root, "watchdog.previous.log"), true);
            File.AppendAllText(LogPath, $"{DateTime.Now:O} {message}{Environment.NewLine}");
        }
        catch { /* Diagnostics must not break recovery. */ }
    }

    private static int TaskCommand(bool remove)
    {
        using var p = new Process();
        p.StartInfo = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "schtasks.exe"))
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        p.StartInfo.ArgumentList.Add(remove ? "/Delete" : "/Create");
        p.StartInfo.ArgumentList.Add("/TN");
        p.StartInfo.ArgumentList.Add(TaskName);
        if (!remove)
        {
            p.StartInfo.ArgumentList.Add("/SC");
            p.StartInfo.ArgumentList.Add("MINUTE");
            p.StartInfo.ArgumentList.Add("/MO");
            p.StartInfo.ArgumentList.Add("2");
            p.StartInfo.ArgumentList.Add("/TR");
            p.StartInfo.ArgumentList.Add($"\"{Environment.ProcessPath}\" --watchdog-check");
            p.StartInfo.ArgumentList.Add("/RL");
            p.StartInfo.ArgumentList.Add("LIMITED");
        }
        p.StartInfo.ArgumentList.Add("/F");
        p.Start();
        // Drain both streams before waiting (localized schtasks output can fill pipes).
        var output = p.StandardOutput.ReadToEndAsync();
        var error = p.StandardError.ReadToEndAsync();
        if (!p.WaitForExit(15_000))
        {
            try { p.Kill(entireProcessTree: true); } catch { }
            Log("TASK timeout");
            return 2;
        }
        Task.WaitAll(output, error);
        Log($"TASK {(remove ? "REMOVE" : "REGISTER")} exit={p.ExitCode}");
        return p.ExitCode;
    }

    internal static int Register()
    {
        try
        {
            var exe = Environment.ProcessPath ?? "";
            var installed = Registry.CurrentUser.OpenSubKey(@"Software\ChatGPTMCPAgent")?
                .GetValue("InstallDir") as string;
            var startup = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run")?
                .GetValue("ChatGPTMCPAgent") as string;
            var role = Registry.CurrentUser.OpenSubKey(@"Software\ChatGPTMCPAgent")?
                .GetValue("Role") as string;
            if (!string.Equals(exe, Path.Combine(installed ?? "", "ChatGptMcpAgent.exe"),
                    StringComparison.OrdinalIgnoreCase))
            {
                Log("TASK registration rejected: executable is not installed path");
                return 3;
            }
            if (role is not ("client" or "server") || string.IsNullOrWhiteSpace(startup) ||
                !startup.Contains($"\"{exe}\"", StringComparison.OrdinalIgnoreCase))
                return Unregister();
            return TaskCommand(remove: false);
        }
        catch (Exception e) { Log($"TASK registration error={e.GetType().Name}"); return 4; }
    }

    internal static int Unregister()
    {
        try
        {
            // schtasks /Query returns nonzero if absent; absence is successful cleanup.
            using var probe = Process.Start(new ProcessStartInfo(
                Path.Combine(Environment.SystemDirectory, "schtasks.exe"))
            {
                ArgumentList = { "/Query", "/TN", TaskName },
                CreateNoWindow = true, UseShellExecute = false,
                RedirectStandardOutput = true, RedirectStandardError = true
            });
            if (probe is null) return 4;
            var output = probe.StandardOutput.ReadToEndAsync();
            var error = probe.StandardError.ReadToEndAsync();
            if (!probe.WaitForExit(15_000)) return 4;
            Task.WaitAll(output, error);
            return probe.ExitCode == 0 ? TaskCommand(remove: true) : 0;
        }
        catch (Exception e) { Log($"TASK remove error={e.GetType().Name}"); return 4; }
    }

    // Policy is independent of probe details and covered by --test-watchdog.
    internal static string Decide(int agent, int children, bool uncertain, bool paused,
        int missingAgent, int missingChildren, bool cooldown)
    {
        if (uncertain || paused || !cooldown || agent > 1) return "NONE";
        if (agent == 0 && children == 0 && missingAgent >= 2) return "START";
        if (agent == 1 && children == 0 && missingChildren >= 4) return "RECOVER";
        return "NONE";
    }

    internal static int SelfTest()
    {
        return Decide(0,0,false,false,2,0,true)=="START" &&
               Decide(1,0,false,false,0,4,true)=="RECOVER" &&
               Decide(1,2,false,false,0,4,true)=="NONE" &&
               Decide(0,2,false,false,4,0,true)=="NONE" &&
               Decide(0,0,true,false,4,0,true)=="NONE" &&
               Decide(0,0,false,true,4,0,true)=="NONE" &&
               Decide(0,0,false,false,4,0,false)=="NONE" &&
               Decide(1,0,false,false,0,3,true)=="NONE" ? 0 : 42;
    }

    // Do not infer absence if a matching process is inaccessible.
    private static (int Count, bool Uncertain, Process? One) Find(string name, string exactPath)
    {
        var count = 0;
        var uncertain = false;
        Process? one = null;
        foreach (var p in Process.GetProcessesByName(name))
        {
            if (p.Id == Environment.ProcessId) { p.Dispose(); continue; }
            try
            {
                if (p.HasExited) continue;
                var path = p.MainModule?.FileName;
                if (string.IsNullOrEmpty(path)) { uncertain = true; continue; }
                if (string.Equals(path, exactPath, StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                    if (one is null) one = p;
                    else p.Dispose();
                }
                else { uncertain = true; p.Dispose(); }
            }
            catch { uncertain = true; p.Dispose(); }
        }
        return (count, uncertain, one);
    }

    internal static int Check()
    {
        try
        {
            Directory.CreateDirectory(Root);
            using var locked = new FileStream(Path.Combine(Root, "watchdog.lock"),
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            var currentExe = Environment.ProcessPath ?? "";
            var installed = Registry.CurrentUser.OpenSubKey(@"Software\ChatGPTMCPAgent")?
                .GetValue("InstallDir") as string;
            var autostart = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run")?
                .GetValue("ChatGPTMCPAgent") as string;
            if (!string.Equals(currentExe, Path.Combine(installed ?? "", "ChatGptMcpAgent.exe"),
                    StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrEmpty(autostart) ||
                !autostart.Contains($"\"{currentExe}\"", StringComparison.OrdinalIgnoreCase))
            {
                Log("CHECK skipped: not the active installed autostart application");
                return 0;
            }
            State state;
            try { state = JsonSerializer.Deserialize<State>(File.ReadAllText(StatePath)) ?? new State(); }
            catch { state = new State(); }
            var root = Path.GetDirectoryName(currentExe)!;
            var role = Registry.CurrentUser.OpenSubKey(@"Software\ChatGPTMCPAgent")?
                .GetValue("Role") as string;
            if (role is not ("server" or "client")) return 0;
            var agent = Find("ChatGptMcpAgent", currentExe);
            var client = role == "server"
                ? Find("tunnel-client", Path.Combine(root, "OpenAI", "tunnel-client.exe"))
                : Find("ChatGptMcpTunnel", Path.Combine(root, "Runtime", "ChatGptMcpTunnel.exe"));
            var server = role == "server"
                ? Find("ChatGptMcpTunnel", Path.Combine(root, "Runtime", "ChatGptMcpTunnel.exe"))
                : (0, false, (Process?)null);
            try
            {
                var children = client.Count + server.Item1;
                var uncertain = agent.Uncertain || client.Uncertain || server.Item2;
                state.MissingAgent = agent.Count == 0 ? Math.Min(100, state.MissingAgent + 1) : 0;
                state.MissingChildren = agent.Count == 1 && children == 0 ?
                    Math.Min(100, state.MissingChildren + 1) : 0;
                var paused = File.Exists(PausePath);
                var cooldown = DateTime.UtcNow - state.LastRepairUtc >= TimeSpan.FromMinutes(20);
                var action = Decide(agent.Count, children, uncertain, paused,
                    state.MissingAgent, state.MissingChildren, cooldown);
                if (action == "RECOVER")
                {
                    // Only when all expected children are absent; ask agent to exit gracefully.
                    var quit = Process.Start(new ProcessStartInfo(currentExe, "--shutdown")
                    { UseShellExecute=false, CreateNoWindow=true });
                    quit?.WaitForExit(28_000);
                    quit?.Dispose();
                    if (agent.One is { HasExited: false }) action = "NONE";
                }
                if (action is "START" or "RECOVER")
                {
                    // Set cooldown before starting, preventing rapid loops even if startup fails.
                    state.LastRepairUtc = DateTime.UtcNow;
                    state.MissingAgent = 0;
                    state.MissingChildren = 0;
                    File.WriteAllText(StatePath, JsonSerializer.Serialize(state));
                    Process.Start(new ProcessStartInfo(currentExe, "--minimized")
                    { UseShellExecute=true, WorkingDirectory=root });
                }
                File.WriteAllText(StatePath, JsonSerializer.Serialize(state));
                Log($"CHECK role={role} agent={agent.Count} children={children} uncertain={uncertain} paused={paused} action={action}");
            }
            finally { agent.One?.Dispose(); client.One?.Dispose(); server.Item3?.Dispose(); }
            return 0;
        }
        catch (IOException) { return 0; } // Concurrent invocation: another worker owns lock.
        catch (Exception e) { Log($"CHECK error={e.GetType().Name}"); return 5; }
    }
}
