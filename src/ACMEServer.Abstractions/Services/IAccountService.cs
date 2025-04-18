using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.JWS;
using Payloads = Th11s.ACMEServer.HttpModel.Payloads;

namespace Th11s.ACMEServer.Services;

public interface IAccountService
{
    Task<Account> CreateAccountAsync(AcmeJwsHeader header, Payloads.CreateOrGetAccount payload, CancellationToken cancellationToken);
    Task<Account> UpdateAccountAsync(string accountId, Payloads.UpdateAccount payload, CancellationToken cancellationToken);

    Task<Account?> FindAccountAsync(Jwk jwk, CancellationToken cancellationToken);

    Task<Account?> LoadAcountAsync(string accountId, CancellationToken cancellationToken);

    Task<List<string>> GetOrderIdsAsync(string accountId, CancellationToken requestAborted);
}
