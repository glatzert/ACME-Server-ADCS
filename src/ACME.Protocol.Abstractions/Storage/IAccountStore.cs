using System.Threading;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.Model;

namespace TGIT.ACME.Protocol.Storage
{
    public interface IAccountStore
    {
        Task SaveAccountAsync(Account account, CancellationToken cancellationToken);
        Task<Account?> LoadAccountAsync(string accountId, CancellationToken cancellationToken);
    }
}
