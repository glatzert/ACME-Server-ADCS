
using Microsoft.Extensions.Logging;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services.ChallengeValidation;

public abstract class StringTokenChallengeValidator(ILogger logger) : ChallengeValidator(logger)
{
    private readonly ILogger _logger = logger;

    protected abstract string GetExpectedContent(Challenge challenge, Account account);

    protected abstract Task<(List<string>? Contents, AcmeError? Error)> LoadChallengeResponseAsync(Challenge challenge, CancellationToken cancellationToken);

    protected sealed override async Task<ChallengeValidationResult> ValidateChallengeInternalAsync(Challenge challenge, Account account, CancellationToken cancellationToken)
    {
        var (challengeContent, error) = await LoadChallengeResponseAsync(challenge, cancellationToken);
        if (error != null)
        {
            _logger.LogInformation("Could not load challenge response: {errorDetail}", error.Detail);
            return ChallengeValidationResult.Invalid(error);
        }

        var expectedContent = GetExpectedContent(challenge, account);
        _logger.LogInformation("Expected content of challenge is {expectedContent}.", expectedContent);

        if (challengeContent?.Contains(expectedContent) != true)
        {
            _logger.LogInformation("Challenge did not match expected value.");
            return ChallengeValidationResult.Invalid(AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, "Challenge response dod not contain the expected content."));
        }
        else
        {
            _logger.LogInformation("Challenge matched expected value.");
            return ChallengeValidationResult.Valid();
        }
    }
}
