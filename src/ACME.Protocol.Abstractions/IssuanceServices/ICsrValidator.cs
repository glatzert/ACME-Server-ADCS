using System.Threading;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.Model;

namespace TGIT.ACME.Protocol.IssuanceServices
{
    public interface ICSRValidator
    {
        Task<AcmeValidationResult> ValidateCsrAsync(Order order, string csr, CancellationToken cancellationToken);
    }
}
