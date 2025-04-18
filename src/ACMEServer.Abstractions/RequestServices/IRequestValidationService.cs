using Th11s.ACMEServer.Model.JWS;

namespace Th11s.ACMEServer.RequestServices;

public interface IRequestValidationService
{
    Task ValidateRequestAsync(
        AcmeJwsToken request, 
        string requestUrl, 
        CancellationToken cancellationToken);
}
