using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Text.Json;

/// <summary>
/// One-shot elevation: the MCP server stays unelevated. Only the child helper
/// created after Windows UAC consent runs the requested shell command.
/// A private, unpredictable local named pipe carries commands and results;
/// no command text, arguments or output are written to disk or to the UAC CLI.
/// </summary>
internal static class ElevatedShell
{
    private sealed record Request(string Command, string Shell, string Directory, int TimeoutSeconds, int MaxOutputChars);
    private sealed record Result(int Pid, int? ExitCode, bool TimedOut, long DurationMs,
        string Stdout, bool StdoutTruncated, string Stderr, bool StderrTruncated);

    internal static async Task<string> RunWithUacAsync(
        string command, string shell, string directory, int timeoutSeconds, int maxOutputChars)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows UAC is required.");
        Validate(shell, directory, timeoutSeconds, maxOutputChars);
        if (ControlState.IsStopped) throw new OperationCanceledException("Stopped by local user.");

        var exe = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot locate MCP runtime.");
        if (!exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("UAC requires the installed Windows MCP runtime executable.");
        var pipeName = "ChatGptMcpUac-" + Guid.NewGuid().ToString("N");
        using var pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 32768, 32768);
        var psi = new ProcessStartInfo(exe)
        {
            UseShellExecute = true,
            Verb = "runas",
            WorkingDirectory = AppContext.BaseDirectory,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        psi.ArgumentList.Add("--elevated-shell");
        psi.ArgumentList.Add(pipeName);

        using var cancelled = new CancellationTokenSource();
        var stopMonitor = MonitorLocalStopAsync(cancelled);
        try
        {
            Process? child;
            try { child = Process.Start(psi); }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                throw new OperationCanceledException("Windows UAC approval was cancelled.", ex);
            }
            using var helper = child ?? throw new InvalidOperationException("Windows could not launch the elevated helper.");
            using (var connect = CancellationTokenSource.CreateLinkedTokenSource(cancelled.Token))
            {
                connect.CancelAfter(TimeSpan.FromSeconds(120));
                await pipe.WaitForConnectionAsync(connect.Token);
            }
            using var writer = new StreamWriter(pipe, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
            using var reader = new StreamReader(pipe, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
            await writer.WriteLineAsync(JsonSerializer.Serialize(
                new Request(command, shell, directory, timeoutSeconds, maxOutputChars)));
            using var answerTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancelled.Token);
            answerTimeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds + 15));
            var line = await reader.ReadLineAsync(answerTimeout.Token)
                ?? throw new IOException("Elevated helper disconnected without a result.");
            var result = JsonSerializer.Deserialize<Result>(line)
                ?? throw new InvalidDataException("Elevated helper returned an empty result.");
            if (ControlState.IsStopped) throw new OperationCanceledException("Stopped by local user.");
            return JsonSerializer.Serialize(new
            {
                command, shell, working_directory = directory, elevated = true, pid = result.Pid,
                exit_code = result.ExitCode, timed_out = result.TimedOut,
                duration_ms = result.DurationMs,
                stdout = result.Stdout, stdout_truncated = result.StdoutTruncated,
                stderr = result.Stderr, stderr_truncated = result.StderrTruncated
            });
        }
        finally
        {
            cancelled.Cancel();
            pipe.Dispose(); // Broken pipe cancels the privileged command through the helper.
            try { await stopMonitor; } catch (OperationCanceledException) { }
        }
    }

    private static async Task MonitorLocalStopAsync(CancellationTokenSource cancelled)
    {
        try
        {
            while (!cancelled.IsCancellationRequested)
            {
                if (ControlState.IsStopped) { cancelled.Cancel(); return; }
                await Task.Delay(150, cancelled.Token);
            }
        }
        catch (OperationCanceledException) { }
    }

    internal static Task<int> RunHelperAsync(string pipeName) => RunHelperCoreAsync(pipeName, requireElevation: true);

    private static async Task<int> RunHelperCoreAsync(string pipeName, bool requireElevation)
    {
        if (!OperatingSystem.IsWindows() || pipeName.Length != 46 ||
            !pipeName.StartsWith("ChatGptMcpUac-", StringComparison.Ordinal) ||
            !pipeName.AsSpan(14).ToString().All(Uri.IsHexDigit))
            return 90;
        using var identity = WindowsIdentity.GetCurrent();
        if (requireElevation && !new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator)) return 91;
        try
        {
            using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(30000);
            using var reader = new StreamReader(pipe, Encoding.UTF8, false, leaveOpen: true);
            using var writer = new StreamWriter(pipe, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
            var line = await reader.ReadLineAsync();
            if (line is null || line.Length > 2_100_000) return 92;
            var request = JsonSerializer.Deserialize<Request>(line);
            if (request is null) return 93;
            Validate(request.Shell, request.Directory, request.TimeoutSeconds, request.MaxOutputChars);
            using var disconnect = new CancellationTokenSource();
            var watching = WatchDisconnectAsync(reader, disconnect);
            try
            {
                var result = await ExecuteAsync(request, disconnect.Token);
                await writer.WriteLineAsync(JsonSerializer.Serialize(result));
                return 0;
            }
            finally
            {
                disconnect.Cancel();
                try { await watching; } catch (OperationCanceledException) { }
            }
        }
        catch (Exception) { return 94; }
    }

    private static async Task WatchDisconnectAsync(StreamReader reader, CancellationTokenSource cancellation)
    {
        try
        {
            // The parent never sends a second request. EOF means timeout or STOP.
            if (await reader.ReadLineAsync(cancellation.Token) is null) cancellation.Cancel();
        }
        catch (IOException) { cancellation.Cancel(); }
        catch (ObjectDisposedException) { cancellation.Cancel(); }
        catch (OperationCanceledException) { }
    }

    private static void Validate(string shell, string directory, int timeoutSeconds, int maxOutputChars)
    {
        if (shell != "powershell" && shell != "cmd") throw new ArgumentException("Unsupported elevated shell.");
        if (timeoutSeconds is < 1 or > 3600) throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));
        if (maxOutputChars is < 1 or > 2_000_000) throw new ArgumentOutOfRangeException(nameof(maxOutputChars));
        if (!Path.IsPathFullyQualified(directory) || !Directory.Exists(directory))
            throw new DirectoryNotFoundException("Elevated working directory is invalid.");
    }

    private static async Task<Result> ExecuteAsync(Request request, CancellationToken disconnect)
    {
        var psi = new ProcessStartInfo
        {
            FileName = request.Shell == "powershell" ? "powershell.exe" : "cmd.exe",
            WorkingDirectory = request.Directory,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
            UseShellExecute = false, CreateNoWindow = true
        };
        if (request.Shell == "powershell")
        {
            psi.ArgumentList.Add("-NoProfile");
            psi.ArgumentList.Add("-NonInteractive");
            psi.StandardOutputEncoding = new UTF8Encoding(false, true);
            psi.StandardErrorEncoding = new UTF8Encoding(false, true);
            psi.ArgumentList.Add("-Command");
            psi.ArgumentList.Add("[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false); " +
                "$OutputEncoding = [Console]::OutputEncoding; " + request.Command);
        }
        else
        {
            psi.ArgumentList.Add("/d");
            psi.ArgumentList.Add("/s");
            psi.ArgumentList.Add("/c");
            psi.ArgumentList.Add(request.Command);
        }
        using var process = new Process { StartInfo = psi };
        var timer = Stopwatch.StartNew();
        if (!process.Start()) throw new InvalidOperationException("Cannot start elevated shell.");
        process.StandardInput.Close();
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        var timedOut = false;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(request.TimeoutSeconds));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, disconnect);
        try { await process.WaitForExitAsync(linked.Token); }
        catch (OperationCanceledException)
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
            await process.WaitForExitAsync();
            if (disconnect.IsCancellationRequested) throw;
            timedOut = true;
        }
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        return new Result(process.Id, timedOut ? null : process.ExitCode, timedOut, timer.ElapsedMilliseconds,
            stdout.Length > request.MaxOutputChars ? stdout[..request.MaxOutputChars] : stdout,
            stdout.Length > request.MaxOutputChars,
            stderr.Length > request.MaxOutputChars ? stderr[..request.MaxOutputChars] : stderr,
            stderr.Length > request.MaxOutputChars);
    }

    internal static async Task<bool> SelfTestAsync()
    {
        Validate("powershell", Environment.CurrentDirectory, 3, 2000);
        var output = await ExecuteAsync(new Request("Write-Output 'UAC_SHELL_CORE_OK'",
            "powershell", Environment.CurrentDirectory, 5, 2000), CancellationToken.None);
        try { Validate("invalid", Environment.CurrentDirectory, 5, 2000); return false; }
        catch (ArgumentException) { }
        if (output.ExitCode != 0 || !output.Stdout.Contains("UAC_SHELL_CORE_OK")) return false;
        // Exercise actual UTF-8 named-pipe IPC and subprocess output without a UAC
        // prompt or elevated account. Only the production entrypoint requires Admin.
        var name = "ChatGptMcpUac-" + Guid.NewGuid().ToString("N");
        using var pipe = new NamedPipeServerStream(name, PipeDirection.InOut, 1,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        var helperTask = RunHelperCoreAsync(name, requireElevation: false);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(12));
        await pipe.WaitForConnectionAsync(deadline.Token);
        using var writer = new StreamWriter(pipe, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
        using var reader = new StreamReader(pipe, Encoding.UTF8, false, leaveOpen: true);
        await writer.WriteLineAsync(JsonSerializer.Serialize(new Request(
            "Write-Output 'ПРОВЕРКА-UAC-PIPE'", "powershell", Environment.CurrentDirectory, 5, 2000)));
        var response = await reader.ReadLineAsync(deadline.Token);
        if (response is null) return false;
        var parsed = JsonSerializer.Deserialize<Result>(response);
        if (parsed?.ExitCode != 0 || !parsed.Stdout.Contains("ПРОВЕРКА-UAC-PIPE")) return false;
        return await helperTask == 0;
    }
}
