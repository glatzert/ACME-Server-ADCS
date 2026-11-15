using System.Security.Cryptography.X509Certificates;

using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Services;

namespace Th11s.ACMEServer.Tests.Utils.Fakes;

internal class FakeCertificateIssuer : ICertificateIssuer
{
    public Task<CertificateIssuanceResult> IssueCertificateAsync(ProfileName profile, string csr, CancellationToken cancellationToken)
    {
        // Create a self-signed certification root
        var rootRsa = X509SelfSignedCertificateUtility.CreateFakeRootCertificate();
        var leafCert = X509SelfSignedCertificateUtility.CreateCertificateSignedByRoot(rootRsa, csr);

        var chain = CreateCertificateChain(leafCert, rootRsa);
        return Task.FromResult(new CertificateIssuanceResult(chain));
    }

    private static X509Certificate2Collection CreateCertificateChain(X509Certificate2 leafCertificate, X509Certificate2 rootCertificate)
    {
        // Loading the exported certificate will strip the private key
        var keylessRoot = X509CertificateLoader.LoadCertificate(rootCertificate.Export(X509ContentType.Cert));

        var chain = new X509Certificate2Collection
        {
            leafCertificate,
            keylessRoot
        };

        return chain;
    }


    public Task RevokeCertificateAsync(ProfileName profile, X509Certificate2 certificate, Dictionary<string, string> issuanceMetadata, int? reason, CancellationToken cancellationToken)
        => Task.CompletedTask;
}