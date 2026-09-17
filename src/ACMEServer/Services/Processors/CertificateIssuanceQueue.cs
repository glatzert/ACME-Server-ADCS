using System.Threading.Channels;

namespace Th11s.ACMEServer.Services.Processors
{
    public sealed class CertificateIssuanceQueue
    {
        private readonly Channel<CertificateIssuanceQueueItem> _queue = Channel.CreateUnbounded<CertificateIssuanceQueueItem>();

        public ChannelReader<CertificateIssuanceQueueItem> Reader => _queue.Reader;
        public ChannelWriter<CertificateIssuanceQueueItem> Writer => _queue.Writer;
    }
}
