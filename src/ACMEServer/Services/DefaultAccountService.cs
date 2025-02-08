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

namespace Th11s.ACMEServer.Services
{
    public class DefaultAccountService : IAccountService
    {
        private readonly IAcmeRequestProvider _requestProvider;
        private readonly IOptions<ACMEServerOptions> _options;
        private readonly IAccountStore _accountStore;

        public DefaultAccountService(
            IAcmeRequestProvider requestProvider, 
            IOptions<ACMEServerOptions> options,
            IAccountStore accountStore)
        {
            _requestProvider = requestProvider;
            _options = options;
            _accountStore = accountStore;
        }

        public async Task<Account> CreateAccountAsync(AcmeJwsHeader header, List<string>? contacts,
            bool termsOfServiceAgreed, AcmeJwsToken? externalAccountBinding, CancellationToken cancellationToken)
        {

            //TODO:
            // ValidateTOS(newAccount);

            var eabStatus = await ValidateExternalAccountBindingAsync(header, externalAccountBinding);
            if(eabStatus == EABStatus.Invalid)
            {
                throw new ExternalAccountBindingRequiredException();
            }



            var newAccount = new Account(jwk, contacts, termsOfServiceAgreed ? DateTimeOffset.UtcNow : null, eabStatus == EABStatus.Valid ? externalAccountBinding : null);

            await _accountStore.SaveAccountAsync(newAccount, cancellationToken);
            return newAccount;
        }


        private static readonly HashSet<string> _hmacAlgorithms = ["HS256","HS384","HS512"];

        /// <summary>
        /// Determines, if an external account binding is required and validates it.
        /// If an external account binding is not required, but present, it will be validated.
        /// </summary>
        /// <exception cref="ExternalAccountBindingRequiredException"></exception>
        private async Task<EABStatus> ValidateExternalAccountBindingAsync(AcmeJwsHeader requestHeader, AcmeJwsToken? externalAccountBinding)
        {
            if (externalAccountBinding == null)
            {
                if (_options.Value.ExternalAccountBinding?.Required == true)
                {
                    throw new ExternalAccountBindingRequiredException();
                }

                return EABStatus.Ignore;
            }

            if (!_hmacAlgorithms.Contains(externalAccountBinding.AcmeHeader.Alg))
                throw new MalformedRequestException("externalAccountBinding JWS header may only indicate HMAC algs like HS256"); 

            if (requestHeader.Nonce != null)
                throw new MalformedRequestException("externalAccountBinding JWS header may not contain a nonce.");

            if(requestHeader.Url != externalAccountBinding.AcmeHeader.Url)
                throw new MalformedRequestException("externalAccountBinding JWS header and request JWS header need to have the same url.");

            if(requestHeader.Jwk!.Json != externalAccountBinding.Payload)
                throw new MalformedRequestException("externalAccountBinding JWS payload and request JWS header JWK need to be identical.");


            var eabMACKey = RetrieveMACKeyFromKIDAsync();

            var symmetricKey = new SymmetricSignatureProvider(new SymmetricSecurityKey(eabMACKey), externalAccountBinding.AcmeHeader.Alg);
            var plainText = System.Text.Encoding.UTF8.GetBytes($"{externalAccountBinding.Protected}.{externalAccountBinding.Payload ?? ""}");
            var isValid = symmetricKey.Verify(plainText, externalAccountBinding.SignatureBytes);

            if(isValid)
            {
                _ = ConfirmKIDUsageAsync();
            }
            else
            {
                _ = RevokeKIDUsageAsync();
            }

            return isValid ? EABStatus.Valid : EABStatus.Invalid;
        }

        public enum EABStatus
        {
            Ignore,
            Valid,
            Invalid
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
