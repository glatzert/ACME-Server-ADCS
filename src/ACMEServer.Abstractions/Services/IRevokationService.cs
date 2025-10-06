using Th11s.ACMEServer.Model.JWS;
using Payloads = Th11s.ACMEServer.HttpModel.Payloads;

namespace Th11s.ACMEServer.Services;

public interface IRevokationService
{
    Task RevokeCertificateAsync(AcmeJwsToken acmeRequest, Payloads.RevokeCertificate payload, CancellationToken cancellationToken);
}
