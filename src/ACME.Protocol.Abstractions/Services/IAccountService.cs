using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.Model;

namespace TGIT.ACME.Protocol.Services
{
    public interface IAccountService
    {
        Task<Account> CreateAccountAsync(Jwk jwk, List<string>? contact,
            bool termsOfServiceAgreed, CancellationToken cancellationToken);

        Task<Account?> FindAccountAsync(Jwk jwk, CancellationToken cancellationToken);

        Task<Account?> LoadAcountAsync(string accountId, CancellationToken cancellationToken);

        Task<Account> FromRequestAsync(CancellationToken cancellationToken);
    }
}
