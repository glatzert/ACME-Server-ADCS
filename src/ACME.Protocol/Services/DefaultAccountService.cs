using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.Model;
using TGIT.ACME.Protocol.Model.Exceptions;
using TGIT.ACME.Protocol.RequestServices;
using TGIT.ACME.Protocol.Storage;

namespace TGIT.ACME.Protocol.Services
{
    public class DefaultAccountService : IAccountService
    {
        private readonly IAcmeRequestProvider _requestProvider;
        private readonly IAccountStore _accountStore;

        public DefaultAccountService(IAcmeRequestProvider requestProvider, IAccountStore accountStore)
        {
            _requestProvider = requestProvider;
            _accountStore = accountStore;
        }

        public async Task<Account> CreateAccountAsync(Jwk jwk, List<string>? contacts,
            bool termsOfServiceAgreed, CancellationToken cancellationToken)
        {
            var newAccount = new Account(jwk, contacts, termsOfServiceAgreed ? DateTimeOffset.UtcNow : (DateTimeOffset?)null);

            await _accountStore.SaveAccountAsync(newAccount, cancellationToken);
            return newAccount;
        }

        public Task<Account?> FindAccountAsync(Jwk jwk, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<Account> FromRequestAsync(CancellationToken cancellationToken)
        {
            var requestHeader = _requestProvider.GetHeader();

            if (string.IsNullOrEmpty(requestHeader.Kid))
                throw new MalformedRequestException("Kid header is missing");

            //TODO: Get accountId from Kid?
            var accountId = requestHeader.GetAccountId();
            var account = await LoadAcountAsync(accountId, cancellationToken);
            ValidateAccount(account);

            return account!;
        }

        public async Task<Account?> LoadAcountAsync(string accountId, CancellationToken cancellationToken)
        {
            return await _accountStore.LoadAccountAsync(accountId, cancellationToken);
        }

        private static void ValidateAccount(Account? account)
        {
            if (account == null)
                throw new NotFoundException();

            if (account.Status != AccountStatus.Valid)
                throw new ConflictRequestException(AccountStatus.Valid, account.Status);
        }
    }
}
