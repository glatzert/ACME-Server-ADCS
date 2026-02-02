using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.JWS;

namespace Th11s.ACMEServer.AspNetCore.Extensions
{
    public static class JsonWebKeyExtensions
    {
        public static bool IsSignatureValid(this JsonWebKey securityKey, AcmeJwsToken request, ILogger logger)
        {
            var signatureProvider = TryCreateSignatureProvider(securityKey, request.AcmeHeader.Alg, logger)
                ?? throw AcmeErrors.BadSignatureAlgorithm("A signature provider could not be created.", []).AsException();

            using (signatureProvider)
            {
                var plainText = System.Text.Encoding.UTF8.GetBytes($"{request.Protected}.{request.Payload ?? ""}");
                var result = signatureProvider.Verify(plainText, request.SignatureBytes);

                logger.SignatureVerificationResult(result);
                return result;
            }
        }

        private static AsymmetricSignatureProvider? TryCreateSignatureProvider(SecurityKey securityKey, string alg, ILogger logger)
        {
            try
            {
                return new AsymmetricSignatureProvider(securityKey, alg);
            }
            catch (NotSupportedException ex)
            {
                logger.ErrorCreatingSignatureProvider(ex);
                return null;
            }
        }
    }
}
