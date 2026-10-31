using System.Diagnostics;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

// A single stable HTTP CONNECT relay for DOMKAT's official tunnel-client.
// It never exposes a listener to the network: 127.0.0.1 only.
// Each new connection reads the GUI's selected mode, therefore mode changes
// do NOT require a second tunnel-client or restarting an existing MCP process.
internal static class Program
{
    private const int Port = 11827;
    private const int FreePort = 11821;
    private const int HappPort = 10809;
    private static readonly string DataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChatGPTMCPAgent", "Vpn");
    private static readonly string ModePath = Environment.GetEnvironmentVariable("CHATGPT_MCP_BRIDGE_TEST_MODE_FILE") ?? Path.Combine(DataFolder, "network-mode.json");
    private static readonly string ProfilePath = Environment.GetEnvironmentVariable("CHATGPT_MCP_BRIDGE_TEST_PROFILE_FILE") ??
        Path.Combine(DataFolder, "profiles", "Free-Reality-Auto.dpapi");
    private static readonly string ProfileStatus = Environment.GetEnvironmentVariable("CHATGPT_MCP_BRIDGE_TEST_STATUS_FILE") ??
        Path.Combine(DataFolder, "profiles", "status.json");
    private static readonly string RuntimeDir = Path.Combine(DataFolder, "bridge-runtime");
    private static readonly SemaphoreSlim FreeLaunch = new(1);
    private static Process? _ownedFree;
    private static string? _loadedProfileHash;
    // The scheduler owns encrypted profiles; only the bridge owns live Xray.
    // A failing active node is replaced by a pre-tested standby first.
    private static string _activeProfilePath = ProfilePath;
    private static readonly string[] StandbyFiles =
    {
        Environment.GetEnvironmentVariable("CHATGPT_MCP_BRIDGE_TEST_STANDBY_FILE") ??
            Path.Combine(DataFolder, "profiles", "Free-Reality-Standby-1.dpapi"),
        Path.Combine(DataFolder, "profiles", "Free-Reality-Standby-2.dpapi")
    };
    private static int _freeFailures;
    private const int CandidatePort = 11822;
    private const int FailureThreshold = 3;
    private static readonly string NoticeFile = Environment.GetEnvironmentVariable("CHATGPT_MCP_BRIDGE_TEST_MODE_FILE") is string path ?
        path + ".notice.json" : Path.Combine(DataFolder, "network-notice.json");
    private static readonly bool IsLabMode = Environment.GetEnvironmentVariable("CHATGPT_MCP_BRIDGE_TEST_MODE_FILE") is not null;
    private static readonly bool ForceTestHealthFailure = IsLabMode &&
        Environment.GetEnvironmentVariable("CHATGPT_MCP_BRIDGE_TEST_FORCE_HEALTH_FAILURE") == "1";

    private static long _upstreamConnections;
    private static readonly object MetricsLock = new();
    private static readonly SemaphoreSlim Connections = new(128, 128);
    // Disconnect stale keep-alive tunnels when the operator switches modes.
    // Go's control-plane HTTP client then reconnects through the new upstream
    // without restarting the MCP runtime or its tunnel-client process.
    private static readonly ConcurrentDictionary<long,(TcpClient Socket,string Mode)> Active = new();
    private static long _nextConnectionId;

    private static string _exeFolder = AppContext.BaseDirectory;

    static async Task<int> Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--selftest")
        {
            Console.WriteLine("BRIDGE_SELFTEST_OK port=" + Port +
                " mode=" + ReadMode() + " happ=" + HappPort + " free=" + FreePort);
            return 0;
        }
        int localPort = Port;
        if (args.Length == 2 && args[0] == "--port" && int.TryParse(args[1], out var requested) && requested is >= 1025 and <= 65535 && requested != HappPort && requested != FreePort)
            localPort = requested;
        else if (args.Length != 0)
        {
            Console.Error.WriteLine("Unrecognized argument");
            return 2;
        }
        using var cancel = new CancellationTokenSource();
        Console.CancelKeyPress += (_, ev) => { ev.Cancel = true; cancel.Cancel(); };
        using var listener = new TcpListener(IPAddress.Loopback, localPort);
        try { listener.Start(128); }
        catch (SocketException) { Console.Error.WriteLine("Bridge port already occupied."); return 3; }
        Console.Error.WriteLine("MCP_SERVER_PROXY_READY loopback=" + localPort);
        _ = Task.Run(() => WatchModeAsync(cancel.Token));

        try
        {
            while (!cancel.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(cancel.Token);
                if (!Connections.Wait(0))
                {
                    client.Dispose();
                    continue;
                }
                _ = Task.Run(async () =>
                {
                    try { await HandleAsync(client, cancel.Token); }
                    catch (Exception ex) when (ex is IOException or SocketException or
                                               OperationCanceledException or ObjectDisposedException)
                    { }
                    finally { client.Dispose(); Connections.Release(); }
                });
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            listener.Stop();
            if (_ownedFree is { HasExited: false } child)
                child.Kill(entireProcessTree: false);
            _ownedFree?.Dispose();
        }
        return 0;
    }

    private static async Task WatchModeAsync(CancellationToken token)
    {
        string last = ReadMode();
        int ticks = 0;
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(500, token);
                string current = ReadMode();
                if (current != last)
                {
                    last = current;
                    _freeFailures = 0;
                    DisconnectStaleSessions(current);
                    if (current == "happ")
                    {
                        StopOwnedXray();
                        _activeProfilePath = ProfilePath;
                    }
                }
                int healthEvery = IsLabMode ? 6 : 40;
                if (current != "free" || (++ticks % healthEvery) != 0) continue;
                try
                {
                    if (!await EnsureFreeAsync(token) ||
                        (ForceTestHealthFailure && _activeProfilePath == ProfilePath) || !await ProbeEndpointAsync(FreePort, token))
                    {
                        _freeFailures++;
                    }
                    else
                    {
                        _freeFailures = 0;
                        // Update is changed on disk by the independent 3h scheduler.
                        // Test a fresh profile on an isolated port BEFORE stopping
                        // the working node; keep old if candidate fails.
                        if (_loadedProfileHash != null && File.Exists(ProfilePath))
                        {
                            string fingerprint = Convert.ToHexString(
                                SHA256.HashData(File.ReadAllBytes(ProfilePath)));
                            if (fingerprint != _loadedProfileHash)
                            {
                                if (await VerifyNewProfileAsync(ProfilePath, token) &&
                                    ReadMode() == "free")
                                {
                                    StopOwnedXray();
                                    DisconnectStaleSessions("restart");
                                    _activeProfilePath = ProfilePath;
                                    if (!await EnsureFreeAsync(token) ||
                                        !await ProbeEndpointAsync(FreePort, token))
                                        _freeFailures = FailureThreshold;
                                }
                                // A bad replacement is NOT promoted; existing
                                // working proxy continues until the next scan.
                            }
                        }
                    }
                }
                catch (Exception ex) when (ex is IOException or JsonException or CryptographicException or
                                           HttpRequestException or TaskCanceledException or InvalidOperationException)
                {
                    _freeFailures++;
                }
                if (_freeFailures >= FailureThreshold)
                {
                    // Do not abandon Free if a second validated node is live.
                    bool rotated = await TryRotateStandbyAsync(token);
                    if (!rotated)
                        SetHappFallback("free-route-health-failed");
                    _freeFailures = 0;
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private static void DisconnectStaleSessions(string mode)
    {
        foreach (var (_, connection) in Active)
        {
            if (connection.Mode == mode) continue;
            try { connection.Socket.Dispose(); }
            catch (ObjectDisposedException) { }
        }
    }

    private static void StopOwnedXray()
    {
        var child = _ownedFree;
        _ownedFree = null;
        _loadedProfileHash = null;
        if (child is null) return;
        try
        {
            if (!child.HasExited) { child.Kill(); child.WaitForExit(3000); }
        }
        catch (InvalidOperationException) { }
        finally { child.Dispose(); }
    }

    private static async Task<bool> TryRotateStandbyAsync(CancellationToken token)
    {
        foreach (var standby in StandbyFiles)
        {
            // Standby is stored under Windows CurrentUser DPAPI by the
            // independent 3-hourly updater and is re-tested here.
            if (standby == _activeProfilePath || !File.Exists(standby)) continue;
            if (!await VerifyNewProfileAsync(standby, token)) continue;
            if (ReadMode() != "free") return false;
            StopOwnedXray();
            DisconnectStaleSessions("restart");
            _activeProfilePath = standby;
            if (await EnsureFreeAsync(token) && await ProbeEndpointAsync(FreePort, token))
            {
                WriteNotice("free-node-rotated",
                    "Бесплатный VPN: подключён резервный рабочий сервер.");
                return true;
            }
            StopOwnedXray();
        }
        return false;
    }

    private static void WriteNotice(string reason, string message)
    {
        try
        {
            Directory.CreateDirectory(DataFolder);
            string tmp = NoticeFile + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(new
            {
                reason, time = DateTimeOffset.UtcNow, message
            }));
            File.Move(tmp, NoticeFile, true);
        }
        catch (IOException) { }
    }

    private static void SetHappFallback(string reason)
    {
        try
        {
            Directory.CreateDirectory(DataFolder);
            // Do not overwrite a newer operator selection.
            if (ReadMode() != "free") return;
            var previous = new { mode = "happ", reason, updatedAt = DateTimeOffset.UtcNow };
            string tmp = ModePath + ".watchdog-tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(previous));
            File.Move(tmp, ModePath, true);
            WriteNotice(reason,
                "Бесплатный сервер недоступен. Выполнен возврат на Happ.");
            DisconnectStaleSessions("happ");
            StopOwnedXray();
        }
        catch (IOException) { }
    }

    private static async Task<bool> ProbeEndpointAsync(int port, CancellationToken token)
    {
        try
        {
            using var handler = new HttpClientHandler {
                Proxy = new WebProxy($"http://127.0.0.1:{port}"),
                UseProxy = true, UseCookies = false
            };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(9) };
            using var reply = await client.GetAsync("https://api.openai.com/v1/models", token);
            return reply.StatusCode == HttpStatusCode.Unauthorized ||
                   reply.StatusCode == HttpStatusCode.Forbidden ||
                   reply.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
        { return false; }
    }

    private static async Task<bool> VerifyNewProfileAsync(string profileFile, CancellationToken token)
    {
        if (IsOpen(CandidatePort)) return false;
        Process? candidate = null;
        string? temp = null;
        byte[]? decrypted = null;
        try
        {
            string exe = Path.Combine(_exeFolder, "Vpn", "xray.exe");
            string expected = File.ReadAllText(Path.Combine(_exeFolder, "Vpn", "xray.sha256")).Trim();
            string real = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(exe)));
            if (expected.Length != 64 || !string.Equals(real, expected, StringComparison.OrdinalIgnoreCase))
                return false;
            decrypted = ProtectedData.Unprotect(File.ReadAllBytes(profileFile),
                null, DataProtectionScope.CurrentUser);
            var document = JsonNode.Parse(decrypted)!.AsObject();
            if (document["inbounds"]?[0]?["listen"]?.ToString() != "127.0.0.1" ||
                document["inbounds"]?[0]?["port"]?.GetValue<int>() != FreePort ||
                document["outbounds"]?[0]?["protocol"]?.ToString() != "vless")
                return false;
            document["inbounds"]![0]!["port"] = CandidatePort;
            Directory.CreateDirectory(RuntimeDir);
            temp = Path.Combine(RuntimeDir, Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(temp, document.ToJsonString());
            var psi = new ProcessStartInfo(exe) { UseShellExecute=false, CreateNoWindow=true,
                WorkingDirectory=Path.GetDirectoryName(exe)! };
            psi.ArgumentList.Add("run"); psi.ArgumentList.Add("-config"); psi.ArgumentList.Add(temp);
            candidate = Process.Start(psi);
            if (candidate is null) return false;
            for(int i=0;i<30;i++)
            {
                if (candidate.HasExited) return false;
                if (IsOpen(CandidatePort)) break;
                await Task.Delay(100, token);
            }
            return IsOpen(CandidatePort) && await ProbeEndpointAsync(CandidatePort, token);
        }
        catch (Exception ex) when (ex is IOException or JsonException or CryptographicException or
                                   InvalidOperationException or System.ComponentModel.Win32Exception)
        { return false; }
        finally
        {
            if (candidate != null)
            {
                if (!candidate.HasExited) { candidate.Kill(); candidate.WaitForExit(2000); }
                candidate.Dispose();
            }
            if (decrypted != null) CryptographicOperations.ZeroMemory(decrypted);
            if (temp != null && File.Exists(temp)) File.Delete(temp);
        }
    }

    private static string ReadMode()
    {
        try
        {
            if (!File.Exists(ModePath)) return "happ";
            using var document = JsonDocument.Parse(File.ReadAllText(ModePath));
            // Invalid settings fail closed instead of falling back to a direct connection.
            var mode = document.RootElement.GetProperty("mode").GetString();
            return mode == "free" ? "free" : mode == "happ" ? "happ" : "invalid";
        }
        catch (FileNotFoundException) { return "happ"; }
        catch (Exception) { return "invalid"; }
    }

    private static async Task HandleAsync(TcpClient incoming, CancellationToken token)
    {
        incoming.NoDelay = true;
        using var inbound = incoming.GetStream();
        var header = new byte[32768];
        int length = 0;
        while (length < header.Length)
        {
            int read = await inbound.ReadAsync(header.AsMemory(length, header.Length - length), token);
            if (read <= 0) return;
            length += read;
            if (header.AsSpan(0, length).IndexOf("\r\n\r\n"u8) >= 0) break;
        }
        if (length >= header.Length || header.AsSpan(0, length).IndexOf("\r\n\r\n"u8) < 0)
        {
            await Fail(inbound, 431, token);
            return;
        }

        var first = Encoding.ASCII.GetString(header, 0,
            Math.Min(length, header.AsSpan(0, length).IndexOf("\r\n"u8)));
        // Go tunnel-client must use HTTP CONNECT to reach https://api.openai.com;
        // rejecting other methods prevents accidentally exposing a general HTTP proxy.
        if (!first.StartsWith("CONNECT ", StringComparison.Ordinal))
        {
            await Fail(inbound, 405, token);
            return;
        }
        string mode = ReadMode();
        if (mode is not ("free" or "happ"))
        {
            await Fail(inbound, 503, token);
            return;
        }
        int upstreamPort = mode == "free" ? FreePort : HappPort;
        if (mode == "free" && !await EnsureFreeAsync(token))
        {
            await Fail(inbound, 503, token);
            return;
        }
        using var outgoing = new TcpClient { NoDelay = true };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            await outgoing.ConnectAsync(IPAddress.Loopback, upstreamPort, timeout.Token);
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException)
        {
            await Fail(inbound, 502, token);
            return;
        }

        var count = Interlocked.Increment(ref _upstreamConnections);
        lock (MetricsLock)
        {
            try
            {
                Directory.CreateDirectory(DataFolder);
                string path = Path.Combine(DataFolder, "bridge-health.json");
                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonSerializer.Serialize(new
                {
                    time = DateTimeOffset.UtcNow,
                    mode,
                    successfulUpstreamConnections = count,
                    upstreamIsLoopback = true
                }));
                File.Move(temp, path, true);
            }
            catch (IOException) { }
        }
        long id = Interlocked.Increment(ref _nextConnectionId);
        Active[id] = (incoming, mode);
        try
        {
        using var upstream = outgoing.GetStream();
            await upstream.WriteAsync(header.AsMemory(0, length), token);
            using var disconnect = CancellationTokenSource.CreateLinkedTokenSource(token);
            var upload = inbound.CopyToAsync(upstream, disconnect.Token);
            var download = upstream.CopyToAsync(inbound, disconnect.Token);
            await Task.WhenAny(upload, download);
            disconnect.Cancel();
        }
        finally { Active.TryRemove(id, out _); }

        // Closing both sockets terminates the other direction.
    }

    private static async Task Fail(NetworkStream output, int code, CancellationToken token)
    {
        var data = Encoding.ASCII.GetBytes(
            $"HTTP/1.1 {code} Unavailable\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
        try { await output.WriteAsync(data, token); }
        catch (Exception ex) when (ex is IOException or SocketException) { }
    }

    private static bool IsOpen(int port)
    {
        try
        {
            using var tcp = new TcpClient();
            var attempt = tcp.BeginConnect(IPAddress.Loopback, port, null, null);
            if (!attempt.AsyncWaitHandle.WaitOne(300)) return false;
            tcp.EndConnect(attempt);
            return true;
        }
        catch (Exception ex) when (ex is SocketException or ObjectDisposedException) { return false; }
    }

    private static async Task<bool> EnsureFreeAsync(CancellationToken token)
    {
        if (IsOpen(FreePort)) return true;
        await FreeLaunch.WaitAsync(token);
        try
        {
            if (IsOpen(FreePort)) return true;
            if (!File.Exists(_activeProfilePath) || !File.Exists(ProfileStatus)) return false;
            using (var status = JsonDocument.Parse(File.ReadAllText(ProfileStatus)))
            {
                var date = DateTimeOffset.Parse(status.RootElement
                    .GetProperty("selected").GetProperty("verifiedAt").GetString()!);
                if (DateTimeOffset.UtcNow - date > TimeSpan.FromHours(6) ||
                    date > DateTimeOffset.UtcNow.AddMinutes(5)) return false;
            }
            string exe = Path.Combine(_exeFolder, "Vpn", "xray.exe");
            string expectedFile = Path.Combine(_exeFolder, "Vpn", "xray.sha256");
            if (!File.Exists(exe) || !File.Exists(expectedFile)) return false;
            string expected = File.ReadAllText(expectedFile).Trim();
            string actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(exe)));
            if (expected.Length != 64 ||
                !CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(expected.ToUpperInvariant()),
                    Encoding.ASCII.GetBytes(actual))) return false;
            byte[] plain = ProtectedData.Unprotect(File.ReadAllBytes(_activeProfilePath), null,
                DataProtectionScope.CurrentUser);
            string configFile = "";
            try
            {
                using var cfg = JsonDocument.Parse(plain);
                var root = cfg.RootElement;
                if (root.GetProperty("inbounds")[0].GetProperty("listen").GetString() != "127.0.0.1" ||
                    root.GetProperty("inbounds")[0].GetProperty("port").GetInt32() != FreePort ||
                    root.GetProperty("outbounds")[0].GetProperty("protocol").GetString() != "vless" ||
                    root.GetProperty("outbounds")[0].GetProperty("streamSettings")
                        .GetProperty("security").GetString() != "reality")
                    return false;
                Directory.CreateDirectory(RuntimeDir);
                configFile = Path.Combine(RuntimeDir, Guid.NewGuid().ToString("N") + ".json");
                File.WriteAllBytes(configFile, plain);
                var info = new ProcessStartInfo(exe)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(exe)!
                };
                info.ArgumentList.Add("run");
                info.ArgumentList.Add("-config");
                info.ArgumentList.Add(configFile);
                var p = Process.Start(info);
                if (p is null) return false;
                for (int i = 0; i < 50 && !p.HasExited; i++)
                {
                    if (IsOpen(FreePort)) {
                        _ownedFree = p;
                        _loadedProfileHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(_activeProfilePath)));
                        return true;
                    }
                    await Task.Delay(100, token);
                }
                if (!p.HasExited) p.Kill();
                p.Dispose();
                return false;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plain);
                if (configFile.Length != 0 && File.Exists(configFile)) File.Delete(configFile);
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or CryptographicException or
                                     FormatException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return false;
        }
        finally { FreeLaunch.Release(); }
    }
}
