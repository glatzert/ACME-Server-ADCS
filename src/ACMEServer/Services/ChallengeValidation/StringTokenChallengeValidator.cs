
using Microsoft.Extensions.Logging;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services.ChallengeValidation;

public abstract class StringTokenChallengeValidator(ILogger logger) : ChallengeValidator(logger)
{
    private readonly ILogger _logger = logger;

    protected abstract string GetExpectedContent(TokenChallenge challenge, Account account);

    protected abstract Task<(List<string>? Contents, AcmeError? Error)> LoadChallengeResponseAsync(TokenChallenge challenge, CancellationToken cancellationToken);

    protected sealed override async Task<ChallengeValidationResult> ValidateChallengeInternalAsync(Challenge challenge, Account account, CancellationToken cancellationToken)
    {
        if (challenge is not TokenChallenge tokenChallenge)
        {
            _logger.ChallengeNotTokenChallenge();
            throw new InvalidOperationException("Challenge is not of type TokenChallenge.");
        }

        var (challengeContent, error) = await LoadChallengeResponseAsync(tokenChallenge, cancellationToken);
        if (error != null)
        {
            _logger.CouldNotLoadChallengeResponse(error.Detail);
            return ChallengeValidationResult.Invalid(error);
        }

        var expectedContent = GetExpectedContent(tokenChallenge, account);
        _logger.ExpectedChallengeContent(expectedContent);

        if (challengeContent?.Contains(expectedContent) != true)
        {
            _logger.ChallengeDidNotMatch();
            return ChallengeValidationResult.Invalid(AcmeErrors.IncorrectResponse(challenge.Authorization.Identifier, "Challenge response dod not contain the expected content."));
        }
        else
        {
            _logger.ChallengeMatched();
            return ChallengeValidationResult.Valid();
        }
    }
}
