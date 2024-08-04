using ACMEServer.Storage.FileSystem.Configuration;
using Microsoft.Extensions.Options;
using System.Globalization;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Storage;

namespace ACMEServer.Storage.FileSystem
{
    public class NonceStore : INonceStore
    {
        private readonly IOptions<FileStoreOptions> _options;

        public NonceStore(IOptions<FileStoreOptions> options)
        {
            _options = options;
            Directory.CreateDirectory(_options.Value.NoncePath);
        }

        public async Task SaveNonceAsync(Nonce nonce, CancellationToken cancellationToken)
        {
            if (nonce is null)
                throw new ArgumentNullException(nameof(nonce));

            var noncePath = Path.Combine(_options.Value.NoncePath, nonce.Token);
            await File.WriteAllTextAsync(noncePath, DateTime.Now.ToString("o", CultureInfo.InvariantCulture), cancellationToken);
        }

        public Task<bool> TryRemoveNonceAsync(Nonce nonce, CancellationToken cancellationToken)
        {
            if (nonce is null)
                throw new ArgumentNullException(nameof(nonce));

            var noncePath = Path.Combine(_options.Value.NoncePath, nonce.Token);
            if (!File.Exists(noncePath))
                return Task.FromResult(false);

            File.Delete(noncePath);
            return Task.FromResult(true);
        }
    }
}
