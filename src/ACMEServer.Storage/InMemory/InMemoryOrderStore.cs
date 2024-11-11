using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Storage;

namespace ACMEServer.Storage.InMemory;

public class InMemoryOrderStore : IOrderStore
{
    private Dictionary<string, Order> _orders = [];

    public Task<List<Order>> GetFinalizableOrders(CancellationToken cancellationToken)
        => Task.FromResult(_orders.Values.Where(o => o.Status == OrderStatus.Processing).ToList());

    public Task<List<Order>> GetValidatableOrders(CancellationToken cancellationToken)
        => Task.FromResult(_orders.Values.Where(order => order.Authorizations.Any(a => a.Challenges.Any(c => c.Status == ChallengeStatus.Processing))).ToList());

    public Task<Order?> LoadOrderAsync(string orderId, CancellationToken cancellationToken)
        => Task.FromResult(_orders.TryGetValue(orderId, out var order) ? order : null);

    public Task SaveOrderAsync(Order order, CancellationToken cancellationToken)
        => Task.FromResult(_orders[order.OrderId] = order);
}
