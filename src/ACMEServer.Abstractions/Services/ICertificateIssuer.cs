using System.Security.Cryptography.X509Certificates;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Services;

public interface ICertificateIssuer
{
    Task<CertificateIssuanceResult> IssueCertificateAsync(ProfileName profile, string csr, CancellationToken cancellationToken);
    Task RevokeCertificateAsync(ProfileName profile, X509Certificate2 certificate, Dictionary<string, string> issuanceMetadata, int? reason, CancellationToken cancellationToken);
}
