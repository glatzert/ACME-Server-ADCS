using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Th11s.ACMEServer.AspNetCore.Extensions;
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
        // https://www.rfc-editor.org/rfc/rfc8555#section-7.3.1
        var existingAccount = await _accountStore.FindAccountAsync(header.Jwk, cancellationToken);
        if (existingAccount != null)
        {
            return existingAccount;
        }

        var requiresTOSAgreement = _options.Value.TOS.RequireAgreement;
        if (requiresTOSAgreement && !payload.TermsOfServiceAgreed)
        {
            _logger.LogInformation("Terms of service agreement is required, but client did not agree to the terms of service.");
            throw AcmeErrors.UserActionRequired("Terms of service need to be accepted.").AsException();
        }

        var requiresExternalAccountBinding = _options.Value.ExternalAccountBinding?.Required == true;
        if (requiresExternalAccountBinding && payload.ExternalAccountBinding == null)
        {
            _logger.LogWarning("External account binding is required, but payload did not contain externalAccountBinding.");
            throw AcmeErrors.ExternalAccountRequired().AsException();
        }

        var effectiveEAB = payload.ExternalAccountBinding;
        if (effectiveEAB != null)
        {
            _logger.LogDebug("Payload contains externalAccountBinding. Validating ...");
            var eabValidationError = await _eabValidator.ValidateExternalAccountBindingAsync(header, effectiveEAB, cancellationToken);

            if (eabValidationError != null)
            {
                if (requiresExternalAccountBinding)
                {
                    _logger.LogWarning("ExternalAccountBinding validation failed.");
                    throw eabValidationError.AsException();
                }

                _logger.LogWarning("ExternalAccountBinding could not be validated. EAB not required, so it's ignored.");
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

    public async Task<Account> UpdateAccountAsync(AccountId accountId, Payloads.UpdateAccount? payload, CancellationToken ct)
    {
        // The account will never be null here, since it has already been loaded during request authorization, nevertheless we add the check.
        var account = await LoadAcountAsync(accountId, ct)
            ?? throw AcmeErrors.AccountDoesNotExist().AsException();

        if (payload?.Contact is { Count: > 0 })
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

    public async Task<Account> ChangeAccountKeyAsync(AccountId accountId, AcmeJwsToken outerJws, AcmeJwsToken innerJws, Payloads.ChangeAccountKey payload, CancellationToken cancellationToken)
    {
        // Check that the JWS protected header of the inner JWS has a JWK
        if (innerJws.AcmeHeader.Jwk is null)
        {
            _logger.LogWarning("Inner JWS did not contain a JWK.");
            throw AcmeErrors.MalformedRequest("Inner JWS did not contain a JWK.").AsException();
        }

        // Check that the inner JWS verifies using the key in its jwk
        if (!innerJws.AcmeHeader.Jwk.SecurityKey.IsSignatureValid(innerJws, _logger))
        {
            _logger.LogWarning("Inner JWS did not have a valid signature.");
            throw AcmeErrors.MalformedRequest("Inner JWS did not have a valid signature.").AsException();
        }

        // Check that the payload of the inner JWS is a well-formed keyChange object
        if (payload.Account is null || payload.OldKey is null)
        {
            throw AcmeErrors.MalformedRequest("The keyChange payload is not valid.").AsException();
        }

        // Check that the url parameters of the inner and outer JWS are the same
        if (innerJws.AcmeHeader.Url != outerJws.AcmeHeader.Url)
        {
            _logger.LogWarning("Inner JWS URL does not match outer JWS URL.");
            throw AcmeErrors.MalformedRequest("Inner JWS URL does not match outer JWS URL.").AsException();
        }

        if (innerJws.AcmeHeader.Nonce is not null)
        {
            _logger.LogWarning("Inner JWS may not contain nonce.");
            throw AcmeErrors.MalformedRequest("Inner JWS may not contain nonce.").AsException();
        }



        // Check that the payloads account field matches the Kid of the outer JWS
        if (payload.Account != outerJws.AcmeHeader.Kid)
        {
            _logger.LogWarning("Payload did not contain the correct accountUrl");
            throw AcmeErrors.MalformedRequest("Payload did not contain the correct accountUrl").AsException();
        }

        // Check that the "oldKey" field of the keyChange object is the same as the account key for the account in question.
        var account = await _accountStore.LoadAccountAsync(accountId, cancellationToken);
        if (payload.OldKey.Json != account!.Jwk.Json)
        {
            _logger.LogWarning("Payload did not contain the correct old key.");
            throw AcmeErrors.MalformedRequest("Payload did not contain the correct old key.").AsException();
        }

        var existingAccount = await _accountStore.FindAccountAsync(innerJws.AcmeHeader.Jwk, cancellationToken);
        if (existingAccount != null)
        {
            _logger.LogWarning("The JWK used to change the account key was already known.");
            throw AcmeErrors.JwkAlreadyInUse().AsException();
        }

        await _accountStore.UpdateAccountKeyAsync(account, innerJws.AcmeHeader.Jwk, cancellationToken);
        return account;
    }


    public Task<Account?> FindAccountAsync(Jwk jwk, CancellationToken cancellationToken)
    {
        return _accountStore.FindAccountAsync(jwk, cancellationToken);
    }

    public async Task<Account?> LoadAcountAsync(AccountId accountId, CancellationToken cancellationToken)
    {
        return await _accountStore.LoadAccountAsync(accountId, cancellationToken);
    }

    public async Task<List<OrderId>> GetOrderIdsAsync(AccountId accountId, CancellationToken ct)
    {
        return await _accountStore.GetAccountOrders(accountId, ct);
    }
}
