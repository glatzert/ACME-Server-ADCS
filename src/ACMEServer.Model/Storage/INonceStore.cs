using System.Threading;
using System.Threading.Tasks;

namespace Th11s.ACMEServer.Model.Storage
{
    public interface INonceStore
    {
        Task SaveNonceAsync(Nonce nonce, CancellationToken cancellationToken);
        Task<bool> TryRemoveNonceAsync(Nonce nonce, CancellationToken cancellationToken);
    }
}
