using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services
{
    public interface IOrderValidator
    {
        public Task<AcmeValidationResult> ValidateOrderAsync(Order order, CancellationToken cancellationToken);
    }
}
