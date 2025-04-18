using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services;

public interface INonceService
{
    Task<Nonce> CreateNonceAsync(CancellationToken cancellationToken);

}
