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

    Task<Order?> GetOrderAsync(AccountId accountId, string orderId, CancellationToken cancellationToken);

    Task<Order> ProcessCsr(AccountId accountId, string orderId, Payloads.FinalizeOrder payload, CancellationToken cancellationToken);
    Task<byte[]> GetCertificate(AccountId accountId, string orderId, CancellationToken cancellationToken);


    Task<Challenge> ProcessChallengeAsync(AccountId accountId, string orderId, string authId, string challengeId, AcmeJwsToken acmeRequest, CancellationToken cancellationToken);
}
