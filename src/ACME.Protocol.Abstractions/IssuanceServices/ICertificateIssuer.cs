using System.Threading;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.Model;

namespace TGIT.ACME.Protocol.IssuanceServices
{
    public interface ICertificateIssuer
    {
        Task<(byte[]? certificate, AcmeError? error)> IssueCertificate(string csr, CancellationToken cancellationToken);
    }
}
