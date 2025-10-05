using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Model.Storage;

namespace ACMEServer.Storage.InMemory;

public class InMemoryAccountStore : IAccountStore
{
    private readonly Dictionary<AccountId, Account> _accounts = [];

    public Task<Account?> FindAccountAsync(Jwk jwk, CancellationToken cancellationToken)
    {
        var accounts = _accounts.Where(a => a.Value.Jwk.Equals(jwk));
        return Task.FromResult(accounts.Any() ? accounts.First().Value : null);
    }

    public Task<List<OrderId>> GetAccountOrders(AccountId accountId, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public Task<Account?> LoadAccountAsync(AccountId accountId, CancellationToken cancellationToken)
        => Task.FromResult(_accounts.TryGetValue(accountId, out var account) ? account : null);

    public Task SaveAccountAsync(Account account, CancellationToken cancellationToken)
        => Task.FromResult(_accounts[account.AccountId] = account);

    public Task<Account> UpdateAccountKeyAsync(Account account, Jwk jwk, CancellationToken cancellationToken)
    {
        var result = _accounts[account.AccountId];
        result.Jwk = jwk;
        return Task.FromResult(result);
    }
}
