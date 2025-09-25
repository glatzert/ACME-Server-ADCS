using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Model.Storage;

public interface IOrderStore
{
    Task<Order?> LoadOrderAsync(OrderId orderId, CancellationToken cancellationToken);

    Task SaveOrderAsync(Order order, CancellationToken cancellationToken);

    Task<List<Order>> GetValidatableOrders(CancellationToken cancellationToken);
    Task<List<Order>> GetFinalizableOrders(CancellationToken cancellationToken);
}
