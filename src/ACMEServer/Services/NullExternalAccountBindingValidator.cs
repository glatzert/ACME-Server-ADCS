using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.JWS;

namespace Th11s.ACMEServer.Services;

public class NullExternalAccountBindingValidator : IExternalAccountBindingValidator
{
    public Task<AcmeError?> ValidateExternalAccountBindingAsync(AcmeJwsHeader requestHeader, AcmeJwsToken externalAccountBinding, CancellationToken ct)
    {
        return Task.FromResult(
            (AcmeError?)AcmeErrors.ExternalAccountBindingFailed("External account binding is not supported.")
        );
    }
}
