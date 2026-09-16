using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Services.Processors
{
    public class OrderValidationQueueItem(OrderId orderId, TaskCompletionSource<Order>? issuanceCompletionSource)
    {
        public OrderId OrderId { get; } = orderId;
        public TaskCompletionSource<Order>? ValidationCompletionSource { get; } = issuanceCompletionSource;
    }
}
