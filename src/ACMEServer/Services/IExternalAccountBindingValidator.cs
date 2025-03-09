using Th11s.ACMEServer.Model.JWS;

namespace Th11s.ACMEServer.Services
{
    public interface IExternalAccountBindingValidator
    {
        /// <summary>
        /// Validates an external account binding.
        /// </summary>
        Task ValidateExternalAccountBindingAsync(AcmeJwsHeader requestHeader, AcmeJwsToken? externalAccountBinding, CancellationToken ct);
    }
}
