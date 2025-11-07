using Th11s.ACMEServer.Model.Primitives;

namespace Th11s.ACMEServer.Model.Storage;

public interface ICertificateStore
{
    public Task SaveCertificateAsync(CertificateContainer certificateInfo, CancellationToken cancellationToken);

    public Task<CertificateContainer?> LoadCertificateAsync(CertificateId certificateId, CancellationToken cancellationToken);
}
