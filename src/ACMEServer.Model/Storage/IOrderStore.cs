using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Model.Storage
{
    public interface IOrderStore
    {
        Task<Order?> LoadOrderAsync(string orderId, CancellationToken cancellationToken);

        Task SaveOrderAsync(Order order, CancellationToken cancellationToken);

        Task<List<Order>> GetValidatableOrders(CancellationToken cancellationToken);
        Task<List<Order>> GetFinalizableOrders(CancellationToken cancellationToken);
    }
}
