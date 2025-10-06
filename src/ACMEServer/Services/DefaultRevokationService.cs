using System.Security.Cryptography.X509Certificates;
using Th11s.ACMEServer.HttpModel.Payloads;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Model.Storage;

namespace Th11s.ACMEServer.Services
{
    public class DefaultRevokationService(ICertificateStore certificateStore) : IRevokationService
    {
        private readonly ICertificateStore _certificateStore = certificateStore;

        public async Task RevokeCertificateAsync(AcmeJwsToken acmeRequest, RevokeCertificate payload, CancellationToken cancellationToken)
        {
            var certificateBytes = Convert.FromBase64String(payload.Certificate);
            var certificate = new X509Certificate2(certificateBytes);

            var certificateId = CertificateId.FromX509Certificate(certificate);

            // First we locate the certificate to be revoked.
            var orderCertificates = await _certificateStore.LoadCertificateAsync(certificateId, cancellationToken) 
                ?? throw AcmeErrors.MalformedRequest("The specified certificate was not found.").AsException();


            // We now need to check if it's signed by the account that owns the certificate or the certificate itself.
            var isAuthorized = await IsRevokationAuthorizedAsync(acmeRequest, payload, cancellationToken);

            if (!isAuthorized)
            {
                throw AcmeErrors.Unauthorized().AsException();
            }
        }

        private Task<bool> IsRevokationAuthorizedAsync(AcmeJwsToken acmeRequest, RevokeCertificate payload, CancellationToken cancellationToken)
        {
            if (acmeRequest.AcmeHeader.Kid is not null)
            {
                // The request is signed by an account.
                var accountId = acmeRequest.AcmeHeader.GetAccountId();

            }
        }
    }
}
