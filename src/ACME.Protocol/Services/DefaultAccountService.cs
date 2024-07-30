using System;
using System.Collections.Generic;
using System.Linq;
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
            return _accountStore.FindAccountAsync(jwk, cancellationToken);
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

        public async Task<Account> UpdateAccountAsync(Account account, List<string>? contacts, AccountStatus? accountStatus, CancellationToken ct)
        {
            if (contacts?.Any() == true)
            {
                account.Contacts = contacts ?? account.Contacts;
            }
            account.Status = accountStatus ?? account.Status;

            await _accountStore.SaveAccountAsync(account, ct);
            return account;
        }

        public async Task<List<string>> GetOrderIdsAsync(Account account, CancellationToken ct)
        {
            return await _accountStore.GetAccountOrders(account.AccountId, ct);
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
