using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Services;

namespace Th11s.ACMEServer.Services.ChallangeValidation
{
    public abstract class TokenChallengeValidator : IChallengeValidator
    {
        private readonly ILogger _logger;

        public TokenChallengeValidator(ILogger logger)
        {
            _logger = logger;
        }

        protected abstract Task<(List<string>? Contents, AcmeError? Error)> LoadChallengeResponseAsync(Challenge challenge, CancellationToken cancellationToken);
        protected abstract string GetExpectedContent(Challenge challenge, Account account);

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

            var (challengeContent, error) = await LoadChallengeResponseAsync(challenge, cancellationToken);
            if (error != null)
            {
                _logger.LogInformation($"Could not load challenge response: {error.Detail}");
                return new(ChallengeResult.Invalid, error);
            }

            var expectedContent = GetExpectedContent(challenge, account);
            _logger.LogInformation($"Expected content of challenge is {expectedContent}.");

            if (challengeContent?.Contains(expectedContent) != true)
            {
                _logger.LogInformation($"Challenge did not match expected value.");
                return new(ChallengeResult.Invalid, new AcmeError("incorrectResponse", "Challenge response dod not contain the expected content.", challenge.Authorization.Identifier));
            }
            else
            {
                _logger.LogInformation($"Challenge matched expected value.");
                return new(ChallengeResult.Valid, null);
            }
        }
    }
}
