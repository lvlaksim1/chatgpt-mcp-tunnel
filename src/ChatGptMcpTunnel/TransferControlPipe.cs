using System.IO.Pipes;
using System.Text;
using System.Text.Json;

// Private same-user control channel for the running Router Lite server.
// It starts file transfers without passing binary content through ChatGPT's tool outputs.
internal static class TransferControlPipe
{
    private static string Name => Environment.GetEnvironmentVariable("CHATGPT_MCP_TRANSFER_PIPE_NAME")
        ?? "ChatGptMcpTunnel.FileTransfers";
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class Request
    {
        public string action { get; set; } = "";
        public string? source_path { get; set; }
        public string? destination_path { get; set; }
        public string? source_computer { get; set; }
        public string? destination_computer { get; set; }
        public string? transfer_id { get; set; }
        public bool overwrite { get; set; }
    }

    public static async Task ServeAsync(CancellationToken ct)
    {
        Console.Error.WriteLine($"Router Lite transfers: local control pipe {Name} enabled.");
        while (!ct.IsCancellationRequested)
        {
            NamedPipeServerStream? stream = null;
            try
            {
                stream = new NamedPipeServerStream(
                    Name, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await stream.WaitForConnectionAsync(ct);
                var accepted = stream;
                stream = null;
                _ = Task.Run(() => HandleAsync(accepted, ct), CancellationToken.None);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine($"Transfer pipe: {e.GetType().Name}: {e.Message}");
                try { await Task.Delay(TimeSpan.FromSeconds(1), ct); }
                catch (OperationCanceledException) { break; }
            }
            finally { stream?.Dispose(); }
        }
    }

    private static async Task HandleAsync(NamedPipeServerStream stream, CancellationToken ct)
    {
        using (stream)
        {
            try
            {
                using var reader = new StreamReader(stream, Encoding.UTF8, false, 4096, leaveOpen: true);
                using var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, leaveOpen: true)
                {
                    AutoFlush = true
                };
                var raw = await reader.ReadLineAsync(ct) ?? throw new IOException("Empty transfer request.");
                if (Encoding.UTF8.GetByteCount(raw) > 16384)
                    throw new ArgumentException("Transfer control request exceeds 16 KiB.");
                var request = JsonSerializer.Deserialize<Request>(raw, Options)
                    ?? throw new ArgumentException("Invalid transfer control request.");
                var response = request.action.ToLowerInvariant() switch
                {
                    "start" => FileTransferTools.Start(
                        request.source_path ?? throw new ArgumentException("Source path required."),
                        request.destination_path ?? throw new ArgumentException("Destination path required."),
                        request.source_computer, request.destination_computer, request.overwrite),
                    "status" => FileTransferTools.Status(request.transfer_id),
                    "cancel" => FileTransferTools.CancelTransfer(
                        request.transfer_id ?? throw new ArgumentException("Transfer ID required.")),
                    _ => throw new ArgumentException("Action must be start, status or cancel.")
                };
                await writer.WriteLineAsync(response.AsMemory(), ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
            catch (Exception ex)
            {
                var error = JsonSerializer.Serialize(new { error = ex.GetBaseException().Message });
                try
                {
                    using var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, leaveOpen: true)
                    {
                        AutoFlush = true
                    };
                    await writer.WriteLineAsync(error.AsMemory(), ct);
                }
                catch (Exception inner) when (inner is IOException or OperationCanceledException or ObjectDisposedException) { }
            }
        }
    }

    // CLI: Runtime/ChatGptMcpTunnel.exe --transfer start MKATUSHONOK <source> DOMKAT <destination> [--overwrite]
    // CLI: Runtime/ChatGptMcpTunnel.exe --transfer status [transfer-id]
    // CLI: Runtime/ChatGptMcpTunnel.exe --transfer cancel <transfer-id>
    public static async Task<int> RunClientAsync(string[] args)
    {
        try
        {
            if (args.Length == 0) throw new ArgumentException("Missing transfer command.");
            var request = args[0].ToLowerInvariant() switch
            {
                "start" when args.Length is 5 or 6 && (args.Length == 5 || args[5] == "--overwrite") =>
                    new Request
                    {
                        action = "start",
                        source_computer = args[1],
                        source_path = args[2],
                        destination_computer = args[3],
                        destination_path = args[4],
                        overwrite = args.Length == 6
                    },
                "status" when args.Length is 1 or 2 =>
                    new Request { action = "status", transfer_id = args.Length == 2 ? args[1] : null },
                "cancel" when args.Length == 2 =>
                    new Request { action = "cancel", transfer_id = args[1] },
                _ => throw new ArgumentException(
                    "Usage: --transfer start SOURCE_COMPUTER SOURCE_PATH DEST_COMPUTER DEST_PATH [--overwrite], " +
                    "--transfer status [ID], or --transfer cancel ID.")
            };
            using var client = new NamedPipeClientStream(
                ".", Name, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            await client.ConnectAsync(3000, timeout.Token);
            using var reader = new StreamReader(client, Encoding.UTF8, false, 4096, leaveOpen: true);
            using var writer = new StreamWriter(client, new UTF8Encoding(false), 4096, leaveOpen: true)
            {
                AutoFlush = true
            };
            await writer.WriteLineAsync(JsonSerializer.Serialize(request).AsMemory(), timeout.Token);
            var answer = await reader.ReadLineAsync(timeout.Token)
                ?? throw new IOException("Router Lite did not answer the control command.");
            Console.WriteLine(answer);
            using var parsed = JsonDocument.Parse(answer);
            return parsed.RootElement.TryGetProperty("error", out _) ? 2 : 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { error = ex.GetBaseException().Message }));
            return 1;
        }
    }
}
