using System.Threading;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.Model;

namespace TGIT.ACME.Protocol.Services
{
    public interface INonceService
    {
        Task<Nonce> CreateNonceAsync(CancellationToken cancellationToken);

    }
}
