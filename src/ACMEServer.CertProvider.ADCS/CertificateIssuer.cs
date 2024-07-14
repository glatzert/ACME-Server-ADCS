using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using TGIT.ACME.Protocol.Model;
using CertCli = CERTCLILib;

namespace TGIT.ACME.Protocol.IssuanceServices.ADCS
{
    public sealed class CertificateIssuer : ICertificateIssuer
    {
        private const int CR_IN_BASE64 = 0x1;
        private const int CR_OUT_BASE64 = 0x1;
        private const int CR_OUT_CHAIN = 0x100;

        private readonly IOptions<ADCSOptions> _options;
        private readonly ILogger<CertificateIssuer> _logger;

        public CertificateIssuer(IOptions<ADCSOptions> options, ILogger<CertificateIssuer> logger)
        {
            _options = options;
            _logger = logger;
        }

        public Task<(byte[]? Certificates, AcmeError? Error)> IssueCertificate(string csr, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Try to issue certificate for CSR: {csr}", csr);
            var result = (Certificates: (byte[]?)null, Error: (AcmeError?)null);

            try
            {
                var certRequest = new CertCli.CCertRequest();
                var attributes = $"CertificateTemplate:{_options.Value.TemplateName}";
                var submitResponseCode = certRequest.Submit(CR_IN_BASE64, csr, attributes, _options.Value.CAServer);

                if(submitResponseCode == 3)
                {
                    var issuerResponse = certRequest.GetCertificate(CR_OUT_BASE64 | CR_OUT_CHAIN);
                    var issuerResponseBytes = Convert.FromBase64String(issuerResponse);

                    var issuerSignedCms = new SignedCms();
                    issuerSignedCms.Decode(issuerResponseBytes);
                    result.Certificates = issuerSignedCms.Certificates.Export(X509ContentType.Pfx);

                    _logger.LogDebug("Certificate has been issued.");
                } 
                else
                {
                    _logger.LogError("Tried using Config {CAServer} and Template {TemplateName} to issue certificate", _options.Value.CAServer, _options.Value.TemplateName);
                    _logger.LogError("Certificate could not be issued. ResponseCode: {submitResponseCode}.", submitResponseCode);

                    result.Error = new AcmeError("serverInternal", "Certificate Issuance failed. Contact Administrator.");
                }
            } 
            catch (Exception ex)
            {
                _logger.LogError("Tried using Config {CAServer} and Template {TemplateName} to issue certificate", _options.Value.CAServer, _options.Value.TemplateName);
                _logger.LogError(ex, "Exception has been raised during certificate issuance.");
                result.Error = new AcmeError("serverInternal", "Certificate Issuance failed. Contact Administrator");
            }

            return Task.FromResult(result);
        }
    }
}
