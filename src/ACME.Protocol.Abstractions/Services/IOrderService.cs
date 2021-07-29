using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.Model;

namespace TGIT.ACME.Protocol.Services
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(Account account,
            IEnumerable<Identifier> identifiers, 
            DateTimeOffset? notBefore, DateTimeOffset? notAfter, 
            CancellationToken cancellationToken);
        
        Task<Order?> GetOrderAsync(Account account, string orderId, CancellationToken cancellationToken);

        Task<Order> ProcessCsr(Account account, string orderId, string? csr, CancellationToken cancellationToken);
        Task<byte[]> GetCertificate(Account account, string orderId, CancellationToken cancellationToken);


        Task<Challenge> ProcessChallengeAsync(Account account, string orderId, string authId, string challengeId, CancellationToken cancellationToken);
    }
}
