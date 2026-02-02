using Microsoft.Extensions.Logging;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Storage;

namespace Th11s.ACMEServer.Services;

public class DefaultNonceFactory(INonceStore nonceStore, ILogger<DefaultNonceFactory> logger) : INonceFactory
{
    private readonly INonceStore _nonceStore = nonceStore;
    private readonly ILogger<DefaultNonceFactory> _logger = logger;

    public async Task<Nonce> CreateNonceAsync(CancellationToken cancellationToken)
    {
        var nonce = new Nonce(GuidString.NewValue());

        await _nonceStore.SaveNonceAsync(nonce, cancellationToken);
        _logger.NonceCreated(nonce.Token);

        return nonce;
    }
}
