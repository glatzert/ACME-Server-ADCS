using DnsClient.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.HttpModel.Services;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.Model.Services;
using Th11s.ACMEServer.Model.Storage;

namespace Th11s.ACMEServer.Services
{
    public class DefaultAccountService : IAccountService
    {
        private readonly IAcmeRequestProvider _requestProvider;
        private readonly IExternalAccountBindingValidator _eabValidator;
        private readonly IOptions<ACMEServerOptions> _options;
        private readonly IAccountStore _accountStore;
        private readonly ILogger<DefaultAccountService> _logger;

        public DefaultAccountService(
            IAcmeRequestProvider requestProvider, 
            IExternalAccountBindingValidator eabValidator,
            IOptions<ACMEServerOptions> options,
            IAccountStore accountStore,
            ILogger<DefaultAccountService> logger)
        {
            _requestProvider = requestProvider;
            _eabValidator = eabValidator;
            _options = options;
            _accountStore = accountStore;
            _logger = logger;
        }

        public async Task<Account> CreateAccountAsync(AcmeJwsHeader header, List<string>? contacts,
            bool termsOfServiceAgreed, AcmeJwsToken? externalAccountBinding, CancellationToken cancellationToken)
        {
            if(_options.Value.TOS.RequireAgreement && !termsOfServiceAgreed)
            {
                throw AcmeErrors.UserActionRequired("TOS acceptance is required").AsException();
            }

            var requiresExternalAccountBinding = _options.Value.ExternalAccountBinding?.Required == true;
            if (requiresExternalAccountBinding && externalAccountBinding == null)
            {
                throw AcmeErrors.ExternalAccountRequired().AsException();
            }

            var effectiveEAB = externalAccountBinding;
            if (effectiveEAB != null)
            {
                _logger.LogDebug("Found external account binding in new-account request.");
                var eabValidationError = await _eabValidator.ValidateExternalAccountBindingAsync(header, effectiveEAB, cancellationToken);

                if(eabValidationError != null)
                {
                    if (requiresExternalAccountBinding)
                    {
                        throw eabValidationError.AsException();
                    }

                    _logger.LogDebug("External account binding could not be validatet, but was not required.");
                    effectiveEAB = null;
                }
            }

            
            var newAccount = new Account(
                header.Jwk!, 
                contacts, 
                termsOfServiceAgreed ? DateTimeOffset.UtcNow : null, 
                effectiveEAB);

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
            if (accountStatus.HasValue && account.Status != accountStatus)
            {
                if (accountStatus != AccountStatus.Deactivated)
                    throw new MalformedRequestException("Only deactivation is supported.");

                account.Status = accountStatus ?? account.Status;
            }


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
