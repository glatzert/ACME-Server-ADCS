using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Model.Storage;

public interface ICertificateStore
{
    public Task SaveCertificateAsync(OrderCertificates certificateInfo, CancellationToken cancellationToken);

    public Task<OrderCertificates?> LoadCertificateAsync(CertificateId certificateId, CancellationToken cancellationToken);
}
