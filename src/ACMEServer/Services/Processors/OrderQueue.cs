using System.Threading.Channels;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Services.Processors
{

    public sealed class OrderQueue : IDisposable
    {
        private readonly Channel<OrderId> _channel;

        public OrderQueue()
        {
            _channel = Channel.CreateUnbounded<OrderId>();
        }

        public void Enqueue(OrderId item)
            => _channel.Writer.TryWrite(item);

        public IAsyncEnumerable<OrderId> DequeueAllAsync(CancellationToken cancellationToken)
            => _channel.Reader.ReadAllAsync(cancellationToken);

        public void Dispose()
        {
            _channel.Writer.Complete();
        }
    }
}
