using System.Threading;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.Model;

namespace TGIT.ACME.Protocol.Services
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
