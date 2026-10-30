using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

// File bytes travel only inside the authenticated Router Lite connection, never as MCP results.
[McpServerToolType]
public static class FileTransferTools
{
    private const int ChunkBytes = 1024 * 1024;
    private const int MaxChunkBytes = 2 * ChunkBytes;
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private static readonly ConcurrentDictionary<string, TransferState> Jobs = new();
    private static readonly ConcurrentDictionary<string, byte> Destinations = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object PublishLock = new();
    private static readonly string DataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ChatGptMcpTunnel", "Transfers");
    private static readonly string CancelFolder = Path.Combine(DataFolder, "Cancel");
    private static readonly string StateFile = Path.Combine(DataFolder, "state.json");

    public sealed class TransferState
    {
        public string id { get; set; } = "";
        public string source_computer { get; set; } = "";
        public string destination_computer { get; set; } = "";
        public string executor { get; set; } = "chatgpt";
        public string source_path { get; set; } = "";
        public string destination_path { get; set; } = "";
        public string status { get; set; } = "queued";
        public long total_bytes { get; set; }
        public long transferred_bytes { get; set; }
        public double progress_percent { get; set; }
        public double bytes_per_second { get; set; }
        public long? eta_seconds { get; set; }
        public string? sha256 { get; set; }
        public string? error { get; set; }
        public DateTime started_utc { get; set; } = DateTime.UtcNow;
        public DateTime updated_utc { get; set; } = DateTime.UtcNow;
        [System.Text.Json.Serialization.JsonIgnore]
        public CancellationTokenSource Cancel { get; } = new();
    }
    private sealed class FileInfoWire
    {
        public long size { get; set; }
        public string sha256 { get; set; } = "";
    }
    private sealed class ResumeInfo
    {
        public long size { get; set; }
        public string sha256 { get; set; } = "";
    }
    private static string NormalizeComputer(string? computer) =>
        string.IsNullOrWhiteSpace(computer) ? RouterLite.LocalComputer : computer.Trim();
    private static string Absolute(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
            throw new ArgumentException("Transfer paths must be absolute.");
        return Path.GetFullPath(path);
    }
    private static string Part(string path) => path + ".mcp-transfer.part";
    private static string Meta(string path) => path + ".mcp-transfer.meta.json";
    private static string DestinationKey(string computer, string path) => computer + "|" + Path.GetFullPath(path);
    internal static bool IsWireTool(string name) => name is
        "file.transfer.inspect" or "file.transfer.read" or "file.transfer.prepare" or
        "file.transfer.append" or "file.transfer.finish";

    [McpServerTool(Name = "file.transfer.start")]
    [Description("Start a background binary file transfer between two connected computers over Router Lite. Returns transfer_id immediately. Poll file.transfer.status for progress; data never enters the chat. Source/destination computers default to the local server. Destination is protected unless overwrite=true.")]
    public static string Start(
        [Description("Absolute source file path on source_computer.")] string source_path,
        [Description("Absolute destination file path on destination_computer.")] string destination_path,
        [Description("Source computer name from computers.list; omitted for server.")] string? source_computer = null,
        [Description("Destination computer name from computers.list; omitted for server.")] string? destination_computer = null,
        [Description("Permit replacing an existing destination file after SHA-256 verification (default false).")] bool overwrite = false,
        [Description("Display-only executor identity: project-manager, supervisor, chatgpt or persona:Name.")] string? executor = null)
    {
        ControlState.EnsureAllowed();
        source_path = Absolute(source_path);
        destination_path = Absolute(destination_path);
        var from = NormalizeComputer(source_computer);
        var to = NormalizeComputer(destination_computer);
        if (from.Equals(to, StringComparison.OrdinalIgnoreCase) &&
            source_path.Equals(destination_path, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Source and destination are the same file.");
        if (RouterLite.IsRemote(from) && !RouterLite.IsConnected(from))
            throw new IOException($"Source computer {from} is not connected.");
        if (RouterLite.IsRemote(to) && !RouterLite.IsConnected(to))
            throw new IOException($"Destination computer {to} is not connected.");
        var key = DestinationKey(to, destination_path);
        if (!Destinations.TryAdd(key, 0))
            throw new IOException("Another transfer is already targeting this destination.");
        var job = new TransferState
        {
            id = Guid.NewGuid().ToString("N"), source_computer = from, destination_computer = to,
            source_path = source_path, destination_path = destination_path, executor = ControlExecutor.Normalize(executor)
        };
        Jobs[job.id] = job;
        Publish();
        _ = Task.Run(async () =>
        {
            try
            {
                using var activity = ControlState.Begin("file.transfer.start", executor: job.executor);
                await Execute(job, overwrite);
            }
            catch (OperationCanceledException)
            {
                job.status = "cancelled";
            }
            catch (Exception ex)
            {
                job.status = "failed";
                job.error = ex.GetBaseException().Message;
            }
            finally
            {
                job.updated_utc = DateTime.UtcNow;
                Publish();
                Destinations.TryRemove(key, out _);
            }
        });
        return JsonSerializer.Serialize(new { transfer_id = job.id, status = job.status, from, to, source_path, destination_path });
    }

    [McpServerTool(Name = "file.transfer.status")]
    [Description("Get file transfer progress, bytes transferred, percentage, speed, ETA, completion or error. Omit transfer_id for up to 20 newest transfers.")]
    public static string Status([Description("Optional transfer ID returned by file.transfer.start.")] string? transfer_id = null)
    {
        if (transfer_id is not null)
        {
            if (!Jobs.TryGetValue(transfer_id, out var item))
                return JsonSerializer.Serialize(new { error = "Unknown transfer ID (perhaps server restarted).", transfer_id });
            return JsonSerializer.Serialize(item, Json);
        }
        return JsonSerializer.Serialize(Jobs.Values.OrderByDescending(x => x.started_utc).Take(20).ToArray(), Json);
    }

    [McpServerTool(Name = "file.transfer.cancel")]
    [Description("Cancel a running transfer. Preserves the partial file for future resume.")]
    public static string CancelTransfer([Description("Transfer ID to cancel.")] string transfer_id)
    {
        if (!Jobs.TryGetValue(transfer_id, out var job))
            throw new ArgumentException("Unknown transfer ID.");
        job.Cancel.Cancel();
        Directory.CreateDirectory(CancelFolder);
        File.WriteAllText(Path.Combine(CancelFolder, transfer_id + ".cancel"), "");
        return JsonSerializer.Serialize(new { transfer_id, cancellation_requested = true });
    }

    private static void CheckCancelled(TransferState job)
    {
        job.Cancel.Token.ThrowIfCancellationRequested();
        if (File.Exists(Path.Combine(CancelFolder, job.id + ".cancel")))
            throw new OperationCanceledException();
    }

    private static void Update(TransferState job, string? status = null)
    {
        if (status is not null) job.status = status;
        job.updated_utc = DateTime.UtcNow;
        job.progress_percent = job.total_bytes == 0 ? (status == "completed" ? 100 : 0)
            : Math.Round(100.0 * job.transferred_bytes / job.total_bytes, 2);
        Publish();
    }

    private static void Publish()
    {
        lock (PublishLock)
        {
            try
            {
                Directory.CreateDirectory(DataFolder);
                var snapshot = Jobs.Values.OrderByDescending(x => x.started_utc).Take(100).ToArray();
                var file = StateFile + "." + Environment.ProcessId + ".tmp";
                File.WriteAllText(file, JsonSerializer.Serialize(snapshot, Json));
                File.Move(file, StateFile, true);
            }
            catch (IOException) { } // The status view must never interrupt a transfer.
            catch (UnauthorizedAccessException) { }
        }
    }

    private static async Task<string> Call(string computer, string name, object args)
    {
        if (!RouterLite.IsRemote(computer))
            return await HandleWireAsync(name, JsonSerializer.SerializeToElement(args));
        return await RouterLite.CallStringAsync(computer, name, args);
    }

    private static async Task Execute(TransferState job, bool overwrite)
    {
        var cancelFile = Path.Combine(CancelFolder, job.id + ".cancel");
        try
        {
            var source = JsonSerializer.Deserialize<FileInfoWire>(
                await Call(job.source_computer, "file.transfer.inspect", new { path = job.source_path, executor = job.executor }), Json)
                ?? throw new InvalidDataException("Cannot inspect source file.");
            job.total_bytes = source.size;
            job.sha256 = source.sha256;
            CheckCancelled(job);
            Update(job, "preparing");
            var watch = Stopwatch.StartNew();
            long baseBytes = 0;
            for (var attempt = 0; attempt < 8; attempt++)
            {
                CheckCancelled(job);
                try
                {
                    var offsetText = await Call(job.destination_computer, "file.transfer.prepare",
                        new { path = job.destination_path, size = source.size, sha256 = source.sha256, overwrite, executor = job.executor });
                    var offset = JsonDocument.Parse(offsetText).RootElement.GetProperty("offset").GetInt64();
                    if (offset < 0 || offset > source.size)
                        throw new InvalidDataException("Invalid resume offset.");
                    job.transferred_bytes = offset;
                    baseBytes = offset;
                    watch.Restart();
                    Update(job, "transferring");
                    while (offset < source.size)
                    {
                        CheckCancelled(job);
                        ControlState.EnsureAllowed();
                        var amount = (int)Math.Min(ChunkBytes, source.size - offset);
                        var encoded = await Call(job.source_computer, "file.transfer.read",
                            new { path = job.source_path, offset, count = amount, executor = job.executor });
                        using var packet = JsonDocument.Parse(encoded);
                        var data = packet.RootElement.GetProperty("data").GetString()
                            ?? throw new InvalidDataException("Empty transfer block.");
                        var actualBytes = Convert.FromBase64String(data).Length;
                        if (actualBytes != amount) throw new InvalidDataException("Source block length changed.");
                        var next = await Call(job.destination_computer, "file.transfer.append",
                            new { path = job.destination_path, offset, data, executor = job.executor });
                        var reportedOffset = JsonDocument.Parse(next).RootElement.GetProperty("offset").GetInt64();
                        if (reportedOffset != offset + amount)
                            throw new InvalidDataException("Receiver offset mismatch.");
                        offset = reportedOffset;
                        job.transferred_bytes = offset;
                        var rate = (offset - baseBytes) / Math.Max(watch.Elapsed.TotalSeconds, 0.001);
                        job.bytes_per_second = Math.Round(rate);
                        job.eta_seconds = rate > 0 ? (long)Math.Ceiling((source.size - offset) / rate) : null;
                        if (watch.ElapsedMilliseconds > 0) Update(job);
                    }
                    CheckCancelled(job);
                    Update(job, "verifying");
                    await Call(job.destination_computer, "file.transfer.finish",
                        new { path = job.destination_path, size = source.size, sha256 = source.sha256, overwrite, executor = job.executor });
                    job.transferred_bytes = source.size;
                    job.eta_seconds = 0;
                    Update(job, "completed");
                    return;
                }
                catch (Exception ex) when (attempt < 7 &&
                    (ex is IOException or TimeoutException or System.Net.WebSockets.WebSocketException ||
                     (ex is InvalidOperationException && ex.Message.Contains("not connected", StringComparison.OrdinalIgnoreCase))))
                {
                    Update(job, "reconnecting");
                    await Task.Delay(TimeSpan.FromSeconds(3), job.Cancel.Token);
                }
            }
        }
        finally
        {
            try { if (File.Exists(cancelFile)) File.Delete(cancelFile); } catch (IOException) { }
        }
    }

    internal static Task<string> HandleWireAsync(string name, JsonElement args)
    {
        var actor = args.TryGetProperty("executor", out var executorJson)
            && executorJson.ValueKind == JsonValueKind.String ? executorJson.GetString() : null;
        using var activity = ControlState.Begin(name,
            readOnly: name is "file.transfer.inspect" or "file.transfer.read",
            executor: actor);
        var path = Absolute(args.GetProperty("path").GetString() ?? "");
        return name switch
        {
            "file.transfer.inspect" => Task.FromResult(Inspect(path)),
            "file.transfer.read" => Task.FromResult(Read(path, args.GetProperty("offset").GetInt64(), args.GetProperty("count").GetInt32())),
            "file.transfer.prepare" => Task.FromResult(Prepare(path, args.GetProperty("size").GetInt64(), args.GetProperty("sha256").GetString()!, args.GetProperty("overwrite").GetBoolean())),
            "file.transfer.append" => Task.FromResult(Append(path, args.GetProperty("offset").GetInt64(), args.GetProperty("data").GetString()!)),
            "file.transfer.finish" => Task.FromResult(Finish(path, args.GetProperty("size").GetInt64(), args.GetProperty("sha256").GetString()!, args.GetProperty("overwrite").GetBoolean())),
            _ => throw new NotSupportedException("Unknown transfer command.")
        };
    }

    private static string Inspect(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var sha = Convert.ToHexString(SHA256.HashData(stream));
        return JsonSerializer.Serialize(new { size = stream.Length, sha256 = sha });
    }

    private static string Read(string path, long offset, int count)
    {
        if (offset < 0 || count <= 0 || count > MaxChunkBytes) throw new ArgumentOutOfRangeException(nameof(count));
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (offset > stream.Length || count > stream.Length - offset)
            throw new EndOfStreamException("Source file changed during transfer.");
        stream.Position = offset;
        var buffer = new byte[count];
        stream.ReadExactly(buffer);
        return JsonSerializer.Serialize(new { offset, data = Convert.ToBase64String(buffer) });
    }

    private static ResumeInfo ValidateMetadata(string path, long size, string hash)
    {
        if (size < 0 || hash.Length != 64 || !hash.All(Uri.IsHexDigit))
            throw new ArgumentException("Invalid expected size or SHA-256.");
        if (!File.Exists(Meta(path))) throw new IOException("Transfer metadata is missing.");
        var meta = JsonSerializer.Deserialize<ResumeInfo>(File.ReadAllText(Meta(path)))
            ?? throw new InvalidDataException("Invalid transfer metadata.");
        if (meta.size != size || !meta.sha256.Equals(hash, StringComparison.OrdinalIgnoreCase))
            throw new IOException("A different partial transfer already exists at destination.");
        return meta;
    }

    private static string Prepare(string path, long size, string sha256, bool overwrite)
    {
        if (size < 0 || sha256.Length != 64 || !sha256.All(Uri.IsHexDigit))
            throw new ArgumentException("Invalid expected size or SHA-256.");
        if (!overwrite && File.Exists(path))
            throw new IOException("Destination exists; specify overwrite=true to replace it.");
        var folder = Path.GetDirectoryName(path) ?? throw new ArgumentException("Invalid destination path.");
        Directory.CreateDirectory(folder);
        if (File.Exists(Meta(path)) || File.Exists(Part(path)))
        {
            ValidateMetadata(path, size, sha256);
            if (!File.Exists(Part(path))) throw new IOException("Partial file is missing.");
        }
        else
        {
            File.WriteAllText(Meta(path), JsonSerializer.Serialize(new ResumeInfo { size = size, sha256 = sha256 }));
            using var created = new FileStream(Part(path), FileMode.CreateNew);
        }
        var length = new FileInfo(Part(path)).Length;
        if (length > size) throw new IOException("Partial file exceeds source length.");
        return JsonSerializer.Serialize(new { offset = length });
    }

    private static string Append(string path, long offset, string data)
    {
        var bytes = Convert.FromBase64String(data);
        if (bytes.Length is <= 0 or > MaxChunkBytes || offset < 0) throw new ArgumentOutOfRangeException(nameof(data));
        var metadata = JsonSerializer.Deserialize<ResumeInfo>(File.ReadAllText(Meta(path)))
            ?? throw new InvalidDataException("Invalid transfer metadata.");
        using var stream = new FileStream(Part(path), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        if (stream.Length != offset || stream.Length + bytes.Length > metadata.size)
            throw new IOException($"Receiver offset differs; actual={stream.Length}, expected={offset}.");
        stream.Position = offset;
        stream.Write(bytes);
        stream.Flush();
        return JsonSerializer.Serialize(new { offset = stream.Position });
    }

    private static string Finish(string path, long size, string sha256, bool overwrite)
    {
        ValidateMetadata(path, size, sha256);
        var part = Part(path);
        using (var stream = new FileStream(part, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            if (stream.Length != size) throw new IOException("Incomplete file; cannot commit.");
            var actual = Convert.ToHexString(SHA256.HashData(stream));
            if (!actual.Equals(sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Destination SHA-256 mismatch; partial file preserved.");
        }
        File.Move(part, path, overwrite);
        File.Delete(Meta(path));
        return JsonSerializer.Serialize(new { path, size, sha256, verified = true });
    }
}
