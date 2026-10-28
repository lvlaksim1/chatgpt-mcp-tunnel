using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ChatGptMcpAgent;

// TLS identity is local to the server. Private key is inside an encrypted PFX;
// its random password is protected with Windows DPAPI for the current user.
internal static class DirectTls
{
    internal static string PfxPath => Path.Combine(AgentStorage.DataFolder, "direct-wan.pfx");
    private static string HostPath => Path.Combine(AgentStorage.DataFolder, "direct-wan-host.txt");

    internal static string NormalizeFingerprint(string? text)
    {
        var cleaned = (text ?? "").Replace(":", "").Replace(" ", "").Replace("-", "").Trim();
        if (cleaned.Length != 64 || !cleaned.All(Uri.IsHexDigit))
            throw new ArgumentException("Отпечаток сертификата: ровно 64 шестнадцатеричных знака SHA-256.");
        return cleaned.ToUpperInvariant();
    }

    internal static bool CheckFingerprint(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length == 64 && value.All(Uri.IsHexDigit);

    internal static string ValidatePublicHost(string text)
    {
        var name = text.Trim();
        if (name.Length is < 4 or > 253 || name.Any(c => c is '/' or '\\' or ':' or '@' or ' '))
            throw new ArgumentException("Укажите публичный IPv4-адрес или доменное имя DOMKAT.");
        if (IPAddress.TryParse(name, out var ip))
        {
            if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork ||
                IPAddress.IsLoopback(ip) || ip.IsIPv6LinkLocal)
                throw new ArgumentException("Для внешнего соединения требуется публичный IPv4.");
            var b = ip.GetAddressBytes();
            if (b[0] == 10 || b[0] == 127 || b[0] == 0 || b[0] >= 224 ||
                b[0] == 192 && b[1] == 168 || b[0] == 172 && b[1] is >= 16 and <= 31 ||
                b[0] == 100 && b[1] is >= 64 and <= 127 ||
                b[0] == 169 && b[1] == 254 || b[0] == 192 && b[1] == 0 && b[2] == 2 ||
                b[0] == 198 && b[1] == 51 && b[2] == 100 ||
                b[0] == 203 && b[1] == 0 && b[2] == 113 ||
                b[0] == 198 && b[1] is 18 or 19 ||
                b[0] == 192 && b[1] == 0 && b[2] == 0 ||
                b[0] == 255 || b[0] == 172 && b[1] is >= 16 and <= 31)
                throw new ArgumentException("Этот адрес не является публичным IPv4.");
        }
        else if (Uri.CheckHostName(name) != UriHostNameType.Dns || !name.Contains('.'))
            throw new ArgumentException("Укажите корректное доменное имя или публичный IPv4.");
        return name;
    }

    internal static void EnsureCertificate(string publicHost)
    {
        publicHost = ValidatePublicHost(publicHost);
        if (File.Exists(PfxPath) && File.Exists(HostPath) &&
            string.Equals(File.ReadAllText(HostPath).Trim(), publicHost, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(AgentStorage.LoadTlsPassword()))
        {
            using var existing = new X509Certificate2(PfxPath, AgentStorage.LoadTlsPassword(), X509KeyStorageFlags.EphemeralKeySet);
            if (existing.NotAfter.ToUniversalTime() > DateTime.UtcNow.AddDays(30)) return;
        }

        var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var temp = PfxPath + ".tmp";
        Directory.CreateDirectory(AgentStorage.DataFolder);
        File.WriteAllBytes(temp, GeneratePfx(publicHost, password));
        AgentStorage.SaveTlsPassword(password);
        File.Move(temp, PfxPath, true);
        File.WriteAllText(HostPath, publicHost);
    }

    internal static string Fingerprint()
    {
        if (!File.Exists(PfxPath)) return "";
        using var certificate = new X509Certificate2(PfxPath, AgentStorage.LoadTlsPassword());
        return Convert.ToHexString(SHA256.HashData(certificate.RawData));
    }

    private static byte[] GeneratePfx(string host, string password)
    {
        using var key = RSA.Create(3072);
        var request = new CertificateRequest("CN=ChatGPT MCP Tunnel", key,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder();
        if (IPAddress.TryParse(host, out var ip)) names.AddIpAddress(ip);
        else names.AddDnsName(host);
        request.CertificateExtensions.Add(names.Build());
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        var usages = new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") };
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(usages, true));
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddYears(3));
        return certificate.Export(X509ContentType.Pfx, password);
    }

    internal static bool SelfTest()
    {
        var hash = new string('A', 64);
        if (NormalizeFingerprint(string.Join(":", Enumerable.Repeat("AA", 32))) != hash ||
            CheckFingerprint("abcd") ||
            ValidatePublicHost("8.8.8.8") != "8.8.8.8" ||
            ValidatePublicHost("router.example.com") != "router.example.com")
            return false;
        using var certificate = new X509Certificate2(
            GeneratePfx("8.8.8.8", "disposable-test-password"), "disposable-test-password");
        return certificate.HasPrivateKey &&
               certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>()
                   .Any(x => x.EnhancedKeyUsages.Cast<Oid>()
                       .Any(oid => oid.Value == "1.3.6.1.5.5.7.3.1")) &&
               SHA256.HashData(certificate.RawData).Length == 32;
    }
}
