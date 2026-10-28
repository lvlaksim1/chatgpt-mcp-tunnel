using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Principal;

namespace ChatGptMcpAgent;

/// <summary>
/// Only installs a narrowly-scoped firewall rule after the user clicks the
/// server settings button and approves Windows elevation. Does not alter
/// network profiles or router/firewall defaults.
/// </summary>
internal static class LanFirewall
{
    private const string RuleName = "ChatGPTMCPRouterLiteLAN";

    internal static string LocalNetwork(string address)
    {
        if (!IPAddress.TryParse(address, out var ip) ||
            ip.AddressFamily != AddressFamily.InterNetwork)
            throw new ArgumentException("Требуется локальный IPv4-адрес.");
        var bytes = ip.GetAddressBytes();
        bool isPrivate = bytes[0] == 10 ||
            bytes[0] == 172 && bytes[1] is >= 16 and <= 31 ||
            bytes[0] == 192 && bytes[1] == 168;
        if (!isPrivate)
            throw new ArgumentException("Доступ клиентов разрешается только для частной локальной сети.");
        var addressInfo = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                        n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .FirstOrDefault(a => a.Address.Equals(ip));
        if (addressInfo is null)
            throw new ArgumentException("Указанный IP-адрес не назначен этому компьютеру.");
        int prefix = addressInfo.PrefixLength;
        if (prefix is < 16 or > 30)
            throw new ArgumentException("Неподдерживаемая маска локальной сети.");
        var network = new byte[4];
        var bits = prefix;
        for (var i = 0; i < 4; i++)
        {
            var count = Math.Clamp(bits, 0, 8);
            var mask = count == 0 ? 0 : 0xff << (8 - count) & 0xff;
            network[i] = (byte)(bytes[i] & mask);
            bits -= 8;
        }
        return new IPAddress(network) + "/" + prefix;
    }

    internal static async Task<bool> RequestSetupAsync(Form owner, string address)
    {
        // Calculate/validate subnet before prompting for elevation.
        var network = LocalNetwork(address);
        var confirm = MessageBox.Show(owner,
            "Разрешить другим компьютерам локальной сети " + network +
            " подключаться к Router Lite на " + address + ":55201?\n\n" +
            "Windows запросит разрешение администратора. Доступ из Интернета не открывается.",
            "Разрешить доступ клиентам", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return false;
        using var app = Process.Start(new ProcessStartInfo(Application.ExecutablePath)
        {
            UseShellExecute = true,
            Verb = "runas",
            Arguments = "--configure-lan-firewall " + address,
            WorkingDirectory = AppContext.BaseDirectory
        }) ?? throw new InvalidOperationException("Не удалось открыть запрос прав администратора.");
        await app.WaitForExitAsync();
        if (app.ExitCode != 0)
            throw new InvalidOperationException(
                "Не удалось настроить брандмауэр Windows. Код: " + app.ExitCode);
        MessageBox.Show(owner,
            "Для Router Lite разрешён входящий TCP-порт 55201 только из " + network + ".\n" +
            "Перезапуск туннеля не требуется.",
            "Брандмауэр настроен", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return true;
    }

    internal static int ConfigureElevated(string address)
    {
        try
        {
            // The elevation is expressly triggered by a settings button.
            using var identity = WindowsIdentity.GetCurrent();
            if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
                return 30;
            var network = LocalNetwork(address);
            var executable = Path.Combine(AppContext.BaseDirectory,
                "Runtime", "ChatGptMcpTunnel.exe");
            if (!File.Exists(executable)) return 31;
            _ = Netsh(["advfirewall", "firewall", "delete", "rule", "name=" + RuleName]);
            return Netsh([
                "advfirewall", "firewall", "add", "rule",
                "name=" + RuleName,
                "dir=in", "action=allow", "enable=yes", "profile=any",
                "protocol=TCP", "localport=55201",
                "localip=" + address, "remoteip=" + network,
                "program=" + executable
            ]) == 0 ? 0 : 32;
        }
        catch { return 33; }
    }

    private static int Netsh(string[] arguments)
    {
        var info = new ProcessStartInfo("netsh.exe")
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (var arg in arguments) info.ArgumentList.Add(arg);
        using var process = Process.Start(info)
            ?? throw new InvalidOperationException("Не удалось запустить netsh.");
        process.StandardOutput.ReadToEnd();
        process.StandardError.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode;
    }

    internal static bool TestLocalNetwork()
    {
        var actual = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork &&
                                 a.PrefixLength is >= 16 and <= 30 &&
                                 a.Address.GetAddressBytes()[0] == 192 &&
                                 a.Address.GetAddressBytes()[1] == 168);
        if (actual is null) return true; // Windows CI runner may have only 10.*.
        var computed = LocalNetwork(actual.Address.ToString());
        var split = computed.Split('/');
        return split.Length == 2 &&
               int.TryParse(split[1], out var prefix) &&
               prefix == actual.PrefixLength &&
               IPAddress.TryParse(split[0], out _);
    }
}
