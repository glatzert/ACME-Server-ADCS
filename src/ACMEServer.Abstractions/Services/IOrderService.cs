using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.JWS;
using Payloads = Th11s.ACMEServer.HttpModel.Payloads;

namespace Th11s.ACMEServer.Services;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(string accountId,
        Payloads.CreateOrder payload,
        CancellationToken cancellationToken);

    Task<Order?> GetOrderAsync(string accountId, string orderId, CancellationToken cancellationToken);

    Task<Order> ProcessCsr(string accountId, string orderId, Payloads.FinalizeOrder payload, CancellationToken cancellationToken);
    Task<byte[]> GetCertificate(string accountId, string orderId, CancellationToken cancellationToken);


    Task<Challenge> ProcessChallengeAsync(string accountId, string orderId, string authId, string challengeId, AcmeJwsToken acmeRequest, CancellationToken cancellationToken);
}
