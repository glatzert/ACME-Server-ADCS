using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services;

public interface IChallengeValidator
{
    Task<ChallengeValidationResult> ValidateChallengeAsync(Challenge challenge, Account account, CancellationToken cancellationToken);

    public string ChallengeType { get; }
    public IEnumerable<string> SupportedIdentiferTypes { get; }
}

public record ChallengeValidationResult(ChallengeResult Result, AcmeError? Error)
{
    public static ChallengeValidationResult Valid() => new(ChallengeResult.Valid, null);
    public static ChallengeValidationResult Invalid(AcmeError error) => new(ChallengeResult.Invalid, error);

    public bool IsValid => Result == ChallengeResult.Valid;
}

public enum ChallengeResult
{
    Invalid,
    Valid
}
