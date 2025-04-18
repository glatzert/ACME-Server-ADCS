using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.Model.Storage;
using Payloads = Th11s.ACMEServer.HttpModel.Payloads;

namespace Th11s.ACMEServer.Services;

public class DefaultAccountService(
    IExternalAccountBindingValidator eabValidator,
    IOptions<ACMEServerOptions> options,
    IAccountStore accountStore) : IAccountService
{
    private readonly IExternalAccountBindingValidator _eabValidator = eabValidator;
    private readonly IOptions<ACMEServerOptions> _options = options;
    private readonly IAccountStore _accountStore = accountStore;

    public async Task<Account> CreateAccountAsync(AcmeJwsHeader header, Payloads.CreateOrGetAccount payload, CancellationToken cancellationToken)
    {
        var requiresTOSAgreement = _options.Value.TOS.RequireAgreement;
        if (requiresTOSAgreement && !payload.TermsOfServiceAgreed)
        {
            throw AcmeErrors.UserActionRequired("Terms of service need to be accepted.").AsException();
        }
        
        var requiresExternalAccountBinding = _options.Value.ExternalAccountBinding?.Required == true;
        if (requiresExternalAccountBinding && payload.ExternalAccountBinding == null)
        {
            throw AcmeErrors.ExternalAccountRequired().AsException();
        }

        var effectiveEAB = payload.ExternalAccountBinding;
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
            payload.Contact, 
            payload.TermsOfServiceAgreed ? DateTimeOffset.UtcNow : null, 
            effectiveEAB);

        await _accountStore.SaveAccountAsync(newAccount, cancellationToken);
        return newAccount;
    }


    public Task<Account?> FindAccountAsync(Jwk jwk, CancellationToken cancellationToken)
    {
        return _accountStore.FindAccountAsync(jwk, cancellationToken);
    }


    public async Task<Account?> LoadAcountAsync(string accountId, CancellationToken cancellationToken)
    {
        return await _accountStore.LoadAccountAsync(accountId, cancellationToken);
    }

    public async Task<Account> UpdateAccountAsync(string accountId, Payloads.UpdateAccount? payload, CancellationToken ct)
    {
        // The account will never be null here, since it has already been loaded during request authorization, nevertheless we add the check.
        var account = await LoadAcountAsync(accountId, ct)
            ?? throw AcmeErrors.AccountDoesNotExist().AsException();
                    
        if(payload?.Contact?.Any() == true)
        {
            account.Contacts = payload.Contact;
        }
        else if (payload?.TermsOfServiceAgreed != null)
        {
            if(!account.TOSAccepted.HasValue || account.TOSAccepted.Value.ToLocalTime() < _options.Value.TOS.LastUpdate)
            {
                account.TOSAccepted = DateTimeOffset.UtcNow;
            }
        }
        else if (payload?.Status != null)
        {
            if (account.Status != AccountStatus.Valid)
                throw new ConflictRequestException(AccountStatus.Valid, account.Status);

            var newStatus = Enum.Parse<AccountStatus>(payload.Status, ignoreCase: true);
            if (newStatus != AccountStatus.Deactivated)
                throw new MalformedRequestException("Only deactivation is supported.");

            account.Status = newStatus;
        }
        else
        {
            return account;
        }

        await _accountStore.SaveAccountAsync(account, ct);
        return account;
    }

    public async Task<List<string>> GetOrderIdsAsync(string accountId, CancellationToken ct)
    {
        return await _accountStore.GetAccountOrders(accountId, ct);
    }
}
