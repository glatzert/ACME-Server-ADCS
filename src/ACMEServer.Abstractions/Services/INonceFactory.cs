using Th11s.ACMEServer.Model;

namespace Th11s.ACMEServer.Services;

public interface INonceFactory
{
    Task<Nonce> CreateNonceAsync(CancellationToken cancellationToken);

}
