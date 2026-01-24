using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Services;

namespace Th11s.ACMEServer.Tests.Utils.Fakes;

internal class FakeChallengeValidator : IChallengeValidator
{
    public FakeChallengeValidator(string challengeType, IEnumerable<string> identifierTypes)
    {
        ChallengeType = challengeType;
        SupportedIdentiferTypes = [.. identifierTypes];
    }

    public string ChallengeType { get; }

    public IEnumerable<string> SupportedIdentiferTypes { get; }

    public Task<ChallengeValidationResult> ValidateChallengeAsync(Challenge challenge, Account account, CancellationToken cancellationToken)
    {
        return Task.FromResult(new ChallengeValidationResult(ChallengeResult.Valid, null));
    }
}
