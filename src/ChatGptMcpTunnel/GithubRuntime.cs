using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class GithubRuntime
{
    private static readonly HttpClient Client = new(new HttpClientHandler
    {
        AllowAutoRedirect = true,
        AutomaticDecompression = DecompressionMethods.All
    }) { Timeout = TimeSpan.FromSeconds(70) };
    private static readonly SemaphoreSlim AuthLock = new(1, 1);
    private static string? CachedToken;
    private static DateTime CacheUntil;

    private static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };

    public static string Json(object? obj) => JsonSerializer.Serialize(obj, Pretty);
    public static object? ParseObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        return JsonNode.Parse(json) ?? throw new ArgumentException("Invalid JSON body.");
    }
    public static string Segment(string s) => Uri.EscapeDataString(s);
    public static string Repo(string fullName)
    {
        var parts = fullName.Split('/');
        if (parts.Length != 2 || parts.Any(p => p.Length == 0 || !System.Text.RegularExpressions.Regex.IsMatch(p, @"^[A-Za-z0-9_.-]+$")))
            throw new ArgumentException("repository must be owner/name");
        return "/repos/" + Segment(parts[0]) + "/" + Segment(parts[1]);
    }

    public static async Task<string> Token()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GH_TOKEN")))
            return Environment.GetEnvironmentVariable("GH_TOKEN")!.Trim();
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GITHUB_TOKEN")))
            return Environment.GetEnvironmentVariable("GITHUB_TOKEN")!.Trim();
        if (CachedToken != null && DateTime.UtcNow < CacheUntil) return CachedToken;
        await AuthLock.WaitAsync();
        try
        {
            if (CachedToken != null && DateTime.UtcNow < CacheUntil) return CachedToken;
            string gh = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "GitHub CLI", "gh.exe");
            if (!File.Exists(gh)) gh = "gh";
            using var proc = new Process { StartInfo = new ProcessStartInfo
            {
                FileName = gh,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }};
            proc.StartInfo.ArgumentList.Add("auth");
            proc.StartInfo.ArgumentList.Add("token");
            proc.StartInfo.ArgumentList.Add("-h");
            proc.StartInfo.ArgumentList.Add("github.com");
            if (!proc.Start()) throw new InvalidOperationException("GitHub CLI failed to start.");
            Task<string> output = proc.StandardOutput.ReadToEndAsync();
            Task<string> errors = proc.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            try { await proc.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException)
            {
                proc.Kill(entireProcessTree: true);
                throw new InvalidOperationException("GitHub CLI authorization timed out.");
            }
            var token = (await output).Trim();
            _ = await errors;
            if (proc.ExitCode != 0 || token.Length < 20)
                throw new InvalidOperationException("GitHub CLI is not authenticated. Run gh auth login locally on DOMKAT.");
            CachedToken = token;
            CacheUntil = DateTime.UtcNow.AddMinutes(5);
            return token;
        }
        finally { AuthLock.Release(); }
    }

    private static Uri ApiUri(string endpoint, bool uploads = false)
    {
        if (!endpoint.StartsWith('/') || endpoint.StartsWith("//") || endpoint.Contains('#') || endpoint.Contains('\\'))
            throw new ArgumentException("Use an API-relative path starting with /; external URLs are not accepted.");
        if (endpoint.Length > 4096) throw new ArgumentOutOfRangeException(nameof(endpoint));
        return new Uri((uploads ? "https://uploads.github.com" : "https://api.github.com") + endpoint);
    }

    public static async Task<JsonNode?> Api(string method, string endpoint, object? body = null,
                                            string accept = "application/vnd.github+json")
    {
        method = method.ToUpperInvariant();
        if (method is not ("GET" or "POST" or "PATCH" or "PUT" or "DELETE"))
            throw new ArgumentException("Method must be GET, POST, PATCH, PUT or DELETE.");
        using var request = new HttpRequestMessage(new HttpMethod(method), ApiUri(endpoint));
        request.Headers.UserAgent.ParseAdd("ChatGptMcpTunnel-GitHub/1.0");
        request.Headers.Accept.ParseAdd(accept);
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await Token());
        if (body != null)
            request.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        string text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("GitHub API HTTP " + (int)response.StatusCode + ": " + text[..Math.Min(text.Length, 1800)]);
        if (string.IsNullOrWhiteSpace(text)) return null;
        return JsonNode.Parse(text);
    }

    public static async Task<string> ApiOutput(string method, string endpoint, object? body = null,
                                               int max_chars = 120000)
    {
        var result = await Api(method, endpoint, body);
        string data = result?.ToJsonString(Pretty) ?? "null";
        if (max_chars < 1000 || max_chars > 1000000)
            throw new ArgumentOutOfRangeException(nameof(max_chars));
        bool truncated = data.Length > max_chars;
        return Json(new { method, endpoint, truncated, total_chars = data.Length,
                          data = truncated ? data[..max_chars] : data });
    }

    public static async Task<string> Download(string endpoint, string destination)
    {
        if (!Path.IsPathFullyQualified(destination)) throw new ArgumentException("destination must be absolute");
        using var request = new HttpRequestMessage(HttpMethod.Get, ApiUri(endpoint));
        request.Headers.UserAgent.ParseAdd("ChatGptMcpTunnel-GitHub/1.0");
        request.Headers.Accept.ParseAdd(endpoint.Contains("/releases/assets/", StringComparison.OrdinalIgnoreCase)
            ? "application/octet-stream" : "application/vnd.github+json");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await Token());
        using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
        {
            var details = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException("GitHub download HTTP " + (int)response.StatusCode + ": " + details[..Math.Min(details.Length, 900)]);
        }
        if (response.Content.Headers.ContentLength > 300_000_000)
            throw new InvalidOperationException("Download exceeds 300 MB limit.");
        string? folder = Path.GetDirectoryName(destination);
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
        string tmp = destination + ".mcpdownload-" + Guid.NewGuid().ToString("N");
        try
        {
            await using var target = File.Create(tmp);
            await using var source = await response.Content.ReadAsStreamAsync();
            byte[] buffer = new byte[65536]; long size = 0; int n;
            while ((n = await source.ReadAsync(buffer)) > 0)
            {
                size += n;
                if (size > 300_000_000) throw new InvalidOperationException("Download exceeds 300 MB limit.");
                await target.WriteAsync(buffer.AsMemory(0, n));
            }
            await target.FlushAsync();
            target.Close();
            File.Move(tmp, destination, true);
            return Json(new { downloaded = true, destination, size });
        }
        finally { if (File.Exists(tmp)) File.Delete(tmp); }
    }

    public static async Task<string> UploadReleaseAsset(string repo, long releaseId, string localPath, string? assetName = null)
    {
        if (!File.Exists(localPath)) throw new FileNotFoundException("Release asset not found.", localPath);
        var fileInfo = new FileInfo(localPath);
        if (fileInfo.Length > 300_000_000) throw new ArgumentOutOfRangeException(nameof(localPath));
        var name = assetName ?? Path.GetFileName(localPath);
        string path = Repo(repo) + "/releases/" + releaseId + "/assets?name=" + Segment(name);
        using var req = new HttpRequestMessage(HttpMethod.Post, ApiUri(path, uploads: true));
        req.Headers.UserAgent.ParseAdd("ChatGptMcpTunnel-GitHub/1.0");
        req.Headers.Accept.ParseAdd("application/vnd.github+json");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await Token());
        req.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        req.Content = new StreamContent(File.OpenRead(localPath));
        req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        using var resp = await Client.SendAsync(req);
        string payload = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException("GitHub asset upload HTTP " + (int)resp.StatusCode + ": " + payload[..Math.Min(payload.Length, 1000)]);
        return Json(new { uploaded = true, asset = JsonNode.Parse(payload) });
    }

    public static async Task<string> RunGit(string cwd, params string[] args)
    {
        if (!Path.IsPathFullyQualified(cwd) || !Directory.Exists(cwd))
            throw new ArgumentException("Absolute existing working directory required.");
        using var proc = new Process { StartInfo = new ProcessStartInfo
        {
            FileName = "git", WorkingDirectory = cwd, UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true
        }};
        foreach (string arg in args) proc.StartInfo.ArgumentList.Add(arg);
        proc.StartInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";
        proc.StartInfo.Environment["GIT_PAGER"] = "cat";
        if (!proc.Start()) throw new InvalidOperationException("Git did not start");
        proc.StandardInput.Close();
        var output = proc.StandardOutput.ReadToEndAsync();
        var error = proc.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        try { await proc.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { proc.Kill(true); throw new InvalidOperationException("Git command timed out"); }
        var o = await output; var e = await error;
        return Json(new { exit_code = proc.ExitCode, stdout = o[..Math.Min(o.Length, 60000)],
                          stderr = e[..Math.Min(e.Length, 8000)], command = args[0] });
    }
}
