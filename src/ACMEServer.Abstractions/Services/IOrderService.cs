using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(string accountId,
        IEnumerable<Identifier> identifiers,
        DateTimeOffset? notBefore, DateTimeOffset? notAfter,
        CancellationToken cancellationToken);

    Task<Order?> GetOrderAsync(string accountId, string orderId, CancellationToken cancellationToken);

    Task<Order> ProcessCsr(string accountId, string orderId, string? csr, CancellationToken cancellationToken);
    Task<byte[]> GetCertificate(string accountId, string orderId, CancellationToken cancellationToken);


    Task<Challenge> ProcessChallengeAsync(string accountId, string orderId, string authId, string challengeId, CancellationToken cancellationToken);
}
