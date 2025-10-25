using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Services
{
    public interface IIdentifierValidator
    {
        public Task<IDictionary<Identifier, AcmeValidationResult>> ValidateIdentifiersAsync(
            IdentifierValidationContext context,
            CancellationToken cancellationToken);
    }
}
