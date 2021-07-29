using System.Threading;
using System.Threading.Tasks;

namespace TGIT.ACME.Protocol.Workers
{
    public interface IValidationWorker
    {
        Task RunAsync(CancellationToken cancellationToken);
    }
}
