using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Model.Storage;
using Th11s.ACMEServer.Services.Processors;
using Payloads = Th11s.ACMEServer.HttpModel.Payloads;

namespace Th11s.ACMEServer.Services;

public class DefaultOrderService(
    IOrderStore orderStore,
    IIssuanceProfileSelector issuanceProfileSelector,
    IOrderValidator orderValidator,
    IAuthorizationFactory authorizationFactory,
    ICSRValidator csrValidator,
    OrderValidationQueue validationQueue,
    CertificateIssuanceQueue issuanceQueue
    ) : IOrderService
{
    private readonly IOrderStore _orderStore = orderStore;
    private readonly IIssuanceProfileSelector _issuanceProfileSelector = issuanceProfileSelector;
    private readonly IOrderValidator _orderValidator = orderValidator;
    private readonly IAuthorizationFactory _authorizationFactory = authorizationFactory;
    private readonly ICSRValidator _csrValidator = csrValidator;
    private readonly OrderValidationQueue _validationQueue = validationQueue;
    private readonly CertificateIssuanceQueue _issuanceQueue = issuanceQueue;

    public async Task<Order> CreateOrderAsync(
        string accountId, 
        bool hasExternalAccountBinding,
        Payloads.CreateOrder payload,
        CancellationToken cancellationToken)
    {
        var identifiers = payload.Identifiers?
            .Select(i => new Identifier(i.Type, i.Value))
            .ToList();

        if (identifiers == null || identifiers.Count == 0)
        {
            throw new MalformedRequestException("No identifiers submitted");
        }

        var order = new Order(accountId, identifiers)
        {
            NotBefore = payload.NotBefore,
            NotAfter = payload.NotAfter
        };

        order.Profile = await _issuanceProfileSelector.SelectProfile(order, hasExternalAccountBinding, ProfileName.None, cancellationToken);


        var validationResult = await _orderValidator.ValidateOrderAsync(order, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw validationResult.Error.AsException();
        }


        _authorizationFactory.CreateAuthorizations(order);

        await _orderStore.SaveOrderAsync(order, cancellationToken);

        return order;
    }

    public async Task<byte[]> GetCertificate(string accountId, string orderId, CancellationToken cancellationToken)
    {
        var order = await HandleLoadOrderAsync(accountId, orderId, cancellationToken);
        if (order.Status != OrderStatus.Valid)
        {
            throw new ConflictRequestException(OrderStatus.Valid, order.Status);
        }

        return order.Certificate!;
    }

    public async Task<Order?> GetOrderAsync(string accountId, string orderId, CancellationToken cancellationToken)
    {
        var order = await HandleLoadOrderAsync(accountId, orderId, cancellationToken);

        return order;
    }

    public async Task<Challenge> ProcessChallengeAsync(string accountId, string orderId, string authId, string challengeId, AcmeJwsToken acmeRequest, CancellationToken cancellationToken)
    {
        var order = await HandleLoadOrderAsync(accountId, orderId, cancellationToken);

        var authZ = order.GetAuthorization(authId);
        var challenge = authZ?.GetChallenge(challengeId);

        if (authZ == null || challenge == null)
            throw new NotFoundException();

        // If the challenge exists AND is not pending, we return it,
        // since some clients are not RFC complaint and poll on the challenge
        if (challenge.Status != ChallengeStatus.Pending)
            return challenge;


        if (order.Status != OrderStatus.Pending)
            throw new ConflictRequestException(OrderStatus.Pending, order.Status);
        if (authZ.Status != AuthorizationStatus.Pending)
            throw new ConflictRequestException(AuthorizationStatus.Pending, authZ.Status);

        challenge.SetStatus(ChallengeStatus.Processing);
        authZ.SelectChallenge(challenge);

        // Some challenges like device-attest-01 have a payload, that we'll store
        challenge.Payload = acmeRequest.Payload;

        await _orderStore.SaveOrderAsync(order, cancellationToken);
        _validationQueue.Writer.TryWrite(new(order.OrderId));

        return challenge;
    }

    public async Task<Order> ProcessCsr(string accountId, string orderId, Payloads.FinalizeOrder payload, CancellationToken cancellationToken)
    {
        var order = await HandleLoadOrderAsync(accountId, orderId, cancellationToken);
        
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

        var validationResult = await _csrValidator.ValidateCsrAsync(order, payload.Csr, cancellationToken);

        if (validationResult.IsValid)
        {
            order.CertificateSigningRequest = payload.Csr;
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
            _issuanceQueue.Writer.TryWrite(new(order.OrderId));
        }

        return order;
    }


    private async Task<Order> HandleLoadOrderAsync(string accountId, string orderId, CancellationToken cancellationToken)
    {
        var order = await _orderStore.LoadOrderAsync(orderId, cancellationToken) 
            ?? throw new NotFoundException();
        
        if (order.AccountId != accountId)
            throw new NotAllowedException();

        return order;
    }
}
