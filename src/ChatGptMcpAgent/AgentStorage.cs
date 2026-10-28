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
    public string Role { get; set; } = "client";
    public string TunnelId { get; set; } = "";
    public string ListenAddress { get; set; } = "127.0.0.1";
}

internal static class AgentStorage
{
    private const string RunValueName = "ChatGPTMCPAgent";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    internal static string DataFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChatGPTMCPAgent");
    internal static string SettingsPath => Path.Combine(DataFolder, "settings.json");
    internal static string CredentialPath => Path.Combine(DataFolder, "credential.dpapi");
    internal static string ServerKeyPath => Path.Combine(DataFolder, "server-key.dpapi");
    internal static string ServerApiKeyPath => Path.Combine(DataFolder, "openai-key.dpapi");
    internal static string ServerRoot => Path.Combine(DataFolder, "Server");
    internal static string ExistingTunnelId()
    {
        var old = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChatGptMcpTunnel", "tunnel-id.txt");
        return File.Exists(old) ? File.ReadAllText(old).Trim() : "";
    }
    internal static bool IsValidTunnelId(string? id) =>
        id is { Length: > 7 and <= 130 } &&
        System.Text.RegularExpressions.Regex.IsMatch(id, @"^tunnel_(?:[a-z0-9]{4}_)?[a-z0-9]{32}$");
    internal static bool IsValidScope(string? scope, string prefix) =>
        !string.IsNullOrWhiteSpace(scope) && scope.StartsWith(prefix, StringComparison.Ordinal) &&
        scope.Length <= 150 &&
        scope.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-' or '.' or ':');
    internal static string SelectedRole
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\ChatGPTMCPAgent");
            var role = key?.GetValue("Role") as string;
            return role is "server" or "client" ? role : "client";
        }
    }
    internal static string UpdatesFolder => Path.Combine(DataFolder, "Updates");
    internal static string LogFolder => Path.Combine(DataFolder, "Logs");
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("ChatGPT-MCP-Agent-Credential-v1");

    internal static AgentSettings Load()
    {
        AgentSettings settings = File.Exists(SettingsPath)
            ? JsonSerializer.Deserialize<AgentSettings>(File.ReadAllText(SettingsPath))
                ?? new AgentSettings()
            : new AgentSettings();
        settings.Role = SelectedRole;
        if (settings.Role == "server" && string.IsNullOrWhiteSpace(settings.TunnelId))
            settings.TunnelId = ExistingTunnelId();
        // The installer's autostart checkbox is authoritative for both roles.
        settings.AutoStart = Registry.CurrentUser.OpenSubKey(RunKey)?
            .GetValue(RunValueName) is string;
        return settings;
    }

    internal static bool HasCredential() =>
        File.Exists(SelectedRole == "server" ? ServerKeyPath : CredentialPath);

    internal static bool HasServerApiKey() => File.Exists(ServerApiKeyPath);
    internal static string LoadServerApiKey() => LoadProtected(ServerApiKeyPath);
    internal static string LoadServerAgentKey() => LoadProtected(ServerKeyPath);

    private static string LoadProtected(string path)
    {
        if (!File.Exists(path)) return "";
        var plaintext = ProtectedData.Unprotect(File.ReadAllBytes(path),
            Entropy, DataProtectionScope.CurrentUser);
        try { return Encoding.UTF8.GetString(plaintext); }
        finally { CryptographicOperations.ZeroMemory(plaintext); }
    }
    private static void SaveProtected(string path, string secret)
    {
        var plain = Encoding.UTF8.GetBytes(secret);
        try
        {
            var encrypted = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);
            var tmp = path + ".tmp";
            File.WriteAllBytes(tmp, encrypted);
            File.Move(tmp, path, overwrite: true);
        }
        finally { CryptographicOperations.ZeroMemory(plain); }
    }
    internal static void SaveServer(AgentSettings settings, string? agentKey, string? openAiKey)
    {
        settings.Role = "server";
        if (!IsValidTunnelId(settings.TunnelId))
            throw new ArgumentException("Укажите действительный ID туннеля OpenAI.");
        if (agentKey is { Length: < 16 })
            throw new ArgumentException("Ключ агентов должен содержать не менее 16 символов.");
        if (openAiKey is { Length: < 12 })
            throw new ArgumentException("Ключ OpenAI слишком короткий.");
        Directory.CreateDirectory(DataFolder);
        if (agentKey is not null) SaveProtected(ServerKeyPath, agentKey);
        if (openAiKey is not null) SaveProtected(ServerApiKeyPath, openAiKey);
        WriteSettings(settings);
    }
    private static void WriteSettings(AgentSettings settings)
    {
        Directory.CreateDirectory(DataFolder);
        var temp = SettingsPath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings,
            new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
        File.Move(temp, SettingsPath, overwrite: true);
        SetAutoStart(settings.AutoStart);
    }

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
        settings.Role = "client";
        WriteSettings(settings);
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
                var sameTunnelId = "tunnel_" + new string('a', 32);
                if (!IsValidTunnelId(sameTunnelId) ||
                    IsValidTunnelId("tunnel_invalid") ||
                    !IsValidTunnelId("tunnel_abcd_" + new string('9', 32)))
                    return 7;
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
