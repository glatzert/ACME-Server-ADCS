using Th11s.ACMEServer.HttpModel.Requests;
using Th11s.ACMEServer.Model.JWS;

namespace Th11s.ACMEServer.HttpModel.Services
{
    public interface IRequestValidationService
    {
        Task ValidateRequestAsync(
            AcmeJwsToken request, 
            string requestUrl, 
            CancellationToken cancellationToken);
    }
}
