using Th11s.ACMEServer.Model.JWS;

namespace Th11s.ACMEServer.Model.Services
{
    public interface IAccountService
    {
        Task<Account> CreateAccountAsync(AcmeJwsHeader header, List<string>? contact, bool termsOfServiceAgreed, AcmeJwsToken? externalAccountBinding, CancellationToken cancellationToken);
        Task<Account> UpdateAccountAsync(string accountId, List<string>? contact, AccountStatus? status, bool? termsOfServiceAgreed, CancellationToken cancellationToken);

        Task<Account?> FindAccountAsync(Jwk jwk, CancellationToken cancellationToken);

        Task<Account?> LoadAcountAsync(string accountId, CancellationToken cancellationToken);

        Task<List<string>> GetOrderIdsAsync(string accountId, CancellationToken requestAborted);
    }
}
