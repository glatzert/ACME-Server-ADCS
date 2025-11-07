using ACMEServer.Storage.FileSystem.Configuration;
using Microsoft.Extensions.Options;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Exceptions;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Model.Storage;

namespace ACMEServer.Storage.FileSystem
{
    public class CertificateStore : StoreBase, ICertificateStore
    {
        public CertificateStore(IOptions<FileStoreOptions> options) 
            : base(options)
        { }

        private string GetCertificatePath(CertificateId certificateId)
            => Path.Combine(Options.Value.CertificateDirectory, $"{certificateId.Value}.json");

        public async Task<CertificateContainer?> LoadCertificateAsync(CertificateId certificateId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(certificateId.Value) || !IdentifierRegex.IsMatch(certificateId.Value))
                throw new MalformedRequestException("CertificateId does not match expected format.");

            var certificateFilePath = GetCertificatePath(certificateId);

            var certificates = await LoadFromPath<CertificateContainer>(certificateFilePath, cancellationToken);
            return certificates;
        }

        public async Task SaveCertificateAsync(CertificateContainer certificates, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(certificates);

            var certificateFilePath = GetCertificatePath(certificates.CertificateId);

            Directory.CreateDirectory(Path.GetDirectoryName(certificateFilePath)!);

            using var fileStream = File.Open(certificateFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            var existingCertificates = await LoadFromStream<CertificateContainer>(fileStream, cancellationToken);

            HandleVersioning(existingCertificates, certificates);
            await ReplaceFileStreamContent(fileStream, certificates, cancellationToken);
        }
    }
}
