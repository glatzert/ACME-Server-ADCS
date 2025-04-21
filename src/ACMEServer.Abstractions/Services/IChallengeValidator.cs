using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services;

public interface IChallengeValidator
{
    Task<ChallengeValidationResult> ValidateChallengeAsync(Challenge challenge, Account account, CancellationToken cancellationToken);

    public string ChallengeType { get; }
    public IEnumerable<string> SupportedIdentiferTypes { get; }
}

public record ChallengeValidationResult(ChallengeResult Result, AcmeError? Error);

public enum ChallengeResult
{
    Invalid,
    Valid
}
