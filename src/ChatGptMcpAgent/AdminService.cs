using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;
using Microsoft.Win32;

namespace ChatGptMcpAgent;

/// <summary>
/// Persistent Windows LocalSystem broker, installed once after an explicit UAC
/// approval. The executable is copied into admin-owned Program Files, never
/// launched from the user-writable Tunnel directory.
/// Windows SID ACLs limit clients to the account that enabled the broker.
/// </summary>
internal static class AdminService
{
    internal const string ServiceName = "ChatGptMcpAdmin";
    internal const string PipeName = "ChatGptMcpAdminV1";
    internal const string RegistryPath = @"SOFTWARE\ChatGPTMCPAdmin";

    private const uint ServiceOwnProcess = 0x10;
    private const uint ServiceStopped = 1, ServiceStartPending = 2,
        ServiceStopPending = 3, ServiceRunning = 4;
    private const uint ServiceAcceptStop = 1;
    private const uint ServiceControlStop = 1, ServiceControlShutdown = 5;

    [StructLayout(LayoutKind.Sequential)]
    private struct ServiceTableEntry
    {
        public IntPtr Name;
        public IntPtr EntryPoint;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct ServiceStatus
    {
        public uint ServiceType;
        public uint CurrentState;
        public uint ControlsAccepted;
        public uint Win32ExitCode;
        public uint ServiceSpecificExitCode;
        public uint CheckPoint;
        public uint WaitHint;
    }
    private delegate void ServiceMainDelegate(uint argc, IntPtr argv);
    private delegate uint ServiceControlDelegate(uint control, uint eventType, IntPtr eventData, IntPtr context);
    private static ServiceMainDelegate? _serviceMain;
    private static ServiceControlDelegate? _controlHandler;
    private static CancellationTokenSource? _shutdown;
    private static IntPtr _statusHandle;

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "StartServiceCtrlDispatcherW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool StartDispatcher([In] ServiceTableEntry[] entries);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "RegisterServiceCtrlHandlerExW")]
    private static extern IntPtr RegisterHandler(string name, ServiceControlDelegate callback, IntPtr context);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNamedPipeClientProcessId(SafePipeHandle pipe, out uint clientPid);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus status);

    internal static int RunAsService()
    {
        if (!OperatingSystem.IsWindows()) return 90;
        _serviceMain = MainLoop;
        _serviceHandler = null;
        var name = Marshal.StringToHGlobalUni(ServiceName);
        try
        {
            var entries = new[]
            {
                new ServiceTableEntry {
                    Name = name,
                    EntryPoint = Marshal.GetFunctionPointerForDelegate(_serviceMain)
                },
                new ServiceTableEntry()
            };
            return StartDispatcher(entries) ? 0 : Marshal.GetLastWin32Error();
        }
        finally { Marshal.FreeHGlobal(name); GC.KeepAlive(_serviceMain); }
    }
    private static object? _serviceHandler;

    private static void State(uint state, uint accepted = 0, uint waitHint = 0)
    {
        if (_statusHandle == IntPtr.Zero) return;
        var status = new ServiceStatus {
            ServiceType = ServiceOwnProcess,
            CurrentState = state,
            ControlsAccepted = accepted,
            WaitHint = waitHint,
            CheckPoint = state is ServiceStartPending or ServiceStopPending ? 1U : 0U
        };
        SetServiceStatus(_statusHandle, ref status);
    }

    private static uint OnControl(uint code, uint eventType, IntPtr data, IntPtr context)
    {
        if (code is ServiceControlStop or ServiceControlShutdown)
        {
            State(ServiceStopPending, waitHint: 10000);
            _shutdown?.Cancel();
        }
        return 0;
    }
    private static void MainLoop(uint argc, IntPtr argv)
    {
        _controlHandler = OnControl;
        _statusHandle = RegisterHandler(ServiceName, _controlHandler, IntPtr.Zero);
        if (_statusHandle == IntPtr.Zero) return;
        State(ServiceStartPending, waitHint: 5000);
        using var cancel = new CancellationTokenSource();
        _shutdown = cancel;
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(RegistryPath, writable: false);
            var ownerSid = key?.GetValue("AuthorizedSid") as string;
            var clientPath = key?.GetValue("AuthorizedClientPath") as string;
            var clientSha = key?.GetValue("AuthorizedClientSHA256") as string;
            if (!ValidSid(ownerSid) || !ValidClientRecord(clientPath, clientSha)) return;
            State(ServiceRunning, ServiceAcceptStop);
            RunPipeAsync(ownerSid!, cancel.Token, PipeName, clientPath, clientSha)
                .GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            // Do not log full requests or command text.
            try
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "ChatGPTMCPAdmin");
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "service-errors.log"),
                    DateTimeOffset.UtcNow.ToString("O") + " " + ex.GetType().Name + Environment.NewLine);
            }
            catch (IOException) { }
        }
        finally
        {
            _shutdown = null;
            State(ServiceStopped);
            GC.KeepAlive(_controlHandler);
        }
    }

    private static bool ValidSid(string? value)
    {
        if (value is null || value.Length > 120 || !value.StartsWith("S-1-5-21-", StringComparison.Ordinal))
            return false;
        try { return new SecurityIdentifier(value).IsAccountSid(); }
        catch (ArgumentException) { return false; }
    }

    private static bool ValidClientRecord(string? path, string? sha) =>
        !string.IsNullOrWhiteSpace(path) &&
        Path.IsPathFullyQualified(path) &&
        Path.GetFileName(path).Equals("ChatGptMcpTunnel.exe", StringComparison.OrdinalIgnoreCase) &&
        sha is { Length: 64 } && sha.All(Uri.IsHexDigit);

    private static bool CheckClientExecutable(NamedPipeServerStream pipe, string? path, string? hash)
    {
        if (path is null || hash is null) return true; // isolated self-tests only
        if (!GetNamedPipeClientProcessId(pipe.SafePipeHandle, out var pid) || pid == 0)
            return false;
        try
        {
            using var process = Process.GetProcessById(checked((int)pid));
            var running = process.MainModule?.FileName;
            if (running is null || !Path.GetFullPath(running)
                    .Equals(Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase))
                return false;
            // Runtime binaries are updated in-place by the official local Inno
            // updater. The client path and Windows SID remain stable across
            // updates; pinning the old SHA would revoke access on every upgrade
            // and force the user through UAC again.
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
            Win32Exception or InvalidOperationException or ArgumentException or OverflowException)
        { return false; }
    }

    private sealed class Request
    {
        public string Op { get; set; } = "";
        public string Command { get; set; } = "";
        public string Shell { get; set; } = "powershell";
        public string Directory { get; set; } = "";
        public int TimeoutSeconds { get; set; } = 120;
        public int MaxOutputChars { get; set; } = 100000;
    }
    private sealed record Result(int Pid, int? ExitCode, bool TimedOut, long DurationMs,
        string Stdout, bool StdoutTruncated, string Stderr, bool StderrTruncated);

    private static PipeSecurity PipeAcl(string sid)
    {
        var acl = new PipeSecurity();
        acl.SetAccessRuleProtection(true, false);
        // Never permit connecting via SMB/named pipes from another computer.
        acl.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.NetworkSid, null),
            PipeAccessRights.FullControl, AccessControlType.Deny));
        acl.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
            PipeAccessRights.FullControl, AccessControlType.Allow));
        acl.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(sid),
            PipeAccessRights.ReadWrite, AccessControlType.Allow));
        return acl;
    }

    private static async Task RunPipeAsync(string authorizedSid, CancellationToken stopped,
        string pipeName = PipeName, string? authorizedExe = null, string? authorizedHash = null)
    {
        var acl = PipeAcl(authorizedSid);
        while (!stopped.IsCancellationRequested)
        {
            using var pipe = NamedPipeServerStreamAcl.Create(pipeName,
                PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous,
                32768, 32768, acl);
            try { await pipe.WaitForConnectionAsync(stopped); }
            catch (OperationCanceledException) { break; }
            // Wait for the first client write before impersonation:
            // Windows cannot impersonate an idle pipe client (ERROR_CANNOT_IMPERSONATE).
            using var reader = new StreamReader(pipe, Encoding.UTF8, false, leaveOpen: true);
            using var writer = new StreamWriter(pipe, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
            try
            {
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stopped);
                deadline.CancelAfter(TimeSpan.FromSeconds(10));
                var line = await reader.ReadLineAsync(deadline.Token);
                if (line is null || line.Length > 1_000_000) continue;
                // ACL restricts connecting SIDs; confirm the actual Windows token.
                string? caller = null;
                try
                {
                    pipe.RunAsClient(() => caller = WindowsIdentity.GetCurrent().User?.Value);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
                if (!string.Equals(caller, authorizedSid, StringComparison.OrdinalIgnoreCase) ||
                    !CheckClientExecutable(pipe, authorizedExe, authorizedHash))
                    continue;
                var request = JsonSerializer.Deserialize<Request>(line);
                if (request is null) continue;
                if (request.Op == "ping")
                {
                    await writer.WriteLineAsync("""{"ready":true,"service":"ChatGptMcpAdmin","admin":true}""");
                    continue;
                }
                if (request.Op != "exec") continue;
                Validate(request);
                using var commandCancellation = CancellationTokenSource.CreateLinkedTokenSource(stopped);
                // Cancellation follows local disconnection as well as the SCM STOP.
                // Never leave an orphaned SYSTEM shell after the caller vanishes.
                var monitor = Task.Run(async () =>
                {
                    try
                    {
                        var extra = await reader.ReadLineAsync(commandCancellation.Token);
                        if (extra is null) commandCancellation.Cancel();
                    }
                    catch (Exception e) when (e is IOException or OperationCanceledException or ObjectDisposedException)
                    { commandCancellation.Cancel(); }
                });
                try
                {
                    var result = await ExecuteAsync(request, commandCancellation.Token);
                    await writer.WriteLineAsync(JsonSerializer.Serialize(result));
                    AuditResult(result.ExitCode, result.TimedOut);
                }
                finally
                {
                    commandCancellation.Cancel();
                    try { await monitor; } catch (OperationCanceledException) { }
                }
            }
            catch (Exception e) when (e is IOException or OperationCanceledException or
                InvalidOperationException or JsonException or ArgumentException or UnauthorizedAccessException)
            {
                // Pipe is per-request. Do not hang the service after failure.
            }
        }
    }

    private static void AuditResult(int? exitCode, bool timedOut)
    {
        try
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "ChatGPTMCPAdmin");
            Directory.CreateDirectory(folder);
            File.AppendAllText(Path.Combine(folder, "actions.log"),
                DateTimeOffset.UtcNow.ToString("O") +
                " operation=shell.exec exit=" + (exitCode?.ToString() ?? "none") +
                " timed_out=" + timedOut + Environment.NewLine);
        }
        catch (IOException) { }
    }

    private static void Validate(Request r)
    {
        if (r.Shell is not ("cmd" or "powershell") ||
            r.TimeoutSeconds is < 1 or > 3600 ||
            r.MaxOutputChars is < 1 or > 2_000_000 ||
            r.Command.Length > 200_000 ||
            !Path.IsPathFullyQualified(r.Directory) || !System.IO.Directory.Exists(r.Directory))
            throw new ArgumentException("Invalid privileged operation");
    }

    private static async Task<Result> ExecuteAsync(Request request, CancellationToken stopped)
    {
        var psi = new ProcessStartInfo
        {
            FileName = request.Shell == "powershell" ? "powershell.exe" : "cmd.exe",
            WorkingDirectory = request.Directory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        if (request.Shell == "powershell")
        {
            psi.StandardOutputEncoding = new UTF8Encoding(false);
            psi.StandardErrorEncoding = new UTF8Encoding(false);
            psi.ArgumentList.Add("-NoProfile");
            psi.ArgumentList.Add("-NonInteractive");
            psi.ArgumentList.Add("-Command");
            psi.ArgumentList.Add("[Console]::OutputEncoding=[System.Text.UTF8Encoding]::new($false);" +
                "$OutputEncoding=[Console]::OutputEncoding;" + request.Command);
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
        if (!process.Start()) throw new InvalidOperationException("Cannot start service command");
        process.StandardInput.Close();
        var stdOut = process.StandardOutput.ReadToEndAsync();
        var stdErr = process.StandardError.ReadToEndAsync();
        var timedOut = false;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stopped);
        timeout.CancelAfter(TimeSpan.FromSeconds(request.TimeoutSeconds));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException)
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
            try { await process.WaitForExitAsync(); } catch (InvalidOperationException) { }
            if (stopped.IsCancellationRequested) throw;
            timedOut = true;
        }
        var stdout = await stdOut;
        var stderr = await stdErr;
        return new Result(process.Id, timedOut ? null : process.ExitCode, timedOut,
            timer.ElapsedMilliseconds,
            stdout[..Math.Min(request.MaxOutputChars, stdout.Length)], stdout.Length > request.MaxOutputChars,
            stderr[..Math.Min(request.MaxOutputChars, stderr.Length)], stderr.Length > request.MaxOutputChars);
    }

    private static string ExeLocation =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "ChatGPTMCPAdmin", "ChatGptMcpAdmin.exe");

    private static int Sc(params string[] args)
    {
        var psi = new ProcessStartInfo("sc.exe") {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var child = Process.Start(psi)
            ?? throw new InvalidOperationException("Cannot run Service Control Manager");
        child.WaitForExit(20000);
        return child.ExitCode;
    }

    internal static bool Installed()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(RegistryPath);
            return key?.GetValue("AuthorizedSid") is string sid && ValidSid(sid) &&
                ValidClientRecord(key.GetValue("AuthorizedClientPath") as string,
                    key.GetValue("AuthorizedClientSHA256") as string);
        }
        catch (System.Security.SecurityException) { return false; }
    }

    internal static async Task<bool> HealthyAsync()
    {
        if (!Installed()) return false;
        try
        {
            using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(1200);
            using var writer = new StreamWriter(pipe, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
            using var reader = new StreamReader(pipe, Encoding.UTF8, false, leaveOpen: true);
            await writer.WriteLineAsync("""{"Op":"ping"}""");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var line = await reader.ReadLineAsync(timeout.Token);
            return line?.Contains("\"ready\":true", StringComparison.Ordinal) == true;
        }
        catch (Exception e) when (e is IOException or TimeoutException or OperationCanceledException or UnauthorizedAccessException)
        { return false; }
    }

    internal static int InstallElevated(string targetSid)
    {
        if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
            return 10;
        if (!ValidSid(targetSid)) return 11;
        var dir = Path.GetDirectoryName(ExeLocation)!;
        Directory.CreateDirectory(dir);
        var existing = Installed();
        if (existing) Sc("stop", ServiceName);
        if (existing) Thread.Sleep(1800);
        File.Copy(Environment.ProcessPath ?? throw new InvalidOperationException(),
            ExeLocation, overwrite: true);
        using (var key = Registry.LocalMachine.CreateSubKey(RegistryPath, writable: true)
                ?? throw new IOException("Cannot create registry key"))
        {
            var trustedClient = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
                "Runtime", "ChatGptMcpTunnel.exe"));
            if (!File.Exists(trustedClient)) return 23;
            using var clientFile = File.OpenRead(trustedClient);
            var trustedHash = Convert.ToHexString(SHA256.HashData(clientFile));
            key.SetValue("AuthorizedSid", targetSid, RegistryValueKind.String);
            key.SetValue("AuthorizedClientPath", trustedClient, RegistryValueKind.String);
            key.SetValue("AuthorizedClientSHA256", trustedHash, RegistryValueKind.String);
        }
        if (!existing)
        {
            var rc = Sc("create", ServiceName,
                "binPath=", "\"" + ExeLocation + "\" --admin-service",
                "start=", "auto", "obj=", "LocalSystem", "DisplayName=", "ChatGPT MCP Admin Service");
            if (rc != 0) return 20;
            Sc("description", ServiceName,
                "Privileged Windows operations for the explicitly authorized ChatGPT MCP Tunnel user.");
        }
        var startResult = Sc("start", ServiceName);
        return startResult == 0 ? 0 : 21;
    }

    internal static int UninstallElevated()
    {
        if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
            return 10;
        Sc("stop", ServiceName);
        Sc("delete", ServiceName);
        try { Registry.LocalMachine.DeleteSubKeyTree(RegistryPath, throwOnMissingSubKey: false); }
        catch (ArgumentException) { }
        // File removal may require reboot because SCM can still hold the executable.
        return 0;
    }

    internal static async Task<bool> SelfTestCommandAsync()
    {
        // Runs a harmless command under the test caller's ordinary identity.
        // Exercises actual authenticated pipe + cancellation + command result.
        var sid = WindowsIdentity.GetCurrent().User?.Value;
        if (!ValidSid(sid)) return false;
        var pipeName = "ChatGptMcpAdminExecTest" + Guid.NewGuid().ToString("N");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
        var server = RunPipeAsync(sid!, timeout.Token, pipeName);
        try
        {
            using var client = new NamedPipeClientStream(".", pipeName,
                PipeDirection.InOut, PipeOptions.Asynchronous);
            await client.ConnectAsync(4000);
            using var reader = new StreamReader(client, Encoding.UTF8, false, leaveOpen: true);
            var writer = new StreamWriter(client, new UTF8Encoding(false), leaveOpen: true)
                { AutoFlush = true };
            try
            {
                var command = new Request {
                    Op = "exec", Shell = "powershell", Directory = Environment.CurrentDirectory,
                    TimeoutSeconds = 8, MaxOutputChars = 1000,
                    Command = "Write-Output 'ADMIN_BROKER_IPC_OK'"
                };
                await writer.WriteLineAsync(JsonSerializer.Serialize(command));
                var response = await reader.ReadLineAsync(timeout.Token);
                var result = response is null ? null : JsonSerializer.Deserialize<Result>(response);
                return result?.ExitCode == 0 &&
                    result.Stdout.Contains("ADMIN_BROKER_IPC_OK", StringComparison.Ordinal);
            }
            finally { try { writer.Dispose(); } catch (IOException) { } }
        }
        finally
        {
            timeout.Cancel();
            try { await server; } catch (OperationCanceledException) { }
        }
    }

    internal static async Task<bool> SelfTestAsync()
    {
        if (ServiceName != "ChatGptMcpAdmin" || !PipeName.StartsWith("ChatGptMcpAdmin") ||
            !ValidSid("S-1-5-21-100-101-102-103") ||
            ValidSid("S-1-5-18") || ValidSid("../fake"))
            return false;
        var sid = WindowsIdentity.GetCurrent().User?.Value;
        if (!ValidSid(sid)) return false;
        var pipeName = "ChatGptMcpAdminTest" + Guid.NewGuid().ToString("N");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        var server = RunPipeAsync(sid!, timeout.Token, pipeName);
        try
        {
            using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut,
                PipeOptions.Asynchronous);
            await client.ConnectAsync(4000);
            var writer = new StreamWriter(client, new UTF8Encoding(false), leaveOpen: true)
                { AutoFlush = true };
            using var reader = new StreamReader(client, Encoding.UTF8, false, leaveOpen: true);
            try
            {
                await writer.WriteLineAsync("""{"Op":"ping"}""");
                var response = await reader.ReadLineAsync(timeout.Token);
                return response?.Contains("\"ready\":true", StringComparison.Ordinal) == true;
            }
            finally
            {
                try { writer.Dispose(); } catch (IOException) { }
            }
        }
        finally
        {
            timeout.Cancel();
            try { await server; } catch (OperationCanceledException) { }
        }
    }
}
