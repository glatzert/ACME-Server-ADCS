using System;
using System.Threading;
using System.Threading.Tasks;
using CertCli = CERTCLILib;
using TGIT.ACME.Protocol.Model;
using Microsoft.Extensions.Options;

namespace TGIT.ACME.Protocol.IssuanceServices.ACDS
{
    public sealed class CertificateIssuer : ICertificateIssuer
    {
        private const int CR_IN_BASE64 = 0x1;
        private const int CR_OUT_BASE64 = 0x1;
        private const int CR_OUT_CHAIN = 0x100;

        private readonly IOptions<ACDSOptions> _options;

        public CertificateIssuer(IOptions<ACDSOptions> options)
        {
            _options = options;
        }

        public Task<(byte[]? certificate, AcmeError? error)> IssueCertificate(string csr, CancellationToken cancellationToken)
        {
            var result = (Certificate: (byte[]?)null, Error: (AcmeError?)null);

            try
            {
                var certRequest = new CertCli.CCertRequest();
                var attributes = $"CertificateTemplate:{_options.Value.TemplateName}";
                var submitResponseCode = certRequest.Submit(CR_IN_BASE64, csr, attributes, _options.Value.CAServer);

                if(submitResponseCode == 3)
                {
                    var base64Certificate = certRequest.GetCertificate(CR_OUT_BASE64 | CR_OUT_CHAIN);
                    result.Certificate = Convert.FromBase64String(base64Certificate);
                } 
                else
                {
                    result.Error = new AcmeError("serverInternal", "Certificate Issuance failed. Contact Administrator.");
                }
            } catch (Exception)
            {
                result.Error = new AcmeError("serverInternal", "Certificate Issuance failed. Contact Administrator");
            }

            return Task.FromResult(result);
        }
    }
}
