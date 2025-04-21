using System.Threading.Channels;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Services.Processors
{
    public sealed class OrderValidationQueue
    {
        private readonly Channel<OrderId> _queue = Channel.CreateUnbounded<OrderId>();

        public ChannelReader<OrderId> Reader => _queue.Reader;
        public ChannelWriter<OrderId> Writer => _queue.Writer;
    }
}
