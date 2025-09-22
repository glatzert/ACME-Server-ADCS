namespace Th11s.ACMEServer.Model.Storage;

public interface ICertificateStore
{
    public Task SaveCertificateAsync(CertificateInfo certificateInfo, CancellationToken cancellationToken);

    public Task<CertificateInfo?> LoadCertificateAsync(string certificateId, CancellationToken cancellationToken);
}

public class CertificateInfo
{
}