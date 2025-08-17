using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Services;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.AcmeServer.Tests.Integration;

internal class FakeCertificateIssuer : ICertificateIssuer
{
    public Task<(X509Certificate2Collection? Certificates, AcmeError? Error)> IssueCertificate(ProfileName profile, string csr, CancellationToken cancellationToken)
    {
        // Create a self-signed certificate for testing purposes
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            new X500DistinguishedName($"CN=example.com"),
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        // Add extensions to the certificate (optional)
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
        request.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        // Create the self-signed certificate
        var notBefore = DateTimeOffset.UtcNow;
        var notAfter = notBefore.AddYears(1);
        var certificate = request.CreateSelfSigned(notBefore, notAfter);

        var result = new X509Certificate2Collection(certificate);

        // Export the certificate with the private key
        return Task.FromResult(((X509Certificate2Collection?)result, (AcmeError?)null));
    }
}