using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Services;
using Th11s.ACMEServer.Model.Primitives;
using Microsoft.IdentityModel.Tokens;

namespace Th11s.AcmeServer.Tests.Integration.Fakes;

internal class FakeCertificateIssuer : ICertificateIssuer
{
    public Task<(X509Certificate2Collection? Certificates, AcmeError? Error)> IssueCertificate(ProfileName profile, string csr, CancellationToken cancellationToken)
    {
        // Create a self-signed certification root
        var rootRsa = CreateFakeRootCertificate();
        var leafCert = CreateCertificateSignedByRoot(rootRsa, csr);

        var chain = CreateCertificateChain(leafCert, rootRsa);
        return Task.FromResult(((X509Certificate2Collection?)chain, (AcmeError?)null));

        //// Create a self-signed certificate for testing purposes
        //using var rsa = RSA.Create(2048);
        //var request = new CertificateRequest(
        //    new X500DistinguishedName($"CN=example.com"),
        //    rsa,
        //    HashAlgorithmName.SHA256,
        //    RSASignaturePadding.Pkcs1);

        //// Add extensions to the certificate (optional)
        //request.CertificateExtensions.Add(
        //    new X509BasicConstraintsExtension(false, false, 0, false));
        //request.CertificateExtensions.Add(
        //    new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
        //request.CertificateExtensions.Add(
        //    new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        //// Create the self-signed certificate
        //var notBefore = DateTimeOffset.UtcNow;
        //var notAfter = notBefore.AddYears(1);
        //var certificate = request.CreateSelfSigned(notBefore, notAfter);

        //var result = new X509Certificate2Collection(certificate);

        //// Export the certificate with the private key
        //return Task.FromResult(((X509Certificate2Collection?)result, (AcmeError?)null));
    }

    private static X509Certificate2Collection CreateCertificateChain(X509Certificate2 leafCertificate, X509Certificate2 rootCertificate)
    {
        var keylessRoot = new X509Certificate2(rootCertificate.Export(X509ContentType.Cert));

        var chain = new X509Certificate2Collection
        {
            leafCertificate,
            keylessRoot
        };

        return chain;
    }

    private static X509Certificate2 CreateCertificateSignedByRoot(X509Certificate2 rootCertificate, string csr)
    {
        var decodedCsr = Base64UrlEncoder.DecodeBytes(csr);
        var request = CertificateRequest.LoadSigningRequest(decodedCsr, HashAlgorithmName.SHA256, CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        // Add authority key identifier (linking to the root certificate)
        request.CertificateExtensions.Add(X509AuthorityKeyIdentifierExtension.CreateFromCertificate(rootCertificate, true, true));

        var signedCertificate = request.Create(rootCertificate, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(5), Guid.NewGuid().ToByteArray());
        return signedCertificate;
    }

    private static X509Certificate2 CreateFakeRootCertificate()
    {
        var privateKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        var rootRequest = new CertificateRequest(
            new X500DistinguishedName("CN=Fake Root CA"),
            privateKey,
            HashAlgorithmName.SHA256);

        rootRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        rootRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        rootRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(rootRequest.PublicKey, false));

        // Create the self-signed root certificate
        var rootCertificate = rootRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(10));
        return rootCertificate;
    }
}