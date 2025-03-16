using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.JWS;

namespace Th11s.ACMEServer.Services
{
    public interface IExternalAccountBindingValidator
    {
        /// <summary>
        /// Validates an external account binding and returns an error if it is invalid.
        /// </summary>
        Task<AcmeError?> ValidateExternalAccountBindingAsync(AcmeJwsHeader requestHeader, AcmeJwsToken externalAccountBinding, CancellationToken ct);
    }
}
