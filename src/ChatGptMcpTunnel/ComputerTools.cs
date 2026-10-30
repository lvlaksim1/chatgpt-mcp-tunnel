using System.ComponentModel;
using System.Diagnostics;
using System.IO.Enumeration;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;
using ModelContextProtocol.Server;

[McpServerToolType]
public static class ComputerTools
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static string Json(object value) => JsonSerializer.Serialize(value, JsonOptions);

    [McpServerTool(Name = "system.info")]
    [Description("Get Windows computer information: OS, CPU, RAM, disks, uptime and optional list of installed programs.")]
    public static async Task<string> SystemInfo(
        [Description("Also list installed programs from Windows uninstall registry entries.")] bool include_installed_programs = false,
        [Description("Computer name from computers.list; defaults to the local computer.")] string? computer = null,
        [Description("Display-only executor identity: project-manager, supervisor, chatgpt, or persona:Name. Defaults to ChatGPT; never used for authorization.")] string? executor = null)
    {
        if (!string.IsNullOrWhiteSpace(computer) &&
            !computer.Equals(RouterLite.LocalComputer, StringComparison.OrdinalIgnoreCase))
            return await RouterLite.CallStringAsync(computer, "system.info", new { include_installed_programs, executor });
        using var control = ControlState.Begin("system.info", readOnly: true, executor: executor);
        return SystemInfoLocal(include_installed_programs);
    }

    internal static string SystemInfoLocal(bool include_installed_programs)
    {
        var memory = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() };
        var memoryOk = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && GlobalMemoryStatusEx(ref memory);
        string? cpu = null;
        if (OperatingSystem.IsWindows())
        {
            try { cpu = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0")?.GetValue("ProcessorNameString")?.ToString()?.Trim(); }
            catch { /* Registry data can be unavailable. */ }
        }

        var disks = DriveInfo.GetDrives().Select(d =>
        {
            try
            {
                return new
                {
                    name = d.Name, type = d.DriveType.ToString(), ready = d.IsReady,
                    format = d.IsReady ? d.DriveFormat : null,
                    total_bytes = d.IsReady ? (long?)d.TotalSize : null,
                    free_bytes = d.IsReady ? (long?)d.AvailableFreeSpace : null
                };
            }
            catch
            {
                return new { name = d.Name, type = d.DriveType.ToString(), ready = false,
                    format = (string?)null, total_bytes = (long?)null, free_bytes = (long?)null };
            }
        }).ToArray();
        return Json(new
        {
            computer = Environment.MachineName,
            user = Environment.UserName,
            windows = RuntimeInformation.OSDescription,
            os_architecture = RuntimeInformation.OSArchitecture.ToString(),
            processor_architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            cpu, logical_processors = Environment.ProcessorCount,
            total_ram_bytes = memoryOk ? (ulong?)memory.TotalPhysical : null,
            available_ram_bytes = memoryOk ? (ulong?)memory.AvailablePhysical : null,
            uptime_seconds = Environment.TickCount64 / 1000,
            dotnet_version = Environment.Version.ToString(),
            disks,
            installed_programs = include_installed_programs ? InstalledPrograms() : null
        });
    }

    [McpServerTool(Name = "fs.list")]
    [Description("List the files and subdirectories in a Windows folder, including file sizes and modification times.")]
    public static async Task<string> FsList([Description("Absolute or relative directory path.")] string path,
        [Description("Target computer from computers.list; omitted means local.")] string? computer = null,
        [Description("Display-only executor identity: project-manager, supervisor, chatgpt, or persona:Name. Defaults to ChatGPT; never used for authorization.")] string? executor = null)
    {
        if (RouterLite.IsRemote(computer)) return await RouterLite.CallStringAsync(computer!, "fs.list", new { path, executor });
        using var control = ControlState.Begin("fs.list", guiAction: false, readOnly: true, executor: executor);
        var folder = Path.GetFullPath(path);
        var directory = new DirectoryInfo(folder);
        if (!directory.Exists) throw new DirectoryNotFoundException(folder);
        var entries = directory.EnumerateFileSystemInfos().Select(entry =>
        {
            try
            {
                var isDirectory = (entry.Attributes & FileAttributes.Directory) != 0;
                return new
                {
                    name = entry.Name, path = entry.FullName,
                    type = isDirectory ? "directory" : "file",
                    size_bytes = !isDirectory && entry is FileInfo file ? (long?)file.Length : null,
                    modified_utc = (DateTime?)entry.LastWriteTimeUtc
                };
            }
            catch
            {
                return new { name = entry.Name, path = entry.FullName,
                    type = "unknown", size_bytes = (long?)null, modified_utc = (DateTime?)null };
            }
        }).OrderBy(x => x.type).ThenBy(x => x.name, StringComparer.OrdinalIgnoreCase).ToArray();
        return Json(new { path = folder, count = entries.Length, entries });
    }

    [McpServerTool(Name = "fs.read")]
    [Description("Read a file without lossy Unicode replacement. Default: strict UTF-8 with BOM detection; choose encoding=windows-1251 for legacy Cyrillic files. Binary content is returned as base64.")]
    public static async Task<string> FsRead(
        [Description("Path to the file.")] string path,
        [Description("Maximum number of text characters or binary bytes to return (default 200000).")] int max_chars = 200000,
        [Description("Target computer from computers.list; omitted means local.")] string? computer = null,
        [Description("Display-only executor identity.")] string? executor = null,
        [Description("Text encoding: auto (strict UTF-8 + Unicode BOM), utf-8, utf-16le, utf-16be, utf-32le, utf-32be, windows-1251. Explicit legacy encoding is required for unmarked Windows-1251 files.")] string encoding = "auto")
    {
        if (RouterLite.IsRemote(computer))
            return await RouterLite.CallStringAsync(computer!, "fs.read",
                new { path, max_chars, encoding, executor });
        using var control = ControlState.Begin("fs.read", guiAction: false, readOnly: true, executor: executor);
        if (max_chars < 1 || max_chars > 2_000_000)
            throw new ArgumentOutOfRangeException(nameof(max_chars), "Use 1..2000000.");
        var file = Path.GetFullPath(path);
        using var input = File.OpenRead(file);
        var head = new byte[Math.Min((int)Math.Min(input.Length, 8192L), 8192)];
        var headRead = input.Read(head, 0, head.Length);
        Encoding? bomEncoding = null;
        var bomBytes = 0;
        if (headRead >= 4 && head[0] == 0xff && head[1] == 0xfe &&
            head[2] == 0 && head[3] == 0)
        {
            bomEncoding = new UTF32Encoding(false, true, true);
            bomBytes = 4;
        }
        else if (headRead >= 4 && head[0] == 0 && head[1] == 0 &&
                 head[2] == 0xfe && head[3] == 0xff)
        {
            bomEncoding = new UTF32Encoding(true, true, true);
            bomBytes = 4;
        }
        else if (headRead >= 3 && head[0] == 0xef && head[1] == 0xbb && head[2] == 0xbf)
        {
            bomEncoding = new UTF8Encoding(false, true);
            bomBytes = 3;
        }
        else if (headRead >= 2 && head[0] == 0xff && head[1] == 0xfe)
        {
            bomEncoding = new UnicodeEncoding(false, true, true);
            bomBytes = 2;
        }
        else if (headRead >= 2 && head[0] == 0xfe && head[1] == 0xff)
        {
            bomEncoding = new UnicodeEncoding(true, true, true);
            bomBytes = 2;
        }
        var binary = encoding.Equals("auto", StringComparison.OrdinalIgnoreCase) &&
                     bomEncoding is null && head.AsSpan(0, headRead).Contains((byte)0);
        input.Position = 0;
        if (binary)
        {
            var bytes = new byte[Math.Min(max_chars, (int)Math.Min(input.Length, 2_000_000L))];
            var count = input.Read(bytes, 0, bytes.Length);
            return Json(new { path = file, format = "base64", size_bytes = input.Length,
                truncated = input.Length > count, base64 = Convert.ToBase64String(bytes, 0, count) });
        }

        Encoding textEncoding = encoding.ToLowerInvariant() switch
        {
            "auto" => bomEncoding ?? new UTF8Encoding(false, true),
            "utf-8" or "utf8" => new UTF8Encoding(false, true),
            "utf-16le" => new UnicodeEncoding(false, true, true),
            "utf-16be" => new UnicodeEncoding(true, true, true),
            "utf-32le" => new UTF32Encoding(false, true, true),
            "utf-32be" => new UTF32Encoding(true, true, true),
            "windows-1251" or "cp1251" => LegacyWindows1251(),
            _ => throw new ArgumentException("Unsupported text encoding.", nameof(encoding))
        };
        try
        {
            // Detect the BOM ourselves. StreamReader's built-in BOM switching uses
            // permissive decoders that silently replace malformed UTF-8 bytes.
            if (bomEncoding is not null && textEncoding.CodePage == bomEncoding.CodePage)
                input.Position = bomBytes;
            using var reader = new StreamReader(input, textEncoding,
                detectEncodingFromByteOrderMarks: false);
            var buffer = new char[max_chars + 1];
            var read = reader.ReadBlock(buffer, 0, buffer.Length);
            var limit = Math.Min(read, max_chars);
            if (read > max_chars && limit > 0 && char.IsHighSurrogate(buffer[limit - 1]))
                limit--; // Never return a broken half of a Unicode surrogate pair.
            return Json(new { path = file, format = "text", size_bytes = input.Length,
                truncated = read > limit, encoding = reader.CurrentEncoding.WebName,
                text = new string(buffer, 0, limit) });
        }
        catch (DecoderFallbackException e)
        {
            throw new InvalidDataException("File does not match the requested text encoding. " +
                "Automatic mode requires valid UTF-8 or a Unicode BOM. " +
                "For legacy Cyrillic use encoding=windows-1251; no lossy replacement was made.", e);
        }
    }

    private static Encoding LegacyWindows1251()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(1251,
            EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
    }

    [McpServerTool(Name = "fs.write")]
    [Description("Atomically write exact UTF-8 text (no BOM), rejecting invalid Unicode. Returns SHA-256; expected_sha256 optionally verifies payload integrity before replacing a file.")]
    public static async Task<string> FsWrite(
        [Description("Path of file to create or overwrite.")] string path,
        [Description("Exact UTF-8 text to store.")] string content,
        [Description("Whether overwriting an existing file is allowed (default true).")] bool overwrite = true,
        [Description("Target computer from computers.list; omitted means local.")] string? computer = null,
        [Description("Display-only executor identity.")] string? executor = null,
        [Description("Optional SHA-256 hex digest of intended UTF-8 bytes; a mismatch rejects writing.")] string? expected_sha256 = null)
    {
        if (RouterLite.IsRemote(computer))
            return await RouterLite.CallStringAsync(computer!, "fs.write",
                new { path, content, overwrite, expected_sha256, executor });
        using var control = ControlState.Begin("fs.write", guiAction: false, readOnly: false, executor: executor);
        if (content is null) throw new ArgumentNullException(nameof(content));
        // Throw on invalid surrogate pairs instead of silently saving a '?' or U+FFFD.
        var bytes = new UTF8Encoding(false, true).GetBytes(content);
        var sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (expected_sha256 is not null &&
            (!System.Text.RegularExpressions.Regex.IsMatch(expected_sha256, "^[a-fA-F0-9]{64}$") ||
             !string.Equals(expected_sha256, sha256, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("UTF-8 payload SHA-256 mismatch; destination was not changed.");
        var file = Path.GetFullPath(path);
        var parent = Path.GetDirectoryName(file)!;
        Directory.CreateDirectory(parent);
        var temp = Path.Combine(parent, "." + Path.GetFileName(file) +
            "." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var stream = new FileStream(temp, FileMode.CreateNew,
                FileAccess.Write, FileShare.None))
                stream.Write(bytes);
            using (var verify = File.OpenRead(temp))
                if (!string.Equals(Convert.ToHexString(SHA256.HashData(verify)), sha256,
                    StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Temporary UTF-8 content failed read-back verification.");
            File.Move(temp, file, overwrite);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
        return Json(new { path = file, encoding = "utf-8", bytes_written = bytes.Length,
            sha256, verified = true, overwritten_allowed = overwrite });
    }

    [McpServerTool(Name = "fs.search")]
    [Description("Search for Windows files and folders by name using wildcards like *.xlsx, recursively or only in one directory.")]
    public static async Task<string> FsSearch(
        [Description("Starting directory.")] string path,
        [Description("File or folder name wildcard, such as *.txt or report*.xlsx.")] string pattern = "*",
        [Description("Include subdirectories (default true).")] bool recursive = true,
        [Description("Maximum matching paths to return (default 200).")] int max_results = 200,
        [Description("Target computer from computers.list; omitted means local.")] string? computer = null,
        [Description("Display-only executor identity: project-manager, supervisor, chatgpt, or persona:Name. Defaults to ChatGPT; never used for authorization.")] string? executor = null)
    {
        if (RouterLite.IsRemote(computer)) return await RouterLite.CallStringAsync(computer!, "fs.search", new { path, pattern, recursive, max_results, executor });
        using var control = ControlState.Begin("fs.search", guiAction: false, readOnly: true, executor: executor);
        if (max_results < 1 || max_results > 10000) throw new ArgumentOutOfRangeException(nameof(max_results), "Use 1..10000.");
        var root = Path.GetFullPath(path);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException(root);
        var pending = new Stack<string>();
        pending.Push(root);
        var matches = new List<object>();
        var skipped = 0;
        var visited = 0;
        while (pending.Count > 0 && matches.Count < max_results)
        {
            var current = pending.Pop();
            visited++;
            try
            {
                foreach (var entry in Directory.EnumerateFileSystemEntries(current))
                {
                    if (matches.Count >= max_results) break;
                    try
                    {
                        var attr = File.GetAttributes(entry);
                        var directory = (attr & FileAttributes.Directory) != 0;
                        if (FileSystemName.MatchesSimpleExpression(pattern, Path.GetFileName(entry), ignoreCase: true))
                            matches.Add(new { path = entry, type = directory ? "directory" : "file" });
                        // Do not follow directory symlinks/junctions to prevent traversal loops.
                        if (recursive && directory && (attr & FileAttributes.ReparsePoint) == 0)
                            pending.Push(entry);
                    }
                    catch (IOException) { skipped++; }
                    catch (UnauthorizedAccessException) { skipped++; }
                }
            }
            catch (IOException) { skipped++; }
            catch (UnauthorizedAccessException) { skipped++; }
        }
        return Json(new { root, pattern, recursive, matches, match_count = matches.Count,
            results_limited = matches.Count >= max_results, directories_visited = visited, skipped_entries = skipped });
    }

    [McpServerTool(Name = "shell.exec")]
    [Description("Execute a PowerShell or CMD command on the Windows computer and return exit code, stdout and stderr.")]
    public static async Task<string> ShellExec(
        [Description("Command to execute using the chosen shell.")] string command,
        [Description("powershell (default) or cmd.")] string shell = "powershell",
        [Description("Working directory; defaults to the server's working directory.")] string? working_directory = null,
        [Description("Execution timeout in seconds (default 120).")] int timeout_seconds = 120,
        [Description("Maximum characters per stdout/stderr stream included in the result.")] int max_output_chars = 100000,
        [Description("Target computer from computers.list; omitted means local.")] string? computer = null,
        [Description("Optional display-only activity code: project_build, tests_run, source_edit, source_read, git_status, git_commit, git_push, app_update, files_check, process_check, diagnostic, etc. Never include arguments, file contents, credentials or free-form descriptions. Automatic safe classification is used when omitted.")] string? activity = null,
        [Description("Display-only executor identity: project-manager, supervisor, chatgpt, or persona:Name. Defaults to ChatGPT; never used for authorization.")] string? executor = null,
        [Description("Set true for one administrative command. Windows requests local UAC approval; the MCP tunnel stays unelevated. No unattended elevation.")] bool elevated = false)
    {
        if (RouterLite.IsRemote(computer)) return await RouterLite.CallStringAsync(computer!, "shell.exec", new { command, shell, working_directory, timeout_seconds, max_output_chars, activity, executor, elevated });
        using var control = ControlState.Begin("shell.exec", guiAction: false, readOnly: false,
            activity: ControlActivityLabels.Shell(command, activity), executor: executor);
        if (timeout_seconds < 1 || timeout_seconds > 3600) throw new ArgumentOutOfRangeException(nameof(timeout_seconds), "Use 1..3600.");
        if (max_output_chars < 1 || max_output_chars > 2_000_000) throw new ArgumentOutOfRangeException(nameof(max_output_chars), "Use 1..2000000.");
        if (elevated)
        {
            // Explicit opt-in: a short-lived elevated helper, never the persistent tunnel process.
            var directory = working_directory is null ? Environment.CurrentDirectory : Path.GetFullPath(working_directory);
            return await ElevatedShell.RunWithUacAsync(command, shell.ToLowerInvariant(), directory,
                timeout_seconds, max_output_chars);
        }
        var startInfo = new ProcessStartInfo
        {
            FileName = shell.ToLowerInvariant() switch
            {
                "powershell" => "powershell.exe",
                "cmd" => "cmd.exe",
                _ => throw new ArgumentException("shell must be powershell or cmd")
            },
            WorkingDirectory = working_directory is null ? Environment.CurrentDirectory : Path.GetFullPath(working_directory),
            RedirectStandardInput = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
            UseShellExecute = false, CreateNoWindow = true
        };
        if (shell.Equals("powershell", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-NonInteractive");
            startInfo.StandardOutputEncoding = new UTF8Encoding(false, true);
            startInfo.StandardErrorEncoding = new UTF8Encoding(false, true);
            startInfo.ArgumentList.Add("-Command");
            // Windows PowerShell 5.1 and .NET child processes must agree on UTF-8.
            startInfo.ArgumentList.Add(
                "[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false); " +
                "$OutputEncoding = [Console]::OutputEncoding; " + command);
        }
        else
        {
            startInfo.ArgumentList.Add("/d");
            startInfo.ArgumentList.Add("/s");
            startInfo.ArgumentList.Add("/c");
            startInfo.ArgumentList.Add(command);
        }
        using var process = new Process { StartInfo = startInfo };
        var timer = Stopwatch.StartNew();
        if (!process.Start()) throw new InvalidOperationException("Failed to start command.");
        using var stopWatcher = new System.Threading.Timer(_ =>
        {
            if (ControlState.IsStopped)
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) { }
            }
        }, null, 150, 150);

        // MCP stdio is a persistent protocol stream. Never let a child shell
        // inherit it: Git and other console programs may await input forever.
        // Deliver EOF to the command immediately unless a future tool adds
        // an explicit stdin payload.
        process.StandardInput.Close();
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        var timedOut = false;
        using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeout_seconds)))
        {
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException)
            {
                timedOut = true;
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }
        if (ControlState.IsStopped) throw new OperationCanceledException("Stopped by local user.");
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        return Json(new
        {
            command, shell, working_directory = startInfo.WorkingDirectory, pid = process.Id,
            exit_code = timedOut ? (int?)null : process.ExitCode, timed_out = timedOut,
            duration_ms = timer.ElapsedMilliseconds,
            stdout = stdout.Length > max_output_chars ? stdout[..max_output_chars] : stdout,
            stdout_truncated = stdout.Length > max_output_chars,
            stderr = stderr.Length > max_output_chars ? stderr[..max_output_chars] : stderr,
            stderr_truncated = stderr.Length > max_output_chars
        });
    }

    [McpServerTool(Name = "process.list")]
    [Description("List running Windows processes with PID, name and memory consumption. Supports filtering by process name.")]
    public static async Task<string> ProcessList(
        [Description("Optional process name substring, case-insensitive.")] string? name_filter = null,
        [Description("Maximum number of processes to return (default 300).")] int max_results = 300,
        [Description("Target computer from computers.list; omitted means local.")] string? computer = null,
        [Description("Display-only executor identity: project-manager, supervisor, chatgpt, or persona:Name. Defaults to ChatGPT; never used for authorization.")] string? executor = null)
    {
        if (RouterLite.IsRemote(computer)) return await RouterLite.CallStringAsync(computer!, "process.list", new { name_filter, max_results, executor });
        using var control = ControlState.Begin("process.list", guiAction: false, readOnly: true, executor: executor);
        if (max_results < 1 || max_results > 20000) throw new ArgumentOutOfRangeException(nameof(max_results), "Use 1..20000.");
        var allProcesses = Process.GetProcesses();
        try
        {
            var filtered = allProcesses.Where(p =>
            {
                try { return name_filter is null || p.ProcessName.Contains(name_filter, StringComparison.OrdinalIgnoreCase); }
                catch { return false; }
            }).OrderBy(p => p.Id).Take(max_results).ToList();
            var results = filtered.Select(p =>
            {
                long? ram = null;
                try { ram = p.WorkingSet64; } catch { }
                return new { pid = p.Id, name = p.ProcessName, working_set_bytes = ram };
            }).ToArray();
            return Json(new { count = results.Length, name_filter, processes = results });
        }
        finally { foreach (var p in allProcesses) p.Dispose(); }
    }

    [McpServerTool(Name = "process.kill")]
    [Description("Terminate a Windows process by PID and optionally terminate child processes as well.")]
    public static async Task<string> ProcessKill(
        [Description("Windows process ID to terminate.")] int pid,
        [Description("Terminate child processes too (default true).")] bool entire_tree = true,
        [Description("Target computer from computers.list; omitted means local.")] string? computer = null,
        [Description("Display-only executor identity: project-manager, supervisor, chatgpt, or persona:Name. Defaults to ChatGPT; never used for authorization.")] string? executor = null)
    {
        if (RouterLite.IsRemote(computer)) return await RouterLite.CallStringAsync(computer!, "process.kill", new { pid, entire_tree, executor });
        using var control = ControlState.Begin("process.kill", guiAction: false, readOnly: false, executor: executor);
        using var process = Process.GetProcessById(pid);
        var name = process.ProcessName;
        process.Kill(entireProcessTree: entire_tree);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { throw new TimeoutException($"Process {pid} has not exited after 15 seconds."); }
        return Json(new { pid, name, terminated = true, entire_tree });
    }

    private static string[] InstalledPrograms()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!OperatingSystem.IsWindows()) return [];
        foreach (var hive in new[] { Registry.LocalMachine, Registry.CurrentUser })
        foreach (var path in new[] { @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall" })
        {
            try
            {
                using var entries = hive.OpenSubKey(path);
                if (entries is null) continue;
                foreach (var name in entries.GetSubKeyNames())
                {
                    try
                    {
                        using var entry = entries.OpenSubKey(name);
                        var display = entry?.GetValue("DisplayName")?.ToString();
                        if (!string.IsNullOrWhiteSpace(display)) names.Add(display);
                    }
                    catch { /* Broken or protected uninstall entries are omitted. */ }
                }
            }
            catch { /* A registry hive can be unavailable. */ }
        }
        return names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatus
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatus memoryStatus);
}
