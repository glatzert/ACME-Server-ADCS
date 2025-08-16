using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services;

public interface ICsrValidator
{
    Task<AcmeValidationResult> ValidateCsrAsync(Order order, CancellationToken cancellationToken);
}
