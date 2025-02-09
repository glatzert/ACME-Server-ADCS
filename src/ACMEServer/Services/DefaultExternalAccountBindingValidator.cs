using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.JWS;

namespace Th11s.ACMEServer.Services
{
    public interface IExternalAccountBindingValidator
    {
        /// <summary>
        /// Validates an external account binding.
        /// </summary>
        Task<bool> ValidateExternalAccountBindingAsync(AcmeJwsHeader requestHeader, AcmeJwsToken? externalAccountBinding, CancellationToken ct);
    }


    public class NullExternalAccountBindingValidator : IExternalAccountBindingValidator
    {
        public Task<bool> ValidateExternalAccountBindingAsync(AcmeJwsHeader requestHeader, AcmeJwsToken? externalAccountBinding, CancellationToken ct)
        {
            return Task.FromResult(false);
        }
    }


    public class DefaultExternalAccountBindingValidator : IExternalAccountBindingValidator
    {
        private static readonly HashSet<string> _hmacAlgorithms = ["HS256", "HS384", "HS512"];
        private readonly DefaultExternalAccountBindingClient _eabClient;
        private readonly IOptions<ACMEServerOptions> _options;

        public DefaultExternalAccountBindingValidator(
            DefaultExternalAccountBindingClient eabClient,
            IOptions<ACMEServerOptions> options)
        {
            _eabClient = eabClient;
            _options = options;
        }


        public async Task<bool> ValidateExternalAccountBindingAsync(AcmeJwsHeader requestHeader, AcmeJwsToken externalAccountBinding, CancellationToken ct)
        {
            if (!_hmacAlgorithms.Contains(externalAccountBinding.AcmeHeader.Alg))
                throw new MalformedRequestException("externalAccountBinding JWS header may only indicate HMAC algs like HS256");

            if (requestHeader.Nonce != null)
                throw new MalformedRequestException("externalAccountBinding JWS header may not contain a nonce.");

            if (requestHeader.Url != externalAccountBinding.AcmeHeader.Url)
                throw new MalformedRequestException("externalAccountBinding JWS header and request JWS header need to have the same url.");

            if (requestHeader.Jwk!.Json != externalAccountBinding.Payload)
                throw new MalformedRequestException("externalAccountBinding JWS payload and request JWS header JWK need to be identical.");

            if (externalAccountBinding.AcmeHeader.Kid == null)
                throw new MalformedRequestException("externalAccountBinding JWS header must contain a kid.");


            var eabMACKey = await _eabClient.GetEABHMACfromKidAsync(externalAccountBinding.AcmeHeader.Kid, ct);

            var symmetricKey = new SymmetricSignatureProvider(new SymmetricSecurityKey(eabMACKey), externalAccountBinding.AcmeHeader.Alg);
            var plainText = System.Text.Encoding.UTF8.GetBytes($"{externalAccountBinding.Protected}.{externalAccountBinding.Payload ?? ""}");
            var isEabMacValid = symmetricKey.Verify(plainText, externalAccountBinding.SignatureBytes);

            _ = isEabMacValid 
                ? _eabClient.SingalEABSucces(externalAccountBinding.AcmeHeader.Kid) 
                : _eabClient.SignalEABFailure(externalAccountBinding.AcmeHeader.Kid);


            return isEabMacValid;
        }
    }

    public interface IExternalAccountBindingClient
    {
        Task<byte[]> GetEABHMACfromKidAsync(string kid, CancellationToken ct);
        
        Task SignalEABFailure(string kid) => Task.CompletedTask;
        Task SingalEABSucces(string kid) => Task.CompletedTask;
    }

    public class DefaultExternalAccountBindingClient : IExternalAccountBindingClient
    {
        public Task<byte[]> GetEABHMACfromKidAsync(string kid, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public Task SignalEABFailure(string kid)
        {
            throw new NotImplementedException();
        }

        public Task SingalEABSucces(string kid)
        {
            throw new NotImplementedException();
        }
    }
}
