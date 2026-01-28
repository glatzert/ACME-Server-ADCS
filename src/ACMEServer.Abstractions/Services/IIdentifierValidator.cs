using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services
{
    public interface IIdentifierValidator
    {
        public Task<IDictionary<Identifier, AcmeValidationResult>> ValidateIdentifiersAsync(
            IdentifierValidationContext context,
            CancellationToken cancellationToken);
    }
}
