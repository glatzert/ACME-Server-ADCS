using Th11s.ACMEServer.HttpModel;
using Th11s.ACMEServer.Model.JWS;

namespace Th11s.ACMEServer.Services;

public class DefaultRenewalInfoService : IRenewalInfoService
{
    public Task<RenewalInfo> GetRenewalInformationAsync(AcmeJwsToken acmeRequest, CancellationToken requestAborted)
    {
        // TODO: Implement the logic to retrieve renewal information based on the provided acme request.
        // TODO: The admins should have a way to force a renewal for all certificates older than a certain date, or for all certificates with a certain profile.
        throw new NotImplementedException();
    }
}
