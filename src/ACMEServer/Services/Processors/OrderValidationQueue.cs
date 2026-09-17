using System.Threading.Channels;
using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Services.Processors
{
    public sealed class OrderValidationQueue
    {
        private readonly Channel<OrderValidationQueueItem> _queue = Channel.CreateUnbounded<OrderValidationQueueItem>();

        public ChannelReader<OrderValidationQueueItem> Reader => _queue.Reader;
        public ChannelWriter<OrderValidationQueueItem> Writer => _queue.Writer;
    }
}
