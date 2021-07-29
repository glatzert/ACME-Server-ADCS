using System.Threading;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.Model;

namespace TGIT.ACME.Protocol.Services
{
    public interface IChallengeValidator
    {
        Task<(bool IsValid, AcmeError? error)> ValidateChallengeAsync(Challenge challenge, Account account, CancellationToken cancellationToken);
    }
}
