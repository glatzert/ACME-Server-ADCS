using System.Threading;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.Model;

namespace TGIT.ACME.Protocol.IssuanceServices
{
    public interface ICsrValidator
    {
        Task<(bool isValid, AcmeError? error)> ValidateCsrAsync(Order order, string csr, CancellationToken cancellationToken);
    }
}
