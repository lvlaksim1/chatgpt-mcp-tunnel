using System.ComponentModel;
using System.Diagnostics;
using System.IO.Enumeration;
using System.Runtime.InteropServices;
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
    public static string SystemInfo(
        [Description("Also list installed programs from Windows uninstall registry entries.")] bool include_installed_programs = false)
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
    public static string FsList([Description("Absolute or relative directory path.")] string path)
    {
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
    [Description("Read a local file. Returns UTF-8/Unicode text or base64 for binary files; output can be truncated to a selected length.")]
    public static string FsRead(
        [Description("Path to the file.")] string path,
        [Description("Maximum number of text characters or binary bytes to return (default 200000).")] int max_chars = 200000)
    {
        if (max_chars < 1 || max_chars > 2_000_000) throw new ArgumentOutOfRangeException(nameof(max_chars), "Use 1..2000000.");
        var file = Path.GetFullPath(path);
        using var input = File.OpenRead(file);
        var head = new byte[Math.Min((int)Math.Min(input.Length, 8192L), 8192)];
        var headRead = input.Read(head, 0, head.Length);
        // UTF-16/UTF-32 text contains zero bytes; recognize BOMs before classifying it as binary.
        var hasUnicodeBom = headRead >= 2 &&
            ((head[0] == 0xFF && head[1] == 0xFE) || (head[0] == 0xFE && head[1] == 0xFF));
        var binary = !hasUnicodeBom && head.AsSpan(0, headRead).Contains((byte)0);
        input.Position = 0;
        if (binary)
        {
            var bytes = new byte[Math.Min(max_chars, (int)Math.Min(input.Length, 2_000_000L))];
            var count = input.Read(bytes, 0, bytes.Length);
            return Json(new { path = file, format = "base64", size_bytes = input.Length,
                truncated = input.Length > count, content = Convert.ToBase64String(bytes, 0, count) });
        }
        using var reader = new StreamReader(input, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var buffer = new char[max_chars + 1];
        var read = reader.ReadBlock(buffer, 0, buffer.Length);
        return Json(new { path = file, format = "text", size_bytes = input.Length,
            truncated = read > max_chars,
            content = new string(buffer, 0, Math.Min(read, max_chars)) });
    }

    [McpServerTool(Name = "fs.write")]
    [Description("Create or overwrite a text file on the Windows computer. Creates missing parent directories.")]
    public static string FsWrite(
        [Description("Path of file to create or overwrite.")] string path,
        [Description("Exact UTF-8 text to store.")] string content,
        [Description("Whether overwriting an existing file is allowed (default true).")] bool overwrite = true)
    {
        var file = Path.GetFullPath(path);
        var parent = Path.GetDirectoryName(file);
        if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
        var bytes = new UTF8Encoding(false).GetBytes(content);
        using (var stream = new FileStream(file, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            stream.Write(bytes);
        return Json(new { path = file, bytes_written = bytes.Length, overwritten_allowed = overwrite });
    }

    [McpServerTool(Name = "fs.search")]
    [Description("Search for Windows files and folders by name using wildcards like *.xlsx, recursively or only in one directory.")]
    public static string FsSearch(
        [Description("Starting directory.")] string path,
        [Description("File or folder name wildcard, such as *.txt or report*.xlsx.")] string pattern = "*",
        [Description("Include subdirectories (default true).")] bool recursive = true,
        [Description("Maximum matching paths to return (default 200).")] int max_results = 200)
    {
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
        [Description("Maximum characters per stdout/stderr stream included in the result.")] int max_output_chars = 100000)
    {
        if (timeout_seconds < 1 || timeout_seconds > 3600) throw new ArgumentOutOfRangeException(nameof(timeout_seconds), "Use 1..3600.");
        if (max_output_chars < 1 || max_output_chars > 2_000_000) throw new ArgumentOutOfRangeException(nameof(max_output_chars), "Use 1..2000000.");
        var startInfo = new ProcessStartInfo
        {
            FileName = shell.ToLowerInvariant() switch
            {
                "powershell" => "powershell.exe",
                "cmd" => "cmd.exe",
                _ => throw new ArgumentException("shell must be powershell or cmd")
            },
            WorkingDirectory = working_directory is null ? Environment.CurrentDirectory : Path.GetFullPath(working_directory),
            RedirectStandardOutput = true, RedirectStandardError = true,
            UseShellExecute = false, CreateNoWindow = true
        };
        if (shell.Equals("powershell", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-NonInteractive");
            startInfo.ArgumentList.Add("-Command");
            startInfo.ArgumentList.Add(command);
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
    public static string ProcessList(
        [Description("Optional process name substring, case-insensitive.")] string? name_filter = null,
        [Description("Maximum number of processes to return (default 300).")] int max_results = 300)
    {
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
        [Description("Terminate child processes too (default true).")] bool entire_tree = true)
    {
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
