using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Th11s.ACMEServer.Configuration;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Model.Storage;
using Payloads = Th11s.ACMEServer.HttpModel.Payloads;

namespace Th11s.ACMEServer.Services;

public class DefaultAccountService(
    IExternalAccountBindingValidator eabValidator,
    IOptions<ACMEServerOptions> options,
    IAccountStore accountStore,
    ILogger<DefaultAccountService> logger) : IAccountService
{
    private readonly IExternalAccountBindingValidator _eabValidator = eabValidator;
    private readonly IOptions<ACMEServerOptions> _options = options;
    private readonly IAccountStore _accountStore = accountStore;
    private readonly ILogger<DefaultAccountService> _logger = logger;

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
            _logger.LogDebug("Payload contains externalAccountBinding. Validating ...");
            var eabValidationError = await _eabValidator.ValidateExternalAccountBindingAsync(header, effectiveEAB, cancellationToken);

            if(eabValidationError != null)
            {
                if (requiresExternalAccountBinding)
                {
                    throw eabValidationError.AsException();
                }

                    _logger.LogDebug("ExternalAccountBinding could not be validated. EAB not required, so it's ignored.");
                    effectiveEAB = null;
                }
            }

        
        var newAccount = new Account(
            header.Jwk!, 
            payload.Contact, 
            payload.TermsOfServiceAgreed ? DateTimeOffset.UtcNow : null, 
            effectiveEAB);

        _logger.LogInformation("Creating new account with id {accountId}", newAccount.AccountId);
        await _accountStore.SaveAccountAsync(newAccount, cancellationToken);
        return newAccount;
    }


    public Task<Account?> FindAccountAsync(Jwk jwk, CancellationToken cancellationToken)
    {
        return _accountStore.FindAccountAsync(jwk, cancellationToken);
    }


    public async Task<Account?> LoadAcountAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        return await _accountStore.LoadAccountAsync(accountId, cancellationToken);
    }

    public async Task<Account> UpdateAccountAsync(AccountId accountId, Payloads.UpdateAccount? payload, CancellationToken ct)
    {
        // The account will never be null here, since it has already been loaded during request authorization, nevertheless we add the check.
        var account = await LoadAcountAsync(accountId, ct)
            ?? throw AcmeErrors.AccountDoesNotExist().AsException();
                    
        if(payload?.Contact is { Count: > 0})
        {
            _logger.LogDebug("Updating contact information for account {accountId}", accountId);
            account.Contacts = payload.Contact;
        }
        else if (payload?.TermsOfServiceAgreed != null)
        {
            _logger.LogDebug("Updating TOS acceptance for account {accountId}", accountId);
            if (!account.TOSAccepted.HasValue || account.TOSAccepted.Value.ToLocalTime() < _options.Value.TOS.LastUpdate)
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

            _logger.LogDebug("Updating status for account {accountId} to {status}", accountId, newStatus);
            account.Status = newStatus;
        }
        else
        {
            return account;
        }

        await _accountStore.SaveAccountAsync(account, ct);
        return account;
    }

    public async Task<List<OrderId>> GetOrderIdsAsync(AccountId accountId, CancellationToken ct)
    {
        return await _accountStore.GetAccountOrders(accountId, ct);
    }
}
