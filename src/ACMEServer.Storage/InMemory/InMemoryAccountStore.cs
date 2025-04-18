using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.Model.Storage;

namespace ACMEServer.Storage.InMemory;

public class InMemoryAccountStore : IAccountStore
{
    private Dictionary<string, Account> _accounts = [];

    public Task<Account?> FindAccountAsync(Jwk jwk, CancellationToken cancellationToken)
    {
        var accounts = _accounts.Where(a => a.Value.Jwk.Equals(jwk));
        return Task.FromResult(accounts.Any() ? accounts.First().Value : null);
    }

    public Task<List<string>> GetAccountOrders(string accountId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<Account?> LoadAccountAsync(string accountId, CancellationToken cancellationToken)
        => Task.FromResult(_accounts.TryGetValue(accountId, out var account) ? account : null);

    public Task SaveAccountAsync(Account account, CancellationToken cancellationToken)
        => Task.FromResult(_accounts[account.AccountId] = account);
}
