using Microsoft.Extensions.Logging;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.Extensions;
using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Model.Storage;
using Th11s.ACMEServer.Services.Processors;
using Payloads = Th11s.ACMEServer.HttpModel.Payloads;

namespace Th11s.ACMEServer.Services;

public class DefaultOrderService(
    IOrderStore orderStore,
    ICertificateStore certificateStore,
    IIssuanceProfileSelector issuanceProfileSelector,
    IAuthorizationFactory authorizationFactory,
    ICsrValidator csrValidator,
    OrderValidationQueue validationQueue,
    CertificateIssuanceQueue issuanceQueue,
    ILogger<DefaultOrderService> logger
    ) : IOrderService
{
    private readonly IOrderStore _orderStore = orderStore;
    private readonly ICertificateStore _certificateStore = certificateStore;
    private readonly IIssuanceProfileSelector _issuanceProfileSelector = issuanceProfileSelector;
    private readonly IAuthorizationFactory _authorizationFactory = authorizationFactory;
    private readonly ICsrValidator _csrValidator = csrValidator;
    private readonly OrderValidationQueue _validationQueue = validationQueue;
    private readonly CertificateIssuanceQueue _issuanceQueue = issuanceQueue;
    private readonly ILogger<DefaultOrderService> _logger = logger;

    public async Task<Order> CreateOrderAsync(
        AccountId accountId, 
        bool hasExternalAccountBinding,
        Payloads.CreateOrder payload,
        CancellationToken cancellationToken)
    {
        var identifiers = payload.Identifiers?
            .Select(i => new Identifier(i.Type, i.Value))
            .ToList();

        if (identifiers == null || identifiers.Count == 0)
        {
            _logger.LogDebug("No identifiers submitted for order creation.");
            throw new MalformedRequestException("No identifiers submitted");
        }

        var order = new Order(accountId, identifiers)
        {
            NotBefore = payload.NotBefore,
            NotAfter = payload.NotAfter
        };

        var requestedProfile = string.IsNullOrEmpty(payload.Profile) ? ProfileName.None : new ProfileName(payload.Profile);
        order.Profile = await _issuanceProfileSelector.SelectProfile(order, hasExternalAccountBinding, requestedProfile, cancellationToken);

        _authorizationFactory.CreateAuthorizations(order);
        // Use the minimum expiration date of all authorizations as order expiration
        order.Expires = order.Authorizations.Min(a => a.Expires);

        _logger.LogInformation("Created order {orderId} for account {accountId} with identifiers {identifiers} and profile {profile}", order.OrderId, accountId, order.Identifiers.AsLogString(), order.Profile);
        await _orderStore.SaveOrderAsync(order, cancellationToken);

        return order;
    }

    public async Task<byte[]> GetCertificate(AccountId accountId, OrderId orderId, CancellationToken cancellationToken)
    {
        var order = await LoadOrderAndAuthorizeAsync(accountId, orderId, cancellationToken);
        if (order.Status != OrderStatus.Valid || order.CertificateId == null)
        {
            _logger.LogDebug("Order {orderId} is not valid. Cannot return certificate.", orderId);
            throw new ConflictRequestException(OrderStatus.Valid, order.Status);
        }

        var certificate = await LoadCertificateAndAuthorizeAsync(accountId, order.CertificateId, cancellationToken);
        return certificate.X509Certificates;
    }

    public async Task<Order?> GetOrderAsync(AccountId accountId, OrderId orderId, CancellationToken cancellationToken)
    {
        var order = await LoadOrderAndAuthorizeAsync(accountId, orderId, cancellationToken);

        return order;
    }

    public async Task<Challenge> ProcessChallengeAsync(AccountId accountId, OrderId orderId, AuthorizationId authId, ChallengeId challengeId, AcmeJwsToken acmeRequest, CancellationToken cancellationToken)
    { 
        var order = await LoadOrderAndAuthorizeAsync(accountId, orderId, cancellationToken);

        var authZ = order.GetAuthorization(authId);
        var challenge = authZ?.GetChallenge(challengeId);

        if (authZ == null || challenge == null)
            throw new NotFoundException();

        // If the challenge exists AND is not pending, we return it,
        // since some clients are not RFC complaint and poll on the challenge
        if (challenge.Status != ChallengeStatus.Pending)
            return challenge;


        if (order.Status != OrderStatus.Pending)
        {
            _logger.LogDebug("Order {orderId} is not pending. Cannot process challenge.", orderId);
            throw new ConflictRequestException(OrderStatus.Pending, order.Status);
        }

        if (authZ.Status != AuthorizationStatus.Pending)
        {
            _logger.LogDebug("Challenge {challengeId} for authorization {authId} is not pending. Cannot process challenge.", challengeId, authId);
            throw new ConflictRequestException(AuthorizationStatus.Pending, authZ.Status);
        }

        challenge.SetStatus(ChallengeStatus.Processing);
        authZ.SelectChallenge(challenge);

        // Some challenges like device-attest-01 have a payload, that we'll store
        challenge.Payload = acmeRequest.Payload;

        _logger.LogInformation("Processing challenge {challengeId} for order {orderId}", challengeId, orderId);
        await _orderStore.SaveOrderAsync(order, cancellationToken);
        _validationQueue.Writer.TryWrite(order.OrderId);

        return challenge;
    }

    public async Task<Order> ProcessCsr(AccountId accountId, OrderId orderId, Payloads.FinalizeOrder payload, CancellationToken cancellationToken)
    {
        var order = await LoadOrderAndAuthorizeAsync(accountId, orderId, cancellationToken);
        
        if(order.Status != OrderStatus.Ready)
        {
            // This is not defined in the specs, but some clients resubmit the csr while waiting.
            // We'll return the current order, if the csr did not change.
            if(order.Status == OrderStatus.Processing || order.Status == OrderStatus.Valid)
            {
                if(payload.Csr == order.CertificateSigningRequest)
                {
                    return order;
                }

                throw new ConflictRequestException("The order was alread 'processing' or 'valid' and the client tried to submit another csr.");
            }

            throw new ConflictRequestException(OrderStatus.Ready, order.Status);
        }

        if (string.IsNullOrWhiteSpace(payload.Csr))
            throw new MalformedRequestException("CSR may not be empty.");

        order.CertificateSigningRequest = payload.Csr;
        var validationResult = await _csrValidator.ValidateCsrAsync(order, cancellationToken);

        if (validationResult.IsValid)
        {
            order.SetStatus(OrderStatus.Processing);
        }
        else
        {
            order.Error = validationResult.Error;
            order.SetStatus(OrderStatus.Invalid);
        }

        await _orderStore.SaveOrderAsync(order, cancellationToken);
        if(order.Status == OrderStatus.Processing)
        {
            _issuanceQueue.Writer.TryWrite(order.OrderId);
        }

        return order;
    }


    private async Task<Order> LoadOrderAndAuthorizeAsync(AccountId accountId, OrderId orderId, CancellationToken cancellationToken)
    {
        var order = await _orderStore.LoadOrderAsync(orderId, cancellationToken) 
            ?? throw new NotFoundException();
        
        if (order.AccountId != accountId)
        {
            throw new NotAllowedException();
        }

        return order;
    }

    private async Task<CertificateContainer> LoadCertificateAndAuthorizeAsync(AccountId accountId, CertificateId certificateId, CancellationToken cancellationToken)
    {
        var certificate = await _certificateStore.LoadCertificateAsync(certificateId, cancellationToken)
            ?? throw new NotFoundException();
        
        if (certificate.AccountId != accountId)
        {
            throw new NotAllowedException();
        }

        return certificate;
    }
}
