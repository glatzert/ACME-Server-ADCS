using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services;

public interface ICSRValidator
{
    Task<AcmeValidationResult> ValidateCsrAsync(Order order, string csr, CancellationToken cancellationToken);
}
