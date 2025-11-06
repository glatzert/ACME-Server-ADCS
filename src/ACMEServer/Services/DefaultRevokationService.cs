using DnsClient.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using Th11s.ACMEServer.HttpModel.Payloads;
using Th11s.ACMEServer.Model;
using Th11s.ACMEServer.Model.JWS;
using Th11s.ACMEServer.Model.Primitives;
using Th11s.ACMEServer.Model.Storage;

namespace Th11s.ACMEServer.Services
{
    public class DefaultRevokationService(
        ICertificateStore certificateStore, 
        IOrderStore orderStore,
        ICertificateIssuer certificateIssuer,
        ILogger<DefaultRevokationService> logger)
        : IRevokationService
    {
        private readonly ICertificateStore _certificateStore = certificateStore;
        private readonly IOrderStore _orderStore = orderStore;
        private readonly ICertificateIssuer _certificateIssuer = certificateIssuer;
        private readonly ILogger<DefaultRevokationService> _logger = logger;

        public async Task RevokeCertificateAsync(AcmeJwsToken acmeRequest, RevokeCertificate payload, CancellationToken cancellationToken)
        {
            var certificateBytes = Convert.FromBase64String(payload.Certificate);
#if NET10_0_OR_GREATER
            var certificate = X509CertificateLoader.LoadCertificate(certificateBytes);
#else
            var certificate = new X509Certificate2(certificateBytes);
#endif

            var certificateId = CertificateId.FromX509Certificate(certificate);

            // First we locate the certificate to be revoked.
            var orderCertificates = await _certificateStore.LoadCertificateAsync(certificateId, cancellationToken) 
                ?? throw AcmeErrors.MalformedRequest("The specified certificate was not found.").AsException();

            if (orderCertificates.RevokationStatus == RevokationStatus.Revoked)
            {
                _logger.LogWarning("Attempt to revoke an already revoked certificate. CertificateId: {CertificateId}", certificateId);
                throw AcmeErrors.AlreadyRevoked().AsException();
            }

            var isAuthorized = IsAuthorizedViaAccount(acmeRequest, orderCertificates) 
                || IsAuthorizedViaCertificate(acmeRequest, certificate);

            if (!isAuthorized)
            {
                _logger.LogWarning("Unauthorized revokation attempt. CertificateId: {CertificateId}", certificateId);
                throw AcmeErrors.Unauthorized().AsException();
            }

            var order = await _orderStore.LoadOrderAsync(orderCertificates.OrderId, cancellationToken);
            if (order is null)
            {
                _logger.LogWarning("Could not locate order for certificate: {CertificateId} - revocation not possible", certificateId);
                throw AcmeErrors.MalformedRequest("The order associated with the certificate was not found.").AsException();
            }

            await _certificateIssuer.RevokeCertificateAsync(order.Profile, certificate, payload.Reason, cancellationToken);
            orderCertificates.RevokationStatus = RevokationStatus.Revoked;

            await _certificateStore.SaveCertificateAsync(
                orderCertificates,
                cancellationToken
            );
        }

        private bool IsAuthorizedViaAccount(AcmeJwsToken acmeRequest, OrderCertificates orderCertificates)
        {
            if (acmeRequest.AcmeHeader.Kid is not null)
            {
                var accountId = acmeRequest.AcmeHeader.GetAccountId();
                return accountId == orderCertificates.AccountId;
            }

            return false;
        }

        private bool IsAuthorizedViaCertificate(AcmeJwsToken acmeRequest, X509Certificate2 certificate)
        {
            if (acmeRequest.AcmeHeader.Jwk is not null)
            {
                var certificateJwk = JsonWebKeyConverter.ConvertFromX509SecurityKey(new X509SecurityKey(certificate));
                return acmeRequest.AcmeHeader.Jwk.SecurityKey.ComputeJwkThumbprint() == certificateJwk.ComputeJwkThumbprint();
            }
            return false;
        }
    }
}
