using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

/// <summary>
/// MCP-side client for a locally installed Windows service. This client
/// never starts an administrator process, copies credentials or prompts UAC.
/// If the owner has enabled the broker but it is offline, fail closed.
/// </summary>
internal static class AdminBrokerClient
{
    private const string Pipe = "ChatGptMcpAdminV1";
    private const string RegistryPath = @"SOFTWARE\ChatGPTMCPAdmin";

    internal static bool IsConfigured
    {
        get
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(RegistryPath);
                return key?.GetValue("AuthorizedSid") is string sid &&
                    sid.StartsWith("S-1-5-21-", StringComparison.Ordinal);
            }
            catch (System.Security.SecurityException) { return false; }
        }
    }

    private sealed record Response(int Pid, int? ExitCode, bool TimedOut, long DurationMs,
        string Stdout, bool StdoutTruncated, string Stderr, bool StderrTruncated);

    internal static async Task<string> RunAsync(
        string command, string shell, string directory, int timeoutSeconds, int maxOutputChars)
    {
        if (!IsConfigured)
            throw new InvalidOperationException(
                "Administrative service is not enabled; open Tunnel Settings > System.");
        using var pipe = new NamedPipeClientStream(".", Pipe, PipeDirection.InOut, PipeOptions.Asynchronous);
        using var connectionTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try { await pipe.ConnectAsync(connectionTimeout.Token); }
        catch (Exception e) when (e is IOException or OperationCanceledException or UnauthorizedAccessException)
        {
            throw new IOException("MCP Admin Service is offline. No UAC prompt was requested.", e);
        }
        using var reader = new StreamReader(pipe, Encoding.UTF8, false, leaveOpen: true);
        using var writer = new StreamWriter(pipe, new UTF8Encoding(false), leaveOpen: true) { AutoFlush = true };
        await writer.WriteLineAsync(JsonSerializer.Serialize(new {
            Op = "exec", Command = command, Shell = shell, Directory = directory,
            TimeoutSeconds = timeoutSeconds, MaxOutputChars = maxOutputChars
        }));
        using var responseTimeout = new CancellationTokenSource(
            TimeSpan.FromSeconds(timeoutSeconds + 15));
        var line = await reader.ReadLineAsync(responseTimeout.Token)
            ?? throw new IOException("MCP Admin Service disconnected before returning a result.");
        if (line.Length > 4_100_000)
            throw new InvalidDataException("Privileged response exceeded the configured maximum.");
        var result = JsonSerializer.Deserialize<Response>(line)
            ?? throw new InvalidDataException("Admin Service returned no result.");
        if (ControlState.IsStopped)
            throw new OperationCanceledException("Stopped by local user.");
        return JsonSerializer.Serialize(new {
            command, shell, working_directory = directory, elevated = true,
            broker = "ChatGptMcpAdmin", pid = result.Pid,
            exit_code = result.ExitCode, timed_out = result.TimedOut,
            duration_ms = result.DurationMs,
            stdout = result.Stdout, stdout_truncated = result.StdoutTruncated,
            stderr = result.Stderr, stderr_truncated = result.StderrTruncated
        });
    }

    internal static bool SelfTest()
    {
        return Pipe == "ChatGptMcpAdminV1" &&
            RegistryPath.StartsWith("SOFTWARE", StringComparison.Ordinal);
    }
}
