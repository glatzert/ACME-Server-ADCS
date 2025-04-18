using Microsoft.Extensions.Logging;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.Storage;

namespace Th11s.ACMEServer.Services;

public class DefaultNonceService(INonceStore nonceStore, ILogger<DefaultNonceService> logger) : INonceService
{
    private readonly INonceStore _nonceStore = nonceStore;
    private readonly ILogger<DefaultNonceService> _logger = logger;

    public async Task<Nonce> CreateNonceAsync(CancellationToken cancellationToken)
    {
        var nonce = new Nonce(GuidString.NewValue());

        await _nonceStore.SaveNonceAsync(nonce, cancellationToken);
        _logger.LogInformation("Created and saved new nonce: {nonce}.", nonce.Token);

        return nonce;
    }
}
