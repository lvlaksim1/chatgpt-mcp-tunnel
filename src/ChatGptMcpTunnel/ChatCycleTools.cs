using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;

// A bounded five-minute diagnostic cycle. The computer never starts a ChatGPT
// turn: the hourly ChatGPT automation is responsible for resuming activity.
[McpServerToolType]
public static class ChatCycleTools
{
    private static readonly object Gate = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static string ChatRoot =>
        Environment.GetEnvironmentVariable("CHATGPT_MCP_CYCLE_TEST_CHAT_ROOT") ??
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChatGPTMCPAgent", "Chat");
    private static string StatusPath => Path.Combine(ChatRoot, "cycle-status.json");
    private static string MessagesPath => Path.Combine(ChatRoot, "messages");
    private const int DurationSeconds = 300;
    private const int InactivitySeconds = 120;
    private static ulong _previousIdle, _previousKernel, _previousUser;
    private static bool _hasCpuBaseline;

    [McpServerTool(Name = "chat.cycle", Title = "Чат · Пятиминутный цикл")]
    [Description("Категория <Чат>. Управление пятиминутным диагностическим циклом. " +
      "action=hello, signal='я тут': регистрация запуска модели и получение первого задания. " +
      "action=step: ожидание до 40 секунд и измерение CPU, памяти, процессов; вызывай регулярно " +
      "с полученными session_id и task_id, обрабатывай входящие сообщения из результата через chat.reply. " +
      "После истечения 300 секунд action=complete с кратким отчётом report сохраняет отчёт в окне чата " +
      "и выдаёт новое пятиминутное задание. action=status только читает состояние. " +
      "Если модель перестала отвечать, статус станет offline через 120 секунд; " +
      "новый запуск создаётся только почасовым планировщиком ChatGPT, не туннелем.")]
    public static async Task<string> chat_cycle(
        string action = "status", string signal = "",
        string session_id = "", string task_id = "",
        int wait_seconds = 35, string report = "")
    {
        var now = DateTimeOffset.UtcNow;
        action = (action ?? "").Trim().ToLowerInvariant();
        if (action == "status")
        {
            lock (Gate)
            {
                var state = Load();
                return JsonSerializer.Serialize(state is null
                  ? new { ok = true, online = false, state = "not_started" } as object
                  : new
                  {
                      ok = true,
                      online = !IsStale(state, now),
                      state = IsStale(state, now) ? "offline" : "active",
                      started_utc = state.StartedUtc,
                      last_seen_utc = state.LastSeenUtc,
                      cycle_number = state.CycleNumber,
                      expires_utc = state.ExpiresUtc,
                      seconds_remaining = Math.Max(0, (int)(state.ExpiresUtc - now).TotalSeconds),
                      samples = state.Samples.Count
                  });
            }
        }

        if (action == "hello")
        {
            if (!string.Equals(signal.Trim(), "я тут", StringComparison.OrdinalIgnoreCase))
                return Error("expected_hello_signal");
            lock (Gate)
            {
                var state = new CycleState
                {
                    SessionId = Guid.NewGuid().ToString("N"),
                    TaskId = Guid.NewGuid().ToString("N"),
                    StartedUtc = now,
                    LastSeenUtc = now,
                    ExpiresUtc = now.AddSeconds(DurationSeconds),
                    CycleNumber = 1
                };
                state.Samples.Add(Sample());
                Save(state);
                return JsonSerializer.Serialize(new
                {
                    ok = true, state = "active", session_id = state.SessionId,
                    task_id = state.TaskId, cycle_number = state.CycleNumber,
                    duration_seconds = DurationSeconds, step_seconds = 35,
                    task = "Пять минут контролируй нагрузку CPU, память и работу процессов ChatGptMcpAgent, ChatGptMcpTunnel и tunnel-client. Вызывай chat.cycle(action=step) каждые 30-40 секунд. Входящие запросы пользователя обрабатывай прежде всего. По истечении 300 секунд вызови action=complete с отчётом.",
                    sample = state.Samples.Last(),
                    started_utc = state.StartedUtc,
                    expires_utc = state.ExpiresUtc
                });
            }
        }

        if (action is not ("step" or "complete")) return Error("invalid_action");
        if (!Guid.TryParseExact(session_id, "N", out _) ||
            !Guid.TryParseExact(task_id, "N", out _)) return Error("invalid_ids");

        CycleState? current;
        lock (Gate)
        {
            current = Load();
            if (current is null || current.SessionId != session_id ||
                current.TaskId != task_id)
                return Error("stale_session_or_task");
        }

        if (action == "step")
        {
            var delay = Math.Clamp(wait_seconds, 0, 40);
            if (delay > 0) await Task.Delay(TimeSpan.FromSeconds(delay));
            object? payload;
            lock (Gate)
            {
                var state = Load();
                if (state is null || state.SessionId != session_id ||
                    state.TaskId != task_id) return Error("stale_session_or_task");
                now = DateTimeOffset.UtcNow;
                state.LastSeenUtc = now;
                var measurement = Sample();
                state.Samples.Add(measurement);
                if (state.Samples.Count > 60) state.Samples.RemoveAt(0);
                Save(state);
                payload = new
                {
                    ok = true,
                    state = now >= state.ExpiresUtc ? "ready_to_complete" : "active",
                    cycle_number = state.CycleNumber,
                    seconds_elapsed = (int)(now - state.StartedUtc).TotalSeconds,
                    seconds_remaining = Math.Max(0, (int)(state.ExpiresUtc - now).TotalSeconds),
                    session_id, task_id, sample = measurement,
                    next_action = now >= state.ExpiresUtc
                        ? "chat.cycle(action=complete,report=...)"
                        : "chat.cycle(action=step,wait_seconds=35)"
                };
            }
            var messages = await ChatTools.chat_receive(wait_seconds: 0, max_messages: 10);
            using var parsed = JsonDocument.Parse(messages);
            return JsonSerializer.Serialize(new
            {
                cycle = payload,
                incoming = parsed.RootElement.Clone()
            });
        }

        if (string.IsNullOrWhiteSpace(report) || report.Length > 8000)
            return Error("report_required_max_8000");
        lock (Gate)
        {
            var state = Load();
            if (state is null || state.SessionId != session_id ||
                state.TaskId != task_id) return Error("stale_session_or_task");
            now = DateTimeOffset.UtcNow;
            if (now < state.ExpiresUtc)
                return JsonSerializer.Serialize(new
                {
                    ok = false,
                    error = "cycle_not_finished",
                    seconds_remaining = Math.Max(1, (int)(state.ExpiresUtc - now).TotalSeconds)
                });
            var doneCycle = state.CycleNumber;
            var sampleCount = state.Samples.Count;
            Directory.CreateDirectory(MessagesPath);
            var record = new
            {
                Id = Guid.NewGuid().ToString("N"),
                CreatedUtc = now,
                Text = $"Диагностический цикл №{doneCycle} завершён",
                Status = "answered",
                Reply = report.Trim(),
                RepliedUtc = now,
                Source = "system"
            };
            AtomicWrite(Path.Combine(MessagesPath, record.Id + ".json"), record);
            state.CycleNumber++;
            state.TaskId = Guid.NewGuid().ToString("N");
            state.StartedUtc = now;
            state.LastSeenUtc = now;
            state.ExpiresUtc = now.AddSeconds(DurationSeconds);
            state.Samples.Clear();
            state.Samples.Add(Sample());
            Save(state);
            return JsonSerializer.Serialize(new
            {
                ok = true,
                completed_cycle_number = doneCycle,
                samples_collected = sampleCount,
                report_saved_to_chat = true,
                next_task = new
                {
                    session_id = state.SessionId,
                    task_id = state.TaskId,
                    cycle_number = state.CycleNumber,
                    duration_seconds = DurationSeconds,
                    started_utc = state.StartedUtc,
                    expires_utc = state.ExpiresUtc,
                    task = "Продолжай ту же пятиминутную проверку, периодически вызывая chat.cycle(action=step)."
                }
            });
        }
    }

    private static string Error(string error) => JsonSerializer.Serialize(new { ok = false, error });

    private static bool IsStale(CycleState state, DateTimeOffset now) =>
        now - state.LastSeenUtc > TimeSpan.FromSeconds(InactivitySeconds);

    private static CycleState? Load()
    {
        if (!File.Exists(StatusPath)) return null;
        try { return JsonSerializer.Deserialize<CycleState>(File.ReadAllText(StatusPath)); }
        catch (Exception ex) when (ex is IOException or JsonException) { return null; }
    }

    private static void Save(CycleState state) => AtomicWrite(StatusPath, state);

    private static void AtomicWrite<T>(string path, T content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(content, JsonOptions), new UTF8Encoding(false));
            File.Move(temp, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    private static SampleRecord Sample()
    {
        var record = new SampleRecord { AtUtc = DateTimeOffset.UtcNow };
        try
        {
            using var own = Process.GetCurrentProcess();
            record.RuntimeWorkingSetMiB = Math.Round(own.WorkingSet64 / 1048576.0, 1);
            using var agent = Process.GetProcessesByName("ChatGptMcpAgent").FirstOrDefault();
            using var tunnel = Process.GetProcessesByName("ChatGptMcpTunnel").FirstOrDefault();
            using var client = Process.GetProcessesByName("tunnel-client").FirstOrDefault();
            record.AgentRunning = agent is not null;
            record.TunnelRuntimeRunning = tunnel is not null || own.ProcessName == "ChatGptMcpTunnel";
            record.OpenAiTunnelRunning = client is not null;
            var processes = Process.GetProcesses();
            record.ProcessCount = processes.Length;
            foreach (var proc in processes) proc.Dispose();
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            record.Error = "process_snapshot_unavailable";
        }
        if (OperatingSystem.IsWindows())
        {
            if (GetSystemTimes(out var idle, out var kernel, out var user))
            {
                ulong i = idle.Value, k = kernel.Value, u = user.Value;
                if (_hasCpuBaseline && k >= _previousKernel && u >= _previousUser && i >= _previousIdle)
                {
                    double total = (k - _previousKernel) + (u - _previousUser);
                    double idleDelta = i - _previousIdle;
                    if (total > 0) record.CpuBusyPercent = Math.Round(Math.Clamp(100.0 * (total - idleDelta) / total, 0, 100), 1);
                }
                _previousIdle = i; _previousKernel = k; _previousUser = u;
                _hasCpuBaseline = true;
            }
            var memory = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() };
            if (GlobalMemoryStatusEx(ref memory))
                record.MemoryUsedPercent = memory.MemoryLoad;
        }
        return record;
    }

    private sealed class CycleState
    {
        public string SessionId { get; set; } = "";
        public string TaskId { get; set; } = "";
        public int CycleNumber { get; set; }
        public DateTimeOffset StartedUtc { get; set; }
        public DateTimeOffset ExpiresUtc { get; set; }
        public DateTimeOffset LastSeenUtc { get; set; }
        public List<SampleRecord> Samples { get; set; } = new();
    }
    private sealed class SampleRecord
    {
        public DateTimeOffset AtUtc { get; set; }
        public double? CpuBusyPercent { get; set; }
        public uint? MemoryUsedPercent { get; set; }
        public double? RuntimeWorkingSetMiB { get; set; }
        public int? ProcessCount { get; set; }
        public bool AgentRunning { get; set; }
        public bool TunnelRuntimeRunning { get; set; }
        public bool OpenAiTunnelRunning { get; set; }
        public string? Error { get; set; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime
    {
        public uint Low, High;
        public ulong Value => ((ulong)High << 32) | Low;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatus
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys, AvailPhys, TotalPage, AvailPage, TotalVirtual, AvailVirtual, AvailExtendedVirtual;
    }
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out FileTime idle, out FileTime kernel, out FileTime user);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatus memory);
}
