using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.Model.Primitives;
using Payloads = Th11s.ACMEServer.HttpModel.Payloads;

namespace Th11s.ACMEServer.Services;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(
        AccountId accountId,
        bool hasExternalAccountBinding,
        Payloads.CreateOrder payload,
        CancellationToken cancellationToken);

    Task<Order?> GetOrderAsync(AccountId accountId, OrderId orderId, CancellationToken cancellationToken);

    Task<Order> ProcessCsr(AccountId accountId, OrderId orderId, Payloads.FinalizeOrder payload, CancellationToken cancellationToken);
    Task<byte[]> GetCertificate(AccountId accountId, OrderId orderId, CancellationToken cancellationToken);


    Task<Challenge> ProcessChallengeAsync(AccountId accountId, OrderId orderId, AuthorizationId authId, ChallengeId challengeId, AcmeJwsToken acmeRequest, CancellationToken cancellationToken);
}
