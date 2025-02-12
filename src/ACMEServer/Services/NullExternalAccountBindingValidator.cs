using Th11s.ACMEServer.Model.JWS;

namespace Th11s.ACMEServer.Services;

public class NullExternalAccountBindingValidator : IExternalAccountBindingValidator
{
    public Task<bool> ValidateExternalAccountBindingAsync(AcmeJwsHeader requestHeader, AcmeJwsToken? externalAccountBinding, CancellationToken ct)
    {
        return Task.FromResult(false);
    }
}
