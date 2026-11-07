using Th11s.ACMEServer.HttpModel;
using Th11s.ACMEServer.Model.JWS;

namespace Th11s.ACMEServer.Services;

public interface IRenewalInfoService
{
    Task<RenewalInfo> GetRenewalInformationAsync(AcmeJwsToken acmeRequest, CancellationToken requestAborted);
}

