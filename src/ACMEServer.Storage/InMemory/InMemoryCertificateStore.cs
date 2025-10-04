using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Model.Storage;

namespace ACMEServer.Storage.InMemory
{
    internal class InMemoryCertificateStore : ICertificateStore
    {
        private readonly Dictionary<CertificateId, OrderCertificates> _certificates = [];

        public Task<OrderCertificates?> LoadCertificateAsync(CertificateId certificateId, CancellationToken cancellationToken)
            => Task.FromResult<OrderCertificates?>(_certificates[certificateId]);

        public Task SaveCertificateAsync(OrderCertificates certificateInfo, CancellationToken cancellationToken)
            => Task.FromResult(_certificates[certificateInfo.CertificateId] = certificateInfo);
    }
}
