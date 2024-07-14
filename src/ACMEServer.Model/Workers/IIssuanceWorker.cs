using System.Threading;
using System.Threading.Tasks;

namespace TGIT.ACME.Protocol.Workers
{
    public interface IIssuanceWorker
    {
        Task RunAsync(CancellationToken cancellationToken);
    }
}
