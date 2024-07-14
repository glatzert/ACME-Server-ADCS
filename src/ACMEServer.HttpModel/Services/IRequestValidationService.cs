
using System.Threading;
using System.Threading.Tasks;
using Th11s.ACMEServer.HttpModel.Requests;

namespace Th11s.ACMEServer.HttpModel.Services
{
    public interface IRequestValidationService
    {
        Task ValidateRequestAsync(AcmeRawPostRequest request, AcmeHeader header,
            string requestUrl, CancellationToken cancellationToken);
    }
}
