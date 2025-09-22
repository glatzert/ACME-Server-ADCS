using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.Model.Primitives;
using Payloads = Th11s.ACMEServer.HttpModel.Payloads;

namespace Th11s.ACMEServer.Services;

public interface IAccountService
{
    Task<Account> CreateAccountAsync(AcmeJwsHeader header, Payloads.CreateOrGetAccount payload, CancellationToken cancellationToken);
    Task<Account> UpdateAccountAsync(AccountId accountId, Payloads.UpdateAccount? payload, CancellationToken cancellationToken);

    Task<Account?> FindAccountAsync(Jwk jwk, CancellationToken cancellationToken);

    Task<Account?> LoadAcountAsync(AccountId accountId, CancellationToken cancellationToken);

    Task<List<string>> GetOrderIdsAsync(AccountId accountId, CancellationToken requestAborted);
}
