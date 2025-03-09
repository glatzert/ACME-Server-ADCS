using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Services;

namespace ACMEServer.Tests.Integration;

internal class FakeChallengeValidator : IChallengeValidator
{
    public FakeChallengeValidator(string challengeType)
    {
        ChallengeType = challengeType;
    }

    public string ChallengeType { get; }

    public Task<ChallengeValidationResult> ValidateChallengeAsync(Challenge challenge, Account account, CancellationToken cancellationToken)
    {
        return Task.FromResult(new ChallengeValidationResult(ChallengeResult.Valid, null));
    }
}
