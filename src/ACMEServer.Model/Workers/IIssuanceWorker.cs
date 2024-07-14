using System.Threading;
using System.Threading.Tasks;

namespace Th11s.ACMEServer.Model.Workers
{
    public interface IIssuanceWorker
    {
        Task RunAsync(CancellationToken cancellationToken);
    }
}
