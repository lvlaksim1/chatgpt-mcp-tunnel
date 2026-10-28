using System.Diagnostics;
using System.Drawing;
using System.IO.Pipes;
using System.Text.Json;

namespace ChatGptMcpAgent;

internal static class AgentUpdates
{
    internal const string Repository = "lvlaksim1/chatgpt-mcp-tunnel";
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(12) };

    static AgentUpdates()
    {
        Client.DefaultRequestHeaders.UserAgent.ParseAdd("ChatGptMcpAgent/1.0");
        Client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    }

    internal static bool IsAllowedInstaller(Uri url)
    {
        if (url.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(url.Host, "github.com", StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(url.Query) || !string.IsNullOrEmpty(url.Fragment)) return false;
        return System.Text.RegularExpressions.Regex.IsMatch(url.AbsolutePath,
            @"^/lvlaksim1/chatgpt-mcp-tunnel/releases/download/v[0-9]+\.[0-9]+\.[0-9]+/ChatGPT_MCP_Tunnel_Update_v[0-9]+\.[0-9]+\.[0-9]+\.exe$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    internal static async Task<(Version? Version, Uri? Installer)> GetLatestAsync()
    {
        using var resp = await Client.GetAsync(
            "https://api.github.com/repos/lvlaksim1/chatgpt-mcp-tunnel/releases/latest");
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return (null, null);
        resp.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var tag = json.RootElement.GetProperty("tag_name").GetString() ?? "";
        if (!tag.StartsWith('v') || !Version.TryParse(tag[1..], out var version)) return (null, null);
        var expected = "ChatGPT_MCP_Tunnel_Update_v" + version + ".exe";
        foreach (var asset in json.RootElement.GetProperty("assets").EnumerateArray())
        {
            if (asset.GetProperty("name").GetString() != expected) continue;
            if (!Uri.TryCreate(asset.GetProperty("browser_download_url").GetString(), UriKind.Absolute, out var address) ||
                !IsAllowedInstaller(address)) continue;
            return (version, address);
        }
        return (null, null);
    }

    internal static async Task RunUpdateAsync(int parentPid, Uri installerUrl, string pipeName)
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new UpdateWindow(parentPid, installerUrl, pipeName));
        await Task.CompletedTask;
    }

    internal static async Task DownloadAsync(Uri source, string dest, Action<int> progress)
    {
        using var response = await Client.GetAsync(source, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        await using var input = await response.Content.ReadAsStreamAsync();
        await StageInstallerAsync(input, dest, response.Content.Headers.ContentLength, progress);
    }

    // The staging FileStream MUST be fully disposed before moving .part to .exe.
    // Windows denies rename while our own FileShare.None writer still holds it.
    internal static async Task StageInstallerAsync(
        Stream input, string dest, long? expected, Action<int> progress)
    {
        const long limit = 300L * 1024 * 1024;
        if (expected is > limit)
            throw new InvalidDataException("Размер обновления превышает 300 МиБ.");
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        var partial = dest + ".part";
        try
        {
            long received = 0;
            await using (var output = new FileStream(
                partial, FileMode.Create, FileAccess.Write, FileShare.None,
                bufferSize: 65536, useAsync: true))
            {
                var buffer = new byte[65536];
                int count;
                while ((count = await input.ReadAsync(buffer.AsMemory())) > 0)
                {
                    received += count;
                    if (received > limit)
                        throw new InvalidDataException("Размер обновления превышает 300 МиБ.");
                    await output.WriteAsync(buffer.AsMemory(0, count));
                    if (expected > 0)
                        progress((int)Math.Min(100, received * 100 / expected.Value));
                }
                await output.FlushAsync();
            } // FileStream disposed: ONLY NOW is rename safe on Windows.

            if (expected is not null && received != expected.Value)
                throw new InvalidDataException("Обновление загружено не полностью.");
            File.Move(partial, dest, overwrite: true);
        }
        finally
        {
            if (File.Exists(partial)) File.Delete(partial);
        }
    }

    internal static async Task<int> TestDownloadStagingAsync()
    {
        var folder = Path.Combine(Path.GetTempPath(),
            "ChatGPTMCPAgent-Staging-Test-" + Guid.NewGuid().ToString("N"));
        var target = Path.Combine(folder, "update.exe");
        try
        {
            byte[] first = System.Security.Cryptography.RandomNumberGenerator.GetBytes(120003);
            await using (var stream = new MemoryStream(first, writable: false))
                await StageInstallerAsync(stream, target, first.Length, _ => { });
            if (!File.ReadAllBytes(target).SequenceEqual(first) ||
                File.Exists(target + ".part")) return 21;
            byte[] second = System.Security.Cryptography.RandomNumberGenerator.GetBytes(65545);
            await using (var stream = new MemoryStream(second, writable: false))
                await StageInstallerAsync(stream, target, second.Length, _ => { });
            if (!File.ReadAllBytes(target).SequenceEqual(second) ||
                File.Exists(target + ".part")) return 22;
            // Incomplete response must not corrupt a valid existing installer.
            try
            {
                await using var broken = new MemoryStream(new byte[5], writable: false);
                await StageInstallerAsync(broken, target, 10, _ => { });
                return 23;
            }
            catch (InvalidDataException) { }
            if (!File.ReadAllBytes(target).SequenceEqual(second) ||
                File.Exists(target + ".part")) return 24;
            // Ensure no stream leaked: exclusive read-write open must work.
            using (var unlocked = new FileStream(
                target, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
            return 0;
        }
        catch { return 25; }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

}

internal sealed class UpdateWindow : Form
{
    private readonly int _parentPid;
    private readonly Uri _source;
    private readonly string _pipe;
    private readonly Label _phase = new()
    {
        Left = 18, Top = 20, Width = 430, Height = 45, Text = "Подготовка обновления…"
    };
    private readonly ProgressBar _progress = new()
    {
        Left = 18, Top = 78, Width = 430, Height = 21, Minimum = 0, Maximum = 100
    };
    private readonly Button _close = new() { Text = "Закрыть", Left = 350, Top = 115,
        Width = 98, Height = 32, Visible = false };

    internal UpdateWindow(int parentPid, Uri source, string pipe)
    {
        _parentPid = parentPid;
        _source = source;
        _pipe = pipe;
        ClientSize = new Size(467, 160);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        Text = "Обновление ChatGPT MCP Agent";
        Icon = TrayIcons.AppIcon();
        Font = new Font("Segoe UI", 10);
        Controls.AddRange([_phase, _progress, _close]);
        _close.Click += (_, _) => Close();
        Shown += async (_, _) => await UpdateAsync();
    }

    private async Task UpdateAsync()
    {
        try
        {
            var output = Path.Combine(AgentStorage.UpdatesFolder, "ChatGPT_MCP_Agent_Update_Staged.exe");
            _phase.Text = "Скачивание обновления…";
            await AgentUpdates.DownloadAsync(_source, output, percent =>
            {
                if (!IsDisposed) _progress.Value = Math.Clamp(percent, 0, 100);
            });
            _phase.Text = "Завершение текущей версии…";
            using (var pipe = new NamedPipeClientStream(".", _pipe, PipeDirection.Out, PipeOptions.Asynchronous))
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                await pipe.ConnectAsync(timeout.Token);
                using var writer = new StreamWriter(pipe) { AutoFlush = true };
                await writer.WriteLineAsync("UPDATE_READY");
            }
            try
            {
                using var parent = Process.GetProcessById(_parentPid);
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
                await parent.WaitForExitAsync(timeout.Token);
            }
            catch (ArgumentException) { /* Parent exited before process lookup. */ }

            _phase.Text = "Установка обновления…";
            _progress.Style = ProgressBarStyle.Marquee;
            using var process = Process.Start(new ProcessStartInfo(output)
            {
                UseShellExecute = true,
                Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS"
            }) ?? throw new InvalidOperationException("Невозможно запустить установщик.");
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
                throw new InvalidOperationException("Ошибка установки: код " + process.ExitCode);
            _phase.Text = "Обновление установлено.";
            _progress.Style = ProgressBarStyle.Continuous;
            _progress.Value = 100;
            await Task.Delay(800);
            Close();
        }
        catch (Exception error)
        {
            _phase.Text = "Не удалось обновить программу: " + error.Message;
            _progress.Style = ProgressBarStyle.Continuous;
            _close.Visible = true;
            AgentStorage.Log("Update failed: " + error.GetType().Name);
        }
    }
}
