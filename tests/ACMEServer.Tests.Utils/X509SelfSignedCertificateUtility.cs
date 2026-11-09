using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Microsoft.IdentityModel.Tokens;

namespace Th11s.ACMEServer.Tests.Utils;

public class X509SelfSignedCertificateUtility
{
    public static X509Certificate2 CreateFakeRootCertificate(TimeProvider? timeProvider = null)
    {
        timeProvider ??= TimeProvider.System;

        var privateKey = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        var rootRequest = new CertificateRequest(
            new X500DistinguishedName("CN=Fake Root CA"),
            privateKey,
            HashAlgorithmName.SHA256);

        rootRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        rootRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        rootRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(rootRequest.PublicKey, false));

        var notBefore = timeProvider.GetUtcNow();
        var notAfter = notBefore.AddYears(10);

        // Create the self-signed root certificate
        var rootCertificate = rootRequest.CreateSelfSigned(notBefore, notAfter);
        return rootCertificate;
    }


    public static X509Certificate2 CreateCertificateSignedByRoot(X509Certificate2 rootCertificate, string csr, DateTimeOffset? notBefore = null)
    {
        notBefore ??= DateTimeOffset.UtcNow;

        var decodedCsr = Base64UrlEncoder.DecodeBytes(csr);
        var request = CertificateRequest.LoadSigningRequest(decodedCsr, HashAlgorithmName.SHA256, CertificateRequestLoadOptions.UnsafeLoadCertificateExtensions);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        // Add authority key identifier (linking to the root certificate)
        request.CertificateExtensions.Add(X509AuthorityKeyIdentifierExtension.CreateFromCertificate(rootCertificate, true, true));

        var signedCertificate = request.Create(rootCertificate, notBefore.Value, notBefore.Value.AddYears(1), Guid.NewGuid().ToByteArray());
        return signedCertificate;
    }
}
