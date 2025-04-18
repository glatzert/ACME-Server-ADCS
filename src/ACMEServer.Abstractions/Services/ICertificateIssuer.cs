using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services;

public interface ICertificateIssuer
{
    Task<(byte[]? Certificates, AcmeError? Error)> IssueCertificate(string csr, CancellationToken cancellationToken);
}
