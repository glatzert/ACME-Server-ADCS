
namespace Th11s.ACMEServer.Model.Storage
{
    public interface IAccountStore
    {
        Task SaveAccountAsync(Account account, CancellationToken cancellationToken);
        Task<Account?> LoadAccountAsync(string accountId, CancellationToken cancellationToken);

        Task<Account?> FindAccountAsync(Jwk jwk, CancellationToken cancellationToken);
        Task<List<string>> GetAccountOrders(string accountId, CancellationToken ct);
    }
}
