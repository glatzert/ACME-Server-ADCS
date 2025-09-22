
using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Model.Storage;

public interface IAccountStore
{
    Task SaveAccountAsync(Account account, CancellationToken cancellationToken);
    Task<Account?> LoadAccountAsync(AccountId accountId, CancellationToken cancellationToken);

    Task<Account?> FindAccountAsync(Jwk jwk, CancellationToken cancellationToken);
    Task<List<string>> GetAccountOrders(AccountId accountId, CancellationToken ct);
}
