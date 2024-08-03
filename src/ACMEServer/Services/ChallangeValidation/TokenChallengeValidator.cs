
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Services;

namespace Th11s.ACMEServer.Services.ChallangeValidation
{
    public abstract class TokenChallengeValidator : ChallengeValidator
    {
        private readonly ILogger _logger;

        public TokenChallengeValidator(ILogger logger)
            :base(logger)
        {
            _logger = logger;
        }

        protected abstract Task<(List<string>? Contents, AcmeError? Error)> LoadChallengeResponseAsync(Challenge challenge, CancellationToken cancellationToken);

        public sealed override async Task<ChallengeValidationResult> ValidateChallengeInternalAsync(Challenge challenge, Account account, CancellationToken cancellationToken)
        {
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
