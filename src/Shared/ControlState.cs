using System.Text.Json;
using System.Text;
using System.Diagnostics;

public sealed class ControlActivity
{
    public string Id { get; set; } = "";
    public string Tool { get; set; } = "";
    public string Activity { get; set; } = "";
    public string Executor { get; set; } = "chatgpt";
    public string StartedUtc { get; set; } = "";
    public string FinishedUtc { get; set; } = "";
    public int ProcessId { get; set; }
}
public sealed class ControlSnapshot
{
    public bool Stopped { get; set; }
    public string LastGuiUtc { get; set; } = "";
    public string LastChangeUtc { get; set; } = "";
    public List<ControlActivity> Active { get; set; } = new();
    public List<ControlActivity> Recent { get; set; } = new();
}

// A persistent local safety gate shared by the tray and the Windows MCP executor.
// Never store user input, command text, paths, filenames, or credentials.
public static class ControlState
{
    private const string MutexName = @"Local\ChatGptMcpControlV1";
    public static string Root => Environment.GetEnvironmentVariable("CHATGPT_MCP_CONTROL_TEST_ROOT") ??
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ChatGPTMCPAgent", "Control");
    public static string StateFile => Path.Combine(Root, "state.json");
    private static T Locked<T>(Func<ControlSnapshot, (T value, bool write)> callback)
    {
        using var gate = new Mutex(false, MutexName);
        var acquired = false;
        try
        {
            try { acquired = gate.WaitOne(TimeSpan.FromSeconds(8)); }
            catch (AbandonedMutexException) { acquired = true; }
            if (!acquired) throw new IOException("Local control lock unavailable.");
            Directory.CreateDirectory(Root);
            var state = File.Exists(StateFile)
                ? JsonSerializer.Deserialize<ControlSnapshot>(File.ReadAllText(StateFile, Encoding.UTF8))
                  ?? throw new InvalidDataException("Local control state is invalid.")
                : new ControlSnapshot();
            state.Active ??= new();
            state.Recent ??= new();
            state.Active.RemoveAll(x => !StillRunning(x.ProcessId));
            var (result, write) = callback(state);
            if (write)
            {
                state.LastChangeUtc = DateTimeOffset.UtcNow.ToString("O");
                var tmp = StateFile + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    File.WriteAllText(tmp, JsonSerializer.Serialize(state), new UTF8Encoding(false));
                    File.Move(tmp, StateFile, true);
                }
                finally { if (File.Exists(tmp)) File.Delete(tmp); }
            }
            return result;
        }
        finally { if (acquired) gate.ReleaseMutex(); }
    }
    private static bool StillRunning(int pid)
    {
        if (pid <= 0) return false;
        try { using var p = Process.GetProcessById(pid); return !p.HasExited; }
        catch (ArgumentException) { return false; }
    }
    public static ControlSnapshot Read() => Locked(s => (s, false));
    public static bool IsStopped => Locked(s => (s.Stopped, false));
    public static void Stop() => Locked(s => { s.Stopped = true; return (true, true); });
    public static void Resume() => Locked(s => { s.Stopped = false; return (true, true); });
    public static void EnsureAllowed()
    {
        if (IsStopped) throw new OperationCanceledException(
            "Computer control was stopped locally by the user.");
    }
    public static Operation Begin(string tool, bool guiAction = false, bool readOnly = false,
        string? activity = null, string? executor = null)
    {
        if (string.IsNullOrWhiteSpace(tool) || tool.Length > 100 ||
            tool.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '.' or '_')))
            throw new ArgumentException("Invalid operation name.", nameof(tool));
        var id = Guid.NewGuid().ToString("N");
        var delay = Locked(s =>
        {
            if (s.Stopped && !readOnly)
                throw new OperationCanceledException("Computer control stopped locally.");
            var remaining = TimeSpan.Zero;
            if (guiAction)
            {
                var now = DateTimeOffset.UtcNow;
                var due = DateTimeOffset.TryParse(s.LastGuiUtc, out var last)
                    ? last.AddSeconds(1) : now;
                if (due > now) remaining = due - now;
                s.LastGuiUtc = now.Add(remaining).ToString("O");
            }
            s.Active.Add(new ControlActivity { Id = id, Tool = tool,
                Activity = ControlActivityLabels.Display(tool, activity) is null ? "" : activity!,
                Executor = ControlExecutor.Normalize(executor),
                StartedUtc = DateTimeOffset.UtcNow.ToString("O"), ProcessId = Environment.ProcessId });
            return (remaining, true);
        });
        var registered = new Operation(id);
        try
        {
            // Check STOP during GUI pacing; clean up registration if interrupted.
            while (delay > TimeSpan.Zero)
            {
                var segment = delay > TimeSpan.FromMilliseconds(70)
                    ? TimeSpan.FromMilliseconds(70) : delay;
                Thread.Sleep(segment);
                if (!readOnly) EnsureAllowed();
                delay -= segment;
            }
            if (!readOnly) EnsureAllowed();
            return registered;
        }
        catch { registered.Dispose(); throw; }
    }
    public sealed class Operation : IDisposable
    {
        private readonly string _id;
        private int _disposed;
        internal Operation(string id) => _id = id;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            Locked(s =>
            {
                var found = s.Active.FirstOrDefault(x => x.Id == _id);
                if (found is not null)
                {
                    s.Active.Remove(found);
                    found.FinishedUtc = DateTimeOffset.UtcNow.ToString("O");
                    s.Recent.Insert(0, found);
                    if (s.Recent.Count > 8) s.Recent.RemoveRange(8, s.Recent.Count - 8);
                }
                return (true, true);
            });
        }
    }
}
