using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Model.Storage;

namespace ACMEServer.Storage.InMemory
{
    internal class InMemoryCertificateStore : ICertificateStore
    {
        private readonly Dictionary<CertificateId, CertificateContainer> _certificates = [];

        public Task<CertificateContainer?> LoadCertificateAsync(CertificateId certificateId, CancellationToken cancellationToken)
            => Task.FromResult<CertificateContainer?>(_certificates[certificateId]);

        public Task SaveCertificateAsync(CertificateContainer certificateInfo, CancellationToken cancellationToken)
            => Task.FromResult(_certificates[certificateInfo.CertificateId] = certificateInfo);
    }
}
