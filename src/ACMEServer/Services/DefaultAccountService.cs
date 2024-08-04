using Th11s.ACMEServer.HttpModel.Services;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.Services;
using Th11s.ACMEServer.Model.Storage;

namespace Th11s.ACMEServer.Services
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
            var newAccount = new Account(jwk, contacts, termsOfServiceAgreed ? DateTimeOffset.UtcNow : null);

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

        public async Task<Account> UpdateAccountAsync(Account account, List<string>? contacts, AccountStatus? accountStatus, bool? termsOfServiceAgreed, CancellationToken ct)
        {
            if (accountStatus != AccountStatus.Deactivated)
                throw new MalformedRequestException("Only deactivation is supported.");
            account.Status = accountStatus ?? account.Status;


            if (contacts?.Any() == true)
            {
                account.Contacts = contacts ?? account.Contacts;
            }


            if (termsOfServiceAgreed == true)
            {
                account.TOSAccepted = DateTimeOffset.UtcNow;
            }


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
