using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Th11s.ACMEServer.Model.Services
{
    public interface IAccountService
    {
        Task<Account> CreateAccountAsync(Jwk jwk, List<string>? contact,
            bool termsOfServiceAgreed, CancellationToken cancellationToken);

        Task<Account?> FindAccountAsync(Jwk jwk, CancellationToken cancellationToken);

        Task<Account?> LoadAcountAsync(string accountId, CancellationToken cancellationToken);
        Task<Account> UpdateAccountAsync(Account account, List<string>? contacts, AccountStatus? accountStatus, CancellationToken ct);

        Task<Account> FromRequestAsync(CancellationToken cancellationToken);

        Task<List<string>> GetOrderIdsAsync(Account account, CancellationToken ct);
    }
}
