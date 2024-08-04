namespace Th11s.ACMEServer.Model.Services
{
    public interface IChallengeValidator
    {
        Task<ChallengeValidationResult> ValidateChallengeAsync(Challenge challenge, Account account, CancellationToken cancellationToken);
    }

    public record ChallengeValidationResult(ChallengeResult Result, AcmeError? Error);

    public enum ChallengeResult
    {
        Invalid,
        Valid
    }
}
