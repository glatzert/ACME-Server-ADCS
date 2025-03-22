using Th11s.ACMEServer.HttpModel.Services;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.Services;
using Th11s.ACMEServer.Model.Storage;
using Microsoft.Extensions.Options;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Model.JWS;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Http;
using Th11s.ACMEServer.AspNetCore.Middleware;

namespace Th11s.ACMEServer.Services
{
    public class DefaultAccountService : IAccountService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IExternalAccountBindingValidator _eabValidator;
        private readonly IOptions<ACMEServerOptions> _options;
        private readonly IAccountStore _accountStore;

        public DefaultAccountService(
            IHttpContextAccessor httpContextAccessor,
            IExternalAccountBindingValidator eabValidator,
            IOptions<ACMEServerOptions> options,
            IAccountStore accountStore)
        {
            _httpContextAccessor = httpContextAccessor;
            _eabValidator = eabValidator;
            _options = options;
            _accountStore = accountStore;
        }

        public async Task<Account> CreateAccountAsync(AcmeJwsHeader header, List<string>? contacts,
            bool termsOfServiceAgreed, AcmeJwsToken? externalAccountBinding, CancellationToken cancellationToken)
        {
            // TODO:
            // ValidateTOS(newAccount);

            var requiresExternalAccountBinding = _options.Value.ExternalAccountBinding?.Required == true;
            if (requiresExternalAccountBinding && externalAccountBinding == null)
            {
                throw AcmeErrors.ExternalAccountRequired().AsException();
            }

            var effectiveEAB = externalAccountBinding;
            if (effectiveEAB != null)
            {
                var eabValidationError = await _eabValidator.ValidateExternalAccountBindingAsync(header, effectiveEAB, cancellationToken);

                if(eabValidationError != null)
                {
                    if (requiresExternalAccountBinding)
                    {
                        throw eabValidationError.AsException();
                    }

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
            var requestHeader = _httpContextAccessor.HttpContext.Features.Get<AcmeRequest>().Request.AcmeHeader;

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
