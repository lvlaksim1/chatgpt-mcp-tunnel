using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

// Disposable certificate fixture for localhost TLS regression only.
// Does not modify the Windows certificate store or production agent settings.
var path = Environment.GetEnvironmentVariable("TEST_CERT_FILE")
    ?? throw new InvalidOperationException("TEST_CERT_FILE required.");
var password = Environment.GetEnvironmentVariable("TEST_CERT_PASS")
    ?? throw new InvalidOperationException("TEST_CERT_PASS required.");
using var key = RSA.Create(2048);
var request = new CertificateRequest("CN=localhost", key,
    HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
var san = new SubjectAlternativeNameBuilder();
san.AddDnsName("localhost");
request.CertificateExtensions.Add(san.Build());
request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
    new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, true));
using var certificate = request.CreateSelfSigned(
    DateTimeOffset.UtcNow.AddMinutes(-2), DateTimeOffset.UtcNow.AddDays(2));
File.WriteAllBytes(path, certificate.Export(X509ContentType.Pfx, password));
Console.WriteLine("DISPOSABLE_TLS_CERT_READY");
