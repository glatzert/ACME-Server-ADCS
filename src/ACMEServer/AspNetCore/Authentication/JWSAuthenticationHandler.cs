using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Th11s.ACMEServer.AspNetCore.Extensions;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.Services;

namespace Th11s.ACMEServer.AspNetCore.Authentication
{
    public static class JWSAuthenticationDefaults
    {
        public const string AuthenticationScheme = "JWSProtectedPayload";
    }

    public class JWSAuthenticationOptions : AuthenticationSchemeOptions { }

    public class JWSAuthenticationHandler : AuthenticationHandler<JWSAuthenticationOptions>
    {
        private readonly IAccountService _accountService;

        public JWSAuthenticationHandler(
            IAccountService accountService,
            IOptionsMonitor<JWSAuthenticationOptions> options, 
            ILoggerFactory logger, 
            UrlEncoder encoder) 
            : base(options, logger, encoder)
        {
            _accountService = accountService;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var jwsToken = Context.TryGetAcmeRequest();

            if(jwsToken == null)
            {
                return AuthenticateResult.Fail("No JWSToken found to authenticate");
            }


            if (jwsToken.AcmeHeader.Jwk is not null)
            {
                Logger.LogDebug("Found JWK in request, validating signature.");

                if(IsSignatureValid(jwsToken.AcmeHeader.Jwk, jwsToken))
                {
                    // JWK was present and valid, but no account was loaded - this is used for finding an account or creating a new one.
                    return AuthenticateResult.Success(CreateTicket([]));
                }

                return AuthenticateResult.Fail("Signature validation failed for JWK in request.");
            }
            else if (jwsToken.AcmeHeader.Kid is not null)
            {
                try
                {
                    var accountId = jwsToken.AcmeHeader.GetAccountId();

                    Logger.LogDebug("Loading account with ID {accountId} from KID", accountId);

                    var account = await _accountService.LoadAcountAsync(accountId, Context.RequestAborted);
                    if(account is null)
                    {
                        return AuthenticateResult.Fail($"Could not find account for KID {jwsToken.AcmeHeader.Kid}");
                    }

                    if(account.Status != AccountStatus.Valid)
                    {
                        return AuthenticateResult.Fail($"Account status of account '{account.AccountId}' is not valid.");
                    }

                    if(IsSignatureValid(account.Jwk, jwsToken))
                    {
                        var claims = new[] {
                            new Claim(AcmeClaimTypes.AccountId, account.AccountId),
                            new Claim(AcmeClaimTypes.TOSAcceptedAt, account.TOSAccepted?.ToString("O") ?? "")
                        };

                        //KID could be associated with an account and the signature was successfully validated
                        return AuthenticateResult.Success(CreateTicket(claims));
                    }

                    return AuthenticateResult.Fail("Signature validation failed for KID in request.");
                }
                catch { }
            }
            //TODO: else if (certificates may be revoked with their private key)

            return AuthenticateResult.Fail("Could not authenticate the ACME request");
        }


        private AuthenticationTicket CreateTicket(IEnumerable<Claim> claims)
        => new AuthenticationTicket(
            new ClaimsPrincipal(
                new ClaimsIdentity(
                    [.. claims],
                    "JWS"
                )
            ),
            Scheme.Name);


        private bool IsSignatureValid(Jwk effectiveJWK, AcmeJwsToken request)
        {
            var securityKey = effectiveJWK.SecurityKey;

            var signatureProvider = TryCreateSignatureProvider(securityKey, request.AcmeHeader.Alg) 
                ?? throw AcmeErrors.BadSignatureAlgorithm("A signature provider could not be created.", []).AsException();

            using (signatureProvider)
            {
                var plainText = System.Text.Encoding.UTF8.GetBytes($"{request.Protected}.{request.Payload ?? ""}");
                var result = signatureProvider.Verify(plainText, request.SignatureBytes);

                Logger.LogDebug("Signature verification result: {result}", result);
                return result; 
            }
        }

        private AsymmetricSignatureProvider? TryCreateSignatureProvider(SecurityKey securityKey, string alg)
        {
            try
            {
                return new AsymmetricSignatureProvider(securityKey, alg);
            }
            catch (NotSupportedException ex)
            {
                Logger.LogError(ex, "Error creating AsymmetricSignatureProvider");
                return null;
            }
        }
    }
}
