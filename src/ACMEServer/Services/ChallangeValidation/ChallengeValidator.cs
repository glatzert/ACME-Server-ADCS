
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Services;

namespace Th11s.ACMEServer.Services.ChallangeValidation
{
    public abstract class ChallengeValidator : IChallengeValidator
    {
        protected ChallengeValidator(ILogger logger)
        {
            _logger = logger;
        }

        private readonly ILogger _logger;

        public async Task<ChallengeValidationResult> ValidateChallengeAsync(Challenge challenge, Account account, CancellationToken cancellationToken)
        {
            if (challenge is null)
                throw new ArgumentNullException(nameof(challenge));
            if (account is null)
                throw new ArgumentNullException(nameof(account));

            _logger.LogInformation($"Attempting to validate challenge {challenge.ChallengeId} ({challenge.Type})");

            if (account.Status != AccountStatus.Valid)
            {
                _logger.LogInformation($"Account is not valid. Challenge validation failed.");
                return new(ChallengeResult.Invalid, new AcmeError("unauthorized", "Account invalid", challenge.Authorization.Identifier));
            }

            if (challenge.Authorization.Expires < DateTimeOffset.UtcNow)
            {
                _logger.LogInformation($"Challenges authorization already expired.");
                challenge.Authorization.SetStatus(AuthorizationStatus.Expired);
                return new(ChallengeResult.Invalid, new AcmeError("custom:authExpired", "Authorization expired", challenge.Authorization.Identifier));
            }
            if (challenge.Authorization.Order.Expires < DateTimeOffset.UtcNow)
            {
                _logger.LogInformation($"Challenges order already expired.");
                challenge.Authorization.Order.SetStatus(OrderStatus.Invalid);
                return new(ChallengeResult.Invalid, new AcmeError("custom:orderExpired", "Order expired"));
            }

            return await ValidateChallengeInternalAsync(challenge, account, cancellationToken);
        }

        protected abstract string GetExpectedContent(Challenge challenge, Account account);

        public abstract Task<ChallengeValidationResult> ValidateChallengeInternalAsync(Challenge challenge, Account account, CancellationToken cancellationToken);


        protected static string GetKeyAuthToken(Challenge challenge, Account account)
        {
            var thumbprintBytes = account.Jwk.SecurityKey.ComputeJwkThumbprint();
            var thumbprint = Base64UrlEncoder.Encode(thumbprintBytes);

            var keyAuthToken = $"{challenge.Token}.{thumbprint}";
            return keyAuthToken;
        }

        protected static string GetKeyAuthDigest(Challenge challenge, Account account)
        {
            var keyAuthBytes = Encoding.UTF8.GetBytes(GetKeyAuthToken(challenge, account));
            var digestBytes = SHA256.HashData(keyAuthBytes);

            var digest = Base64UrlEncoder.Encode(digestBytes);
            return digest;
        }
    }
}
