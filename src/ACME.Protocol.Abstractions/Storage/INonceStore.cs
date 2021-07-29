using System.Threading;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.Model;

namespace TGIT.ACME.Protocol.Storage
{
    public interface INonceStore
    {
        Task SaveNonceAsync(Nonce nonce, CancellationToken cancellationToken);
        Task<bool> TryRemoveNonceAsync(Nonce nonce, CancellationToken cancellationToken);
    }
}
