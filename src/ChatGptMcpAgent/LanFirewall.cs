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

    // Explicit opt-in for direct WAN TLS. Router forwarding must be configured separately;
    // never change the router or enable plaintext public ingress automatically.
    internal static async Task<bool> RequestWanSetupAsync(Form owner, string localAddress)
    {
        if (!AgentStorage.Load().DirectWanEnabled || DirectTls.Fingerprint().Length != 64)
            throw new InvalidOperationException("Сначала сохраните настройки WSS и сертификат.");
        VerifyLocalAddress(localAddress);
        if (MessageBox.Show(owner,
            "Открыть входящий TCP-порт 55202 для зашифрованного WSS на " +
            localAddress + "?\\n\\n" +
            "Порт будет доступен из Интернета после ручной настройки переадресации на роутере. " +
            "Удостоверьтесь, что клиент получил правильный отпечаток сертификата и ключ.",
            "Доступ через Интернет", MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning) != DialogResult.Yes) return false;
        using var process = Process.Start(new ProcessStartInfo(Application.ExecutablePath)
        {
            UseShellExecute = true, Verb = "runas",
            Arguments = "--configure-wan-firewall " + localAddress,
            WorkingDirectory = AppContext.BaseDirectory
        }) ?? throw new InvalidOperationException("Не удалось запросить права администратора.");
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
            throw new InvalidOperationException("Не удалось изменить брандмауэр. Код: " + process.ExitCode);
        MessageBox.Show(owner,
            "Входящий TCP 55202 разрешён для TLS. Настройте на роутере ручную переадресацию " +
            "внешнего TCP 55202 на " + localAddress + ":55202. Обычный порт 55201 не перенаправляйте.",
            "Доступ настроен", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return true;
    }

    private static void VerifyLocalAddress(string address)
    {
        if (!IPAddress.TryParse(address, out var ip) ||
            ip.AddressFamily != AddressFamily.InterNetwork ||
            !NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .Any(a => a.Address.Equals(ip)))
            throw new ArgumentException("Адрес WSS должен быть назначен сетевому интерфейсу этого ПК.");
    }

    internal static int ConfigureElevatedWan(string localAddress)
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
                return 50;
            VerifyLocalAddress(localAddress);
            if (!AgentStorage.Load().DirectWanEnabled || DirectTls.Fingerprint().Length != 64)
                return 51;
            var executable = Path.Combine(AppContext.BaseDirectory, "Runtime", "ChatGptMcpTunnel.exe");
            if (!File.Exists(executable)) return 52;
            const string wanRule = "ChatGPTMCPRouterLiteWSS";
            _ = Netsh(["advfirewall", "firewall", "delete", "rule", "name=" + wanRule]);
            return Netsh([
                "advfirewall", "firewall", "add", "rule",
                "name=" + wanRule, "dir=in", "action=allow",
                "enable=yes", "profile=any", "protocol=TCP",
                "localport=55202", "localip=" + localAddress,
                "remoteip=any", "program=" + executable
            ]) == 0 ? 0 : 53;
        }
        catch { return 54; }
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


    // CGNAT 100.64/10: Tailscale virtual IPs, not ordinary RFC1918 LAN.
    internal static bool IsTailnetIpv4(string text)
    {
        if (!IPAddress.TryParse(text, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork)
            return false;
        var bytes = ip.GetAddressBytes();
        return bytes[0] == 100 && bytes[1] is >= 64 and <= 127;
    }

    private static string VerifiedServerTailnetIp(string address)
    {
        if (!IsTailnetIpv4(address))
            throw new ArgumentException("Нужен IPv4-адрес Tailscale вида 100.x.y.z.");
        bool local = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                (n.Name.Contains("Tailscale", StringComparison.OrdinalIgnoreCase) ||
                 n.Description.Contains("Tailscale", StringComparison.OrdinalIgnoreCase)))
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Any(n => n.Address.ToString() == address);
        if (!local) throw new InvalidOperationException(
            "Адрес не назначен работающему сетевому адаптеру Tailscale.");
        return address;
    }

    private static string VerifiedTailnetPeer(string peer, string server)
    {
        if (!IsTailnetIpv4(peer) || peer == server)
            throw new ArgumentException("Укажите отдельный адрес Tailscale удалённого клиента.");
        return peer;
    }

    internal static async Task<bool> RequestTailnetSetupAsync(Form owner, string address)
    {
        var server = VerifiedServerTailnetIp(address);
        using var form = new Form
        {
            Text = "Доступ через Tailscale", Width = 455, Height = 215,
            FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false,
            MinimizeBox = false, ShowInTaskbar = false,
            StartPosition = FormStartPosition.CenterParent
        };
        form.Controls.Add(new Label
        {
            Left = 18, Top = 14, Width = 405, Height = 50,
            Text = "Введите адрес Tailscale удалённого компьютера. Доступ получит только этот адрес, не вся виртуальная сеть."
        });
        form.Controls.Add(new Label
        {
            Left = 18, Top = 70, Width = 405, Height = 23,
            Text = "Адрес клиента (100.x.y.z):"
        });
        var input = new TextBox { Left = 18, Top = 95, Width = 400 };
        form.Controls.Add(input);
        var ok = new Button
        {
            Text = "Продолжить", Left = 184, Top = 129,
            Width = 112, DialogResult = DialogResult.OK
        };
        var cancel = new Button
        {
            Text = "Отмена", Left = 306, Top = 129,
            Width = 112, DialogResult = DialogResult.Cancel
        };
        form.Controls.Add(ok);
        form.Controls.Add(cancel);
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        if (form.ShowDialog(owner) != DialogResult.OK) return false;
        var peer = VerifiedTailnetPeer(input.Text.Trim(), server);
        if (MessageBox.Show(owner,
            "Разрешить соединение от " + peer + " к " + server +
            ":55201 по Tailscale?\\n\\nРазрешение относится только к этому клиенту. " +
            "Windows запросит права администратора.",
            "Ограниченный доступ", MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) != DialogResult.Yes) return false;
        using var process = Process.Start(new ProcessStartInfo(Application.ExecutablePath)
        {
            UseShellExecute = true, Verb = "runas",
            Arguments = "--configure-tailnet-firewall " + server + " " + peer,
            WorkingDirectory = AppContext.BaseDirectory
        }) ?? throw new InvalidOperationException("Не удалось запросить права администратора.");
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
            throw new InvalidOperationException("Не удалось настроить брандмауэр. Код: " + process.ExitCode);
        MessageBox.Show(owner, "Разрешён TCP 55201 только от " + peer + " к " + server + ".",
            "Доступ через Tailscale", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return true;
    }

    internal static int ConfigureElevatedTailnet(string server, string peer)
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
                return 40;
            VerifiedServerTailnetIp(server);
            VerifiedTailnetPeer(peer, server);
            var executable = Path.Combine(AppContext.BaseDirectory,
                "Runtime", "ChatGptMcpTunnel.exe");
            if (!File.Exists(executable)) return 41;
            var rule = "ChatGPTMCPRouterLiteTailnet-" + server + "-" + peer;
            _ = Netsh(["advfirewall", "firewall", "delete", "rule", "name=" + rule]);
            return Netsh([
                "advfirewall", "firewall", "add", "rule",
                "name=" + rule, "dir=in", "action=allow",
                "enable=yes", "profile=any", "protocol=TCP",
                "localport=55201", "localip=" + server,
                "remoteip=" + peer, "program=" + executable
            ]) == 0 ? 0 : 42;
        }
        catch { return 43; }
    }

    internal static bool TestTailnetRanges()
    {
        return IsTailnetIpv4("100.64.0.1") && IsTailnetIpv4("100.127.255.254") &&
               !IsTailnetIpv4("100.63.255.255") && !IsTailnetIpv4("100.128.0.0") &&
               !IsTailnetIpv4("192.168.0.3") && !IsTailnetIpv4("127.0.0.1") &&
               VerifiedTailnetPeer("100.65.3.4", "100.65.3.5") == "100.65.3.4";
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
