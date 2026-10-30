using System.Diagnostics;

namespace ChatGptMcpAgent;

// Persistent, local-only Web ChatGPT bridge. The user signs in to the
// dedicated browser profile once; all subsequent local chat turns are automatic.
// The legacy MCP Apps relay remains available, but claims prevent double delivery.
internal sealed class BrowserChatWorker : IDisposable
{
    private Process? _process;
    private DateTime _nextAttemptUtc;
    private readonly string _runtime = Path.Combine(AppContext.BaseDirectory, "Runtime");
    private string NodePath => Path.Combine(AppContext.BaseDirectory, "OpenAI", "node.exe");
    private string ScriptPath => Path.Combine(_runtime, "BrowserChatBridge.js");

    internal static bool HasPackagedFiles(string root) =>
        File.Exists(Path.Combine(root,"OpenAI","node.exe")) &&
        File.Exists(Path.Combine(root,"Runtime","BrowserChatBridge.js"));

    internal static void ShowSignIn(IWin32Window owner)
    {
        var root = AppContext.BaseDirectory;
        var node = Path.Combine(root, "OpenAI", "node.exe");
        var script = Path.Combine(root, "Runtime", "BrowserChatBridge.js");
        if (!File.Exists(node) || !File.Exists(script))
        {
            MessageBox.Show(owner, "Браузерный транспорт отсутствует в текущей сборке.",
                "ChatGPT MCP Tunnel", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            var info = new ProcessStartInfo(node) { WorkingDirectory = Path.GetDirectoryName(script)!,
                UseShellExecute = false, CreateNoWindow = true };
            info.ArgumentList.Add(script);
            info.ArgumentList.Add("--login");
            using var launched = Process.Start(info);
            if (launched is null) throw new IOException("Не удалось запустить профиль авторизации");
        }
        catch (Exception e) when (e is IOException or System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            MessageBox.Show(owner, e.Message, "ChatGPT MCP Tunnel", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    internal void EnsureRunning()
    {
        if (_process is { HasExited: false } || DateTime.UtcNow < _nextAttemptUtc)
            return;
        _nextAttemptUtc = DateTime.UtcNow.AddSeconds(15);
        try
        {
            if (_process is not null)
            {
                _process.Dispose();
                _process = null;
            }
            if (!File.Exists(NodePath) || !File.Exists(ScriptPath))
            {
                AgentStorage.Log("BrowserChatWorker: local runtime not installed.");
                return;
            }
            var info=new ProcessStartInfo(NodePath)
            {
                WorkingDirectory=_runtime,
                UseShellExecute=false,
                CreateNoWindow=true,
                WindowStyle=ProcessWindowStyle.Hidden
            };
            info.ArgumentList.Add(ScriptPath);
            _process=Process.Start(info);
            if (_process is null)
                AgentStorage.Log("BrowserChatWorker: process start failed.");
        }
        catch(Exception ex) when (ex is IOException or
            UnauthorizedAccessException or System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            AgentStorage.Log("BrowserChatWorker: "+ex.GetType().Name);
        }
    }

    internal void Stop()
    {
        try
        {
            if (_process is { HasExited: false })
                _process.Kill(entireProcessTree:false);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        { }
        finally { _process?.Dispose();_process=null; }
    }
    public void Dispose()=>Stop();
}
