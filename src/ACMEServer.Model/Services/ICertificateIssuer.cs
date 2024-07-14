using System.Threading;
using System.Threading.Tasks;

namespace Th11s.ACMEServer.Model.Services
{
    public interface ICertificateIssuer
    {
        Task<(byte[]? Certificates, AcmeError? Error)> IssueCertificate(string csr, CancellationToken cancellationToken);
    }
}
