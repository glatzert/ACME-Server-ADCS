using System.Security.Cryptography.X509Certificates;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Services;

public interface ICertificateIssuer
{
    Task<(X509Certificate2Collection? Certificates, AcmeError? Error)> IssueCertificateAsync(ProfileName profile, string csr, CancellationToken cancellationToken);
    Task RevokeCertificateAsync(ProfileName profile, X509Certificate2 certificate, int? reason, CancellationToken cancellationToken);
}
