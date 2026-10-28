using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace ChatGptMcpAgent;

internal sealed class AgentSettings
{
    public string Computer { get; set; } = Environment.MachineName;
    public string RouterUrl { get; set; } = "";
    public bool AutoStart { get; set; } = true;
}

internal static class AgentStorage
{
    private const string RunValueName = "ChatGPTMCPAgent";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    internal static string DataFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChatGPTMCPAgent");
    internal static string SettingsPath => Path.Combine(DataFolder, "settings.json");
    internal static string CredentialPath => Path.Combine(DataFolder, "credential.dpapi");
    internal static string UpdatesFolder => Path.Combine(DataFolder, "Updates");
    internal static string LogFolder => Path.Combine(DataFolder, "Logs");
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("ChatGPT-MCP-Agent-Credential-v1");

    internal static AgentSettings Load()
    {
        if (!File.Exists(SettingsPath)) return new AgentSettings();
        return JsonSerializer.Deserialize<AgentSettings>(File.ReadAllText(SettingsPath))
               ?? new AgentSettings();
    }

    internal static bool HasCredential() => File.Exists(CredentialPath);

    internal static string LoadCredential()
    {
        if (!File.Exists(CredentialPath)) return "";
        var plaintext = ProtectedData.Unprotect(File.ReadAllBytes(CredentialPath),
            Entropy, DataProtectionScope.CurrentUser);
        try { return Encoding.UTF8.GetString(plaintext); }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }

    internal static void Save(AgentSettings settings, string? replacementSecret)
    {
        if (replacementSecret is not null && replacementSecret.Length < 16)
            throw new ArgumentException("Ключ подключения должен содержать не менее 16 символов.");
        Directory.CreateDirectory(DataFolder);
        // Persist credentials before settings, so incomplete saves never advertise a usable profile.
        if (replacementSecret is not null)
        {
            var plain = Encoding.UTF8.GetBytes(replacementSecret);
            try
            {
                var encrypted = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);
                var tmp = CredentialPath + ".tmp";
                File.WriteAllBytes(tmp, encrypted);
                File.Move(tmp, CredentialPath, overwrite: true);
            }
            finally { CryptographicOperations.ZeroMemory(plain); }
        }
        var tmpSettings = SettingsPath + ".tmp";
        File.WriteAllText(tmpSettings, JsonSerializer.Serialize(settings,
            new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
        File.Move(tmpSettings, SettingsPath, overwrite: true);
        SetAutoStart(settings.AutoStart);
    }

    internal static void SetAutoStart(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true)
            ?? throw new InvalidOperationException("Не удалось открыть автозапуск Windows.");
        if (enabled)
            key.SetValue(RunValueName, "\"" + Application.ExecutablePath + "\" --minimized", RegistryValueKind.String);
        else
            key.DeleteValue(RunValueName, throwOnMissingValue: false);
    }

    internal static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(LogFolder);
            var file = Path.Combine(LogFolder, "agent.log");
            if (File.Exists(file) && new FileInfo(file).Length > 2_000_000)
                File.Move(file, file + ".old", overwrite: true);
            File.AppendAllText(file, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {message}{Environment.NewLine}");
        }
        catch { /* Logging must not terminate the agent. */ }
    }

    internal static bool ValidateRouter(string text, out string reason)
    {
        reason = "";
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("ws" or "wss") ||
            uri.AbsolutePath.TrimEnd('/') != "/agent" ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            reason = "Укажите адрес вида wss://адрес/agent либо ws://частный-адрес/agent.";
            return false;
        }
        if (uri.Scheme == "ws" && !IsPrivateHost(uri.Host))
        {
            reason = "Обычный ws:// разрешён только в частной сети. Для Интернета используйте wss://.";
            return false;
        }
        return true;
    }

    private static bool IsPrivateHost(string name)
    {
        if (string.Equals(name, "localhost", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith(".ts.net", StringComparison.OrdinalIgnoreCase) ||
            (!name.Contains('.') && !name.Contains(':'))) return true;
        if (!System.Net.IPAddress.TryParse(name, out var ip)) return false;
        if (System.Net.IPAddress.IsLoopback(ip)) return true;
        var bytes = ip.GetAddressBytes();
        if (bytes.Length != 4) return false;
        return bytes[0] == 10 || bytes[0] == 192 && bytes[1] == 168 ||
               bytes[0] == 172 && bytes[1] is >= 16 and <= 31 ||
               bytes[0] == 100 && bytes[1] is >= 64 and <= 127;
    }

    internal static int RunSelfTest()
    {
        try
        {
            var test = Path.Combine(Path.GetTempPath(), "mcp-agent-selftest-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(test);
            try
            {
                var secret = "local-test-" + Guid.NewGuid().ToString("N");
                var cipher = ProtectedData.Protect(Encoding.UTF8.GetBytes(secret), Entropy, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(Path.Combine(test, "key"), cipher);
                if (Encoding.UTF8.GetString(ProtectedData.Unprotect(
                    File.ReadAllBytes(Path.Combine(test, "key")), Entropy,
                    DataProtectionScope.CurrentUser)) != secret)
                    return 3;
                if (!ValidateRouter("ws://127.0.0.1:55201/agent", out _) ||
                    !ValidateRouter("ws://100.103.25.9:55201/agent", out _) ||
                    !ValidateRouter("wss://router.example/agent", out _) ||
                    ValidateRouter("ws://8.8.8.8:55201/agent", out _) ||
                    ValidateRouter("https://router.example/agent", out _))
                    return 4;
                return 0;
            }
            finally { Directory.Delete(test, recursive: true); }
        }
        catch { return 5; }
    }
}
