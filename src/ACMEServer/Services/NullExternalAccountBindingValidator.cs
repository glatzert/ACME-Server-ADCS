using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.JWS;

namespace Th11s.ACMEServer.Services;

public class NullExternalAccountBindingValidator : IExternalAccountBindingValidator
{
    public Task ValidateExternalAccountBindingAsync(AcmeJwsHeader requestHeader, AcmeJwsToken? externalAccountBinding, CancellationToken ct)
    {
        throw new ExternalAccountBindingFailedException("External account binding is not supported.");
    }
}
