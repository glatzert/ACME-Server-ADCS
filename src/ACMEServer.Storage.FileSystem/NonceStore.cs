using ACMEServer.Storage.FileSystem.Configuration;
using Microsoft.Extensions.Options;
using System.Globalization;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Storage;

namespace ACMEServer.Storage.FileSystem;

public class NonceStore : INonceStore
{
    private readonly IOptions<FileStoreOptions> _options;

    public NonceStore(IOptions<FileStoreOptions> options)
    {
        _options = options;
        Directory.CreateDirectory(_options.Value.NonceDirectory);
    }

    public async Task SaveNonceAsync(Nonce nonce, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(nonce);

        var nonceFile = Path.Combine(_options.Value.NonceDirectory, nonce.Token);
        await File.WriteAllTextAsync(nonceFile, DateTime.Now.ToString("o", CultureInfo.InvariantCulture), cancellationToken);
    }

    public async Task<bool> TryConsumeNonceAsync(Nonce nonce, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(nonce);

        var nonceFile = Path.Combine(_options.Value.NonceDirectory, nonce.Token);

        try
        {
            // Use FileStream with Delete access to atomically check if the file exists and delete it upon closing it
            var stream = new FileStream(nonceFile, FileMode.Open, FileAccess.Read, FileShare.None, 1, FileOptions.DeleteOnClose);
            await stream.DisposeAsync();
            
            return true;
        }
        catch
        {
            return false;
        }
    }
}
