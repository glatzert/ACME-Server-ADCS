using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Configuration;

namespace Th11s.ACMEServer.Services
{
    public interface IIdentifierValidator
    {
        public Task<IDictionary<Identifier, AcmeValidationResult>> ValidateIdentifiersAsync(
            List<Identifier> identifiers, 
            ProfileConfiguration profileConfig, 
            CancellationToken cancellationToken);
    }
}
