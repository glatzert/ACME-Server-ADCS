using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Services;

public interface ICertificateIssuer
{
    Task<(byte[]? Certificates, AcmeError? Error)> IssueCertificate(ProfileName profile, string csr, CancellationToken cancellationToken);
}
